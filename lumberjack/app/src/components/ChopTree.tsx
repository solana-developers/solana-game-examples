import {
  useAnchorWallet,
  useConnection,
  useWallet,
} from "@solana/wallet-adapter-react"
import { FC, useCallback, useEffect, useState } from "react"
import { notify } from "../utils/notifications"
import { AnchorProvider, Program, setProvider } from "@coral-xyz/anchor"
import { IDL } from "../idl/lumberjack"
import {
  ENERGY_PER_TICK,
  LUMBERJACK_PROGRAM_ID,
  MAX_ENERGY,
  TIME_TO_REFILL_ENERGY,
} from "utils/anchor"
import { PublicKey, SystemProgram } from "@solana/web3.js"
import { useSessionWallet } from "@gumhq/react-sdk"

type GameDataAccount = {
  name: String
  level: number
  xp: number
  wood: number
  energy: number
  lastLogin: number
}

export const ChopTree: FC = () => {
  const { connection } = useConnection()
  const { publicKey, sendTransaction } = useWallet()
  const [gameState, setGameState] = useState<GameDataAccount | null>(null)
  const [timePassed, setTimePassed] = useState<any>([])
  const [nextEnergyIn, setEnergyNextIn] = useState<number>(0)
  const wallet = useAnchorWallet()

  const sessionWallet = useSessionWallet()

  const provider = new AnchorProvider(connection, wallet, {})
  setProvider(provider)
  const program = new Program(IDL, LUMBERJACK_PROGRAM_ID, provider)

  useEffect(() => {
    console.log("gameState", JSON.stringify(gameState))
  }, [gameState])

  useEffect(() => {
    if (!publicKey) {
      return
    }
    const [pda] = PublicKey.findProgramAddressSync(
      [Buffer.from("player2", "utf8"), publicKey.toBuffer()],
      new PublicKey(LUMBERJACK_PROGRAM_ID)
    )

    program.account.playerData
      .fetch(pda)
      .then((data) => {
        setGameState(data)
      })
      .catch((error) => {
        window.alert("No player data found, please init!")
      })

    connection.onAccountChange(pda, (account) => {
      setGameState(program.coder.accounts.decode("playerData", account.data))
    })
  }, [publicKey])

  useEffect(() => {
    const interval = setInterval(async () => {
      if (
        gameState == null ||
        gameState.lastLogin == undefined ||
        gameState.energy >= 10
      ) {
        return
      }
      const lastLoginTime = gameState.lastLogin * 1000
      let timePassed = (Date.now() - lastLoginTime) / 1000
      while (
        timePassed > TIME_TO_REFILL_ENERGY &&
        gameState.energy < MAX_ENERGY
      ) {
        gameState.energy = +gameState.energy + 1
        gameState.lastLogin = gameState.lastLogin + TIME_TO_REFILL_ENERGY
        timePassed -= TIME_TO_REFILL_ENERGY
      }
      setTimePassed(timePassed)
      let nextEnergyIn = Math.floor(TIME_TO_REFILL_ENERGY - timePassed)
      if (nextEnergyIn < TIME_TO_REFILL_ENERGY && nextEnergyIn > 0) {
        setEnergyNextIn(nextEnergyIn)
      } else {
        setEnergyNextIn(0)
      }
    }, 1000)

    return () => clearInterval(interval)
  }, [gameState, timePassed])

  const onInitClick = useCallback(async () => {
    if (!publicKey) {
      console.log("error", "Wallet not connected!")
      notify({
        type: "error",
        message: "error",
        description: "Wallet not connected!",
      })
      return
    }

    const [pda] = PublicKey.findProgramAddressSync(
      [Buffer.from("player2", "utf8"), publicKey.toBuffer()],
      new PublicKey(LUMBERJACK_PROGRAM_ID)
    )

    try {
      const transaction = program.methods
        .initPlayer()
        .accounts({
          player: pda,
          signer: publicKey,
          systemProgram: SystemProgram.programId,
        })
        .transaction()

      const tx = await transaction
      const txSig = await sendTransaction(tx, connection, {
        skipPreflight: true,
      })
      await connection.confirmTransaction(txSig, "confirmed")

      notify({ type: "success", message: "Chopped tree!", txid: txSig })
    } catch (error: any) {
      notify({
        type: "error",
        message: `Init failed!`,
        description: error?.message,
        txid: "failed",
      })
      console.log("error", `Init failed! ${error?.message}`, "failed")
    }
  }, [publicKey, connection])

  const onChopClick = useCallback(async () => {
    if (!publicKey) {
      console.log("error", "Wallet not connected!")
      notify({
        type: "error",
        message: "error",
        description: "Wallet not connected!",
      })
      return
    }

    try {
      const [pda] = PublicKey.findProgramAddressSync(
        [Buffer.from("player2", "utf8"), publicKey.toBuffer()],
        new PublicKey(LUMBERJACK_PROGRAM_ID)
      )

      const transaction = program.methods
        .chopTree()
        .accounts({
          player: pda,
          signer: sessionWallet.publicKey,
          authority: publicKey,
          sessionToken: sessionWallet.sessionToken,
        })
        .transaction()

      console.log("sessionWallet.publicKey: " + sessionWallet.publicKey)
      console.log("sessionToken: " + sessionWallet.sessionToken)
      console.log("publicKey: " + publicKey)
      console.log("pda: " + pda)

      const tx = await transaction
      const txids = await sessionWallet.signAndSendTransaction(tx)

      if (txids && txids.length > 0) {
        console.log("Transaction sent:", txids)
        notify({ type: "success", message: "Chopped tree success!" })
      } else {
        console.error("Failed to send transaction")
        notify({ type: "failed", message: "Chopped tree failed!" })
      }
    } catch (error: any) {
      notify({
        type: "error",
        message: `Chopping failed!`,
        description: error?.message,
        txid: "signature",
      })
      console.log("error", `Chopping failed! ${error?.message}`, "signature")
    }
  }, [publicKey, connection, sessionWallet])

  const handleCreateSession = async () => {
    const targetProgramPublicKey = program.programId
    console.log("targetProgramPublicKey", program.programId.toBase58())
    const topUp = true
    const expiryInMinutes = 600

    const session = await sessionWallet.createSession(
      targetProgramPublicKey,
      topUp,
      expiryInMinutes
    )

    if (session) {
      console.log("Session created:", session)
    } else {
      console.error("Failed to create session")
    }
  }

  const handleRevokeSession = async () => {
    await sessionWallet.revokeSession()
    console.log("Session revoked")
  }

  return (
    <div className="flex flex-row justify-center">
      <div>
        {!publicKey && "Connect to dev net wallet!"}

        <button
          className="px-8 m-2 btn animate-pulse bg-gradient-to-br from-indigo-500 to-fuchsia-500 hover:from-white hover:to-purple-300 text-black"
          onClick={onInitClick}
        >
          <span>Init </span>
        </button>

        {gameState && publicKey && (
          <div className="relative group items-center">
            <div className="flex flex-col items-center">
              {"Wood: " +
                gameState.wood +
                " Energy: " +
                gameState.energy +
                " Next energy: " +
                nextEnergyIn}
            </div>

            <button
              className="px-8 m-2 btn animate-pulse bg-gradient-to-br from-indigo-500 to-fuchsia-500 hover:from-white hover:to-purple-300 text-black"
              onClick={onChopClick}
            >
              <span>Chop tree </span>
            </button>
            <button
              className="px-8 m-2 btn animate-pulse bg-gradient-to-br from-indigo-500 to-fuchsia-500 hover:from-white hover:to-purple-300 text-black"
              onClick={handleCreateSession}
            >
              <span>Create session </span>
            </button>
            <button
              className="px-8 m-2 btn animate-pulse bg-gradient-to-br from-indigo-500 to-fuchsia-500 hover:from-white hover:to-purple-300 text-black"
              onClick={handleRevokeSession}
            >
              <span>Revoke Session </span>
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
