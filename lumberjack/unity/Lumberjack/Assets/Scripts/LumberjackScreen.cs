using System;
using System.Collections;
using Frictionless;
using Lumberjack.Accounts;
using Solana.Unity.SDK;
using SolPlay.Scripts.Services;
using SolPlay.Scripts.Ui;
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

    public TextMeshProUGUI EnergyAmountText;
    public TextMeshProUGUI WoodAmountText;
    public TextMeshProUGUI NextEnergyInText;

    public GameObject LoggedInRoot;
    public GameObject NotInitializedRoot;
    public GameObject InitializedRoot;
    public GameObject NotLoggedInRoot;

    public PlayerData CurrentPlayerData;

    void Start()
    {
        LoggedInRoot.SetActive(false);
        NotLoggedInRoot.SetActive(true);
        
        LoginButton.onClick.AddListener(OnLoginClicked);
        LoginWalletAdapterButton.onClick.AddListener(OnLoginWalletAdapterButtonClicked);
        ChuckWoodButton.onClick.AddListener(OnChuckWoodButtonClicked);
        ChuckWoodSessionButton.onClick.AddListener(OnChuckWoodSessionButtonClicked);
        RevokeSessionButton.onClick.AddListener(OnRevokeSessionButtonClicked);
        NftsButton.onClick.AddListener(OnNftsButtonnClicked);
        InitGameDataButton.onClick.AddListener(OnIitGameDataButtonClicked);
        LumberjackService.OnPlayerDataChanged += OnPlayerDataChanged;

        StartCoroutine(UpdateNextEnergy());
        
        LumberjackService.OnInitialDataLoaded += UpdateContent;
    }

    private async void OnIitGameDataButtonClicked()
    {
        await LumberjackService.Instance.InitGameDataAccount();
    }

    private void OnNftsButtonnClicked()
    {
        ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.NftListPopup, new NftListPopupUiData(false, Web3.Wallet));
    }

    private void UpdateContent()
    {
        var isInitialized = LumberjackService.Instance.IsInitialized();
        LoggedInRoot.SetActive(Web3.Account != null);
        NotInitializedRoot.SetActive(!isInitialized);
        InitializedRoot.SetActive(isInitialized);

        NotLoggedInRoot.SetActive(Web3.Account == null);
    }

    private async void OnRevokeSessionButtonClicked()
    {
        var res =  await LumberjackService.Instance.RevokeSession();
        Debug.Log("Revoked Session: " + res.Account);
    }

    private async void OnLoginWalletAdapterButtonClicked()
    {
        await Web3.Instance.LoginWalletAdapter();
    }

    private IEnumerator UpdateNextEnergy()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            UpdateContent(CurrentPlayerData);
        }
    }

    private void OnPlayerDataChanged(PlayerData playerData)
    {
        CurrentPlayerData = playerData;
        UpdateContent(playerData);
    }

    private void UpdateContent(PlayerData playerData)
    {
        if (CurrentPlayerData == null)
        {
            return;
        }
        var lastLoginTime = playerData.LastLogin;
        var timePassed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastLoginTime;
        
        while (
            timePassed >= LumberjackService.TIME_TO_REFILL_ENERGY &&
            playerData.Energy < LumberjackService.MAX_ENERGY
        ) {
            playerData.Energy += 1;
            playerData.LastLogin += LumberjackService.TIME_TO_REFILL_ENERGY;
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
        
        EnergyAmountText.text = playerData.Energy.ToString();
        WoodAmountText.text = playerData.Wood.ToString();
    }

    private async void OnChuckWoodSessionButtonClicked()
    {
        var res =  await LumberjackService.Instance.ChopTree(true);
        Debug.Log("Request: " + res.RawRpcRequest);
        Debug.Log("Response: " + res.RawRpcResponse);
    }

    private async void OnChuckWoodButtonClicked()
    {
       var res =  await LumberjackService.Instance.ChopTree(false);
        Debug.Log(res.Result);
    }

    private async void OnLoginClicked()
    {
        // Dont use this one for production.
        await Web3.Instance.LoginInGameWallet("1234");
    }
}
