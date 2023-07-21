import Image from "next/image"
import { HStack, VStack, Text } from "@chakra-ui/react"
import { useWallet } from "@solana/wallet-adapter-react"
import { useGameState } from "@/contexts/GameStateProvider"

const DisplayPlayerData = () => {
  const { publicKey } = useWallet()
  const { gameState, nextEnergyIn } = useGameState()

  return (
    <>
      {gameState && publicKey && (
        <HStack justifyContent="center" spacing={4}>
          <HStack>
            <Image src="/Wood.png" alt="Wood Icon" width={64} height={64} />
            <Text>Wood: {Number(gameState.wood)}</Text>
          </HStack>
          <HStack>
            <Image src="/energy.png" alt="Energy Icon" width={64} height={64} />
            <VStack>
              <Text>Energy: {Number(gameState.energy)}</Text>
              <Text>Next in: {nextEnergyIn}</Text>
            </VStack>
          </HStack>
        </HStack>
      )}
    </>
  )
}

export default DisplayPlayerData
