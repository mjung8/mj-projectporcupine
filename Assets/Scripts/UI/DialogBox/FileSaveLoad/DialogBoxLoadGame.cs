using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Xml.Serialization;
using System.IO;

public class DialogBoxLoadGame : DialogBoxLoadSaveGame
{

    public void OkayWasClicked()
    {
        string fileName = gameObject.GetComponentInChildren<InputField>().text;

        // Is the filename valid? i.e. may want to ban path delimiters (/ \ or :) and periods?

        // Right now fileName is just what as in the dialog box. Need to pad this out to the full
        // path and an extension
        // like: C:\Users\user\ApplicationData\MyCompanyName\MyGameName\Save\SaveGameName123.sav
        // Application.persistentDataPath = C:\Users\user\ApplicationData\MyCompanyName\MyGameName\
        string filePath = Path.Combine(WorldController.Instance.FileSaveBasePath(), fileName + ".sav");

        // At this point, filePath should look right

        if (File.Exists(filePath) == false)
        {
            // TODO: Do file overwrite dialog box.
            Debug.LogError("File doesn't exists -- what?");
            CloseDialog();
            return;
        }

        CloseDialog();

        LoadWorld(filePath);
    }

    public void LoadWorld(string filePath)
    {
        // This function gets called when the user confirms a filename in dialog box.

        Debug.Log("LoadWorld button clicked");

        WorldController.Instance.LoadWorld(filePath);
    }
}
