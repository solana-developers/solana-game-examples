using System.Collections.Generic;

namespace Solnet.Metaplex
{
    internal static class VaultProgramInstructions
    {
        /// <summary>
        /// Represents the user-friendly names for the instruction types for the <see cref="VaultProgram"/>.
        /// </summary>
        internal static readonly Dictionary<Values, string> Names = new()
        {
            { Values.InitVault, "InitVault" },
            { Values.AddTokenToInactiveVault, "AddTokenToInactiveVault" },
            { Values.ActivateVault, "ActivateVault" },
            { Values.CombineVault, "CombineVault" },
            { Values.RedeemShares, "RedeemShares" },
            { Values.WithdrawTokenFromSafetyDepositBox, "WithdrawTokenFromSafetyDepositBox" },
            { Values.MintFractionalShares, "MintFractionalShares" },
            { Values.WithdrawSharesFromTreasury, "WithdrawSharesFromTreasury" },
            { Values.AddSharesToTreasury, "AddSharesToTreasury" },
            { Values.UpdateExternalPriceAccount, "UpdateExternalPriceAccount" },
            { Values.SetAuthority, "SetAuthority" }
        };

        internal enum Values
        {
            InitVault = 0,
            AddTokenToInactiveVault =1,
            ActivateVault = 2,
            CombineVault = 3,
            RedeemShares = 4,
            WithdrawTokenFromSafetyDepositBox = 5,
            MintFractionalShares = 6,
            WithdrawSharesFromTreasury = 7,
            AddSharesToTreasury = 8,
            UpdateExternalPriceAccount = 9,
            SetAuthority = 10
        }
    }
}