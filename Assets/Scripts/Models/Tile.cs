using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;


// TileType is  the base type of the tile. In some tile-based games, that might be
// the terrain type. For us, we only need to differentiate between empty space
// and floor (aka the station structure/scaffold). Walls/Doors/etc... will be
// furnitures sitting on top of the floor
public enum TileType { Empty, Floor };
public enum ENTERABILITY { Yes, Never, Soon };

public class Tile : IXmlSerializable
{

    private TileType _type = TileType.Empty;
    public TileType Type
    {
        get
        {
            return _type;
        }

        set
        {
            TileType oldType = _type;
            _type = value;
            // Call the callback and let things know we've changed
            if (cbTileChanged != null && oldType != _type)
                cbTileChanged(this);
        }
    }

    // LooseObject is like a stack of something
    Inventory inventory;

    public Room room;

    // furniture is a wall, door, furniture
    public Furniture furniture
    {
        get; protected set;
    }

    public Job pendingFunitureJob;

    // Know the context in which this exists...
    public World world { get; protected set; }
    public int X { get; protected set; }
    public int Y { get; protected set; }

    float baseTileMovementCost = 1; // FIXME: bad hardcoded

    public float movementCost
    {
        get
        {
            if (Type == TileType.Empty)
                return 0;   // Unwalkable

            if (furniture == null)
                return baseTileMovementCost;

            return baseTileMovementCost * furniture.movementCost;
        }
    }

    // The function to callback any time the data changes
    Action<Tile> cbTileChanged;

    /// <summary>
    /// Initialize a new instance of the Tile class.
    /// </summary>
    /// <param name="world">A World instance.</param>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    public Tile(World world, int x, int y)
    {
        this.world = world;
        this.X = x;
        this.Y = y;
    }

    /// <summary>
    /// Register a function to be called back when the tile type changes.
    /// </summary>
    /// <param name="callback"></param>
    public void RegisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileChanged += callback;
    }

    /// <summary>
    /// Unregister a callback.
    /// </summary>
    /// <param name="callback"></param>
    public void UnregisterTileTypeChangedCallback(Action<Tile> callback)
    {
        cbTileChanged -= callback;
    }

    public bool PlaceFurniture(Furniture objInstance)
    {
        if (objInstance == null)
        {
            // We are uninstalling whatever was here
            furniture = null;
            return true;
        }

        // objInstance isn't null

        if (furniture != null)
        {
            Debug.LogError("Trying to assign a furniture to a tile that already has one!");
            return false;
        }

        // At this point, everything's fine!
        furniture = objInstance;
        return true;
    }

    // Tells us if two tiles are adjaccent
    public bool IsNeighbour(Tile tile, bool diagOkay = false)
    {
        return
            Mathf.Abs(this.X - tile.X) + Mathf.Abs(this.Y - tile.Y) == 1 ||
            (diagOkay && (Mathf.Abs(this.X - tile.X) == 1 && Mathf.Abs(this.Y - tile.Y) == 1));
    }

    /// <summary>
    /// Gets the neighbours.
    /// </summary>
    /// <param name="diagOkay">Is diagonal movement ok?</param>
    /// <returns>The neighbours.</returns>
    public Tile[] GetNeighbours(bool diagOkay = false)
    {
        Tile[] ns;

        if (diagOkay == false)
        {
            ns = new Tile[4];   // N E S W
        }
        else
        {
            ns = new Tile[8];   // N E S W NE SE SW NW
        }

        Tile n;

        n = world.GetTileAt(X, Y + 1);
        ns[0] = n;  // All of these could be null and that's ok.
        n = world.GetTileAt(X + 1, Y);
        ns[1] = n;  // All of these could be null and that's ok.
        n = world.GetTileAt(X, Y - 1);
        ns[2] = n;  // All of these could be null and that's ok.
        n = world.GetTileAt(X - 1, Y);
        ns[3] = n;  // All of these could be null and that's ok.

        if (diagOkay == true)
        {
            n = world.GetTileAt(X + 1, Y + 1);
            ns[4] = n;  // All of these could be null and that's ok.
            n = world.GetTileAt(X + 1, Y - 1);
            ns[5] = n;  // All of these could be null and that's ok.
            n = world.GetTileAt(X - 1, Y - 1);
            ns[6] = n;  // All of these could be null and that's ok.
            n = world.GetTileAt(X - 1, Y + 1);
            ns[7] = n;  // All of these could be null and that's ok.
        }

        return ns;
    }

    public XmlSchema GetSchema()
    {
        throw new NotImplementedException();
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", X.ToString());
        writer.WriteAttributeString("Y", Y.ToString());
        writer.WriteAttributeString("Type", ((int)Type).ToString());
    }

    public void ReadXml(XmlReader reader)
    {
        Type = (TileType)int.Parse(reader.GetAttribute("Type"));
    }

    public ENTERABILITY IsEnterable()
    {
        if (movementCost == 0)
        {
            return ENTERABILITY.Never;
        }

        // Check furniture to see if it's enterable
        if (furniture != null && furniture.IsEnterable != null)
        {
            return furniture.IsEnterable(furniture);
        }

        return ENTERABILITY.Yes;
    }

    public Tile North()
    {
        return world.GetTileAt(X, Y + 1);
    }

    public Tile South()
    {
        return world.GetTileAt(X, Y - 1);
    }

    public Tile East()
    {
        return world.GetTileAt(X + 1, Y);
    }

    public Tile West()
    {
        return world.GetTileAt(X - 1, Y);
    }

}
