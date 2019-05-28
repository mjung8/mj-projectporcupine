using UnityEngine;
using System.Collections;
using System;

public class Furniture
{
    // Represents base tile of object -- but large objects will occupy
    // multiple tiles.
    public Tile tile {
        get; protected set;
    }

    // This will be queried by the visual system to know what sprite to render
    public string objectType
    {
        get; protected set;
    }

    // This is a multiplier. Value of 2 means move twice as slow (at half speed)
    // Tile type and other environmental effects (fire) may be combined.
    // SPECIAL: If movementCost = 0 then the tile is impassible (e.g. a wall).
    float movementCost;

    // For example, a sofa might be 3x2 but graphics are only 3x1 (extra row for leg room)
    int width;
    int height;

    public bool linksToNeighbour
    {
        get; protected set;
    }

    Action<Furniture> cbOnChanged;

    Func<Tile, bool> funcPositionValidation;

    // TODO: implement larger objects
    // TODO: implement object rotation

    protected Furniture()
    {

    }

    static public Furniture CreatePrototype(string objectType, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false)
    {
        Furniture obj = new Furniture();

        obj.objectType = objectType;
        obj.movementCost = movementCost;
        obj.width = width;
        obj.height = height;
        obj.linksToNeighbour = linksToNeighbour;

        obj.funcPositionValidation = obj.__IsValidPosition;

        return obj;
    }

    static public Furniture PlaceInstance(Furniture proto, Tile tile)
    {
        if (proto.funcPositionValidation(tile) == false)
        {
            Debug.LogError("PlaceInstance -- Position Validity function returned false.");
            return null;
        }

        // We know our placement destination is valid

        Furniture furn = new Furniture();

        furn.objectType = proto.objectType;
        furn.movementCost = proto.movementCost;
        furn.width = proto.width;
        furn.height = proto.height;
        furn.linksToNeighbour = proto.linksToNeighbour;

        furn.tile = tile;

        if (tile.PlaceFurniture(furn) == false)
        {
            // For some reason we weren't able t place the object in this tile
            // (probably already occupied)
            // Do not return our newly instantiated object
            // (it will be garbage)
            return null;
        }

        if(furn.linksToNeighbour)
        {
            // This type of furniture links itself to its neighbours,
            // so we should inform our neighbours of a new neighbour.
            // Trigger their OnChangedCallback
            Tile t;
            int x = tile.X;
            int y = tile.Y;

            t = tile.world.GetTileAt(x, y + 1);
            if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
            {
                // We have a northern neighbour with the same object type as us, so
                // tell it that it has changed by firing its callback
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x + 1, y);
            if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x, y - 1);
            if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x - 1, y);
            if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
        }

        return furn;
    }

    public void RegisterOnChangedCallback(Action<Furniture> callbackfunc)
    {
        cbOnChanged += callbackfunc;
    }

    public void UnregisterOnChangedCallback(Action<Furniture> callbackfunc)
    {
        cbOnChanged -= callbackfunc;
    }

    public bool IsValidPosition (Tile t)
    {
        return funcPositionValidation(t);
    }

    public bool __IsValidPosition(Tile t)
    {
        // Make sure tile is floor
        if (t.Type != TileType.Floor)
        {
            return false;
        }

        // Make sure tile doesn't already have furniture
        if (t.furniture != null)
        {
            return false;
        }

        return true;
    }

    public bool IsValidPosition_Door(Tile t)
    {
        if (__IsValidPosition(t) == false)
            return false;
        // Make sure we have a pair of E/W walls or N/S walls

        return true;
    }

}
