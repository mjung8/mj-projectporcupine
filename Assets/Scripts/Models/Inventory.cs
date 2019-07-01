using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Inventory are things that are lying on the floor/stockpile like a bunch of metal bars
/// or uninstalled furniture.
/// </summary>
public class Inventory
{
    public string objectType = "Steel Plate";
    public int maxStackSize = 50;

    protected int _stackSize = 1;
    public int stackSize
    {
        get { return _stackSize; }
        set
        {
            if(_stackSize != value)
            {
                _stackSize = value;
                if(cbInventoryChanged != null)
                {
                    cbInventoryChanged(this);
                }
            }
        }
    }

    Action<Inventory> cbInventoryChanged;

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

    public void RegisterChangedCallback(Action<Inventory> callback)
    {
        cbInventoryChanged += callback;
    }

    public void UnregisterChangedCallback(Action<Inventory> callback)
    {
        cbInventoryChanged -= callback;
    }
}
