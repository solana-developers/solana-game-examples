import {
  useAnchorWallet,
  useConnection,
  useWallet,
} from "@solana/wallet-adapter-react";
import { FC, useCallback, useEffect, useState } from "react";
import { notify } from "../utils/notifications";
import { AnchorProvider, Program, setProvider } from "@coral-xyz/anchor";
import { IDL } from "../idl/lumberjack";
import { LUMBERJACK_PROGRAM_ID, MAX_ENERGY, TIME_TO_REFILL_ENERGY } from "utils/anchor";
import { PublicKey, SystemProgram } from "@solana/web3.js";

export const ChopTree: FC = () => {
  const { connection } = useConnection();
  const { publicKey, sendTransaction } = useWallet();
  const [gameState, setGameState] = useState<any>([]);
  const [timePassed, setTimePassed] = useState<any>([]);
  const [nextEnergyIn, setEnergyNextIn] = useState<number>(0);
  const wallet = useAnchorWallet();

  const provider = new AnchorProvider(connection, wallet, {});
  setProvider(provider);
  const program = new Program(IDL, LUMBERJACK_PROGRAM_ID, provider);

  useEffect(() => {
    console.log("gameState", JSON.stringify(gameState));
  }, [gameState]);

  useEffect(() => {
    if (!publicKey) {return;}
    const [pda] = PublicKey.findProgramAddressSync(
        [Buffer.from("player", "utf8"), 
        publicKey.toBuffer()],
        new PublicKey(LUMBERJACK_PROGRAM_ID)
      );
    
      program.account.playerData.fetch(pda).then((data) => {
        setGameState(data);
      })
      .catch((error) => {
        window.alert("No player data found, please init!");
      });
  
    connection.onAccountChange(pda, (account) => {
        setGameState(program.coder.accounts.decode("playerData", account.data));
    });

  }, [publicKey]);

  useEffect(() => {
    const interval = setInterval(async () => {
        if (gameState == null || gameState.lastLogin == undefined || gameState.energy >= 10) {return;}
        const lastLoginTime=gameState.lastLogin * 1000;
        let timePassed = ((Date.now() - lastLoginTime) / 1000);
        while (timePassed > TIME_TO_REFILL_ENERGY && gameState.energy < MAX_ENERGY) {
            gameState.energy = (parseInt(gameState.energy) + 1);
            gameState.lastLogin = parseInt(gameState.lastLogin) + TIME_TO_REFILL_ENERGY;
            timePassed -= TIME_TO_REFILL_ENERGY;
        }
        setTimePassed(timePassed);
        let nextEnergyIn = Math.floor(TIME_TO_REFILL_ENERGY -timePassed);
        if (nextEnergyIn < TIME_TO_REFILL_ENERGY && nextEnergyIn > 0) {
            setEnergyNextIn(nextEnergyIn);
        } else {
            setEnergyNextIn(0);
        }

    }, 1000);

    return () => clearInterval(interval);
  }, [gameState, timePassed]);

  const onInitClick = useCallback(async () => {
    if (!publicKey) {
      console.log("error", "Wallet not connected!");
      notify({
        type: "error",
        message: "error",
        description: "Wallet not connected!",
      });
      return;
    }

    const [pda] = PublicKey.findProgramAddressSync(
      [Buffer.from("player", "utf8"), publicKey.toBuffer()],
      new PublicKey(LUMBERJACK_PROGRAM_ID)
    );

    try {
      const transaction = program.methods
        .initPlayer()
        .accounts({
          player: pda,
          signer: publicKey,
          systemProgram: SystemProgram.programId,
        })
        .transaction();

      const tx = await transaction;
      const txSig = await sendTransaction(tx, connection, { skipPreflight: true });
      await connection.confirmTransaction(txSig, "confirmed");

      notify({ type: "success", message: "Chopped tree!", txid: txSig });
    } catch (error: any) {
      notify({
        type: "error",
        message: `Init failed!`,
        description: error?.message,
        txid: "failed",
      });
      console.log("error", `Init failed! ${error?.message}`, "failed");
    }
  }, [publicKey, connection]);

  const onChopClick = useCallback(async () => {
    if (!publicKey) {
      console.log("error", "Wallet not connected!");
      notify({
        type: "error",
        message: "error",
        description: "Wallet not connected!",
      });
      return;
    }

    try {
      const [pda] = PublicKey.findProgramAddressSync(
        [Buffer.from("player", "utf8"), publicKey.toBuffer()],
        new PublicKey(LUMBERJACK_PROGRAM_ID)
      );

      const transaction = program.methods
        .chopTree()
        .accounts({
          player: pda,
          signer: publicKey,
        })
        .transaction();

      const tx = await transaction;
      const txSig = await sendTransaction(tx, connection);
      await connection.confirmTransaction(txSig, "confirmed");

      notify({ type: "success", message: "Chopped tree!", txid: txSig });
    } catch (error: any) {
      notify({
        type: "error",
        message: `Chopping failed!`,
        description: error?.message,
        txid: "signature",
      });
      console.log("error", `Chopping failed! ${error?.message}`, "signature");
    }
  }, [publicKey, connection]);

  return (
    <div className="flex flex-row justify-center">
      <div className="relative group items-center">
        Dont forget to set your wallet to devnet!
        {(gameState && <div className="flex flex-col items-center">
            {("wood: " + gameState.wood + " Energy: " + gameState.energy + " Next energy: " + nextEnergyIn )}
        </div>)} 

        <button
          className="px-8 m-2 btn animate-pulse bg-gradient-to-br from-indigo-500 to-fuchsia-500 hover:from-white hover:to-purple-300 text-black"
          onClick={onInitClick}
        >
          <span>Init </span>
        </button>
        <button
          className="px-8 m-2 btn animate-pulse bg-gradient-to-br from-indigo-500 to-fuchsia-500 hover:from-white hover:to-purple-300 text-black"
          onClick={onChopClick}
        >
          <span>Chop tree </span>
        </button>
      </div>
    </div>
  );
};
