using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

/// <summary>
/// A Character is an entity on the map that can move between tiles.
/// For now, it only moves after getting a job from the work queue.
/// </summary>
public class Character : IXmlSerializable, ISelectable
{
    /// <summary>
    /// Returns a float representing the Character's X position.
    /// Can be part-way between two tiles during movement.
    /// </summary>
    public float X
    {
        get
        {
            if (NextTile == null)
                return CurrTile.X;

            return Mathf.Lerp(CurrTile.X, NextTile.X, movementPercentage);
        }
    }

    /// <summary>
    /// Returns a float representing the Character's Y position.
    /// Can be part-way between two tiles during movement. 
    /// </summary>
    public float Y
    {
        get
        {
            if (NextTile == null)
                return CurrTile.Y;

            return Mathf.Lerp(CurrTile.Y, NextTile.Y, movementPercentage);
        }
    }

    /// <summary>
    /// The tile the Character is considered to still be standing in.
    /// </summary>
    public Tile CurrTile
    {
        get
        {
            return _currTile;
        }

        protected set
        {
            if (_currTile != null)
            {
                _currTile.characters.Remove(this);
            }
            _currTile = value;
            _currTile.characters.Add(this);
        }
    }
    private Tile _currTile;

    /// <summary>
    /// The Character's current goal tile (not necessarily the next one they'll be entering).
    /// If not moving, destTile = currTile
    /// </summary>
    Tile DestTile
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
    Tile _destTile;

    /// The next tile in the pathfinding sequence (the one we are about to enter). 
    Tile NextTile;

    /// Goes from 0-1 as it moves from CurrTile to NextTile.
    float movementPercentage;

    /// Holds the path to reach DestTile.
    Path_AStar pathAStar;

    /// Tiles per second.
    float speed = 5f;

    /// A callback to trigger when Character information changes (e.g. the position).
    Action<Character> cbCharacterChanged;

    /// Our job, if any.
    Job myJob;

    /// The item we are carrying (not gear/equipment).
    public Inventory inventory;

    /// Use only for serialization
    public Character()
    {

    }

    public Character(Tile tile)
    {
        CurrTile = DestTile = NextTile = tile;
    }

    void GetNewJob()
    {
        myJob = World.Current.jobQueue.Dequeue();

        if (myJob == null)
        {
            myJob = new Job(CurrTile,
                "Waiting",
                null,
                UnityEngine.Random.Range(0.1f, 0.5f),
                null,
                false);
        }

        DestTile = myJob.tile;
        myJob.RegisterJobStoppedCallback(OnJobStopped);

        // Immediately check to see if the job tile is reachable
        pathAStar = new Path_AStar(World.Current, CurrTile, DestTile);  // This will calculate path from curr to dest
        if (pathAStar.Length() == 0)
        {
            Debug.LogError("Path_AStar returned no path to destination!");
            AbandonJob();
            DestTile = CurrTile;
        }
    }

    void Update_DoJob(float deltaTime)
    {
        // Do I have a job?
        if (myJob == null)
        {
            GetNewJob();
        }

        if (CheckForJobMaterials()) //make sure all materials are in place
        {
            // Materials acquired. Go to job and do job.
            DestTile = myJob.tile;

            // Are we there yet?
            if (CurrTile == myJob.tile)
            {
                // We are at the correct tile for our job so execute
                // the job's DoWork which is mostly going to couintdown
                // jobTime and potentially call its JobCompleteCallback
                myJob.DoWork(deltaTime);
            }
        }
    }

    /// <summary>
    /// Checks weather the current job has all the materials in place and if not instructs the working character to get the materials there first.
    /// Only ever returns true if all materials for the job are at the job location and thus signals to the calling code, that it can proceed with job execution.
    /// </summary>
    /// <returns></returns>
    bool CheckForJobMaterials()
    {
        if (myJob.HasAllMaterial())
            return true; //we can return early

        // Job still needs materials
        // Am I carrying anything the job location wants?
        if (inventory != null)
        {
            if (myJob.DesiresInventoryType(inventory) > 0)
            {
                // If so, deliver
                // Walk to the job tile then drop off the stack into the job
                if (CurrTile == myJob.tile)
                {
                    // We are at the job site so drop the inventory
                    World.Current.inventoryManager.PlaceInventory(myJob, inventory);
                    myJob.DoWork(0);  // This will call all cbJobWorked callbacks

                    //at this point we should dump anything in our inventory
                    DumpExcessInventory();
                }
                else
                {
                    // Walk to job site
                    DestTile = myJob.tile;
                    return false;
                }
            }
            else
            {
                // We are carrying something but the job doesn't want it
                // Dump it
                DumpExcessInventory();
            }
        }
        else
        {
            // At this point, the job still requires inventory but we aren't carrying it
            // Are we standing on a tile with goods desired by job?
            if (CurrTile.inventory != null &&
                myJob.DesiresInventoryType(CurrTile.inventory) > 0 &&
                (myJob.canTakeFromStockpile || CurrTile.furniture == null || CurrTile.furniture.IsStockpile() == false))
            {
                // Pick up the stuff
                World.Current.inventoryManager.PlaceInventory(
                    this,
                    CurrTile.inventory,
                    myJob.DesiresInventoryType(CurrTile.inventory)
                );
            }
            else
            {
                // If not, walk towards a tile containing the required goods

                // Find the first thing in the job that isn't satisfied
                Inventory desired = myJob.GetFirstDesiredInventory();

                if (CurrTile != NextTile)
                {
                    // We are still moving somewhere so just bail out
                    return false;
                }

                // Any chance we already have a path that leads to the items we want?
                if (pathAStar != null && pathAStar.EndTile() != null && pathAStar.EndTile().inventory != null && pathAStar.EndTile().inventory.objectType == desired.objectType)
                {
                    // We are already moving towards a tile that we want so do nothing
                }
                else
                {
                    Path_AStar newPath = World.Current.inventoryManager.GetPathToClosestInventoryOfType(
                        desired.objectType,
                        CurrTile,
                        desired.maxStackSize - desired.stackSize,
                        myJob.canTakeFromStockpile
                        );

                    if (newPath == null || newPath.Length() == 0)
                    {
                        Debug.Log("No tile contains objects of type " + desired.objectType + "to satisfy job requirements");
                        AbandonJob();
                        return false;
                    }

                    Debug.Log("pathAStar returned with length of: " + newPath.Length());

                    DestTile = newPath.EndTile();

                    // Since we already have a path calculated let's just save that
                    pathAStar = newPath;

                    // Ignore the first tile because that's the tile we're already in
                    NextTile = newPath.Dequeue();
                }

                // One way or the other, we are now on route to an object of the right type
                return false;
            }
        }

        return false; // Can't continue until all materials are satisifed
    }

    /// <summary>
    /// This function instructs the character to null its inventory.
    /// TODO: actually look for a place to dump the materials and then do so.
    /// </summary>
    private void DumpExcessInventory()
    {
        // TODO: Look for Places accepting the inventory in the following order:
        // - Jobs also needing this item (this could serve us when building Walls, as the character could transport ressources for multiple walls at once)
        // - Stockpiles (as not to clutter the floor)
        // - Floor

        //if (World.current.inventoryManager.PlaceInventory(CurrTile, inventory) == false)
        //{
        //    Debug.LogError("Character tried to dump inventory into an invalid tile (maybe there's already something here). FIXME: Setting inventory to null and leaking for now");
        //    // FIXME: For the sake of continuing on, we are still going to dump any
        //    // reference to the current inventory, but this means we are "leaking"
        //    // inventory.  This is permanently lost now.
        //}

        inventory = null;
    }

    public void AbandonJob()
    {
        NextTile = DestTile = CurrTile;
        World.Current.jobQueue.Enqueue(myJob);
        myJob = null;
    }

    void Update_DoMovement(float deltaTime)
    {
        if (CurrTile == DestTile)
        {
            pathAStar = null;
            return; // Already there
        }

        // currTile = the tile I'm currently in (and may be leaving)
        // nextTile = the tile I'm currently entering
        // destTile = Our final destination -- we never walk here directly, but instead use it for the pathfinder

        if (NextTile == null || NextTile == CurrTile)
        {
            // Get the next tile from the pathfinder
            if (pathAStar == null || pathAStar.Length() == 0)
            {
                // Generate a path to our destination
                pathAStar = new Path_AStar(World.Current, CurrTile, DestTile);  // This will calculate path from curr to dest
                if (pathAStar.Length() == 0)
                {
                    Debug.LogError("Path_AStar returned no path to destination!");
                    AbandonJob();
                    return;
                }

                // Let's ignore the first tile because that's the tile we're currently in
                NextTile = pathAStar.Dequeue();
            }

            // Grab the next waypoint from the pathing system!
            NextTile = pathAStar.Dequeue();

            if (NextTile == CurrTile)
            {
                Debug.LogError("Update_DoMovement - nextTile is currTile?");
            }
        }

        // At this point we should have a valid nextTile to move to

        // Total distance between A and B
        // Euclidean distance for now; for pathfinding change to Manhattan or something else
        float distToTravel = Mathf.Sqrt(
            Mathf.Pow(CurrTile.X - NextTile.X, 2) +
            Mathf.Pow(CurrTile.Y - NextTile.Y, 2)
        );

        if (NextTile.IsEnterable() == ENTERABILITY.Never)
        {
            // Most likely a wall got built, so we need to reset pathfinding
            // FIXME: when a wall gets spanwed, invalidate path immediately. or check sometimes to save CPU.
            // or register a callback to ontilechanged event
            Debug.LogError("Fix me - character trying to walk through unwalkable  tile");
            NextTile = null;    // our next tile is a no-go
            pathAStar = null;   // pathfinding info is out of date
            return;
        }
        else if (NextTile.IsEnterable() == ENTERABILITY.Soon)
        {
            // Can't enter now but should be able to in the future.(Door?)
            // Don't bail on movement path but return now and don't process movement.
            return;
        }

        // How much distance can be travelled this Update
        float distThisFrame = speed / NextTile.movementCost * deltaTime;

        // How much is that in terms of percentage
        float percThisFrame = distThisFrame / distToTravel;

        // Add that to overall percentage travelled
        movementPercentage += percThisFrame;

        if (movementPercentage >= 1)
        {
            // Reached destination

            // TODO: get the next tile from pathfinding system

            CurrTile = NextTile;
            movementPercentage = 0;

            // FIXME: Overshot movement?
        }
    }

    /// Runs every "frame" while simulation is not paused
    public void Update(float deltaTime)
    {
        //Debug.Log("Character Update");

        Update_DoJob(deltaTime);

        Update_DoMovement(deltaTime);

        if (cbCharacterChanged != null)
            cbCharacterChanged(this);

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

    #region IXmlSerializable implementation

    public XmlSchema GetSchema()
    {
        throw new NotImplementedException();
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", CurrTile.X.ToString());
        writer.WriteAttributeString("Y", CurrTile.Y.ToString());
    }

    #endregion

    public void ReadXml(XmlReader reader)
    {

    }

    #region ISelectableInterface implementation

    public string GetName()
    {
        return "Sally S. Smith";
    }

    public string GetDescription()
    {
        return "A human astronaut. She is currently depressed because her friend was ejected out of an airlock.";
    }

    public string GetHitPointString()
    {
        return "100/100";
    }

    #endregion
}
