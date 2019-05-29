using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Character
{
    // How to track the position?
    public float X
    {
        get
        {
            return Mathf.Lerp(currTile.X, destTile.X, movementPercentage);
        }
    }

    public float Y
    {
        get
        {
            return Mathf.Lerp(currTile.Y, destTile.Y, movementPercentage);
        }
    }

    public Tile currTile { get; protected set; }
    Tile destTile;  // If we aren't moving then destTile = currTile
    float movementPercentage;   // 0-1 as we move from currTile to destTile

    float speed = 2f;   // Tiles per second

    Action<Character> cbCharacterChanged;

    Job myJob;

    public Character(Tile tile)
    {
        currTile = destTile = tile;
    }

    public void Update(float deltaTime)
    {
        //Debug.Log("Character Update");

        // Do I have a job?
        if (myJob == null)
        {
            // Grab a new job
            myJob = currTile.world.jobQueue.Dequeue();

            if(myJob != null)
            {
                // We have job
                destTile = myJob.tile;
                myJob.RegisterJobCompleteCallback(OnJobEnded);
                myJob.RegisterJobCancelCallback(OnJobEnded);
            }
        }

        // Are we there
        if (currTile == destTile)
        {
            if(myJob != null)
            {
                myJob.DoWork(deltaTime);
            }

            return;
        }

        // Total distance between A and B
        float distToTravel = Mathf.Sqrt(Mathf.Pow(currTile.X - destTile.X, 2) + Mathf.Pow(currTile.Y - destTile.Y, 2));

        // How much distance can be travelled this Update
        float distThisFrame = speed * deltaTime;

        // How much is that in terms of percentage
        float percThisFrame = distThisFrame / distToTravel;

        // Add that to overall percentage travelled
        movementPercentage += percThisFrame;

        if (movementPercentage >= 1)
        {
            // Reached destination
            currTile = destTile;
            movementPercentage = 0;

            // FIXME: Overshot movement?
        }

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
}
