﻿using System.Collections;
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

    public static readonly Color secondaryColor = new Color(0.9f, 0.9f, 0.9f);

    /// <summary>
    /// If directory doesn't exist EnsureDirectoryExists will create one.
    /// </summary>
    /// <param name="directoryPath">Full directory path.</param>
    public void EnsureDirectoryExists(string directoryPath)
    {
        if (Directory.Exists(directoryPath) == false)
        {
            Debug.LogWarning("Directory: " + directoryPath + " doesn't exist - creating.");
            Directory.CreateDirectory(directoryPath);
        }
    }

    public override void ShowDialog()
    {
        base.ShowDialog();

        // Get list of files in save location
        string saveDirectoryPath = WorldController.Instance.FileSaveBasePath();

        EnsureDirectoryExists(saveDirectoryPath);

        DirectoryInfo saveDir = new DirectoryInfo(saveDirectoryPath);
        FileInfo[] saveGames = saveDir.GetFiles().OrderByDescending(f => f.CreationTime).ToArray();

        // Our save dialog has an input field which the fileListItems fill out when it's clicked
        InputField inputField = gameObject.GetComponentInChildren<InputField>();

        // Build file list by instantiating fileListItemPrefab

        for (int i = 0; i < saveGames.Length; i++)
        {
            FileInfo file = saveGames[i];
            GameObject go = GameObject.Instantiate(fileListItemPrefab);

            // Make sure this gameobject is a child of our list box
            go.transform.SetParent(fileList);

            string fileName = Path.GetFileNameWithoutExtension(file.FullName);

            go.GetComponentInChildren<Text>().text = string.Format("{0}\n<size=11><i>{1}</i></size>", fileName, file.CreationTime);

            DialogListItem listItem = go.GetComponent<DialogListItem>();
            listItem.fileName = fileName;
            listItem.inputField = inputField;

            go.GetComponent<Image>().color = (i % 2 == 0 ? Color.white : secondaryColor);
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
