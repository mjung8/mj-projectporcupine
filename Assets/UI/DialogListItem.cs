using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DialogListItem : MonoBehaviour, IPointerClickHandler
{
    public InputField inputField;

    #region IPointerClickHandler
    public void OnPointerClick(PointerEventData eventData)
    {
        // Take the text label and copy it into a target field.
        inputField.text = transform.GetComponentInChildren<Text>().text;

    }
    #endregion
}
