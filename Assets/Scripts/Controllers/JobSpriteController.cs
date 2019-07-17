using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobSpriteController : MonoBehaviour
{

    // This barebones controller is mostly going to piggyback
    // on FurnitureSpriteController for now

    FurnitureSpriteController fsc;
    Dictionary<Job, GameObject> jobGameObjectMap;

    // Use this for initialization
    void Start()
    {
        jobGameObjectMap = new Dictionary<Job, GameObject>();
        fsc = GameObject.FindObjectOfType<FurnitureSpriteController>();

        WorldController.Instance.world.jobQueue.RegisterJobCreationCallback(OnJobCreated);
    }

    void OnJobCreated(Job job)
    {
        if (job.jobObjectType == null)
        {
            //This job doesn't have an associated sprite so no need to render
            return;
        }

        // FIXME we can only do furniture building jobs

        // TODO: Sprite

        if (jobGameObjectMap.ContainsKey(job))
        {
            //Debug.LogError("OnJobCreated for a jobGO that already exists -- most likely job REQUEUE");
            return;
        }

        GameObject job_go = new GameObject();

        // Add the tile/GO pair to the dictionary
        jobGameObjectMap.Add(job, job_go);

        job_go.name = "JOB_" + job.jobObjectType + "_ " + job.tile.X + "_" + job.tile.Y;
        job_go.transform.position = new Vector3(job.tile.X + ((job.furniturePrototype.Width - 1) / 2f), job.tile.Y + ((job.furniturePrototype.Height - 1) / 2f), 0);
        job_go.transform.SetParent(this.transform, true);

        SpriteRenderer sr = job_go.AddComponent<SpriteRenderer>();
        sr.sprite = fsc.GetSpriteForFurniture(job.jobObjectType);
        sr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        sr.sortingLayerName = "Jobs";

        // FIXME: This hardcoding is not good
        if (job.jobObjectType == "Door")
        {
            // By default, door graphic is meant for walls EW
            // Check to see if we actually have a wall NS and then rotate
            Tile northTile = World.Current.GetTileAt(job.tile.X, job.tile.Y + 1);
            Tile southTile = World.Current.GetTileAt(job.tile.X, job.tile.Y - 1);

            if (northTile != null && southTile != null && northTile.furniture != null
                && southTile.furniture != null && northTile.furniture.objectType.Contains("Wall")
                && southTile.furniture.objectType.Contains("Wall"))
            {
                job_go.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        job.RegisterJobCompletedCallback(OnJobEnded);
        job.RegisterJobStoppedCallback(OnJobEnded);
    }

    void OnJobEnded(Job job)
    {
        // This executes whether a job was COMPLETED or CANCELLED
        // FIXME we can only do furniture building jobs

        GameObject job_go = jobGameObjectMap[job];
        job.UnregisterJobCompletedCallback(OnJobEnded);
        job.UnregisterJobStoppedCallback(OnJobEnded);

        Destroy(job_go);
    }

}
