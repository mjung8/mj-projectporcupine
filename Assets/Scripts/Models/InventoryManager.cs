using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager
{
    public Dictionary<string, List<Inventory>> inventories;

    public InventoryManager()
    {
        inventories = new Dictionary<string, List<Inventory>>();
    }

    public bool PlaceInventory(Tile tile, Inventory inv)
    {
        bool tileWasEmpty = tile.Inventory == null;

        if (tile.PlaceInventory(inv) == false)
        {
            // The tile did not accept the inv item for some reason
            return false;
        }

        // inv might be an empty stack if it was merged to another stack
        if (inv.stackSize == 0)
        {
            if (inventories.ContainsKey(tile.Inventory.objectType))
            {
                inventories[inv.objectType].Remove(inv);
            }
        }

        // may have also created a new stack on the tile if the tile was previously empty
        if (tileWasEmpty)
        {
            if (inventories.ContainsKey(tile.Inventory.objectType) == false)
            {
                inventories[tile.Inventory.objectType] = new List<Inventory>();
            }

            inventories[tile.Inventory.objectType].Add(tile.Inventory);
        }

        return true;
    }
}
