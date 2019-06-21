using System.Collections;
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
            return Mathf.Lerp(currTile.X, nextTile.X, movementPercentage);
        }
    }

    public float Y
    {
        get
        {
            return Mathf.Lerp(currTile.Y, nextTile.Y, movementPercentage);
        }
    }

    public Tile currTile { get; protected set; }

    Tile destTile;  // If we aren't moving then destTile = currTile
    Tile nextTile;  // The next tile in the pathfinding sequence
    Path_AStar pathAStar;

    float movementPercentage;   // 0-1 as we move from currTile to destTile

    float speed = 5f;   // Tiles per second

    Action<Character> cbCharacterChanged;

    Job myJob;

    public Character()
    {
        // Use only for serialization
    }

    public Character(Tile tile)
    {
        currTile = destTile = nextTile = tile;
    }

    void Update_DoJob(float deltaTime)
    {
        // Do I have a job?
        if (myJob == null)
        {
            // Grab a new job
            myJob = currTile.world.jobQueue.Dequeue();

            if (myJob != null)
            {
                // We have job

                // TODO: Check to see if job is reachable

                destTile = myJob.tile;
                myJob.RegisterJobCompleteCallback(OnJobEnded);
                myJob.RegisterJobCancelCallback(OnJobEnded);
            }
        }

        // Are we there
        if (currTile == destTile)
        //if(pathAStar != null && pathAStar.Length() == 1) // We are adjacent to the job site 
        {
            if (myJob != null)
            {
                myJob.DoWork(deltaTime);
            }
        }
    }

    public void AbandonJob()
    {
        nextTile = destTile = currTile;
        pathAStar = null;
        currTile.world.jobQueue.Enqueue(myJob);
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
                pathAStar = new Path_AStar(currTile.world, currTile, destTile);  // This will calculate path from curr to dest
                if (pathAStar.Length() == 0)
                {
                    Debug.LogError("Path_AStar returned no path to destination!");
                    // FIXME: maybe job should be requeued
                    AbandonJob();
                    pathAStar = null;
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
        else if(nextTile.IsEnterable() == ENTERABILITY.Soon)
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

    void OnJobEnded(Job j)
    {
        //Job completed or cancelled
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
