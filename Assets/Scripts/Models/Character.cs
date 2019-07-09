﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Character : IXmlSerializable
{
    // How to track the position?
    public float X
    {
        get
        {
            if (nextTile == null)
                return currTile.X;

            return Mathf.Lerp(currTile.X, nextTile.X, movementPercentage);
        }
    }

    public float Y
    {
        get
        {
            if (nextTile == null)
                return currTile.Y;

            return Mathf.Lerp(currTile.Y, nextTile.Y, movementPercentage);
        }
    }

    public Tile currTile { get; protected set; }

    // If we aren't moving then destTile = currTile
    Tile _destTile;
    Tile destTile
    {
        get { return _destTile; }
        set
        {
            if (_destTile != value)
            {
                _destTile = value;
                pathAStar = null;  // If this is a new destination then we need to invalidate pathfinding
            }
        }
    }

    Tile nextTile;  // The next tile in the pathfinding sequence
    Path_AStar pathAStar;

    float movementPercentage;   // 0-1 as we move from currTile to destTile

    float speed = 5f;   // Tiles per second

    Action<Character> cbCharacterChanged;

    Job myJob;

    public Inventory inventory;

    public Character()
    {
        // Use only for serialization
    }

    public Character(Tile tile)
    {
        currTile = destTile = nextTile = tile;
    }

    void GetNewJob()
    {
        myJob = World.Current.jobQueue.Dequeue();

        if (myJob == null)
            return;

        destTile = myJob.tile;
        myJob.RegisterJobStoppedCallback(OnJobStopped);

        // Immediately check to see if the job tile is reachable
        pathAStar = new Path_AStar(World.Current, currTile, destTile);  // This will calculate path from curr to dest
        if (pathAStar.Length() == 0)
        {
            Debug.LogError("Path_AStar returned no path to destination!");
            AbandonJob();
            destTile = currTile;
        }
    }

    void Update_DoJob(float deltaTime)
    {
        // Do I have a job?
        if (myJob == null)
        {
            GetNewJob();

            if (myJob == null)
            {
                // There was no job in the queue so just return
                destTile = currTile;
                return;
            }
        }

        // We have job. Do job?
        if (myJob.HasAllMaterial() == false)
        {
            // No we are missing something
            // Am I carrying anything the job location wants?
            if (inventory != null)
            {
                if (myJob.DesiresInventoryType(inventory) > 0)
                {
                    // If so, deliver
                    // Walk to the job tile then drop off the stack into the job
                    if (currTile == myJob.tile)
                    {
                        // We are at the job site so drop the inventory
                        World.Current.inventoryManager.PlaceInventory(myJob, inventory);
                        myJob.DoWork(0);  // This will call all cbJobWorked callbacks

                        // Are we still carrying things?
                        if (inventory.stackSize == 0)
                        {
                            inventory = null;
                        }
                        else
                        {
                            Debug.LogError("Character is still carrying inventory but shouldn't be. Set to NULL for now");
                            inventory = null;
                        }
                    }
                    else
                    {
                        destTile = myJob.tile;
                        return;
                    }
                }
                else
                {
                    // We are carrying something but the job doesn't want it
                    // Dump it
                    // TODO: Walk to nearest empty tile and dump it
                    if (World.Current.inventoryManager.PlaceInventory(currTile, inventory) == false)
                    {
                        Debug.LogError("Character tried to dump inventory into an invalid tile");
                        // FIXME: Dump any reference to current inventory
                        inventory = null;
                    }
                }
            }
            else
            {
                // Are we standing on a tile with goods desired by job?
                if (currTile.inventory != null &&
                    (myJob.canTakeFromStockpile || currTile.furniture == null || currTile.furniture.IsStockpile() == false) &&
                    myJob.DesiresInventoryType(currTile.inventory) > 0)
                {
                    // Pick up the stuff
                    World.Current.inventoryManager.PlaceInventory(
                        this,
                        currTile.inventory,
                        myJob.DesiresInventoryType(currTile.inventory)
                    );
                }
                else
                {
                    // If not, walk towards a tile containing the required goods

                    // Find the first thing in the job that isn't satisfied
                    Inventory desired = myJob.GetFirstDesiredInventory();
                    Inventory supplier = World.Current.inventoryManager.GetClosestInventoryOfType(
                        desired.objectType,
                        currTile,
                        desired.maxStackSize - desired.stackSize,
                        myJob.canTakeFromStockpile
                    );

                    if (supplier == null)
                    {
                        Debug.Log("No tile contains objects of type " + desired.objectType + "to satisfy job requirements");
                        AbandonJob();
                        return;
                    }

                    destTile = supplier.tile;
                    return;
                }
            }

            return; // Can't continue until all materials are satisifed
        }

        // Materials acquired. Go to job and do job.
        destTile = myJob.tile;

        if (currTile == myJob.tile)
        {
            // We are at the correct tile for our job so execute
            // the job's DoWork which is mostly going to couintdown
            // jobTime and potentially call its JobCompleteCallback
            myJob.DoWork(deltaTime);
        }

        // DoMovement gets called in the master Update
    }

    public void AbandonJob()
    {
        nextTile = destTile = currTile;
        World.Current.jobQueue.Enqueue(myJob);
        myJob = null;
    }

    void Update_DoMovement(float deltaTime)
    {
        if (currTile == destTile)
        {
            pathAStar = null;
            return; // Already there
        }

        // currTile = the tile I'm currently in (and may be leaving)
        // nextTile = the tile I'm currently entering
        // destTile = Our final destination -- we never walk here directly, but instead use it for the pathfinder

        if (nextTile == null || nextTile == currTile)
        {
            // Get the next tile from the pathfinder
            if (pathAStar == null || pathAStar.Length() == 0)
            {
                // Generate a path to our destination
                pathAStar = new Path_AStar(World.Current, currTile, destTile);  // This will calculate path from curr to dest
                if (pathAStar.Length() == 0)
                {
                    Debug.LogError("Path_AStar returned no path to destination!");
                    // FIXME: maybe job should be requeued
                    AbandonJob();
                    return;
                }

                // Let's ignore the first tile because that's the tile we're currently in
                nextTile = pathAStar.Dequeue();
            }

            // Grab the next waypoint from the pathing system!
            nextTile = pathAStar.Dequeue();

            if (nextTile == currTile)
            {
                Debug.LogError("Update_DoMovement - nextTile is currTile?");
            }
        }

        // At this point we should have a valid nextTile to move to

        // Total distance between A and B
        // Euclidean distance for now; for pathfinding change to Manhattan or something else
        float distToTravel = Mathf.Sqrt(
            Mathf.Pow(currTile.X - nextTile.X, 2) +
            Mathf.Pow(currTile.Y - nextTile.Y, 2)
        );

        if (nextTile.IsEnterable() == ENTERABILITY.Never)
        {
            // Most likely a wall got built, so we need to reset pathfinding
            // FIXME: when a wall gets spanwed, invalidate path immediately. or check sometimes to save CPU.
            // or register a callback to ontilechanged event
            Debug.LogError("Fix me - character trying to walk through unwalkable  tile");
            nextTile = null;    // our next tile is a no-go
            pathAStar = null;   // pathfinding info is out of date
            return;
        }
        else if (nextTile.IsEnterable() == ENTERABILITY.Soon)
        {
            // Can't enter now but should be able to in the future.(Door?)
            // Don't bail on movement path but return now and don't process movement.
            return;
        }

        // How much distance can be travelled this Update
        float distThisFrame = speed / nextTile.movementCost * deltaTime;

        // How much is that in terms of percentage
        float percThisFrame = distThisFrame / distToTravel;

        // Add that to overall percentage travelled
        movementPercentage += percThisFrame;

        if (movementPercentage >= 1)
        {
            // Reached destination

            // TODO: get the next tile from pathfinding system

            currTile = nextTile;
            movementPercentage = 0;

            // FIXME: Overshot movement?
        }
    }

    public void Update(float deltaTime)
    {
        //Debug.Log("Character Update");

        Update_DoJob(deltaTime);

        Update_DoMovement(deltaTime);

        if (cbCharacterChanged != null)
            cbCharacterChanged(this);

    }

    public void SetDestination(Tile tile)
    {
        if (currTile.IsNeighbour(tile, true) == false)
        {
            Debug.Log("Character::SetDestination -- destination tile is not a neighbour.");
        }

        destTile = tile;
    }

    public void RegisterOnChangedCallback(Action<Character> cb)
    {
        cbCharacterChanged += cb;
    }

    public void UnregisterOnChangedCallback(Action<Character> cb)
    {
        cbCharacterChanged -= cb;
    }

    void OnJobStopped(Job j)
    {
        //Job completed (if non-repeating) or cancelled

        j.UnregisterJobStoppedCallback(OnJobStopped);

        if (j != myJob)
        {
            Debug.LogError("Character being told about job that isn't his. You forgot to unregister something");
            return;
        }

        myJob = null;
    }

    public XmlSchema GetSchema()
    {
        throw new NotImplementedException();
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", currTile.X.ToString());
        writer.WriteAttributeString("Y", currTile.Y.ToString());
    }

    public void ReadXml(XmlReader reader)
    {

    }
}
