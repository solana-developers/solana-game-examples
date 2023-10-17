using System;
using System.Collections;
using Frictionless;
using Lumberjack.Accounts;
using Solana.Unity.SDK;
using Solana.Unity.Wallet.Bip39;
using Services;
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
        AnchorService.OnPlayerDataChanged += OnPlayerDataChanged;

        StartCoroutine(UpdateNextEnergy());
        
        AnchorService.OnInitialDataLoaded += UpdateContent;
    }

    private void Update()
    {
        LoadingSpinner.gameObject.SetActive(AnchorService.Instance.IsAnyTransactionInProgress);
    }

    private async void OnInitGameDataButtonClicked()
    {
        await AnchorService.Instance.InitGameDataAccount(!Web3.Rpc.NodeAddress.AbsoluteUri.Contains("localhost"));
    }

    private void OnNftsButtonnClicked()
    {
        ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.NftListPopup, new NftListPopupUiData(false, Web3.Wallet));
    }

    private async void OnRevokeSessionButtonClicked()
    {
        var res =  await AnchorService.Instance.RevokeSession();
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
        var isInitialized = AnchorService.Instance.IsInitialized();
        LoggedInRoot.SetActive(Web3.Account != null);
        NotInitializedRoot.SetActive(!isInitialized);
        InitGameDataButton.gameObject.SetActive(!isInitialized && AnchorService.Instance.CurrentPlayerData == null);
        InitializedRoot.SetActive(isInitialized);

        NotLoggedInRoot.SetActive(Web3.Account == null);

        if (AnchorService.Instance.CurrentPlayerData == null)
        {
            return;
        }
        
        var lastLoginTime = AnchorService.Instance.CurrentPlayerData.LastLogin;
        var timePassed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastLoginTime;
        
        while (
            timePassed >= AnchorService.TIME_TO_REFILL_ENERGY &&
            AnchorService.Instance.CurrentPlayerData.Energy < AnchorService.MAX_ENERGY
        ) {
            AnchorService.Instance.CurrentPlayerData.Energy += 1;
            AnchorService.Instance.CurrentPlayerData.LastLogin += AnchorService.TIME_TO_REFILL_ENERGY;
            timePassed -= AnchorService.TIME_TO_REFILL_ENERGY;
        }

        var timeUntilNextRefill = AnchorService.TIME_TO_REFILL_ENERGY - timePassed;

        if (timeUntilNextRefill > 0)
        {
            NextEnergyInText.text = timeUntilNextRefill.ToString();
        }
        else
        {
            NextEnergyInText.text = "";
        }
        
        EnergyAmountText.text = AnchorService.Instance.CurrentPlayerData.Energy.ToString();
        WoodAmountText.text = AnchorService.Instance.CurrentPlayerData.Wood.ToString();
    }

    private void OnChuckWoodSessionButtonClicked()
    {
        AnchorService.Instance.ChopTree(!Web3.Rpc.NodeAddress.AbsoluteUri.Contains("localhost"));
    }

    private void OnChuckWoodButtonClicked()
    {
       AnchorService.Instance.ChopTree(false);
    }

    private async void OnEditorLoginClicked()
    {
        var newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);

        // Dont use this one for production.
        var account = await Web3.Instance.LoginInGameWallet("1234") ??
                      await Web3.Instance.CreateAccount(newMnemonic.ToString(), "1234");

    }
}
