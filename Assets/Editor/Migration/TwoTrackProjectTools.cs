using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class TwoTrackProjectTools
{
    static string[] ScenePaths => new[]{"Assets/Scenes/WhiteScreen.unity"}.Concat(MigrationAudit.Tracks.Select(t=>"Assets/Scenes/"+t+".unity")).ToArray();
    public static void Configure()
    {
        var scene=EditorSceneManager.OpenScene(ScenePaths[0]);
        foreach(var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            string method=button.onClick.GetPersistentEventCount()>0?button.onClick.GetPersistentMethodName(0):"";
            if(new[]{"ToadHarbor","RainbowRoad","WaterPark"}.Contains(method)){button.gameObject.SetActive(false);continue;}
            if(method!="MooMooMeadows"&&method!="MarioCircuit")continue;
            button.gameObject.SetActive(true);
            var rect=button.GetComponent<RectTransform>();
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.localScale=Vector3.one;rect.sizeDelta=new Vector2(360,64);
            rect.anchoredPosition=new Vector2(0,method=="MooMooMeadows"?45:-45);
            var label=button.GetComponentInChildren<Text>();label.text=method=="MooMooMeadows"?"Moo Moo Meadows":"Mario Circuit";
            label.fontSize=26;label.alignment=TextAnchor.MiddleCenter;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes=ScenePaths.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
        Validate();
        // Render the existing UI for inspection without saving this camera-mode change.
        var camera=Camera.main;
        foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if(canvas.isRootCanvas){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
        Canvas.ForceUpdateCanvases();Directory.CreateDirectory("Migration/Previews");
        MigrationAudit.Capture(camera,"Migration/Previews/TwoTrackMenu.png");
        EditorSceneManager.OpenScene(ScenePaths[0]);
        Debug.Log("TWO TRACK CONFIGURATION COMPLETE");EditorApplication.Exit(0);
    }
    static void Validate()
    {
        if(!EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).SequenceEqual(ScenePaths))throw new Exception("Unexpected build scene list");
        var buttons=Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        if(buttons.Length!=2)throw new Exception("Expected exactly two visible track buttons");
        foreach(var track in MigrationAudit.Tracks)
            if(!buttons.Any(b=>b.onClick.GetPersistentEventCount()==1&&b.onClick.GetPersistentMethodName(0)==track.Replace(" ","")))throw new Exception("Missing track button: "+track);
        foreach(var archived in new[]{"Toad Harbor","Rainbow Road","Water Park"})
            if(!File.Exists("Assets/Scenes/"+archived+".unity"))throw new Exception("Archived scene was removed");
    }
    public static void Build()
    {
        EditorSceneManager.OpenScene(ScenePaths[0]);Validate();
        Directory.CreateDirectory("Builds/TwoTracks");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=ScenePaths,locationPathName="Builds/TwoTracks/MarioKart.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        string result=$"Build: {report.summary.result}; errors: {report.summary.totalErrors}; scenes: {string.Join(", ",ScenePaths)}";
        File.WriteAllText("Migration/two-track-build.txt",result);Debug.Log(result);
        EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
    }
}
