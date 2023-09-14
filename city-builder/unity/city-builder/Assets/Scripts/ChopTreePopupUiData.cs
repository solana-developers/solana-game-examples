using System;
using Solana.Unity.SDK;
using SolPlay.Scripts.Services;

public class ChopTreePopupUiData : UiService.UiData
{
    public WalletBase Wallet;
    public Action OnClick;
    
    public ChopTreePopupUiData(WalletBase wallet, Action onClick)
    {
        Wallet = wallet;
        OnClick = onClick; 
    }
}
