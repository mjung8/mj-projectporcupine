using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;

public class WorldController : MonoBehaviour
{
    public static WorldController Instance { get; protected set; }

    // World and tile data
    public World world { get; protected set; }

    static bool loadWorld = false;

    public GameObject saveFileDialogBox;

    // Use this for initialization
    // OnEnable runs first
    void OnEnable()
    {

        if (Instance != null)
        {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;

        if (loadWorld)
        {
            loadWorld = false;
            CreateWorldFromSaveFile();
        }
        else
        {
            CreateEmptyWorld();
        }
    }

    void Update()
    {
        // TODO: Add pause/unpause, speed, etc...
        world.Update(Time.deltaTime);
    }

    /// <summary>
    /// Gets the tile at the unity-space coordinates.
    /// </summary>
    /// <param name="coord">Unity world-space coordinates.</param>
    /// <returns>The tile at the world coordinate.</returns>
    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x + 0.5f);
        int y = Mathf.FloorToInt(coord.y + 0.5f);

        return world.GetTileAt(x, y);
    }

    public void NewWorld()
    {
        Debug.Log("NewWorld button clicked");

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ShowSaveDialog()
    {
        // When "Save" is clicked, show the user a file dialog box
        // asking for a filename to save the game. The user can overwrite existing.

        // When the dialog box is closed with Save/OK, do the save. 
        // User can close/cancel the dialog box to do nothing.

        saveFileDialogBox.SetActive(true);
    }

    string FileSaveBasePath()
    {
        return Path.Combine(Application.persistentDataPath, "Saves");
    }

    public void SaveDialogOkayWasClicked()
    {
        // TODO:
        // Check to see if the file already exists
        // If so, ask for overwrite confirmation.

        // Get the file name from the save file dialog box
        string fileName = saveFileDialogBox.GetComponentInChildren<InputField>().text;

        // Is the filename valid? i.e. may want to ban path delimiters (/ \ or :) and periods?

        // Right now fileName is just what as in the dialog box. Need to pad this out to the full
        // path and an extension
        // like: C:\Users\user\ApplicationData\MyCompanyName\MyGameName\Save\SaveGameName123.sav
        // Application.persistentDataPath = C:\Users\user\ApplicationData\MyCompanyName\MyGameName\
        string filePath = Path.Combine(FileSaveBasePath(), fileName + ".sav");

        // At this point, filePath should look right

        if (File.Exists(filePath) == true)
        {
            // TODO: Do file overwrite dialog box.
            return;
        }

        saveFileDialogBox.SetActive(false);

        SaveWorld(filePath);
    }

    public void SaveWorld(string filePath)
    {
        // This function gets called when the user confirms a filename in dialog box.

        Debug.Log("SaveWorld button clicked");

        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextWriter writer = new StringWriter();
        serializer.Serialize(writer, world);
        writer.Close();

        Debug.Log(writer.ToString());

        //PlayerPrefs.SetString("SaveGame00", writer.ToString());

        // Create/overwrite the save file with the xml text
        // Make sure the save folder exists
        if (Directory.Exists(FileSaveBasePath()) == false)
        {
            // NOTE: This can throw an exception if we can't create the folder,
            // but this shouldn't happen since we have the ability to save to
            // persistent data by definition, unless something is wrong with the device.
            Directory.CreateDirectory(FileSaveBasePath());
        }

        File.WriteAllText(filePath, writer.ToString());
    }

    public void LoadWorld()
    {
        Debug.Log("LoadWorld button clicked");

        // Reload the scene to reset all data (and purge all references)
        loadWorld = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void CreateEmptyWorld()
    {
        // Create a world with Empty tiles
        world = new World(100, 100);

        // Center the camera
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }

    void CreateWorldFromSaveFile()
    {
        Debug.Log("CreateWorldFromSaveFile");

        // Create a world from save file data
        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextReader reader = new StringReader(PlayerPrefs.GetString("SaveGame00"));
        Debug.Log(reader.ToString());
        world = (World)serializer.Deserialize(reader);
        reader.Close();

        // Center the camera
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }
}
