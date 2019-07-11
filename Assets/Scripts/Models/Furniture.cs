﻿using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class Furniture : IXmlSerializable
{
    /// <summary>
    /// Custom parameter for this particular piece of furniture.
    /// </summary>
    protected Dictionary<string, float> furnParameters;

    /// <summary>
    /// These actions are called every update. They get passed the furniture
    /// they belong to and a deltaTime.
    /// </summary>
    //protected Action<Furniture, float> updateActions;
    protected List<string> updateActions;

    public Func<Furniture, ENTERABILITY> IsEnterable;

    List<Job> jobs;

    // If this furniture gets worked by a person,
    // where is the correct spot for them to stand,
    // relative to the bottom-left tile of the sprite
    // Note: This could be something outside of the actual furniture itself
    public Vector2 jobSpotOffset = Vector2.zero;
    // If the job causes some kind of object to be spawned, where will it appear?
    public Vector2 jobSpawnSpotOffset = Vector2.zero;

    public void Update(float deltaTime)
    {
        if (updateActions != null)
        {
            //updateActions(this, deltaTime);
            FurnitureActions.CallFunctionsWithFurniture(updateActions.ToArray(), this, deltaTime);
        }
    }

    // Represents base tile of object -- but large objects will occupy
    // multiple tiles.
    public Tile tile
    {
        get; protected set;
    }

    // This will be queried by the visual system to know what sprite to render
    public string objectType
    {
        get; protected set;
    }

    private string _Name = null;
    public string Name
    {
        get
        {
            if (_Name == null || _Name.Length == 0)
            {
                return objectType;
            }

            return _Name;
        }
        set
        {
            _Name = value;
        }
    }

    // This is a multiplier. Value of 2 means move twice as slow (at half speed)
    // Tile type and other environmental effects (fire) may be combined.
    // SPECIAL: If movementCost = 0 then the tile is impassible (e.g. a wall).
    public float movementCost { get; protected set; }

    public bool roomEnclosure { get; protected set; }

    // For example, a sofa might be 3x2 but graphics are only 3x1 (extra row for leg room)
    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public Color tint = Color.white;

    public bool linksToNeighbour
    {
        get; protected set;
    }

    public Action<Furniture> cbOnChanged;
    public Action<Furniture> cbOnRemoved;

    Func<Tile, bool> funcPositionValidation;

    // TODO: implement larger objects
    // TODO: implement object rotation

    // Empty constructor for serialization
    public Furniture()
    {
        updateActions = new List<string>();
        furnParameters = new Dictionary<string, float>();
        jobs = new List<Job>();
        this.funcPositionValidation = this.DEFAULT__IsValidPosition;
    }

    // Copy constructor -- don't call this directly unless we never
    // do any subclassing. Use Clone() which is more virtual.
    protected Furniture(Furniture other)
    {
        this.objectType = other.objectType;
        this.Name = other.Name;
        this.movementCost = other.movementCost;
        this.roomEnclosure = other.roomEnclosure;
        this.Width = other.Width;
        this.Height = other.Height;
        this.tint = other.tint;
        this.linksToNeighbour = other.linksToNeighbour;

        this.jobSpotOffset = other.jobSpotOffset;
        this.jobSpawnSpotOffset = other.jobSpawnSpotOffset;

        this.furnParameters = new Dictionary<string, float>(other.furnParameters);
        jobs = new List<Job>();

        if (other.updateActions != null)
            this.updateActions = new List<string>(other.updateActions);

        if (other.funcPositionValidation != null)
            this.funcPositionValidation = (Func<Tile, bool>)other.funcPositionValidation.Clone();

        this.IsEnterable = other.IsEnterable;
    }

    // Make a copy of the current furniture
    // Subclasses should overide this if a different copy constructure should be run.
    virtual public Furniture Clone()
    {
        return new Furniture(this);
    }

    // Create furniture from parameter -- this will probably only be used for prototypes
    //public Furniture(string objectType, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false, bool roomEnclosure = false)
    //{
    //    this.objectType = objectType;
    //    this.movementCost = movementCost;
    //    this.roomEnclosure = roomEnclosure;
    //    this.Width = width;
    //    this.Height = height;
    //    this.linksToNeighbour = linksToNeighbour;

    //    this.funcPositionValidation = this.DEFAULT__IsValidPosition;

    //    furnParameters = new Dictionary<string, float>();
    //}

    static public Furniture PlaceInstance(Furniture proto, Tile tile)
    {
        if (proto.funcPositionValidation(tile) == false)
        {
            Debug.LogError("PlaceInstance -- Position Validity function returned false.");
            return null;
        }

        // We know our placement destination is valid

        Furniture furn = proto.Clone(); //new Furniture(proto);
        furn.tile = tile;

        // FIXME: this assumes 1x1
        if (tile.PlaceFurniture(furn) == false)
        {
            // For some reason we weren't able t place the object in this tile
            // (probably already occupied)
            // Do not return our newly instantiated object
            // (it will be garbage)
            return null;
        }

        if (furn.linksToNeighbour)
        {
            // This type of furniture links itself to its neighbours,
            // so we should inform our neighbours of a new neighbour.
            // Trigger their OnChangedCallback
            Tile t;
            int x = tile.X;
            int y = tile.Y;

            t = World.Current.GetTileAt(x, y + 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == furn.objectType)
            {
                // We have a northern neighbour with the same object type as us, so
                // tell it that it has changed by firing its callback
                t.furniture.cbOnChanged(t.furniture);
            }
            t = World.Current.GetTileAt(x + 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == furn.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = World.Current.GetTileAt(x, y - 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == furn.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = World.Current.GetTileAt(x - 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == furn.objectType)
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

    public void RegisterOnRemovedCallback(Action<Furniture> callbackfunc)
    {
        cbOnRemoved += callbackfunc;
    }

    public void UnregisterOnRemovedCallback(Action<Furniture> callbackfunc)
    {
        cbOnRemoved -= callbackfunc;
    }

    public bool IsValidPosition(Tile t)
    {
        return funcPositionValidation(t);
    }

    protected bool DEFAULT__IsValidPosition(Tile t)
    {
        for (int x_off = t.X; x_off < (t.X + Width); x_off++)
        {
            for (int y_off = t.Y; y_off < (t.Y + Height); y_off++)
            {
                Tile t2 = World.Current.GetTileAt(x_off, y_off);

                // Make sure tile is floor
                if (t2.Type != TileType.Floor)
                {
                    return false;
                }

                // Make sure tile doesn't already have furniture
                if (t2.furniture != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public XmlSchema GetSchema()
    {
        throw new NotImplementedException();
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", tile.X.ToString());
        writer.WriteAttributeString("Y", tile.Y.ToString());
        writer.WriteAttributeString("objectType", objectType);
        //writer.WriteAttributeString("movementCost", movementCost.ToString());

        foreach (string k in furnParameters.Keys)
        {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", k);
            writer.WriteAttributeString("value", furnParameters[k].ToString());
            writer.WriteEndElement();
        }
    }

    public void ReadXmlPrototype(XmlReader reader_parent)
    {
        //Debug.Log("ReadXmlPrototype");

        objectType = reader_parent.GetAttribute("objectType");

        // only the content of the 'Furniture' tag (it's children)
        XmlReader reader = reader_parent.ReadSubtree();

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Name":
                    reader.Read();
                    Name = reader.ReadContentAsString();
                    break;
                case "MovementCost":
                    reader.Read();
                    movementCost = reader.ReadContentAsFloat();
                    break;
                case "Width":
                    reader.Read();
                    Width = reader.ReadContentAsInt();
                    break;
                case "Height":
                    reader.Read();
                    Height = reader.ReadContentAsInt();
                    break;
                case "LinksToNeighbours":
                    reader.Read();
                    linksToNeighbour = reader.ReadContentAsBoolean();
                    break;
                case "EnclosesRooms":
                    reader.Read();
                    roomEnclosure = reader.ReadContentAsBoolean();
                    break;
                case "BuildingJob":
                    float jobTime = float.Parse(reader.GetAttribute("jobTime"));

                    List<Inventory> invs = new List<Inventory>();

                    XmlReader invs_reader = reader.ReadSubtree();

                    while (invs_reader.Read())
                    {
                        //Debug.Log("invs_reader: " + invs_reader.Name);
                        if (invs_reader.Name == "Inventory")
                        {
                            // Found an inventory requirement, add it to the list
                            invs.Add(new Inventory(
                                invs_reader.GetAttribute("objectType"),
                                int.Parse(invs_reader.GetAttribute("amount")),
                                0
                            ));
                        }
                    }

                    Job j = new Job(null,
                        objectType,
                        FurnitureActions.JobComplete_FurnitureBuilding,
                        jobTime,
                        invs.ToArray());

                    World.Current.SetFurnitureJobPrototype(j, this);

                    break;
                case "OnUpdate":
                    string functionName = reader.GetAttribute("FunctionName");
                    RegisterUpdateAction(functionName);
                    break;
                case "Params":
                    ReadXmlParams(reader);
                    break;
            }
        }
    }

    public void ReadXml(XmlReader reader)
    {
        // X, Y, and objectType have already been set 
        // and should be assigned to a tile
        //movementCost = int.Parse(reader.GetAttribute("movementCost"));

        ReadXmlParams(reader);
    }

    public void ReadXmlParams(XmlReader reader)
    {
        // X, Y, and objectType have already been set 
        // and should be assigned to a tile
        //movementCost = int.Parse(reader.GetAttribute("movementCost"));

        if (reader.ReadToDescendant("Param"))
        {
            do
            {
                string k = reader.GetAttribute("name");
                float v = float.Parse(reader.GetAttribute("value"));
                furnParameters[k] = v;
            } while (reader.ReadToNextSibling("Param"));
        }
    }

    /// <summary>
    /// Gets the custom furniture parameter from a string key.
    /// </summary>
    /// <param name="key">The key</param>
    /// <param name="default_value"></param>
    /// <returns>The parameter</returns>
    public float GetParameter(string key, float default_value = 0)
    {
        if (furnParameters.ContainsKey(key) == false)
        {
            return default_value;
        }

        return furnParameters[key];
    }

    public void SetParameter(string key, float value)
    {
        furnParameters[key] = value;
    }

    public void ChangeParameter(string key, float value)
    {
        if (furnParameters.ContainsKey(key) == false)
        {
            furnParameters[key] = value;
        }

        furnParameters[key] += value;
    }

    /// <summary>
    /// Registers a function that will be called every Update.
    /// </summary>
    /// <param name="a">The function</param>
    public void RegisterUpdateAction(string luaFunctionName)
    {
        updateActions.Add(luaFunctionName);
    }


    public void UnregisterUpdateAction(string luaFunctionName)
    {
        updateActions.Remove(luaFunctionName);
    }

    public int JobCount()
    {
        return jobs.Count;
    }

    public void AddJob(Job j)
    {
        j.furniture = this;
        jobs.Add(j);
        j.RegisterJobStoppedCallback(OnJobStopped);
        World.Current.jobQueue.Enqueue(j);
    }

    void OnJobStopped(Job j)
    {
        RemoveJob(j);
    }

    protected void RemoveJob(Job j)
    {
        j.UnregisterJobStoppedCallback(OnJobStopped);
        jobs.Remove(j);
        j.furniture = null;
    }

    protected void ClearJobs()
    {
        Job[] jobs_array = jobs.ToArray();
        foreach (Job j in jobs_array)
        {
            RemoveJob(j);
        }
    }

    public void CancelJobs()
    {
        Job[] jobs_array = jobs.ToArray();
        foreach (Job j in jobs_array)
        {
            j.CancelJob();
        }
    }

    public bool IsStockpile()
    {
        return objectType == "Stockpile";
    }

    public void Deconstruct()
    {
        Debug.Log("Deconstruct");

        tile.UnplaceFurniture();

        if (cbOnRemoved != null)
            cbOnRemoved(this);

        // Do we need ot recalculate our rooms?
        if (roomEnclosure)
        {
            Room.DoRoomFloodFill(this.tile);
        }

        World.Current.InvalidateTileGraph();

        // At this point, no DATA structures should be pointing to us, so we
        // should get garbage-collected
    }

    public Tile GetJobSpotTile()
    {
        return World.Current.GetTileAt(tile.X + (int)jobSpotOffset.x, tile.Y + (int)jobSpotOffset.y);
    }

    public Tile GetSpawnSpotTile()
    {
        // TODO: allow us to customize this
        return World.Current.GetTileAt(tile.X + (int)jobSpawnSpotOffset.x, tile.Y + (int)jobSpawnSpotOffset.y);
    }
}
