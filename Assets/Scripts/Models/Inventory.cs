using UnityEngine;
using System.Collections;

/// <summary>
/// Inventory are things that are lying on the floor/stockpile like a bunch of metal bars
/// or uninstalled furniture.
/// </summary>
public class Inventory
{
    public string objectType = "Steel Plate";
    public int maxStackSize = 50;
    public int stackSize = 1;

    public Tile tile;
    public Character character;

    public Inventory()
    {

    }

    public Inventory(string objectType, int maxStackSize, int stackSize)
    {
        this.objectType = objectType;
        this.maxStackSize = maxStackSize;
        this.stackSize = stackSize;
    }

    protected Inventory(Inventory other)
    {
        objectType = other.objectType;
        maxStackSize = other.maxStackSize;
        stackSize = other.stackSize;
    }

    public virtual Inventory Clone()
    {
        return new Inventory(this);
    }
}
