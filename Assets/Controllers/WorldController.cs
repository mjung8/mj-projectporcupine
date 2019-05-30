using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class WorldController : MonoBehaviour {

    public static WorldController Instance { get; protected set; }

    // World and tile data
	public World world { get; protected set; }

	// Use this for initialization
    // OnEnable runs first
	void OnEnable () {

        if (Instance != null)
        {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;

        // Create a world with Empty tiles
		world = new World();

        // Center the camera
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
	}

    void Update()
    {
        // TODO: Add pause/unpause, speed, etc...
        world.Update(Time.deltaTime);
    }

    /// <summary>
    /// Gets the tile at the unity-space coordinates.
    /// </summary>
    /// <param name="coord">Unity world-space coordinates.</param>
    /// <returns>The tile at the world coordinate.</returns>
    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x);
        int y = Mathf.FloorToInt(coord.y);

        return world.GetTileAt(x, y);
    }

}
