import {
  VStack,
  HStack,
  Button,
  Text,
  Box,
  Flex,
  Spacer,
  Heading,
} from "@chakra-ui/react"
import { useEffect, useState } from "react"
import { PublicKey, SystemProgram, Transaction } from "@solana/web3.js"
import { useWallet } from "@solana/wallet-adapter-react"
import WalletMultiButton from "@/components/WalletMultiButton"
import {
  program,
  connection,
  globalLevel1GameDataAccount,
  goldTokenMint
} from "@/utils/anchor"
import {
  TOKEN_PROGRAM_ID,
  createMint,
  getMint,
  mintTo,
  getAccount,
  getOrCreateAssociatedTokenAccount,
  ASSOCIATED_TOKEN_PROGRAM_ID,
  getAssociatedTokenAddressSync,
} from "@solana/spl-token"; 

type GameDataAccount = {
  board: Array<Array<Tile>>
  action_id: number
}

type Tile = {
  player: PublicKey,      // 32
  state: number,           // 1
  health: number,         // 8
  damage: number,         // 8
  range: number,          // 2
  collect_reward: number, // 8
  avatar: PublicKey,      // 32 used in the client to display the avatar
  look_direction: number,  // 1 (Up, right, down, left)
  ship_level: number,     // 2
  start_health: number,
}

export default function Home() {
  const { publicKey, sendTransaction } = useWallet()

  const [loadingInitialize, setLoadingInitialize] = useState(false)
  const [loadingRight, setLoadingRight] = useState(false)
  const [loadingLeft, setLoadingLeft] = useState(false)

  const [playerPosition, setPlayerPosition] = useState("........")
  const [message, setMessage] = useState("")
  const [gameDataAccount, setGameDataAccount] =
    useState<any | null>(null)

  const updatePlayerPosition = (gameDataAccount: GameDataAccount) => {
    console.log(JSON.stringify(gameDataAccount));
    setMessage("The board has width " + gameDataAccount.board.length);
  }

  useEffect(() => {
    if (gameDataAccount) {
      updatePlayerPosition(gameDataAccount)
    } else {
      console.log("gameDataAccount or playerPosition is null")
    }
  }, [gameDataAccount])

  useEffect(() => {
    fetchData(globalLevel1GameDataAccount)
  }, [])

  async function handleClickGetData() {
    fetchData(globalLevel1GameDataAccount)
  }

  async function handleClickInitialize() {
    if (publicKey) {
      const transaction = program.methods
        .initialize()
        .accounts({
          newGameDataAccount: globalLevel1GameDataAccount,
          signer: publicKey,
        })
        .transaction()

      await sendAndConfirmTransaction(() => transaction, setLoadingInitialize)
    } else {
      try {
        const response = await fetch("/api/sendTransaction", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ instruction: "initialize" }),
        })
        const data = await response.json()
        console.log(data)
      } catch (error) {
        console.error(error)
      }
    }
  }

  async function handleClickRight() {
    if (publicKey) {
      // TODO: add random block bump
      const transaction = program.methods
        .movePlayerV2(1, 999)
        .accounts({
          gameDataAccount: globalLevel1GameDataAccount,
          // TODO: Add account
        })
        .transaction()

      await sendAndConfirmTransaction(() => transaction, setLoadingRight)
    } else {
      try {
        const response = await fetch("/api/sendTransaction", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ instruction: "moveRight" }),
        })
        const data = await response.json()
        console.log(data)
      } catch (error) {
        console.error(error)
      }
    }
  }

  async function handleInitShipRight() {
    if (publicKey) {
      const [shipPDA] = PublicKey.findProgramAddressSync(
        [Buffer.from("ship"), publicKey.toBuffer()],
        program.programId
      );
  
      let [token_vault, bump2] = PublicKey.findProgramAddressSync(
        [Buffer.from("token_vault", "utf8"), goldTokenMint.toBuffer()],
        program.programId
      );

      const playerTokenAccount = await getAssociatedTokenAddressSync(
        goldTokenMint,
        publicKey,
        true,
        TOKEN_PROGRAM_ID,
        ASSOCIATED_TOKEN_PROGRAM_ID
    );
  
      let tx = await program.methods.initializeShip()
      .accounts({
        newShip: shipPDA,
        signer: publicKey,
        nftAccount: publicKey,
        systemProgram: SystemProgram.programId,
      });
      console.log("Init ship transaction", tx);
  

      await sendAndConfirmTransaction(() => transaction, setLoadingRight)
    } else {
      try {
        const response = await fetch("/api/sendTransaction", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ instruction: "moveRight" }),
        })
        const data = await response.json()
        console.log(data)
      } catch (error) {
        console.error(error)
      }
    }
  }

  async function handleClickLeft() {
    if (publicKey) {
      // TODO: add random block bump. Extract into function
      const transaction = program.methods
        .movePlayerV2(3, 999)
        .accounts({
          gameDataAccount: globalLevel1GameDataAccount,
        })
        .transaction()

      await sendAndConfirmTransaction(() => transaction, setLoadingLeft)
    } else {
      try {
        const response = await fetch("/api/sendTransaction", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ instruction: "moveLeft" }),
        })
        setLoadingLeft(false)
        const data = await response.json()
        console.log(data)
      } catch (error) {
        console.error(error)
      }
    }
  }

  async function sendAndConfirmTransaction(
    transactionBuilder: () => Promise<Transaction>,
    setLoading: (loading: boolean) => void
  ) {
    if (!publicKey || !program || !connection) return

    setLoading(true)

    try {
      const tx = await transactionBuilder()
      const txSig = await sendTransaction(tx, connection)

      const { blockhash, lastValidBlockHeight } =
        await connection.getLatestBlockhash()

      await connection.confirmTransaction({
        blockhash,
        lastValidBlockHeight,
        signature: txSig,
      })

      setLoading(false)
    } catch (error) {
      console.error("Error processing transaction:", error)
      setLoading(false)
    }
  }

  const fetchData = async (pda: PublicKey) => {
    console.log("Fetching GameDataAccount state...")

    try {
      const account = await program.account.gameDataAccount.fetch(pda)
      console.log(JSON.stringify(account, null, 2))
      setGameDataAccount(account)
    } catch (error) {
      console.log(`Error fetching GameDataAccount state: ${error}`)
    }
  }

  useEffect(() => {
    if (!globalLevel1GameDataAccount) return

    const subscriptionId = connection.onAccountChange(
      globalLevel1GameDataAccount,
      (accountInfo) => {
        const decoded = program.coder.accounts.decode(
          "gameDataAccount",
          accountInfo.data
        )
        console.log("New player position via socket", decoded.playerPosition)
        setGameDataAccount(decoded)
      }
    )

    return () => {
      connection.removeAccountChangeListener(subscriptionId)
    }
  }, [connection, globalLevel1GameDataAccount, program])

  return (
    <Box>
      <Flex px={4} py={4}>
        <Spacer />
        <WalletMultiButton />
      </Flex>
      <VStack justifyContent="center" alignItems="center" height="75vh">
        <VStack>
          <Heading fontSize="xl">{message}</Heading>
          <Text fontSize="6xl">{playerPosition}</Text>
          <HStack>
            <Button
              width="100px"
              isLoading={loadingLeft}
              onClick={handleClickLeft}
            >
              Move Left
            </Button>
            <Button width="100px" onClick={handleClickGetData}>
              Get Data
            </Button>
            <Button
              width="100px"
              isLoading={loadingRight}
              onClick={handleClickRight}
            >
              Move Right
            </Button>
            <Button
              width="100px"
              isLoading={loadingRight}
              onClick={handleClickRight}
            >
              Init Ship
            </Button>
          </HStack>
          <Button
            width="100px"
            isLoading={loadingInitialize}
            onClick={handleClickInitialize}
          >
            Initialize
          </Button>
        </VStack>
      </VStack>
    </Box>
  )
}
