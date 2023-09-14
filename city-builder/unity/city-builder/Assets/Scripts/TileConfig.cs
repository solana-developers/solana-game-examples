using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(menuName = "Tile Config")]
    public class TileConfig : ScriptableObject
    {
        public uint Number;
        public GameObject Prefab;
        public GameObject MergeFx;
        public Material Material;
        public float ShakeStrength = 0.3f;
        public Color MaterialColor;
        public Material BackgroundMaterial;
        public int Index;
        public byte building_type;
        public string BuildingName;
        public Sprite Icon;
        public bool IsBuildable;
    }
}