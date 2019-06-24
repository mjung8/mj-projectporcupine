using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;

public class Furniture : IXmlSerializable
{
    public Dictionary<string, float> furnParameters;
    public Action<Furniture, float> updateActions;

    public Func<Furniture, ENTERABILITY> IsEnterable;

    public void Update(float deltaTime)
    {
        if (updateActions != null)
        {
            updateActions(this, deltaTime);
        }
    }

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
    public float movementCost { get; protected set; }

    public bool roomEnclosure { get; protected set; }

    // For example, a sofa might be 3x2 but graphics are only 3x1 (extra row for leg room)
    int width;
    int height;

    public bool linksToNeighbour
    {
        get; protected set;
    }

    public Action<Furniture> cbOnChanged;

    Func<Tile, bool> funcPositionValidation;

    // TODO: implement larger objects
    // TODO: implement object rotation

    // Empty constructor for serialization
    public Furniture()
    {
        furnParameters = new Dictionary<string, float>();
    }

    // Copy constructor
    protected Furniture(Furniture other)
    {
        this.objectType = other.objectType;
        this.movementCost = other.movementCost;
        this.roomEnclosure = other.roomEnclosure;
        this.width = other.width;
        this.height = other.height;
        this.linksToNeighbour = other.linksToNeighbour;

        this.furnParameters = new Dictionary<string, float>(other.furnParameters);

        if (other.updateActions != null)
            this.updateActions = (Action<Furniture, float>)other.updateActions.Clone();

        this.IsEnterable = other.IsEnterable;
    }

    virtual public Furniture Clone()
    {
        return new Furniture(this);
    }

    // Create furniture from parameter -- this will probably only be used for prototypes
    public Furniture (string objectType, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false, bool roomEnclosure = false)
    {
        this.objectType = objectType;
        this.movementCost = movementCost;
        this.roomEnclosure = roomEnclosure;
        this.width = width;
        this.height = height;
        this.linksToNeighbour = linksToNeighbour;

        this.funcPositionValidation = this.__IsValidPosition;

        furnParameters = new Dictionary<string, float>();
    }

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
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == furn.objectType)
            {
                // We have a northern neighbour with the same object type as us, so
                // tell it that it has changed by firing its callback
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x + 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == furn.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x, y - 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == furn.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x - 1, y);
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

    public void ReadXml(XmlReader reader)
    {
        // X, Y, and objectType have already been set 
        // and should be assigned to a tile
        //movementCost = int.Parse(reader.GetAttribute("movementCost"));

        if(reader.ReadToDescendant("Param"))
        {
            do
            {
                string k = reader.GetAttribute("name");
                float v = float.Parse(reader.GetAttribute("value"));
                furnParameters[k] = v;
            } while (reader.ReadToNextSibling("Param"));
        }
    }
    
}
