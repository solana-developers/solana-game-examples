using System;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildBuildingListItemView : MonoBehaviour
{
    public CostWidget CostWidget;
    public TextMeshProUGUI Name;
    public Button Button;
    public TileConfig CurrentTileConfig;
    
    private Action<BuildBuildingListItemView> onClick;
    
    private void Awake()
    {
        Button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        onClick.Invoke(this);
    }

    public void SetData(TileConfig tileConfig, Action<BuildBuildingListItemView> onClick)
    {
        CurrentTileConfig = tileConfig;
        this.onClick = onClick;
        CostWidget.SetDataBuildCost(tileConfig);
        Name.text = tileConfig.BuildingName;
    }

    private void Update()
    {
        if (CurrentTileConfig == null)
        {
            return;
        }
        CostWidget.SetDataBuildCost(CurrentTileConfig);
    }
}