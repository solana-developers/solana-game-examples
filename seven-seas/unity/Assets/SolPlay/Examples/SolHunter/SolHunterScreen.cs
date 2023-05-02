using Frictionless;
using NativeWebSocket;
using SolHunter;
using SevenSeas.Accounts;
using SolPlay.Scripts;
using SolPlay.Scripts.Services;
using SolPlay.Scripts.Ui;
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
            MoveDownButton.onClick.AddListener(OnMoveDownButtonClicked);
            InitializeButton.onClick.AddListener(OnInitializeButtonClicked);
            ResetButton.onClick.AddListener(OnResetButtonClicked);
            SpawnPlayerAndChestButton.onClick.AddListener(OnSpawnPlayerButtonClicked);
            PickAvatarButton.onClick.AddListener(OnPickAvatarButtonClicked);
            OpenNftScreenButton.onClick.AddListener(OnPickAvatarButtonClicked);
            OpenInGameWalletPopup.onClick.AddListener(OnInGameWalletButtonClicked);
        }

        private void Start()
        {
            MessageRouter.AddHandler<SolHunterService.SolHunterGameDataChangedMessage>(OnGameDataChangedMessage);
            MessageRouter.AddHandler<NftSelectedMessage>(OnNftSelectedMessage);
            MessageRouter.AddHandler<NftJsonLoadedMessage>(OnNftJsonLoadedMessage);
            MessageRouter.AddHandler<NftLoadingFinishedMessage>(OnNftLoadingFinishedMessage);

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

        private void OnNftLoadingFinishedMessage(NftLoadingFinishedMessage message)
        {
            UpdateContent();
        }

        private void OnNftJsonLoadedMessage(NftJsonLoadedMessage message)
        {
            var solHunterService = ServiceFactory.Resolve<SolHunterService>();
            var nftService = ServiceFactory.Resolve<NftService>();
            var playerAvatar = solHunterService.TryGetSpawnedPlayerAvatar();
            SolPlayNft solPlayNft = nftService.GetNftByMintAddress(playerAvatar);
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

        private void OnNftSelectedMessage(NftSelectedMessage message)
        {
            SetNftGraphic();
            UpdateContent();
        }

        private void SetNftGraphic()
        {
            var solHunterService = ServiceFactory.Resolve<SolHunterService>();
            var nftService = ServiceFactory.Resolve<NftService>();

            var playerAvatar = solHunterService.TryGetSpawnedPlayerAvatar();
            SolPlayNft solPlayNft = nftService.GetNftByMintAddress(playerAvatar);
            if (solPlayNft == null)
            {
                solPlayNft = nftService.SelectedNft;
            }

            if (solPlayNft == null)
            {
                Debug.Log("Players doesnt have the nft anymore:" + playerAvatar);
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