using System;
using Cysharp.Threading.Tasks;
using Frictionless;
#if GLTFAST
using GLTFast;
#endif
using SolPlay.Scripts.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Scripts.Ui
{
    /// <summary>
    /// Show the image and the power level of a given Nft and can have a click handler
    /// </summary>
    public class NftItemView : MonoBehaviour
    {
        public SolPlayNft CurrentSolPlayNft;
        public RawImage Icon;
        public TextMeshProUGUI Headline;
        public TextMeshProUGUI Description;
        public TextMeshProUGUI PowerLevel;
        public TextMeshProUGUI ErrorText;
        public Button Button;
        public GameObject SelectionGameObject;
        public GameObject GltfRoot;
        public GameObject IsLoadingDataRoot;
        public GameObject LoadingErrorRoot;
#if GLTFAST
        public GltfAsset GltfAsset;
#endif
        public RenderTexture RenderTexture;
        public Camera Camera;
        public int RenderTextureSize = 75;

        [Tooltip(
            "Loading the 3D nfts from the animation url. Note thought that i can be a bit slow to load and the app may stuck a bit when instantiating ")]
        public bool Load3DNfts;

        private Action<NftItemView> onButtonClickedAction;

        public async void SetData(SolPlayNft solPlayNft, Action<NftItemView> onButtonClicked)
        {
            if (solPlayNft == null)
            {
                return;
            }

            CurrentSolPlayNft = solPlayNft;
            Icon.gameObject.SetActive(false);
            GltfRoot.SetActive(false);
            LoadingErrorRoot.gameObject.SetActive(false);
            IsLoadingDataRoot.gameObject.SetActive(true);
            PowerLevel.text = "Loading Image";

            if (solPlayNft.LoadingImageTask != null)
            {
                await solPlayNft.LoadingImageTask;
            }
            
            if (!string.IsNullOrEmpty(solPlayNft.LoadingError))
            {
                ErrorText.text = solPlayNft.LoadingError;
                LoadingErrorRoot.gameObject.SetActive(true);
                return;
            }

            IsLoadingDataRoot.gameObject.SetActive(false);

            if (Load3DNfts && !string.IsNullOrEmpty(solPlayNft.MetaplexData.data.json.animation_url))
            {
                Icon.gameObject.SetActive(true);
                GltfRoot.SetActive(true);
                RenderTexture = new RenderTexture(RenderTextureSize, RenderTextureSize, 1);
                Camera.targetTexture = RenderTexture;
                Camera.cullingMask = (1 << 19);
                Icon.texture = RenderTexture;
#if GLTFAST
                var isLoaded = await GltfAsset.Load(solPlayNft.MetaplexData.data.json.animation_url);
                if (isLoaded)
                {
                    if (!GltfAsset)
                    {
                        // In case it was destroyed while loading
                        return;
                    }

                    LayerUtils.SetRenderLayerRecursive(GltfAsset.gameObject, 19);
                }
#endif
            }
            else if (solPlayNft.MetaplexData.nftImage != null)
            {
                Icon.gameObject.SetActive(true);
                Icon.texture = solPlayNft.MetaplexData.nftImage.file;
            }

            var nftService = ServiceFactory.Resolve<NftService>();
            
            SelectionGameObject.gameObject.SetActive(nftService.IsNftSelected(solPlayNft));
            
            if (solPlayNft.MetaplexData.data.json != null)
            {
                Description.text = solPlayNft.MetaplexData.data.json.description;
            }

            Headline.text = solPlayNft.MetaplexData.data.name;
            var nftPowerLevelService = ServiceFactory.Resolve<HighscoreService>();

            if (nftPowerLevelService.TryGetHighscoreForSeed(solPlayNft.MetaplexData.mint, out HighscoreEntry highscoreEntry))
            {
                PowerLevel.text = $"Score: {highscoreEntry.Highscore}";
            }
            else
            {
                PowerLevel.text = solPlayNft.MetaplexData.data.name;
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