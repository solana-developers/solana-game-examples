using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Utilities;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Utilities;
using Solana.Unity.Programs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Buffers.Binary;
using System.Linq;

namespace Solnet.Metaplex
{
    /// <summary>
    /// Implements the Metadata program methods.
    /// <remarks>
    /// For more information see:
    /// https://github.com/metaplex-foundation/metaplex
    /// https://metaplex-foundation.github.io/metaplex-program-library/docs/token-metadata/index.html
    /// </remarks>
    /// </summary>
    public static class MetadataProgram
    {
        /// <summary>
        /// The public key of the Metadata Program.
        /// </summary>
        public static readonly PublicKey ProgramIdKey = new("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s");

        /// <summary>
        /// The program's name.
        /// </summary>
        private const string ProgramName = "Metadata Program";

        /// <summary>
        /// Create Metadata object.
        /// </summary>
        /// <param name="metadataKey"> Metadata key (pda of ['metadata', program id, mint id]) </param>
        /// <param name="mintKey"> Mint of token asset </param>
        /// <param name="authorityKey"> Mint authority </param>
        /// <param name="payerKey"> Transaction payer </param>
        /// <param name="updateAuthority"> Metadata update authority </param>
        /// <param name="data"> Metadata struct with name,symbol,uri and optional list of creators </param>
        /// <param name="updateAuthorityIsSigner"> Is the update authority a signer </param>
        /// <param name="isMutable"> Will the account stay mutable.</param>
        /// <returns>The transaction instruction.</returns> 
        public static TransactionInstruction CreateMetadataAccount(PublicKey metadataKey, PublicKey mintKey, PublicKey authorityKey, PublicKey payerKey, PublicKey updateAuthority, MetadataV1 data, bool updateAuthorityIsSigner, bool isMutable)
        {

            List<AccountMeta> keys = new()
            {
                AccountMeta.Writable(metadataKey, false),
                AccountMeta.ReadOnly(mintKey, false),
                AccountMeta.ReadOnly(authorityKey, true),
                AccountMeta.Writable(payerKey, true),
                AccountMeta.ReadOnly(updateAuthority, false),
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(SysVars.RentKey, false)
            };

            return new TransactionInstruction
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = keys,
                Data = MetadataProgramData.EncodeCreateMetadataAccountData(data, isMutable)
            };
        }
        /// <summary>
        /// Create Metadata object.
        /// </summary>
        /// <param name="metadataKey"> Metadata key (pda of ['metadata', program id, mint id]) </param>
        /// <param name="mintKey"> Mint of token asset </param>
        /// <param name="authorityKey"> Mint authority </param>
        /// <param name="payerKey"> Transaction payer </param>
        /// <param name="updateAuthority"> Metadata update authority </param>
        /// <param name="data"> Metadata struct with name,symbol,uri and optional list of creators </param>
        /// <param name="updateAuthorityIsSigner"> Is the update authority a signer </param>
        /// <param name="isMutable"> Will the account stay mutable.</param>
        /// /// <param name="collectionDetails"> Collection details - serial code</param>
        /// <returns>The transaction instruction.</returns> 
        public static TransactionInstruction CreateMetadataAccountV3(PublicKey metadataKey, PublicKey mintKey, PublicKey authorityKey, PublicKey payerKey, PublicKey updateAuthority,  MetadataV3 data, bool isMutable, bool updateAuthorityIsSigner, ulong collectionDetails = 0)
        {   
            List<AccountMeta> keys = new()
            {
                AccountMeta.Writable(metadataKey, false),
                AccountMeta.ReadOnly(mintKey, false),
                AccountMeta.ReadOnly(authorityKey, true),
                AccountMeta.ReadOnly(payerKey, true),
                AccountMeta.ReadOnly(updateAuthority, false),
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(SysVars.RentKey, false)
            };

            return new TransactionInstruction
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = keys,
                Data = MetadataProgramData.EncodeCreateMetadataAccountDataV3(data, isMutable, collectionDetails)
            };
        }

        ///<summary>
        /// Update metadata account.
        ///</summary>
        public static TransactionInstruction UpdateMetadataAccount( PublicKey metadataKey, PublicKey updateAuthority, PublicKey newUpdateAuthority, MetadataV1 data, bool? primarySaleHappend)
        {
            List<AccountMeta> keys = new()
            {
                AccountMeta.Writable(metadataKey, false),
                AccountMeta.ReadOnly(updateAuthority, true)
            };

            return new TransactionInstruction
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = keys,
                Data = MetadataProgramData.EncodeUpdateMetadataData(data, newUpdateAuthority, primarySaleHappend)
            };
        }

        /// <summary>
        /// Sign a piece of metadata that has you as an unverified creator so that it is now verified.
        /// </summary>
        /// <param name="metadataKey"> PDA of ('metadata', program id, mint id) </param>
        /// <param name="creatorKey"> Creator key </param>
        /// <returns></returns>
        public static TransactionInstruction SignMetada(PublicKey metadataKey, PublicKey creatorKey)
        {
            byte[] data = new byte[1];
            data.WriteU8((byte)MetadataProgramDiscriminatorStruct.Values.SignMetadata, 0);

            return new TransactionInstruction()
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = new List<AccountMeta>()
                {
                    AccountMeta.Writable( metadataKey , false),
                    AccountMeta.ReadOnly( creatorKey, true)
                },
                Data = data
            };
        }

        /// <summary>
        /// Make all of metadata variable length fields (name/uri/symbol) a fixed length
        /// </summary>
        /// <param name="metadataKey"> PDA of ('metadata', program id, mint id) </param>
        /// <returns></returns>
        public static TransactionInstruction PuffMetada(PublicKey metadataKey)
        {
            return new TransactionInstruction()
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = new List<AccountMeta>()
                {
                    AccountMeta.Writable( metadataKey , false )
                },
                Data = new byte[] { (byte)MetadataProgramDiscriminatorStruct.Values.PuffMetadata }
            };
        }

        /// <summary>
        ///  Allows updating the primary sale boolean on Metadata solely through owning an account
        /// containing a token from the metadata's mint and being a signer on this transaction.
        /// A sort of limited authority for limited update capability that is required for things like
        /// Metaplex to work without needing full authority passing.
        /// </summary>
        /// <param name="metadataKey"> Metadata key (pda of ['metadata', program id, mint id]) </param>
        /// <param name="owner"> Owner on the token account </param>
        /// <param name="tokenAccount">  Account containing tokens from the metadata's mint </param>
        /// <returns></returns>
        public static TransactionInstruction UpdatePrimarySaleHappendViaToken(PublicKey metadataKey,PublicKey owner,PublicKey tokenAccount)
        {
            return new TransactionInstruction()
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = new List<AccountMeta>()
                {
                    AccountMeta.Writable(metadataKey, false),
                    AccountMeta.ReadOnly(owner, true),
                    AccountMeta.ReadOnly(tokenAccount, false)
                },
                Data = new byte[] { (byte)MetadataProgramDiscriminatorStruct.Values.UpdatePrimarySaleHappenedViaToken }
            };
        }


        /// <summary>
        ///  Create MasterEdition PDA.
        /// </summary>
        /// <param name="maxSupply"></param>
        /// <param name="masterEditionKey"> PDA of [ 'metadata', program id, mint, 'edition' ]</param>
        /// <param name="mintKey"></param>
        /// <param name="updateAuthorityKey"> </param>
        /// <param name="mintAuthority"> Mint authority on the metadata's mint - THIS WILL TRANSFER AUTHORITY AWAY FROM THIS KEY </param>
        /// <param name="payer"></param>
        /// <param name="metadataKey"></param>
        /// <returns> Transaction instruction. </returns>/
        public static TransactionInstruction CreateMasterEdition(ulong? maxSupply, PublicKey masterEditionKey, PublicKey mintKey, PublicKey updateAuthorityKey, PublicKey mintAuthority, PublicKey payer, PublicKey metadataKey)
        {
            List<AccountMeta> keys = new()
            {
                AccountMeta.Writable(masterEditionKey, false),
                AccountMeta.Writable(mintKey, false),
                AccountMeta.ReadOnly(updateAuthorityKey, true),
                AccountMeta.ReadOnly(mintAuthority, true),
                AccountMeta.ReadOnly(payer, true),
                AccountMeta.ReadOnly(metadataKey, false),
                AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(SysVars.RentKey, false)
            };

            return new TransactionInstruction
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = keys,
                Data = MetadataProgramData.EncodeCreateMasterEdition(maxSupply)
            };
        }

        /// <summary>
        ///  Mint a new edition via token
        /// </summary>
        /// <param name="edition"></param>
        /// <param name="newMetadataKey"></param>
        /// <param name="newEdition"></param>
        /// <param name="masterEdition"></param>
        /// <param name="newMint"></param>
        /// <param name="newMintAuthority"></param>
        /// <param name="payer"></param>
        /// <param name="tokenAccountOwner"></param>
        /// <param name="tokenAccount"></param>
        /// <param name="updateAuthority"></param>
        /// <param name="newMetadataUpdateAuthority"></param>
        /// <param name="metadataKey"></param>
        /// <param name="metadataMint"></param>
        /// <returns></returns>
        public static TransactionInstruction MintNewEditionFromMasterEditionViaToken(
            uint edition,
            PublicKey newMetadataKey,
            PublicKey newEdition,
            PublicKey masterEdition,
            PublicKey newMint,
            PublicKey newMintAuthority,
            PublicKey payer,
            PublicKey tokenAccountOwner,
            PublicKey tokenAccount,
            PublicKey updateAuthority,
            PublicKey newMetadataUpdateAuthority,
            PublicKey metadataKey,
            PublicKey metadataMint
        )
        {
            int BIT_SIZE = 248;
            int editionNumber = (int)Math.Floor((double)edition / BIT_SIZE);
            //EDITION PDA
            PublicKey editionPda;
            byte nonce;
            PublicKey.TryFindProgramAddress(
                new List<byte[]>() {
                    Encoding.UTF8.GetBytes("metadata"),
                    MetadataProgram.ProgramIdKey,
                    metadataMint,
                    Encoding.UTF8.GetBytes("edition"),
                    Encoding.UTF8.GetBytes(editionNumber.ToString())
                },
                MetadataProgram.ProgramIdKey,
                out editionPda,
                out nonce
            );

            List<AccountMeta> keys = new()
            {
                AccountMeta.Writable(newMetadataKey, false),
                AccountMeta.Writable(newEdition, false),
                AccountMeta.Writable(masterEdition, false),
                AccountMeta.Writable(newMint, false),
                AccountMeta.Writable(editionPda, false),

                AccountMeta.ReadOnly(newMintAuthority, true),
                AccountMeta.ReadOnly(payer, true),
                AccountMeta.ReadOnly(tokenAccountOwner, true),

                AccountMeta.ReadOnly(tokenAccount, false),
                AccountMeta.ReadOnly(updateAuthority, false),
                AccountMeta.ReadOnly(metadataKey, false),

                AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(SysVars.RentKey, false)
            };

            return new TransactionInstruction 
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = keys,
                Data = MetadataProgramData.EncodeMintNewEditionFromMasterEditionViaToken( edition )
            };
        }

        /// <summary>
        /// Decodes an instruction created by the Metadata Program.
        /// </summary>
        /// <param name="data">The instruction data to decode.</param>
        /// <param name="keys">The account keys present in the transaction.</param>
        /// <param name="keyIndices">The indices of the account keys for the instruction as they appear in the transaction.</param>
        /// <returns>A decoded instruction.</returns>
        public static DecodedInstruction Decode( ReadOnlySpan<byte> data , IList<PublicKey> keys , byte[] keyIndices )
        {
            uint instruction = data.GetU8(MetadataProgramData.MethodOffset);
            MetadataProgramDiscriminatorStruct.Values instructionValue =
                (MetadataProgramDiscriminatorStruct.Values)Enum.Parse(typeof(MetadataProgramDiscriminatorStruct.Values), instruction.ToString());

            DecodedInstruction decodedInstruction = new()
            {
                PublicKey = ProgramIdKey,
                InstructionName = MetadataProgramDiscriminatorStruct.Names[instructionValue],
                ProgramName = ProgramName,
                Values = new Dictionary<string, object>(),
                InnerInstructions = new List<DecodedInstruction>()
            };

            switch (instructionValue)
            {
                case MetadataProgramDiscriminatorStruct.Values.CreateMetadataAccount:
                    MetadataProgramData.DecodeCreateMetadataAccountData(decodedInstruction, data, keys, keyIndices);
                    break;
                case MetadataProgramDiscriminatorStruct.Values.CreateMetadataAccountV3:
                    MetadataProgramData.DecodeCreateMetadataAccountData(decodedInstruction, data, keys, keyIndices);
                    break;
                case MetadataProgramDiscriminatorStruct.Values.UpdateMetadataAccount:
                    MetadataProgramData.DecodeUpdateMetadataAccountData(decodedInstruction, data, keys, keyIndices);
                    break;
                case MetadataProgramDiscriminatorStruct.Values.CreateMasterEdition:
                    MetadataProgramData.DecodeCreateMasterEdition(decodedInstruction, data, keys, keyIndices);
                    break;
                case MetadataProgramDiscriminatorStruct.Values.PuffMetadata:
                    MetadataProgramData.DecodePuffMetada(decodedInstruction, data, keys, keyIndices);
                    break;
                case MetadataProgramDiscriminatorStruct.Values.SignMetadata:
                    MetadataProgramData.DecodeSignMetada(decodedInstruction, data, keys, keyIndices);
                    break;
                case MetadataProgramDiscriminatorStruct.Values.UpdatePrimarySaleHappenedViaToken:
                    MetadataProgramData.DecodeUpdatePrimarySaleHappendViaToken(decodedInstruction, data, keys, keyIndices);
                    break;
                case MetadataProgramDiscriminatorStruct.Values.MintNewEditionFromMasterEditionViaToken:
                    MetadataProgramData.DecodeMintNewEditionFromMasterEditionViaToken(decodedInstruction, data, keys, keyIndices);
                    break;
            }

            return decodedInstruction;
        }

    } //class
} //namespace
