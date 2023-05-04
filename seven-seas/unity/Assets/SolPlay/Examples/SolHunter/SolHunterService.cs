using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Frictionless;
using NUnit.Framework.Constraints;
using SevenSeas.Accounts;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using SevenSeas.Program;
using SevenSeas.Types;
using SolPlay.DeeplinksNftExample.Utils;
using SolPlay.Scripts.Services;
using UnityEngine;

public class SolHunterService : MonoBehaviour
{
    public static PublicKey ProgramId = new PublicKey("HgmkLCs9Ti1e3juimSR8JNhdLE6eup5Pjk6PujiewZU6");

    public enum Direction
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,
    }

    public static byte STATE_EMPTY = 0;
    public static byte STATE_PLAYER = 1;
    public static byte STATE_CHEST = 2;

    public static int TILE_COUNT_X = 10;
    public static int TILE_COUNT_Y = 10;

    public SevenSeas.Accounts.GameDataAccount CurrentGameData;

    private PublicKey levelAccount;

    private PublicKey chestVaultAccount;
    private PublicKey gameActionsAccount;

    // This is a little trick to not get the same transaction hash when there are multiple of the same transactions in the 
    // with the same block hash. Since the would result in the same hash one would be dropped. So we just increase this byte 
    // and send it the with instruction. 
    private int InstructionBump;
    private Dictionary<ulong, GameAction> alreadyPreformedGameActions = new Dictionary<ulong, GameAction>(); 

    public class SolHunterGameDataChangedMessage
    {
        public SevenSeas.Accounts.GameDataAccount GameDataAccount;
    }
    
    public class ShipShotMessage
    {
        public PublicKey ShipOwner;
    }

    public void OnEnable()
    {
        ServiceFactory.RegisterSingleton(this);
        PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("level")
            },
            ProgramId, out levelAccount, out var bump);

        PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("chestVault")
            },
            ProgramId, out chestVaultAccount, out var bumpChest);
        PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("gameActions")
            },
            ProgramId, out gameActionsAccount, out var actionsBump);
    }

    private void Start()
    {
        MessageRouter.AddHandler<SocketServerConnectedMessage>(OnSocketConnected);
    }

    private void OnSocketConnected(SocketServerConnectedMessage message)
    {
        GetGameData();
        ServiceFactory.Resolve<SolPlayWebSocketService>().SubscribeToPubKeyData(levelAccount, result =>
        {
            GameDataAccount gameDataAccount =
                GameDataAccount.Deserialize(Convert.FromBase64String(result.result.value.data[0]));
            SetCachedGameData(gameDataAccount);
            MessageRouter.RaiseMessage(new SolHunterGameDataChangedMessage()
            {
                GameDataAccount = gameDataAccount
            });
        });
        ServiceFactory.Resolve<SolPlayWebSocketService>().SubscribeToPubKeyData(gameActionsAccount, result =>
        {
            GameActionHistory gameActionHistory =
                GameActionHistory.Deserialize(Convert.FromBase64String(result.result.value.data[0]));
            Debug.Log("Actions len: " + gameActionHistory.GameActions.Length);
            OnNewGameActions(gameActionHistory);
        });
    }

    private void OnNewGameActions(GameActionHistory gameActionHistory)
    {
        foreach (var gameAction in gameActionHistory.GameActions)
        {
            if (!alreadyPreformedGameActions.ContainsKey(gameAction.ActionId))
            {
                //if (gameAction.ActionType == 0)
                //{
                    MessageRouter.RaiseMessage(new ShipShotMessage()
                    {
                        ShipOwner = ServiceFactory.Resolve<WalletHolderService>().InGameWallet.Account.PublicKey
                    });
                //}
                alreadyPreformedGameActions.Add(gameAction.ActionId, gameAction);
            }
        }
    }

    public static int GetMaxHealthByLevel(Tile tile)
    {
        switch (tile.ShipLevel)
        {
            case 1:
                return 3;
            case 2:
                return 7;
            case 3:
                return 12;
            case 4:
                return 20;
        }

        return 1;
    }
    
    public async Task<GameDataAccount> GetGameData()
    {
        var gameData = await ServiceFactory.Resolve<WalletHolderService>().InGameWallet.ActiveRpcClient
            .GetAccountInfoAsync(this.levelAccount, Commitment.Confirmed, BinaryEncoding.JsonParsed);
        GameDataAccount gameDataAccount =
            GameDataAccount.Deserialize(Convert.FromBase64String(gameData.Result.Value.Data[0]));
        SetCachedGameData(gameDataAccount);
        MessageRouter.RaiseMessage(new SolHunterGameDataChangedMessage()
        {
            GameDataAccount = gameDataAccount
        });
        return gameDataAccount;
    }

    private void SetCachedGameData(SevenSeas.Accounts.GameDataAccount gameDataAccount)
    {
        bool playerWasAlive = false;
        if (CurrentGameData != null)
        {
            playerWasAlive = IsPlayerSpawned();
        }

        CurrentGameData = gameDataAccount;
        bool playerAliveAfterData = IsPlayerSpawned();

        if (playerWasAlive && !playerAliveAfterData)
        {
            LoggingService.Log("You died :( ", true);
            ServiceFactory.Resolve<NftService>().ResetSelectedNft();
        }
        else
        {
            var avatar = TryGetSpawnedPlayerAvatar();
            if (avatar != null)
            {
                var avatarNft = ServiceFactory.Resolve<NftService>().GetNftByMintAddress(avatar);
                ServiceFactory.Resolve<NftService>().SelectNft(avatarNft);
            }
        }
    }

    public void Initialize()
    {
        var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
        if (!walletHolderService.HasEnoughSol(true, 300000000))
        {
            Debug.LogError("Not enough sol");
            return;
        }

        TransactionInstruction initializeInstruction = InitializeInstruction();
        ServiceFactory.Resolve<TransactionService>().SendInstructionInNextBlock("Initialize", initializeInstruction,
            walletHolderService.InGameWallet);
    }

    public void Reset()
    {
        var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
        if (!walletHolderService.HasEnoughSol(true, 300000000))
        {
            return;
        }

        TransactionInstruction initializeInstruction = GetResetInstruction();
        ServiceFactory.Resolve<TransactionService>().SendInstructionInNextBlock("Reset", initializeInstruction,
            walletHolderService.InGameWallet);
    }

    public void Move(Direction direction)
    {
        var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
        if (!walletHolderService.HasEnoughSol(true, 10000))
        {
            return;
        }

        TransactionInstruction initializeInstruction = GetMovePlayerInstruction((byte) direction);
        ServiceFactory.Resolve<TransactionService>().SendInstructionInNextBlock($"Move{direction}",
            initializeInstruction, walletHolderService.InGameWallet);
    }

    public void Shoot()
    {
        var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
        if (!walletHolderService.HasEnoughSol(true, 10000))
        {
            return;
        }

        TransactionInstruction initializeInstruction = GetShootInstruction();
        ServiceFactory.Resolve<TransactionService>().SendInstructionInNextBlock($"Shoot",
            initializeInstruction, walletHolderService.InGameWallet);
    }

    public void SpawnPlayerAndChest()
    {
        long costForSpawn = (long) (0.11f * SolanaUtils.SolToLamports);
        var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
        if (!walletHolderService.HasEnoughSol(true, costForSpawn))
        {
            return;
        }

        LoggingService.Log("Spawn player and chest", true);

        TransactionInstruction initShip = GetInitShipInstruction();
        ServiceFactory.Resolve<TransactionService>().SendInstructionInNextBlock("Init ship", initShip,
            walletHolderService.InGameWallet,
            s =>
            {
                //GetGameData(); not needed since we rely on the socket connection
                GetGameData();
            });
        
        TransactionInstruction initializeInstruction = GetSpawnPlayerAndChestInstruction();
        ServiceFactory.Resolve<TransactionService>().SendInstructionInNextBlock("Spawn player", initializeInstruction,
            walletHolderService.InGameWallet,
            s =>
            {
                //GetGameData(); not needed since we rely on the socket connection
                GetGameData();
            });
    }

    private TransactionInstruction GetShootInstruction()
    {
        ShootAccounts accounts = new ShootAccounts();
        accounts.GameDataAccount = levelAccount;
        accounts.ChestVault = chestVaultAccount;
        accounts.GameActions = gameActionsAccount;
        accounts.Player = ServiceFactory.Resolve<WalletHolderService>().InGameWallet.Account.PublicKey;

        TransactionInstruction initializeInstruction =
            SevenSeasProgram.Shoot(accounts, GetInstructionBump(), ProgramId);
        return initializeInstruction;
    }

    private TransactionInstruction GetMovePlayerInstruction(byte direction)
    {
        MovePlayerV2Accounts accounts = new MovePlayerV2Accounts();
        accounts.GameDataAccount = levelAccount;
        accounts.ChestVault = chestVaultAccount;
        accounts.SystemProgram = SystemProgram.ProgramIdKey;
        accounts.Player = ServiceFactory.Resolve<WalletHolderService>().InGameWallet.Account.PublicKey;

        TransactionInstruction initializeInstruction =
            SevenSeasProgram.MovePlayerV2(accounts, direction, GetInstructionBump(), ProgramId);
        return initializeInstruction;
    }

    private byte GetInstructionBump()
    {
        return (byte) (InstructionBump++ % 127);
    }

    private TransactionInstruction InitializeInstruction()
    {
        var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
        var wallet = walletHolderService.InGameWallet;

        var accounts = new InitializeAccounts();
        accounts.Signer = wallet.Account.PublicKey;
        accounts.NewGameDataAccount = levelAccount;
        accounts.SystemProgram = SystemProgram.ProgramIdKey;
        accounts.ChestVault = chestVaultAccount;
        accounts.GameActions = gameActionsAccount;

        TransactionInstruction initializeInstruction = SevenSeasProgram.Initialize(accounts, ProgramId);
        return initializeInstruction;
    }

    private TransactionInstruction GetResetInstruction()
    {
        var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
        var wallet = walletHolderService.InGameWallet;

        var accounts = new ResetAccounts();
        accounts.Signer = wallet.Account.PublicKey;
        accounts.NewGameDataAccount = levelAccount;
        accounts.SystemProgram = SystemProgram.ProgramIdKey;
        accounts.ChestVault = chestVaultAccount;
        accounts.GameActions = gameActionsAccount;

        TransactionInstruction resetInstruction = SevenSeasProgram.Reset(accounts, ProgramId);
        return resetInstruction;
    }

    public TransactionInstruction GetSpawnPlayerAndChestInstruction()
    {
        var selectedNft = ServiceFactory.Resolve<NftService>().SelectedNft;
        if (selectedNft == null)
        {
            LoggingService.Log("Nft is still loading...", true);
            return null;
        }

        SpawnPlayerAccounts accounts = new SpawnPlayerAccounts();
        accounts.GameDataAccount = levelAccount;
        accounts.ChestVault = chestVaultAccount;
        accounts.SystemProgram = SystemProgram.ProgramIdKey;
        accounts.Payer = ServiceFactory.Resolve<WalletHolderService>().InGameWallet.Account.PublicKey;
        accounts.NftAccount = new PublicKey(ServiceFactory.Resolve<NftService>().SelectedNft.MetaplexData.mint);
        PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("ship"),
                accounts.NftAccount.KeyBytes
            },
            ProgramId, out PublicKey shipPda, out var bump);
        accounts.Ship = shipPda;
        
        TransactionInstruction initializeInstruction =
            SevenSeasProgram.SpawnPlayer(accounts, new PublicKey(selectedNft.MetaplexData.mint), ProgramId);
        return initializeInstruction;
    }

    public TransactionInstruction GetInitShipInstruction()
    {
        var selectedNft = ServiceFactory.Resolve<NftService>().SelectedNft;
        if (selectedNft == null)
        {
            LoggingService.Log("Nft is still loading...", true);
            return null;
        }

        InitializeShipAccounts accounts = new InitializeShipAccounts();
        accounts.SystemProgram = SystemProgram.ProgramIdKey;
        accounts.NftAccount = new PublicKey(ServiceFactory.Resolve<NftService>().SelectedNft.MetaplexData.mint);
        PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("ship"),
                accounts.NftAccount.KeyBytes
            },
            ProgramId, out PublicKey shipPda, out var bump);
        accounts.NewShip = shipPda;
        accounts.Signer = ServiceFactory.Resolve<WalletHolderService>().InGameWallet.Account.PublicKey;

        TransactionInstruction initializeInstruction = SevenSeasProgram.InitializeShip(accounts, ProgramId);
        return initializeInstruction;
    }

    public PublicKey TryGetSpawnedPlayerAvatar()
    {
        if (CurrentGameData == null)
        {
            return null;
        }

        var localWallet = ServiceFactory.Resolve<WalletHolderService>().InGameWallet.Account.PublicKey.Key;
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                var tile = CurrentGameData.Board[y][x];
                if (tile.Player == localWallet && tile.State == STATE_PLAYER)
                {
                    return tile.Avatar;
                }
            }
        }

        return null;
    }

    public bool IsPlayerSpawned()
    {
        if (CurrentGameData == null)
        {
            return false;
        }

        var localWallet = ServiceFactory.Resolve<WalletHolderService>().InGameWallet.Account.PublicKey.Key;
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                var tile = CurrentGameData.Board[y][x];
                if (tile.Player == localWallet && tile.State == STATE_PLAYER)
                {
                    return true;
                }
            }
        }

        return false;
    }
}