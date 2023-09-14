using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DefaultNamespace;
using Frictionless;
using Lumberjack;
using Lumberjack.Accounts;
using Lumberjack.Program;
using Lumberjack.Types;
using Solana.Unity.Programs;
using Solana.Unity.Programs.Models;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using SolPlay.Scripts.Services;
using UnityEngine;

public class LumberjackService : MonoBehaviour
{
    public PublicKey LumberjackProgramIdPubKey = new PublicKey("HsT4yX959Qh1vis8fEqoQdgrHEJuKvaWGtHoPcTjk4mJ");
    
    public const int TIME_TO_REFILL_ENERGY = 60;
    public const int MAX_ENERGY = 10;
    
    public const byte BUILDING_TYPE_TREE = 0;
    public const byte BUILDING_TYPE_EMPTY = 1;
    public const byte BUILDING_TYPE_SAWMILL = 2;
    public const byte BUILDING_TYPE_MINE = 3;
    public const byte BUILDING_TYPE_GOOD = 4;
    public const byte BUILDING_TYPE_EVIL = 5;
    
    public static LumberjackService Instance { get; private set; }
    public static Action<PlayerData> OnPlayerDataChanged;
    public static Action<BoardAccount> OnBoardDataChanged;
    public static Action<GameActionHistory> OnGameActionHistoryChanged;
    public static Action OnInitialDataLoaded;
    public bool IsAnyTransactionInProgress => transactionsInProgress > 0;
    public PlayerData CurrentPlayerData;
    public BoardAccount CurrentBoardAccount;
    public GameActionHistory CurrentGameActionHistory;

    private SessionWallet sessionWallet;
    private PublicKey PlayerDataPDA;
    private PublicKey BoardPDA;
    private PublicKey GameActionsPDA;
    private bool _isInitialized;
    private LumberjackClient lumberjackClient;
    private int transactionsInProgress;
    
    private void Awake() 
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(this); 
        } 
        else 
        { 
            Instance = this; 
        }

        Web3.OnLogin += OnLogin;
    }

    private void OnDestroy()
    {
        Web3.OnLogin -= OnLogin;
    }

    private async void OnLogin(Account account)
    {
        var solBalance = await Web3.Instance.WalletBase.GetBalance(Commitment.Confirmed);
        if (solBalance < 20000)
        {
            Debug.Log("Not enough sol. Requsting airdrop");
            var result = await Web3.Instance.WalletBase.RequestAirdrop(commitment: Commitment.Confirmed);
            if (!result.WasSuccessful)
            {
                Debug.Log("Airdrop failed.");
            }
        }

        PublicKey.TryFindProgramAddress(new[]
                {Encoding.UTF8.GetBytes("player"), account.PublicKey.KeyBytes},
            LumberjackProgramIdPubKey, out PlayerDataPDA, out byte bump);

        PublicKey.TryFindProgramAddress(new[]
                {Encoding.UTF8.GetBytes("board")},
            LumberjackProgramIdPubKey, out BoardPDA, out byte bump2);

        PublicKey.TryFindProgramAddress(new[]
                {Encoding.UTF8.GetBytes("gameActions")},
            LumberjackProgramIdPubKey, out GameActionsPDA, out byte bump3);

        lumberjackClient = new LumberjackClient(Web3.Rpc, Web3.WsRpc, LumberjackProgramIdPubKey);
        ServiceFactory.Resolve<SolPlayWebSocketService>().Connect(Web3.WsRpc.NodeAddress.AbsoluteUri);
        await SubscribeToPlayerDataUpdates();

        sessionWallet = await SessionWallet.GetSessionWallet(LumberjackProgramIdPubKey, "ingame");
        OnInitialDataLoaded?.Invoke();
    }

    public bool IsInitialized()
    {
        return _isInitialized;
    }

    private async Task SubscribeToPlayerDataUpdates()
    {
        AccountResultWrapper<PlayerData> playerData = null;
        
        try
        {
            playerData = await lumberjackClient.GetPlayerDataAsync(PlayerDataPDA, Commitment.Confirmed);
            if (playerData.ParsedResult != null)
            {
                CurrentPlayerData = playerData.ParsedResult;
                OnPlayerDataChanged?.Invoke(playerData.ParsedResult);
            }
            
            _isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.Log("Probably playerData not available " + e.Message);
        }

        AccountResultWrapper<BoardAccount> boardAccount = null;

        try
        {
            boardAccount = await lumberjackClient.GetBoardAccountAsync(BoardPDA, Commitment.Confirmed);
            if (boardAccount.ParsedResult != null)
            {
                CurrentBoardAccount = boardAccount.ParsedResult;
                OnBoardDataChanged?.Invoke(boardAccount.ParsedResult);
            }
            
        }
        catch (Exception e)
        {
            Debug.Log("Probably playerData not available " + e.Message);
        }

        AccountResultWrapper<GameActionHistory> gameActionHistroy = null;

        try
        {
            gameActionHistroy = await lumberjackClient.GetGameActionHistoryAsync(GameActionsPDA, Commitment.Confirmed);
            if (gameActionHistroy.ParsedResult != null)
            {
                CurrentGameActionHistory = gameActionHistroy.ParsedResult;
                OnGameActionHistoryChanged?.Invoke(gameActionHistroy.ParsedResult);
            }
            
        }
        catch (Exception e)
        {
            Debug.Log("gameActionHistroy not available " + e.Message);
        }

        ServiceFactory.Resolve<SolPlayWebSocketService>().SubscribeToPubKeyData(PlayerDataPDA, result =>
        {
            var playerData = PlayerData.Deserialize(Convert.FromBase64String(result.result.value.data[0]));
            Debug.Log("Player data socket " + playerData.Energy + " energy");
            CurrentPlayerData = playerData;
            OnPlayerDataChanged?.Invoke(playerData);
        });
        
        ServiceFactory.Resolve<SolPlayWebSocketService>().SubscribeToPubKeyData(BoardPDA, result =>
        {
            var boardAccount = BoardAccount.Deserialize(Convert.FromBase64String(result.result.value.data[0]));
            Debug.Log("Player data socket " + boardAccount.Wood + " wood");
            CurrentBoardAccount = boardAccount;
            OnBoardDataChanged?.Invoke(boardAccount);
        });

        ServiceFactory.Resolve<SolPlayWebSocketService>().SubscribeToPubKeyData(GameActionsPDA, result =>
        {
            var gameActionHistory = GameActionHistory.Deserialize(Convert.FromBase64String(result.result.value.data[0]));
            Debug.Log("GameActions data socket new game action: " + gameActionHistory.GameActions[0].ActionType + " by " + gameActionHistory.GameActions[0].Player + " is collectable: " + IsCollectable(gameActionHistory.GameActions[0].Tile));
            CurrentGameActionHistory = gameActionHistory;
            OnGameActionHistoryChanged?.Invoke(gameActionHistory);
        });
    }

    public async Task<RequestResult<string>> InitGameDataAccount(bool useSession)
    {
        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash()
        };

        InitPlayerAccounts accounts = new InitPlayerAccounts();
        accounts.Player = PlayerDataPDA;
        accounts.Board = BoardPDA;
        accounts.GameActions = GameActionsPDA;
        accounts.Signer = Web3.Account;
        accounts.SystemProgram = SystemProgram.ProgramIdKey;

        var initTx = LumberjackProgram.InitPlayer(accounts, LumberjackProgramIdPubKey);
        tx.Add(initTx);

        if (useSession)
        {
            if (!(await sessionWallet.IsSessionTokenInitialized()))
            {
                var topUp = true;

                var validity = DateTimeOffset.UtcNow.AddHours(23).ToUnixTimeSeconds();
                var createSessionIX = sessionWallet.CreateSessionIX(topUp, validity);
                accounts.Signer = Web3.Account.PublicKey;
                tx.Add(createSessionIX);
                Debug.Log("Has no session -> partial sign");
                tx.PartialSign(new[] { Web3.Account, sessionWallet.Account });
            }
        }

        var initResult =  await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
        Debug.Log(initResult.RawRpcResponse);
        await Web3.Rpc.ConfirmTransaction(initResult.Result, Commitment.Confirmed);
        await SubscribeToPlayerDataUpdates();
        return initResult;
    }

    public async Task<RequestResult<string>> RestartGame()
    {
        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash()
        };

        RestartGameAccounts accounts = new RestartGameAccounts();
        accounts.Board = BoardPDA;
        accounts.GameActions = GameActionsPDA;
        accounts.Signer = Web3.Account;
        accounts.SystemProgram = SystemProgram.ProgramIdKey;

        var initTx = LumberjackProgram.RestartGame(accounts, LumberjackProgramIdPubKey);
        tx.Add(initTx);

        var initResult =  await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
        Debug.Log(initResult.RawRpcResponse);
        await Web3.Rpc.ConfirmTransaction(initResult.Result, Commitment.Confirmed);
        await SubscribeToPlayerDataUpdates();
        return initResult;
    }
    
    public async Task<SessionWallet> RevokeSession()
    {
        await sessionWallet.PrepareLogout();
        sessionWallet.Logout();
        return sessionWallet;
    }

    public async void ChopTree(bool useSession, byte x, byte y)
    {
        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash(maxSeconds:1)
        };

        ChopTreeAccounts accounts = new ChopTreeAccounts();
        accounts.Player = PlayerDataPDA;
        accounts.Board = BoardPDA;
        accounts.GameActions = GameActionsPDA;
        accounts.Avatar = GetAvatar();
        if (useSession)
        {
            if (!(await sessionWallet.IsSessionTokenInitialized()))
            {
                var topUp = true;

                var validity = DateTimeOffset.UtcNow.AddHours(23).ToUnixTimeSeconds();
                var createSessionIX = sessionWallet.CreateSessionIX(topUp, validity);
                accounts.Signer = Web3.Account.PublicKey;
                tx.Add(createSessionIX);
                var chopInstruction = LumberjackProgram.ChopTree(accounts, x, y, LumberjackProgramIdPubKey);
                tx.Add(chopInstruction);
                Debug.Log("Has no session -> partial sign");
                tx.PartialSign(new[] { Web3.Account, sessionWallet.Account });
                SendAndConfirmTransaction(Web3.Wallet, tx, "Chop Tree and init session");
            }
            else
            {
                tx.FeePayer = sessionWallet.Account.PublicKey;
                accounts.SessionToken = sessionWallet.SessionTokenPDA;
                accounts.Signer = sessionWallet.Account.PublicKey;
                Debug.Log("Has session -> sign and send session wallet");
                var chopInstruction = LumberjackProgram.ChopTree(accounts, x, y, LumberjackProgramIdPubKey);
                tx.Add(chopInstruction);
                SendAndConfirmTransaction(sessionWallet, tx, "Chop Tree");
            }
        }
        else
        {
            tx.FeePayer = Web3.Account.PublicKey;
            accounts.Signer = Web3.Account.PublicKey;
            var chopInstruction = LumberjackProgram.ChopTree(accounts, x, y, LumberjackProgramIdPubKey);
            tx.Add(chopInstruction);
            Debug.Log("Sign without session");
            SendAndConfirmTransaction(Web3.Wallet, tx, "Chop Tree without session");
        }
    }
    
    public async void Build(bool useSession, byte x, byte y, byte buildingType)
    {
        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash(maxSeconds:1)
        };

        BuildAccounts accounts = new BuildAccounts();
        accounts.Player = PlayerDataPDA;
        accounts.Board = BoardPDA;
        accounts.GameActions = GameActionsPDA;
        accounts.Avatar = GetAvatar();
        if (useSession)
        {
            if (!(await sessionWallet.IsSessionTokenInitialized()))
            {
                var topUp = true;

                var validity = DateTimeOffset.UtcNow.AddHours(23).ToUnixTimeSeconds();
                var createSessionIX = sessionWallet.CreateSessionIX(topUp, validity);
                accounts.Signer = Web3.Account.PublicKey;
                tx.Add(createSessionIX);
                var chopInstruction = LumberjackProgram.Build(accounts, x, y, buildingType, LumberjackProgramIdPubKey);
                tx.Add(chopInstruction);
                Debug.Log("Has no session -> partial sign");
                tx.PartialSign(new[] { Web3.Account, sessionWallet.Account });
                SendAndConfirmTransaction(Web3.Wallet, tx, "Build and init session");
            }
            else
            {
                tx.FeePayer = sessionWallet.Account.PublicKey;
                accounts.SessionToken = sessionWallet.SessionTokenPDA;
                accounts.Signer = sessionWallet.Account.PublicKey;
                Debug.Log("Has session -> sign and send session wallet");
                var chopInstruction = LumberjackProgram.Build(accounts, x, y, buildingType, LumberjackProgramIdPubKey);
                tx.Add(chopInstruction);
                SendAndConfirmTransaction(sessionWallet, tx, "Build");
            }
        }
        else
        {
            tx.FeePayer = Web3.Account.PublicKey;
            accounts.Signer = Web3.Account.PublicKey;
            var chopInstruction = LumberjackProgram.Build(accounts, x, y, buildingType, LumberjackProgramIdPubKey);
            tx.Add(chopInstruction);
            Debug.Log("Sign without session");
            SendAndConfirmTransaction(Web3.Wallet, tx, "Build without session");
        }
    }
    
    public async void Upgrade(bool useSession, byte x, byte y)
    {
        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash(maxSeconds:1)
        };

        UpgradeAccounts accounts = new UpgradeAccounts();
        accounts.Player = PlayerDataPDA;
        accounts.Board = BoardPDA;
        accounts.GameActions = GameActionsPDA;
        accounts.Avatar = GetAvatar();
        if (useSession)
        {
            if (!(await sessionWallet.IsSessionTokenInitialized()))
            {
                var topUp = true;

                var validity = DateTimeOffset.UtcNow.AddHours(23).ToUnixTimeSeconds();
                var createSessionIX = sessionWallet.CreateSessionIX(topUp, validity);
                accounts.Signer = Web3.Account.PublicKey;
                tx.Add(createSessionIX);
                var chopInstruction = LumberjackProgram.Upgrade(accounts, x, y, LumberjackProgramIdPubKey);
                tx.Add(chopInstruction);
                Debug.Log("Has no session -> partial sign");
                tx.PartialSign(new[] { Web3.Account, sessionWallet.Account });
                SendAndConfirmTransaction(Web3.Wallet, tx, "Upgrade and init session");
            }
            else
            {
                tx.FeePayer = sessionWallet.Account.PublicKey;
                accounts.SessionToken = sessionWallet.SessionTokenPDA;
                accounts.Signer = sessionWallet.Account.PublicKey;
                Debug.Log("Has session -> sign and send session wallet");
                var chopInstruction = LumberjackProgram.Upgrade(accounts, x, y, LumberjackProgramIdPubKey);
                tx.Add(chopInstruction);
                SendAndConfirmTransaction(sessionWallet, tx, "Upgrade");
            }
        }
        else
        {
            tx.FeePayer = Web3.Account.PublicKey;
            accounts.Signer = Web3.Account.PublicKey;
            var chopInstruction = LumberjackProgram.Upgrade(accounts, x, y, LumberjackProgramIdPubKey);
            tx.Add(chopInstruction);
            Debug.Log("Sign without session");
            SendAndConfirmTransaction(Web3.Wallet, tx, "Upgrade without session");
        }
    }

    private PublicKey GetAvatar()
    {
        var nftService = ServiceFactory.Resolve<NftService>();
        if (nftService.SelectedNft != null)
        {
            return new PublicKey(nftService.SelectedNft.metaplexData.data.mint);
        }
        
        return Web3.Account.PublicKey;
    }
    
    public async void Collect(bool useSession, byte x, byte y)
    {
        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash(maxSeconds:1)
        };

        CollectAccounts accounts = new CollectAccounts();
        accounts.Player = PlayerDataPDA;
        accounts.Board = BoardPDA;
        accounts.GameActions = GameActionsPDA;
        accounts.Avatar = GetAvatar();
        
        if (useSession)
        {
            if (!(await sessionWallet.IsSessionTokenInitialized()))
            {
                var topUp = true;

                var validity = DateTimeOffset.UtcNow.AddHours(23).ToUnixTimeSeconds();
                var createSessionIX = sessionWallet.CreateSessionIX(topUp, validity);
                accounts.Signer = Web3.Account.PublicKey;
                tx.Add(createSessionIX);
                var chopInstruction = LumberjackProgram.Collect(accounts, x, y, LumberjackProgramIdPubKey);
                tx.Add(chopInstruction);
                Debug.Log("Has no session -> partial sign");
                tx.PartialSign(new[] { Web3.Account, sessionWallet.Account });
                SendAndConfirmTransaction(Web3.Wallet, tx, "Collect and init session");
            }
            else
            {
                tx.FeePayer = sessionWallet.Account.PublicKey;
                accounts.SessionToken = sessionWallet.SessionTokenPDA;
                accounts.Signer = sessionWallet.Account.PublicKey;
                Debug.Log("Has session -> sign and send session wallet");
                var chopInstruction = LumberjackProgram.Collect(accounts, x, y, LumberjackProgramIdPubKey);
                tx.Add(chopInstruction);
                SendAndConfirmTransaction(sessionWallet, tx, "Collect");
            }
        }
        else
        {
            tx.FeePayer = Web3.Account.PublicKey;
            accounts.Signer = Web3.Account.PublicKey;
            var chopInstruction = LumberjackProgram.Collect(accounts, x, y, LumberjackProgramIdPubKey);
            tx.Add(chopInstruction);
            Debug.Log("Sign without session");
            SendAndConfirmTransaction(Web3.Wallet, tx, "Collect without session");
        }
    }
    
    private async void SendAndConfirmTransaction(WalletBase wallet, Transaction transaction, string label = "")
    {
        transactionsInProgress++;
        var res=  await wallet.SignAndSendTransaction(transaction, commitment: Commitment.Confirmed);
        if (res.WasSuccessful && res.Result != null)
        {
            await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
        }
        Debug.Log($"Send tranaction {label} with response: {res.RawRpcResponse}");
        transactionsInProgress--;
    }

    public static bool IsCollectable(TileData tileData)
    {
        long unixTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

        var tileDataBuildingStartCollectTime = tileData.BuildingStartCollectTime + new TimeSpan(0,0,61).TotalSeconds;
        //Debug.Log("Time: " + tileDataBuildingStartCollectTime + " current time " + unixTime + " diff " + (unixTime - tileDataBuildingStartCollectTime));
        
        return tileDataBuildingStartCollectTime < unixTime;
    }

    public static string GetName(TileData tileData)
    {
        switch (tileData.BuildingType)
        {
            case BUILDING_TYPE_MINE:
                return "Stone mine";

            case BUILDING_TYPE_TREE:
                return "Tree";

            case BUILDING_TYPE_EMPTY:
                return "empty";

            case BUILDING_TYPE_SAWMILL:
                return "Sawmill";
        }

        return "NaN";
    }

    public void OnCellClicked(byte x, byte y)
    {
        var cell = ServiceFactory.Resolve<BoardManager>().GetCell(x, y);
        var tileData = CurrentBoardAccount.Data[x][y];
        if (tileData.BuildingType == BUILDING_TYPE_EVIL || tileData.BuildingType == BUILDING_TYPE_GOOD)
        {
            var uiData = new UpgradeBuildingPopupUiData(Web3.Wallet, () =>
            {
                Upgrade(!Web3.Rpc.NodeAddress.AbsoluteUri.Contains("localhost"), x, y);    
            }, tileData);
            ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.UpgradeBuildingPopup, uiData);
        }else if (tileData.BuildingType == BUILDING_TYPE_EMPTY)
        {
            if (!CheckForEnergy(1))
            {
                return;
            }
            // Build 
            var uiData = new BuildBuildingPopupUiData(Web3.Wallet, config =>
            {
                Build(!Web3.Rpc.NodeAddress.AbsoluteUri.Contains("localhost"), x, y, config.building_type);
            }, tileData);
            ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.BuildBuildingPopup, uiData);
        } else if (tileData.BuildingType == BUILDING_TYPE_TREE)
        {
            if (!CheckForEnergy(3))
            {
                return;
            }
            var uiData = new ChopTreePopupUiData(Web3.Wallet, () =>
            {
                ChopTree(!Web3.Rpc.NodeAddress.AbsoluteUri.Contains("localhost"), x, y);
            });
            ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.ChopTreePopup, uiData);
        } else if (tileData.BuildingType == BUILDING_TYPE_MINE ||
                   tileData.BuildingType == BUILDING_TYPE_SAWMILL)
        {
            if (!CheckForEnergy(1))
            {
                return;
            }
            if (IsCollectable(tileData))
            {
                // Collect
                Collect(!Web3.Rpc.NodeAddress.AbsoluteUri.Contains("localhost"), x, y);
            }
            else
            {
                var uiData = new UpgradeBuildingPopupUiData(Web3.Wallet, () =>
                {
                    Upgrade(!Web3.Rpc.NodeAddress.AbsoluteUri.Contains("localhost"), x, y);    
                }, tileData);
                ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.UpgradeBuildingPopup, uiData);
            }
        }
    }

    private bool CheckForEnergy(ulong amountNeeded)
    {
        if (CurrentPlayerData.Energy < amountNeeded)
        {
            ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.RefillEnergyPopup, new RefillEnergyPopupUiData(Web3.Wallet));
            return false;
        }
        return true;
    }

    public async Task RefillEnergy()
    {
        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash(maxSeconds:1)
        };
        
        RefillEnergyAccounts accounts = new RefillEnergyAccounts();
        accounts.Player = PlayerDataPDA;
        accounts.SystemProgram = SystemProgram.ProgramIdKey;
        accounts.Treasury = new PublicKey("CYg2vSJdujzEC1E7kHMzB9QhjiPLRdsAa4Js7MkuXfYq");
        
        tx.FeePayer = Web3.Account.PublicKey;
        accounts.Signer = Web3.Account.PublicKey;
        var ix = LumberjackProgram.RefillEnergy(accounts, LumberjackProgramIdPubKey);
        tx.Add(ix);
        SendAndConfirmTransaction(Web3.Wallet, tx, "Refill energy");
        
        var res=  await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
        if (res.WasSuccessful && res.Result != null)
        {
            await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
        }
    }

    public static bool HasEnoughResources(BalancingService.Cost cost)
    {
        var hasEnoughWood = cost.Wood <= Instance.CurrentBoardAccount.Wood;
        var hasEnoughStone = cost.Stone <= Instance.CurrentBoardAccount.Stone;

        return hasEnoughWood && hasEnoughStone;
    }
    
    public static bool HasEnoughResources(ulong woodCost, ulong stoneCost)
    {
        var hasEnoughWood = woodCost <= Instance.CurrentBoardAccount.Wood;
        var hasEnoughStone = stoneCost <= Instance.CurrentBoardAccount.Stone;

        return hasEnoughWood && hasEnoughStone;
    }
}
