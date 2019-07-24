using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

public class SpriteManager : MonoBehaviour
{

    // Sprite Manager isn't responsible for creating GameObjects
    // That is going to be the job of the individual _SpriteController scripts
    // This script's job is to load all sprites from disk and keep them organized

    static public SpriteManager current;

    Dictionary<string, Sprite> sprites;

    // Use this for initialization
    void OnEnable()
    {
        current = this;

        LoadSprites();
    }

    void LoadSprites()
    {
        sprites = new Dictionary<string, Sprite>();

        string filePath = Path.Combine(Application.streamingAssetsPath, "Images");
        //filePath = Path.Combine(filePath, "CursorCircle.png");

        //LoadSprite("CursorCircle", filePath);
        LoadSpritesFromDirectory(filePath);
    }

    void LoadSpritesFromDirectory(string filePath)
    {
        Debug.Log("LoadSpritesFromDirectory: " + filePath);

        // First, see if there are any more sub-dirs and call this on that
        string[] subDirs = Directory.GetDirectories(filePath);
        foreach (string sd in subDirs)
        {
            LoadSpritesFromDirectory(sd);
        }

        string[] filesInDir = Directory.GetFiles(filePath);
        foreach (string fn in filesInDir)
        {
            // Is this an image file?
            // Unity's LoadImage supports PNG and JPG
            // NOTE: Alternatively, check file extensions but easier to try blindly
            string spriteCategory = new DirectoryInfo(filePath).Name;

            LoadImage(spriteCategory, fn);
        }
    }

    void LoadImage(string spriteCategory, string filePath)
    {
        //Debug.Log("Load image: " + filePath);

        // TODO: LoadImage is returning TRUE for non-images .meta .xml
        // bail for now as a temp fix
        if (filePath.Contains(".xml") || filePath.Contains(".meta") || filePath.Contains(".db"))
        {
            return;
        }

        // load the file into a texture
        byte[] imageBytes = File.ReadAllBytes(filePath);

        Texture2D imageTexture = new Texture2D(2, 2);   // Create a dummy instance
        // LoadImage correctly resizes the texture

        if (imageTexture.LoadImage(imageBytes))
        {
            // Image load successful
            // See if there's a matching XML file for this image
            string baseSpriteName = Path.GetFileNameWithoutExtension(filePath);
            string basePath = Path.GetDirectoryName(filePath);

            // NOTE: the extension must be in lowercase
            string xmlPath = Path.Combine(basePath, baseSpriteName + ".xml");

            if (File.Exists(xmlPath))
            {
                string xmlText = File.ReadAllText(xmlPath);
                // TODO: loop through xml file finding all sprite tags and calling 
                // LoadSprite for each of them
                XmlTextReader reader = new XmlTextReader(new StringReader(xmlText));

                if (reader.ReadToDescendant("Sprites") && reader.ReadToDescendant("Sprite"))
                {
                    do
                    {
                        ReadSpriteFromXml(spriteCategory, reader, imageTexture);
                    } while (reader.ReadToNextSibling("Sprite"));
                }
                else
                {
                    Debug.LogError("Could not find a <Sprites> tag.");
                    return;
                }
            }
            else
            {
                // File couldn't be read, probably because it doesn't exist
                // So assume the whole image is just one sprite with pixelPerUnit = 32
                LoadSprite(spriteCategory, baseSpriteName, imageTexture, new Rect(0, 0, imageTexture.width, imageTexture.height), 32);
            }

            // Attempt to load/parse t he XML file to get information on the sprite(s)


        }

        // File wasn't an image
    }

    void ReadSpriteFromXml(string spriteCategory, XmlReader reader, Texture2D imageTexture)
    {
        //Debug.Log("ReadSpriteFromXml");
        string name = reader.GetAttribute("name");
        int x = int.Parse(reader.GetAttribute("x"));
        int y = int.Parse(reader.GetAttribute("y"));
        int w = int.Parse(reader.GetAttribute("w"));
        int h = int.Parse(reader.GetAttribute("h"));
        int pixelPerUnit = int.Parse(reader.GetAttribute("pixelPerUnit"));

        LoadSprite(spriteCategory, name, imageTexture, new Rect(x, y, w, h), pixelPerUnit);
    }

    void LoadSprite(string spriteCategory, string spriteName, Texture2D imageTexture, Rect spriteCoordinates, int pixelsPerUnit)
    {
        spriteName = spriteCategory + "/" + spriteName;
        //Debug.Log("LoadSprite: " + spriteName);
        Vector2 pivotPoint = new Vector2(0.5f, 0.5f);   // range from 0..1 -- so 0.5f is center

        Sprite s = Sprite.Create(imageTexture, spriteCoordinates, pivotPoint, pixelsPerUnit);

        sprites[spriteName] = s;
    }

    public Sprite GetSprite(string categoryName, string spriteName)
    {
        spriteName = categoryName + "/" + spriteName;

        if (sprites.ContainsKey(spriteName) == false)
        {
            //Debug.LogError("No sprite with name: " + spriteName);
            return null;
        }

        return sprites[spriteName];
    }

}
