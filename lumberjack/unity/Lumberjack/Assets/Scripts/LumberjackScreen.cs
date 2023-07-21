using System;
using System.Collections;
using Frictionless;
using Lumberjack.Accounts;
using Solana.Unity.SDK;
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

    public TextMeshProUGUI EnergyAmountText;
    public TextMeshProUGUI WoodAmountText;
    public TextMeshProUGUI NextEnergyInText;

    public GameObject LoadingSpinner;

    public GameObject LoggedInRoot;
    public GameObject NotInitializedRoot;
    public GameObject InitializedRoot;
    public GameObject NotLoggedInRoot;
    
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
        await LumberjackService.Instance.InitGameDataAccount();
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
        InitGameDataButton.gameObject.SetActive(isInitialized && LumberjackService.Instance.CurrentPlayerData == null);
        InitializedRoot.SetActive(isInitialized);

        NotLoggedInRoot.SetActive(Web3.Account == null);

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
        WoodAmountText.text = LumberjackService.Instance.CurrentPlayerData.Wood.ToString();
    }

    private void OnChuckWoodSessionButtonClicked()
    {
        LumberjackService.Instance.ChopTree(true);
    }

    private void OnChuckWoodButtonClicked()
    {
       LumberjackService.Instance.ChopTree(false);
    }

    private async void OnEditorLoginClicked()
    {
        // Dont use this one for production.
        await Web3.Instance.LoginInGameWallet("1234");
    }
}
