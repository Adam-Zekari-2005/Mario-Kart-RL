using System;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RivalFeatureAudit
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        Physics.SyncTransforms();
        var human = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        var items = human.GetComponent<ItemManager>();
        var dump = new Dump {
            start = human.transform.position,
            route = Enumerable.Range(0, items.path.childCount).Select(i => items.path.GetChild(i).position).ToArray(),
            checkpoints = Enumerable.Range(0, human.GetComponent<LapCounter>().checkpoints.childCount).Select(i => human.GetComponent<LapCounter>().checkpoints.GetChild(i).position).ToArray(),
            features = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
                .Where(c => c.CompareTag("TrickCollider") || c.CompareTag("GliderPanel") || c.CompareTag("JumpPanel"))
                .Select(c => new Feature { name=c.name, tag=c.tag, center=c.bounds.center, size=c.bounds.size, forward=c.transform.forward, trigger=c.isTrigger }).ToArray(),
            inventory = items.item_gameobjects.Select(x => x.name).ToArray(),
            ui = items.ItemUI.name + " / " + items.your_item.name + " child=" + items.your_item.transform.IsChildOf(items.ItemUI.transform)
        };
        Directory.CreateDirectory("Training/Results");
        File.WriteAllText("Training/Results/rival-features.json", JsonUtility.ToJson(dump,true));
        Debug.Log("RIVAL FEATURE AUDIT PASS: " + dump.features.Length + " bonus colliders; " + dump.ui);
    }
    [Serializable] class Dump { public Vector3 start; public Vector3[] route, checkpoints; public Feature[] features; public string[] inventory; public string ui; }
    [Serializable] class Feature { public string name,tag; public Vector3 center,size,forward; public bool trigger; }
}
