using System.Collections.Generic;
using SolPlay.Scripts.Services;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public int X { private set; get; }
    public int Y { private set; get; }
    public Tile Tile;
    public MeshRenderer MeshRenderer;
    public List<Material> Materials;
    
    public void Init(int x, int y, Tile tile)
    {
        X = x;
        Y = y;
        Tile = tile;
        MeshRenderer.material = Materials[Random.Range(0, Materials.Count)];
    }

    private void OnMouseUp()
    {
        if (UiService.IsPointerOverUIObject())
        {
            return;
        }
        LumberjackService.Instance.OnCellClicked((byte)X, (byte) Y);
    }

    public bool IsEmpty()
    {
        return Tile == null;
    }
    
    public bool IsOccupied()
    {
        return Tile != null;
    }
}
