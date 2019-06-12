﻿using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class FurnitureSpriteController : MonoBehaviour {

    Dictionary<Furniture, GameObject> furnitureGameObjectMap;

    Dictionary<string, Sprite> furnitureSprites;

    World world
    {
        get { return WorldController.Instance.world; }
    }

	// Use this for initialization
	void Start () {

        LoadSprites();

        // Instantiate the dictionary that tracks which GameObject is rendering which Tile data
        furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();

        world.RegisterFurnitureCreated(OnFurnitureCreated);
	}

    void LoadSprites()
    {
        furnitureSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Furniture/");

        foreach (Sprite s in sprites)
        {
            furnitureSprites[s.name] = s;
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
        furn_go.transform.position = new Vector3(furn.tile.X, furn.tile.Y, 0);
        furn_go.transform.SetParent(this.transform, true);

        // FIXME: we assume the object must be a wall so use the hardcoded
        // reference to the wall sprite
        SpriteRenderer sr = furn_go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSpriteForFurniture(furn);
        sr.sortingLayerName = "Furniture";

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

    public Sprite GetSpriteForFurniture(Furniture furn)
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
        
        t = world.GetTileAt(x, y+1);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "N";
        }
        t = world.GetTileAt(x+1, y);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "E";
        }
        t = world.GetTileAt(x, y-1);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "S";
        }
        t = world.GetTileAt(x-1, y);
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

    public Sprite GetSpriteForFurniture(string objectType)
    {
        if (furnitureSprites.ContainsKey(objectType))
        {
            return furnitureSprites[objectType];
        }
        
        if (furnitureSprites.ContainsKey(objectType + "_"))
        {
            return furnitureSprites[objectType + "_"];
        }

        Debug.LogError("GetSpriteForFurniture -- no sprites with name: " + objectType);
        return null;
    }

}
