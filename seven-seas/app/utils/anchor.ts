import {
  Program,
  AnchorProvider,
  Idl,
  setProvider,
} from "@project-serum/anchor"
import NodeWallet from "@project-serum/anchor/dist/cjs/nodewallet"
import { IDL, SevenSeas } from "../idl/seven_seas"
import { clusterApiUrl, Connection, Keypair, PublicKey } from "@solana/web3.js"

// Create a connection to the devnet cluster
export const connection = new Connection(clusterApiUrl("devnet"), {
  commitment: "confirmed",
})

// Create a placeholder wallet to set up AnchorProvider
const wallet = new NodeWallet(Keypair.generate())

// Create an Anchor provider
const provider = new AnchorProvider(connection, wallet, {})

// Set the provider as the default provider
setProvider(provider)

// Seven Seas program ID
const programId = new PublicKey("2a4NcnkF5zf14JQXHAv39AsRf7jMFj13wKmTL6ZcDQNd")
export const goldTokenMint = new PublicKey("goLdQwNaZToyavwkbuPJzTt5XPNR3H7WQBGenWtzPH3");
export const cannonTokenMint = new PublicKey("boomkN8rQpbgGAKcWvR3yyVVkjucNYcq7gTav78NQAG");
export const rumTokenMint = new PublicKey("rumwqxXmjKAmSdkfkc5qDpHTpETYJRyXY22DWYUmWDt");
export const threadId = "thread-wind";


export const program = new Program(
  IDL as Idl,
  programId
) as unknown as Program<SevenSeas>

// GameDataAccount PDA
export const [globalLevel1GameDataAccount] = PublicKey.findProgramAddressSync(
  [Buffer.from("level", "utf8")],
  programId
)
