using Solana.Unity.SDK;
using SolPlay.Scripts.Services;

public class NftListPopupUiData : UiService.UiData
{
    public bool RequestNfts;
    public WalletBase Wallet;

    public NftListPopupUiData(bool requestNfts, WalletBase wallet)
    {
        RequestNfts = requestNfts;
        Wallet = wallet;
    }
}
