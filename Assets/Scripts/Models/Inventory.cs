using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MoonSharp.Interpreter;

/// <summary>
/// Inventory are things that are lying on the floor/stockpile like a bunch of metal bars
/// or uninstalled furniture.
/// </summary>
[MoonSharpUserData]
public class Inventory : IXmlSerializable, ISelectable
{
    public string objectType = "Steel Plate";
    public int maxStackSize = 50;

    protected int _stackSize = 1;
    public int stackSize
    {
        get { return _stackSize; }
        set
        {
            if (_stackSize != value)
            {
                _stackSize = value;
                if (cbInventoryChanged != null)
                {
                    cbInventoryChanged(this);
                }
            }
        }
    }

    public event Action<Inventory> cbInventoryChanged;

    public Tile tile;
    public Character character;

    public Inventory()
    {

    }

    public Inventory(string objectType, int maxStackSize, int stackSize)
    {
        this.objectType = objectType;
        this.maxStackSize = maxStackSize;
        this.stackSize = stackSize;
    }

    protected Inventory(Inventory other)
    {
        objectType = other.objectType;
        maxStackSize = other.maxStackSize;
        stackSize = other.stackSize;
    }

    public virtual Inventory Clone()
    {
        return new Inventory(this);
    }

    #region ISelectableInterface
    public string GetName()
    {
        return this.objectType;
    }

    public string GetDescription()
    {
        return "A stack of inventory.";
    }

    public string GetHitPointString()
    {
        return "";  // Does inventory have hitpoints? How does it get destroyed? Maybe it's just a percentage chance based on damage.
    }
    #endregion

    #region IXmlSerializable

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", tile.X.ToString());
        writer.WriteAttributeString("Y", tile.Y.ToString());
        writer.WriteAttributeString("objectType", objectType);
        writer.WriteAttributeString("maxStackSize", maxStackSize.ToString());
        writer.WriteAttributeString("stackSize", stackSize.ToString());
    }

    public void ReadXml(XmlReader reader)
    {
    }

    #endregion
}
