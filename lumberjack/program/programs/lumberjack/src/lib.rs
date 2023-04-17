use anchor_lang::prelude::*;
use gpl_session::{SessionError, SessionToken, session_auth_or, Session};

declare_id!("BrnX41TKsyg7etkaDfhzoxsCHpicLDMWB8GvEm3tonNv");

#[error_code]
pub enum GameErrorCode {
    #[msg("Not enough energy")]
    NotEnoughEnergy,
    #[msg("Wrong Authority")]
    WrongAuthority,
}

const TIME_TO_REFILL_ENERGY: i64 = 60;
const MAX_ENERGY: u64 = 10;

#[program]
pub mod lumberjack {
    use super::*;

    pub fn init_player(ctx: Context<InitPlayer>) -> Result<()> {
        ctx.accounts.player.energy = MAX_ENERGY;
        ctx.accounts.player.last_login = Clock::get()?.unix_timestamp;
        ctx.accounts.player.authority = ctx.accounts.signer.key();
        Ok(())
    }

    #[session_auth_or(
        ctx.accounts.player.authority.key() == ctx.accounts.authority.key(),
        GameErrorCode::WrongAuthority
    )]
    pub fn chop_tree(mut ctx: Context<ChopTree>) -> Result<()> {
        let account = &mut ctx.accounts;
        update_energy(account)?;

        if ctx.accounts.player.energy == 0 {
            return err!(GameErrorCode::NotEnoughEnergy);
        }

        ctx.accounts.player.wood = ctx.accounts.player.wood + 1;
        ctx.accounts.player.energy = ctx.accounts.player.energy - 1;
        msg!("You chopped a tree and got 1 wood. You have {} wood and {} energy left.", ctx.accounts.player.wood, ctx.accounts.player.energy);
        Ok(())
    }

    pub fn update(mut ctx: Context<ChopTree>) -> Result<()> {
        let account = &mut ctx.accounts;
        update_energy(account)?;
        msg!("Updated energy. You have {} wood and {} energy left.", ctx.accounts.player.wood, ctx.accounts.player.energy);
        Ok(())
    }
}

pub fn update_energy(ctx: &mut ChopTree) -> Result<()> {
    let mut time_passed: i64 = &Clock::get()?.unix_timestamp - &ctx.player.last_login;
    let mut time_spent: i64 = 0;
    while time_passed > TIME_TO_REFILL_ENERGY {
        ctx.player.energy = ctx.player.energy + 1;
        time_passed -= TIME_TO_REFILL_ENERGY;
        time_spent += TIME_TO_REFILL_ENERGY;
        if ctx.player.energy == MAX_ENERGY {
            break;
        }
    }

    if ctx.player.energy >= MAX_ENERGY {
        ctx.player.last_login = Clock::get()?.unix_timestamp;
    } else {
        ctx.player.last_login += time_spent;
    }

    Ok(())
}

#[derive(Accounts)]
pub struct InitPlayer <'info> {
    #[account( 
        init,
        payer = signer,
        space = 1000,
        seeds = [b"player2".as_ref(), signer.key().as_ref()],
        bump,
    )]
    pub player: Account<'info, PlayerData>,
    #[account(mut)]
    pub signer: Signer<'info>,
    pub system_program: Program<'info, System>,
}

#[account]
pub struct PlayerData {
    pub authority: Pubkey,
    pub name: String,
    pub level: u8,
    pub xp: u64,
    pub wood: u64,
    pub energy: u64,
    pub last_login: i64
}

#[derive(Accounts, Session)]
pub struct ChopTree <'info> {
    #[session(
        // The ephemeral key pair signing the transaction
        signer = signer,
        // The authority of the user account which must have created the session
        authority = authority.key()
    )]
    // Session Tokens are passed as optional accounts
    pub session_token: Option<Account<'info, SessionToken>>,

    #[account( 
        mut,
        seeds = [b"player2".as_ref(), authority.key().as_ref()],
        bump,
    )]
    pub player: Account<'info, PlayerData>,
    #[account(mut)]
    pub signer: Signer<'info>,
    /// CHECK:
    pub authority: AccountInfo<'info>,
}