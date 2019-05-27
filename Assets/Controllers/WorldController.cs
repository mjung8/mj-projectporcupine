using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class WorldController : MonoBehaviour {

    public static WorldController Instance { get; protected set; }

    // Simple way to handle current sprite
    public Sprite floorSprite;
    public Sprite emptySprite;

    Dictionary<Tile, GameObject> tileGameObjectMap;
    Dictionary<Furniture, GameObject> furnitureGameObjectMap;

    Dictionary<string, Sprite> furnitureSprites;

    // World and tile data
	public World World { get; protected set; }

	// Use this for initialization
	void Start () {

        LoadSprites();

        if (Instance != null)
        {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;

        // Create a world with Empty tiles
		World = new World();

        World.RegisterFurnitureCreated(OnFurnitureCreated);

        // Instantiate the dictionary that tracks which GameObject is rendering which Tile data
        tileGameObjectMap = new Dictionary<Tile, GameObject>();
        furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();

        // Create a GameObject for each of our tiles, so they show visually
        for (int x = 0; x < World.Width; x++)
        {
            for (int y = 0; y < World.Height; y++)
            {
                // Get the tile data
                Tile tile_data = World.GetTileAt(x, y);
                // Create a new GameObject and add to the scene
                GameObject tile_go = new GameObject();

                // Add the tile/GO pair to the dictionary
                tileGameObjectMap.Add(tile_data, tile_go);

                tile_go.name = "Tile_ " + x + "_" + y;
                tile_go.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
                tile_go.transform.SetParent(this.transform, true);

                // Add a sprite renderer, add empty tile sprite
                tile_go.AddComponent<SpriteRenderer>().sprite = emptySprite;

                // Register our callback so that or GameObjet gets updated whenever
                // the tile's type changes
                tile_data.RegisterTileTypeChangedCallback(OnTileTypeChanged);
            }
        }

        // Center the camera
        Camera.main.transform.position = new Vector3(World.Width / 2, World.Height / 2, Camera.main.transform.position.z);

        // For testing
        //World.RandomizeTiles(); 

	}

    void LoadSprites()
    {
        furnitureSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Furniture/");

        Debug.Log("LOADED RESOURCE: ");
        foreach (Sprite s in sprites)
        {
            Debug.Log(s);
            furnitureSprites[s.name] = s;
        }
    }

	// Update is called once per frame
	void Update () {

	}

    // THIS IS AN EXAMPLE - NOT CURRENTLY USED
    void DestroyAllTileGameObjects()
    {
        // This function might get called when we are changing floors/levels.
        // We need to destroy all visual **GameObjects** -- but not the actual tile data!

        while(tileGameObjectMap.Count > 0)
        {
            Tile tile_data = tileGameObjectMap.Keys.First();
            GameObject tile_go = tileGameObjectMap[tile_data];

            // Remove the pair from the map
            tileGameObjectMap.Remove(tile_data);

            // Unregister the callback!
            tile_data.UnregisterTileTypeChangedCallback(OnTileTypeChanged);

            // Destroy the visual GameObject
            Destroy(tile_go);
        }

        // Presumabley, after this function gets called, we'd be calling another
        // function to build all the GameObject's for the tile on the new floor/level
    }

    // Called automatically whenever a tile's type gets changed
    void OnTileTypeChanged(Tile tile_data)
    {
        if (tileGameObjectMap.ContainsKey(tile_data) == false)
        {
            Debug.LogError("tileGameObjectmap doesn't contain the tile_data -- did you forget to add the tile to the dictionary or unregister the callback?");
            return;
        }

        GameObject tile_go = tileGameObjectMap[tile_data];

        if (tile_go == null)
        {
            Debug.LogError("tileGameObjectmap returned GameObject is null -- did you forget to add the tile to the dictionary or unregister the callback?");
            return;
        }

        if (tile_data.Type == TileType.Floor)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = floorSprite;
        } else if (tile_data.Type == TileType.Empty)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = null;
        } else
        {
            Debug.LogError("OnTileTypeChanged - Unrecognized tile type.");
        }
    }

    /// <summary>
    /// Gets the tile at the unity-space coordinates.
    /// </summary>
    /// <param name="coord">Unity world-space coordinates.</param>
    /// <returns>The tile at the world coordinate.</returns>
    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x);
        int y = Mathf.FloorToInt(coord.y);

        return World.GetTileAt(x, y);
    }

    public void OnFurnitureCreated(Furniture furn)
    {
        //Debug.Log("OnfurnitureCreated");
        // Create a visual GameObject linked to this data

        // FIXME: does not consider multi-tile objects nor rotated objects

        // Create a new GameObject and add to the scene
        GameObject furn_go = new GameObject();

        // Add the tile/GO pair to the dictionary
        furnitureGameObjectMap.Add(furn, furn_go);

        furn_go.name = furn.objectType + "_ " + furn.tile.X + "_" + furn.tile.Y;
        furn_go.transform.position = new Vector3(furn.tile.X, furn.tile.Y, 0);
        furn_go.transform.SetParent(this.transform, true);

        // FIXME: we assume the object must be a wall so use the hardcoded
        // reference to the wall sprite
        furn_go.AddComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furn);
        // Make sorting order in layer above tiles (builds have a bug where wall sprite is below tile sprite)
        furn_go.GetComponent<SpriteRenderer>().sortingOrder = 1;

        // Register our callback so that or GameObject gets updated whenever
        // the object's info changes
        furn.RegisterOnChangedCallback(OnFurnitureChanged);
    }

    void OnFurnitureChanged(Furniture furn)
    {
        // Make sure furniture's graphics are correct
        if (furnitureGameObjectMap.ContainsKey(furn) == false)
        {
            Debug.LogError("OnFurnitureChanged -- trying to change visuals for furniture not in our map.");
            return;
        }

        GameObject furn_go = furnitureGameObjectMap[furn];
        furn_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furn);
    }

    Sprite GetSpriteForFurniture(Furniture furn)
    {
        if (furn.linksToNeighbour == false)
        {
            return furnitureSprites[furn.objectType];
        }

        // Otherwise, the sprite name is more complicated.
        string spriteName = furn.objectType + "_";

        // Check for neighbours North, East, South, West
        int x = furn.tile.X;
        int y = furn.tile.Y;

        Tile t;
        
        t = World.GetTileAt(x, y+1);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "N";
        }
        t = World.GetTileAt(x+1, y);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "E";
        }
        t = World.GetTileAt(x, y-1);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "S";
        }
        t = World.GetTileAt(x-1, y);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "W";
        }

        if(furnitureSprites[spriteName] == false)
        {
            Debug.LogError("GetSpriteForfurniture -- no sprites with name: " + spriteName);
            return null;
        }

        return furnitureSprites[spriteName];

    }


}
