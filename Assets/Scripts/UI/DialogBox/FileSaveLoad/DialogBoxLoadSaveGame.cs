using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Xml.Serialization;
using System.IO;
using System.Linq;

// Monobehaviour > DialogBox > DialogBoxLoadSaveGame >
//                                         DialogBoxSaveGame
//                                         DialogBoxLoadGame

public class DialogBoxLoadSaveGame : DialogBox
{

    public GameObject fileListItemPrefab;
    public Transform fileList;

    public override void ShowDialog()
    {
        base.ShowDialog();

        // Get list of files in save location
        string directoryPath = WorldController.Instance.FileSaveBasePath();

        DirectoryInfo saveDir = new DirectoryInfo(directoryPath);
        FileInfo[] saveGames = saveDir.GetFiles().OrderByDescending(f => f.CreationTime).ToArray();

        // Our save dialog has an input field which the fileListItems fill out when it's clicked
        InputField inputField = gameObject.GetComponentInChildren<InputField>();

        // Build file list by instantiating fileListItemPrefab

        foreach (FileInfo file in saveGames)
        {
            GameObject go = GameObject.Instantiate(fileListItemPrefab);

            // Make sure this gameobject is a child of our list box
            go.transform.SetParent(fileList);

            go.GetComponentInChildren<Text>().text = Path.GetFileNameWithoutExtension(file.FullName);

            go.GetComponent<DialogListItem>().inputField = inputField;
        }

    }

    public override void CloseDialog()
    {
        // Clear out all the children of the file list
        while (fileList.childCount > 0)
        {
            Transform c = fileList.GetChild(0);
            c.SetParent(null);  // Become Batman
            Destroy(c.gameObject);
        }

        // Could clear out the inputField but leaving old filename might make sense
        // Alternatively, a) clear out text box; b) append an incremental number (fileName 3);
        base.CloseDialog();
    }

}
