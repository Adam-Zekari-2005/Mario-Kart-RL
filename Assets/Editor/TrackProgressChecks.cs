using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class TrackProgressChecks
{
    public static void Run()
    {
        var root = new GameObject("ProgressChecks");
        try
        {
            var points = new GameObject("Checkpoints").transform;
            points.SetParent(root.transform);
            Vector3[] positions = { new Vector3(0,0,10), new Vector3(10,0,10), new Vector3(10,0,0), Vector3.zero };
            for (int i=0; i<positions.Length; i++)
            {
                var point = new GameObject("Checkpoint" + i);
                point.transform.SetParent(points);
                point.transform.position = positions[i];
                point.AddComponent<BoxCollider>().isTrigger = true;
            }
            var kart = new GameObject("TestKart");
            kart.transform.SetParent(root.transform);
            var body = kart.AddComponent<Rigidbody>();
            body.useGravity = false;
            var counter = kart.AddComponent<LapCounter>();
            counter.enabled = false;
            counter.checkpoints = points;
            var progress = kart.AddComponent<TrackProgress>();
            typeof(TrackProgress).GetMethod("Start", BindingFlags.Instance|BindingFlags.NonPublic).Invoke(progress,null);
            Check(progress.IsReady, "initialisation");
            kart.transform.position = new Vector3(0,0,5);
            progress.Sample();
            Check(Mathf.Abs(progress.ProgressMetres-5)<0.001f, "progression de cinq metres");
            kart.transform.position = new Vector3(0,0,2);
            progress.Sample();
            Check(Mathf.Abs(progress.ProgressMetres-2)<0.001f, "recul mesure");
            RACE_MANAGER.RACE_STARTED = true;
            RACE_MANAGER.RACE_COMPLETED = false;
            var trigger = typeof(TrackProgress).GetMethod("OnTriggerEnter",BindingFlags.Instance|BindingFlags.NonPublic);
            body.linearVelocity = Vector3.back;
            trigger.Invoke(progress,new object[]{points.GetChild(0).GetComponent<Collider>()});
            Check(progress.CheckpointsPassed==0,"passage inverse refuse");
            body.linearVelocity = Vector3.forward;
            trigger.Invoke(progress,new object[]{points.GetChild(2).GetComponent<Collider>()});
            Check(progress.CheckpointsPassed==0,"checkpoint saute refuse");
            trigger.Invoke(progress,new object[]{points.GetChild(0).GetComponent<Collider>()});
            Check(progress.CheckpointsPassed==1 && progress.NextCheckpoint==1,"passage ordonne accepte");
            trigger.Invoke(progress,new object[]{points.GetChild(0).GetComponent<Collider>()});
            Check(progress.CheckpointsPassed==1,"double passage refuse");
            var episode = kart.AddComponent<KartEpisode>();
            var surface = new GameObject("Surface");
            surface.transform.SetParent(root.transform);
            surface.transform.position = kart.transform.position + Vector3.down;
            surface.AddComponent<BoxCollider>();
            surface.tag = "Dirt";
            Physics.SyncTransforms();
            Check(episode.IsOnGrass(), "gazon detecte sous le kart");
            surface.tag = "Ground";
            Check(!episode.IsOnGrass(), "route sans penalite gazon");
            surface.tag = "Dirt";
            kart.transform.position += Vector3.up * 10;
            Physics.SyncTransforms();
            Check(!episode.IsOnGrass(), "saut sans penalite gazon");
            Debug.Log("TrackProgressChecks PASS (10 controles)");
        }
        finally
        {
            RACE_MANAGER.RACE_STARTED = false;
            RACE_MANAGER.RACE_COMPLETED = false;
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
    static void Check(bool result,string label)
    {
        if (!result) throw new Exception("TrackProgressChecks FAIL : " + label);
    }
}

