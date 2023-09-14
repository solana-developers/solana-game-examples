using DefaultNamespace;
using SolPlay.Scripts.Services;
using SolPlay.Scripts.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen that lets you refill energy for sol 
/// </summary>
public class ChopTreePopup : BasePopup
{
    public Button Button;
    public GameObject LoadingSpinner;
    public TextMeshProUGUI EnergyText;
    
    void Start()
    {
        Button.onClick.AddListener(OnRefillEnergyButtonClicked);
    }

    public override void Open(UiService.UiData uiData)
    {
        var refillUiData = (uiData as ChopTreePopupUiData);

        EnergyText.text = BalancingService.RefillEnergyCost.ToString();
        EnergyText.color = LumberjackService.Instance.CurrentPlayerData.Energy < BalancingService.RefillEnergyCost ? Color.red :  Color.white;

        if (refillUiData == null)
        {
            Debug.LogError("Wrong ui data for nft list popup");
            return;
        }

        base.Open(uiData);
    }

    private void Update()
    {
        EnergyText.color = LumberjackService.Instance.CurrentPlayerData.Energy < BalancingService.RefillEnergyCost ? Color.red :  Color.white;
    }
    
    private async void OnRefillEnergyButtonClicked()
    {
        (uiData as ChopTreePopupUiData).OnClick?.Invoke();
        Close();
    }
}
