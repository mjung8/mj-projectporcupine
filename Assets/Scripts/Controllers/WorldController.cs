﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;

public class WorldController : MonoBehaviour
{
    public static WorldController Instance { get; protected set; }

    // World and tile data
    public World world { get; protected set; }

    static string loadWorldFromFile = null;

    private bool _isPaused = false;
    public bool IsPaused
    {
        get
        {
            return _isPaused || IsModal;
        }
        set
        {
            _isPaused = value;
        }
    }
    // If true, a modal dialog box is open so normal inputs should be ignored.
    public bool IsModal;

    // Multiplier of Time.deltaTime.
    private float timeScale = 1f;

    // An array of possible time multipliers.
    private float[] possibleTimeScales = new float[6] { 0.1f, 0.5f, 1f, 2f, 4f, 8f };
    // Current position in that array.
    int currentTimeScalePosition = 2;

    // Use this for initialization
    // OnEnable runs first
    void OnEnable()
    {

        if (Instance != null)
        {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;

        if (loadWorldFromFile != null)
        {
            CreateWorldFromSaveFile();
            loadWorldFromFile = null;
        }
        else
        {
            CreateEmptyWorld();
        }
    }

    void Update()
    {
        CheckTimeInput();

        if (IsPaused == false)
        {
            world.Update(Time.deltaTime  * timeScale);
        }

    }

    void CheckTimeInput()
    {
        // TODO: Move this into centralized keyboard manager where
        // all of the buttons can be rebinded.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            IsPaused = !IsPaused;
            Debug.Log("Game " + (IsPaused ? "paused" : "resumed"));
        }
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            if (currentTimeScalePosition == possibleTimeScales.Length - 1)
            {
                // We are on the top of possibleTimeScales so just bail out.
                return;
            }

            currentTimeScalePosition++;
            SetTimeScale(possibleTimeScales[currentTimeScalePosition]);
        }
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            if (currentTimeScalePosition == 0)
            {
                // We are on the bottom of possibleTimeScales so just bail out.
                return;
            }

            currentTimeScalePosition--;
            SetTimeScale(possibleTimeScales[currentTimeScalePosition]);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SetTimeScale(1f);
            currentTimeScalePosition = 2;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SetTimeScale(2f);
            currentTimeScalePosition = 3;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            SetTimeScale(4f);
            currentTimeScalePosition = 4;
        }
    }

    /// <summary>
    /// Set's game speed (it's a multiplier so 1 == normal game speed).
    /// </summary>
    /// <param name="timeScale">Desired time scale</param>
    public void SetTimeScale(float timeScale)
    {
        this.timeScale = timeScale;
        Debug.Log("Game speed set to " + timeScale + "x");
    }

    /// <summary>
    /// Gets the tile at the unity-space coordinates.
    /// </summary>
    /// <param name="coord">Unity world-space coordinates.</param>
    /// <returns>The tile at the world coordinate.</returns>
    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x + 0.5f);
        int y = Mathf.FloorToInt(coord.y + 0.5f);

        return world.GetTileAt(x, y);
    }

    public void NewWorld()
    {
        Debug.Log("NewWorld button clicked");

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public string FileSaveBasePath()
    {
        return Path.Combine(Application.persistentDataPath, "Saves");
    }

    public void LoadWorld(string fileName)
    {
        Debug.Log("LoadWorld button clicked");

        // Reload the scene to reset all data (and purge all references)
        loadWorldFromFile = fileName;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void CreateEmptyWorld()
    {
        // Create a world with Empty tiles
        world = new World(100, 100);

        // Center the camera
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }

    void CreateWorldFromSaveFile()
    {
        Debug.Log("CreateWorldFromSaveFile");

        // Create a world from save file data
        XmlSerializer serializer = new XmlSerializer(typeof(World));

        // This can throw an ex 
        // TODO: show an error message
        string saveGameText = File.ReadAllText(loadWorldFromFile);

        TextReader reader = new StringReader(saveGameText);

        Debug.Log(reader.ToString());
        world = (World)serializer.Deserialize(reader);
        reader.Close();

        // Center the camera
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }
}
