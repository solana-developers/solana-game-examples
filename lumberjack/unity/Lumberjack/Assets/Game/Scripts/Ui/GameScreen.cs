using System;
using System.Collections;
using Frictionless;
using Lumberjack.Accounts;
using Solana.Unity.SDK;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This is the screen which handles the interaction with the anchor program.
/// It checks if there is a game account already and has a button to call a function in the program.
/// </summary>
public class GameScreen : MonoBehaviour
{
    public Button ChuckWoodSessionButton;
    public Button NftsButton;
    public Button InitGameDataButton;

    public TextMeshProUGUI EnergyAmountText;
    public TextMeshProUGUI WoodAmountText;
    public TextMeshProUGUI NextEnergyInText;

    public GameObject NotInitializedRoot;
    public GameObject InitializedRoot;
    
    void Start()
    {
        ChuckWoodSessionButton.onClick.AddListener(OnChuckWoodSessionButtonClicked);
        NftsButton.onClick.AddListener(OnNftsButtonClicked);
        InitGameDataButton.onClick.AddListener(OnInitGameDataButtonClicked);
        AnchorService.OnPlayerDataChanged += OnPlayerDataChanged;

        StartCoroutine(UpdateNextEnergy());
        
        AnchorService.OnInitialDataLoaded += UpdateContent;
    }

    private async void OnInitGameDataButtonClicked()
    {
        await AnchorService.Instance.InitGameDataAccount(!Web3.Rpc.NodeAddress.AbsoluteUri.Contains("localhost"));
    }

    private void OnNftsButtonClicked()
    {
        ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.NftListPopup, new NftListPopupUiData(false, Web3.Wallet));
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
        NotInitializedRoot.SetActive(!isInitialized);
        InitGameDataButton.gameObject.SetActive(!isInitialized && AnchorService.Instance.CurrentPlayerData == null);
        InitializedRoot.SetActive(isInitialized);

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
}
