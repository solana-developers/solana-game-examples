using System;
using DefaultNamespace;
using Lumberjack.Types;
using Solana.Unity.SDK;
using SolPlay.Scripts.Services;

public class BuildBuildingPopupUiData : UiService.UiData
{
    public WalletBase Wallet;
    public Action<TileConfig> OnClick;
    public TileData TileData;

    public BuildBuildingPopupUiData(WalletBase wallet, Action<TileConfig> onClick, TileData tileData)
    {
        OnClick = onClick;
        Wallet = wallet;
        TileData = tileData;
    }
}
