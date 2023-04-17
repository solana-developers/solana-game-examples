import { Connection } from "@solana/web3.js";

export const connection = new Connection(
    "https://api.devnet.solana.com",
    "confirmed"
);

export const LUMBERJACK_PROGRAM_ID = "BrnX41TKsyg7etkaDfhzoxsCHpicLDMWB8GvEm3tonNv";
export const TIME_TO_REFILL_ENERGY = 60;
export const MAX_ENERGY = 10;
export const ENERGY_PER_TICK = 1;
