using System;
using System.Collections;
using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using SolPlay.DeeplinksNftExample.Utils;
using SolPlay.Scripts.Ui;
using UnityEngine;

namespace SolPlay.Scripts.Services
{
    public enum WalletType { Phantom, Backpack }
    public class WalletLoggedInMessage
    {
        public WalletBase Wallet;
    }

    public class SolBalanceChangedMessage
    {
        public double SolBalanceChange;
        public bool IsInGameWallet;

        public SolBalanceChangedMessage(double solBalanceChange = 0, bool isInGameWallet = false)
        {
            SolBalanceChange = solBalanceChange;
            IsInGameWallet = isInGameWallet;
        }
    }
    public class WalletHolderService : MonoBehaviour, IMultiSceneSingleton
    {
        public RpcCluster DevnetWalletCluster = RpcCluster.DevNet;

        [HideIfEnumValue("DevnetWalletCluster", HideIf.NotEqual, (int) RpcCluster.Custom)]
        public string DevnetLoginRPCUrl = "";

        public RpcCluster MainnetWalletCluster = RpcCluster.DevNet;

        [HideIfEnumValue("MainnetWalletCluster", HideIf.NotEqual, (int) RpcCluster.Custom)]
        public string MainNetRpcUrl = "";

        public PhantomWalletOptions PhantomWalletOptions;

        [NonSerialized] public WalletBase BaseWallet;

        public bool IsLoggedIn { get; private set; }
        public bool AutomaticallyConnectWebSocket = true;
        public long BaseWalletSolBalance;
        public long InGameWalletSolBalance;
        public WalletType walletType;

        public PhantomWallet DeeplinkWallet;
        public XNFTWallet xnftWallet;
        public InGameWallet InGameWallet;
        public bool IsDevNetLogin;

        private void Awake()
        {
            if (ServiceFactory.Resolve<WalletHolderService>() != null)
            {
                Destroy(gameObject);
                return;
            }

            ServiceFactory.RegisterSingleton(this);
        }


        public async Task<Account> Login(WalletType walletType,bool devNetLogin)
        {
            string rpcUrl = null;
            RpcCluster cluster = RpcCluster.DevNet;
            
            if (devNetLogin)
            {
                rpcUrl = DevnetLoginRPCUrl;
                cluster = DevnetWalletCluster;
            }
            else
            {
                rpcUrl = MainNetRpcUrl;
                cluster = MainnetWalletCluster;
            }

            DeeplinkWallet = new PhantomWallet(PhantomWalletOptions, cluster, rpcUrl, null, true);
            xnftWallet = new XNFTWallet(cluster, rpcUrl, null, true);
            InGameWallet = new InGameWallet(cluster, rpcUrl, null, true);

            IsDevNetLogin = devNetLogin;
            
            if (devNetLogin)
            {
                var newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);

                var account = await InGameWallet.Login("1234") ??
                              await InGameWallet.CreateAccount(newMnemonic.ToString(), "1234");

                BaseWallet = InGameWallet;

                // Copy this private key if you want to import your wallet into phantom. Dont share it with anyone.
                // var privateKeyString = account.PrivateKey.Key;
                double sol = await BaseWallet.GetBalance();

                if (sol < 0.8)
                {
                    await RequestAirdrop();
                }
            }
            else
            {
#if (UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL)
                switch (walletType)
                {
                    case WalletType.Phantom:
                        BaseWallet = DeeplinkWallet;
                        break;
                    case WalletType.Backpack:
                        BaseWallet = xnftWallet;
                        break;
                }
                //BaseWallet = DeeplinkWallet;
                Debug.Log(BaseWallet.ActiveRpcClient.NodeAddress);
                await BaseWallet.Login();
#endif
                var newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);

                var account = await InGameWallet.Login("1234") ??
                              await InGameWallet.CreateAccount(newMnemonic.ToString(), "1234");
            }

            IsLoggedIn = true;
            MessageRouter.RaiseMessage(new WalletLoggedInMessage()
            {
                Wallet = BaseWallet
            });

            if (AutomaticallyConnectWebSocket)
            {
                var solPlayWebSocketService = ServiceFactory.Resolve<SolPlayWebSocketService>();
                if (solPlayWebSocketService != null)
                { 
                    solPlayWebSocketService.Connect(BaseWallet.ActiveRpcClient.NodeAddress.ToString());
                }

                SubscribeToWalletAccountChanges();
            }

            var baseSolBalance = await BaseWallet.ActiveRpcClient.GetBalanceAsync(BaseWallet.Account.PublicKey, Commitment.Confirmed);
            BaseWalletSolBalance = (long) baseSolBalance.Result.Value;
            MessageRouter.RaiseMessage(new SolBalanceChangedMessage(BaseWalletSolBalance, false));
            
            var ingameSolBalance = await InGameWallet.ActiveRpcClient.GetBalanceAsync(InGameWallet.Account.PublicKey, Commitment.Confirmed);
            InGameWalletSolBalance = (long) ingameSolBalance.Result.Value;
            MessageRouter.RaiseMessage(new SolBalanceChangedMessage(InGameWalletSolBalance, true));

            Debug.Log("Logged in Base: " + BaseWallet.Account.PublicKey + " balance: " + baseSolBalance.Result.Value);
            Debug.Log("Logged in InGameWallet: " + InGameWallet.Account.PublicKey + " balance: " + ingameSolBalance.Result.Value);

            return BaseWallet.Account;
        }

        private void SubscribeToWalletAccountChanges()
        {
            //ServiceFactory.Resolve<SolPlayWebSocketService>().SubscribeToBlocks();
            if (IsDevNetLogin)
            {
                ServiceFactory.Resolve<SolPlayWebSocketService>().SubscribeToPubKeyData(BaseWallet.Account.PublicKey,
                    result =>
                    {
                        long balanceChange = result.result.value.lamports - BaseWalletSolBalance;
                        BaseWalletSolBalance = result.result.value.lamports;
                        InGameWalletSolBalance = result.result.value.lamports;
                        MessageRouter.RaiseMessage(
                            new SolBalanceChangedMessage((float) balanceChange / SolanaUtils.SolToLamports, true));
                        MessageRouter.RaiseMessage(new SolBalanceChangedMessage((float) balanceChange / SolanaUtils.SolToLamports));
                    });
            }
            else
            {
                ServiceFactory.Resolve<SolPlayWebSocketService>().SubscribeToPubKeyData(BaseWallet.Account.PublicKey,
                    result =>
                    {
                        long balanceChange = result.result.value.lamports - BaseWalletSolBalance;
                        BaseWalletSolBalance = result.result.value.lamports;
                        MessageRouter.RaiseMessage(new SolBalanceChangedMessage((float) balanceChange / SolanaUtils.SolToLamports));
                    });
                ServiceFactory.Resolve<SolPlayWebSocketService>().SubscribeToPubKeyData(InGameWallet.Account.PublicKey,
                    result =>
                    {
                        long balanceChange = result.result.value.lamports - InGameWalletSolBalance;
                        InGameWalletSolBalance = result.result.value.lamports;
                        MessageRouter.RaiseMessage(new SolBalanceChangedMessage((float) balanceChange / SolanaUtils.SolToLamports,
                            true));
                    });
            }
        }

        public async Task RequestAirdrop()
        {
            MessageRouter.RaiseMessage(new BlimpSystem.ShowLogMessage("Requesting airdrop"));
            RequestResult<string> result = await BaseWallet.ActiveRpcClient.RequestAirdropAsync(BaseWallet.Account.PublicKey, SolanaUtils.SolToLamports, Commitment.Confirmed);
            if (result.WasSuccessful)
            {
                ServiceFactory.Resolve<TransactionService>().CheckSignatureStatus(result.Result, b => {});
            }
            else
            {
                MessageRouter.RaiseMessage(new BlimpSystem.ShowLogMessage("Airdrop failed: " + result.ErrorData));
            }
        }

        public bool TryGetPhantomPublicKey(out string phantomPublicKey)
        {
            if (BaseWallet.Account == null)
            {
                phantomPublicKey = string.Empty;
                return false;
            }

            phantomPublicKey = BaseWallet.Account.PublicKey;
            return true;
        }

        public IEnumerator HandleNewSceneLoaded()
        {
            yield return null;
        }

        public bool HasEnoughSol(bool inGameWallet, long requiredLamports)
        {
            Debug.Log($"Checking sol balance {inGameWallet} for {requiredLamports}");
            Debug.Log($"Ingame {InGameWalletSolBalance} Base Wallet {BaseWalletSolBalance}");
            bool hasEnoughSol = false;
            if (inGameWallet)
            {
                hasEnoughSol = InGameWalletSolBalance >= requiredLamports;
            }
            else
            {
                hasEnoughSol = BaseWalletSolBalance >= requiredLamports;
            }

            if (!hasEnoughSol)
            {
                ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.InGameWalletPopup,
                    new InGameWalletPopupUiData(requiredLamports));
            }

            return hasEnoughSol;
        }
    }
}