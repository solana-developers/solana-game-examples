import { createContext, useContext, useEffect, useState } from "react"
import { PublicKey } from "@solana/web3.js"
import { useConnection, useWallet } from "@solana/wallet-adapter-react"
import {
  program,
  PlayerData,
  MAX_ENERGY,
  TIME_TO_REFILL_ENERGY,
} from "@/utils/anchor"
import { BN } from "@coral-xyz/anchor"

const GameStateContext = createContext<{
  playerDataPDA: PublicKey | null
  gameState: PlayerData | null
  nextEnergyIn: number
}>({
  playerDataPDA: null,
  gameState: null,
  nextEnergyIn: 0,
})

export const useGameState = () => useContext(GameStateContext)

export const GameStateProvider = ({
  children,
}: {
  children: React.ReactNode
}) => {
  const { publicKey } = useWallet()
  const { connection } = useConnection()

  const [playerDataPDA, setPlayerData] = useState<PublicKey | null>(null)
  const [gameState, setGameState] = useState<PlayerData | null>(null)
  const [timePassed, setTimePassed] = useState<any>([])
  const [nextEnergyIn, setEnergyNextIn] = useState<number>(0)

  useEffect(() => {
    setGameState(null)
    if (!publicKey) {
      return
    }
    const [pda] = PublicKey.findProgramAddressSync(
      [Buffer.from("player", "utf8"), publicKey.toBuffer()],
      program.programId
    )
    setPlayerData(pda)

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
        gameState.energy.toNumber() >= MAX_ENERGY
      ) {
        return
      }
      const lastLoginTime = gameState.lastLogin.toNumber() * 1000
      let timePassed = (Date.now() - lastLoginTime) / 1000
      while (
        timePassed >= TIME_TO_REFILL_ENERGY.toNumber() &&
        gameState.energy.toNumber() < MAX_ENERGY
      ) {
        gameState.energy = gameState.energy.add(new BN(1))
        gameState.lastLogin = gameState.lastLogin.add(TIME_TO_REFILL_ENERGY)
        timePassed -= TIME_TO_REFILL_ENERGY.toNumber()
      }
      setTimePassed(timePassed)
      let nextEnergyIn = Math.floor(
        TIME_TO_REFILL_ENERGY.toNumber() - timePassed
      )
      if (
        nextEnergyIn < TIME_TO_REFILL_ENERGY.toNumber() &&
        nextEnergyIn >= 0
      ) {
        setEnergyNextIn(nextEnergyIn)
      } else {
        setEnergyNextIn(0)
      }
    }, 1000)

    return () => clearInterval(interval)
  }, [gameState, timePassed, nextEnergyIn])

  return (
    <GameStateContext.Provider
      value={{
        playerDataPDA,
        gameState,
        nextEnergyIn,
      }}
    >
      {children}
    </GameStateContext.Provider>
  )
}
