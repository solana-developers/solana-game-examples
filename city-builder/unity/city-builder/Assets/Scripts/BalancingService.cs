using Lumberjack.Types;
using UnityEngine;

namespace DefaultNamespace
{
    public class BalancingService
    {

        public class Cost
        {
            public ulong Wood;
            public ulong Stone;
        }
        
        private const int BASE_SAWMILL_WOOD_COST = 10;
        private const int BASE_SAWMILL_STONE_COST = 5;
        private const int BASE_STONE_MINE_WOOD_COST = 5;
        private const int BASE_STONE_MINE_STONE_COST = 10;

        // Define the cost increase multiplier per level for the sawmill and stone mine
        private const float SAWMILL_WOOD_COST_MULTIPLIER = 1.1f; // Increase by 10% per level
        private const float SAWMILL_STONE_COST_MULTIPLIER = 1.05f; // Increase by 5% per level
        private const float STONE_MINE_WOOD_COST_MULTIPLIER = 1.05f; // Increase by 5% per level
        private const float STONE_MINE_STONE_COST_MULTIPLIER = 1.1f; // Increase by 10% per level

        public static ulong RefillEnergyCost = 3;

        public static Cost GetUpgradeCost(TileData tileData)
        {
            Cost newCost = new Cost();
            if (tileData.BuildingType == LumberjackService.BUILDING_TYPE_SAWMILL)
            {
                newCost.Wood = CalculateSawmillWoodUpgradeCost(tileData.BuildingLevel);
                newCost.Stone = CalculateSawmillStoneUpgradeCost(tileData.BuildingLevel);
            } else if (tileData.BuildingType == LumberjackService.BUILDING_TYPE_MINE)
            {
                newCost.Wood = CalculateStoneMineWoodUpgradeCost(tileData.BuildingLevel);
                newCost.Stone = CalculateStoneMineStoneUpgradeCost(tileData.BuildingLevel);
            } else if (tileData.BuildingType == LumberjackService.BUILDING_TYPE_GOOD)
            {
                newCost.Wood = CalculateGoodWoodUpgradeCost(tileData.BuildingLevel);
                newCost.Stone = CalculateGoodStoneUpgradeCost(tileData.BuildingLevel);
            } else if (tileData.BuildingType == LumberjackService.BUILDING_TYPE_EVIL)
            {
                newCost.Wood = CalculateEvilWoodUpgradeCost(tileData.BuildingLevel);
                newCost.Stone = CalculateEvilStoneUpgradeCost(tileData.BuildingLevel);
            }

            return newCost;
        }
        
        public static Cost GetBuildCost(TileConfig tileConfig)
        {
            Cost newCost = new Cost();
            if (tileConfig.building_type == LumberjackService.BUILDING_TYPE_SAWMILL)
            {
                newCost.Wood = CalculateSawmillWoodBuildCost();
                newCost.Stone = CalculateSawmillStoneBuildCost();
            } else if (tileConfig.building_type == LumberjackService.BUILDING_TYPE_MINE)
            {
                newCost.Wood = CalculateStoneMineWoodBuildCost();
                newCost.Stone = CalculateStoneMineStoneBuildCost();
            }

            return newCost;
        }

        public static ulong CalculateSawmillWoodUpgradeCost(uint buildingLevel)
        {
            int sawmillWoodCost = Mathf.RoundToInt(BASE_SAWMILL_WOOD_COST * Mathf.Pow(SAWMILL_WOOD_COST_MULTIPLIER, buildingLevel));
            return (ulong ) sawmillWoodCost;
        }

        public static ulong CalculateSawmillStoneUpgradeCost(uint buildingLevel)
        {
            int sawmillStoneCost = Mathf.RoundToInt(BASE_SAWMILL_STONE_COST * Mathf.Pow(SAWMILL_STONE_COST_MULTIPLIER, buildingLevel));
            return (ulong )sawmillStoneCost;
        }

        public static ulong CalculateStoneMineWoodUpgradeCost(uint buildingLevel)
        {
            int stoneMineWoodCost = Mathf.RoundToInt(BASE_STONE_MINE_WOOD_COST * Mathf.Pow(STONE_MINE_WOOD_COST_MULTIPLIER, buildingLevel));
            return (ulong )stoneMineWoodCost;
        }

        public static ulong CalculateStoneMineStoneUpgradeCost(uint buildingLevel)
        {
            int stoneMineStoneCost = Mathf.RoundToInt(BASE_STONE_MINE_STONE_COST * Mathf.Pow(STONE_MINE_STONE_COST_MULTIPLIER, buildingLevel));
            return (ulong )stoneMineStoneCost;
        }

        public static ulong CalculateGoodStoneUpgradeCost(uint buildingLevel)
        {
            int stoneMineStoneCost = Mathf.RoundToInt(15 * Mathf.Pow(1.1f, buildingLevel));
            return (ulong )stoneMineStoneCost;
        }

        public static ulong CalculateGoodWoodUpgradeCost(uint buildingLevel)
        {
            int stoneMineStoneCost = Mathf.RoundToInt(15 * Mathf.Pow(1.1f, buildingLevel));
            return (ulong )stoneMineStoneCost;
        }

        public static ulong CalculateEvilStoneUpgradeCost(uint buildingLevel)
        {
            int stoneMineStoneCost = Mathf.RoundToInt(15 * Mathf.Pow(1.1f, buildingLevel));
            return (ulong )stoneMineStoneCost;
        }

        public static ulong CalculateEvilWoodUpgradeCost(uint buildingLevel)
        {
            int stoneMineStoneCost = Mathf.RoundToInt(15 * Mathf.Pow(1.1f, buildingLevel));
            return (ulong )stoneMineStoneCost;
        }

        public static ulong CalculateSawmillWoodBuildCost()
        {
            return (ulong ) 0;
        }

        public static ulong CalculateSawmillStoneBuildCost()
        {
            return (ulong ) 15;
        }

        public static ulong CalculateStoneMineWoodBuildCost()
        {
            return (ulong )15;
        }

        public static ulong CalculateStoneMineStoneBuildCost()
        {
            return (ulong )0;
        }
    }
}