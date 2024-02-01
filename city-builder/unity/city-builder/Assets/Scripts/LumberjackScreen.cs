using System;
using System.Collections;
using Frictionless;
using Lumberjack.Accounts;
using Solana.Unity.SDK;
using Solana.Unity.Wallet.Bip39;
using SolPlay.Scripts.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LumberjackScreen : MonoBehaviour
{
    public Button LoginButton;
    public Button LoginWalletAdapterButton;
    public Button ChuckWoodButton;
    public Button ChuckWoodSessionButton;
    public Button RevokeSessionButton;
    public Button NftsButton;
    public Button InitGameDataButton;
    public Button RestartButton;

    public TextMeshProUGUI EnergyAmountText;
    public TextMeshProUGUI WoodAmountText;
    public TextMeshProUGUI StoneAmountText;
    public TextMeshProUGUI NextEnergyInText;
    public TextMeshProUGUI GameOverText;

    public GameObject LoadingSpinner;

    public GameObject LoggedInRoot;
    public GameObject NotInitializedRoot;
    public GameObject InitializedRoot;
    public GameObject NotLoggedInRoot;
    public GameObject GameOverRoot;
    public GameObject EvilWonRoot;
    public GameObject GoodWonRoot;
    
    void Start()
    {
        LoggedInRoot.SetActive(false);
        NotLoggedInRoot.SetActive(true);
        
        LoginButton.onClick.AddListener(OnEditorLoginClicked);
        LoginWalletAdapterButton.onClick.AddListener(OnLoginWalletAdapterButtonClicked);
        ChuckWoodButton.onClick.AddListener(OnChuckWoodButtonClicked);
        ChuckWoodSessionButton.onClick.AddListener(OnChuckWoodSessionButtonClicked);
        RevokeSessionButton.onClick.AddListener(OnRevokeSessionButtonClicked);
        NftsButton.onClick.AddListener(OnNftsButtonnClicked);
        InitGameDataButton.onClick.AddListener(OnInitGameDataButtonClicked);
        RestartButton.onClick.AddListener(OnRestartGameClicked);
        LumberjackService.OnPlayerDataChanged += OnPlayerDataChanged;

        StartCoroutine(UpdateNextEnergy());
        
        LumberjackService.OnInitialDataLoaded += UpdateContent;
    }

    private void Update()
    {
        LoadingSpinner.gameObject.SetActive(LumberjackService.Instance.IsAnyTransactionInProgress);
    }

    private async void OnInitGameDataButtonClicked()
    {
        await LumberjackService.Instance.InitGameDataAccount(!Web3.Rpc.NodeAddress.AbsoluteUri.Contains("localhost"));
    }

    private async void OnRestartGameClicked()
    {
        await LumberjackService.Instance.RestartGame();
    }

    private void OnNftsButtonnClicked()
    {
        ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.NftListPopup, new NftListPopupUiData(false, Web3.Wallet));
    }

    private async void OnRevokeSessionButtonClicked()
    {
        var res =  await LumberjackService.Instance.RevokeSession();
        Debug.Log("Revoked Session: " + res.Account);
    }

    private async void OnLoginWalletAdapterButtonClicked()
    {
        Web3.Instance.customRpc = "https://light-red-uranium.solana-mainnet.quiknode.pro/9d63f34cfe3e6e00543a34ff3f19855e537f0a99/";
        Web3.Instance.webSocketsRpc = "https://light-red-uranium.solana-mainnet.quiknode.pro/9d63f34cfe3e6e00543a34ff3f19855e537f0a99/";
        await Web3.Instance.LoginWalletAdapter();
    }

    private IEnumerator UpdateNextEnergy()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            UpdateContent();
        }
    }

    private void OnPlayerDataChanged(PlayerData playerData)
    {
        UpdateContent();
    }

    private void UpdateContent()
    {
        var isInitialized = LumberjackService.Instance.IsInitialized();
        LoggedInRoot.SetActive(Web3.Account != null);
        NotInitializedRoot.SetActive(!isInitialized);
        InitGameDataButton.gameObject.SetActive(!isInitialized && LumberjackService.Instance.CurrentPlayerData == null);
        InitializedRoot.SetActive(isInitialized);
            
        NotLoggedInRoot.SetActive(Web3.Account == null);

        if (LumberjackService.Instance.CurrentBoardAccount != null)
        {
            GameOverRoot.SetActive(LumberjackService.Instance.CurrentBoardAccount.EvilWon || LumberjackService.Instance.CurrentBoardAccount.GoodWon);
            if (LumberjackService.Instance.CurrentBoardAccount.EvilWon)
            {
                GameOverText.text = "Game Over! Evil Won!";
                EvilWonRoot.gameObject.SetActive(true);
                GoodWonRoot.gameObject.SetActive(false);
            } else if (LumberjackService.Instance.CurrentBoardAccount.GoodWon)
            {
                GameOverText.text = "Game Over! Good Won!";
                EvilWonRoot.gameObject.SetActive(false);
                GoodWonRoot.gameObject.SetActive(true);
            }
        }
        
        if (LumberjackService.Instance.CurrentPlayerData == null)
        {
            return;
        }
        
        var lastLoginTime = LumberjackService.Instance.CurrentPlayerData.LastLogin;
        var timePassed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastLoginTime;
        
        while (
            timePassed >= LumberjackService.TIME_TO_REFILL_ENERGY &&
            LumberjackService.Instance.CurrentPlayerData.Energy < LumberjackService.MAX_ENERGY
        ) {
            LumberjackService.Instance.CurrentPlayerData.Energy += 1;
            LumberjackService.Instance.CurrentPlayerData.LastLogin += LumberjackService.TIME_TO_REFILL_ENERGY;
            timePassed -= LumberjackService.TIME_TO_REFILL_ENERGY;
        }

        var timeUntilNextRefill = LumberjackService.TIME_TO_REFILL_ENERGY - timePassed;

        if (timeUntilNextRefill > 0)
        {
            NextEnergyInText.text = timeUntilNextRefill.ToString();
        }
        else
        {
            NextEnergyInText.text = "";
        }
        
        EnergyAmountText.text = LumberjackService.Instance.CurrentPlayerData.Energy.ToString();
        if (LumberjackService.Instance.CurrentBoardAccount != null)
        {
            WoodAmountText.text = LumberjackService.Instance.CurrentBoardAccount.Wood.ToString();
            StoneAmountText.text = LumberjackService.Instance.CurrentBoardAccount.Stone.ToString();
        }
    }

    private void OnChuckWoodSessionButtonClicked()
    {
        LumberjackService.Instance.ChopTree(!Web3.Rpc.NodeAddress.AbsoluteUri.Contains("localhost"), 1 ,0);
    }

    private void OnChuckWoodButtonClicked()
    {
       LumberjackService.Instance.OnCellClicked(0 ,1);
    }

    private async void OnEditorLoginClicked()
    {
        // Dont use this one for production.
        var newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);

        // Dont use this one for production. Its only ment for editor login
        var account = await Web3.Instance.LoginInGameWallet("1234") ??
                      await Web3.Instance.CreateAccount(newMnemonic.ToString(), "1234");
    }
}
