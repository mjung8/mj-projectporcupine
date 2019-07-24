using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverRoomIndexText : MonoBehaviour
{
    // Every frame, this checks to see which tile is under
    // the mouse and updates the GetComponent<Text>.text param

    Text myText;
    MouseController mouseController;

    // Use this for initialization
    void Start()
    {
        myText = GetComponent<Text>();

        if (myText == null)
        {
            Debug.LogError("MouseOverRoomIndexText: No 'Text' UI component on this object");
            this.enabled = false;
        }

        mouseController = GameObject.FindObjectOfType<MouseController>();
        if (mouseController == null)
        {
            Debug.LogError("MouseController instance does not exist");
        }
    }

    // Update is called once per frame
    void Update()
    {
        Tile t = mouseController.GetMouseOverTile();

        string roomID = "N/A";

        if (t != null && t.room != null)
        {
            roomID = t.room.ID.ToString();
        }
        myText.text = "Room Index: " + roomID;
    }
}
