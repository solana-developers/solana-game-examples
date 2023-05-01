using System;
using Frictionless;
using SevenSeas.Types;
using SolPlay.Scripts;
using SolPlay.Scripts.Services;
using SolPlay.Scripts.Ui;
using TMPro;
using UnityEngine;

namespace SolHunter
{
    public class SolHunterTile : MonoBehaviour
    {
        public TextMeshProUGUI TileInfo;
        public NftItemView NftItemView;

        public async void SetData(Tile tile)
        {
            if (tile.State == SolHunterService.STATE_EMPTY)
            {
                TileInfo.text = "";
                NftItemView.gameObject.SetActive(false);
                return;
            }

            if (tile.State == SolHunterService.STATE_CHEST)
            {
                NftItemView.gameObject.SetActive(false);
                TileInfo.text = "Chest\n<color=green>(0.05Sol)</color>";
            }
            else
            {
                TileInfo.text = String.Empty;
                var wallet= ServiceFactory.Resolve<WalletHolderService>().BaseWallet;

                var avatarNft = ServiceFactory.Resolve<NftService>().GetNftByMintAddress(tile.Avatar);
                
                if (avatarNft == null)
                {
                    avatarNft = SolPlayNft.TryLoadNftFromLocal(tile.Avatar);
                }

                if (avatarNft == null)
                {
                    avatarNft = new SolPlayNft();
                    await avatarNft.LoadData(tile.Avatar, wallet.ActiveRpcClient);
                    if (avatarNft.LoadingImageTask != null)
                    {
                        await avatarNft.LoadingImageTask;
                    }
                }

                NftItemView.gameObject.SetActive(true);
                if (!string.IsNullOrEmpty(avatarNft.LoadingError) || avatarNft.MetaplexData == null)
                {
                    NftItemView.SetData(ServiceFactory.Resolve<NftService>().CreateDummyLocalNft(wallet.Account.PublicKey), view =>
                    {
                    });
                }
                else
                {
                    NftItemView.SetData(avatarNft, view =>
                    {
                    });
                }
            }
        }
    }
}