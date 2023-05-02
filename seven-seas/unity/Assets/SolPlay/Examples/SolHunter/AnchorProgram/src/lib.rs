use anchor_lang::prelude::*;
use anchor_lang::solana_program::native_token::LAMPORTS_PER_SOL;

pub use crate::errors::TinyAdventureError;
pub mod errors;

pub mod state;
pub use state::*;

// This is your program's public key and it will update
// automatically when you build the project.
declare_id!("huntegMDH7NicWeJ7ezxiV4PsTrvMRkswNL4Uamm44h");

#[program]
pub mod sol_hunter {
    use super::*;
    pub const PLAYER_KILL_REWARD: u64 = LAMPORTS_PER_SOL / 20; // 0.05 SOL
    pub const PLAY_GAME_FEE: u64 = LAMPORTS_PER_SOL / 50; // 0.02 SOL
    pub const CHEST_REWARD: u64 = LAMPORTS_PER_SOL / 20; // 0.05 SOL

    pub fn initialize(ctx: Context<Initialize>) -> Result<()> {
        msg!("Initialized!");

        //let game = &mut ctx.accounts.new_game_data_account.load_mut()?;
        //game.clear().unwrap();
        Ok(())
    }

    pub fn spawn_player(ctx: Context<SpawnPlayer>, avatar: Pubkey) -> Result<()> {
        let game = &mut ctx.accounts.game_data_account.load_mut()?;

        match game.spawn_player(ctx.accounts.payer.to_account_info(), avatar) {
            Ok(_val) => {
                let cpi_context = CpiContext::new(
                    ctx.accounts.system_program.to_account_info().clone(),
                    anchor_lang::system_program::Transfer {
                        from: ctx.accounts.payer.to_account_info().clone(),
                        to: ctx.accounts.chest_vault.to_account_info().clone(),
                    },
                );
                anchor_lang::system_program::transfer(
                    cpi_context,
                    PLAYER_KILL_REWARD + PLAY_GAME_FEE,
                )?;
            }
            Err(err) => {
                panic!("Error: {}", err);
            }
        }
        match game.spawn_chest(ctx.accounts.payer.to_account_info()) {
            Ok(_val) => {
                let cpi_context = CpiContext::new(
                    ctx.accounts.system_program.to_account_info().clone(),
                    anchor_lang::system_program::Transfer {
                        from: ctx.accounts.payer.to_account_info().clone(),
                        to: ctx.accounts.chest_vault.to_account_info().clone(),
                    },
                );
                anchor_lang::system_program::transfer(cpi_context, CHEST_REWARD)?;
            }
            Err(err) => {
                panic!("Error: {}", err);
            }
        }
        Ok(())
    }

    pub fn move_player(ctx: Context<MovePlayer>, direction: u8) -> Result<()> {
        let game = &mut ctx.accounts.game_data_account.load_mut()?;

        match game.move_in_direction(
            direction,
            ctx.accounts.player.to_account_info(),
            ctx.accounts.chest_vault.to_account_info(),
        ) {
            Ok(_val) => {}
            Err(err) => {
                panic!("Error: {}", err);
            }
        }
        game.print().unwrap();
        Ok(())
    }

    pub fn move_player_v2(ctx: Context<MovePlayer>, direction: u8, block_bump: u8) -> Result<()> {
        let game = &mut ctx.accounts.game_data_account.load_mut()?;

        match game.move_in_direction(
            direction,
            ctx.accounts.player.to_account_info(),
            ctx.accounts.chest_vault.to_account_info(),
        ) {
            Ok(_val) => {}
            Err(err) => {
                panic!("Error: {}", err);
            }
        }
        //game.print().unwrap();
        Ok(())
    }
}
