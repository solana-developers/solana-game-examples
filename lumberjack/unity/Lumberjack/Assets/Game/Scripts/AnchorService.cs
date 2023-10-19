using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Frictionless;
using Game.Scripts.Ui;
using Lumberjack;
using Lumberjack.Accounts;
using Lumberjack.Program;
using Solana.Unity.Programs;
using Solana.Unity.Programs.Models;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.SessionKeys.GplSession.Accounts;
using Solana.Unity.Wallet;
using Services;
using UnityEngine;

public class AnchorService : MonoBehaviour
{
    public PublicKey AnchorProgramIdPubKey = new ("MkabCfyUD6rBTaYHpgKBBpBo5qzWA2pK2hrGGKMurJt");
    
    public const int TIME_TO_REFILL_ENERGY = 60;
    public const int MAX_ENERGY = 10;
    
    public static AnchorService Instance { get; private set; }
    public static Action<PlayerData> OnPlayerDataChanged;
    public static Action OnInitialDataLoaded;
    public bool IsAnyTransactionInProgress => transactionsInProgress > 0;
    public PlayerData CurrentPlayerData;

    private SessionWallet sessionWallet;
    private PublicKey PlayerDataPDA;
    private bool _isInitialized;
    private LumberjackClient lumberjackClient;
    private int transactionsInProgress;
    private long? sessionValidUntil;

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
        Debug.Log("Logged in with pubkey: " + account.PublicKey);
        
        var solBalance = await Web3.Instance.WalletBase.GetBalance();
        if (solBalance < 20000)
        {
            Debug.Log("Not enough sol. Requesting airdrop");
            var result = await Web3.Instance.WalletBase.RequestAirdrop(commitment: Commitment.Confirmed);
            if (!result.WasSuccessful)
            {
                Debug.Log("Airdrop failed.");
            }
        }

        sessionWallet = await SessionWallet.GetSessionWallet(AnchorProgramIdPubKey, "ingame");
        await UpdateSessionValid();
        
        PublicKey.TryFindProgramAddress(new[]
        {Encoding.UTF8.GetBytes("player"), account.PublicKey.KeyBytes},
            AnchorProgramIdPubKey, out PlayerDataPDA, out byte bump);
        
        lumberjackClient = new LumberjackClient(Web3.Rpc, Web3.WsRpc, AnchorProgramIdPubKey);

        await SubscribeToPlayerDataUpdates();

        OnInitialDataLoaded?.Invoke();
    }

    public bool IsInitialized()
    {
        return _isInitialized;
    }

    private long GetSessionKeysEndTime()
    {
        return DateTimeOffset.UtcNow.AddMinutes(3).ToUnixTimeSeconds();
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
        Debug.Log($"Socket Message: Player has  {playerData.Wood} wood now.");
        CurrentPlayerData = playerData;
        OnPlayerDataChanged?.Invoke(playerData);
    }

    public async Task InitGameDataAccount(bool useSession)
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
        
        var initTx = LumberjackProgram.InitPlayer(accounts, AnchorProgramIdPubKey);
        tx.Add(initTx);

        if (useSession)
        {
            if (!(await IsSessionTokenInitialized()))
            {
                var topUp = true;

                var validity = GetSessionKeysEndTime();
                var createSessionIX = sessionWallet.CreateSessionIX(topUp, validity);
                accounts.Signer = Web3.Account.PublicKey;
                tx.Add(createSessionIX);
                Debug.Log("Has no session -> partial sign");
                tx.PartialSign(new[] { Web3.Account, sessionWallet.Account });
            }
        }

        bool success = await SendAndConfirmTransaction(Web3.Wallet, tx, "initialize", () =>
        {
        }, s =>
        {
        });

        if (!success)
        {
            Debug.LogError("Init was not successful");
        }

        await UpdateSessionValid();
        
        await SubscribeToPlayerDataUpdates();
    }

    private async Task<bool> SendAndConfirmTransaction(WalletBase wallet, Transaction transaction, string label = "", Action onSucccess = null, Action<string> onError = null)
    {
        transactionsInProgress++;
        Debug.Log("Sending and confirming transaction: " + label);
        RequestResult<string> res;
        try
        {
            res = await wallet.SignAndSendTransaction(transaction, commitment: Commitment.Confirmed);
        }
        catch (Exception e)
        {
            Debug.Log("Transaction exception " + e);
            transactionsInProgress--;
            onError?.Invoke(e.ToString());
            return false;
        }
        
        Debug.Log("Transaction sent: " + res.RawRpcResponse);
        if (res.WasSuccessful && res.Result != null)
        {
            Debug.Log("Confirm");

            await ConfirmTransaction(Web3.Rpc, res.Result, Commitment.Confirmed);
            Debug.Log("Confirm done");
        }
        else
        {
            Debug.LogError("Transaction failed: " + res.RawRpcResponse);
            if (res.RawRpcResponse.Contains("InsufficientFundsForRent"))
            {
                Debug.Log("Trigger session top up");
                //TriggerTopUpTransaction();
            }
            transactionsInProgress--;
            onError?.Invoke(res.RawRpcResponse);
            return false;
        }
        Debug.Log($"Send transaction {label} with response: {res.RawRpcResponse}");
        transactionsInProgress--;
        onSucccess?.Invoke();
        return true;
    }

    public static async UniTask<bool> ConfirmTransaction(
        IRpcClient rpc,
        string hash,
        Commitment commitment = Commitment.Finalized)
    {
        TimeSpan delay = commitment == Commitment.Finalized ? TimeSpan.FromSeconds(60.0) : TimeSpan.FromSeconds(30.0);
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancelToken = cancellationTokenSource.Token;
        cancellationTokenSource.CancelAfter(delay);
        if (commitment == Commitment.Processed)
            commitment = Commitment.Confirmed;
        while (!cancelToken.IsCancellationRequested)
        {
            await UniTask.Delay(50, cancellationToken: cancelToken);
            RequestResult<ResponseValue<List<SignatureStatusInfo>>> signatureStatusesAsync = await rpc.GetSignatureStatusesAsync(new List<string>()
            {
                hash
            }, true);
            if (signatureStatusesAsync.WasSuccessful && signatureStatusesAsync.Result?.Value != null && signatureStatusesAsync.Result.Value.TrueForAll((Predicate<SignatureStatusInfo>) (sgn =>
                {
                    if (sgn == null || sgn.ConfirmationStatus == null)
                        return false;
                    if (sgn.ConfirmationStatus.Equals(commitment.ToString().ToLower()))
                        return true;
                    return commitment.Equals((object) Commitment.Confirmed) && sgn.ConfirmationStatus.Equals(Commitment.Finalized.ToString().ToLower());
                })))
                return true;
        }
        return false;
    }
    
    public async Task<SessionWallet> RevokeSession()
    {
        await sessionWallet.CloseSession();
        return sessionWallet;
    }

    public async void ChopTree(bool useSession)
    {
        if (!Instance.IsSessionValid())
        {
            await Instance.UpdateSessionValid();
            ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.SessionPopup, new SessionPopupUiData());
            return;
        }
        
        var transaction = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash(maxSeconds:1)
        };

        ChopTreeAccounts chopTreeAccounts = new ChopTreeAccounts
        {
            Player = PlayerDataPDA
        };

        if (useSession)
        {
            transaction.FeePayer = sessionWallet.Account.PublicKey;
            chopTreeAccounts.Signer = sessionWallet.Account.PublicKey;
            chopTreeAccounts.SessionToken = sessionWallet.SessionTokenPDA;
            var chopInstruction = LumberjackProgram.ChopTree(chopTreeAccounts, AnchorProgramIdPubKey);
            transaction.Add(chopInstruction);
            Debug.Log("Sign and send chop tree with session");
            SendAndConfirmTransaction(sessionWallet, transaction, "Chop Tree with session.");
        }
        else
        {
            transaction.FeePayer = Web3.Account.PublicKey;
            chopTreeAccounts.Signer = Web3.Account.PublicKey;
            var chopInstruction = LumberjackProgram.ChopTree(chopTreeAccounts, AnchorProgramIdPubKey);
            transaction.Add(chopInstruction);
            Debug.Log("Sign and send init without session");
            SendAndConfirmTransaction(Web3.Wallet, transaction, "Chop Tree without session.");
        }
    }

    public async Task<bool> IsSessionTokenInitialized()
    {
        var sessionTokenData = await Web3.Rpc.GetAccountInfoAsync(sessionWallet.SessionTokenPDA, Commitment.Confirmed);
        if (sessionTokenData.Result != null && sessionTokenData.Result.Value != null)
        {
            return true;
        }
       
        return false;
    }
    
    public async Task<bool> UpdateSessionValid()
    {
        SessionToken sessionToken = await RequestSessionToken();
        
        if (sessionToken == null) return false;

        Debug.Log("Session token valid until: " + (new DateTime(1970, 1, 1)).AddSeconds(sessionToken.ValidUntil) + " Now: " + DateTimeOffset.UtcNow);
        sessionValidUntil = sessionToken.ValidUntil;
        return IsSessionValid();
    }
    
    public async Task<SessionToken> RequestSessionToken()
    {
        ResponseValue<AccountInfo> sessionTokenData = (await Web3.Rpc.GetAccountInfoAsync(sessionWallet.SessionTokenPDA, Commitment.Confirmed)).Result;

        if (sessionTokenData == null) return null;
        if (sessionTokenData.Value == null || sessionTokenData.Value.Data[0] == null)
        {
            return null;
        }
        
        var sessionToken = SessionToken.Deserialize(Convert.FromBase64String(sessionTokenData.Value.Data[0]));

        return sessionToken;
    }
    
    public bool IsSessionValid()
    {
        return sessionValidUntil != null && sessionValidUntil > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private async Task RefreshSessionWallet()
    {
        sessionWallet = await SessionWallet.GetSessionWallet(AnchorProgramIdPubKey, "ingame",
            Web3.Wallet);
    }
    
    public async Task<SessionWallet> CreateNewSession()
    {
        var sessionToken = await Instance.RequestSessionToken();
        if (sessionToken != null)
        {
            await sessionWallet.CloseSession();
        }
        
        var transaction = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash(Commitment.Confirmed, false)
        };
        SessionWallet.Instance = null;
        await RefreshSessionWallet();
        var sessionIx = sessionWallet.CreateSessionIX(true, GetSessionKeysEndTime());
        transaction.Add(sessionIx);
        transaction.PartialSign(new[] { Web3.Account, sessionWallet.Account });

        var res = await Web3.Wallet.SignAndSendTransaction(transaction, commitment: Commitment.Confirmed);

        Debug.Log("Create session wallet: " + res.RawRpcResponse);
        await Web3.Wallet.ActiveRpcClient.ConfirmTransaction(res.Result, Commitment.Confirmed);
        var sessionValid = await UpdateSessionValid();
        Debug.Log("After create session, the session is valid: " + sessionValid);
        return sessionWallet;
    }
    
    private async void SendAndConfirmTransaction(WalletBase wallet, Transaction transaction, string label = "")
    {
        transactionsInProgress++;
        var res=  await wallet.SignAndSendTransaction(transaction, commitment: Commitment.Confirmed);
        if (res.WasSuccessful && res.Result != null)
        {
            await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
        }
        Debug.Log($"Sent transaction {label} with response: {res.RawRpcResponse}");
        transactionsInProgress--;
    }
}
