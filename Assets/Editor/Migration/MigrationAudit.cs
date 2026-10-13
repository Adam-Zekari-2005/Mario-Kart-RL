using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class MigrationAudit
{
    public static readonly string[] Tracks={"MooMooMeadows","Toad Harbor","Mario Circuit","Rainbow Road","Water Park"};
    [MenuItem("Mario Kart/Ouvrir le menu du jeu migre")]
    public static void OpenMenu(){if(!EditorApplication.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene("Assets/Scenes/WhiteScreen.unity");}
    public static void Run()
    {
        Directory.CreateDirectory("Migration/Previews");
        var report=new System.Text.StringBuilder();
        foreach(var name in Tracks)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
            var objects=Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            int scripts=objects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
            var meshes=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(x=>x.sharedMesh==null).ToArray();
            var skins=Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(x=>x.sharedMesh==null).ToArray();
            var mats=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).SelectMany(x=>x.sharedMaterials).Where(x=>x==null||x.shader==null||!x.shader.isSupported).ToArray();
            string line=$"AUDIT {name}: objects={objects.Length}, missingScripts={scripts}, missingMeshes={meshes.Length}, missingSkins={skins.Length}, unsupportedMaterials={mats.Length}";
            report.AppendLine(line);Debug.Log(line);
            foreach(var m in meshes)report.AppendLine("Missing mesh: "+Hierarchy(m.transform));
            foreach(var m in skins)report.AppendLine("Missing skin: "+Hierarchy(m.transform));
            var camera=Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if(camera!=null)
            {
                var player=Object.FindFirstObjectByType<Player>();
                if(player!=null){camera.transform.position=player.transform.position-player.transform.forward*8+player.transform.up*4;camera.transform.LookAt(player.transform.position+player.transform.forward*8+player.transform.up);}
                camera.enabled=true;camera.gameObject.SetActive(true);
                Capture(camera,"Migration/Previews/"+name+".png");
            }
        }
        File.WriteAllText("Migration/scene-audit.txt",report.ToString());
        EditorSceneManager.OpenScene("Assets/Scenes/WhiteScreen.unity");
        AssetDatabase.SaveAssets();Debug.Log("MIGRATION AUDIT COMPLETE");EditorApplication.Exit(0);
    }
    static string Hierarchy(Transform t){return t.parent==null?t.name:Hierarchy(t.parent)+"/"+t.name;}
    public static void Capture(Camera camera,string path)
    {
        var rt=RenderTexture.GetTemporary(1280,720,24);camera.targetTexture=rt;
        camera.Render();camera.Render();camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);
    }
}
