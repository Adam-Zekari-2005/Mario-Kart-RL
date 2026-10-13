using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class SoloMigration
{
    static bool Belongs(Object value,HashSet<GameObject> objects)
    {
        if(value is GameObject go)return objects.Contains(go);
        if(value is Component component)return objects.Contains(component.gameObject);
        return false;
    }
    public static void Run()
    {
        Directory.CreateDirectory("Migration/BeforeSolo");
        var report=new System.Text.StringBuilder();
        foreach(string track in MigrationAudit.Tracks)
        {
            string path="Assets/Scenes/"+track+".unity";
            string backup="Migration/BeforeSolo/"+track+".unity";
            if(!File.Exists(backup))File.Copy(path,backup);
            var scene=EditorSceneManager.OpenScene(path);
            var bots=Object.FindObjectsByType<ComputerDriver>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var removed=new HashSet<GameObject>();
            foreach(var bot in bots)
            {
                if(bot.GetComponent<Player>()!=null)throw new Exception("Refusing to remove human player");
                foreach(var t in bot.GetComponentsInChildren<Transform>(true))removed.Add(t.gameObject);
                foreach(var map in bot.GetComponentsInChildren<Minimap>(true))
                    if(map.playerInMap!=null)removed.Add(map.playerInMap.gameObject);
            }
            // Remove outgoing serialized array references before deleting the racer instances.
            // Prefabs and character assets are deliberately kept for later RL work.
            foreach(var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(behaviour==null||removed.Contains(behaviour.gameObject))continue;
                var serialized=new SerializedObject(behaviour);var property=serialized.GetIterator();
                var arrays=new List<string>();
                while(property.NextVisible(true))if(property.isArray&&property.propertyType!=SerializedPropertyType.String)arrays.Add(property.propertyPath);
                foreach(var name in arrays)
                {
                    var array=serialized.FindProperty(name);if(array==null)continue;
                    for(int i=array.arraySize-1;i>=0;i--)
                    {
                        var entry=array.GetArrayElementAtIndex(i);
                        if(entry.propertyType!=SerializedPropertyType.ObjectReference||!Belongs(entry.objectReferenceValue,removed))continue;
                        entry.objectReferenceValue=null;array.DeleteArrayElementAtIndex(i);
                    }
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var bot in bots)
            {
                foreach(var map in bot.GetComponentsInChildren<Minimap>(true))
                    if(map.playerInMap!=null)Object.DestroyImmediate(map.playerInMap.gameObject);
                Object.DestroyImmediate(bot.gameObject);
            }
            var race=Object.FindAnyObjectByType<RACE_MANAGER>();
            var humans=Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
            if(humans.Length!=1)throw new Exception(track+": expected one human kart");
            race.lapCounters=new List<LapCounter>{humans[0].GetComponent<LapCounter>()};race.sortedRacers=new List<LapCounter>(race.lapCounters);
            race.PositionCounter.SetActive(false);EditorUtility.SetDirty(race);
            EditorSceneManager.SaveScene(scene);
            string line=$"SOLO {track}: removed={bots.Length}, humans={humans.Length}, botsRemaining={Object.FindObjectsByType<ComputerDriver>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length}, registeredRacers={race.lapCounters.Count}";
            report.AppendLine(line);Debug.Log(line);
        }
        File.WriteAllText("Migration/solo-audit.txt",report.ToString());
        EditorSceneManager.OpenScene("Assets/Scenes/WhiteScreen.unity");AssetDatabase.SaveAssets();EditorApplication.Exit(0);
    }
}
