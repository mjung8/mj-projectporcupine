using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySpriteController : MonoBehaviour
{
    public GameObject inventoryUIPrefab;

    Dictionary<Inventory, GameObject> inventoryGameObjectMap;

    Dictionary<string, Sprite> inventorySprites;

    World world
    {
        get { return WorldController.Instance.world; }
    }

    // Use this for initialization
    void Start()
    {
        LoadSprites();

        // Instantiate the dictionary that tracks which GameObject is rendering which Tile data
        inventoryGameObjectMap = new Dictionary<Inventory, GameObject>();

        // Reigster a callback so we know when a character is created
        world.RegisterInventoryCreated(OnInventoryCreated);

        // Check for pre-existing characters which won't be do the callback
        foreach (string objectType in world.inventoryManager.inventories.Keys)
        {
            foreach (Inventory inv in world.inventoryManager.inventories[objectType])
            {
                OnInventoryCreated(inv);
            }
        }

        //c.SetDestination(world.GetTileAt(world.Width / 2 + 5, world.Height / 2));
    }

    void LoadSprites()
    {
        inventorySprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Inventory/");

        foreach (Sprite s in sprites)
        {
            inventorySprites[s.name] = s;
        }
    }

    public void OnInventoryCreated(Inventory inv)
    {
        Debug.Log("OnInventoryCreated");
        // Create a visual GameObject linked to this data

        // FIXME: does not consider multi-tile objects nor rotated objects

        // Create a new GameObject and add to the scene
        GameObject inv_go = new GameObject();

        // Add the tile/GO pair to the dictionary
        inventoryGameObjectMap.Add(inv, inv_go);

        inv_go.name = inv.objectType;
        inv_go.transform.position = new Vector3(inv.tile.X, inv.tile.Y, 0);
        inv_go.transform.SetParent(this.transform, true);

        SpriteRenderer sr = inv_go.AddComponent<SpriteRenderer>();
        sr.sprite = inventorySprites[inv.objectType];
        sr.sortingLayerName = "Inventory";

        if (inv.maxStackSize > 1)
        {
            // This is a stackable object so add the InventoryUI component
            GameObject ui_go = Instantiate(inventoryUIPrefab);
            ui_go.transform.SetParent(inv_go.transform);
            ui_go.transform.localPosition = Vector3.zero;
            ui_go.GetComponentInChildren<Text>().text = inv.stackSize.ToString();
        }

        // Register our callback so that or GameObject gets updated whenever
        // the object's info changes
        //inv.RegisterOnChangedCallback(OnCharacterChanged);
    }

    void OnInventoryChanged(Inventory inv)
    {
        //Make sure furniture's graphics are correct
        if (inventoryGameObjectMap.ContainsKey(inv) == false)
        {
            Debug.LogError("OnInventoryChanged -- trying to change visuals for Inventory not in our map.");
            return;
        }

        GameObject inv_go = inventoryGameObjectMap[inv];
        //furn_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furn);
        inv_go.transform.position = new Vector3(inv.tile.X, inv.tile.Y, 0);
    }
}
