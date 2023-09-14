using Lumberjack.Types;
using TMPro;
using UnityEngine;

namespace DefaultNamespace
{
    public class CostWidget : MonoBehaviour
    {
        public TextMeshProUGUI WoodCost;
        public TextMeshProUGUI StoneCost;
        public TextMeshProUGUI NotEnoughResources;

        public void SetDataUpgradeCost(TileData tiledata)
        {
            BalancingService.Cost upgradeCost = BalancingService.GetUpgradeCost(tiledata);

            SetCost(upgradeCost);
        }

        public void SetDataBuildCost(TileConfig config)
        {
            BalancingService.Cost cost = BalancingService.GetBuildCost(config);
            SetCost(cost);
        }
        
        private void SetCost(BalancingService.Cost upgradeCost)
        {
            WoodCost.text = upgradeCost.Wood.ToString();
            StoneCost.text = upgradeCost.Stone.ToString();

            var hasEnoughWood = upgradeCost.Wood <= LumberjackService.Instance.CurrentBoardAccount.Wood;
            WoodCost.color = hasEnoughWood ? Color.white : Color.red;
            var hasEnoughStone = upgradeCost.Stone <= LumberjackService.Instance.CurrentBoardAccount.Stone;
            StoneCost.color = hasEnoughStone ? Color.white : Color.red;

            NotEnoughResources.gameObject.SetActive(!LumberjackService.HasEnoughResources(upgradeCost));
        }
    }
}