﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ProjectPorcupine.Localization;

public class FurnitureBuildMenu : MonoBehaviour
{

    public GameObject buildFurnitureButtonPrefab;
    string lastLanguage;

    // Use this for initialization
    void Start()
    {
        BuildModeController bmc = GameObject.FindObjectOfType<BuildModeController>();

        // For each furniture prototype in our world, create one instance
        // of the button to be clicked

        foreach (string s in World.Current.furniturePrototypes.Keys)
        {
            GameObject go = (GameObject)Instantiate(buildFurnitureButtonPrefab);
            go.transform.SetParent(this.transform);

            string objectId = s;
            string objectName = World.Current.furniturePrototypes[s].Name;

            go.name = "Button - Build " + objectId;
            go.transform.GetComponentInChildren<TextLocalizer>().formatValues =
                new string[] { LocalizationTable.GetLocalization(World.Current.furniturePrototypes[s].LocalizationCode) };

            Button b = go.GetComponent<Button>();
            b.onClick.AddListener(delegate { bmc.SetMode_BuildFurniture(objectId); });
        }

        lastLanguage = LocalizationTable.currentLanguage;
    }

    void Update()
    {
        if (lastLanguage != LocalizationTable.currentLanguage)
        {
            lastLanguage = LocalizationTable.currentLanguage;

            TextLocalizer[] localizers = GetComponentsInChildren<TextLocalizer>();

            for (int i = 0; i < localizers.Length; i++)
            {
                localizers[i].UpdateText(LocalizationTable.GetLocalization(World.Current.furniturePrototypes.ElementAt(i).Value.GetName()));
            }
        }

    }
}
