using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class DriverPoseAudit
{
    const string Key="DriverPoseAudit";
    static float start=-1;
    static int sample;
    static readonly float[] moments={.5f,2.5f,3.5f,5f,7f,8.8f,10f};
    static DriverPoseAudit(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        Directory.CreateDirectory("Migration/PoseAudit");File.WriteAllText("Migration/PoseAudit/report.txt","");
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        SessionState.SetInt(Key+"track",0);SessionState.SetInt(Key+"limit",1);SessionState.SetInt(Key,1);EditorApplication.isPlaying=true;
    }
    public static void RunOtherTracks(){Run();SessionState.SetInt(Key+"track",1);SessionState.SetInt(Key+"limit",5);EditorApplication.isPlaying=false;SessionState.SetInt(Key,3);}
    static void Tick()
    {
        int stage=SessionState.GetInt(Key,0);
        if(stage==0||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if((stage==2||stage==3)&&!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            int next=SessionState.GetInt(Key+"track",0)+(stage==2?1:0);
            if(next>=SessionState.GetInt(Key+"limit",1)){SessionState.SetInt(Key,0);EditorApplication.Exit(0);return;}
            SessionState.SetInt(Key+"track",next);start=-1;sample=0;
            EditorSceneManager.OpenScene("Assets/Scenes/"+MigrationAudit.Tracks[next]+".unity");SessionState.SetInt(Key,1);EditorApplication.isPlaying=true;return;
        }
        if(!EditorApplication.isPlaying||!RACE_MANAGER.RACE_STARTED)return;
        var p=Object.FindAnyObjectByType<Player>();
        if(start<0)start=Time.time;
        float t=Time.time-start;
        KartKeyboard.TestHeld=k=>(k==KeyCode.UpArrow&&t>=1&&t<3)||(k==KeyCode.A&&t>=8&&t<9);
        if(sample<moments.Length&&t>=moments[sample])
        {
            string track=MigrationAudit.Tracks[SessionState.GetInt(Key+"track",0)];
            string report=$"SAMPLE {track} {sample} time={t:F2} speed={p.currentspeed:F2}\n";
            foreach(var animator in p.GetComponentsInChildren<Animator>(true))
            {
                report+=$"ANIM {animator.name} rootMotion={animator.applyRootMotion} localRot={animator.transform.localEulerAngles}\n";
                if(animator.isActiveAndEnabled)for(int layer=0;layer<animator.layerCount;layer++)report+=$"LAYER {layer} "+string.Join(",",animator.GetCurrentAnimatorClipInfo(layer).Select(c=>c.clip.name+":"+c.weight))+"\n";
            }
            foreach(var tr in p.Driver.GetComponentsInChildren<Transform>(true).Where(x=>x.name=="Armature"||x.name=="Bone"||x.name=="Bone_009"))report+=$"BONE {tr.name} local={tr.localEulerAngles} world={tr.eulerAngles} pos={tr.localPosition}\n";
            File.AppendAllText("Migration/PoseAudit/report.txt",report);Debug.Log($"POSE SAMPLE {sample}");
            var camera=Object.FindAnyObjectByType<RACE_MANAGER>().FrontCam.GetComponent<Camera>();
            var pos=camera.transform.position;var rot=camera.transform.rotation;
            camera.transform.position=p.transform.position+p.transform.right*7-p.transform.forward*2+p.transform.up*3;
            camera.transform.LookAt(p.transform.position+p.transform.up*1.2f);
            MigrationAudit.Capture(camera,$"Migration/PoseAudit/{track}-{sample}.png");
            camera.transform.SetPositionAndRotation(pos,rot);sample++;
        }
        if(t>11){KartKeyboard.TestHeld=null;SessionState.SetInt(Key,2);EditorApplication.isPlaying=false;}
    }
}
