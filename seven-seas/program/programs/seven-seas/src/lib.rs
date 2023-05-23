use anchor_lang::prelude::*;
pub use crate::errors::SevenSeasError;
pub mod errors;
pub mod state;
pub use state::*;
use anchor_spl::{
    token::{Mint, Token, TokenAccount, Transfer},
};
use clockwork_sdk::state::{Thread};
use anchor_lang::prelude::Account;
use anchor_lang::solana_program::{
    instruction::Instruction, native_token::LAMPORTS_PER_SOL, system_program,
};
use anchor_lang::InstructionData;

// This is your program's public key and it will update
// automatically when you build the project.
declare_id!("2a4NcnkF5zf14JQXHAv39AsRf7jMFj13wKmTL6ZcDQNd");

#[program]
pub mod seven_seas {

    use super::*;
    pub const PLAYER_KILL_REWARD: u64 = LAMPORTS_PER_SOL / 20; // 0.05 SOL
    pub const PLAY_GAME_FEE: u64 = LAMPORTS_PER_SOL / 50; // 0.02 SOL
    pub const CHEST_REWARD: u64 = LAMPORTS_PER_SOL / 20; // 0.05 SOL

    /// Seed for thread_authority PDA.
    pub const THREAD_AUTHORITY_SEED: &[u8] = b"authority";

    pub fn initialize(_ctx: Context<Initialize>) -> Result<()> {
        msg!("Initialized!");
        Ok(())
    }

    pub fn initialize_ship(ctx: Context<InitShip>) -> Result<()> {
        msg!("Ship Initialized!");
        ctx.accounts.new_ship.health = 100;
        ctx.accounts.new_ship.start_health = 100;
        ctx.accounts.new_ship.level = 1;
        ctx.accounts.new_ship.upgrades = 1;
        ctx.accounts.new_ship.cannons = 1;
        Ok(())
    }

    pub fn upgrade_ship(ctx: Context<UpgradeShip>) -> Result<()> {        
        let transfer_instruction = Transfer {
            from: ctx.accounts.player_token_account.to_account_info(),
            to: ctx.accounts.vault_token_account.to_account_info(),
            authority: ctx.accounts.signer.to_account_info(),
        };

        let cpi_ctx = CpiContext::new(
            ctx.accounts.token_program.to_account_info(),
            transfer_instruction
        );

        let cost: u64;
        match ctx.accounts.new_ship.upgrades {
            0 => {
                ctx.accounts.new_ship.health = 100;
                ctx.accounts.new_ship.upgrades = 1;                
                cost = 5;
            },
            1 => {
                ctx.accounts.new_ship.health = 120;
                ctx.accounts.new_ship.upgrades = 2;                
                cost = 5;
            },
            2 => {
                ctx.accounts.new_ship.health = 300;
                ctx.accounts.new_ship.upgrades = 3;                
                cost = 200;
            },
            3 => {
                ctx.accounts.new_ship.health = 500;
                ctx.accounts.new_ship.upgrades = 4;                
                cost = 20000;
            }
            _ => {
                return Err(SevenSeasError::MaxShipLevelReached.into());
            }  
        }
        anchor_spl::token::transfer(cpi_ctx, cost * TOKEN_DECIMAL_MULTIPLIER)?;           

        msg!("Ship upgraded to level: {}", ctx.accounts.new_ship.upgrades);

        Ok(())
    }

    pub fn reset(_ctx: Context<Reset>) -> Result<()> {
        msg!("Reseted board!");

        _ctx.accounts.game_data_account.load_mut()?.reset().unwrap();
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

    pub fn start_thread(ctx: Context<StartThread>, thread_id: Vec<u8>) -> Result<()> {
        let system_program = &ctx.accounts.system_program;
        let clockwork_program = &ctx.accounts.clockwork_program;
        let payer = &ctx.accounts.payer;
        let thread = &ctx.accounts.thread;
        let thread_authority = &ctx.accounts.thread_authority;
        let game_data = &mut ctx.accounts.game_data_account;

        // 1️⃣ Prepare an instruction to automate.
        //    In this case, we will automate the ThreadTick instruction.
        let target_ix = Instruction {
            program_id: ID,
            accounts: crate::__client_accounts_thread_tick::ThreadTick {
                game_data: game_data.key(),
                thread: thread.key(),
                thread_authority: thread_authority.key(),
            }
            .to_account_metas(Some(true)),
            data: crate::instruction::OnThreadTick {}.data(),
        };

        // 2️⃣ Define a trigger for the thread.
        let trigger = clockwork_sdk::state::Trigger::Cron {
            schedule: format!("*/{} * * * * * *", 2).into(),
            skippable: true,
        };

        // 3️⃣ Create a Thread via CPI
        let bump = *ctx.bumps.get("thread_authority").unwrap();
        clockwork_sdk::cpi::thread_create(
            CpiContext::new_with_signer(
                clockwork_program.to_account_info(),
                clockwork_sdk::cpi::ThreadCreate {
                    payer: payer.to_account_info(),
                    system_program: system_program.to_account_info(),
                    thread: thread.to_account_info(),
                    authority: thread_authority.to_account_info(),
                },
                &[&[THREAD_AUTHORITY_SEED, &[bump]]],
            ),
            LAMPORTS_PER_SOL * 2, // amount of sol for the thread which pays the transaction fees
            thread_id,              // id
            vec![target_ix.into()], // instructions
            trigger,                // trigger
        )?;

        Ok(())
    }

    pub fn pause_thread(ctx: Context<ChangeThread>, _thread_id: Vec<u8>) -> Result<()> {
        let clockwork_program = &ctx.accounts.clockwork_program;
        let thread = &ctx.accounts.thread;
        let thread_authority = &ctx.accounts.thread_authority;

        // Pause Thread
        let bump = *ctx.bumps.get("thread_authority").unwrap();
        clockwork_sdk::cpi::thread_pause(
            CpiContext::new_with_signer(
                clockwork_program.to_account_info(),
                clockwork_sdk::cpi::ThreadPause {
                    thread: thread.to_account_info(),
                    authority: thread_authority.to_account_info(),
                },
                &[&[THREAD_AUTHORITY_SEED, &[bump]]],
            )
        )?;

        Ok(())
    }

    pub fn resume_thread(ctx: Context<ChangeThread>, _thread_id: Vec<u8>) -> Result<()> {
        let clockwork_program = &ctx.accounts.clockwork_program;
        let thread = &ctx.accounts.thread;
        let thread_authority = &ctx.accounts.thread_authority;

        // Resume Thread
        let bump = *ctx.bumps.get("thread_authority").unwrap();
        clockwork_sdk::cpi::thread_resume(
            CpiContext::new_with_signer(
                clockwork_program.to_account_info(),
                clockwork_sdk::cpi::ThreadResume {
                    thread: thread.to_account_info(),
                    authority: thread_authority.to_account_info(),
                },
                &[&[THREAD_AUTHORITY_SEED, &[bump]]],
            )
        )?;

        Ok(())
    }

    pub fn on_thread_tick(ctx: Context<ThreadTick>) -> Result<()> {
        let game = &mut ctx.accounts.game_data.load_mut()?;
        game.move_in_direction_by_thread().unwrap();
        Ok(())
    }

    pub fn spawn_player(ctx: Context<SpawnPlayer>, avatar: Pubkey) -> Result<()> {
        let game = &mut ctx.accounts.game_data_account.load_mut()?;
        let ship = &mut ctx.accounts.ship;
        
        let decimals = ctx.accounts.cannon_mint.decimals;
        ship.cannons = ctx.accounts.cannon_token_account.amount / ((u64::pow(10, decimals as u32) as u64));

        let extra_health = ctx.accounts.rum_token_account.amount / ((u64::pow(10, decimals as u32) as u64));

        msg!("Spawned player! With {} cannons", ship.cannons);

        match game.spawn_player(ctx.accounts.player.to_account_info(), avatar, ship, extra_health) {
            Ok(_val) => {
                let cpi_context = CpiContext::new(
                    ctx.accounts.system_program.to_account_info().clone(),
                    anchor_lang::system_program::Transfer {
                        from: ctx.accounts.token_account_owner.to_account_info().clone(),
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
        match game.spawn_chest(ctx.accounts.player.to_account_info()) {
            Ok(_val) => {
                let cpi_context = CpiContext::new(
                    ctx.accounts.system_program.to_account_info().clone(),
                    anchor_lang::system_program::Transfer {
                        from: ctx.accounts.token_account_owner.to_account_info().clone(),
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

    pub fn cthulhu(ctx: Context<Shoot>, _block_bump: u8) -> Result<()> {
        let game = &mut ctx.accounts.game_data_account.load_mut()?;

        match game.cthulhu(
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

    pub fn shoot(ctx: Context<Shoot>, _block_bump: u8) -> Result<()> {
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
    #[instruction(thread_id: Vec<u8>)]
    pub struct StartThread<'info> {
        #[account(mut)]
        pub game_data_account: AccountLoader<'info, GameDataAccount>,
    
        /// The Clockwork thread program.
        #[account(address = clockwork_sdk::ID)]
        pub clockwork_program: Program<'info, clockwork_sdk::ThreadProgram>,
    
        /// The signer who will pay to initialize the program.
        /// (not to be confused with the thread executions).
        #[account(mut)]
        pub payer: Signer<'info>,
    
        #[account(address = system_program::ID)]
        pub system_program: Program<'info, System>,
    
        /// Address to assign to the newly created thread.
        #[account(mut, address = Thread::pubkey(thread_authority.key(), thread_id))]
        pub thread: SystemAccount<'info>,
    
        /// The pda that will own and manage the thread.
        #[account(seeds = [THREAD_AUTHORITY_SEED], bump)]
        pub thread_authority: SystemAccount<'info>,
    }

    #[derive(Accounts)]
    #[instruction(thread_id: Vec<u8>)]
    pub struct ChangeThread<'info> {
        #[account(mut)]
        pub payer: Signer<'info>,
    
        /// The Clockwork thread program.
        #[account(address = clockwork_sdk::ID)]
        pub clockwork_program: Program<'info, clockwork_sdk::ThreadProgram>,

        /// Address to assign to the newly created thread.
        /// CHECK: is this the correct account type?
        #[account(mut, address = Thread::pubkey(thread_authority.key(), thread_id))]
        pub thread: AccountInfo<'info>,
    
        /// The pda that will own and manage the thread.
        #[account(seeds = [THREAD_AUTHORITY_SEED], bump)]
        pub thread_authority: SystemAccount<'info>,
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
        #[account( 
            mut,     
            associated_token::mint = mint_of_token_being_sent,
            associated_token::authority = signer      
        )]
        pub player_token_account: Account<'info, TokenAccount>,
        #[account(
            mut,
            seeds=[b"token_vault".as_ref(), mint_of_token_being_sent.key().as_ref()],
            token::mint=mint_of_token_being_sent,
            bump
        )]
        pub vault_token_account: Account<'info, TokenAccount>,
        pub mint_of_token_being_sent: Account<'info, Mint>,
        pub token_program: Program<'info, Token>,
    }

    #[derive(Accounts)]
    pub struct ThreadTick<'info> {
        #[account(mut)]
        pub game_data: AccountLoader<'info, GameDataAccount>,
        
        /// Verify that only this thread can execute the ThreadTick Instruction
        #[account(signer, constraint = thread.authority.eq(&thread_authority.key()))]
        pub thread: Account<'info, Thread>,

        /// The Thread Admin
        /// The authority that was used as a seed to derive the thread address
        /// `thread_authority` should equal `thread.thread_authority`
        #[account(seeds = [THREAD_AUTHORITY_SEED], bump)]
        pub thread_authority: SystemAccount<'info>,
    }

    #[account]
    pub struct Ship {
        pub health: u64,
        pub kills: u16,
        pub cannons: u64,
        pub upgrades: u16,
        pub xp: u16,
        pub level: u16,
        pub start_health: u64,
    }

}