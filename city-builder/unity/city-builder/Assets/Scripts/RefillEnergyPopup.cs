using SolPlay.Scripts.Services;
using SolPlay.Scripts.Ui;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen that lets you refill energy for sol 
/// </summary>
public class RefillEnergyPopup : BasePopup
{
    public Button RefillEnergyButton;
    public GameObject LoadingSpinner;
    
    void Start()
    {
        RefillEnergyButton.onClick.AddListener(OnRefillEnergyButtonClicked);
    }

    public override void Open(UiService.UiData uiData)
    {
        var refillUiData = (uiData as RefillEnergyPopupUiData);

        if (refillUiData == null)
        {
            Debug.LogError("Wrong ui data for nft list popup");
            return;
        }

        base.Open(uiData);
    }

    private async void OnRefillEnergyButtonClicked()
    {
        LoadingSpinner.gameObject.SetActive(true);
        await LumberjackService.Instance.RefillEnergy();
        LoadingSpinner.gameObject.SetActive(false);
        Close();
    }
}
