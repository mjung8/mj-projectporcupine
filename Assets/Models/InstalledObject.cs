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

    // This is used by our object factory to create the prototypical object
    public InstalledObject(string objectType, float movementCost = 1f, int width = 1, int height = 1)
    {
        this.objectType = objectType;
        this.movementCost = movementCost;
        this.width = width;
        this.height = height;
    }

    protected InstalledObject(InstalledObject proto, Tile tile)
    {
        this.objectType = proto.objectType;
        this.movementCost = proto.movementCost;
        this.width = proto.width;
        this.height = proto.height;

        this.tile = tile;

        tile.installedObject = this;
    }

}
