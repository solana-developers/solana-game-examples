import * as anchor from "@coral-xyz/anchor"
import { Program } from "@coral-xyz/anchor"
import { BattleCoins } from "../target/types/battle_coins"
import * as spl from "@solana/spl-token"
import { assert } from "chai"
import { Metaplex } from "@metaplex-foundation/js"
import { PROGRAM_ID as TOKEN_METADATA_PROGRAM_ID } from "@metaplex-foundation/mpl-token-metadata"

describe("battle-coins", () => {
  // Configure the client to use the local cluster.
  anchor.setProvider(anchor.AnchorProvider.env())

  const program = anchor.workspace.BattleCoins as Program<BattleCoins>
  const wallet = anchor.workspace.BattleCoins.provider.wallet
  const connection = program.provider.connection
  const metaplex = Metaplex.make(connection)

  // PDA for the reward token mint
  const [rewardTokenMintPDA] = anchor.web3.PublicKey.findProgramAddressSync(
    [Buffer.from("reward")],
    program.programId
  )

  // PDA for the player data account
  const [playerPDA] = anchor.web3.PublicKey.findProgramAddressSync(
    [Buffer.from("player"), wallet.publicKey.toBuffer()],
    program.programId
  )

  // Player associated token account address
  const playerTokenAccount = spl.getAssociatedTokenAddressSync(
    rewardTokenMintPDA,
    wallet.publicKey
  )

  // token metadata
  const metadata = {
    uri: "https://raw.githubusercontent.com/solana-developers/program-examples/new-examples/tokens/tokens/.assets/spl-token.json",
    name: "Solana Gold",
    symbol: "GOLDSOL",
  }

  it("Initialize New Token Mint", async () => {
    // PDA for the token metadata account for the reward token mint
    const rewardTokenMintMetadataPDA = await metaplex
      .nfts()
      .pdas()
      .metadata({ mint: rewardTokenMintPDA })

    const tx = await program.methods
      .createMint(metadata.uri, metadata.name, metadata.symbol)
      .accounts({
        rewardTokenMint: rewardTokenMintPDA,
        metadataAccount: rewardTokenMintMetadataPDA,
        tokenMetadataProgram: TOKEN_METADATA_PROGRAM_ID,
      })
      .rpc()
    console.log("Your transaction signature", tx)
  })

  it("Init Player", async () => {
    const tx = await program.methods
      .initPlayer()
      .accounts({
        playerData: playerPDA,
        player: wallet.publicKey,
      })
      .rpc()
    console.log("Your transaction signature", tx)

    // Check that the player data account was initialized correctly
    const playerData = await program.account.playerData.fetch(playerPDA)
    assert(playerData.health === 100)
  })

  it("Kill Enemy to Mint 1 Token", async () => {
    const tx = await program.methods
      .killEnemy()
      .accounts({
        playerData: playerPDA,
        playerTokenAccount: playerTokenAccount,
        rewardTokenMint: rewardTokenMintPDA,
      })
      .rpc()
    console.log("Your transaction signature", tx)

    // Check that 1 token was minted to the player's token account
    assert.strictEqual(
      Number(
        (await connection.getTokenAccountBalance(playerTokenAccount)).value
          .amount
      ),
      1_000_000_000
    )

    // Fetch and log the player's health, reduced by random damage
    // This may log as same number when testing locally due to the same slot getting used in the xorshift function
    const playerData = await program.account.playerData.fetch(playerPDA)
    console.log("Player Health: ", playerData.health)
  })

  it("Burn 1 Token to Heal", async () => {
    const tx = await program.methods
      .heal()
      .accounts({
        playerData: playerPDA,
        playerTokenAccount: playerTokenAccount,
        rewardTokenMint: rewardTokenMintPDA,
      })
      .rpc()
    console.log("Your transaction signature", tx)

    // Check that 1 token was burned from the player's token account
    assert.strictEqual(
      Number(
        (await connection.getTokenAccountBalance(playerTokenAccount)).value
          .amount
      ),
      0
    )

    // Check that the player's health was restored to 100
    const playerData = await program.account.playerData.fetch(playerPDA)
    assert(playerData.health === 100)
  })
})
