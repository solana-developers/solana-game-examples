use anchor_lang::prelude::*;
use anchor_spl::{
    associated_token::AssociatedToken,
    token::{Mint, Token, TokenAccount, Transfer},
};
pub use crate::errors::SevenSeasError;
use crate::CHEST_REWARD;
use crate::PLAYER_KILL_REWARD;
use crate::seven_seas::Ship;
const BOARD_SIZE_X: usize = 10;
const BOARD_SIZE_Y: usize = 10;

const STATE_EMPTY: u8 = 0;
const STATE_PLAYER: u8 = 1;
const STATE_CHEST: u8 = 2;

const CHEST_COIN_REWARD: u64 = 10;

pub const TOKEN_DECIMAL_MULTIPLIER: u64 = 1000000000;

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
        mut,
        seeds = [b"level"],
        bump,
    )]
    pub game_data_account: AccountLoader<'info, GameDataAccount>,
}

#[account(zero_copy(unsafe))]
#[repr(packed)]
#[derive(Default)]
pub struct GameDataAccount {
    board: [[Tile; BOARD_SIZE_X]; BOARD_SIZE_Y],
    action_id: u64,
}

#[zero_copy(unsafe)]
#[repr(packed)]
#[derive(Default)]
pub struct Tile {
    player: Pubkey,      // 32
    state: u8,           // 1
    health: u64,         // 8
    damage: u64,         // 8
    range: u16,          // 2
    collect_reward: u64, // 8
    avatar: Pubkey,      // 32 used in the client to display the avatar
    look_direction: u8,  // 1 (Up, right, down, left) 
    ship_level: u16,     // 2
    start_health: u64,   // 8
}

#[account]
pub struct GameActionHistory {
    id_counter: u64,
    game_actions: Vec<GameAction>,
}

#[derive(Debug, Clone, AnchorSerialize, AnchorDeserialize)]
pub struct GameAction {
    action_id: u64,      // 1
    action_type: u8,     // 1
    player: Pubkey,      // 32
    target: Pubkey,      // 32
    damage: u64,         // 8  
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

    pub fn reset(&mut self) -> Result<()> {
        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                self.board[x][y].state = STATE_EMPTY
            }
        }

        Ok(())
    }

    pub fn euclidean_distance(x1: &usize, x2: &usize, y1: &usize, y2: &usize) -> f64 {
        let dx = (x1 - x2) as f64;
        let dy = (y1 - y2) as f64;
        (dx * dx + dy * dy).sqrt()
    }

    pub fn cthulhu<'info>(
        &mut self,
        _player: AccountInfo,
        game_actions: &mut GameActionHistory,
        _chest_vault: AccountInfo,
        _vault_token_account: AccountInfo<'info>,
        _player_token_account: AccountInfo<'info>,
        _token_account_owner_pda: AccountInfo<'info>,
        _token_program: AccountInfo<'info>,
        _token_owner_bump: u8,
    ) -> Result<()> {
    
        let mut smallest_distance: f64 = 100000.0;
        let mut attacked_player_position: Option<(usize, usize)> = None;

        let cthulhu_position: (usize, usize) = (0, 0);

        // Find closest player on the board
        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                let tile = self.board[x][y];                
                if tile.state == STATE_PLAYER {
                    let distance = Self::euclidean_distance(&x, &cthulhu_position.0, &y, &cthulhu_position.0);
                    if distance < smallest_distance {
                        smallest_distance = distance;
                        attacked_player_position = Some((x, y));
                    }                    
                }
            }
        }

        // Found a player on the board that we can attack
        match attacked_player_position {
            None => {
                return Err(SevenSeasError::CouldNotFindAShipToAttack.into());
            }
            Some(val) => {                
                self.increase_action_id();

                let mut tile = &mut self.board[val.0][val.1];

                let mut rng = XorShift64 {
                    a: tile.health as u64,
                };

                let chtulu_damage: u64 = 10;
                let damage_variant = ((chtulu_damage as f64) * 0.3).ceil() as u64;
                let damage = chtulu_damage + (rng.next() % damage_variant);
                let option = tile.health.checked_sub(damage);
                match option {
                    None => {
                        tile.health = 0;
                    }
                    Some(val) => {
                        tile.health = val;
                    }
                }

                if tile.health == 0 {
                    tile.state = STATE_EMPTY;
                }

                let item = GameAction {
                    action_id: self.action_id,
                    action_type: 2,
                    player: tile.player.key(),
                    target: tile.player.key(),
                    damage: damage
                };
        
                if game_actions.game_actions.len() > 20 {
                    game_actions.game_actions.drain(0..5);
                }
        
                game_actions.game_actions.push(item);  

                msg!("Attack closes enemy is at {} {} with damage {}", val.0, val.1, damage);
            }
        }

        Ok(())
    }

    pub fn shoot<'info>(
        &mut self,
        player: AccountInfo,
        game_actions: &mut GameActionHistory,
        chest_vault: AccountInfo,
        vault_token_account: AccountInfo<'info>,
        player_token_account: AccountInfo<'info>,
        token_account_owner_pda: AccountInfo<'info>,
        token_program: AccountInfo<'info>,
        token_owner_bump: u8,
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
                    msg!("{} {}", tile.player, tile.state);
                }
            }
        }

        // If the player is on the board shoot
        match player_position {
            None => {
                return Err(SevenSeasError::TriedToShootWithPlayerThatWasNotOnTheBoard.into());
            }
            Some(val) => {                
                msg!("Player position x:{} y:{}", val.0, val.1);
                let player_tile: Tile = self.board[val.0][val.1];
                let range_usize : usize = usize::from(player_tile.range);
                for range in 1..range_usize+1 {
                    
                    // Shoot left
                    if player_tile.look_direction % 2 == 0 && val.0 >= range {
                        self.attack_tile((val.0 - range, val.1), player_tile.damage, player.clone(), chest_vault.clone(),game_actions, &vault_token_account, &player_token_account, &token_account_owner_pda, &token_program, token_owner_bump)?;
                    } 

                    // Shoot right
                    if player_tile.look_direction % 2 == 0 && val.0 < BOARD_SIZE_X -range {
                        self.attack_tile((val.0 + range, val.1), player_tile.damage, player.clone(), chest_vault.clone(),game_actions, &vault_token_account, &player_token_account, &token_account_owner_pda, &token_program, token_owner_bump)?;
                    } 
                    
                    // Shoot down
                    if player_tile.look_direction % 2 == 1 && val.1 < BOARD_SIZE_Y -range {
                        self.attack_tile((val.0, val.1 + range), player_tile.damage, player.clone(), chest_vault.clone(),game_actions, &vault_token_account, &player_token_account, &token_account_owner_pda, &token_program, token_owner_bump)?;
                    } 
                    
                    // Shoot up
                    if player_tile.look_direction % 2 == 1 && val.1 >= range {
                        self.attack_tile((val.0, val.1 - range), player_tile.damage, player.clone(), chest_vault.clone(),game_actions, &vault_token_account, &player_token_account, &token_account_owner_pda, &token_program, token_owner_bump)?;
                    }                    
                }

                self.increase_action_id();

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

    fn increase_action_id(&mut self) {
        let option_add = self.action_id.checked_add(1);
        match option_add {
            Some(val) =>  {
                self.action_id = val;
            }, 
            None => {
                self.action_id = 0;
            }
        }
    }

    fn attack_tile<'info>(&mut self, attacked_position: (usize, usize), 
        damage: u64, 
        attacker: AccountInfo, 
        chest_vault: AccountInfo, 
        game_actions: &mut GameActionHistory,
        vault_token_account: &AccountInfo<'info>,
        player_token_account: &AccountInfo<'info>,
        token_account_owner_pda: &AccountInfo<'info>,
        token_program: &AccountInfo<'info>,
        token_owner_bump: u8,) -> Result<()> {
        let mut attacked_tile: Tile = self.board[attacked_position.0][attacked_position.1];
        msg!("Attack x:{} y:{}", attacked_position.0, attacked_position.1);

        let transfer_instruction = Transfer {
            from: vault_token_account.to_account_info(),
            to: player_token_account.to_account_info(),
            authority: token_account_owner_pda.to_account_info(),
        };

        let seeds = &[b"token_account_owner_pda".as_ref(), &[token_owner_bump]];
        let signer = &[&seeds[..]];

        let cpi_ctx = CpiContext::new_with_signer(
            token_program.to_account_info(),
            transfer_instruction,
            signer,
        );

        self.increase_action_id();

        if game_actions.game_actions.len() > 20 {
            game_actions.game_actions.drain(0..5);
        }

        if attacked_tile.state == STATE_PLAYER {
            let match_option = attacked_tile.health.checked_sub(damage);
            match  match_option {
                None => {
                    attacked_tile.health = 0;
                    self.on_ship_died(attacked_position, attacked_tile, chest_vault, &attacker)?;  
                    anchor_spl::token::transfer(cpi_ctx, (attacked_tile.ship_level as u64) * 10*TOKEN_DECIMAL_MULTIPLIER)?;           
                },
                Some(value) =>  {
                    msg!("New health {}", value);
                    self.board[attacked_position.0][attacked_position.1].health = value;
                    if value == 0 {
                        self.on_ship_died(attacked_position, attacked_tile, chest_vault, &attacker)?;  
                        anchor_spl::token::transfer(cpi_ctx, (attacked_tile.ship_level as u64) * 10*TOKEN_DECIMAL_MULTIPLIER)?;           
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

    pub fn move_in_direction_by_thread<'info>(
        &mut self
    ) -> Result<()> {
        let mut alive_players: Vec<(usize, usize)> = Vec::new();

        // Find the player on the board
        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                if self.board[x][y].state == STATE_PLAYER { 
                    let new_position:(usize, usize) = (x, y);
                    alive_players.push(new_position)
                }
            }
        }

        for player in alive_players {
            if self.board[player.0][player.1].state == STATE_PLAYER {
                let mut new_position:(usize, usize) = (player.0, player.1);
                msg!("Player found at x:{} y:{}", player.0, player.1);
                match self.board[player.0][player.1].look_direction {
                    // Up
                    0 => {
                        if new_position.1 == 0 {
                            new_position.1 = BOARD_SIZE_Y - 1;
                        } else {
                            new_position.1 -= 1;
                        }                    
                    }
                    // Right
                    1 => {
                        if new_position.0 == BOARD_SIZE_X - 1 {
                            new_position.0 = 0;
                        } else {
                            new_position.0 += 1;
                        }                        
                    }
                    // Down
                    2 => {
                        if new_position.1 == BOARD_SIZE_Y - 1 {
                            new_position.1 = 0;
                        } else {
                            new_position.1 += 1;
                        }                    
                    }
                    // Left
                    3 => {
                        if new_position.0 == 0 {
                            new_position.0 = BOARD_SIZE_X -1;
                        } else {
                            new_position.0 -= 1;
                        }                        
                    }
                    _ => {
                        return Err(SevenSeasError::WrongDirectionInput.into());
                    }
                }
                
                if self.board[new_position.0][new_position.1].state == STATE_EMPTY {
                    msg!("Move to x:{} y:{}", new_position.0, new_position.1);
                    self.board[new_position.0][new_position.1] = self.board[player.0][player.1];
                    self.board[player.0][player.1].state = STATE_EMPTY;
                }
            }
        }


        Ok(())
    }

    pub fn move_in_direction<'info>(
        &mut self,
        direction: u8,
        player: AccountInfo,
        chest_vault: AccountInfo,
        vault_token_account: AccountInfo<'info>,
        player_token_account: AccountInfo<'info>,
        token_account_owner_pda: AccountInfo<'info>,
        token_program: AccountInfo<'info>,
        token_owner_bump: u8,
        game_actions: &mut GameActionHistory,
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
                return Err(SevenSeasError::TriedToMovePlayerThatWasNotOnTheBoard.into());
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
                        return Err(SevenSeasError::WrongDirectionInput.into());
                    }
                }

                let new_tile = self.board[new_player_position.0][new_player_position.1];
                if new_tile.state == STATE_EMPTY {
                    self.board[new_player_position.0][new_player_position.1] =
                        self.board[player_position.unwrap().0][player_position.unwrap().1];
                    self.board[player_position.unwrap().0][player_position.unwrap().1].state =
                        STATE_EMPTY;
                    self.board[new_player_position.0][new_player_position.1].look_direction = direction;

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
                    if new_tile.state == STATE_CHEST {
                        self.board[new_player_position.0][new_player_position.1] =
                            self.board[player_position.unwrap().0][player_position.unwrap().1];
                        self.board[player_position.unwrap().0][player_position.unwrap().1].state =
                            STATE_EMPTY;
                        **chest_vault.try_borrow_mut_lamports()? -= new_tile.collect_reward;
                        **player.try_borrow_mut_lamports()? += new_tile.collect_reward;
                        let transfer_instruction = Transfer {
                            from: vault_token_account,
                            to: player_token_account,
                            authority: token_account_owner_pda,
                        };
                
                        let seeds = &[b"token_account_owner_pda".as_ref(), &[token_owner_bump]];
                        let signer = &[&seeds[..]];
                
                        let cpi_ctx = CpiContext::new_with_signer(
                            token_program,
                            transfer_instruction,
                            signer,
                        );
                        anchor_spl::token::transfer(cpi_ctx, CHEST_COIN_REWARD*TOKEN_DECIMAL_MULTIPLIER)?;

                        self.increase_action_id();

                        let item = GameAction {
                            action_id: self.action_id,
                            action_type: 3,
                            player: player.key(),
                            target: player.key(),
                            damage: CHEST_COIN_REWARD
                        };
                        game_actions.game_actions.push(item);

                        msg!("Collected Chest");
                    } else if new_tile.state == STATE_PLAYER {
                        self.attack_tile((new_player_position.0,new_player_position.1),
                         1,
                         player.clone(),
                          chest_vault.clone(),
                          game_actions, &vault_token_account, &player_token_account, &token_account_owner_pda, &token_program, token_owner_bump)?;

                        /*self.board[new_player_position.0][new_player_position.1] =
                            self.board[player_position.unwrap().0][player_position.unwrap().1];
                        self.board[player_position.unwrap().0][player_position.unwrap().1].state =
                            STATE_EMPTY;
                        **chest_vault.try_borrow_mut_lamports()? -= new_tile.collect_reward;
                        **player.try_borrow_mut_lamports()? += new_tile.collect_reward;
                        anchor_spl::token::transfer(cpi_ctx, (new_tile.ship_level as u64) * 10*TOKEN_DECIMAL_MULTIPLIER)?;*/
                        msg!("Other player killed");
                    }

                    msg!("{} type {}", new_tile.player, new_tile.state);
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

    pub fn spawn_player(&mut self, player: AccountInfo, avatar: Pubkey, ship: &mut Ship, extra_health: u64) -> Result<()> {
        let mut empty_slots: Vec<(usize, usize)> = Vec::new();

        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                let tile = self.board[x][y];
                if tile.state == STATE_EMPTY {
                    empty_slots.push((x, y));
                } else {
                    if tile.player == player.key.clone() && tile.state == STATE_PLAYER {
                        return Err(SevenSeasError::PlayerAlreadyExists.into());
                    }
                    //msg!("{}", tile.player);
                }
            }
        }

        if empty_slots.len() == 0 {
            return Err(SevenSeasError::BoardIsFull.into());
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

        let mut range: u16 = 1;

        match ship.upgrades {
            0 | 1 |  2 => {
                range = 1;
            } 
            _ => {
                range = 2;
            }
        }

        self.board[random_empty_slot.0][random_empty_slot.1] = Tile {
            player: player.key.clone(),
            avatar: avatar.clone(),
            state: STATE_PLAYER,
            health: ship.health + extra_health,
            start_health: ship.health + extra_health,
            damage: ship.cannons,
            range: range,
            collect_reward: PLAYER_KILL_REWARD,
            look_direction: 0,
            ship_level: ship.upgrades,
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
            return Err(SevenSeasError::BoardIsFull.into());
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
            state: STATE_CHEST,
            health: 1,
            start_health: 1,
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
    #[account(mut)]
    pub player: Signer<'info>,
    /// CHECK:
    pub token_account_owner: AccountInfo<'info>,
    pub system_program: Program<'info, System>,
    #[account(      
        init_if_needed,
        payer = player,
        associated_token::mint = mint_of_token_being_sent,
        associated_token::authority = token_account_owner      
    )]
    pub player_token_account: Account<'info, TokenAccount>,
    #[account(
        init_if_needed,
        payer = player,
        seeds=[b"token_vault".as_ref(), mint_of_token_being_sent.key().as_ref()],
        token::mint=mint_of_token_being_sent,
        token::authority=token_account_owner_pda,
        bump
    )]
    pub vault_token_account: Account<'info, TokenAccount>,
    /// CHECK:
    #[account(
        mut,
        seeds=[b"token_account_owner_pda".as_ref()],
        bump
    )]
    pub token_account_owner_pda: AccountInfo<'info>,    
    pub mint_of_token_being_sent: Account<'info, Mint>,
    pub token_program: Program<'info, Token>,
    pub associated_token_program: Program<'info, AssociatedToken>,
    #[account(mut)]
    pub game_actions: Account<'info, GameActionHistory>,
}

#[derive(Accounts)]
pub struct Shoot<'info> {
    /// CHECK:
    #[account(
        mut,
        seeds = [b"chestVault"],
        bump
    )]
    pub chest_vault: AccountInfo<'info>,
    #[account(mut)]
    pub game_data_account: AccountLoader<'info, GameDataAccount>,
    #[account(mut)]
    pub game_actions: Account<'info, GameActionHistory>,
    #[account(mut)]
    pub player: Signer<'info>,
    pub system_program: Program<'info, System>,
    /// CHECK:
    pub token_account_owner: AccountInfo<'info>,
    #[account(      
        init_if_needed,
        payer = player,
        associated_token::mint = mint_of_token_being_sent,
        associated_token::authority = token_account_owner      
    )]
    pub player_token_account: Account<'info, TokenAccount>,
    #[account(
        mut,
        seeds=[b"token_vault".as_ref(), mint_of_token_being_sent.key().as_ref()],
        token::mint=mint_of_token_being_sent,
        token::authority=token_account_owner_pda,
        bump
    )]
    pub vault_token_account: Account<'info, TokenAccount>,
    /// CHECK:
    #[account(
        mut,
        seeds=[b"token_account_owner_pda".as_ref()],
        bump
    )]
    pub token_account_owner_pda: AccountInfo<'info>,    
    pub mint_of_token_being_sent: Account<'info, Mint>,
    pub token_program: Program<'info, Token>,
    pub associated_token_program: Program<'info, AssociatedToken>,
}

#[derive(Accounts)]
pub struct SpawnPlayer<'info> {
    /// CHECK: needs to sign for every move later. Would be better if both are signers
    #[account(mut)]
    pub player: AccountInfo<'info>,
    #[account(mut)]
    pub token_account_owner: Signer<'info>,
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
    #[account(      
        init_if_needed,
        payer = token_account_owner,
        associated_token::mint = cannon_mint,
        associated_token::authority = token_account_owner      
    )]
    pub cannon_token_account: Account<'info, TokenAccount>,
    pub cannon_mint: Account<'info, Mint>,
    #[account(      
        init_if_needed,
        payer = token_account_owner,
        associated_token::mint = rum_mint,
        associated_token::authority = token_account_owner      
    )]
    pub rum_token_account: Account<'info, TokenAccount>,
    pub rum_mint: Account<'info, Mint>,
    pub token_program: Program<'info, Token>,
    pub associated_token_program: Program<'info, AssociatedToken>,
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
