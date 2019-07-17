﻿using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class World : IXmlSerializable
{

    // A 2D arary to hold tile data
    Tile[,] tiles;
    public List<Character> characters;
    public List<Furniture> furnitures;
    public List<Room> rooms;
    public InventoryManager inventoryManager;

    // The pathfinding graph used to navigate the world
    public Path_TileGraph tileGraph;

    public Dictionary<string, Furniture> furniturePrototypes;
    public Dictionary<string, Job> furnitureJobPrototypes;

    // The tile width of world.
    public int Width { get; protected set; }
    // The tile height of world.
    public int Height { get; protected set; }

    Action<Furniture> cbFurnitureCreated;
    Action<Character> cbCharacterCreated;
    Action<Inventory> cbInventoryCreated;
    Action<Tile> cbTileChanged;

    // TODO: most likely replaced with dedicated class
    public JobQueue jobQueue;

    public static World Current { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the World class.
    /// </summary>
    /// <param name="width">Width in tiles.</param>
    /// <param name="height">Height in tiles.</param>
	public World(int width, int height)
    {
        // Creates an empty world
        SetupWorld(width, height);

        // Make one character
        CreateCharacter(GetTileAt(Width / 2, Height / 2));

    }

    /// <summary>
    /// Default constructor used when loading a world from a file.
    /// </summary>
    public World()
    {

    }

    public Room GetOutsideRoom()
    {
        return rooms[0];
    }

    public int GetRoomID(Room r)
    {
        return rooms.IndexOf(r);
    }

    public Room GetRoomFromID(int i)
    {
        if (i < 0 || i > rooms.Count - 1)
            return null;

        return rooms[i];
    }

    public void AddRoom(Room r)
    {
        rooms.Add(r);
    }

    public void DeleteRoom(Room r)
    {
        if (r == GetOutsideRoom())
        {
            Debug.LogError("Tried to delete the outside room.");
            return;
        }
        // Remove this room from our rooms list
        rooms.Remove(r);

        // All tiles that belonged to this room should be reassigned to
        // the outside
        r.ReturnTilesToOutsideRoom();
    }

    void SetupWorld(int width, int height)
    {
        jobQueue = new JobQueue();

        // Set the current world to be this world
        // TODO: do we need to do any cleanup of the old world?
        Current = this;

        Width = width;
        Height = height;

        tiles = new Tile[Width, Height];

        rooms = new List<Room>();
        rooms.Add(new Room());  // Create the outside

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                tiles[x, y] = new Tile(x, y);
                tiles[x, y].RegisterTileTypeChangedCallback(OnTileChanged);
                tiles[x, y].room = GetOutsideRoom();  // Rooms 0 is always going to be outside, the default room
            }
        }

        Debug.Log("World created with " + (Width * Height) + " tiles.");

        CreateFurniturePrototypes();

        characters = new List<Character>();
        furnitures = new List<Furniture>();
        inventoryManager = new InventoryManager();

    }

    // Tick
    public void Update(float deltaTime)
    {
        foreach (Character c in characters)
        {
            c.Update(deltaTime);
        }

        foreach (Furniture f in furnitures)
        {
            f.Update(deltaTime);
        }
    }

    public Character CreateCharacter(Tile t)
    {
        Character c = new Character(t);

        characters.Add(c);

        if (cbCharacterCreated != null)
            cbCharacterCreated(c);

        return c;
    }

    public void SetFurnitureJobPrototype(Job j, Furniture f)
    {
        furnitureJobPrototypes[f.objectType] = j;
    }

    void LoadFurnitureLua()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "LUA");
        filePath = Path.Combine(filePath, "Furniture.lua");
        string myLuaCode = File.ReadAllText(filePath);

        //Debug.Log("My LUA Code");
        //Debug.Log(myLuaCode);

        // Instantiate the singleton
        new FurnitureActions(myLuaCode);
    }

    void CreateFurniturePrototypes()
    {
        LoadFurnitureLua();

        furniturePrototypes = new Dictionary<string, Furniture>();
        furnitureJobPrototypes = new Dictionary<string, Job>();

        // READ FURNITURE XML FILE HERE
        // TODO: should be geting passed a StreamIO handle or the raw text instead of opening file ourself
        string filePath = Path.Combine(Application.streamingAssetsPath, "Data");
        filePath = Path.Combine(filePath, "Furniture.xml");
        string furnitureXmlText = File.ReadAllText(filePath);

        XmlTextReader reader = new XmlTextReader(new StringReader(furnitureXmlText));

        int furnCount = 0;
        if (reader.ReadToDescendant("Furnitures"))
        {
            if (reader.ReadToDescendant("Furniture"))
            {
                do
                {
                    furnCount++;

                    Furniture furn = new Furniture();
                    furn.ReadXmlPrototype(reader);

                    furniturePrototypes[furn.objectType] = furn;


                } while (reader.ReadToNextSibling("Furniture"));
            }
            else
            {
                Debug.LogError("The furniture prototype definition file doesn't have any 'Furniture' elements.");
            }
        }
        else
        {
            Debug.LogError("Did not find a 'Furnitures' element in the prototype definition file.");
        }

        Debug.Log("Furniture prototypes read: " + furnCount);

        // This bit will come form parsing a LUA file later but for now we still need to implement
        // furniture behaviour directly
        //furniturePrototypes["Door"].RegisterUpdateAction(FurnitureActions.Door_UpdateAction);
        //furniturePrototypes["Door"].IsEnterable = FurnitureActions.Door_IsEnterable;
    }

    //void CreateFurniturePrototypes()
    //{
    //    // This will be replaced by a function that reads data
    //    // from a text file

    //    furniturePrototypes = new Dictionary<string, Furniture>();
    //    furnitureJobPrototypes = new Dictionary<string, Job>();

    //    furniturePrototypes.Add("furn_SteelWall",
    //        new Furniture(
    //            "furn_SteelWall",
    //            0,  // Impassable
    //            1,  // Width
    //            1,  // Height
    //            true,   // Links to neighbours and "sort of" becomes part of a larger object
    //            true    // Enclose rooms
    //        )
    //    );
    //    furniturePrototypes["furn_SteelWall"].Name = "Basic Wall";
    //    furnitureJobPrototypes.Add("furn_SteelWall",
    //        new Job(
    //            null,
    //            "furn_SteelWall",
    //            FurnitureActions.JobComplete_FurnitureBuilding,
    //            1f,
    //            new Inventory[] { new Inventory("Steel Plate", 5, 0) }
    //        )
    //    );

    //    furniturePrototypes.Add("Door",
    //        new Furniture(
    //            "Door",
    //            1,  // Door pathfinding cost
    //            1,  // Width
    //            1,  // Height
    //            false,  // Links to neighbours and "sort of" becomes part of a larger object
    //            true    // Enclose rooms
    //        )
    //    );

    //    // What if object behaviours were scriptable? And part of the text file?
    //    furniturePrototypes["Door"].SetParameter("openness", 0);
    //    furniturePrototypes["Door"].SetParameter("is_opening", 0);
    //    furniturePrototypes["Door"].RegisterUpdateAction(FurnitureActions.Door_UpdateAction);

    //    furniturePrototypes["Door"].IsEnterable = FurnitureActions.Door_IsEnterable;


    //    furniturePrototypes.Add("Stockpile",
    //        new Furniture(
    //            "Stockpile",
    //            1,  // Impassable
    //            1,  // Width
    //            1,  // Height
    //            true,   // Links to neighbours and "sort of" becomes part of a larger object
    //            false    // Enclose rooms
    //        )
    //    );
    //    furniturePrototypes["Stockpile"].RegisterUpdateAction(FurnitureActions.Stockpile_UpdateAction);
    //    furniturePrototypes["Stockpile"].tint = new Color32(186, 31, 31, 255);
    //    furnitureJobPrototypes.Add("Stockpile",
    //        new Job(
    //            null,
    //            "Stockpile",
    //            FurnitureActions.JobComplete_FurnitureBuilding,
    //            -1,
    //            null
    //        )
    //    );


    //    furniturePrototypes.Add("Oxygen Generator",
    //        new Furniture(
    //            "Oxygen Generator",
    //            10,  // pathfinding cost
    //            2,  // Width
    //            2,  // Height
    //            false,  // Links to neighbours and "sort of" becomes part of a larger object
    //            false    // Enclose rooms
    //        )
    //    );
    //    furniturePrototypes["Oxygen Generator"].RegisterUpdateAction(FurnitureActions.OxygenGenerator_UpdateAction);


    //    furniturePrototypes.Add("Mining Drone Station",
    //       new Furniture(
    //           "Mining Drone Station",
    //           1,  // pathfinding cost
    //           3,  // Width
    //           3,  // Height    // TODO: in the future, the mining drone station will be a 3x2 object with an offset work spot
    //           false,  // Links to neighbours and "sort of" becomes part of a larger object
    //           false    // Enclose rooms
    //       )
    //    );
    //    furniturePrototypes["Mining Drone Station"].jobSpotOffset = new Vector2(1, 0);

    //    furniturePrototypes["Mining Drone Station"].RegisterUpdateAction(FurnitureActions.MiningDroneStation_UpdateAction);

    //}

    public void SetupPathfindingExample()
    {
        Debug.Log("SetupPathfindingExample");

        // Make a set of floor/walls to test pathfinding with

        int l = Width / 2 - 5;
        int b = Height / 2 - 5;

        for (int x = l - 5; x < l + 15; x++)
        {
            for (int y = b - 5; y < b + 15; y++)
            {
                tiles[x, y].Type = TileType.Floor;

                if (x == l || x == (l + 9) || y == b || y == (b + 9))
                {
                    if (x != (l + 9) && y != (b + 4))
                    {
                        PlaceFurniture("furn_SteelWall", tiles[x, y]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get the tile data at x and y.
    /// </summary>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns>The Tile.</returns>
	public Tile GetTileAt(int x, int y)
    {
        if (x >= Width || x < 0 || y >= Height || y < 0)
        {
            //Debug.LogError("Tile (" + x + "," + y + ") is out of range.");
            return null;
        }
        return tiles[x, y];
    }

    public Furniture PlaceFurniture(string objectType, Tile t, bool doRoomFloodFill = true)
    {
        //Debug.Log("Placefurniture");
        //TODO: This function assumes 1x1 tiles -- change this later
        if (furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("furniturePrototypes doesn't contain a proto for key: " + objectType);
            return null;
        }

        Furniture furn = Furniture.PlaceInstance(furniturePrototypes[objectType], t);

        if (furn == null)
        {
            // Failed to place object, most likely something already there
            return null;
        }

        furn.RegisterOnRemovedCallback(OnFurnitureRemoved);

        furnitures.Add(furn);

        // Do we need to recalculate our rooms?
        if (doRoomFloodFill && furn.roomEnclosure)
        {
            Room.DoRoomFloodFill(furn.tile);
        }

        if (cbFurnitureCreated != null)
        {
            cbFurnitureCreated(furn);

            if (furn.movementCost != 1)
            {
                // Tiles return movement cost as base cost times furniture movement cost
                // so a furniture with movement 1 doesn't impact pathfinding
                InvalidateTileGraph();  // Reset the pathfinding system
            }
        }

        return furn;
    }

    public void RegisterFurnitureCreated(Action<Furniture> callbackfunc)
    {
        cbFurnitureCreated += callbackfunc;
    }

    public void UnregisterFurnitureCreated(Action<Furniture> callbackfunc)
    {
        cbFurnitureCreated -= callbackfunc;
    }

    public void RegisterCharacterCreated(Action<Character> callbackfunc)
    {
        cbCharacterCreated += callbackfunc;
    }

    public void UnregisterCharacterCreated(Action<Character> callbackfunc)
    {
        cbCharacterCreated -= callbackfunc;
    }

    public void RegisterInventoryCreated(Action<Inventory> callbackfunc)
    {
        cbInventoryCreated += callbackfunc;
    }

    public void UnregisterInventoryCreated(Action<Inventory> callbackfunc)
    {
        cbInventoryCreated -= callbackfunc;
    }

    public void RegisterTileChanged(Action<Tile> callbackfunc)
    {
        cbTileChanged += callbackfunc;
    }

    public void UnregisterTileChanged(Action<Tile> callbackfunc)
    {
        cbTileChanged -= callbackfunc;
    }

    // Gets called when any tile changes
    void OnTileChanged(Tile t)
    {
        if (cbTileChanged == null)
            return;

        cbTileChanged(t);

        InvalidateTileGraph();
    }

    // This should be called whenever a change to the world
    // means that our old pathfinding info is invalid.
    public void InvalidateTileGraph()
    {
        tileGraph = null;
    }

    public bool IsFurniturePlacementValid(string furnitureType, Tile t)
    {
        return furniturePrototypes[furnitureType].IsValidPosition(t);
    }

    public Furniture GetFurniturePrototype(string objectType)
    {
        if (furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("No furniture with type: " + objectType);
            return null;
        }

        return furniturePrototypes[objectType];
    }

    /**********************************
     *  SAVING & LOADING
     * 
     *********************************/

    public XmlSchema GetSchema()
    {
        throw new NotImplementedException();
    }

    public void WriteXml(XmlWriter writer)
    {
        // Save info here
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());

        writer.WriteStartElement("Rooms");
        foreach (Room r in rooms)
        {
            if (GetOutsideRoom() == r)
                continue;   // Skip the outside room. Alternatively, should SetupWorld be changed not to create one?

            writer.WriteStartElement("Room");
            r.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Tiles");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (tiles[x, y].Type != TileType.Empty)
                {
                    writer.WriteStartElement("Tile");
                    tiles[x, y].WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Furnitures");
        foreach (Furniture furn in furnitures)
        {
            writer.WriteStartElement("Furniture");
            furn.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        writer.WriteStartElement("Characters");
        foreach (Character c in characters)
        {
            writer.WriteStartElement("Character");
            c.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        //writer.WriteStartElement("Width");
        //writer.WriteValue(Width);
        //writer.WriteEndElement();
    }

    public void ReadXml(XmlReader reader)
    {
        // Load info here
        //Debug.Log("World::ReadXml");

        Width = int.Parse(reader.GetAttribute("Width"));
        Height = int.Parse(reader.GetAttribute("Height"));

        SetupWorld(Width, Height);

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Rooms":
                    ReadXml_Rooms(reader);
                    break;
                case "Tiles":
                    ReadXml_Tiles(reader);
                    break;
                case "Furnitures":
                    ReadXml_Furnitures(reader);
                    break;
                case "Characters":
                    ReadXml_Characters(reader);
                    break;
            }
        }

        // DEBUG ONLY REMOVE ME LATER
        // Create an Inventory item
        Inventory inv = new Inventory("Steel Plate", 50, 50);
        Tile t = GetTileAt(Width / 2, Height / 2);
        inventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.inventory);
        }

        inv = new Inventory("Steel Plate", 50, 4);
        t = GetTileAt(Width / 2 + 2, Height / 2);
        inventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.inventory);
        }

        inv = new Inventory("Steel Plate", 50, 3);
        t = GetTileAt(Width / 2 + 1, Height / 2 + 2);
        inventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(t.inventory);
        }
    }

    void ReadXml_Tiles(XmlReader reader)
    {
        //Debug.Log("ReadXml_Tiles");
        // We are in the "Tiles" element, so read elements
        // until we run out of "Tile" nodes

        if (reader.ReadToDescendant("Tile"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));
                tiles[x, y].ReadXml(reader);
            } while (reader.ReadToNextSibling("Tile"));
        }

    }

    void ReadXml_Furnitures(XmlReader reader)
    {
        //Debug.Log("ReadXml_Furnitures");

        if (reader.ReadToDescendant("Furniture"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                Furniture furn = PlaceFurniture(reader.GetAttribute("objectType"), tiles[x, y], false);
                furn.ReadXml(reader);
            } while (reader.ReadToNextSibling("Furniture"));

            // Don't need this on load beacuse we're getting room info from save file
            //foreach (Furniture furn in furnitures)
            //{
            //    Room.DoRoomFloodFill(furn.tile, true);
            //}
        }
    }

    void ReadXml_Rooms(XmlReader reader)
    {
        //Debug.Log("ReadXml_Rooms");

        if (reader.ReadToDescendant("Room"))
        {
            do
            {
                //int x = int.Parse(reader.GetAttribute("X"));
                //int y = int.Parse(reader.GetAttribute("Y"));

                //Furniture furn = PlaceFurniture(reader.GetAttribute("objectType"), tiles[x, y], false);
                //furn.ReadXml(reader);
                Room r = new Room();
                rooms.Add(r);
                r.ReadXml(reader);

            } while (reader.ReadToNextSibling("Room"));

        }
    }

    void ReadXml_Characters(XmlReader reader)
    {
        //Debug.Log("ReadXml_Characters");

        if (reader.ReadToDescendant("Character"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                Character c = CreateCharacter(tiles[x, y]);
                c.ReadXml(reader);
            } while (reader.ReadToNextSibling("Character"));
        }
    }

    public void OnInventoryCreated(Inventory inv)
    {
        if (cbInventoryCreated != null)
            cbInventoryCreated(inv);
    }

    public void OnFurnitureRemoved(Furniture furn)
    {
        furnitures.Remove(furn);
    }
}
