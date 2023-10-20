use crate::constants::MAX_ENERGY;
pub use crate::errors::GameErrorCode;
use anchor_lang::prelude::*;

use super::chop_tree::PlayerData;

pub fn init_player(ctx: Context<InitPlayer>) -> Result<()> {
    ctx.accounts.player.energy = MAX_ENERGY;
    ctx.accounts.player.last_login = Clock::get()?.unix_timestamp;
    ctx.accounts.player.authority = ctx.accounts.signer.key();
    Ok(())
}

#[derive(Accounts)]
pub struct InitPlayer<'info> {
    #[account(
        init,
        payer = signer,
        space = 1000,
        seeds = [b"player".as_ref(), signer.key().as_ref()],
        bump,
    )]
    pub player: Account<'info, PlayerData>,
    #[account(mut)]
    pub signer: Signer<'info>,
    pub system_program: Program<'info, System>,
}

#[account]
pub struct GameData {
    pub total_wood_collected: u64,
}
