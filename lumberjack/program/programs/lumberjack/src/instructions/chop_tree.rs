pub use crate::errors::GameErrorCode;
pub use crate::state::game_data::GameData;
use crate::state::player_data::PlayerData;
use anchor_lang::prelude::*;
use session_keys::{Session, SessionToken};

pub fn chop_tree(mut ctx: Context<ChopTree>) -> Result<()> {
    let account: &mut &mut ChopTree<'_> = &mut ctx.accounts;
    account.player.update_energy()?;
    account.player.print()?;

    if account.player.energy == 0 {
        return err!(GameErrorCode::NotEnoughEnergy);
    }

    account.player.chop_tree(1)?;
    account.game_data.on_tree_chopped(1)?;

    msg!(
        "You chopped a tree and got 1 wood. You have {} wood and {} energy left.",
        ctx.accounts.player.wood,
        ctx.accounts.player.energy
    );
    Ok(())
}

#[derive(Accounts, Session)]
pub struct ChopTree<'info> {
    #[session(
        // The ephemeral key pair signing the transaction
        signer = signer,
        // The authority of the user account which must have created the session
        authority = player.authority.key()
    )]
    // Session Tokens are passed as optional accounts
    pub session_token: Option<Account<'info, SessionToken>>,

    #[account(
        mut,
        seeds = [b"player".as_ref(), player.authority.key().as_ref()],
        bump,
    )]
    pub player: Account<'info, PlayerData>,

    #[account(
        mut,
        seeds = [b"gameData".as_ref()],
        bump,
    )]
    pub game_data: Account<'info, GameData>,

    #[account(mut)]
    pub signer: Signer<'info>,
}
