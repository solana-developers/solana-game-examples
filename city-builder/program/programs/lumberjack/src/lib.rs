use anchor_lang::{prelude::*, solana_program::native_token::LAMPORTS_PER_SOL};
use gpl_session::{SessionError, SessionToken, session_auth_or, Session};
pub mod state;
use solana_program::pubkey;
pub use state::*;

declare_id!("HsT4yX959Qh1vis8fEqoQdgrHEJuKvaWGtHoPcTjk4mJ");
pub const TREASURY_PUBKEY: Pubkey = pubkey!("CYg2vSJdujzEC1E7kHMzB9QhjiPLRdsAa4Js7MkuXfYq");

#[error_code]
pub enum GameErrorCode {
    #[msg("Not enough energy")]
    NotEnoughEnergy,
    #[msg("Tile Already Occupied")]
    TileAlreadyOccupied,
    #[msg("Tile cant be upgraded")]
    TileCantBeUpgraded,
    #[msg("Tile has no tree")]
    TileHasNoTree,
    #[msg("Wrong Authority")]
    WrongAuthority,
    #[msg("Tile cant be collected")]
    TileCantBeCollected,
    #[msg("Production not ready yet")]
    ProductionNotReadyYet,
    #[msg("Building type not collectable")]
    BuildingTypeNotCollectable,
    #[msg("Not enough stone")]
    NotEnoughStone,
    #[msg("Not enough wood")]
    NotEnoughWood,
}

const TIME_TO_REFILL_ENERGY: i64 = 60;
const MAX_ENERGY: u64 = 10;
const BOARD_SIZE_X: usize = 10;
const BOARD_SIZE_Y: usize = 10;
pub const ENERGY_REFILL_FEE: u64 = LAMPORTS_PER_SOL / 20; // 0.05 SOL

#[program]
pub mod lumberjack {

    use super::*;

    pub fn init_player(ctx: Context<InitPlayer>) -> Result<()> {
        ctx.accounts.player.energy = MAX_ENERGY;
        ctx.accounts.player.last_login = Clock::get()?.unix_timestamp;
        ctx.accounts.player.authority = ctx.accounts.signer.key();

        Ok(())
    }

    pub fn restart_game(ctx: Context<RestartGame>) -> Result<()> {
        let board = &mut ctx.accounts.board.load_mut()?;

        if board.evil_won || board.good_won {
            let game_action = &mut ctx.accounts.game_actions.load_mut()?;
            board.Restart(game_action, ctx.accounts.signer.key())?;            
        }    
        
        Ok(())
    }

    #[session_auth_or(
        ctx.accounts.player.authority.key() == ctx.accounts.signer.key(),
        GameErrorCode::WrongAuthority
    )]
    pub fn chop_tree(mut ctx: Context<BoardAction>, x: u8, y: u8) -> Result<()> {
        let account = &mut ctx.accounts;
        update_energy(account)?;

        let board = &mut ctx.accounts.board.load_mut()?;
        let game_action = &mut ctx.accounts.game_actions.load_mut()?;

        if !board.initialized {
            board.Restart(game_action, ctx.accounts.player.key())?;
        }    
        if ctx.accounts.player.energy < 3 {
            return err!(GameErrorCode::NotEnoughEnergy);
        }

        board.chop_tree(x, y, ctx.accounts.player.key(), ctx.accounts.avatar.key(), game_action)?;

        ctx.accounts.player.energy -= 3;
        let wood = board.wood;

        msg!("You chopped a tree and got 1 wood. You have {} wood and {} energy left.",wood, ctx.accounts.player.energy);
        Ok(())
    }

    #[session_auth_or(
        ctx.accounts.player.authority.key() == ctx.accounts.signer.key(),
        GameErrorCode::WrongAuthority
    )]
    pub fn build(mut ctx: Context<BoardAction>, x :u8, y :u8, building_type :u8) -> Result<()> {
        let account = &mut ctx.accounts;
        update_energy(account)?;
        let board = &mut ctx.accounts.board.load_mut()?;

        if ctx.accounts.player.energy == 0 {
            return err!(GameErrorCode::NotEnoughEnergy);
        }

        let game_action = &mut ctx.accounts.game_actions.load_mut()?;
        board.build(x, y, building_type, ctx.accounts.player.key(), ctx.accounts.avatar.key(), game_action)?;

        ctx.accounts.player.energy -= 1;
        msg!("You built a building. You have and {} energy left.", ctx.accounts.player.energy);
        Ok(())
    }

    #[session_auth_or(
        ctx.accounts.player.authority.key() == ctx.accounts.signer.key(),
        GameErrorCode::WrongAuthority
    )]
    pub fn upgrade(mut ctx: Context<BoardAction>, x :u8, y :u8) -> Result<()> {
        let account = &mut ctx.accounts;
        update_energy(account)?;
        let board = &mut ctx.accounts.board.load_mut()?;

        if ctx.accounts.player.energy == 0 {
            return err!(GameErrorCode::NotEnoughEnergy);
        }
        let game_action = &mut ctx.accounts.game_actions.load_mut()?;
        board.upgrade(x, y, ctx.accounts.player.key(), ctx.accounts.avatar.key(), game_action)?;

        ctx.accounts.player.energy -= 1;
        let wood = board.wood;
        msg!("You chopped a tree and got 1 wood. You have {} wood and {} energy left.", wood, ctx.accounts.player.energy);
        Ok(())
    }

    #[session_auth_or(
        ctx.accounts.player.authority.key() == ctx.accounts.signer.key(),
        GameErrorCode::WrongAuthority
    )]
    pub fn collect(mut ctx: Context<BoardAction>, x :u8, y :u8) -> Result<()> {
        let account = &mut ctx.accounts;
        update_energy(account)?;
        let board = &mut ctx.accounts.board.load_mut()?;

        if ctx.accounts.player.energy == 0 {
            return err!(GameErrorCode::NotEnoughEnergy);
        }
        let game_action = &mut ctx.accounts.game_actions.load_mut()?;
        board.collect(x, y, ctx.accounts.player.key(), ctx.accounts.avatar.key(), game_action)?;

        ctx.accounts.player.energy -= 1;
        let wood = board.wood;
        msg!("You collected from building. You have {} wood and {} energy left.", wood, ctx.accounts.player.energy);
        Ok(())
    }

    pub fn update(mut ctx: Context<BoardAction>) -> Result<()> {
        let account = &mut ctx.accounts;
        update_energy(account)?;
        let board = &mut ctx.accounts.board.load_mut()?;
        let wood = board.wood;
        msg!("Updated energy. You have {} wood and {} energy left.", wood, ctx.accounts.player.energy);
        Ok(())
    }

    pub fn refill_energy(mut ctx: Context<RefillEnergyAccounts>) -> Result<()> {
        let account = &mut ctx.accounts;
        
        account.player.energy = MAX_ENERGY;

        let cpi_context = CpiContext::new(
            ctx.accounts.system_program.to_account_info().clone(),
            anchor_lang::system_program::Transfer {
                from: ctx.accounts.signer.to_account_info().clone(),
                to: ctx.accounts.treasury.to_account_info().clone(),
            },
        );
        anchor_lang::system_program::transfer(
            cpi_context,
            ENERGY_REFILL_FEE,
        )?;

        msg!("Energy refilled");
        Ok(())
    }
}

pub fn print_state(ctx: &mut BoardAction) -> Result<()> {
    let board = &mut ctx.board.load_mut()?;
    let wood = board.wood;
    msg!("Updated energy. You have {} wood and {} energy left.", wood, ctx.player.energy);
    Ok(())
}

pub fn update_energy(ctx: &mut BoardAction) -> Result<()> {
    let mut time_passed: i64 = &Clock::get()?.unix_timestamp - &ctx.player.last_login;
    let mut time_spent: i64 = 0;
    while time_passed > TIME_TO_REFILL_ENERGY {
        ctx.player.energy = ctx.player.energy + 1;
        time_passed -= TIME_TO_REFILL_ENERGY;
        time_spent += TIME_TO_REFILL_ENERGY;
        if ctx.player.energy >= MAX_ENERGY {
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
        seeds = [b"player".as_ref(), signer.key().as_ref()],
        bump,
    )]
    pub player: Account<'info, PlayerData>,
    #[account( 
        init_if_needed,
        space = 10024,
        seeds = [b"board".as_ref()],
        payer = signer,
        bump,
    )]
    pub board: AccountLoader<'info, BoardAccount>,
    #[account(
        init_if_needed,
        seeds = [b"gameActions"],
        bump,
        payer = signer,
        space = 10024
    )]
    pub game_actions: AccountLoader<'info, GameActionHistory>,    
    #[account(mut)]
    pub signer: Signer<'info>,
    pub system_program: Program<'info, System>,

}

#[derive(Accounts)]
pub struct RestartGame <'info> {
    #[account( 
        init_if_needed,
        space = 10024,
        seeds = [b"board".as_ref()],
        payer = signer,
        bump,
    )]
    pub board: AccountLoader<'info, BoardAccount>,
    #[account(
        init_if_needed,
        seeds = [b"gameActions"],
        bump,
        payer = signer,
        space = 10024
    )]
    pub game_actions: AccountLoader<'info, GameActionHistory>,    
    #[account(mut)]
    pub signer: Signer<'info>,
    pub system_program: Program<'info, System>,
}

#[account]
pub struct PlayerData {
    pub authority: Pubkey,
    pub avatar: Pubkey,
    pub name: String,
    pub level: u8,
    pub xp: u64,    
    pub energy: u64,
    pub last_login: i64
}

#[derive(Accounts, Session)]
pub struct BoardAction <'info> {
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
        seeds = [b"board".as_ref()],
        bump,
    )]
    pub board: AccountLoader<'info, BoardAccount>,
    #[account(
        mut,
        seeds = [b"gameActions"],
        bump
    )]
    pub game_actions: AccountLoader<'info, GameActionHistory>,
    /// CHECK: can be anything, its ok 
    pub avatar: AccountInfo<'info>,
    #[account( 
        mut,
        seeds = [b"player".as_ref(), player.authority.key().as_ref()],
        bump,
    )]
    pub player: Account<'info, PlayerData>,
    #[account(mut)]
    pub signer: Signer<'info>,
}

#[derive(Accounts)]
pub struct RefillEnergyAccounts <'info> {
    #[account( 
        mut,
        seeds = [b"player".as_ref(), player.authority.key().as_ref()],
        bump,
    )]
    pub player: Account<'info, PlayerData>,
    #[account(mut)]
    pub signer: Signer<'info>,
    #[account(mut, 
        address = TREASURY_PUBKEY,
    )]
    pub treasury: SystemAccount<'info>,
    pub system_program: Program<'info, System>,
}

