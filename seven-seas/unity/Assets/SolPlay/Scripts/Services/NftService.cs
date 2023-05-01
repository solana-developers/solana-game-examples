using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Frictionless;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.SDK.Nft;
using Solana.Unity.Wallet;
using SolPlay.Scripts.Ui;
using UnityEngine;

namespace SolPlay.Scripts.Services
{
    /// <summary>
    /// Handles all logic related to NFTs and calculating their power level or whatever you like to do with the NFTs
    /// </summary>
    public class NftService : MonoBehaviour, IMultiSceneSingleton
    {
        public List<SolPlayNft> MetaPlexNFts = new List<SolPlayNft>();
        public int NftImageSize = 75;
        public float RateLimitTimeBetweenNftLoads = 0.1f;
        public bool IsLoadingTokenAccounts { get; private set; }
        public const string BeaverNftMintAuthority = "GsfNSuZFrT2r4xzSndnCSs9tTXwt47etPqU8yFVnDcXd";
        public SolPlayNft SelectedNft { get; private set; }
        public Texture2D LocalDummyNft;
        public bool LoadNftsOnStartUp = true;
        public bool AddDummyNft = true;

        private const string IgnoredTokenListPlayerPrefsKey = "IgnoredTokenList";

        public void Awake()
        {
            if (ServiceFactory.Resolve<NftService>() != null)
            {
                Destroy(gameObject);
                return;
            }

            ServiceFactory.RegisterSingleton(this);
        }

        private async void Start()
        {
            if (LoadNftsOnStartUp)
            {
                if (ServiceFactory.Resolve<WalletHolderService>().IsLoggedIn)
                {
                    var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
                    await RequestNfts(walletHolderService.BaseWallet);
                }
                else
                {
                    MessageRouter.AddHandler<WalletLoggedInMessage>(OnWalletLoggedInMessage);
                }
            }
        }

        private async Task RequestNfts(WalletBase wallet)
        {
            await RequestNftsFromWallet(wallet);
        }

        private async void OnWalletLoggedInMessage(WalletLoggedInMessage message)
        {
            await RequestNfts(message.Wallet);
        }

        public async UniTask RequestNftsFromWallet(WalletBase wallet, bool tryUseLocalContent = true)
        {
            if (IsLoadingTokenAccounts)
            {
                MessageRouter
                    .RaiseMessage(new BlimpSystem.ShowLogMessage("Loading in progress."));
                return;
            }

            MessageRouter.RaiseMessage(new NftLoadingStartedMessage());

            IsLoadingTokenAccounts = true;

            TokenAccount[] tokenAccounts = await GetOwnedTokenAccounts(wallet.Account.PublicKey);

            if (tokenAccounts == null)
            {
                string error = $"Could not load Token Accounts, are you connected to the internet?";
                MessageRouter.RaiseMessage(new BlimpSystem.ShowLogMessage(error));
                IsLoadingTokenAccounts = false;
                return;
            }

            MetaPlexNFts.Clear();

            if (AddDummyNft)
            {
                var dummyLocalNft = CreateDummyLocalNft(wallet.Account.PublicKey);
                MetaPlexNFts.Add(dummyLocalNft);
            }

            string result = $"{tokenAccounts.Length} token accounts loaded. Getting data now.";
            MessageRouter.RaiseMessage(new BlimpSystem.ShowLogMessage(result));

            List<UniTask> loadingTasks = new List<UniTask>();

            int counter = 0;
            foreach (TokenAccount tokenAccount in tokenAccounts)
            {
                if (float.Parse(tokenAccount.Account.Data.Parsed.Info.TokenAmount.Amount) > 0)
                {
                    SolPlayNft solPlayNft = SolPlayNft.TryLoadNftFromLocal(tokenAccount.Account.Data.Parsed.Info.Mint);
                    if (solPlayNft != null && tryUseLocalContent)
                    {
                        solPlayNft.TokenAccount = tokenAccount;
                        MetaPlexNFts.Add(solPlayNft);
                    }
                    else
                    {
                        // We put broken tokens on an ignore list so we dont need to load the information every time. 
                        if (IsTokenMintIgnored(tokenAccount.Account.Data.Parsed.Info.Mint))
                        {
                            continue;
                        }

                        solPlayNft = new SolPlayNft(tokenAccount);

                        loadingTasks.Add(solPlayNft.LoadData(solPlayNft.TokenAccount.Account.Data.Parsed.Info.Mint,
                            wallet.ActiveRpcClient));

                        await UniTask.Delay(TimeSpan.FromSeconds(RateLimitTimeBetweenNftLoads), ignoreTimeScale: false);

                        // We need to do this because of rate limits
                        //StartCoroutine(StartNftLoadingDelayed(solPlayNft, wallet.ActiveRpcClient, counter));
                        counter++;
                    }
                }
            }

            await UniTask.WhenAll(loadingTasks);

            foreach (var nft in MetaPlexNFts)
            {
                var lastSelectedNft = GetSelectedNftPubKey();
                if (!string.IsNullOrEmpty(lastSelectedNft) && nft.TokenAccount != null &&
                    lastSelectedNft == nft.TokenAccount.PublicKey)
                {
                    SelectNft(nft);
                }
            }

            MessageRouter.RaiseMessage(new NftLoadingFinishedMessage());
            IsLoadingTokenAccounts = false;
        }

        public static async Task DelayAsync(float secondsDelay)
        {
            float startTime = Time.time;
            while (Time.time < startTime + secondsDelay) await Task.Yield();
        }

        private static bool IsTokenMintIgnored(string mint)
        {
            if (GetIgnoreTokenList().TokenList.Contains(mint))
            {
                return true;
            }

            return false;
        }

        private static IgnoreTokenList GetIgnoreTokenList()
        {
            if (!PlayerPrefs.HasKey(IgnoredTokenListPlayerPrefsKey))
            {
                PlayerPrefs.SetString(IgnoredTokenListPlayerPrefsKey, JsonUtility.ToJson(new IgnoreTokenList()));
            }

            var json = PlayerPrefs.GetString(IgnoredTokenListPlayerPrefsKey);
            var ignoreTokenList = JsonUtility.FromJson<IgnoreTokenList>(json);
            return ignoreTokenList;
        }

        public void AddToIgnoredTokenListAndSave(string mint)
        {
            string blimpMessage = $"Added {mint} to ignore list.";
            LoggingService.Log(blimpMessage, false);

            var ignoreTokenList = GetIgnoreTokenList();
            ignoreTokenList.TokenList.Add(mint);
            PlayerPrefs.SetString(IgnoredTokenListPlayerPrefsKey, JsonUtility.ToJson(ignoreTokenList));
            PlayerPrefs.Save();
        }

        private IEnumerator StartNftLoadingDelayed(SolPlayNft nft, IRpcClient connection, int counter)
        {
            yield return new WaitForSeconds(counter * 0.4f);
        }

        public SolPlayNft CreateDummyLocalNft(string publicKey)
        {
            SolPlayNft dummyLocalNft = new SolPlayNft();
            dummyLocalNft.TokenAccount = new TokenAccount();
            dummyLocalNft.TokenAccount.PublicKey = publicKey;
            dummyLocalNft.MetaplexData = new Metaplex();
            dummyLocalNft.MetaplexData.nftImage = new NftImage()
            {
                name = "DummyNft",
                file = LocalDummyNft
            };
            dummyLocalNft.MetaplexData.mint = publicKey;
            dummyLocalNft.MetaplexData.data = new MetaplexData();
            dummyLocalNft.MetaplexData.data.symbol = "dummy";
            dummyLocalNft.MetaplexData.data.name = "Dummy Nft";
            dummyLocalNft.MetaplexData.data.json = new MetaplexJsonData();
            dummyLocalNft.MetaplexData.data.json.name = "Dummy nft";
            dummyLocalNft.MetaplexData.data.json.description = "A dummy nft which uses the wallet puy key";
            return dummyLocalNft;
        }

        public bool IsNftSelected(SolPlayNft nft)
        {
            if (nft.TokenAccount == null)
            {
                return false;
            }

            return nft.TokenAccount.PublicKey == GetSelectedNftPubKey();
        }

        private string GetSelectedNftPubKey()
        {
            return PlayerPrefs.GetString("SelectedNft");
        }

        private async Task<TokenAccount[]> GetOwnedTokenAccounts(string publicKey)
        {
            var wallet = ServiceFactory.Resolve<WalletHolderService>().BaseWallet;
            try
            {
                RequestResult<ResponseValue<List<TokenAccount>>> result =
                    await wallet.ActiveRpcClient.GetTokenAccountsByOwnerAsync(publicKey, null,
                        TokenProgram.ProgramIdKey, Commitment.Confirmed);

                if (result.Result != null && result.Result.Value != null)
                {
                    return result.Result.Value.ToArray();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log($"Token loading error: {ex}", true);
                Debug.LogError(ex);
                IsLoadingTokenAccounts = false;
            }

            return null;
        }

        public bool OwnsNftOfMintAuthority(string authority)
        {
            foreach (var nft in MetaPlexNFts)
            {
                if (nft.MetaplexData.authority == authority)
                {
                    return true;
                }
            }

            return false;
        }

        public List<SolPlayNft> GetAllNftsByMintAuthority(string mintAuthority)
        {
            List<SolPlayNft> result = new List<SolPlayNft>();
            foreach (var nftData in MetaPlexNFts)
            {
                if (nftData.MetaplexData.authority != mintAuthority)
                {
                    continue;
                }

                result.Add(nftData);
            }

            return result;
        }

        public bool IsBeaverNft(SolPlayNft solPlayNft)
        {
            return solPlayNft.MetaplexData.authority == BeaverNftMintAuthority;
        }

        public void BurnNft(SolPlayNft currentNft)
        {
            ServiceFactory.Resolve<MetaPlexInteractionService>().BurnNFt(currentNft);
        }

        public void SelectNft(SolPlayNft nft)
        {
            if (nft == null)
            {
                return;
            }

            SelectedNft = nft;
            PlayerPrefs.SetString("SelectedNft", SelectedNft.TokenAccount.PublicKey);
            MessageRouter.RaiseMessage(new NftSelectedMessage(SelectedNft));
        }

        public void ResetSelectedNft()
        {
            SelectedNft = null;
            PlayerPrefs.DeleteKey("SelectedNft");
            MessageRouter.RaiseMessage(new NftSelectedMessage(SelectedNft));
        }

        public IEnumerator HandleNewSceneLoaded()
        {
            yield return null;
        }

        public SolPlayNft GetNftByMintAddress(PublicKey nftMintAddress)
        {
            if (nftMintAddress == null)
            {
                return null;
            }

            foreach (var nft in MetaPlexNFts)
            {
                if (nftMintAddress == nft.MetaplexData.mint)
                {
                    return nft;
                }
            }

            return null;
        }
    }

    public class NftImageLoadedMessage
    {
        public SolPlayNft Nft;

        public NftImageLoadedMessage(SolPlayNft nft)
        {
            Nft = nft;
        }
    }

    public class NftJsonLoadedMessage
    {
        public SolPlayNft Nft;

        public NftJsonLoadedMessage(SolPlayNft nft)
        {
            Nft = nft;
        }
    }

    public class NftSelectedMessage
    {
        public SolPlayNft NewNFt;

        public NftSelectedMessage(SolPlayNft newNFt)
        {
            NewNFt = newNFt;
        }
    }

    public class NftLoadingStartedMessage
    {
    }

    public class NftLoadingFinishedMessage
    {
    }

    public class TokenValueChangedMessage
    {
    }

    public class NftMintFinishedMessage
    {
    }
}