using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Xml.Serialization;
using System.IO;
using UnityEditor;

public class DialogBoxLoadGame : DialogBoxLoadSaveGame
{
    public GameObject dialog;
    GameObject go;
    public bool pressedDelete;
    Component fileItem;

    void Update()
    {
        if (pressedDelete)
        {
            SetButtonLocation(fileItem);
        }
    }

    public void SetFileItem(Component item)
    {
        fileItem = item;
    }

    public void SetButtonLocation(Component item)
    {
        GameObject go = GameObject.FindGameObjectWithTag("DeleteButton");
        go.transform.position = new Vector3(item.transform.position.x + 110f, item.transform.position.y - 8f);
    }

    public void OkayWasClicked()
    {
        string fileName = gameObject.GetComponentInChildren<InputField>().text;

        // Is the filename valid? i.e. may want to ban path delimiters (/ \ or :) and periods?

        // Right now fileName is just what as in the dialog box. Need to pad this out to the full
        // path and an extension
        // like: C:\Users\user\ApplicationData\MyCompanyName\MyGameName\Save\SaveGameName123.sav
        // Application.persistentDataPath = C:\Users\user\ApplicationData\MyCompanyName\MyGameName\
        string saveDirectoryPath = WorldController.Instance.FileSaveBasePath();

        EnsureDirectoryExists(saveDirectoryPath);

        string filePath = System.IO.Path.Combine(saveDirectoryPath, fileName + ".sav");

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

    public override void CloseDialog()
    {
        GameObject go = GameObject.FindGameObjectWithTag("DeleteButton");
        go.GetComponent<Image>().color = new Color(255, 255, 255, 0);
        pressedDelete = false;
        base.CloseDialog();
    }

    public void DeleteFile()
    {
        string fileName = gameObject.GetComponentInChildren<InputField>().text;

        string saveDirectoryPath = WorldController.Instance.FileSaveBasePath();

        EnsureDirectoryExists(saveDirectoryPath);

        string filePath = System.IO.Path.Combine(saveDirectoryPath, fileName + ".sav");

        if (File.Exists(filePath) == false)
        {

            Debug.LogError("File doesn't exist.  What?");
            CloseDialog();
            return;
        }

        CloseSureDialog();
        FileUtil.DeleteFileOrDirectory(filePath);
        CloseDialog();
        ShowDialog();
    }

    public void CloseSureDialog()
    {
        dialog.SetActive(false);
    }

    public void DeleteWasClicked()
    {

        dialog.SetActive(true);
    }

    public void LoadWorld(string filePath)
    {
        // This function gets called when the user confirms a filename in dialog box.

        Debug.Log("LoadWorld button clicked");

        WorldController.Instance.LoadWorld(filePath);
    }
}
