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
            // This tile is already in this room
            return;
        }

        if (t.room != null)
        {
            t.room.tiles.Remove(t);
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

        Room oldRoom = sourceFurniture.tile.room;

        // Try building a new rooms for each of our NESW directions
        foreach (Tile t in sourceFurniture.tile.GetNeighbours())
        {
            ActualFloodFill(t, oldRoom);
        }

        sourceFurniture.tile.room = null;
        oldRoom.tiles.Remove(sourceFurniture.tile);

        // If this piece of furniture was added to an existing room
        // (which should always be true assuming the outside is considered a room)
        // delete that room and assign all tiles within to be outside for now

        if (oldRoom != world.GetOutsideRoom())
        {
            // At this point, oldRoom shouldn't have any more tiles left in it
            // so in practice "DeleteRoom" should mostly only need to remove
            // the room from the world's list

            if (oldRoom.tiles.Count > 0)
            {
                Debug.LogError("oldRoom still has tiles assigned to it");
            }

            world.DeleteRoom(oldRoom);
        }
    }

    protected static void ActualFloodFill(Tile tile, Room oldRoom)
    {
        if (tile == null)
        {
            // We are trying to flood fill off the map
            return;
        }

        if (tile.room != oldRoom)
        {
            // This tile was already assigned to another "new" room 
            // which means the direction picked isn't isolated
            return;
        }

        if (tile.furniture != null && tile.furniture.roomEnclosure)
        {
            // This tile has a wall/door/whatever in it so no room here
            return;
        }

        if (tile.Type == TileType.Empty)
        {
            // This tile is empty and must remain part of the outside
        }

        // If we get to this point then we need to create a new room
        Room newRoom = new Room();
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);

        while (tilesToCheck.Count > 0)
        {
            Tile t = tilesToCheck.Dequeue();

            if (t.room == oldRoom)
            {
                newRoom.AssignTile(t);

                Tile[] ns = t.GetNeighbours();
                foreach (Tile t2 in ns)
                {
                    if (t2 == null || t2.Type == TileType.Empty)
                    {
                        // We have hit open space so this wip room is part of the ouside
                        // So immediately end flood fill and delete this wip room and reassign
                        // all tiles to the outside
                        newRoom.UnAssignAllTiles();
                        return;
                    }

                    // We know t2 is not null or empty so make sure it hasn't already
                    // been processed and isn't a "wall" type tile
                    if (t2.room == oldRoom && (t2.furniture == null || t2.furniture.roomEnclosure == false))
                    {
                        tilesToCheck.Enqueue(t2);
                    }
                }
            }
        }

        // Tell the world that a new room has formed
        tile.world.AddRoom(newRoom);
    }

}
