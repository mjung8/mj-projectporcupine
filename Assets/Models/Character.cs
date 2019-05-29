using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public Character(Tile tile)
    {
        currTile = destTile = tile;
    }

    public void Update(float deltaTime)
    {
        // Are we there
        if (currTile == destTile)
            return;

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
    }

    public void SetDestination(Tile tile)
    {
        if (currTile.IsNeighbour(tile, true) == false)
        {
            Debug.Log("Character::SetDestination -- destination tile is not a neighbour.");
        }
    }
}
