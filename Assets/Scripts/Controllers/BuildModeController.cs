using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public enum BuildMode
{
    FLOOR,
    FURNITURE,
    DECONSTRUCT
}

public class BuildModeController : MonoBehaviour
{
    public BuildMode buildMode = BuildMode.FLOOR;
    TileType buildModeTile = TileType.Floor;
    public string buildModeObjectType;

    // Use this for initialization
    void Start()
    {

    }

    public bool IsObjectDraggable()
    {
        if (buildMode == BuildMode.FLOOR || buildMode == BuildMode.DECONSTRUCT)
        {
            // floors are draggable
            return true;
        }

        Furniture proto = WorldController.Instance.world.furniturePrototypes[buildModeObjectType];

        return proto.Width == 1 && proto.Height == 1;
    }

    public void SetMode_BuildFloor()
    {
        buildMode = BuildMode.FLOOR;
        buildModeTile = TileType.Floor;
        GameObject.FindObjectOfType<MouseController>().StartBuildMode();
    }

    public void SetMode_Bulldoze()
    {
        buildMode = BuildMode.FLOOR;
        buildModeTile = TileType.Empty;
        GameObject.FindObjectOfType<MouseController>().StartBuildMode();
    }

    public void SetMode_BuildFurniture(string objectType)
    {
        // Wall is not a Tile. Wall is an "Furniture" that exists on TOP of a tile.
        buildMode = BuildMode.FURNITURE;
        buildModeObjectType = objectType;
        GameObject.FindObjectOfType<MouseController>().StartBuildMode();
    }

    public void SetMode_Deconstruct()
    {
        buildMode = BuildMode.DECONSTRUCT;
        GameObject.FindObjectOfType<MouseController>().StartBuildMode();
    }

    public void DoPathfindingTest()
    {
        WorldController.Instance.world.SetupPathfindingExample();
    }

    public void DoBuild(Tile t)
    {
        if (buildMode == BuildMode.FURNITURE)
        {
            // Create the furniture and assign it to the tile

            // FIXME: This instantly builds furniture
            //WorldController.Instance.World.PlaceFurniture(buildModeObjectType, t);

            // Can we build the furniture in the selected tile?
            // Run the ValidPlacement function
            string furnitureType = buildModeObjectType;

            if (WorldController.Instance.world.IsFurniturePlacementValid(furnitureType, t)
                && t.pendingFunitureJob == null)
            {
                // This tile position is valid for this furniture

                // If there is existing furniture, delete it
                // TODO: Return resources? Will the Deconstruct() method handle that? 
                // If so what will happen if resources drop ontop of new non-passable structure.
                if (t.furniture != null)
                {
                    t.furniture.Deconstruct();
                }


                // Create a job for it to be built
                Job j;

                if (WorldController.Instance.world.furnitureJobPrototypes.ContainsKey(furnitureType))
                {
                    // Make a clone of the job prototype
                    j = WorldController.Instance.world.furnitureJobPrototypes[furnitureType].Clone();
                    // assign the correct tile
                    j.tile = t;
                }
                else
                {
                    Debug.LogError("There is no furnitureJobPrototype for: " + furnitureType);
                    j = new Job(t, furnitureType, FurnitureActions.JobComplete_FurnitureBuilding, 0.1f, null, false);
                }

                j.furniturePrototype = WorldController.Instance.world.furniturePrototypes[furnitureType];

                // FIXME: not good to manually and explicitly set
                // flags that preven conflicts. It's too easy to forget to set/clear them!
                t.pendingFunitureJob = j;
                j.RegisterJobCompletedCallback((theJob) => theJob.tile.pendingFunitureJob = null);

                // Add the job to the queue
                WorldController.Instance.world.jobQueue.Enqueue(j);
            }

        }
        else if (buildMode == BuildMode.FLOOR)
        {
            // We are in tile-changing mode
            t.Type = buildModeTile;
        }
        else if (buildMode == BuildMode.DECONSTRUCT)
        {
            // TODO
            if (t.furniture != null)
            {
                t.furniture.Deconstruct();
            }
        }
        else
        {
            Debug.LogError("UNIMPLEMENTED BUILD MODE");
        }
    }

}
