using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Xml.Serialization;
using System.IO;

public class DialogBoxSaveGame : DialogBoxLoadSaveGame
{

    public void OkayWasClicked()
    {
        // TODO:
        // Check to see if the file already exists
        // If so, ask for overwrite confirmation.

        // Get the file name from the save file dialog box
        string fileName = gameObject.GetComponentInChildren<InputField>().text;

        // Is the filename valid? i.e. may want to ban path delimiters (/ \ or :) and periods?

        // Right now fileName is just what as in the dialog box. Need to pad this out to the full
        // path and an extension
        // like: C:\Users\user\ApplicationData\MyCompanyName\MyGameName\Save\SaveGameName123.sav
        // Application.persistentDataPath = C:\Users\user\ApplicationData\MyCompanyName\MyGameName\
        string filePath = Path.Combine(WorldController.Instance.FileSaveBasePath(), fileName + ".sav");

        // At this point, filePath should look right

        if (File.Exists(filePath) == true)
        {
            // TODO: Do file overwrite dialog box.
            Debug.LogError("File already exists -- overwriting the file for now.");
        }

        CloseDialog();

        SaveWorld(filePath);
    }

    public void SaveWorld(string filePath)
    {
        // This function gets called when the user confirms a filename in dialog box.

        Debug.Log("SaveWorld button clicked");

        XmlSerializer serializer = new XmlSerializer(typeof(World));
        TextWriter writer = new StringWriter();
        serializer.Serialize(writer, WorldController.Instance.world);
        writer.Close();

        Debug.Log(writer.ToString());

        //PlayerPrefs.SetString("SaveGame00", writer.ToString());

        // Create/overwrite the save file with the xml text
        // Make sure the save folder exists
        if (Directory.Exists(WorldController.Instance.FileSaveBasePath()) == false)
        {
            // NOTE: This can throw an exception if we can't create the folder,
            // but this shouldn't happen since we have the ability to save to
            // persistent data by definition, unless something is wrong with the device.
            Directory.CreateDirectory(WorldController.Instance.FileSaveBasePath());
        }

        File.WriteAllText(filePath, writer.ToString());
    }
}
