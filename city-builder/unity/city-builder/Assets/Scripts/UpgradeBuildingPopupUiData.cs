using System;
using Lumberjack.Types;
using Solana.Unity.SDK;
using SolPlay.Scripts.Services;

public class UpgradeBuildingPopupUiData : UiService.UiData
{
    public WalletBase Wallet;
    public Action OnClick;
    public TileData TileData;

    public UpgradeBuildingPopupUiData(WalletBase wallet, Action onClick, TileData tileData)
    {
        Wallet = wallet;
        OnClick = onClick;
        TileData = tileData;
    }
}
