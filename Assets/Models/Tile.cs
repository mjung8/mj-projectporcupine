using UnityEngine;
using System.Collections;
using System;

public class Tile {

	public enum TileType { Empty, Floor };

	TileType type = TileType.Empty;

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
            type = value;
            // Call the callback and let things know we've changed
        }
    }

    public Tile( World world, int x, int y ) {
		this.world = world;
		this.x = x;
		this.y = y;
	}

}
