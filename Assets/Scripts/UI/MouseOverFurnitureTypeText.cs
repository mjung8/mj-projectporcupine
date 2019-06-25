using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseOverFurnitureTypeText : MonoBehaviour {

    // Every frame, this checks to see which tile is under
    // the mouse and updates the GetComponent<Text>.text param

    Text myText;
    MouseController mouseController;

	// Use this for initialization
	void Start () {
        myText = GetComponent<Text>();

        if(myText == null)
        {
            Debug.LogError("MouseOverFurnitureTypeText: No 'Text' UI component on this object");
            this.enabled = false;
        }

        mouseController = GameObject.FindObjectOfType<MouseController>();
        if(mouseController == null)
        {
            Debug.LogError("MouseController instance does not exist");
        }
	}
	
	// Update is called once per frame
	void Update () {
        Tile t = mouseController.GetMouseOverTile();
        string s = "NULL";
        if (t.furniture != null)
        {
            s = t.furniture.objectType;
        }
        myText.text = "Furniture Type: " + s;
	}
}
