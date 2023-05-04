use anchor_lang::prelude::*;
use anchor_spl::token::Mint;
use anchor_spl::token::Token;
use anchor_spl::token::TokenAccount;
pub use crate::errors::TinyAdventureError;
use crate::CHEST_REWARD;
use crate::PLAYER_KILL_REWARD;
use crate::seven_seas::Ship;
const BOARD_SIZE_X: usize = 10;
const BOARD_SIZE_Y: usize = 10;

const STATE_EMPTY: u8 = 0;
const STATE_PLAYER: u8 = 1;
const STATE_CHEST: u8 = 2;

#[derive(Accounts)]
pub struct Initialize<'info> {
    #[account(mut)]
    pub signer: Signer<'info>,
    // We must specify the space in order to initialize an account.
    // First 8 bytes are default account discriminator,
    #[account(
        init,
        payer = signer, 
        seeds = [b"level"],
        bump,
        space = 10240
    )]
    pub new_game_data_account: AccountLoader<'info, GameDataAccount>,
    // This is the PDA in which we will deposit the reward SOl and
    // from where we send it back to the first player reaching the chest.
    #[account(
        init,
        seeds = [b"chestVault"],
        bump,
        payer = signer,
        space = 8
    )]
    pub chest_vault: Box<Account<'info, ChestVaultAccount>>,
    // These are used so that the clients can animate certain actions in the game.
    #[account(
        init,
        seeds = [b"gameActions"],
        bump,
        payer = signer,
        space = 4096
    )]
    pub game_actions: Box<Account<'info, GameActionHistory>>,
    /// CHECK: Derived PDAs
    #[account(
        init,
        payer = signer,
        seeds=[b"token_account_owner_pda".as_ref()],
        bump,
        space = 8
    )]
    token_account_owner_pda: AccountInfo<'info>,

    #[account(
        init,
        payer = signer,
        seeds=[b"token_vault".as_ref(), mint_of_token_being_sent.key().as_ref()],
        token::mint=mint_of_token_being_sent,
        token::authority=token_account_owner_pda,
        bump
    )]
    vault_token_account: Account<'info, TokenAccount>,

    pub mint_of_token_being_sent: Account<'info, Mint>,
    
    pub system_program: Program<'info, System>,
    pub token_program: Program<'info, Token>,
}

#[derive(Accounts)]
pub struct Reset<'info> {
    #[account(mut)]
    pub signer: Signer<'info>,
    // We must specify the space in order to initialize an account.
    // First 8 bytes are default account discriminator,
    #[account(
        seeds = [b"level"],
        bump,
    )]
    pub new_game_data_account: AccountLoader<'info, GameDataAccount>,
    // This is the PDA in which we will deposit the reward SOl and
    // from where we send it back to the first player reaching the chest.
    #[account(
        seeds = [b"chestVault"],
        bump,
    )]
    pub chest_vault: Box<Account<'info, ChestVaultAccount>>,
    #[account(
        seeds = [b"gameActions"],
        bump,
    )]
    pub game_actions: Box<Account<'info, GameActionHistory>>,
    pub system_program: Program<'info, System>,
}

#[account(zero_copy)]
#[repr(packed)]
#[derive(Default)]
pub struct GameDataAccount {
    board: [[Tile; BOARD_SIZE_X]; BOARD_SIZE_Y],
    action_id: u64,
}

#[account]
pub struct GameActionHistory {
    id_counter: u64,
    game_actions: Vec<GameAction>,
}

#[derive(Debug, Clone, AnchorSerialize, AnchorDeserialize)]
pub struct GameAction {
    action_id: u64,        // 1
    action_type: u8,      // 1
    player: Pubkey,      // 32
    target: Pubkey,      // 32
    damage: u16,         // 2   
}

#[zero_copy]
#[repr(packed)]
#[derive(Default)]
pub struct Tile {
    player: Pubkey,      // 32
    state: u8,           // 1
    health: u16,         // 1
    damage: u16,         // 1
    range: u16,          // 1
    collect_reward: u64, // 8
    avatar: Pubkey,      // 32
    kills: u8,           // 1
    look_direction: u8,  // 1 (Up, right, down, left) 
    ship_level: u16,     // 2
}

impl GameDataAccount {
    pub fn print(&mut self) -> Result<()> {
        // print will only work locally for debugging otherwise its eats too muc compute
        
        /* 
        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                let tile = self.board[x][y  ];
                if tile.state == STATE_EMPTY {
                    msg!("empty")
                } else {
                    msg!("{} {}", tile.player, tile.state)
                }
            }
        }*/

        Ok(())
    }

    pub fn shoot(
        &mut self,
        player: AccountInfo,
        game_actions: &mut GameActionHistory,
        chest_vault: AccountInfo
    ) -> Result<()> {

        let option_add = self.action_id.checked_add(1);
        match option_add {
            Some(val) =>  {
                self.action_id = val;
            }, 
            None => {
                self.action_id = 0;
            }
        }
    
        let mut player_position: Option<(usize, usize)> = None;

        // Find the player on the board
        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                let tile = self.board[x][y];
                if tile.state == STATE_PLAYER {
                    if tile.player == player.key.clone() {
                        player_position = Some((x, y));
                    }
                    msg!("{} {}", tile.player, tile.state);
                }
            }
        }

        // If the player is on the board shoot
        match player_position {
            None => {
                return Err(TinyAdventureError::TriedToShootWithPlayerThatWasNotOnTheBoard.into());
            }
            Some(val) => {
                
                msg!("Player position x:{} y:{}", val.0, val.1);
                let player_tile: Tile = self.board[val.0][val.1];
                let range_usize : usize = usize::from(player_tile.range);
                for range in 1..range_usize+1 {
                    
                    // Shoot left
                    if player_tile.look_direction % 2 == 1 && val.0 >= range {
                        self.attack_tile((val.0 - range, val.1), player_tile.damage, player.clone(), chest_vault.clone(),game_actions)?;
                    }

                    // Shoot right
                    if player_tile.look_direction % 2 == 1 && val.0 < BOARD_SIZE_X -range {
                        self.attack_tile((val.0 + range, val.1), player_tile.damage, player.clone(), chest_vault.clone(),game_actions)?;
                    }
                    
                    // Shoot down
                    if player_tile.look_direction % 2 == 0 && val.1 < BOARD_SIZE_Y -range {
                        self.attack_tile((val.0, val.1 + range), player_tile.damage, player.clone(), chest_vault.clone(),game_actions)?;
                    }
                    
                    // Shoot up
                    if player_tile.look_direction % 2 == 0 && val.1 >= range {
                        self.attack_tile((val.0, val.1 - range), player_tile.damage, player.clone(), chest_vault.clone(),game_actions)?;
                    }                    
                }

                let item = GameAction {
                    action_id: self.action_id,
                    action_type: 0,
                    player: player_tile.player.key(),
                    target: player_tile.player.key(),
                    damage: player_tile.damage
                };
        
                if game_actions.game_actions.len() > 20 {
                    game_actions.game_actions.drain(0..5);
                }
        
                game_actions.game_actions.push(item);                
            }
        }

        Ok(())
    }

    fn attack_tile(&mut self, attacked_position: (usize, usize), damage: u16, attacker: AccountInfo, chest_vault: AccountInfo, game_actions: &mut GameActionHistory,)  -> Result<()> {
        let mut attacked_tile: Tile = self.board[attacked_position.0][attacked_position.1];
        msg!("Attack x:{} y:{}", attacked_position.0, attacked_position.1);

        if attacked_tile.state == STATE_PLAYER {
            let match_option = attacked_tile.health.checked_sub(damage);
            match  match_option {
                None => {
                    attacked_tile.health = 0;
                    self.on_ship_died(attacked_position, attacked_tile, chest_vault, &attacker)?;             
                },
                Some(value) =>  {
                    msg!("New health {}", value);
                    self.board[attacked_position.0][attacked_position.1].health = value;
                    if value == 0 {
                        self.on_ship_died(attacked_position, attacked_tile, chest_vault, &attacker)?;  
                    }
                }   
            };    
            let item = GameAction {
                action_id: self.action_id,
                action_type: 1,
                player: attacker.key(),
                target: attacked_tile.player.key(),
                damage: damage
            };
    
            if game_actions.game_actions.len() > 20 {
                game_actions.game_actions.drain(0..5);
            }
    
            game_actions.game_actions.push(item);
        }
        Ok(())
    }

    fn on_ship_died(&mut self, attacked_position: (usize, usize), attacked_tile: Tile, chest_vault: AccountInfo, attacker: &AccountInfo) -> Result<()> {
        msg!("Enemy killed x:{} y:{} pubkey: {}", attacked_position.0, attacked_position.1, attacked_tile.player);
        self.board[attacked_position.0][attacked_position.1].state = STATE_EMPTY;
        **chest_vault.try_borrow_mut_lamports()? -= attacked_tile.collect_reward;
        **attacker.try_borrow_mut_lamports()? += attacked_tile.collect_reward;
        Ok(())
    }

    pub fn move_in_direction(
        &mut self,
        direction: u8,
        player: AccountInfo,
        chest_vault: AccountInfo,
    ) -> Result<()> {
        let mut player_position: Option<(usize, usize)> = None;

        // Find the player on the board
        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                let tile = self.board[x][y];
                if tile.state == STATE_PLAYER {
                    if tile.player == player.key.clone() {
                        player_position = Some((x, y));
                    }
                    // Printing the whole board eats too much compute
                    //msg!("{} {}", tile.player, tile.state);
                }
            }
        }

        // If the player is on the board move him
        match player_position {
            None => {
                return Err(TinyAdventureError::TriedToMovePlayerThatWasNotOnTheBoard.into());
            }
            Some(val) => {
                let mut new_player_position: (usize, usize) = (val.0, val.1);
                match direction {
                    // Up
                    0 => {
                        if new_player_position.1 == 0 {
                            new_player_position.1 = BOARD_SIZE_Y - 1;
                        } else {
                            new_player_position.1 -= 1;
                        }                    
                    }
                    // Right
                    1 => {
                        if new_player_position.0 == BOARD_SIZE_X - 1 {
                            new_player_position.0 = 0;
                        } else {
                            new_player_position.0 += 1;
                        }                        
                    }
                    // Down
                    2 => {
                        if new_player_position.1 == BOARD_SIZE_Y - 1 {
                            new_player_position.1 = 0;
                        } else {
                            new_player_position.1 += 1;
                        }                    
                    }
                    // Left
                    3 => {
                        if new_player_position.0 == 0 {
                            new_player_position.0 = BOARD_SIZE_X -1;
                        } else {
                            new_player_position.0 -= 1;
                        }                        
                    }
                    _ => {
                        return Err(TinyAdventureError::WrongDirectionInput.into());
                    }
                }

                let mut tile = self.board[new_player_position.0][new_player_position.1];
                self.board[new_player_position.0][new_player_position.1].look_direction = direction;
                if tile.state == STATE_EMPTY {
                    self.board[new_player_position.0][new_player_position.1] =
                        self.board[player_position.unwrap().0][player_position.unwrap().1];
                    self.board[player_position.unwrap().0][player_position.unwrap().1].state =
                        STATE_EMPTY;
                    msg!("Moved player to new tile");
                } else {
                    msg!(
                        "player position {} {}",
                        player_position.unwrap().0,
                        player_position.unwrap().1
                    );
                    msg!(
                        "new player position {} {}",
                        new_player_position.0,
                        new_player_position.1
                    );
                    if tile.state == STATE_CHEST {
                        self.board[new_player_position.0][new_player_position.1] =
                            self.board[player_position.unwrap().0][player_position.unwrap().1];
                        self.board[player_position.unwrap().0][player_position.unwrap().1].state =
                            STATE_EMPTY;
                        **chest_vault.try_borrow_mut_lamports()? -= tile.collect_reward;
                        **player.try_borrow_mut_lamports()? += tile.collect_reward;
                        msg!("Collected Chest");
                    }
                    if tile.state == STATE_PLAYER {
                        self.board[new_player_position.0][new_player_position.1] =
                            self.board[player_position.unwrap().0][player_position.unwrap().1];
                        self.board[player_position.unwrap().0][player_position.unwrap().1].state =
                            STATE_EMPTY;
                        **chest_vault.try_borrow_mut_lamports()? -= tile.collect_reward;
                        **player.try_borrow_mut_lamports()? += tile.collect_reward;
                        msg!("Other player killed");
                    }

                    msg!("{} type {}", tile.player, tile.state);
                }
            }
        }

        Ok(())
    }

    pub fn clear(&mut self) -> Result<()> {
        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                self.board[x][y].state = STATE_EMPTY;
            }
        }
        Ok(())
    }

    pub fn spawn_player(&mut self, player: AccountInfo, avatar: Pubkey, ship: &mut Ship) -> Result<()> {
        let mut empty_slots: Vec<(usize, usize)> = Vec::new();

        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                let tile = self.board[x][y];
                if tile.state == STATE_EMPTY {
                    empty_slots.push((x, y));
                } else {
                    if tile.player == player.key.clone() && tile.state == STATE_PLAYER {
                        return Err(TinyAdventureError::PlayerAlreadyExists.into());
                    }
                    //msg!("{}", tile.player);
                }
            }
        }

        if empty_slots.len() == 0 {
            return Err(TinyAdventureError::BoardIsFull.into());
        }

        let mut rng = XorShift64 {
            a: empty_slots.len() as u64,
        };

        let random_empty_slot = empty_slots[(rng.next() % (empty_slots.len() as u64)) as usize];
        msg!(
            "Player spawn at {} {}",
            random_empty_slot.0,
            random_empty_slot.1
        );

        self.board[random_empty_slot.0][random_empty_slot.1] = Tile {
            player: player.key.clone(),
            avatar: avatar.clone(),
            kills: 0,
            state: STATE_PLAYER,
            health: ship.health,
            damage: ship.cannons,
            range: ship.level,
            collect_reward: PLAYER_KILL_REWARD,
            look_direction: 0,
            ship_level: ship.level,
        };

        Ok(())
    }

    pub fn spawn_chest(&mut self, player: AccountInfo) -> Result<()> {
        let mut empty_slots: Vec<(usize, usize)> = Vec::new();

        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                let tile = self.board[x][y];
                if tile.state == STATE_EMPTY {
                    empty_slots.push((x, y));
                } else {
                    //msg!("{}", tile.player);
                }
            }
        }

        if empty_slots.len() == 0 {
            return Err(TinyAdventureError::BoardIsFull.into());
        }

        let mut rng = XorShift64 {
            a: (empty_slots.len() + 1) as u64,
        };

        let random_empty_slot = empty_slots[(rng.next() % (empty_slots.len() as u64)) as usize];
        msg!(
            "Chest spawn at {} {}",
            random_empty_slot.0,
            random_empty_slot.1
        );

        self.board[random_empty_slot.0][random_empty_slot.1] = Tile {
            player: player.key.clone(),
            avatar: player.key.clone(),
            kills: 0,
            state: STATE_CHEST,
            health: 1,
            damage: 0,
            range: 0,
            collect_reward: CHEST_REWARD,
            look_direction: 0,
            ship_level: 0
        };

        Ok(())
    }
}

#[derive(Accounts)]
pub struct MovePlayer<'info> {
    /// CHECK:
    #[account(mut)]
    pub chest_vault: AccountInfo<'info>,
    #[account(mut)]
    pub game_data_account: AccountLoader<'info, GameDataAccount>,
    /// CHECK:
    #[account(mut)]
    pub player: AccountInfo<'info>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct Shoot<'info> {
    /// CHECK:
    #[account(mut)]
    pub chest_vault: AccountInfo<'info>,
    #[account(mut)]
    pub game_data_account: AccountLoader<'info, GameDataAccount>,
    #[account(mut)]
    pub game_actions: Account<'info, GameActionHistory>,
    #[account(mut)]
    pub player: Signer<'info>,
}

#[derive(Accounts)]
pub struct SpawnPlayer<'info> {
    #[account(mut)]
    pub payer: Signer<'info>,
    #[account(
        mut,
        seeds = [b"chestVault"],
        bump
    )]
    pub chest_vault: Account<'info, ChestVaultAccount>,
    #[account(mut)]
    pub game_data_account: AccountLoader<'info, GameDataAccount>,
    #[account(
        mut,
        seeds = [b"ship", nft_account.key().as_ref()],
        bump
    )]
    pub ship: Account<'info, Ship>,
    /// CHECK: change to token account later
    pub nft_account: AccountInfo<'info>,
    pub system_program: Program<'info, System>,
}

#[account]
pub struct ChestVaultAccount {}

pub struct XorShift64 {
    a: u64,
}

impl XorShift64 {
    pub fn next(&mut self) -> u64 {
        let mut x = self.a;
        x ^= x << 13;
        x ^= x >> 7;
        x ^= x << 17;
        self.a = x;
        x
    }
}
