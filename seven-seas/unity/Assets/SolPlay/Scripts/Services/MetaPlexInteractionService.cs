using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Frictionless;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using SolPlay.Scripts.Ui;
using UnityEngine;

namespace SolPlay.Scripts.Services
{
    public class MetaPlexInteractionService : MonoBehaviour, IMultiSceneSingleton
    {
        private void Awake()
        {
            if (ServiceFactory.Resolve<MetaPlexInteractionService>() != null)
            {
                Destroy(gameObject);
                return;
            }

            ServiceFactory.RegisterSingleton(this);
        }

        /// <summary>
        /// WIP
        /// Be careful with this function, it will really burn the nft and return the accounts sol. 
        /// </summary>
        /// <param name="nft"></param>
        public async void BurnNFt(SolPlayNft nft)
        {
            var wallet = ServiceFactory.Resolve<WalletHolderService>().BaseWallet;
            var nftService = ServiceFactory.Resolve<NftService>();
            var blockHash = await wallet.ActiveRpcClient.GetRecentBlockHashAsync();

            if (blockHash.Result == null)
            {
                MessageRouter
                    .RaiseMessage(new BlimpSystem.ShowLogMessage("Block hash null. Connected to internet?"));
                return;
            }

            //var burnNftTransaction = CreateBurnNFTTransaction(nft, blockHash);
            //var signedTransaction = burnNftTransaction.Build(SimpleWallet.instance.wallet.GetAccount(0));
            //var result = await SimpleWallet.instance.activeRpcClient.SendTransactionAsync(signedTransaction);
            //Debug.Log(result.Reason);
        }

        private Transaction CreateBurnNFTTransaction(SolPlayNft nft,
            RequestResult<ResponseValue<BlockHash>> blockHash)
        {
            if (!ServiceFactory.Resolve<WalletHolderService>().TryGetPhantomPublicKey(out string phantomPublicKey))
            {
                return null;
            }

            var seeds2 = new List<byte[]>();
            seeds2.Add(Encoding.UTF8.GetBytes("metadata"));
            seeds2.Add(new PublicKey("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s").KeyBytes);
            seeds2.Add(new PublicKey(nft.MetaplexData.mint).KeyBytes);

            PublicKey.TryFindProgramAddress(
                seeds2,
                new PublicKey("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s"),
                out PublicKey derivedMetaDataAccount, out var _bump2);

            var seeds = new List<byte[]>();
            seeds.Add(Encoding.UTF8.GetBytes("metadata"));
            seeds.Add(new PublicKey("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s").KeyBytes);
            seeds.Add(new PublicKey(nft.MetaplexData.mint).KeyBytes);
            seeds.Add(Encoding.UTF8.GetBytes("edition"));

            PublicKey.TryFindProgramAddress(
                seeds,
                new PublicKey("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s"),
                out PublicKey derivedMasterEdition2Account, out var _bump);

            Transaction garblesSdkTransaction = new Transaction();
            garblesSdkTransaction.Instructions = new List<TransactionInstruction>();
            var keys = new List<AccountMeta>();
            keys.Add(AccountMeta.Writable(derivedMetaDataAccount, false));
            keys.Add(AccountMeta.Writable(new PublicKey(phantomPublicKey), true));
            keys.Add(AccountMeta.Writable(new PublicKey(nft.MetaplexData.mint), false));
            keys.Add(AccountMeta.Writable(new PublicKey(nft.TokenAccount.PublicKey), false));
            keys.Add(AccountMeta.Writable(derivedMasterEdition2Account, false));
            keys.Add(AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false));

            uint descriminator = 29;
            TransactionInstruction burnInstruction = new TransactionInstruction()
            {
                Data = Array.Empty<byte>(),
                Keys = keys,
                ProgramId = new PublicKey("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s")
            };

            garblesSdkTransaction.Instructions.Add(burnInstruction);
            garblesSdkTransaction.FeePayer = new PublicKey(phantomPublicKey);
            garblesSdkTransaction.RecentBlockHash = blockHash.Result.Value.Blockhash;
            return garblesSdkTransaction;
        }

        public IEnumerator HandleNewSceneLoaded()
        {
            yield return null;
        }
    }
}