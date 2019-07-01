using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class BuildModeController : MonoBehaviour
{
    bool buildModeIsObjects = false;
    TileType buildModeTile = TileType.Floor;
    string buildModeObjectType;

    // Use this for initialization
    void Start()
    {
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

    public void DoPathfindingTest()
    {
        WorldController.Instance.world.SetupPathfindingExample();
    }

    public void DoBuild(Tile t)
    {
        if (buildModeIsObjects == true)
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
                    j = new Job(t, furnitureType, FurnitureActions.JobComplete_FurnitureBuilding, 0.1f, null);
                }

                j.furniturePrototype = WorldController.Instance.world.furniturePrototypes[furnitureType];

                // not good to explicitly set stuff like this
                t.pendingFunitureJob = j;
                j.RegisterJobCompleteCallback((theJob) => theJob.tile.pendingFunitureJob = null);

                // Add the job to the queue
                WorldController.Instance.world.jobQueue.Enqueue(j);
            }

        }
        else
        {
            // We are in tile-changing mode
            t.Type = buildModeTile;
        }
    }

}
