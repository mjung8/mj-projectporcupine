﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MouseController : MonoBehaviour
{
    public GameObject circleCursorPrefab;

    // The world-position of the mouse last frame.
    Vector3 lastFramePosition;
    Vector3 currentFramePosition;

    // The world-position start of the left-mouse drag operation.
    Vector3 dragStartPosition;
    List<GameObject> dragPreviewGameObjects;

    BuildModeController bmc;
    FurnitureSpriteController fsc;

    bool isDragging = false;

    enum MouseMode
    {
        SELECT,
        BUILD
    }
    MouseMode currentMode = MouseMode.SELECT;

    // Use this for initialization
    void Start()
    {
        bmc = GameObject.FindObjectOfType<BuildModeController>();
        fsc = GameObject.FindObjectOfType<FurnitureSpriteController>();

        dragPreviewGameObjects = new List<GameObject>();
    }

    /// <summary>
    /// Gets the mouse position in world space.
    /// </summary>
    public Vector3 GetMousePosition()
    {
        return currentFramePosition;
    }

    public Tile GetMouseOverTile()
    {
        return WorldController.Instance.GetTileAtWorldCoord(currentFramePosition);
    }

    // Update is called once per frame
    void Update()
    {
        if (WorldController.Instance.IsModal)
        {
            // A modal dialog is open so don't process any game inputs
            return;
        }

        currentFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currentFramePosition.z = 0;

        if (Input.GetKeyUp(KeyCode.Escape) || Input.GetMouseButtonUp(1))
        {
            if (currentMode == MouseMode.BUILD)
            {
                currentMode = MouseMode.SELECT;
            }
            else if (currentMode == MouseMode.SELECT)
            {
                Debug.Log("Show game menu?");
            }
        }

        //UpdateCursor();

        UpdateDragging();
        UpdateCameraMovement();
        UpdateSelection();

        // Save the mouse position from this frame
        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
    }

    public class SelectionInfo
    {
        public Tile tile;
        public ISelectable[] stuffInTile;
        public int subSelection = 0;
    }

    public SelectionInfo mySelection;

    void UpdateSelection()
    {
        // This handles left-clicking on furniture or characters to set a selection.

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            mySelection = null;
        }

        if (currentMode != MouseMode.SELECT)
        {
            return;
        }

        // If we are over a UI element, bail out
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Just released the mouse button, so update the selection.
            Tile tileUnderMouse = GetMouseOverTile();

            if (tileUnderMouse == null)
            {
                // No valid tile under mouse
                return;
            }

            if (mySelection == null || mySelection.tile != tileUnderMouse)
            {
                //Debug.Log("new tile");
                // We have just selected a brand new tile, reset the info
                mySelection = new SelectionInfo();
                mySelection.tile = tileUnderMouse;
                RebuildSelectionStuffInTile();

                // Select the first non-null entry
                for (int i = 0; i < mySelection.stuffInTile.Length; i++)
                {
                    if (mySelection.stuffInTile[i] != null)
                    {
                        mySelection.subSelection = i;
                        break;
                    }
                }
            }
            else
            {
                // This is the same time we already have selected so cycle the subSelection to the next non-null item
                // Note that the tile sub selection can never be null so we know we will always find something

                // Rebuild the arra of possible sub-selection in case characters moved in or out of the tile
                RebuildSelectionStuffInTile();

                do
                {
                    mySelection.subSelection = (mySelection.subSelection + 1) % mySelection.stuffInTile.Length;
                } while (mySelection.stuffInTile[mySelection.subSelection] == null);
            }

            //Debug.Log(mySelection.subSelection);
        }
    }

    void RebuildSelectionStuffInTile()
    {
        // Make sure stuffInTile is big enough to handle all the charactesr, plus the extra 3 values
        mySelection.stuffInTile = new ISelectable[mySelection.tile.characters.Count + 3];

        // Copy the character references
        for (int i = 0; i < mySelection.tile.characters.Count; i++)
        {
            mySelection.stuffInTile[i] = mySelection.tile.characters[i];
        }

        // Now assign references to the other three sub-selections available
        mySelection.stuffInTile[mySelection.stuffInTile.Length - 3] = mySelection.tile.furniture;
        mySelection.stuffInTile[mySelection.stuffInTile.Length - 2] = mySelection.tile.inventory;
        mySelection.stuffInTile[mySelection.stuffInTile.Length - 1] = mySelection.tile;
    }

    void UpdateDragging()
    {
        // If we are over a UI element, bail out
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Clean up old drag previews
        while (dragPreviewGameObjects.Count > 0)
        {
            GameObject go = dragPreviewGameObjects[0];
            dragPreviewGameObjects.RemoveAt(0);
            SimplePool.Despawn(go);
        }

        if (currentMode != MouseMode.BUILD)
        {
            return;
        }

        // Start Drag
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPosition = currentFramePosition;
            isDragging = true;
        }
        else if (isDragging == false)
        {
            dragStartPosition = currentFramePosition;
        }

        if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.Escape))
        {
            // The RMB was released so we are cancelling any drag/build mode.
            isDragging = false;
        }

        if (bmc.IsObjectDraggable() == false)
        {
            dragStartPosition = currentFramePosition;
        }

        int start_x = Mathf.FloorToInt(dragStartPosition.x + 0.5f);
        int end_x = Mathf.FloorToInt(currentFramePosition.x + 0.5f);
        int start_y = Mathf.FloorToInt(dragStartPosition.y + 0.5f);
        int end_y = Mathf.FloorToInt(currentFramePosition.y + 0.5f);

        // Flip direction.
        if (end_x < start_x)
        {
            int tmp = end_x;
            end_x = start_x;
            start_x = tmp;
        }
        if (end_y < start_y)
        {
            int tmp = end_y;
            end_y = start_y;
            start_y = tmp;
        }

        //if (isDragging)
        //{
        // Display a preview of the drag area
        // Loop through all tiles.
        for (int x = start_x; x <= end_x; x++)
        {
            for (int y = start_y; y <= end_y; y++)
            {
                Tile t = WorldController.Instance.world.GetTileAt(x, y);
                if (t != null)
                {
                    // Display the building hint on top of this tile position
                    if (bmc.buildMode == BuildMode.FURNITURE)
                    {
                        ShowFurnitureSpriteAtTile(bmc.buildModeObjectType, t);
                    }
                    else
                    {
                        // Show the generic dragging visuals
                        GameObject go = SimplePool.Spawn(circleCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                        go.transform.SetParent(this.transform, true);
                        go.GetComponent<SpriteRenderer>().sprite = SpriteManager.current.GetSprite("UI", "CursorCircle");
                        dragPreviewGameObjects.Add(go);
                    }
                }
            }
            //}
        }

        // End Drag
        if (isDragging & Input.GetMouseButtonUp(0))
        {
            isDragging = false;

            // Loop through all tiles.
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    Tile t = WorldController.Instance.world.GetTileAt(x, y);

                    if (t != null)
                    {
                        // Call BuildModeController::DoBuild
                        bmc.DoBuild(t);
                    }
                }
            }
        }
    }

    void UpdateCameraMovement()
    {
        // Handle screen dragging
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2)) // Right or middle mouse button
        {
            Vector3 diff = lastFramePosition - currentFramePosition;
            Camera.main.transform.Translate(diff);
        }

        Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis("Mouse ScrollWheel");

        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);
    }

    void ShowFurnitureSpriteAtTile(string furnitureType, Tile t)
    {
        GameObject go = new GameObject();
        go.transform.SetParent(this.transform, true);
        dragPreviewGameObjects.Add(go);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Jobs";
        sr.sprite = fsc.GetSpriteForFurniture(furnitureType);

        if (WorldController.Instance.world.IsFurniturePlacementValid(furnitureType, t))
        {
            sr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        }
        else
        {
            sr.color = new Color(1f, 0.5f, 0.5f, 0.25f);
        }

        Furniture proto = World.Current.furniturePrototypes[furnitureType];

        go.transform.position = new Vector3(t.X + ((proto.Width - 1) / 2f), t.Y + ((proto.Height - 1) / 2f), 0);
    }

    public void StartBuildMode()
    {
        currentMode = MouseMode.BUILD;
    }

}
