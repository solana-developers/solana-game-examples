using Frictionless;
using NativeWebSocket;
using SolHunter;
using SevenSeas.Accounts;
using Solana.Unity.SDK.Nft;
using SolPlay.Scripts.Services;
using SolPlay.Scripts.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyAdventure
{
    public class SolHunterScreen : MonoBehaviour
    {
        public Button GetDataButton;
        public Button MoveRightButton;
        public Button MoveLeftButton;   
        public Button MoveUpButton;
        public Button BoomButton;
        public Button MoveDownButton;
        public Button InitializeButton;
        public Button ResetButton;
        public Button SpawnPlayerAndChestButton;
        public Button PickAvatarButton;
        public Button OpenInGameWalletPopup;
        public Button OpenNftScreenButton;
        public Button UpgradeButton;
        public Button InitShipButton;
        public Button ChutuluhButton;
        public TextMeshProUGUI ShipLevel;

        public GameObject NoSelectedNftRoot;
        public GameObject NoPlayerSpawnedRoot;
        public GameObject GameRunningRoot;

        public SolHunterTile TilePrefab;
        public GameObject TilesRoot;
        public SolHunterTile[,] Tiles = new SolHunterTile[10, 10];
        public NftItemView AvatarNftItemView;
        public NftItemView GameAvatarNftItemView;
        
        private void Awake()
        {
            GetDataButton.onClick.AddListener(OnGetDataButtonClicked);
            MoveRightButton.onClick.AddListener(OnMoveRightButtonClicked);
            MoveLeftButton.onClick.AddListener(OnMoveLeftButtonClicked);
            MoveUpButton.onClick.AddListener(OnMoveUpButtonClicked);
            BoomButton.onClick.AddListener(OnBoomButtonClicked);
            ChutuluhButton.onClick.AddListener(OnChutuluhButtonClicked);
            MoveDownButton.onClick.AddListener(OnMoveDownButtonClicked);
            InitializeButton.onClick.AddListener(OnInitializeButtonClicked);
            ResetButton.onClick.AddListener(OnResetButtonClicked);
            SpawnPlayerAndChestButton.onClick.AddListener(OnSpawnPlayerButtonClicked);
            PickAvatarButton.onClick.AddListener(OnPickAvatarButtonClicked);
            OpenNftScreenButton.onClick.AddListener(OnPickAvatarButtonClicked);
            OpenInGameWalletPopup.onClick.AddListener(OnInGameWalletButtonClicked);
            UpgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
            InitShipButton.onClick.AddListener(OnInitShipButtonClicked);
        }

        private void Start()
        {
            MessageRouter.AddHandler<SolHunterService.SolHunterGameDataChangedMessage>(OnGameDataChangedMessage);
            MessageRouter.AddHandler<SolHunterService.SolHunterShipDataChangedMessage>(OnShipDataChangedMessage);
            MessageRouter.AddHandler<NftSelectedMessage>(OnNftSelectedMessage);
            MessageRouter.AddHandler<NftLoadedMessage>(OnNftJsonLoadedMessage);
            MessageRouter.AddHandler<NftLoadingFinishedMessage>(OnNftLoadingFinishedMessage);
            MessageRouter.AddHandler<WalletLoggedInMessage>(OnWalletLoggedInMessage);

            for (int x = 0; x < SolHunterService.TILE_COUNT_X; x++)
            {
                for (int y = 0; y < SolHunterService.TILE_COUNT_Y; y++)
                {
                    SolHunterTile solHunterTile = Instantiate(TilePrefab.gameObject, TilesRoot.transform)
                        .GetComponent<SolHunterTile>();
                    Tiles[x, y] = solHunterTile;
                }
            }

            UpdateContent();
        }

        private void OnShipDataChangedMessage(SolHunterService.SolHunterShipDataChangedMessage message)
        {
            UpgradeButton.gameObject.SetActive(message != null);
            InitShipButton.gameObject.SetActive(message == null);
            ShipLevel.text = "Level: " + message.Ship.Upgrades;
        }

        private async void OnWalletLoggedInMessage(WalletLoggedInMessage message)
        {
            var res = await ServiceFactory.Resolve<SolHunterService>().GetGameData();
            if (res == null)
            {
                InitializeButton.gameObject.SetActive(true);
            }
        }

        private void OnNftLoadingFinishedMessage(NftLoadingFinishedMessage message)
        {
            UpdateContent();
        }

        private void OnNftJsonLoadedMessage(NftLoadedMessage message)
        {
            var solHunterService = ServiceFactory.Resolve<SolHunterService>();
            var nftService = ServiceFactory.Resolve<NftService>();
            var playerAvatar = solHunterService.TryGetSpawnedPlayerAvatar();
            Nft solPlayNft = nftService.GetNftByMintAddress(playerAvatar);
            if (solPlayNft != null && message.Nft == solPlayNft)
            {
                UpdateContent();
            }
        }

        private void UpdateContent()
        {
            if (ServiceFactory.Resolve<SolPlayWebSocketService>().GetState() != WebSocketState.Open)
            {
                GameRunningRoot.gameObject.SetActive(false);
                NoPlayerSpawnedRoot.gameObject.SetActive(false);
                NoSelectedNftRoot.gameObject.SetActive(false);
                return;
            }

            var solHunterService = ServiceFactory.Resolve<SolHunterService>();
            if (solHunterService.IsPlayerSpawned())
            {
                SetNftGraphic();
                GameRunningRoot.gameObject.SetActive(true);
                NoPlayerSpawnedRoot.gameObject.SetActive(false);
                NoSelectedNftRoot.gameObject.SetActive(false);
                return;
            }

            var selectedNft = ServiceFactory.Resolve<NftService>().SelectedNft;
            if (selectedNft == null)
            {
                GameRunningRoot.gameObject.SetActive(false);
                NoPlayerSpawnedRoot.gameObject.SetActive(false);
                NoSelectedNftRoot.gameObject.SetActive(true);
                return;
            }

            if (!solHunterService.IsPlayerSpawned())
            {
                GameRunningRoot.gameObject.SetActive(false);
                NoPlayerSpawnedRoot.gameObject.SetActive(true);
                NoSelectedNftRoot.gameObject.SetActive(false);
                return;
            }

            NoPlayerSpawnedRoot.gameObject.SetActive(false);
            NoSelectedNftRoot.gameObject.SetActive(false);
            GameRunningRoot.gameObject.SetActive(true);
            SetNftGraphic();
        }

        private void OnInGameWalletButtonClicked()
        {
            ServiceFactory.Resolve<UiService>()
                .OpenPopup(UiService.ScreenType.InGameWalletPopup, new InGameWalletPopupUiData(0));
        }


        private async void OnUpgradeButtonClicked()
        {
            var ship = await ServiceFactory.Resolve<SolHunterService>().GetShipData();

            if (ship != null)
            {
                ServiceFactory.Resolve<SolHunterService>().UpgradeShip();
                Debug.Log("Ship level " + ship.Level);    
            }
            else
            {
                Debug.Log("Player has no ship yet");
                ServiceFactory.Resolve<SolHunterService>().InitShip();
            }
        }
        
        private void OnInitShipButtonClicked()
        {
            ServiceFactory.Resolve<SolHunterService>().InitShip();
        }
        
        private async void OnNftSelectedMessage(NftSelectedMessage message)
        {
            SetNftGraphic();
            UpdateContent();
            await ServiceFactory.Resolve<SolHunterService>().GetShipData();
        }

        private void SetNftGraphic()
        {
            var solHunterService = ServiceFactory.Resolve<SolHunterService>();
            var nftService = ServiceFactory.Resolve<NftService>();

            var playerAvatar = solHunterService.TryGetSpawnedPlayerAvatar();
            Nft solPlayNft = nftService.GetNftByMintAddress(playerAvatar);
            if (solPlayNft == null)
            {
                solPlayNft = nftService.SelectedNft;
            }

            if (solPlayNft == null)
            {
                if (playerAvatar == null)
                {
                    Debug.Log("Players doesnt have the nft anymore. And avatar is null");
                }
                else
                {
                    Debug.Log("Players doesnt have the nft anymore:" + playerAvatar);
                }
                
                return;
            }

            AvatarNftItemView.gameObject.SetActive(true);
            AvatarNftItemView.SetData(solPlayNft, view =>
            {
                // Nothing on click
            });
            GameAvatarNftItemView.SetData(solPlayNft, view =>
            {
                // Nothing on click
            });
        }

        private void OnPickAvatarButtonClicked()
        {
            var baseWallet = ServiceFactory.Resolve<WalletHolderService>().BaseWallet;
            ServiceFactory.Resolve<UiService>()
                .OpenPopup(UiService.ScreenType.NftListPopup, new NftListPopupUiData(false, baseWallet));
        }

        private void OnGameDataChangedMessage(SolHunterService.SolHunterGameDataChangedMessage message)
        {
            UpdateGameDataView(message.GameDataAccount);
            UpdateContent();
        }

        private void OnInitializeButtonClicked()
        {
            ServiceFactory.Resolve<SolHunterService>().Initialize();
        }

        private void OnResetButtonClicked()
        {
            ServiceFactory.Resolve<SolHunterService>().Reset();
        }

        private void OnSpawnPlayerButtonClicked()
        {
            ServiceFactory.Resolve<SolHunterService>().SpawnPlayerAndChest();
        }

        private void OnMoveUpButtonClicked()
        {
            ServiceFactory.Resolve<SolHunterService>().Move(SolHunterService.Direction.Up);
        }

        private void OnBoomButtonClicked()
        {
            ServiceFactory.Resolve<SolHunterService>().Shoot();
        }

        private void OnChutuluhButtonClicked()
        {
            ServiceFactory.Resolve<SolHunterService>().Chutuluh();
        }

        private void OnMoveRightButtonClicked()
        {
            ServiceFactory.Resolve<SolHunterService>().Move(SolHunterService.Direction.Right);
        }

        private void OnMoveDownButtonClicked()
        {
            ServiceFactory.Resolve<SolHunterService>().Move(SolHunterService.Direction.Down);
        }

        private void OnMoveLeftButtonClicked()
        {
            ServiceFactory.Resolve<SolHunterService>().Move(SolHunterService.Direction.Left);
        }

        private async void OnGetDataButtonClicked()
        {
            GameDataAccount gameData = await ServiceFactory.Resolve<SolHunterService>().GetGameData();
            UpdateGameDataView(gameData);
        }

        private void UpdateGameDataView(GameDataAccount gameData)
        {
            var length = gameData.Board.GetLength(0);
            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    var tile = gameData.Board[y][x];
                    Tiles[x, y].SetData(tile);
                }
            }
        }
    }
}