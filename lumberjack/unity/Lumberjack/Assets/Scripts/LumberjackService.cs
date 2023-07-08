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
        
        private SessionWallet sessionWallet;
        private PublicKey PlayerDataPDA;
        private bool _isInitialized;
        private LumberjackClient lumberjackClient;
        
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
            if (solBalance < 10000)
            {
                await Web3.Instance.WalletBase.RequestAirdrop(commitment: Commitment.Confirmed);
            }

            PublicKey.TryFindProgramAddress(new[]
            {Encoding.UTF8.GetBytes("player"), account.PublicKey.KeyBytes},
                LumberjackProgramIdPubKey, out PlayerDataPDA, out byte bump);
            
            lumberjackClient = new LumberjackClient(Web3.Rpc, Web3.WsRpc, LumberjackProgramIdPubKey);

            AccountResultWrapper<PlayerData> playerData = null;
            
            try
            {
                playerData = await lumberjackClient.GetPlayerDataAsync(PlayerDataPDA, Commitment.Confirmed);
                if (playerData.ParsedResult != null)
                {
                    OnPlayerDataChanged?.Invoke(playerData.ParsedResult);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Probably playerData not available " + e.Message);
            }
            
            if (playerData != null)
            {
                _isInitialized = true;
                Debug.Log("Player data first "+ playerData.ParsedResult.Wood + " wood");
                await SubscribeToPlayerDataUpdates();
            }
            else
            {
               // The player needs to create a new account which will be triggered from the ui
            }
            
            sessionWallet = await SessionWallet.GetSessionWallet(LumberjackProgramIdPubKey, "ingame");
            OnInitialDataLoaded?.Invoke();
        }

        public bool IsInitialized()
        {
            return _isInitialized;
        }
        
        private async Task InitPlayerData()
        {
            var initResult = await InitGameDataAccount();
            Debug.Log("Init result: " + initResult);
            await SubscribeToPlayerDataUpdates();
            OnInitialDataLoaded?.Invoke();
        }

        private async Task SubscribeToPlayerDataUpdates()
        {
            await lumberjackClient.SubscribePlayerDataAsync(PlayerDataPDA, OnRecievedPlayerDataUpdate, Commitment.Confirmed);
        }

        private void OnRecievedPlayerDataUpdate(SubscriptionState state, ResponseValue<AccountInfo> value, PlayerData playerData)
        {
            Debug.Log("Socket Message " + state + value + playerData);
            Debug.Log("Player data first " + playerData.Wood + " wood");
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
            
            tx.Add(initTx);
            return await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
        }

        public async Task<SessionWallet> RevokeSession()
        { 
            sessionWallet.Logout();
            return sessionWallet;
        }

        public async Task<RequestResult<string>> ChopTree(bool useSession)
        {
            var tx = new Transaction()
            {
                FeePayer = Web3.Account,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = await Web3.BlockHash(maxSeconds:0)
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
                }
                else
                {
                    tx.FeePayer = sessionWallet.Account.PublicKey;
                    accounts.SessionToken = sessionWallet.SessionTokenPDA;
                    accounts.Signer = sessionWallet.Account.PublicKey;
                    var chopInstruction = LumberjackProgram.ChopTree(accounts, LumberjackProgramIdPubKey);
                    tx.Add(chopInstruction);
                    Debug.Log("Has session -> sign and send session wallet");

                    return await sessionWallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
                }
            }
            
            return await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
        }
    }
