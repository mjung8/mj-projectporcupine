using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MouseController : MonoBehaviour
{
    public GameObject circleCursorPrefab;

    bool buildModeIsObjects = false;
    TileType buildModeTile = TileType.Floor;
    string buildModeObjectType;

    // The world-position of the mouse last frame.
    Vector3 lastFramePosition;
    Vector3 currentFramePosition;

    // The world-position start of the left-mouse drag operation.
    Vector3 dragStartPosition;
    List<GameObject> dragPreviewGameObjects;

    // Use this for initialization
    void Start()
    {
        dragPreviewGameObjects = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        currentFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currentFramePosition.z = 0;

        //UpdateCursor();
        UpdateDragging();
        UpdateCameraMovement();

        // Save the mouse position from this frame
        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
    }

    //void UpdateCursor()
    //{
    //    // Update the circle cursor position
    //    Tile tileUnderMouse = WorldController.Instance.GetTileAtWorldCoord(currentFramePosition);
    //    if (tileUnderMouse != null)
    //    {
    //        circleCursor.SetActive(true);
    //        Vector3 cursorPosition = new Vector3(tileUnderMouse.X, tileUnderMouse.Y, 0);
    //        circleCursor.transform.position = cursorPosition;
    //    }
    //    else
    //    {
    //        // Mouse is outside of the world space, so hide the cursor.
    //        circleCursor.SetActive(false);
    //    }
    //}

    void UpdateDragging()
    {
        // If we are over a UI element, bail out
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Start Drag
        if (Input.GetMouseButtonDown(0))
        {
            dragStartPosition = currentFramePosition;
        }

        int start_x = Mathf.FloorToInt(dragStartPosition.x);
        int end_x = Mathf.FloorToInt(currentFramePosition.x);
        int start_y = Mathf.FloorToInt(dragStartPosition.y);
        int end_y = Mathf.FloorToInt(currentFramePosition.y);

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

        // Clean up old drag previews
        while (dragPreviewGameObjects.Count > 0)
        {
            GameObject go = dragPreviewGameObjects[0];
            dragPreviewGameObjects.RemoveAt(0);
            SimplePool.Despawn(go);
        }

        if (Input.GetMouseButton(0))
        {
            // Display a preview of the drag area
            // Loop through all tiles.
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    Tile t = WorldController.Instance.World.GetTileAt(x, y);
                    if (t != null)
                    {
                        // Display the building hint on top of this tile position
                        GameObject go = SimplePool.Spawn(circleCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                        go.transform.SetParent(this.transform, true);
                        dragPreviewGameObjects.Add(go);
                    }
                }
            }
        }

        // End Drag
        if (Input.GetMouseButtonUp(0))
        {
            // Loop through all tiles.
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    Tile t = WorldController.Instance.World.GetTileAt(x, y);

                    if (t != null)
                    {
                        if (buildModeIsObjects == true)
                        {
                            // Create the furniture and assign it to the tile

                            // FIXME: Right now, we're just going to assume walls
                            WorldController.Instance.World.PlaceFurniture(buildModeObjectType, t);

                        } else
                        {
                            // We are in tile-changing mode
                            t.Type = buildModeTile;
                        }
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

    public void SetMode_BuildFloor()
    {
        buildModeIsObjects = false;
        buildModeTile = TileType.Floor;
    }

    public void SetMode_Bulldoze()
    {
        buildModeIsObjects = false;
        buildModeTile = TileType.Empty;
    }

    public void SetMode_BuildFurniture(string objectType)
    {
        // Wall is not a Tile. Wall is an furniture that exists on top of a tile.
        buildModeIsObjects = true;
        buildModeObjectType = objectType;
    }

}
