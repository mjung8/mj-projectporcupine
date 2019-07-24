using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[MoonSharpUserData]
public class InventoryManager
{
    public Dictionary<string, List<Inventory>> inventories;

    public InventoryManager()
    {
        inventories = new Dictionary<string, List<Inventory>>();
    }

    void CleanupInventory(Inventory inv)
    {
        // inv might be an empty stack if it was merged to another stack
        if (inv.stackSize == 0)
        {
            if (inventories.ContainsKey(inv.objectType))
            {
                inventories[inv.objectType].Remove(inv);
            }
            if (inv.tile != null)
            {
                inv.tile.inventory = null;
                inv.tile = null;
            }
            if (inv.character != null)
            {
                inv.character.inventory = null;
                inv.character = null;
            }
        }
    }

    public bool PlaceInventory(Tile tile, Inventory inv)
    {
        bool tileWasEmpty = tile.inventory == null;

        if (tile.PlaceInventory(inv) == false)
        {
            // The tile did not accept the inv item for some reason
            return false;
        }

        CleanupInventory(inv);

        // may have also created a new stack on the tile if the tile was previously empty
        if (tileWasEmpty)
        {
            if (inventories.ContainsKey(tile.inventory.objectType) == false)
            {
                inventories[tile.inventory.objectType] = new List<Inventory>();
            }

            inventories[tile.inventory.objectType].Add(tile.inventory);

            World.Current.OnInventoryCreated(tile.inventory);
        }

        return true;
    }

    public bool PlaceInventory(Job job, Inventory inv)
    {
        if (job.inventoryRequirements.ContainsKey(inv.objectType) == false)
        {
            Debug.LogError("Trying to add inv to a job that it doesn't want.");
            return false;
        }

        job.inventoryRequirements[inv.objectType].stackSize += inv.stackSize;
        if (job.inventoryRequirements[inv.objectType].maxStackSize < job.inventoryRequirements[inv.objectType].stackSize)
        {
            inv.stackSize = job.inventoryRequirements[inv.objectType].stackSize - job.inventoryRequirements[inv.objectType].maxStackSize;
            job.inventoryRequirements[inv.objectType].stackSize = job.inventoryRequirements[inv.objectType].maxStackSize;
        }
        else
        {
            inv.stackSize = 0;
        }

        CleanupInventory(inv);

        return true;
    }

    public bool PlaceInventory(Character character, Inventory sourceInventory, int amount = -1)
    {
        if (amount < 0)
        {
            amount = sourceInventory.stackSize;
        }
        else
        {
            amount = Mathf.Min(amount, sourceInventory.stackSize);
        }

        if (character.inventory == null)
        {
            character.inventory = sourceInventory.Clone();
            character.inventory.stackSize = 0;
            inventories[character.inventory.objectType].Add(character.inventory);
        }
        else if (character.inventory.objectType != sourceInventory.objectType)
        {
            Debug.LogError("Character is trying to pick up a mismatched inventory object type.");
            return false;
        }

        character.inventory.stackSize += amount;

        if (character.inventory.maxStackSize < character.inventory.stackSize)
        {
            sourceInventory.stackSize = character.inventory.stackSize - character.inventory.maxStackSize;
            character.inventory.stackSize = character.inventory.maxStackSize;
        }
        else
        {
            sourceInventory.stackSize -= amount;
        }

        CleanupInventory(sourceInventory);

        return true;
    }

    /// <summary>
    /// Gets the type of the closest inventory of.
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="t"></param>
    /// <param name="desiredAmount"></param>
    /// <returns></returns>
    public Inventory GetClosestInventoryOfType(string objectType, Tile t, int desiredAmount, bool canTakeFromStockpile)
    {
        Path_AStar path = GetPathToClosestInventoryOfType(objectType, t, desiredAmount, canTakeFromStockpile);
        return path.EndTile().inventory;
    }

    public Path_AStar GetPathToClosestInventoryOfType(string objectType, Tile t, int desiredAmount, bool canTakeFromStockpile)
    {
        if (inventories.ContainsKey(objectType) == false
             || (!canTakeFromStockpile && inventories[objectType].TrueForAll(i => i.tile != null && i.tile.furniture != null && i.tile.furniture.IsStockpile()))) 
             //we can also avoid going through the Astar construction if we know that all available inventories are stockpiles and we are not allowed to touch those
        {
            //Debug.LogError("GetClosestInventoryType -- no itmes of desired type.");
            return null;
        }

        Path_AStar path = new Path_AStar(World.Current, t, null, objectType, desiredAmount, canTakeFromStockpile);

        return path;
    }

}
