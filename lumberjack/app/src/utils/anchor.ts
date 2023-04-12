import { Connection } from "@solana/web3.js";

export const connection = new Connection(
    "https://api.devnet.solana.com",
    "confirmed"
);

export const LUMBERJACK_PROGRAM_ID = "2udB5teZ2HJK5sUrVoD8xMjzqkf48tpTigaF66mTnnzK";
export const TIME_TO_REFILL_ENERGY = 60;
export const MAX_ENERGY = 10;
