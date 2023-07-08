using Frictionless;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using SolPlay.Scripts.Services;
using TMPro;
using UnityEngine;

namespace SolPlay.Scripts.Ui
{
    /// <summary>
    /// Shows the amount of the token "TokenMintAddress" from the connected Wallet.
    /// </summary>
    public class TokenPanel : MonoBehaviour
    {
        public TextMeshProUGUI TokenAmount;

        public string
            TokenMintAdress =
                "PLAyKbtrwQWgWkpsEaMHPMeDLDourWEWVrx824kQN8P"; // Solplay Token, replace with whatever token you like.

        private PublicKey _associatedTokenAddress;
        
        void Start()
        {
            if (Web3.Instance.WalletBase.Account != null)
            {
                UpdateTokenAmount();
            }
        }

        private async void UpdateTokenAmount()
        {
            if (Web3.Instance.WalletBase.Account == null)
            {
                return;
            }

            var wallet = Web3.Instance.WalletBase;

            if (wallet != null && wallet.Account.PublicKey != null)
            {
                _associatedTokenAddress =
                    AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(wallet.Account.PublicKey, new PublicKey(TokenMintAdress));
            }
            
            if (_associatedTokenAddress == null)
            {
                return;
            }
            
            var tokenBalance = await wallet.ActiveRpcClient.GetTokenAccountBalanceAsync(_associatedTokenAddress, Commitment.Confirmed);
            if (tokenBalance.Result == null || tokenBalance.Result.Value == null)
            {
                TokenAmount.text = "0";
                return;
            }
            TokenAmount.text = tokenBalance.Result.Value.UiAmountString;
        }
    }
}