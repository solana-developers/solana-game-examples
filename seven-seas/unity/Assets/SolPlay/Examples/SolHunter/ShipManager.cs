using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks.Triggers;
using Frictionless;
using SevenSeas.Types;
using UnityEngine;

public class ShipManager : MonoBehaviour
{
    public Ship ShipPrefab;
    public Dictionary<string, Ship> Ships = new Dictionary<string, Ship>();
    public Tile[][] Board { get; set; }

    private void Start()
    {
        MessageRouter.AddHandler<SolHunterService.SolHunterGameDataChangedMessage>(OnGameDataChangedMessage);   
        MessageRouter.AddHandler<SolHunterService.ShipShotMessage>(OnShipShotMessage);   
    }

    private void OnShipShotMessage(SolHunterService.ShipShotMessage obj)
    {
        if (Ships.ContainsKey(obj.ShipOwner))
        {
            Ships[obj.ShipOwner].Shoot();
        }
    }

    private void OnGameDataChangedMessage(SolHunterService.SolHunterGameDataChangedMessage obj)
    {
        InitWithData(obj.GameDataAccount.Board);
    }

    public void InitWithData(Tile[][] Board)
    {
        var length = Board.GetLength(0);

        for (int y = 0; y < length; y++)
        {
            for (int x = 0; x < length; x++)
            {
                Tile tile = Board[x][y];
                if (tile.State == SolHunterService.STATE_PLAYER)
                {
                    if (!Ships.ContainsKey(tile.Player))
                    {
                        var newShip = SpawnShip(new Vector2(x, -y));
                        Ships.Add(tile.Player, newShip);
                    }
                    else
                    {
                        Ships[tile.Player].SetNewTargetPosition(new Vector2(x, -y));
                    }
                }
            }
        }
        
        
    }

    private Ship SpawnShip(Vector2 startPosition)
    {
        var newShip = Instantiate(ShipPrefab);
        newShip.Init(startPosition);
        return newShip;
    }
}