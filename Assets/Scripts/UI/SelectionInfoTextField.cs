﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionInfoTextField : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    MouseController mc;
    Text txt;

    // Use this for initialization
    void Start()
    {
        mc = FindObjectOfType<MouseController>();
        txt = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        if (mc.mySelection == null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return;
        }

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        txt.text = mc.mySelection.ToString();
    }
}