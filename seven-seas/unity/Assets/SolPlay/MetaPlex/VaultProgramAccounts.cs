using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Utilities;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Utilities;
using Solana.Unity.Programs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace Solnet.Metaplex
{
    enum VaultKey {
        Uninitialized = 0,
        VaultV1 = 3, 
        SafetyDepositBoxV1 = 1,
        ExternalPriceAccountV1 = 2,
    }
    
    enum VaultState {
        Inactive = 0,
        Active = 1,
        Combined = 2,
        Deactivated = 3,
    }
    
    class VaultAccount : Account
    {
        public VaultKey key;
        public PublicKey tokenProgram;
        public PublicKey fractionMint;
        public PublicKey authority;
        public PublicKey fractionTreasury;
        public PublicKey reedemTreasury;
        public bool allowFurtherShareCreation;
        public PublicKey pricingLookupAddress;
        public short tokenTypeCount;
        public VaultState state;

        private UInt64 lockedPricePerShare;

        VaultAccount()
        {
            this.key = VaultKey.VaultV1;
        }

        public async Task<PublicKey> getPDA(PublicKey pk)
        {
            throw new NotImplementedException();
        }

        static bool IsCompatible(ReadOnlySpan<byte> data)
        {
            return data.GetS8(0) == (sbyte) VaultKey.VaultV1;
        }

        class SafetyDepositBox : Account
        {
            private AccountInfo info;      
            public VaultKey key;
            public PublicKey vault;
            public PublicKey tokenMint;
            public PublicKey store;
            public short order;

            SafetyDepositBox()
            {
                this.key = VaultKey.SafetyDepositBoxV1;
            }

            SafetyDepositBox(PublicKey pk, AccountInfo info)
            {
                if (VaultProgram.ProgramIdKey != info.Owner) throw new ErrorNotOwner();
                if ( info.Data.Count != 0 
                    && SafetyDepositBox.IsCompatible( Encoding.UTF8.GetBytes(info.Data[0]) )) 
                    throw new ErrorInvalidAccountData(); 
                this.info = info;
            }
            
            static PublicKey getPDA(PublicKey vault, PublicKey mint)
            {
                PublicKey address;
                byte nonce;
                PublicKey.TryFindProgramAddress(
                    new List<byte[]>() {
                        Encoding.UTF8.GetBytes(VaultProgram.PREFIX),
                        vault,
                        mint
                    },
                    VaultProgram.ProgramIdKey,
                    out address,
                    out nonce
                );

                return address;
            }
            
            static bool IsCompatible(ReadOnlySpan<byte> data)
            {
                return data.GetS8(0) == (byte) VaultKey.SafetyDepositBoxV1;
            }
            
            
        }

        public class ErrorNotOwner : Exception
        {
            public ErrorNotOwner() : base("Private key given is not a owner.") {}
        }
        
        public class ErrorInvalidAccountData : Exception
        {
            public ErrorInvalidAccountData() : base("Account data is not of a correct type.") {}
        }
    }
    
    
    
}