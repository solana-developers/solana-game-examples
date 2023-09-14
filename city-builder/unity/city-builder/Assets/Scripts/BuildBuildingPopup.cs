using DefaultNamespace;
using Frictionless;
using SolPlay.Scripts.Services;
using SolPlay.Scripts.Ui;
using UnityEngine;
using UnityEngine.UI;

public class BuildBuildingPopup : BasePopup
{
    public GameObject LoadingSpinner;
    public GameObject ListRoot;
    public BuildBuildingListItemView BuildBuildingListItemViewPrefab;

    public override void Open(UiService.UiData uiData)
    {
        var refillUiData = (uiData as BuildBuildingPopupUiData);

        foreach (Transform trans in ListRoot.transform)
        {
            Destroy(trans.gameObject);
        }
        
        foreach (var config in ServiceFactory.Resolve<BoardManager>().tileConfigs)
        {
            if (config.IsBuildable)
            {
                var newListItem = Instantiate(BuildBuildingListItemViewPrefab, ListRoot.transform);
                newListItem.SetData(config, view =>
                {
                    if (LumberjackService.HasEnoughResources(BalancingService.GetBuildCost(config)))
                    {
                        (uiData as BuildBuildingPopupUiData).OnClick(config);
                        Close();
                    }
                });
            }
        }
        
        if (refillUiData == null)
        {
            Debug.LogError("Wrong ui data for nft list popup");
            return;
        }

        base.Open(uiData);
    }
}