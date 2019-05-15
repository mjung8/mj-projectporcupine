using UnityEngine;
using System.Collections;
using System;

public class WorldController : MonoBehaviour {

    public static WorldController Instance { get; protected set; }

    // Simple way to handle current sprite
    public Sprite floorSprite;

    // World and tile data
	public World World { get; protected set; }

	// Use this for initialization
	void Start () {

        if (Instance != null)
        {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;

        // Create a world with Empty tiles
		World = new World();

        // Create a GameObject for each of our tiles, so they show visually
        for (int x = 0; x < World.Width; x++)
        {
            for (int y = 0; y < World.Height; y++)
            {
                // Get the tile data
                Tile tile_data = World.GetTileAt(x, y);
                // Create a new GameObject and add to the scene
                GameObject tile_go = new GameObject();
                tile_go.name = "Tile_ " + x + "_" + y;
                tile_go.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
                tile_go.transform.SetParent(this.transform, true);

                // Add a sprite renderer, but don't bother setting a sprite
                // because all the tiles are empty right now.
                tile_go.AddComponent<SpriteRenderer>();

                // Use a lambda to create anonymous function to 'wrap' the callback function
                tile_data.RegisterTileTypeChangedCallback( (tile) => { OnTileTypeChanged(tile, tile_go); } );
            }
        }

        // For testing
        World.RandomizeTiles(); 

	}

	// Update is called once per frame
	void Update () {

	}

    // Called automatically whenever a tile's type gets changed
    void OnTileTypeChanged(Tile tile_data, GameObject tile_go)
    {
        if (tile_data.Type == Tile.TileType.Floor)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = floorSprite;
        } else if (tile_data.Type == Tile.TileType.Empty)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = null;
        } else
        {
            Debug.LogError("OnTileTypeChanged - Unrecognized tile type.");
        }
    }

}
