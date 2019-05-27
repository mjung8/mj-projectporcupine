using UnityEngine;
using System.Collections.Generic;
using System;

public class World
{

    // A 2D arary to hold tile data
    Tile[,] tiles;

    Dictionary<string, Furniture> furniturePrototypes;

    // The tile width of world.
    public int Width { get; protected set; }
    // The tile height of world.
    public int Height { get; protected set; }

    Action<Furniture> cbFurnitureCreated;

    /// <summary>
    /// Initializes a new instance of the World class.
    /// </summary>
    /// <param name="width">Width in tiles.</param>
    /// <param name="height">Height in tiles.</param>
	public World(int width = 100, int height = 100)
    {
        Width = width;
        Height = height;

        tiles = new Tile[Width, Height];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                tiles[x, y] = new Tile(this, x, y);
            }
        }

        Debug.Log("World created with " + (Width * Height) + " tiles.");

        CreateFurniturePrototypes();
    }

    void CreateFurniturePrototypes()
    {
        furniturePrototypes = new Dictionary<string, Furniture>();

        furniturePrototypes.Add("Wall",
            Furniture.CreatePrototype(
                                "Wall",
                                0,  // Impassable
                                1,  // Width
                                1,  // Height
                                true    // Links to neighbours and "sort of" becomes part of a larger object
                            )
        );
    }

    /// <summary>
    /// A function for testing.
    /// </summary>
    //public void RandomizeTiles()
    //{
    //    Debug.Log("RandomizeTiles");
    //    for (int x = 0; x < Width; x++)
    //    {
    //        for (int y = 0; y < Height; y++)
    //        {
    //            if (UnityEngine.Random.Range(0, 2) == 0)
    //            {
    //                tiles[x, y].Type = TileType.Empty;
    //            }
    //            else
    //            {
    //                tiles[x, y].Type = TileType.Floor;
    //            }
    //        }
    //    }
    //}

    /// <summary>
    /// Get the tile data at x and y.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns>The Tile.</returns>
	public Tile GetTileAt(int x, int y)
    {
        if (x > Width || x < 0 || y > Height || y < 0)
        {
            Debug.LogError("Tile (" + x + "," + y + ") is out of range.");
            return null;
        }
        return tiles[x, y];
    }

    public void PlaceFurniture(string objectType, Tile t)
    {
        //Debug.Log("Placefurniture");
        //TODO: This function assumes 1x1 tiles -- change this later
        if (furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("furniturePrototypes doesn't contain a proto for key: " + objectType);
        }

        Furniture obj = Furniture.PlaceInstance(furniturePrototypes[objectType], t);

        if (obj == null)
        {
            // Failed to place object, most likely something already there
            return;
        }

        if (cbFurnitureCreated != null)
        {
            cbFurnitureCreated(obj);
        }

    }

    public void RegisterFurnitureCreated(Action<Furniture> callbackfunc)
    {
        cbFurnitureCreated += callbackfunc;
    }

    public void UnregisterfurnitureCreated(Action<Furniture> callbackfunc)
    {
        cbFurnitureCreated -= callbackfunc;
    }

}
