using System;
using Frictionless;
using Solana.Unity.SDK.Nft;
using SolPlay.Scripts.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Scripts.Ui
{
    /// <summary>
    /// Show the image of a given Nft and can have a click handler
    /// </summary>
    public class NftItemView : MonoBehaviour
    {
        public Nft CurrentSolPlayNft;
        public RawImage Icon;
        public TextMeshProUGUI Headline;
        public TextMeshProUGUI Description;
        public TextMeshProUGUI ErrorText;
        public Button Button;
        public GameObject SelectionGameObject;
        public GameObject IsLoadingDataRoot;
        public GameObject LoadingErrorRoot;

        private Action<NftItemView> onButtonClickedAction;

        public async void SetData(Nft nft, Action<NftItemView> onButtonClicked)
        {
            if (nft == null)
            {
                return;
            }

            CurrentSolPlayNft = nft;
            Icon.gameObject.SetActive(false);
            LoadingErrorRoot.gameObject.SetActive(false);
            IsLoadingDataRoot.gameObject.SetActive(true);

            IsLoadingDataRoot.gameObject.SetActive(false);

            if (nft.metaplexData.nftImage != null)
            {
                Icon.gameObject.SetActive(true);
                Icon.texture = nft.metaplexData.nftImage.file;
            }

            var nftService = ServiceFactory.Resolve<NftService>();
            
            SelectionGameObject.gameObject.SetActive(nftService.IsNftSelected(nft));
            
            if (nft.metaplexData.data.offchainData != null)
            {
                Description.text = nft.metaplexData.data.offchainData.description;
                Headline.text = nft.metaplexData.data.offchainData.name;
            }
            
            Button.onClick.AddListener(OnButtonClicked);
            onButtonClickedAction = onButtonClicked;
        }

        private void OnButtonClicked()
        {
            onButtonClickedAction?.Invoke(this);
        }
    }
}