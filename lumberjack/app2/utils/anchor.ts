import { Program, IdlAccounts, BN } from "@coral-xyz/anchor"
import { Lumberjack, IDL } from "../idl/lumberjack"
import { clusterApiUrl, Connection, PublicKey } from "@solana/web3.js"

// Create a connection to the devnet cluster
const connection = new Connection(clusterApiUrl("devnet"), {
  commitment: "confirmed",
})

// Lumberjack game program ID
const programId = new PublicKey("MkabCfyUD6rBTaYHpgKBBpBo5qzWA2pK2hrGGKMurJt")

// Create the program interface using the idl, program ID, and provider
export const program = new Program<Lumberjack>(IDL, programId, {
  connection,
})

// Player Data Account Type from Idl
export type PlayerData = IdlAccounts<Lumberjack>["playerData"]

// Constants for the game
export const TIME_TO_REFILL_ENERGY: BN = new BN(60)
export const MAX_ENERGY = 10
export const ENERGY_PER_TICK: BN = new BN(1)
