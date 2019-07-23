using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class TileSpriteController : MonoBehaviour
{
    Dictionary<Tile, GameObject> tileGameObjectMap;

    World world
    {
        get { return WorldController.Instance.world; }
    }

    // Use this for initialization
    void Start()
    {
        // Instantiate the dictionary that tracks which GameObject is rendering which Tile data
        tileGameObjectMap = new Dictionary<Tile, GameObject>();

        // Create a GameObject for each of our tiles, so they show visually
        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                // Get the tile data
                Tile tile_data = world.GetTileAt(x, y);
                // Create a new GameObject and add to the scene
                GameObject tile_go = new GameObject();

                // Add the tile/GO pair to the dictionary
                tileGameObjectMap.Add(tile_data, tile_go);

                tile_go.name = "Tile_ " + x + "_" + y;
                tile_go.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
                tile_go.transform.SetParent(this.transform, true);

                // Add a sprite renderer, add empty tile sprite
                SpriteRenderer sr = tile_go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteManager.current.GetSprite("Tile", "Empty");
                sr.sortingLayerName = "Tiles";

                OnTileChanged(tile_data);
            }
        }

        // Register callback so GameObject gets updated whenever tile changes
        world.cbTileChanged += OnTileChanged;
    }

    // THIS IS AN EXAMPLE - NOT CURRENTLY USED
    void DestroyAllTileGameObjects()
    {
        // This function might get called when we are changing floors/levels.
        // We need to destroy all visual **GameObjects** -- but not the actual tile data!

        while (tileGameObjectMap.Count > 0)
        {
            Tile tile_data = tileGameObjectMap.Keys.First();
            GameObject tile_go = tileGameObjectMap[tile_data];

            // Remove the pair from the map
            tileGameObjectMap.Remove(tile_data);

            // Unregister the callback!
            tile_data.cbTileChanged -= OnTileChanged;

            // Destroy the visual GameObject
            Destroy(tile_go);
        }

        // Presumabley, after this function gets called, we'd be calling another
        // function to build all the GameObject's for the tile on the new floor/level
    }

    // Called automatically whenever a tile's data gets changed
    void OnTileChanged(Tile tile_data)
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
            tile_go.GetComponent<SpriteRenderer>().sprite = SpriteManager.current.GetSprite("Tile", "Floor");
        }
        else if (tile_data.Type == TileType.Empty)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = SpriteManager.current.GetSprite("Tile", "Empty");
        }
        else
        {
            Debug.LogError("OnTileTypeChanged - Unrecognized tile type.");
        }
    }

}
