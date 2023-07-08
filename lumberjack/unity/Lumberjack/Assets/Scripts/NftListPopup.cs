using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using SolPlay.Scripts.Services;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Scripts.Ui
{
    /// <summary>
    /// Screen that loads all NFTs when opened
    /// </summary>
    public class NftListPopup : BasePopup
    {
        public Button GetNFtsDataButton;
        public Button MintInAppButton;
        public NftItemListView NftItemListView;
        public GameObject YouDontOwnANftOfCollectionRoot;
        public GameObject YouOwnANftOfCollectionRoot;
        public GameObject LoadingSpinner;
        public GameObject MinitingBlocker;

        async void Start()
        {
            GetNFtsDataButton.onClick.AddListener(OnGetNftButtonClicked);
            MintInAppButton.onClick.AddListener(OnMintInAppButtonClicked);
            
            MessageRouter
                .AddHandler<NftLoadingStartedMessage>(OnNftLoadingStartedMessage);
            MessageRouter
                .AddHandler<NftLoadingFinishedMessage>(OnNftLoadingFinishedMessage);
            MessageRouter
                .AddHandler<NftLoadedMessage>(OnNftLoadedMessage);
            MessageRouter
                .AddHandler<NftMintFinishedMessage>(OnNftMintFinishedMessage);
            MessageRouter
                .AddHandler<NftSelectedMessage>(OnNftSelectedMessage);

            Web3.OnLogin += OnLogin;
        }

        private void OnNftSelectedMessage(NftSelectedMessage obj)
        {
            Close();
        }

        private void OnDestroy()
        {
            Web3.OnLogin -= OnLogin;
        }
        
        private async void OnLogin(Account account)
        {
            await OnLogin();
        }

        public override void Open(UiService.UiData uiData)
        {
            var nftListPopupUiData = (uiData as NftListPopupUiData);

            if (nftListPopupUiData == null)
            {
                Debug.LogError("Wrong ui data for nft list popup");
                return;
            }

            NftItemListView.UpdateContent();
            NftItemListView.SetData(nft =>
            {
                // when an nft was selected we want to close the popup so we can start the game.
                Close();
            });
            base.Open(uiData);
        }
        
        private async Task OnLogin()
        {
            await RequestNfts();
        }

        private async void OnMintInAppButtonClicked()
        {
            if (MinitingBlocker != null)
            {
                MinitingBlocker.gameObject.SetActive(true);
            }

            // Mint a pirate sship
            var signature = await ServiceFactory.Resolve<NftMintingService>()
                .MintNftWithMetaData(
                    "https://shdw-drive.genesysgo.net/QZNGUVnJgkw6sGQddwZVZkhyUWSUXAjXF9HQAjiVZ55/DummyPirateShipMetaData.json",
                    "Simple Pirate Ship", "Pirate", b =>
                    {
                        if (MinitingBlocker != null)
                        {
                            MinitingBlocker.gameObject.SetActive(false);
                        }
                    });
            Debug.Log("Mint signature: " + signature);
        }

        private void OnNftLoadedMessage(NftLoadedMessage message)
        {
            NftItemListView.AddNFt(message.Nft);
            UpdateOwnCollectionStatus();
        }

        private bool UpdateOwnCollectionStatus()
        {
            var nftService = ServiceFactory.Resolve<NftService>();
            bool ownsBeaver = nftService.OwnsNftOfMintAuthority(NftService.BeaverNftMintAuthority);
            YouDontOwnANftOfCollectionRoot.gameObject.SetActive(!ownsBeaver);
            YouOwnANftOfCollectionRoot.gameObject.SetActive(ownsBeaver);
            return ownsBeaver;
        }

        private async void OnGetNftButtonClicked()
        {
            await RequestNfts();
        }

        private void OnNftLoadingStartedMessage(NftLoadingStartedMessage message)
        {
            GetNFtsDataButton.interactable = false;
        }

        private void OnNftLoadingFinishedMessage(NftLoadingFinishedMessage message)
        {
            NftItemListView.UpdateContent();
        }

        private async void OnNftMintFinishedMessage(NftMintFinishedMessage message)
        {
            await RequestNfts();
        }

        private void Update()
        {
            var nftService = ServiceFactory.Resolve<NftService>();
            if (nftService != null)
            {
                GetNFtsDataButton.interactable = !nftService.IsLoadingTokenAccounts;
                LoadingSpinner.gameObject.SetActive(nftService.IsLoadingTokenAccounts);
            }
        }

        private async Task RequestNfts()
        {
            ServiceFactory.Resolve<NftService>().LoadNfts();
        }
    }
}