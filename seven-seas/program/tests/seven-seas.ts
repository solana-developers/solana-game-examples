import * as anchor from "@project-serum/anchor";
import { Program } from "@project-serum/anchor";
import { SevenSeas } from "../target/types/seven_seas";
import { publicKey } from "@project-serum/anchor/dist/cjs/utils";

describe("seven-seas", () => {
  // Configure the client to use the local cluster.
  anchor.setProvider(anchor.AnchorProvider.env());

  const program = anchor.workspace.SevenSeas as Program<SevenSeas>;
  const signer = anchor.web3.Keypair.generate();
  console.log("Local signer is: ", signer.publicKey.toBase58());

  it("Is initialized!", async () => {
    
    let confirmOptions = {
      skipPreflight: true,
    };

    console.log(new Date(), "requesting airdrop");
    let airdropTx = await anchor.getProvider().connection.requestAirdrop(
      signer.publicKey,
      5 * anchor.web3.LAMPORTS_PER_SOL
    );

    const res = await anchor.getProvider().connection.confirmTransaction(airdropTx);

    const [level] = anchor.web3.PublicKey.findProgramAddressSync(
      [Buffer.from("level")],
      program.programId
    );
    
    const [chestVault] = anchor.web3.PublicKey.findProgramAddressSync(
      [Buffer.from("chestVault")],
      program.programId
    );
    
    const tx = await program.methods.initialize()
    .accounts({
      newGameDataAccount: level,
      chestVault: chestVault,
      signer: signer.publicKey,
      systemProgram: anchor.web3.SystemProgram.programId,
    })
    .signers([signer])
    .rpc(confirmOptions);
    console.log("Your transaction signature", tx);
  });

  it("Spawn ship!", async () => {

    console.log(new Date(), "requesting airdrop");
    let airdropTx = await anchor.getProvider().connection.requestAirdrop(
      signer.publicKey,
      5 * anchor.web3.LAMPORTS_PER_SOL
    );

    let confirmOptions = {
      skipPreflight: true,
    };

    const res = await anchor.getProvider().connection.confirmTransaction(airdropTx);

    const [level] = anchor.web3.PublicKey.findProgramAddressSync(
      [Buffer.from("level")],
      program.programId
    );
    
    const [chestVault] = anchor.web3.PublicKey.findProgramAddressSync(
      [Buffer.from("chestVault")],
      program.programId
    );
    const avatarPubkey = anchor.web3.Keypair.generate();

    const tx = await program.methods.spawnPlayer(avatarPubkey.publicKey)
    .accounts({
      payer: signer.publicKey,
      gameDataAccount: level,
      chestVault: chestVault,
      systemProgram: anchor.web3.SystemProgram.programId,
    })
    .signers([signer])
    .rpc(confirmOptions);
    console.log("Your transaction signature", tx);
  });
});
