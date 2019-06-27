using UnityEngine;
using System.Collections;

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
