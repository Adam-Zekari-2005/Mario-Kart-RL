using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class SoloPolishMigration
{
    public static void Run()
    {
        var report=new System.Text.StringBuilder();
        var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/Models/Characters/Mario.fbx");
        var takes=importer.defaultClipAnimations;
        foreach(var take in takes)report.AppendLine("TAKE "+take.name+" source="+take.takeName);
        var clips=importer.clipAnimations;
        foreach(var clip in clips)
        {
            var take=takes.FirstOrDefault(t=>t.name==clip.name||t.name.EndsWith("|"+clip.name)||t.takeName.EndsWith("|"+clip.name));
            if(take!=null){clip.takeName=take.takeName;report.AppendLine("BOUND "+clip.name+" -> "+take.takeName);}
            else report.AppendLine("UNMATCHED "+clip.name);
        }
        importer.clipAnimations=clips;importer.SaveAndReimport();
        foreach(var track in MigrationAudit.Tracks)
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+track+".unity");
            var player=Object.FindAnyObjectByType<Player>();
            if(player.GetComponent<SoloPlayerAssist>()==null)player.gameObject.AddComponent<SoloPlayerAssist>();
            foreach(var bomb in Object.FindObjectsByType<Bobomb>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var skins=bomb.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                report.AppendLine(track+" BOMB skins: "+string.Join(", ",skins.Select(s=>s.name+" material="+s.sharedMaterial?.name)));
                for(int i=0;i<bomb.renderers.Length;i++)
                    if(bomb.renderers[i]==null)
                    {
                        var match=skins.FirstOrDefault(s=>!bomb.renderers.Contains(s)&&s.sharedMaterial==bomb.regMat[i])??skins.FirstOrDefault(s=>!bomb.renderers.Contains(s));
                        if(match==null)throw new Exception("Missing bomb renderer: "+track);
                        bomb.renderers[i]=match;
                    }
                EditorUtility.SetDirty(bomb);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        File.WriteAllText("Migration/solo-polish.txt",report.ToString());
        EditorSceneManager.OpenScene("Assets/Scenes/WhiteScreen.unity");
        Debug.Log("SOLO POLISH COMPLETE");EditorApplication.Exit(0);
    }
}
