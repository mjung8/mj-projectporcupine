using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;

// TileType is  the base type of the tile. In some tile-based games, that might be
// the terrain type. For us, we only need to differentiate between empty space
// and floor (aka the station structure/scaffold). Walls/Doors/etc... will be
// furnitures sitting on top of the floor
public enum TileType
{
    Empty,
    Floor
};

public enum ENTERABILITY
{
    Yes,
    Never,
    Soon
};

[MoonSharpUserData]
public class Tile : IXmlSerializable, ISelectable
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
    public Inventory inventory { get; set; }

    public Room room;

    public List<Character> characters;

    // furniture is a wall, door, furniture
    public Furniture furniture
    {
        get; protected set;
    }

    public Job pendingBuildJob;

    // Know the context in which this exists...
    public int X { get; protected set; }
    public int Y { get; protected set; }

    float baseTileMovementCost = 1; // FIXME: bad hardcoded

    public float movementCost
    {
        get
        {
            // This prevents the character from walking in empty tiles. 
            // Disabled to allow the character to construct floor tiles.
            // TODO: Permanent solution for handling characters and empty tiles
            //if (Type == TileType.Empty)
            //    return 0;   // Unwalkable

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
    public Tile(int x, int y)
    {
        this.X = x;
        this.Y = y;
        characters = new List<Character>();
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

    public bool UnplaceFurniture()
    {
        // Just uninstalling. FIXME: what if we have a multi-tile furniture?

        if (furniture == null)
            return false;

        Furniture f = furniture;

        for (int x_off = X; x_off < (X + f.Width); x_off++)
        {
            for (int y_off = Y; y_off < (Y + f.Height); y_off++)
            {
                Tile t = World.Current.GetTileAt(x_off, y_off);
                t.furniture = null;
            }
        }

        return true;
    }

    public bool PlaceFurniture(Furniture objInstance)
    {
        if (objInstance == null)
        {
            return UnplaceFurniture();
        }

        if (objInstance.IsValidPosition(this) == false)
        {
            Debug.LogError("Trying to assign a furniture to a tile that isn't valid!");
            return false;
        }

        for (int x_off = X; x_off < (X + objInstance.Width); x_off++)
        {
            for (int y_off = Y; y_off < (Y + objInstance.Height); y_off++)
            {
                Tile t = World.Current.GetTileAt(x_off, y_off);
                t.furniture = objInstance;
            }
        }

        return true;
    }

    public bool PlaceInventory(Inventory inv)
    {
        if (inv == null)
        {
            inventory = null;
            return true;
        }

        if (inventory != null)
        {
            // There's already inventory here. Maybe combine stack?
            if (inventory.objectType != inv.objectType)
            {
                Debug.LogError("Trying to assign inventory to a tile that already has some of a different type!");
                return false;
            }

            int numToMove = inv.stackSize;
            if (inventory.stackSize + numToMove > inventory.maxStackSize)
            {
                numToMove = inventory.maxStackSize - inventory.stackSize;
            }

            inventory.stackSize += numToMove;
            inv.stackSize -= numToMove;

            return true;
        }

        // At this point we know that our current inventory
        // is actually null. Can't do a direct assignment because
        // the inventory manager needs to know that the old stack
        // is empty and has to be removed from previous lists.

        inventory = inv.Clone();
        inventory.tile = this;
        inv.stackSize = 0;

        return true;
    }

    // Called when the character has completed the job to change tile type
    public static void ChangeTileTypeJobComplete(Job theJob)
    {
        // FIXME: For now this is hardcoded to build floor
        theJob.tile.Type = theJob.jobTileType;

        // FIXME: I don't like having to manually and explicitly set
        // flags that preven conflicts. It's too easy to forget to set/clear them!
        theJob.tile.pendingBuildJob = null;
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

        n = World.Current.GetTileAt(X, Y + 1);
        ns[0] = n;  // All of these could be null and that's ok.
        n = World.Current.GetTileAt(X + 1, Y);
        ns[1] = n;  // All of these could be null and that's ok.
        n = World.Current.GetTileAt(X, Y - 1);
        ns[2] = n;  // All of these could be null and that's ok.
        n = World.Current.GetTileAt(X - 1, Y);
        ns[3] = n;  // All of these could be null and that's ok.

        if (diagOkay == true)
        {
            n = World.Current.GetTileAt(X + 1, Y + 1);
            ns[4] = n;  // All of these could be null and that's ok.
            n = World.Current.GetTileAt(X + 1, Y - 1);
            ns[5] = n;  // All of these could be null and that's ok.
            n = World.Current.GetTileAt(X - 1, Y - 1);
            ns[6] = n;  // All of these could be null and that's ok.
            n = World.Current.GetTileAt(X - 1, Y + 1);
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
        writer.WriteAttributeString("RoomID", room == null ? "-1" : room.ID.ToString());
        writer.WriteAttributeString("Type", ((int)Type).ToString());
    }

    public void ReadXml(XmlReader reader)
    {
        // X and Y have aleady been read/processed

        room = World.Current.GetRoomFromID(int.Parse(reader.GetAttribute("RoomID")));

        Type = (TileType)int.Parse(reader.GetAttribute("Type"));
    }

    public ENTERABILITY IsEnterable()
    {
        if (movementCost == 0)
        {
            return ENTERABILITY.Never;
        }

        // Check furniture to see if it's enterable
        if (furniture != null)
        {
            return furniture.IsEnterable();
        }

        return ENTERABILITY.Yes;
    }

    public Tile North()
    {
        return World.Current.GetTileAt(X, Y + 1);
    }

    public Tile South()
    {
        return World.Current.GetTileAt(X, Y - 1);
    }

    public Tile East()
    {
        return World.Current.GetTileAt(X + 1, Y);
    }

    public Tile West()
    {
        return World.Current.GetTileAt(X - 1, Y);
    }

    #region ISelectableInterface
    public string GetName()
    {
        return this._type.ToString(); ;
    }

    public string GetDescription()
    {
        return "The tile.";
    }

    public string GetHitPointString()
    {
        return "";  // Do tiles have hitpoints? Can flooring be damaged? Empty cannot be destroyed.
    }
    #endregion
}
