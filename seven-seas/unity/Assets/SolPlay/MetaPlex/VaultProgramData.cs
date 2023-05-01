using Solana.Unity.Programs.Utilities;
using Solana.Unity.Programs;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.IO;
using System.Text;


namespace Solnet.Metaplex
{

    internal class VaultProgramData
    {
        internal static int MethodOffset = 0;
      
        internal static byte[] EncodeAddTokenToInactiveVault( UInt64 amount )
        {
            var buffer = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(buffer);

            writer.Write( (byte) VaultProgramInstructions.Values.AddTokenToInactiveVault );
            writer.Write( amount );

            return buffer.ToArray();
        }

        internal static byte[] EncodeActivateVault( UInt64 numberOfInitialShares )
        {
            var buffer = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(buffer);

            writer.Write( (byte) VaultProgramInstructions.Values.ActivateVault );
            writer.Write( numberOfInitialShares );

            return buffer.ToArray();
        }

        internal static byte[] EncodeWithdrawTokenFromSafetyDepositBox( UInt64 Amount)
        {
            var buffer = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(buffer);

            writer.Write( (byte) VaultProgramInstructions.Values.WithdrawTokenFromSafetyDepositBox );
            writer.Write( Amount );

            return buffer.ToArray();
        }

        internal static byte[] EncodeMintFractionalShares( UInt64 amount )
        {
            var buffer = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(buffer);

            writer.Write( (byte) VaultProgramInstructions.Values.MintFractionalShares );
            writer.Write( amount );

            return buffer.ToArray();
        }

        internal static byte[] EncodeWithdrawSharesFromTreasury( UInt64 amount )
        {
            var buffer = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(buffer);

            writer.Write( (byte) VaultProgramInstructions.Values.WithdrawSharesFromTreasury );
            writer.Write( amount );

            return buffer.ToArray();
        }

        internal static byte[] EncodeAddSharesToTreasury( UInt64 amount )
        {
            var buffer = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(buffer);

            writer.Write( (byte) VaultProgramInstructions.Values.AddSharesToTreasury );
            writer.Write( amount );

            return buffer.ToArray();
        }

        internal static byte[] EncodeUpdateExternalPriceAccount( UInt64 PricePerShare, PublicKey PriceMint, bool AllowedToCombine)
        {
            var buffer = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(buffer);

            writer.Write( (byte) VaultProgramInstructions.Values.UpdateExternalPriceAccount );
            writer.Write( PricePerShare );
            writer.Write( PriceMint.KeyBytes );
            writer.Write( AllowedToCombine );

            return buffer.ToArray();
        }


        /// DECODING FUNCTIONS

        internal static void DecodeInitVault( 
            DecodedInstruction decodedInstruction, 
            ReadOnlySpan<byte> data,
            IList<PublicKey> keys, 
            byte[] keyIndices
            )
            {
                decodedInstruction.Values.Add("ShareMint", keys[keyIndices[0]]);
                decodedInstruction.Values.Add("RedeemTreasuryTokenAccount", keys[keyIndices[1]]);
                decodedInstruction.Values.Add("FractionTreasuryTokenAccount", keys[keyIndices[2]]);
                decodedInstruction.Values.Add("Vault", keys[keyIndices[3]]); 
                decodedInstruction.Values.Add("VaultAuthority", keys[keyIndices[4]]);
                decodedInstruction.Values.Add("PricingLookupAddress", keys[keyIndices[5]]);
                decodedInstruction.Values.Add("TokenProgram", keys[keyIndices[6]]);
                decodedInstruction.Values.Add("SysVarRent", keys[keyIndices[7]]);
            }

        internal static void DecodeActivateVault(
            DecodedInstruction decodedInstruction,
            ReadOnlySpan<byte> data,
            IList<PublicKey> keys,
            byte[] keyIndices
            )
        {
            decodedInstruction.Values.Add("Vault", keys[keyIndices[0]]);
            decodedInstruction.Values.Add("FractonalMint", keys[keyIndices[1]]);
            decodedInstruction.Values.Add("FractionTreasury", keys[keyIndices[2]]);
            decodedInstruction.Values.Add("FractionMintAuthority", keys[keyIndices[3]]);
            decodedInstruction.Values.Add("VaultAuthority", keys[keyIndices[4]]);

            UInt64 amount = data.GetU64(1);
            decodedInstruction.Values.Add("NumberOfInitialShares", amount);
        } 

        internal static void DecodeCombineVault(
            DecodedInstruction decodedInstruction,
            ReadOnlySpan<byte> data,
            IList<PublicKey> keys,
            byte[] keyIndices
            )
        {
            decodedInstruction.Values.Add("Vault", keys[keyIndices[0]]);
            decodedInstruction.Values.Add("TokenAccountShares", keys[keyIndices[1]]);
            decodedInstruction.Values.Add("TokenAccountReedem", keys[keyIndices[2]]);
            decodedInstruction.Values.Add("FractionMint", keys[keyIndices[3]]);
            decodedInstruction.Values.Add("FractionTreasury", keys[keyIndices[4]]);
            decodedInstruction.Values.Add("ReedemTreasury", keys[keyIndices[5]]);
            decodedInstruction.Values.Add("NewVaultAuthority", keys[keyIndices[6]]);
            decodedInstruction.Values.Add("VaultAuthority", keys[keyIndices[7]]);
            decodedInstruction.Values.Add("BurnAuthority", keys[keyIndices[8]]);
            decodedInstruction.Values.Add("PricingOracle", keys[keyIndices[9]]);
        }
        

        internal static void DecodeAddTokenToInactiveVault(
            DecodedInstruction decodedInstruction,
            ReadOnlySpan<byte> data,
            IList<PublicKey> keys,
            byte[] keyIndices
            )
        {
            decodedInstruction.Values.Add("SafetyDepositBox", keys[keyIndices[0]]);
            decodedInstruction.Values.Add("TokenAccount", keys[keyIndices[1]]);
            decodedInstruction.Values.Add("TokenStoreAccount", keys[keyIndices[2]]);
            decodedInstruction.Values.Add("FractionTreasuryTokenAccount", keys[keyIndices[3]]);
            decodedInstruction.Values.Add("VaultAuthority", keys[keyIndices[4]]);
            decodedInstruction.Values.Add("Payer", keys[keyIndices[5]]);
            decodedInstruction.Values.Add("TransferAuthority", keys[keyIndices[6]]);

            UInt64 amount = data.GetU64(1);
            decodedInstruction.Values.Add("Amount", amount);
        }

        internal static void DecodeRedeemShares(
            DecodedInstruction decodedInstruction,
            ReadOnlySpan<byte> data,
            IList<PublicKey> keys,
            byte[] keyIndices
            )
        {
            decodedInstruction.Values.Add("TokenAccountShares", keys[keyIndices[0]]);
            decodedInstruction.Values.Add("TokenAccountReedem", keys[keyIndices[1]]);
            decodedInstruction.Values.Add("FractionMint", keys[keyIndices[2]]);
            decodedInstruction.Values.Add("ReedemTreasury", keys[keyIndices[3]]);
            decodedInstruction.Values.Add("TransferAuthority", keys[keyIndices[4]]);
            decodedInstruction.Values.Add("BurnAuthority", keys[keyIndices[5]]);
            decodedInstruction.Values.Add("Vault", keys[keyIndices[6]]);
        }
        
        internal static void DecodeAddSharesToTreasury(
            DecodedInstruction decodedInstruction,
            ReadOnlySpan<byte> data,
            IList<PublicKey> keys,
            byte[] keyIndices
            )
        {
            decodedInstruction.Values.Add("Source", keys[keyIndices[0]]);
            decodedInstruction.Values.Add("FractionTreasury", keys[keyIndices[1]]);
            decodedInstruction.Values.Add("Vault", keys[keyIndices[2]]);
            decodedInstruction.Values.Add("TransferAuthority", keys[keyIndices[3]]);
            decodedInstruction.Values.Add("VaultAuthority", keys[keyIndices[4]]);

            UInt64 amount = data.GetU64(1);
            decodedInstruction.Values.Add("Amount", amount);
        }

        internal static void DecodeWidthdrawTokenFromSafetyDepositBox(
            DecodedInstruction decodedInstruction,
            ReadOnlySpan<byte> data,
            IList<PublicKey> keys,
            byte[] keyIndices
            )
        {
            decodedInstruction.Values.Add("Destination", keys[keyIndices[0]]);
            decodedInstruction.Values.Add("SafetyDepositBox", keys[keyIndices[1]]);
            decodedInstruction.Values.Add("Store", keys[keyIndices[2]]);
            decodedInstruction.Values.Add("Vault", keys[keyIndices[3]]);
            decodedInstruction.Values.Add("FractionMint", keys[keyIndices[4]]);
            decodedInstruction.Values.Add("VaultAuthority", keys[keyIndices[5]]);
            decodedInstruction.Values.Add("TransferAuthority", keys[keyIndices[6]]);

            UInt64 amount = data.GetU64(1);
            decodedInstruction.Values.Add("Amount", amount);
        }

        internal static void DecodeMintFractionalShares(
            DecodedInstruction decodedInstruction,
            ReadOnlySpan<byte> data,
            IList<PublicKey> keys,
            byte[] keyIndices
            )
        {
            decodedInstruction.Values.Add("FractionTreasury", keys[keyIndices[0]]);
            decodedInstruction.Values.Add("FractionMint", keys[keyIndices[1]]);
            decodedInstruction.Values.Add("Vault", keys[keyIndices[2]]);
            decodedInstruction.Values.Add("MintAuthority", keys[keyIndices[3]]);
            decodedInstruction.Values.Add("VaultAuthority", keys[keyIndices[4]]);

            UInt64 amount = data.GetU64(1);
            decodedInstruction.Values.Add("Amount", amount);
        }

        internal static void DecodeWidthdrawSharesFromTreasury(
            DecodedInstruction decodedInstruction,
            ReadOnlySpan<byte> data,
            IList<PublicKey> keys,
            byte[] keyIndices
            )
        {
            decodedInstruction.Values.Add("Destination", keys[keyIndices[0]]);
            decodedInstruction.Values.Add("FractionTreasury", keys[keyIndices[1]]);
            decodedInstruction.Values.Add("Vault", keys[keyIndices[2]]);
            decodedInstruction.Values.Add("TransferAuthority", keys[keyIndices[3]]);
            decodedInstruction.Values.Add("VaultAuthority", keys[keyIndices[4]]);

            UInt64 amount = data.GetU64(1);
            decodedInstruction.Values.Add("Amount", amount);
        }

        internal static void DecodeUpdateExternalPriceAccount(
            DecodedInstruction decodedInstruction,
            ReadOnlySpan<byte> data,
            IList<PublicKey> keys,
            byte[] keyIndices
            )
        {
            decodedInstruction.Values.Add("ExternalPriceAccount", keys[keyIndices[0]]);
            decodedInstruction.Values.Add("PricePerShare", data.GetU64(1));
            decodedInstruction.Values.Add("PriceMint", keys[keyIndices[1]]);
            decodedInstruction.Values.Add("AllowedToCombine", data.GetBool(9));
        }
        
        internal static void DecodeSetAuthority(
            DecodedInstruction decodedInstruction,
            ReadOnlySpan<byte> data,
            IList<PublicKey> keys,
            byte[] keyIndices
            )
        {
            decodedInstruction.Values.Add("Vault", keys[keyIndices[0]]);
            decodedInstruction.Values.Add("VaultAuthority", keys[keyIndices[1]]);
            decodedInstruction.Values.Add("NewAuthority", keys[keyIndices[2]]);
        }

    }

}