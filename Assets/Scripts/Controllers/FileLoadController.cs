﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Xml.Serialization;
using System.IO;

public class FileLoadController : DialogBoxController
{

    public GameObject fileListItemPrefab;
    public Transform fileList;

    // Use this for initialization
    void Start()
    {

    }

    public override void ShowDialog()
    {
        base.ShowDialog();

        // Get list of files in save location
        string filePath = WorldController.Instance.FileSaveBasePath();
        // existingSaves will contain the full path
        string[] existingSaves = Directory.GetFiles(filePath, "*.sav");

        // TODO: make sure saves are sorted by date/time with the newest on top

        // Our save dialog has an input field which the fileListItems fill out when it's clicked
        InputField inputField = gameObject.GetComponentInChildren<InputField>();

        // Build file list by instantiating fileListItemPrefab

        foreach (string file in existingSaves)
        {
            GameObject go = GameObject.Instantiate(fileListItemPrefab);

            // Make sure this gameobject is a child of our list box
            go.transform.SetParent(fileList);

            go.GetComponentInChildren<Text>().text = Path.GetFileNameWithoutExtension(file);

            go.GetComponent<DialogListItem>().inputField = inputField;
        }

    }

    public override void CloseDialog()
    {
        // Clear out all the children of the file list
        while(fileList.childCount > 0)
        {
            Transform c = fileList.GetChild(0);
            c.SetParent(null);  // Become Batman
            Destroy(c.gameObject);
        }

        // Could clear out the inputField but leaving old filename might make sense
        // Alternatively, a) clear out text box; b) append an incremental number (fileName 3);
        base.CloseDialog();
    }

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