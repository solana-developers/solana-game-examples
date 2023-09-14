using System;
using DefaultNamespace;
using Frictionless;
using SolPlay.Scripts.Services;
using SolPlay.Scripts.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeBuildingPopup : BasePopup
{
    public Button Button;
    public GameObject LoadingSpinner;
    public CostWidget CostWidget;
    public TextMeshProUGUI BuildingNameText;
    
    void Start()
    {
        Button.onClick.AddListener(ButtonClicked);
    }

    public override void Open(UiService.UiData uiData)
    {
        var upgradeBuildingUiData = (uiData as UpgradeBuildingPopupUiData);

        Update();
        var tileConfig = ServiceFactory.Resolve<BoardManager>().FindTileConfigByTileData(upgradeBuildingUiData.TileData);
        BuildingNameText.text = tileConfig.BuildingName;
        
        if (upgradeBuildingUiData == null)
        {
            Debug.LogError("Wrong ui data for nft list popup");
            return;
        }

        base.Open(uiData);
    }

    private void Update()
    {
        var upgradeBuildingUiData = (uiData as UpgradeBuildingPopupUiData);
        if (upgradeBuildingUiData != null)
        {
            CostWidget.SetDataUpgradeCost(upgradeBuildingUiData.TileData);   
        }
    }

    private async void ButtonClicked()
    {
        var refillUiData = (uiData as UpgradeBuildingPopupUiData);

        if (LumberjackService.HasEnoughResources(BalancingService.GetUpgradeCost(refillUiData.TileData)))
        {
            (uiData as UpgradeBuildingPopupUiData).OnClick?.Invoke();
            Close();
        }else
        {
            Debug.LogError("Not enough resources");
        }
    }
}
