using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using MoonSharp;
using MoonSharp.Interpreter.Interop;

[MoonSharpUserData]
public class Furniture : IXmlSerializable, ISelectable
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

    //public Func<Furniture, ENTERABILITY> IsEnterable;
    protected string isEnterableAction;

    protected List<string> replaceableFurniture = new List<string>();

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

            if (powerValue > 0 && isPowerGenerator == false)
            {
                if (World.Current.powerSystem.RequestPower(this) == false)
                {
                    World.Current.powerSystem.RegisterPowerConsumer(this);
                    return;
                }
            }

            FurnitureActions.CallFunctionsWithFurniture(updateActions.ToArray(), this, deltaTime);
        }
    }

    public ENTERABILITY IsEnterable()
    {
        if (isEnterableAction == null || isEnterableAction.Length == 0)
            return ENTERABILITY.Yes;

        //FurnitureActions.CallFunctionsWithFurniture(isEnterableActions.ToArray(), this);
        DynValue ret = FurnitureActions.CallFunction(isEnterableAction, this);

        return (ENTERABILITY)ret.Number;
    }

    // This is true if the Furniture produces power
    public bool isPowerGenerator;
    // If it is a generator this is the amount of power it produces otherwise this is the amount it consumes.
    public float powerValue;

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

    private string Description;

    public List<string> ReplaceableFurniture
    {
        get
        {
            return replaceableFurniture;
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

    public string LocalizationCode { get; protected set; }
    public string UnlocalizedDescription { get; protected set; }

    public Color tint = Color.white;

    public bool linksToNeighbour
    {
        get; protected set;
    }

    public event Action<Furniture> cbOnChanged;
    public event Action<Furniture> cbOnRemoved;

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
        this.Description = other.Description;
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

        this.isEnterableAction = other.isEnterableAction;

        this.isPowerGenerator = other.isPowerGenerator;
        this.powerValue = other.powerValue;

        if (isPowerGenerator == true)
        {
            World.Current.powerSystem.RegisterPowerSupply(this);
        }
        else if (powerValue > 0)
        {
            World.Current.powerSystem.RegisterPowerConsumer(this);
        }

        if (other.funcPositionValidation != null)
            this.funcPositionValidation = (Func<Tile, bool>)other.funcPositionValidation.Clone();

        this.LocalizationCode = other.LocalizationCode;
        this.UnlocalizedDescription = other.UnlocalizedDescription;
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

                // Check to see if there is furniture which is replaceable
                bool isReplaceable = false;

                if (t2.furniture != null)
                {
                    for (int i = 0; i < ReplaceableFurniture.Count; i++)
                    {
                        if (t2.furniture.Name == ReplaceableFurniture[i])
                        {
                            isReplaceable = true;
                        }

                    }
                }

                // Make sure tile is floor
                if (t2.Type != TileType.Floor)
                {
                    return false;
                }

                // Make sure tile doesn't already have furniture
                if (t2.furniture != null && isReplaceable == false)
                {
                    return false;
                }
            }
        }

        return true;
    }

    [MoonSharpVisible(true)]
    private void UpdateOnChanged(Furniture furn)
    {
        if (cbOnChanged != null)
        {
            cbOnChanged(furn);
        }
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
                case "Description":
                    reader.Read();
                    Description = reader.ReadContentAsString();
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
                case "CanReplaceFurniture":
                    replaceableFurniture.Add(reader.GetAttribute("objectName").ToString());
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
                        invs.ToArray(),
                        false);

                    World.Current.SetFurnitureJobPrototype(j, this);

                    break;
                case "OnUpdate":
                    string functionName = reader.GetAttribute("FunctionName");
                    RegisterUpdateAction(functionName);
                    break;
                case "IsEnterable":
                    isEnterableAction = reader.GetAttribute("FunctionName");
                    break;
                case "JobSpotOffset":
                    jobSpotOffset = new Vector2(
                        int.Parse(reader.GetAttribute("X")),
                        int.Parse(reader.GetAttribute("Y"))
                    );
                    break;
                case "JobSpawnSpotOffset":
                    jobSpawnSpotOffset = new Vector2(
                        int.Parse(reader.GetAttribute("X")),
                        int.Parse(reader.GetAttribute("Y"))
                    );
                    break;
                case "PowerGenerator":
                    isPowerGenerator = true;
                    powerValue = float.Parse(reader.GetAttribute("supply"));
                    break;
                case "Power":
                    reader.Read();
                    powerValue = reader.ReadContentAsFloat();
                    break;
                case "Params":
                    ReadXmlParams(reader);
                    break;
                case "LocalizationCode":
                    reader.Read();
                    LocalizationCode = reader.ReadContentAsString();
                    break;

                case "UnlocalizedDescription":
                    reader.Read();
                    UnlocalizedDescription = reader.ReadContentAsString();
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
    public float GetParameter(string key, float default_value)
    {
        if (furnParameters.ContainsKey(key) == false)
        {
            return default_value;
        }

        return furnParameters[key];
    }

    /// <summary>
    /// for Lua
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public float GetParameter(string key)
    {
        return GetParameter(key, 0);
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

    public void RegisterIsEnterableAction(string luaFunctionName)
    {
        updateActions.Add(luaFunctionName);
    }


    public void UnregisterIsEnterableAction(string luaFunctionName)
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
        j.cbJobStopped += OnJobStopped;
        World.Current.jobQueue.Enqueue(j);
    }

    void OnJobStopped(Job j)
    {
        RemoveJob(j);
    }

    protected void RemoveJob(Job j)
    {
        j.cbJobStopped -= OnJobStopped;
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

        //World.Current.InvalidateTileGraph();
        if (World.Current.tileGraph != null)
        {
            World.Current.tileGraph.RegenerateGraphAtTile(tile);
        }

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

    #region ISelectableInterface
    public string GetName()
    {
        return LocalizationCode; //this.Name;
    }

    public string GetDescription()
    {
        return UnlocalizedDescription; //this.Description;
    }

    public string GetHitPointString()
    {
        return "18/18"; // TODO: add a hitpoint system to everything
    }
    #endregion
}
