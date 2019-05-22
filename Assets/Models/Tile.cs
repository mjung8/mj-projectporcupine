using UnityEngine;
using System.Collections;
using System;

// TileType is  the base type of the tile. In some tile-based games, that might be
// the terrain type. For us, we only need to differentiate between empty space
// and floor (aka the station structure/scaffold). Walls/Doors/etc... will be
// InstalledObjects sitting on top of the floor
public enum TileType { Empty, Floor };

public class Tile
{

    private TileType _type = TileType.Empty;
    public TileType Type
    {
        get
        {
            return _type;
        }

        set
        {
            TileType oldType = _type;
            _type = value;
            // Call the callback and let things know we've changed
            if (cbTileTypeChanged != null && oldType != _type)
                cbTileTypeChanged(this);
        }
    }

    // LooseObject is like a stack of something
    LooseObject looseObject;
    // InstalledObject is a wall, door, furniture
    InstalledObject installedObject;

    // Know the context in which this exists...
    World world;
    public int X { get; protected set; }
    public int Y { get; protected set; }

    // The function to callback any time the type changes
    Action<Tile> cbTileTypeChanged;

    /// <summary>
    /// Initialize a new instance of the Tile class.
    /// </summary>
    /// <param name="world">A World instance.</param>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public Tile(World world, int x, int y)
    {
        this.world = world;
        this.X = x;
        this.Y = y;
    }

    /// <summary>
    /// Register a function to be called back when the tile type changes.
    /// </summary>
    /// <param name="callback"></param>
    public void RegisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileTypeChanged += callback;
    }

    /// <summary>
    /// Unregister a callback.
    /// </summary>
    /// <param name="callback"></param>
    public void UnregisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileTypeChanged -= callback;
    }

    public bool PlaceObject(InstalledObject objInstance)
    {
        if (objInstance == null)
        {
            // We are uninstalling whatever was here
            installedObject = null;
            return true;
        }

        // objInstance isn't null

        if(installedObject != null)
        {
            Debug.LogError("Trying to assign an installed object to a tile that already has one!");
            return false;
        }

        // At this point, everything's fine!
        installedObject = objInstance;
        return true;
    }
}
