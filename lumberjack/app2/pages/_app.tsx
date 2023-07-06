import { ChakraProvider } from "@chakra-ui/react"
import WalletContextProvider from "../contexts/WalletContextProvider"
import SessionProvider from "@/contexts/SessionProvider"
import { GameStateProvider } from "@/contexts/GameStateProvider"
import type { AppProps } from "next/app"

export default function App({ Component, pageProps }: AppProps) {
  return (
    <ChakraProvider>
      <WalletContextProvider>
        <SessionProvider>
          <GameStateProvider>
            <Component {...pageProps} />
          </GameStateProvider>
        </SessionProvider>
      </WalletContextProvider>
    </ChakraProvider>
  )
}
