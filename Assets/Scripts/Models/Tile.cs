﻿using UnityEngine;
using System.Collections;
using System;

// TileType is  the base type of the tile. In some tile-based games, that might be
// the terrain type. For us, we only need to differentiate between empty space
// and floor (aka the station structure/scaffold). Walls/Doors/etc... will be
// furnitures sitting on top of the floor
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
            if (cbTileChanged != null && oldType != _type)
                cbTileChanged(this);
        }
    }

    // LooseObject is like a stack of something
    Inventory inventory;
    // furniture is a wall, door, furniture
    public Furniture furniture
    {
        get; protected set;
    }

    public Job pendingFunitureJob;

    // Know the context in which this exists...
    public World world { get; protected set; }
    public int X { get; protected set; }
    public int Y { get; protected set; }

    public float movementCost
    {
        get
        {
            if (Type == TileType.Empty)
                return 0;

            if (furniture == null)
                return 1;

            return 1 * furniture.movementCost;
        }
    }

    // The function to callback any time the data changes
    Action<Tile> cbTileChanged;

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
        cbTileChanged += callback;
    }

    /// <summary>
    /// Unregister a callback.
    /// </summary>
    /// <param name="callback"></param>
    public void UnregisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileChanged -= callback;
    }

    public bool PlaceFurniture(Furniture objInstance)
    {
        if (objInstance == null)
        {
            // We are uninstalling whatever was here
            furniture = null;
            return true;
        }

        // objInstance isn't null

        if (furniture != null)
        {
            Debug.LogError("Trying to assign a furniture to a tile that already has one!");
            return false;
        }

        // At this point, everything's fine!
        furniture = objInstance;
        return true;
    }

    // Tells us if two tiles are adjaccent
    public bool IsNeighbour(Tile tile, bool diagOkay = false)
    {
        return
            Mathf.Abs(this.X - tile.X) + Mathf.Abs(this.Y - tile.Y) == 1 ||
            (diagOkay && (Mathf.Abs(this.X - tile.X) == 1 && Mathf.Abs(this.Y - tile.Y) == 1));

        //// Same column X, check Y difference
        //if (this.X == tile.X && (this.Y == tile.Y + 1 || this.Y == tile.Y - 1))
        //    return true;
        //// Same row Y, check X difference
        //if (this.Y == tile.Y && (this.X == tile.X + 1 || this.X == tile.X - 1))
        //    return true;

        //if (diagOkay)
        //{
        //    if (this.X == tile.X + 1 && (this.Y == tile.Y + 1 || this.X == tile.X - 1))
        //        return true;
        //    if (this.X == tile.X - 1 && (this.Y == tile.Y + 1 || this.X == tile.X - 1))
        //        return true;
        //}

        //return false;
    }

}
