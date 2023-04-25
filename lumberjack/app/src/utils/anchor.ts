import { Connection } from "@solana/web3.js";

export const connection = new Connection(
    "http://localhost:8899",
    "confirmed"
);

export const LUMBERJACK_PROGRAM_ID = "MkabCfyUD6rBTaYHpgKBBpBo5qzWA2pK2hrGGKMurJt";
export const TIME_TO_REFILL_ENERGY = 60;
export const MAX_ENERGY = 10;
export const ENERGY_PER_TICK = 1;
