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
            furn.ClearJobs();
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
        if(furn.tile.inventory != null && furn.tile.inventory.stackSize == 0)
        {
            Debug.LogError("Stockpile has a zero-size stack. This is wrong!");
            furn.ClearJobs();
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
            itemsDesired = Stockpile_GetItemsFromFilter();
        }
        else
        {
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
        j.tile.furniture.RemoveJob(j);

        // TODO: change this when we figure out what to do for all/any pickup job
        foreach (Inventory inv in j.inventoryRequirements.Values)
        {
            if (inv.stackSize > 0)
            {
                j.tile.world.inventoryManager.PlaceInventory(j.tile, inv);
                return;  // There should be no way we ever end up with more than one inventory req with stackSize > 0
            }
        }

    }

}
