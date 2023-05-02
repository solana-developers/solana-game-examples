use anchor_lang::prelude::*;
pub use crate::errors::TinyAdventureError;
use crate::CHEST_REWARD;
use crate::PLAYER_KILL_REWARD;
const BOARD_SIZE_X: usize = 4;
const BOARD_SIZE_Y: usize = 4;

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
        init_if_needed,
        payer = signer, 
        seeds = [b"level105"],
        bump,
        space = 3000
    )]
    pub new_game_data_account: AccountLoader<'info, GameDataAccount>,
    // This is the PDA in which we will deposit the reward SOl and
    // from where we send it back to the first player reaching the chest.
    #[account(
        init_if_needed,
        seeds = [b"chestVault105"],
        bump,
        payer = signer,
        space = 8
    )]
    pub chest_vault: Box<Account<'info, ChestVaultAccount>>,
    pub system_program: Program<'info, System>,
}

#[account(zero_copy)]
#[repr(packed)]
#[derive(Default)]
pub struct GameDataAccount {
    board: [[Tile; BOARD_SIZE_X]; BOARD_SIZE_Y], // 9 * (41) = 369
}

#[zero_copy]
#[repr(packed)]
#[derive(Default)]
pub struct Tile {
    player: Pubkey,      // 32
    state: u8,           // 1
    health: u8,          // 1
    collect_reward: u64, // 8
    avatar: Pubkey,      // 32
    kills: u8,
}

impl GameDataAccount {
    pub fn print(&mut self) -> Result<()> {
        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                let mut tile = self.board[x][y];
                if tile.state == STATE_EMPTY {
                    msg!("empty")
                } else {
                    msg!("{} {}", tile.player, tile.state)
                }
            }
        }

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
                let mut tile = self.board[x][y];
                if tile.state == STATE_PLAYER {
                    if tile.player == player.key.clone() {
                        player_position = Some((x, y));
                    }
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
                            return Err(TinyAdventureError::TileOutOfBounds.into());
                        }
                        new_player_position.1 -= 1;
                    }
                    // Right
                    1 => {
                        if new_player_position.0 == BOARD_SIZE_X - 1 {
                            return Err(TinyAdventureError::TileOutOfBounds.into());
                        }
                        new_player_position.0 += 1;
                    }
                    // Down
                    2 => {
                        if new_player_position.1 == BOARD_SIZE_Y - 1 {
                            return Err(TinyAdventureError::TileOutOfBounds.into());
                        }
                        new_player_position.1 += 1;
                    }
                    // Left
                    3 => {
                        if new_player_position.0 == 0 {
                            return Err(TinyAdventureError::TileOutOfBounds.into());
                        }
                        new_player_position.0 -= 1;
                    }
                    _ => {
                        return Err(TinyAdventureError::WrongDirectionInput.into());
                    }
                }

                let mut tile = self.board[new_player_position.0][new_player_position.1];
                if tile.state == STATE_EMPTY {
                    self.board[new_player_position.0][new_player_position.1] =
                        self.board[player_position.unwrap().0][player_position.unwrap().1];
                    self.board[player_position.unwrap().0][player_position.unwrap().1].state =
                        STATE_EMPTY;
                    //msg!("Moved player to new tile");
                } else {
                    /*msg!(
                        "player position {} {}",
                        player_position.unwrap().0,
                        player_position.unwrap().1
                    );
                    msg!(
                        "new player position {} {}",
                        new_player_position.0,
                        new_player_position.1
                    );*/
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

                    //msg!("{} type {}", tile.player, tile.state);
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

    pub fn spawn_player(&mut self, player: AccountInfo, avatar: Pubkey) -> Result<()> {
        let mut empty_slots: Vec<(usize, usize)> = Vec::new();

        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                let mut tile = self.board[x][y];
                if tile.state == STATE_EMPTY {
                    empty_slots.push((x, y));
                } else {
                    if tile.player == player.key.clone() && tile.state == STATE_PLAYER {
                        return Err(TinyAdventureError::PlayerAlreadyExists.into());
                    }
                    msg!("{}", tile.player);
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
            health: 1,
            collect_reward: PLAYER_KILL_REWARD,
        };

        Ok(())
    }

    pub fn spawn_chest(&mut self, player: AccountInfo) -> Result<()> {
        let mut empty_slots: Vec<(usize, usize)> = Vec::new();

        for x in 0..BOARD_SIZE_X {
            for y in 0..BOARD_SIZE_Y {
                let mut tile = self.board[x][y];
                if tile.state == STATE_EMPTY {
                    empty_slots.push((x, y));
                } else {
                    msg!("{}", tile.player);
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
            collect_reward: CHEST_REWARD,
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
pub struct SpawnPlayer<'info> {
     /// CHECK:
     #[account(mut)]
    pub payer: AccountInfo<'info>,
     /// CHECK:
     #[account(mut)]
    pub chest_vault: AccountInfo<'info>,
    #[account(mut)]
    pub game_data_account: AccountLoader<'info, GameDataAccount>,
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
