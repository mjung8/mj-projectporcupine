using UnityEngine;
using System.Collections;
using System;

public class Tile {

	public enum TileType { Empty, Floor };

	TileType type = TileType.Empty;

    Action<Tile> cbTileTypeChanged;

	LooseObject looseObject;
	InstalledObject installedObject;

	World world;
	int x;

    public int X
    {
        get
        {
            return x;
        }
    }

	int y;

    public int Y
    {
        get
        {
            return y;
        }
    }

    public TileType Type
    {
        get
        {
            return type;
        }

        set
        {
            TileType oldType = type;
            type = value;
            // Call the callback and let things know we've changed
            if (cbTileTypeChanged != null && oldType != type)
                cbTileTypeChanged(this);
        }
    }

    public Tile( World world, int x, int y ) {
		this.world = world;
		this.x = x;
		this.y = y;
	}

    public void RegisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileTypeChanged += callback;
    }

    public void UnRegisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileTypeChanged -= callback;
    }
}
