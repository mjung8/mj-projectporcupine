using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class FurnitureSpriteController : MonoBehaviour
{
    Dictionary<Furniture, GameObject> furnitureGameObjectMap;

    World world
    {
        get { return WorldController.Instance.world; }
    }

    // Use this for initialization
    void Start()
    {
        // Instantiate the dictionary that tracks which GameObject is rendering which Tile data
        furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();

        // Register callback so GameObject gets updated whenever Furniture is created
        world.cbFurnitureCreated += OnFurnitureCreated;

        // Go through any EXISTING furniture (ie. from a save file that was loaded OnEnable) and call the OnCreated event manually
        foreach (Furniture furn in world.furnitures)
        {
            OnFurnitureCreated(furn);
        }
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
        furn_go.transform.position = new Vector3(furn.tile.X + ((furn.Width - 1) / 2f), furn.tile.Y + ((furn.Height - 1) / 2f), 0);
        furn_go.transform.SetParent(this.transform, true);

        // FIXME: This hardcoding is not good
        if (furn.objectType == "Door")
        {
            // By default, door graphic is meant for walls EW
            // Check to see if we actually have a wall NS and then rotate
            Tile northTile = world.GetTileAt(furn.tile.X, furn.tile.Y + 1);
            Tile southTile = world.GetTileAt(furn.tile.X, furn.tile.Y - 1);

            if (northTile != null && southTile != null && northTile.furniture != null
                && southTile.furniture != null && northTile.furniture.objectType.Contains("Wall")
                && southTile.furniture.objectType.Contains("Wall"))
            {
                furn_go.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        SpriteRenderer sr = furn_go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSpriteForFurniture(furn);
        sr.sortingLayerName = "Furniture";
        sr.color = furn.tint;

        // Register our callback so that or GameObject gets updated whenever
        // the object's info changes
        furn.cbOnChanged += OnFurnitureChanged;
        furn.cbOnRemoved += OnFurnitureRemoved;
    }

    void OnFurnitureRemoved(Furniture furn)
    {
        if (furnitureGameObjectMap.ContainsKey(furn) == false)
        {
            Debug.LogError("OnFurnitureRemoved -- trying to change visuals for furniture not in our map.");
            return;
        }

        GameObject furn_go = furnitureGameObjectMap[furn];
        Destroy(furn_go);
        furnitureGameObjectMap.Remove(furn);
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
        furn_go.GetComponent<SpriteRenderer>().color = furn.tint;
    }

    public Sprite GetSpriteForFurniture(Furniture furn)
    {
        string spriteName = furn.objectType;
        if (furn.linksToNeighbour == false)
        {
            // If this is a door check openness and update the sprite
            // FIXME: hardcoding needs to be generalized later
            if (furn.objectType == "Door")
            {
                if (furn.GetParameter("openness") < 0.1f)
                {
                    // Door is closed
                    spriteName = "Door";
                }
                else if (furn.GetParameter("openness") < 0.5f)
                {
                    spriteName = "Door_openness_1";
                }
                else if (furn.GetParameter("openness") < 0.9f)
                {
                    spriteName = "Door_openness_2";
                }
                else
                {
                    spriteName = "Door_openness_3";
                }
            }

            return SpriteManager.current.GetSprite("Furniture", spriteName); // furnitureSprites[spriteName];
        }

        // Otherwise, the sprite name is more complicated.
        spriteName = furn.objectType + "_";

        // Check for neighbours North, East, South, West
        int x = furn.tile.X;
        int y = furn.tile.Y;

        Tile t;

        t = world.GetTileAt(x, y + 1);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "N";
        }
        t = world.GetTileAt(x + 1, y);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "E";
        }
        t = world.GetTileAt(x, y - 1);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "S";
        }
        t = world.GetTileAt(x - 1, y);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "W";
        }

        // e.g. Wall_NESW
        //if (furnitureSprites[spriteName] == false)
        //{
        //    Debug.LogError("GetSpriteForfurniture -- no sprites with name: " + spriteName);
        //    return null;
        //}

        return SpriteManager.current.GetSprite("Furniture", spriteName); // furnitureSprites[spriteName];
    }

    public Sprite GetSpriteForFurniture(string objectType)
    {
        Sprite s = SpriteManager.current.GetSprite("Furniture", objectType);

        if (s == null)
        {
            s = SpriteManager.current.GetSprite("Furniture", objectType + "_");
        }

        return s;
    }

}
