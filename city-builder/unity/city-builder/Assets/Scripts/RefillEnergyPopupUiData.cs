using Solana.Unity.SDK;
using SolPlay.Scripts.Services;

public class RefillEnergyPopupUiData : UiService.UiData
{
    public WalletBase Wallet;

    public RefillEnergyPopupUiData(WalletBase wallet)
    {
        Wallet = wallet;
    }
}
