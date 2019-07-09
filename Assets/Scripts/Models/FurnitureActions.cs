using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FurnitureActions
{

    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {
        //Debug.Log("Door_UpdateAction");
        if (furn.GetParameter("is_opening") >= 1)
        {
            furn.ChangeParameter("openness", deltaTime * 4);
            if (furn.GetParameter("openness") >= 1)
            {
                furn.SetParameter("is_opening", 0);
            }
        }
        else
        {
            furn.ChangeParameter("openness", deltaTime * -4);
        }

        furn.SetParameter("openness", Mathf.Clamp01(furn.GetParameter("openness")));

        if (furn.cbOnChanged != null)
            furn.cbOnChanged(furn);
    }

    public static ENTERABILITY Door_IsEnterable(Furniture furn)
    {
        furn.SetParameter("is_opening", 1);

        if (furn.GetParameter("openness") >= 1)
        {
            return ENTERABILITY.Yes;
        }

        return ENTERABILITY.Soon;
    }

    public static void JobComplete_FurnitureBuilding(Job theJob)
    {
        WorldController.Instance.world.PlaceFurniture(theJob.jobObjectType, theJob.tile);
        theJob.tile.pendingFunitureJob = null;
    }

    public static Inventory[] Stockpile_GetItemsFromFilter()
    {
        // TODO: this should be reading in from some kind of UI

        // Since jobs copy arrays automatically, we could already have
        // an Inventory[] prepared and just return that (as sort of example filter)
        return new Inventory[1] { new Inventory("Steel Plate", 50, 0) };
    }

    public static void Stockpile_UpdateAction(Furniture furn, float deltaTime)
    {
        // Ensure that we have a job on the queue asking for either:
        // (if we are empty): that any loose inventory be brought to us
        // (if we have something): then if we are still below the max stack size,
        //                         that more of the same should be brought to us

        // TODO: this function doesn't need to run each update. Once we get a lot of
        // furniture in a running game, this will run a LOT more than required
        // Instead, it only read needs to run whenever: 
        //                                    -- it gets created
        //                                    -- a good gets delivered (reset job)
        //                                    -- a good gets picked up (reset job)
        //                                    -- the UI's filter of allowed items gets changed

        if (furn.tile.inventory != null && furn.tile.inventory.stackSize >= furn.tile.inventory.maxStackSize)
        {
            // We are full
            furn.CancelJobs();
            return;
        }

        // Maybe we already have a job queued up?
        if (furn.JobCount() > 0)
        {
            // All done
            return;
        }

        // We are currently not full but don't have a job either
        // Two possibilities: either we have some inventory or we have no inventory

        // Third possibility: Something is whack
        if (furn.tile.inventory != null && furn.tile.inventory.stackSize == 0)
        {
            Debug.LogError("Stockpile has a zero-size stack. This is wrong!");
            furn.CancelJobs();
            return;
        }

        // TODO: in the future, stockpiles, rather than being a bunch of individual
        // 1x1 tiles, should manifest themselves as a single, large object
        // this would represent our first and probably only variable sized furniture
        // What happens if there's a hole in the stockpile (if an actual furniture is
        // installed in the middle?)
        // In any case, once we implement 'mega stockpiles', then the job-creation system
        // can be smarter in that even if the stockpile has stuff in it, it can
        // also still be requesting different object types in its job creation

        Inventory[] itemsDesired;

        if (furn.tile.inventory == null)
        {
            Debug.Log("Creating job for new stack.");
            itemsDesired = Stockpile_GetItemsFromFilter();
        }
        else
        {
            Debug.Log("Creating job for existing stack.");
            Inventory desInv = furn.tile.inventory.Clone();
            desInv.maxStackSize -= desInv.stackSize;
            desInv.stackSize = 0;

            itemsDesired = new Inventory[] { desInv };
        }

        Job j = new Job(
                furn.tile,
                null, // ""
                null,
                0,
                itemsDesired
            );

        // TODO: add stockpile priorities so we can take from lower to higher
        j.canTakeFromStockpile = false;

        j.RegisterJobWorkedCallback(Stockpile_JobWorked);
        furn.AddJob(j);
    }

    static void Stockpile_JobWorked(Job j)
    {
        Debug.Log("Stockpile_JobWorked");
        j.CancelJob();

        // TODO: change this when we figure out what to do for all/any pickup job
        foreach (Inventory inv in j.inventoryRequirements.Values)
        {
            if (inv.stackSize > 0)
            {
                World.Current.inventoryManager.PlaceInventory(j.tile, inv);
                return;  // There should be no way we ever end up with more than one inventory req with stackSize > 0
            }
        }

    }

    public static void OxygenGenerator_UpdateAction(Furniture furn, float deltaTime)
    {
        if (furn.tile.room == null)
        {
            Debug.LogError("Why are we in a null room?");
        }

        if (furn.tile.room.GetGasAmount("O2") < 0.20f)
        {
            // TODO: Change the gas contribution based on volume of room
            furn.tile.room.ChangeGas("O2", 0.01f * deltaTime);   // TODO: Replace hardcoded value
            // TODO: consume electricity while running
        }
        else
        {
            // TODO: standby electric usage?
        }
    }

    public static void MiningDroneStation_UpdateAction(Furniture furn, float deltaTime)
    {
        Tile spawnSpot = furn.GetSpawnSpotTile();

        if (furn.JobCount() > 0)
        {
            // Check to see if the Metal Plate destination tile is full
            if (spawnSpot.inventory != null && spawnSpot.inventory.stackSize >= spawnSpot.inventory.maxStackSize)
            {
                // We should stop this job because it's impossible to make any more items
                furn.CancelJobs();
            }

            return;
        }

        // If we get here we have no current job. Check to see if our destination is full
        if (spawnSpot.inventory != null && spawnSpot.inventory.stackSize >= spawnSpot.inventory.maxStackSize)
        {
            // We are full, don't make a job
            return;
        }

        // If we get here we need to create a new job
        Tile jobSpot = furn.GetJobSpotTile();

        if (jobSpot.inventory != null && (jobSpot.inventory.stackSize >= jobSpot.inventory.maxStackSize))
        {
            // Our drop spot is already full, so don't create a job.
            return;
        }

        Job j = new Job(
            jobSpot,
            null,
            MiningDroneStation_JobComplete,
            1f,
            null,
            true    // This job repeats until the destination tile is full
        );

        furn.AddJob(j);
    }

    public static void MiningDroneStation_JobComplete(Job j)
    {
        World.Current.inventoryManager.PlaceInventory(j.furniture.GetSpawnSpotTile(), new Inventory("Steel Plate", 50, 20));
    }

}
