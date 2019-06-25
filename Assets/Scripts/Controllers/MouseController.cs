using UnityEngine;
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

    // Use this for initialization
    void Start()
    {
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
        return WorldController.Instance.world.GetTileAt((int)currentFramePosition.x, (int)currentFramePosition.y);
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
                    Tile t = WorldController.Instance.world.GetTileAt(x, y);
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
            BuildModeController bmc = GameObject.FindObjectOfType<BuildModeController>();
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

}
