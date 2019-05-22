using UnityEngine;
using System.Collections;
using System;

public class InstalledObject
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

    Action<InstalledObject> cbOnChanged;

    // TODO: implement larger objects
    // TODO: implement object rotation

    protected InstalledObject()
    {

    }

    static public InstalledObject CreatePrototype(string objectType, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false)
    {
        InstalledObject obj = new InstalledObject();

        obj.objectType = objectType;
        obj.movementCost = movementCost;
        obj.width = width;
        obj.height = height;
        obj.linksToNeighbour = linksToNeighbour;

        return obj;
    }

    static public InstalledObject PlaceInstance(InstalledObject proto, Tile tile)
    {
        InstalledObject obj = new InstalledObject();

        obj.objectType = proto.objectType;
        obj.movementCost = proto.movementCost;
        obj.width = proto.width;
        obj.height = proto.height;
        obj.linksToNeighbour = proto.linksToNeighbour;

        obj.tile = tile;

        if (tile.PlaceObject(obj) == false)
        {
            // For some reason we weren't able t place the object in this tile
            // (probably already occupied)
            // Do not return our newly instantiated object
            // (it will be garbage)
            return null;
        }

        return obj;
    }

    public void RegisterOnChangedCallback(Action<InstalledObject> callbackfunc)
    {
        cbOnChanged += callbackfunc;
    }

    public void UnregisterOnChangedCallback(Action<InstalledObject> callbackfunc)
    {
        cbOnChanged -= callbackfunc;
    }

}
