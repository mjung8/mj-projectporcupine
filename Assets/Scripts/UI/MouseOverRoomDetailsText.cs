﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverRoomDetailsText : MonoBehaviour
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
            Debug.LogError("MouseOverRoomDetailsText: No 'Text' UI component on this object");
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

        if (t == null || t.room == null)
        {
            myText.text = "";
            return;
        }

        string s = "";
        foreach (string g in t.room.GetGasNames())
        {
            s += g + ": " + t.room.GetGasPressure(g) + " (" + (t.room.GetGasPercentage(g) * 100) + "%) ";
        }

        myText.text = s;
    }
}
