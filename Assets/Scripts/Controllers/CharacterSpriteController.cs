using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpriteController : MonoBehaviour
{

    Dictionary<Character, GameObject> characterGameObjectMap;

    World world
    {
        get { return WorldController.Instance.world; }
    }

    // Use this for initialization
    void Start()
    {
        // Instantiate the dictionary that tracks which GameObject is rendering which Tile data
        characterGameObjectMap = new Dictionary<Character, GameObject>();

        // Reigster a callback so we know when a character is created
        world.RegisterCharacterCreated(OnCharacterCreated);

        // Check for pre-existing characters which won't do the callback
        foreach (Character c in world.characters)
        {
            OnCharacterCreated(c);
        }

        //c.SetDestination(world.GetTileAt(world.Width / 2 + 5, world.Height / 2));
    }

    public void OnCharacterCreated(Character c)
    {
        // Create a visual GameObject linked to this data

        // FIXME: does not consider multi-tile objects nor rotated objects

        // Create a new GameObject and add to the scene
        GameObject char_go = new GameObject();

        // Add the tile/GO pair to the dictionary
        characterGameObjectMap.Add(c, char_go);

        char_go.name = "Character";
        char_go.transform.position = new Vector3(c.X, c.Y, 0);
        char_go.transform.SetParent(this.transform, true);

        SpriteRenderer sr = char_go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteManager.current.GetSprite("Character", "p1_front");
        sr.sortingLayerName = "Characters";

        // Register our callback so that or GameObject gets updated whenever
        // the object's info changes
        c.RegisterOnChangedCallback(OnCharacterChanged);
    }

    void OnCharacterChanged(Character c)
    {
        //Make sure character's graphics are correct
        if (characterGameObjectMap.ContainsKey(c) == false)
        {
            Debug.LogError("OnCharacterChanged -- trying to change visuals for character not in our map.");
            return;
        }

        GameObject char_go = characterGameObjectMap[c];
        //furn_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furn);
        char_go.transform.position = new Vector3(c.X, c.Y, 0);
    }
}
