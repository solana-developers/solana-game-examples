using DefaultNamespace;
using Solana.Unity.SDK.Nft;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Scripts.Ui
{
    /// <summary>
    /// A little animated text on the screen, that disappears after some time. 
    /// </summary>
    public class TextBlimp3D : MonoBehaviour
    {
        public TextMeshProUGUI Text;
        public NftItemView NftItemView;
        public Image Icon;

        public void SetData(string text, Nft nft, TileConfig tileConfig)
        {
            Text.text = text;
            if (nft != null)
            {
                NftItemView.SetData(nft, view => {});   
            }
            Icon.sprite = tileConfig.Icon;
        }
    }
}