using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DialogListItem : MonoBehaviour, IPointerClickHandler
{
    public string fileName;
    public InputField inputField;

    #region IPointerClickHandler
    public void OnPointerClick(PointerEventData eventData)
    {
        // Take the text label and copy it into a target field.
        inputField.text = fileName;
        GameObject go = GameObject.FindGameObjectWithTag("DeleteButton");
        if (go != null)
        {
            go.GetComponent<Image>().color = new Color(255, 255, 255, 255);
            Component text = transform.GetComponentInChildren<Text>();
            GetComponentInParent<DialogBoxLoadGame>().pressedDelete = true;
            GetComponentInParent<DialogBoxLoadGame>().SetFileItem(text);
        }
    }
    #endregion
}
