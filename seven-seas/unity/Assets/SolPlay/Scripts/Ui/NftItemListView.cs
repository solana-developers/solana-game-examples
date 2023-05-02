using System;
using System.Collections.Generic;
using Frictionless;
using SolPlay.Scripts.Services;
using UnityEngine;

namespace SolPlay.Scripts.Ui
{
    public class NftItemListView : MonoBehaviour
    {
        public GameObject ItemRoot;
        public NftItemView itemPrefab;
        public string FilterSymbol;
        public string BlackList;

        private List<NftItemView> allNftItemViews = new List<NftItemView>();
        private Action<SolPlayNft> onNftSelected;

        public void OnEnable()
        {
            UpdateContent();
        }

        public void Start()
        {
            MessageRouter.AddHandler<NftSelectedMessage>(OnNFtSelectedMessage);
            MessageRouter.AddHandler<NewHighScoreLoadedMessage>(OnHighscoreLoadedMessage);
        }

        public void SetData(Action<SolPlayNft> onNftSelected)
        {
            this.onNftSelected = onNftSelected;
        }

        private void OnHighscoreLoadedMessage(NewHighScoreLoadedMessage message)
        {
            foreach (var itemView in allNftItemViews)
            {
                if (itemView.CurrentSolPlayNft.MetaplexData.mint == message.HighscoreEntry.Seed)
                {
                    itemView.PowerLevel.text = $"Score: {message.HighscoreEntry.Highscore}";
                }
            }
        }

        private void OnNFtSelectedMessage(NftSelectedMessage message)
        {
            UpdateContent();
        }

        public void UpdateContent()
        {
            var nftService = ServiceFactory.Resolve<NftService>();
            if (nftService == null)
            {
                return;
            }

            foreach (SolPlayNft nft in nftService.MetaPlexNFts)
            {
                AddNFt(nft);
            }

            List<NftItemView> notExistingNfts = new List<NftItemView>();
            foreach (NftItemView nftItemView in allNftItemViews)
            {
                bool existsInWallet = false;
                foreach (SolPlayNft walletNft in nftService.MetaPlexNFts)
                {
                    if (nftItemView.CurrentSolPlayNft.MetaplexData.mint == walletNft.MetaplexData.mint)
                    {
                        existsInWallet = true;
                        break;
                    }
                }

                if (!existsInWallet)
                {
                    notExistingNfts.Add(nftItemView);
                }
            }

            for (var index = notExistingNfts.Count - 1; index >= 0; index--)
            {
                var nftView = notExistingNfts[index];
                allNftItemViews.Remove(nftView);
                Destroy(nftView.gameObject);
            }
        }

        public void AddNFt(SolPlayNft newSolPlayNft)
        {
            foreach (var nft in allNftItemViews)
            {
                if (nft.CurrentSolPlayNft.MetaplexData.mint == newSolPlayNft.MetaplexData.mint)
                {
                    // already exists
                    return;
                }
            }

            InstantiateListNftItem(newSolPlayNft);
        }

        private void InstantiateListNftItem(SolPlayNft solPlayNft)
        {
            if (string.IsNullOrEmpty(solPlayNft.MetaplexData.mint))
            {
                return;
            }

            if (!string.IsNullOrEmpty(FilterSymbol) && solPlayNft.MetaplexData.data.symbol != FilterSymbol)
            {
                return;
            }

            if (!string.IsNullOrEmpty(BlackList) && solPlayNft.MetaplexData.data.symbol == BlackList)
            {
                return;
            }

            NftItemView nftItemView = Instantiate(itemPrefab, ItemRoot.transform);
            nftItemView.SetData(solPlayNft, OnItemClicked);
            allNftItemViews.Add(nftItemView);
        }

        private void OnItemClicked(NftItemView itemView)
        {
            Debug.Log("Item Clicked: " + itemView.CurrentSolPlayNft.MetaplexData.data.name);
            ServiceFactory.Resolve<NftContextMenu>().Open(itemView, onNftSelected);
        }
    }
}