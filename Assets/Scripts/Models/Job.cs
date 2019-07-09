using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Job
{

    // This class holds info for a queued job, which can include
    // things like placing furniture, moving stored inventory,
    // working at a desk, and maybe even fighting enemies.

    public Tile tile;
    public float jobTime { get; protected set; }

    protected float jobTimeRequired;

    protected bool jobRepeats = false;

    public string jobObjectType { get; protected set; }

    public Furniture furniturePrototype;

    public Furniture furniture; // The piece of furniture that owns this job. Frequently will be null.

    public bool acceptsAnyInventoryItem = false;

    Action<Job> cbJobCompleted; // We have finished the work cycle so things should probably get built or whatever
    Action<Job> cbJobStopped;   // The job has been stopped, because it's non-repeating or was cancelled
    Action<Job> cbJobWorked;    // Gets called each time some work is performed -- maybe update the UI?

    public bool canTakeFromStockpile = true;

    public Dictionary<string, Inventory> inventoryRequirements;

    public Job(Tile tile, string jobObjectType, Action<Job> cbJobComplete, float jobTime, Inventory[] inventoryRequirements, bool jobRepeats = false)
    {
        this.tile = tile;
        this.jobObjectType = jobObjectType;
        this.cbJobCompleted += cbJobComplete;
        this.jobTimeRequired = this.jobTime = jobTime;
        this.jobRepeats = jobRepeats;

        this.inventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements != null)
        {
            foreach (Inventory inv in inventoryRequirements)
            {
                this.inventoryRequirements[inv.objectType] = inv.Clone();
            }
        }
    }

    protected Job(Job other)
    {
        this.tile = other.tile;
        this.jobObjectType = other.jobObjectType;
        this.cbJobCompleted += other.cbJobCompleted;
        this.jobTime = other.jobTime;

        this.inventoryRequirements = new Dictionary<string, Inventory>();
        if (inventoryRequirements != null)
        {
            foreach (Inventory inv in other.inventoryRequirements.Values)
            {
                this.inventoryRequirements[inv.objectType] = inv.Clone();
            }
        }
    }

    public virtual Job Clone()
    {
        return new Job(this);
    }

    public void RegisterJobCompletedCallback(Action<Job> cb)
    {
        cbJobCompleted += cb;
    }

    public void UnregisterJobCompletedCallback(Action<Job> cb)
    {
        cbJobCompleted -= cb;
    }

    public void RegisterJobStoppedCallback(Action<Job> cb)
    {
        cbJobStopped += cb;
    }

    public void UnregisterJobStoppedCallback(Action<Job> cb)
    {
        cbJobStopped -= cb;
    }

    public void RegisterJobWorkedCallback(Action<Job> cb)
    {
        cbJobWorked += cb;
    }

    public void UnregisterJobWorkedCallback(Action<Job> cb)
    {
        cbJobWorked -= cb;
    }

    public void DoWork(float workTime)
    {
        // Check to make sure we actually have everything we need
        // If not, don't register the work time
        if (HasAllMaterial() == false)
        {
            //Debug.LogError("Tried to do work on a job that doesn't have all material.");

            // job can't actually be worked but still call the callbacks
            // so that animations and what not can be updated
            if (cbJobWorked != null)
                cbJobWorked(this);

            return;
        }

        jobTime -= workTime;

        if (cbJobWorked != null)
            cbJobWorked(this);

        if (jobTime <= 0)
        {
            // Do whateve ris supposesd to happen when a job cycle compeltes
            if (cbJobCompleted != null)
                cbJobCompleted(this);

            if (jobRepeats == false)
            {
                // Let everyone know that the job is officially concluded
                if (cbJobStopped != null)
                    cbJobStopped(this);
            }
            else
            {
                // This is a repeating job and must be reset
                jobTime += jobTimeRequired;
            }
        }
    }

    public void CancelJob()
    {
        if (cbJobStopped != null)
        {
            cbJobStopped(this);
        }

        World.Current.jobQueue.Remove(this);
    }

    public bool HasAllMaterial()
    {
        foreach (Inventory inv in inventoryRequirements.Values)
        {
            if (inv.maxStackSize > inv.stackSize)
            {
                return false;
            }
        }

        return true;
    }

    public int DesiresInventoryType(Inventory inv)
    {
        if (acceptsAnyInventoryItem)
        {
            return inv.maxStackSize;
        }

        if (inventoryRequirements.ContainsKey(inv.objectType) == false)
            return 0;

        if (inventoryRequirements[inv.objectType].stackSize >= inventoryRequirements[inv.objectType].maxStackSize)
            return 0;

        // We need this
        return inventoryRequirements[inv.objectType].maxStackSize - inventoryRequirements[inv.objectType].stackSize;
    }

    public Inventory GetFirstDesiredInventory()
    {
        foreach (Inventory inv in inventoryRequirements.Values)
        {
            if (inv.maxStackSize > inv.stackSize)
                return inv;
        }

        return null;
    }

}
