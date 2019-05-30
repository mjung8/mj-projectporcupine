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
        // FIXME we can only do furniture building jobs

        GameObject job_go = new GameObject();

        // Add the tile/GO pair to the dictionary
        jobGameObjectMap.Add(job, job_go);

        job_go.name = "JOB_" + job.jobObjectType + "_ " + job.tile.X + "_" + job.tile.Y;
        job_go.transform.position = new Vector3(job.tile.X, job.tile.Y, 0);
        job_go.transform.SetParent(this.transform, true);

        // FIXME: we assume the object must be a wall so use the hardcoded
        // reference to the wall sprite
        SpriteRenderer sr = job_go.AddComponent<SpriteRenderer>();
        sr.sprite = fsc.GetSpriteForFurniture(job.jobObjectType);
        sr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        sr.sortingLayerName = "Jobs";

        // Make sorting order in layer above tiles (builds have a bug where wall sprite is below tile sprite)
        sr.sortingOrder = 1;

        job.RegisterJobCompleteCallback(OnJobEnded);
        job.RegisterJobCancelCallback(OnJobEnded);
    }

    void OnJobEnded(Job job)
    {
        // This executes whether a job was COMPLETED or CANCELLED
        // FIXME we can only do furniture building jobs

        GameObject job_go = jobGameObjectMap[job];
        job.UnregisterJobCompleteCallback(OnJobEnded);
        job.UnregisterJobCancelCallback(OnJobEnded);

        Destroy(job_go);
    }

}
