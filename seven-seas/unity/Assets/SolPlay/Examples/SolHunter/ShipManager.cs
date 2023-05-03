using System.Collections.Generic;
using Frictionless;
using SevenSeas.Types;
using UnityEngine;

public class ShipManager : MonoBehaviour
{
    public Ship ShipPrefab;
    public TreasureChest TreasureChestPrefab;
    public Dictionary<string, Ship> Ships = new Dictionary<string, Ship>();
    public Dictionary<string, TreasureChest> Chests = new Dictionary<string, TreasureChest>();
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

    public void InitWithData(Tile[][] board)
    {
        var length = board.GetLength(0);

        for (int y = 0; y < length; y++)
        {
            for (int x = 0; x < length; x++)
            {
                Tile tile = board[x][y];
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

                if (tile.State == SolHunterService.STATE_CHEST)
                {
                    var key = x + "_" + y;
                    if (!Chests.ContainsKey(key))
                    {
                        var newChest = SpawnTreasuryChest(new Vector2(x, -y));
                        Chests.Add(key, newChest);
                    }
                }
            }
        }

        DestroyAllShipsThatAreNotOnTheBoard(board);
        DestroyAllChestsThatAreNotOnTheBoard(board);
    }

    private void DestroyAllShipsThatAreNotOnTheBoard(Tile[][] board)
    {
        var length = board.GetLength(0);

        List<KeyValuePair<string, Ship>> deadShips = new List<KeyValuePair<string, Ship>>();
        
        foreach (KeyValuePair<string, Ship> ship in Ships)
        {
            bool found = false;
            for (int y = 0; y < length; y++)
            {
                for (int x = 0; x < length; x++)
                {
                    Tile tile = board[x][y];
                    if (tile.State == SolHunterService.STATE_PLAYER && tile.Player == ship.Key)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
            }

            if (!found)
            {
                deadShips.Add(ship);
            }
        }

        foreach (var ship in deadShips) 
        {
            Destroy(ship.Value.gameObject);
            Ships.Remove(ship.Key);
        }
    }

    private void DestroyAllChestsThatAreNotOnTheBoard(Tile[][] board)
    {
        var length = board.GetLength(0);

        List<KeyValuePair<string, TreasureChest>> deadChests = new List<KeyValuePair<string, TreasureChest>>();
        
        foreach (KeyValuePair<string, TreasureChest> chest in Chests)
        {
            bool found = false;
            for (int y = 0; y < length; y++)
            {
                for (int x = 0; x < length; x++)
                {
                    Tile tile = board[x][y];
                    if (tile.State == SolHunterService.STATE_CHEST && x+"_"+y == chest.Key)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
            }

            if (!found)
            {
                deadChests.Add(chest);
            }
        }

        foreach (var chest in deadChests) 
        {
            Destroy(chest.Value.gameObject);
            Chests.Remove(chest.Key);
        }
    }

    private Ship SpawnShip(Vector2 startPosition)
    {
        var newShip = Instantiate(ShipPrefab);
        newShip.Init(startPosition);
        return newShip;
    }
    
    private TreasureChest SpawnTreasuryChest(Vector2 startPosition)
    {
        var newChest = Instantiate(TreasureChestPrefab);
        newChest.Init(startPosition);
        return newChest;
    }
}