using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogBoxController : MonoBehaviour
{

    virtual public void ShowDialog()
    {
        WorldController.Instance.IsModal = true;
        gameObject.SetActive(true);
    }

    virtual public void CloseDialog()
    {
        WorldController.Instance.IsModal = false;
        gameObject.SetActive(false);
    }
}
