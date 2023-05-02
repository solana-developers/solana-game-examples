use anchor_lang::prelude::*;
use anchor_spl::{
    associated_token::AssociatedToken,
    metadata::{create_metadata_accounts_v3, CreateMetadataAccountsV3, Metadata},
    token::{burn, mint_to, Burn, Mint, MintTo, Token, TokenAccount},
};
use mpl_token_metadata::{pda::find_metadata_account, state::DataV2};
use solana_program::{pubkey, pubkey::Pubkey};

declare_id!("Fg6PaFpoGXkYsidMpWTK6W2BeZ7FEfcYkg476zPFsLnS");

// TODO: run "solana address" cli command and update ADMIN_PUBKEY with your own pubkey
const ADMIN_PUBKEY: Pubkey = pubkey!("...");
const MAX_HEALTH: u8 = 100;

#[program]
pub mod battle_coins {
    use super::*;

    // Create new token mint with metadata using PDA as mint authority
    pub fn create_mint(
        ctx: Context<CreateMint>,
        uri: String,
        name: String,
        symbol: String,
    ) -> Result<()> {
        // PDA seeds and bump to "sign" for CPI
        let seeds = b"reward";
        let bump = *ctx.bumps.get("reward_token_mint").unwrap();
        let signer: &[&[&[u8]]] = &[&[seeds, &[bump]]];

        // On-chain token metadata for the mint
        let data_v2 = DataV2 {
            name: name,
            symbol: symbol,
            uri: uri,
            seller_fee_basis_points: 0,
            creators: None,
            collection: None,
            uses: None,
        };

        // CPI Context
        let cpi_ctx = CpiContext::new_with_signer(
            ctx.accounts.token_metadata_program.to_account_info(),
            CreateMetadataAccountsV3 {
                metadata: ctx.accounts.metadata_account.to_account_info(), // the metadata account being created
                mint: ctx.accounts.reward_token_mint.to_account_info(), // the mint account of the metadata account
                mint_authority: ctx.accounts.reward_token_mint.to_account_info(), // the mint authority of the mint account
                update_authority: ctx.accounts.reward_token_mint.to_account_info(), // the update authority of the metadata account
                payer: ctx.accounts.admin.to_account_info(), // the payer for creating the metadata account
                system_program: ctx.accounts.system_program.to_account_info(), // the system program account, required when creating new accounts
                rent: ctx.accounts.rent.to_account_info(), // the rent sysvar account
            },
            signer, // pda signer
        );

        create_metadata_accounts_v3(
            cpi_ctx, // cpi context
            data_v2, // token metadata
            true,    // is_mutable
            true,    // update_authority_is_signer
            None,    // collection details
        )?;

        Ok(())
    }

    // Create new player account
    pub fn init_player(ctx: Context<InitPlayer>) -> Result<()> {
        // Set initial player health
        ctx.accounts.player_data.health = MAX_HEALTH;
        Ok(())
    }

    // Calculate random damage and mint 1 token to player token account
    pub fn kill_enemy(ctx: Context<KillEnemy>) -> Result<()> {
        // Check if player has enough health
        if ctx.accounts.player_data.health == 0 {
            return err!(ErrorCode::NotEnoughHealth);
        }

        // Calculate random damage using slot as seed for xorshift RNG
        let slot = Clock::get()?.slot;
        let xorshift_output = xorshift64(slot);
        let random_damage = xorshift_output % (MAX_HEALTH + 1) as u64;
        msg!("Random damage: {}", random_damage);

        // Subtract health from player, min health is 0
        ctx.accounts.player_data.health = ctx
            .accounts
            .player_data
            .health
            .saturating_sub(random_damage as u8);
        msg!("Player Health: {}", ctx.accounts.player_data.health);

        // PDA seeds and bump to "sign" for CPI
        let seeds = b"reward";
        let bump = *ctx.bumps.get("reward_token_mint").unwrap();
        let signer: &[&[&[u8]]] = &[&[seeds, &[bump]]];

        // CPI Context
        let cpi_ctx = CpiContext::new_with_signer(
            ctx.accounts.token_program.to_account_info(),
            MintTo {
                mint: ctx.accounts.reward_token_mint.to_account_info(), // mint account of token to mint
                to: ctx.accounts.player_token_account.to_account_info(), // player token account to mint to
                authority: ctx.accounts.reward_token_mint.to_account_info(), // pda is used as both address of mint and mint authority
            },
            signer, // pda signer
        );

        // Mint 1 token, accounting for decimals of mint
        let amount = (1u64)
            .checked_mul(10u64.pow(ctx.accounts.reward_token_mint.decimals as u32))
            .unwrap();

        mint_to(cpi_ctx, amount)?;
        Ok(())
    }

    // Burn Token to health player
    pub fn heal(ctx: Context<Heal>) -> Result<()> {
        // Set player health to max
        ctx.accounts.player_data.health = MAX_HEALTH;

        // CPI Context
        let cpi_ctx = CpiContext::new(
            ctx.accounts.token_program.to_account_info(),
            Burn {
                mint: ctx.accounts.reward_token_mint.to_account_info(), // mint account of token to burn
                from: ctx.accounts.player_token_account.to_account_info(), // player token account to burn from
                authority: ctx.accounts.player.to_account_info(), // player account is authority for player token account, required as signer
            },
        );

        // Burn 1 token, accounting for decimals of mint
        let amount = (1u64)
            .checked_mul(10u64.pow(ctx.accounts.reward_token_mint.decimals as u32))
            .unwrap();

        burn(cpi_ctx, amount)?;
        Ok(())
    }
}

#[derive(Accounts)]
pub struct CreateMint<'info> {
    // Use ADMIN_PUBKEY as constraint, only the specified admin can invoke this instruction
    #[account(
        mut,
        address = ADMIN_PUBKEY
    )]
    pub admin: Signer<'info>,

    // The PDA is both the address of the mint account and the mint authority
    #[account(
        init,
        seeds = [b"reward"],
        bump,
        payer = admin,
        mint::decimals = 9,
        mint::authority = reward_token_mint,

    )]
    pub reward_token_mint: Account<'info, Mint>,

    ///CHECK: Using "address" constraint to validate metadata account address, this account is created via CPI in the instruction
    #[account(
        mut,
        address=find_metadata_account(&reward_token_mint.key()).0
    )]
    pub metadata_account: UncheckedAccount<'info>,

    pub token_program: Program<'info, Token>,
    pub token_metadata_program: Program<'info, Metadata>,
    pub system_program: Program<'info, System>,
    pub rent: Sysvar<'info, Rent>,
}

#[derive(Accounts)]
pub struct InitPlayer<'info> {
    #[account(mut)]
    pub player: Signer<'info>,

    // Initialize player data account, using player.key() as a seed allows each player to create their own account
    #[account(
        init,
        payer = player,
        space = 8 + 8,
        seeds = [b"player", player.key().as_ref()],
        bump,
    )]
    pub player_data: Account<'info, PlayerData>,

    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct KillEnemy<'info> {
    #[account(mut)]
    pub player: Signer<'info>,

    #[account(
        mut,
        seeds = [b"player", player.key().as_ref()],
        bump,
    )]
    pub player_data: Account<'info, PlayerData>,

    // Initialize player token account if it doesn't exist
    #[account(
        init_if_needed,
        payer = player,
        associated_token::mint = reward_token_mint,
        associated_token::authority = player
    )]
    pub player_token_account: Account<'info, TokenAccount>,

    #[account(
        mut,
        seeds = [b"reward"],
        bump,
    )]
    pub reward_token_mint: Account<'info, Mint>,

    pub token_program: Program<'info, Token>,
    pub associated_token_program: Program<'info, AssociatedToken>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct Heal<'info> {
    #[account(mut)]
    pub player: Signer<'info>,

    #[account(
        mut,
        seeds = [b"player", player.key().as_ref()],
        bump,
    )]
    pub player_data: Account<'info, PlayerData>,

    #[account(
        mut,
        associated_token::mint = reward_token_mint,
        associated_token::authority = player
    )]
    pub player_token_account: Account<'info, TokenAccount>,

    #[account(
        mut,
        seeds = [b"reward"],
        bump,
    )]
    pub reward_token_mint: Account<'info, Mint>,

    pub token_program: Program<'info, Token>,
    pub associated_token_program: Program<'info, AssociatedToken>,
}

#[account]
pub struct PlayerData {
    pub health: u8,
}

#[error_code]
pub enum ErrorCode {
    #[msg("Not enough health")]
    NotEnoughHealth,
}

// Use xorshift64 to generate a random number
// Note that this should not be used for cases where security is important
pub fn xorshift64(seed: u64) -> u64 {
    let mut x = seed;
    x ^= x << 13;
    x ^= x >> 7;
    x ^= x << 17;
    x
}
