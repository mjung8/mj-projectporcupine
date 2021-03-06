﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class JobQueue
{
    Queue<Job> jobQueue;

    public event Action<Job> cbJobCreated;

    public JobQueue()
    {
        jobQueue = new Queue<Job>();
    }

    public void Enqueue(Job j)
    {
        //Debug.Log("Adding job to queue. Existing queue size: " + jobQueue.Count);
        if (j.jobTime < 0)
        {
            // Job has a negative job time, so it's not supposed to
            // queue. Just insta-complete.
            j.DoWork(0);
            return;
        }

        jobQueue.Enqueue(j);

        if (cbJobCreated != null)
        {
            cbJobCreated(j);
        }
    }

    public Job Dequeue()
    {
        if (jobQueue.Count == 0)
        {
            return null;
        }

        return jobQueue.Dequeue();
    }

    public void Remove(Job j)
    {
        // TODO: check docs to see if there's a less memory/swappy solution
        List<Job> jobs = new List<Job>(jobQueue);

        if (jobs.Contains(j) == false)
        {
            //Debug.LogError("Trying to remove a job that doesn't exist on the queue.");
            // Most likely, this job wasn't on the queue because a character was working it
            return;
        }

        jobs.Remove(j);
        jobQueue = new Queue<Job>(jobs);
    }
}
