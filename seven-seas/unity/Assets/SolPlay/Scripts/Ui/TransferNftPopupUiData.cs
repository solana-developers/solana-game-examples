using SolPlay.Orca;
using SolPlay.Scripts.Services;

namespace SolPlay.Scripts.Ui
{
    public class TransferNftPopupUiData : UiService.UiData
    {
        public SolPlayNft NftToTransfer;
        
        public TransferNftPopupUiData(SolPlayNft solPlayNft)
        {
            NftToTransfer = solPlayNft;
        }
    }
}