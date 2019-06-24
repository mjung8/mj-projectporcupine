using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public float atmosO2 = 0;
    public float atmosN = 0;
    public float atmosCO2 = 0;

    List<Tile> tiles;

    public Room()
    {
        tiles = new List<Tile>();
    }

    public void AssignTile(Tile t)
    {
        if (tiles.Contains(t))
        {
            return;
        }

        t.room = this;
        tiles.Add(t);
    }

    public void UnAssignAllTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = tiles[i].world.GetOutsideRoom();    // Assign to outside
        }
        tiles = new List<Tile>();
    }

    public static void DoRoomFloodFill(Furniture sourceFurniture)
    {
        // sourceFurniture is the piece of furniture that may be
        // splitting two existing rooms or the final enclosing piece
        // of a new room. 

        // Check the NESW neighbours of the furniture's tile and flood fill.

        World world = sourceFurniture.tile.world;

        // If this piece of furniture was added to an existing room
        // (which should always be true assuming the outside is considered a room)
        // delete that room and assign all tiles within to be outside for now
        if (sourceFurniture.tile.room != world.GetOutsideRoom())
        {
            world.DeleteRoom(sourceFurniture.tile.room);    // This reassigns tiles to the outside room
        }
    }
}
