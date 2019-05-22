using UnityEngine;
using System.Collections;

public class InstalledObject
{
    // Represents base tile of object -- but large objects will occupy
    // multiple tiles.
    Tile tile;

    // This will be queried by the visual system to know what sprite to render
    string objectType;

    // This is a multiplier. Value of 2 means move twice as slow (at half speed)
    // Tile type and other environmental effects (fire) may be combined.
    // SPECIAL: If movementCost = 0 then the tile is impassible (e.g. a wall).
    float movementCost;

    // For example, a sofa might be 3x2 but graphics are only 3x1 (extra row for leg room)
    int width;
    int height;

    // TODO: implement larger objects
    // TODO: implement object rotation

    protected InstalledObject()
    {

    }

    static public InstalledObject CreatePrototype(string objectType, float movementCost = 1f, int width = 1, int height = 1)
    {
        InstalledObject obj = new InstalledObject();

        obj.objectType = objectType;
        obj.movementCost = movementCost;
        obj.width = width;
        obj.height = height;

        return obj;
    }

    static public InstalledObject PlaceInstance(InstalledObject proto, Tile tile)
    {
        InstalledObject obj = new InstalledObject();

        obj.objectType = proto.objectType;
        obj.movementCost = proto.movementCost;
        obj.width = proto.width;
        obj.height = proto.height;

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

}
