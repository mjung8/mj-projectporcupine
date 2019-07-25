using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Room : IXmlSerializable
{
    // Dictionary with the amount of gas in room stored in preasure(in atm) multiplyed by number of tiles
    Dictionary<string, float> atmosphericGasses;

    List<Tile> tiles;

    public Room()
    {
        tiles = new List<Tile>();
        atmosphericGasses = new Dictionary<string, float>();
    }

    public int ID
    {
        get
        {
            return World.Current.GetRoomID(this);
        }
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

    public void ReturnTilesToOutsideRoom()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = World.Current.GetOutsideRoom();    // Assign to outside
        }
        tiles = new List<Tile>();
    }

    public bool IsOutsideRoom()
    {
        return this == World.Current.GetOutsideRoom();
    }

    public int GetSize()
    {
        return tiles.Count();
    }

    // Changes gas by an amount in pressure (in atm) multiplied by number of tiles
    public void ChangeGas(string name, float amount)
    {
        if (IsOutsideRoom())
            return;

        if (atmosphericGasses.ContainsKey(name))
        {
            atmosphericGasses[name] += amount;
        }
        else
        {
            atmosphericGasses[name] = amount;
        }

        if (atmosphericGasses[name] < 0)
            atmosphericGasses[name] = 0;
    }

    public void EqualiseGas(Room otherRoom, float leakFactor)
    {
        if (otherRoom == null)
        {
            return;
        }

        List<string> gasses = this.GetGasNames().ToList();
        gasses = gasses.Union(otherRoom.GetGasNames().ToList()).ToList();
        foreach (string gas in gasses)
        {
            float pressureDifference = this.GetGasPressure(gas) - otherRoom.GetGasPressure(gas);
            this.ChangeGas(gas, (-1) * pressureDifference * leakFactor);
            otherRoom.ChangeGas(gas, pressureDifference * leakFactor);
        }
    }

    public static void EqualiseGasByTile(Tile tile, float leakFactor)
    {
        List<Room> roomsDone = new List<Room>();
        foreach (Tile t in tile.GetNeighbours())
        {
            if (t.room != null && roomsDone.Contains(t.room) == false)
            {
                foreach (Room r in roomsDone)
                {
                    t.room.EqualiseGas(r, leakFactor);
                }
                roomsDone.Add(t.room);
            }
        }
    }

    // Gets absolute gas amount in pressure (in atm) multiplied by number of tiles
    public float GetGasAmount(string name)
    {
        if (atmosphericGasses.ContainsKey(name))
        {
            return atmosphericGasses[name];
        }

        return 0;
    }

    // Gets gas amount in pressure (in atm)
    public float GetGasPressure(string name)
    {
        if (atmosphericGasses.ContainsKey(name))
        {
            return atmosphericGasses[name] / GetSize();
        }

        return 0;
    }

    public float GetGasPercentage(string name)
    {
        if (atmosphericGasses.ContainsKey(name) == false)
            return 0;

        float t = 0;

        foreach (string n in atmosphericGasses.Keys)
        {
            t += atmosphericGasses[n];
        }

        return atmosphericGasses[name] / t;
    }

    public string[] GetGasNames()
    {
        return atmosphericGasses.Keys.ToArray();
    }

    public static void DoRoomFloodFill(Tile sourceTile, bool onlyIfOutside = false)
    {
        // sourceFurniture is the piece of furniture that may be
        // splitting two existing rooms or the final enclosing piece
        // of a new room. 

        // Check the NESW neighbours of the furniture's tile and flood fill.

        World world = World.Current;

        Room oldRoom = sourceTile.room;

        if (oldRoom != null)
        {
            // Save the size of old room before we start removing tiles
            // Needed for gas calculations
            int sizeOfOldRoom = oldRoom.GetSize();

            // The source tile had a room, so this must be a new piece of furniture
            // that is potentially dividing this old room into as many as four new rooms

            // Try building a new rooms for each of our NESW directions
            foreach (Tile t in sourceTile.GetNeighbours())
            {
                if (t.room != null && (onlyIfOutside == false || t.room.IsOutsideRoom()))
                    ActualFloodFill(t, oldRoom, sizeOfOldRoom);
            }

            sourceTile.room = null;

            oldRoom.tiles.Remove(sourceTile);

            // If this piece of furniture was added to an existing room
            // (which should always be true assuming the outside is considered a room)
            // delete that room and assign all tiles within to be outside for now

            if (oldRoom.IsOutsideRoom() == false)
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
        else
        {
            // oldRoom is null, which means the source tile was probably a wall,
            // though this may not be the case any longer (i.e. the wall was
            // probably deconstructed. The only thing we have to try is to spawn
            // one new room starting from the tile in question
            ActualFloodFill(sourceTile, null, 0);
        }
    }

    protected static void ActualFloodFill(Tile tile, Room oldRoom, int sizeOfOldRoom)
    {
        //Debug.Log("ActualFloodFill");

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
            return;
        }

        // If we get to this point then we need to create a new room
        List<Room> listOfOldRooms = new List<Room>();
        Room newRoom = new Room();
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(tile);

        bool isConnectedToSpace = false;
        int processedTiles = 0;

        while (tilesToCheck.Count > 0)
        {
            Tile t = tilesToCheck.Dequeue();

            processedTiles++;

            if (t.room != newRoom)
            {
                if (t.room != null && listOfOldRooms.Contains(t.room) == false)
                {
                    listOfOldRooms.Add(t.room);
                    newRoom.MoveGas(t.room);
                }

                newRoom.AssignTile(t);

                Tile[] ns = t.GetNeighbours();
                foreach (Tile t2 in ns)
                {
                    if (t2 == null || t2.Type == TileType.Empty)
                    {
                        // We have hit open space so this wip room is part of the ouside
                        // So immediately end flood fill and delete this wip room and reassign
                        // all tiles to the outside

                        isConnectedToSpace = true;

                        //if (oldRoom != null)
                        //{
                        //    newRoom.ReturnTilesToOutsideRoom();
                        //    return;
                        //}
                    }
                    else
                    {
                        // We know t2 is not null or empty so make sure it hasn't already
                        // been processed and isn't a "wall" type tile
                        if (t2.room != newRoom && (t2.furniture == null || t2.furniture.roomEnclosure == false))
                        {
                            tilesToCheck.Enqueue(t2);
                        }
                    }
                }
            }
        }

        //Debug.Log("ActualFloodFill -- processedTiles: " + processedTiles);

        if (isConnectedToSpace)
        {
            // All tiles that were found by this flood fill should
            // actually be assigned to outside
            newRoom.ReturnTilesToOutsideRoom();
            return;
        }

        // Copy data
        if (oldRoom != null)
        {
            // In this case we are splitting one room into two or more,
            // so we can just copy the old gas ratios
            newRoom.CopyGasPressure(oldRoom, sizeOfOldRoom);
        }


        // Tell the world that a new room has formed
        World.Current.AddRoom(newRoom);
    }

    void CopyGasPressure(Room other, int sizeOfOtherRoom)
    {
        foreach (string n in other.atmosphericGasses.Keys)
        {
            this.atmosphericGasses[n] = other.atmosphericGasses[n] / sizeOfOtherRoom * this.GetSize();
        }
    }

    void MoveGas(Room other)
    {
        foreach (string n in other.atmosphericGasses.Keys)
        {
            this.ChangeGas(n, other.atmosphericGasses[n]);
        }
    }

    public XmlSchema GetSchema()
    {
        throw new NotImplementedException();
    }

    public void WriteXml(XmlWriter writer)
    {
        // write out gas info
        foreach (string k in atmosphericGasses.Keys)
        {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", k);
            writer.WriteAttributeString("value", atmosphericGasses[k].ToString());
            writer.WriteEndElement();
        }
    }

    public void ReadXml(XmlReader reader)
    {
        // read gas info
        if (reader.ReadToDescendant("Param"))
        {
            do
            {
                string k = reader.GetAttribute("name");
                float v = float.Parse(reader.GetAttribute("value"));
                atmosphericGasses[k] = v;
            } while (reader.ReadToNextSibling("Param"));
        }
    }

}
