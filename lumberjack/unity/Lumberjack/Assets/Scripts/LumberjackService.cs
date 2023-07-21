using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lumberjack;
using Lumberjack.Accounts;
using Lumberjack.Program;
using Solana.Unity.Programs;
using Solana.Unity.Programs.Models;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine;


public class LumberjackService : MonoBehaviour
{
    public PublicKey LumberjackProgramIdPubKey = new PublicKey("MkabCfyUD6rBTaYHpgKBBpBo5qzWA2pK2hrGGKMurJt");
    
    public const int TIME_TO_REFILL_ENERGY = 60;
    public const int MAX_ENERGY = 10;
    
    public static LumberjackService Instance { get; private set; }
    public static Action<PlayerData> OnPlayerDataChanged;
    public static Action OnInitialDataLoaded;
    public bool IsAnyTransactionInProgress => transactionsInProgress > 0;
    public PlayerData CurrentPlayerData;

    private SessionWallet sessionWallet;
    private PublicKey PlayerDataPDA;
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
        
        lumberjackClient = new LumberjackClient(Web3.Rpc, Web3.WsRpc, LumberjackProgramIdPubKey);
        
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
        
        if (playerData != null)
        {
            await lumberjackClient.SubscribePlayerDataAsync(PlayerDataPDA, OnRecievedPlayerDataUpdate, Commitment.Confirmed);
        }
    }

    private void OnRecievedPlayerDataUpdate(SubscriptionState state, ResponseValue<AccountInfo> value, PlayerData playerData)
    {
        Debug.Log("Socket Message " + state + value + playerData);
        Debug.Log("Player data first " + playerData.Wood + " wood");
        CurrentPlayerData = playerData;
        OnPlayerDataChanged?.Invoke(playerData);
    }

    public async Task<RequestResult<string>> InitGameDataAccount()
    {
        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash()
        };

        InitPlayerAccounts accounts = new InitPlayerAccounts();
        accounts.Player = PlayerDataPDA;
        accounts.Signer = Web3.Account;
        accounts.SystemProgram = SystemProgram.ProgramIdKey;
        
        var initTx = LumberjackProgram.InitPlayer(accounts, LumberjackProgramIdPubKey);
        tx.Add(initTx);

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
        
        var initResult =  await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
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

    public async void ChopTree(bool useSession)
    {
        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash(maxSeconds:1)
        };

        ChopTreeAccounts accounts = new ChopTreeAccounts();
        accounts.Player = PlayerDataPDA;
        
        if (useSession)
        {
            if (!(await sessionWallet.IsSessionTokenInitialized()))
            {
                var topUp = true;

                var validity = DateTimeOffset.UtcNow.AddHours(23).ToUnixTimeSeconds();
                var createSessionIX = sessionWallet.CreateSessionIX(topUp, validity);
                accounts.Signer = Web3.Account.PublicKey;
                tx.Add(createSessionIX);
                var chopInstruction = LumberjackProgram.ChopTree(accounts, LumberjackProgramIdPubKey);
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
                var chopInstruction = LumberjackProgram.ChopTree(accounts, LumberjackProgramIdPubKey);
                tx.Add(chopInstruction);
                Debug.Log("Has session -> sign and send session wallet");

                SendAndConfirmTransaction(sessionWallet, tx, "Chop Tree");
            }
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
}
