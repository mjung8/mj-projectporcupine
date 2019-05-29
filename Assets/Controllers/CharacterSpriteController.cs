using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpriteController : MonoBehaviour
{

    Dictionary<Character, GameObject> characterGameObjectMap;

    Dictionary<string, Sprite> characterSprites;

    World world
    {
        get { return WorldController.Instance.world; }
    }

    // Use this for initialization
    void Start()
    {
        LoadSprites();

        // Instantiate the dictionary that tracks which GameObject is rendering which Tile data
        characterGameObjectMap = new Dictionary<Character, GameObject>();

        // Reigster a callback so we know when a character is created
        world.RegisterCharacterCreated(OnCharacterCreated);

        //DEBUG
        world.CreateCharacter(world.GetTileAt(world.Width / 2, world.Height / 2));
    }

    void LoadSprites()
    {
        characterSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Characters/");

        Debug.Log("LOADED RESOURCE: ");
        foreach (Sprite s in sprites)
        {
            Debug.Log(s);
            characterSprites[s.name] = s;
        }
    }

    public void OnCharacterCreated(Character character)
    {
        //Debug.Log("OnfurnitureCreated");
        // Create a visual GameObject linked to this data

        // FIXME: does not consider multi-tile objects nor rotated objects

        // Create a new GameObject and add to the scene
        GameObject char_go = new GameObject();

        // Add the tile/GO pair to the dictionary
        characterGameObjectMap.Add(character, char_go);

        char_go.name = "Character";
        char_go.transform.position = new Vector3(character.currTile.X, character.currTile.Y, 0);
        char_go.transform.SetParent(this.transform, true);

        SpriteRenderer sr = char_go.AddComponent<SpriteRenderer>();
        sr.sprite = characterSprites["p1_front"];
        sr.sortingLayerName = "Characters";

        // Register our callback so that or GameObject gets updated whenever
        // the object's info changes
        //character.RegisterOnChangedCallback(OnFurnitureChanged);
    }

    //void OnFurnitureChanged(Furniture furn)
    //{
    //     Make sure furniture's graphics are correct
    //    if (furnitureGameObjectMap.ContainsKey(furn) == false)
    //    {
    //        Debug.LogError("OnFurnitureChanged -- trying to change visuals for furniture not in our map.");
    //        return;
    //    }

    //    GameObject furn_go = furnitureGameObjectMap[furn];
    //    furn_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furn);
    //}
}
