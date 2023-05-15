use anchor_lang::prelude::*;
use anchor_lang::solana_program::native_token::LAMPORTS_PER_SOL;
pub use crate::errors::TinyAdventureError;
pub mod errors;
pub mod state;
pub use state::*;
use anchor_spl::{
    associated_token::AssociatedToken,
    token::{burn, mint_to, Burn, Mint, MintTo, Token, TokenAccount},
};

// This is your program's public key and it will update
// automatically when you build the project.
declare_id!("BZdtLcjPeNCC65Y71Qo5Xhbtf1udCR6fBiPyk91x554M");

#[program]
pub mod seven_seas {

    use super::*;
    pub const PLAYER_KILL_REWARD: u64 = LAMPORTS_PER_SOL / 20; // 0.05 SOL
    pub const PLAY_GAME_FEE: u64 = LAMPORTS_PER_SOL / 50; // 0.02 SOL
    pub const CHEST_REWARD: u64 = LAMPORTS_PER_SOL / 20; // 0.05 SOL

    pub fn initialize(_ctx: Context<Initialize>) -> Result<()> {
        msg!("Initialized!");
        Ok(())
    }

    pub fn initialize_ship(ctx: Context<InitShip>) -> Result<()> {
        msg!("Ship Initialized!");
        ctx.accounts.new_ship.health = 3;
        ctx.accounts.new_ship.level = 1;
        ctx.accounts.new_ship.cannons = 1;
        Ok(())
    }

    pub fn upgrade_ship(ctx: Context<UpgradeShip>) -> Result<()> {
        // TODO: add costs in gold token
        match ctx.accounts.new_ship.level {
            1 => {
                ctx.accounts.new_ship.health = 7;
                ctx.accounts.new_ship.level = 2;                
            },
            2 => {
                ctx.accounts.new_ship.health = 12;
                ctx.accounts.new_ship.level = 3;                
            },
            3 => {
                ctx.accounts.new_ship.health = 20;
                ctx.accounts.new_ship.level = 4;                
            }
            _ => {
                panic!("Max level reached");
            }  
        }
        msg!("Ship upgraded to level: {}", ctx.accounts.new_ship.level);

        Ok(())
    }

    pub fn reset(_ctx: Context<Initialize>) -> Result<()> {
        msg!("Initialized!");

        /*let value: u64 = _ctx.accounts.chest_vault.to_account_info().lamports();
        **_ctx.accounts.chest_vault.to_account_info().try_borrow_mut_lamports()? -= value;
        **_ctx.accounts.signer.try_borrow_mut_lamports()? += value;

        let value = _ctx.accounts.game_actions.to_account_info().lamports();
        **_ctx.accounts.game_actions.to_account_info().try_borrow_mut_lamports()? -= value;
        **_ctx.accounts.signer.try_borrow_mut_lamports()? += value;

        let value = _ctx.accounts.new_game_data_account.to_account_info().lamports();
        **_ctx.accounts.new_game_data_account.to_account_info().try_borrow_mut_lamports()? -= value;
        **_ctx.accounts.signer.try_borrow_mut_lamports()? += value;*/

        Ok(())
    }

    pub fn spawn_player(ctx: Context<SpawnPlayer>, avatar: Pubkey) -> Result<()> {
        let game = &mut ctx.accounts.game_data_account.load_mut()?;
        let ship = &mut ctx.accounts.ship;
        
        match game.spawn_player(ctx.accounts.payer.to_account_info(), avatar, ship) {
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

    pub fn shoot(ctx: Context<MovePlayer>, _block_bump: u8) -> Result<()> {
        let game = &mut ctx.accounts.game_data_account.load_mut()?;

        match game.shoot(
            ctx.accounts.player.to_account_info(),
            &mut ctx.accounts.game_actions,
            ctx.accounts.chest_vault.to_account_info(),
            ctx.accounts.vault_token_account.to_account_info(),
            ctx.accounts.player_token_account.to_account_info(),
            ctx.accounts.token_account_owner_pda.to_account_info(),
            ctx.accounts.token_program.to_account_info(),
            ctx.bumps["token_account_owner_pda"],
        ) {
            Ok(_val) => {}
            Err(err) => {
                panic!("Error: {}", err);
            }
        }
        game.print().unwrap();
        Ok(())
    }

    pub fn move_player_v2(ctx: Context<MovePlayer>, direction: u8, _block_bump: u8) -> Result<()> {
        let game = &mut ctx.accounts.game_data_account.load_mut()?;

        match game.move_in_direction(
            direction,
            ctx.accounts.player.to_account_info(),
            ctx.accounts.chest_vault.to_account_info(),
            ctx.accounts.vault_token_account.to_account_info(),
            ctx.accounts.player_token_account.to_account_info(),
            ctx.accounts.token_account_owner_pda.to_account_info(),
            ctx.accounts.token_program.to_account_info(),
            ctx.bumps["token_account_owner_pda"],
            &mut ctx.accounts.game_actions,
        ) {
            Ok(_val) => {}
            Err(err) => {
                panic!("Error: {}", err);
            }
        }
        game.print().unwrap();
        Ok(())
    }

    #[derive(Accounts)]
    pub struct InitShip<'info> {
        #[account(mut)]
        pub signer: Signer<'info>,
        #[account(
            init,
            payer = signer, 
            seeds = [b"ship", nft_account.key().as_ref()],
            bump,
            space = 1024
        )]
        pub new_ship: Account<'info, Ship>,
        /// CHECK:
        pub nft_account: AccountInfo<'info>,
        pub system_program: Program<'info, System>,
    }

    #[derive(Accounts)]
    pub struct UpgradeShip<'info> {
        #[account(mut)]
        pub signer: Signer<'info>,
        #[account(
            seeds = [b"ship", nft_account.key().as_ref()],
            bump
        )]
        #[account(mut)]
        pub new_ship: Account<'info, Ship>,
        /// CHECK:
        #[account(mut)]
        pub nft_account: AccountInfo<'info>,
        pub system_program: Program<'info, System>,
    }

    #[account]
    pub struct Ship {
        pub health: u16,
        pub kills: u16,
        pub cannons: u16,
        pub upgrades: u16,
        pub xp: u16,
        pub level: u16
    }
}
