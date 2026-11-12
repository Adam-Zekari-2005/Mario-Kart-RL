using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class MooMooDuelCheck
{
    const string Key = "MooMooDuelCheck";
    static float start = -1, idleTravel, drivenTravel;
    static Vector3 grid;
    static int nextReport;
    static bool errors, gridOK, forcedFall;
    static MooMooDuelCheck() { EditorApplication.update += Tick; Application.logMessageReceived += Log; }
    static void Log(string msg,string stack,LogType type)
    {
        if(SessionState.GetInt(Key,0)==1 && (type==LogType.Exception || type==LogType.Error)) errors=true;
    }
    public static void Run()
    {
        SessionState.SetInt(Key,1);
        SessionState.SetString(Key+"deadline", DateTime.UtcNow.AddMinutes(6).Ticks.ToString());
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        int stage=SessionState.GetInt(Key,0);
        if(stage==0) return;
        if(stage==2 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetInt(Key,0);
            EditorSceneManager.OpenScene("Assets/Scenes/WhiteScreen.unity");
            EditorApplication.Exit(SessionState.GetBool(Key+"pass",false)?0:1); return;
        }
        if(stage!=1) return;
        if(DateTime.UtcNow.Ticks > long.Parse(SessionState.GetString(Key+"deadline","0"))) { End(false); return; }
        if(!EditorApplication.isPlaying || !RACE_MANAGER.RACE_STARTED) return;
        var human=GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        var rival=Object.FindAnyObjectByType<MooMooRival>();
        if(!rival) { End(false); return; }
        if(start<0)
        {
            Time.timeScale=3;
            start=Time.time;
            grid=human.transform.position;
            var playerIcon=human.GetComponent<Minimap>().playerInMap;
            var rivalIcon=rival.GetComponent<Minimap>().playerInMap;
            gridOK = OnRoad(human) && OnRoad(rival.GetComponent<Player>()) && playerIcon != rivalIcon
                && rivalIcon.GetChild(0).GetComponent<UnityEngine.UI.Image>().sprite.name == "LuigiMap";
            Debug.Log($"DUEL GRID roads and icons={gridOK}");
        }
        float elapsed=Time.time-start;
        KartKeyboard.TestHeld=k=>elapsed>=3 && elapsed<5 && k==KeyCode.UpArrow;
        float distance=Vector3.Distance(grid,human.transform.position);
        if(elapsed<3) idleTravel=Mathf.Max(idleTravel,distance);
        if(elapsed<6) drivenTravel=Mathf.Max(drivenTravel,distance);
        var progress=rival.GetComponent<TrackProgress>();
        if(elapsed>=nextReport)
        {
            Debug.Log($"DUEL PROGRESS t={elapsed:F0} lap={progress.CompletedLaps} next={progress.NextCheckpoint} pos={rival.transform.position} onRoad={OnRoad(rival.GetComponent<Player>())}");
            nextReport+=15;
        }
        if(!forcedFall && progress.CompletedLaps==1)
        {
            var body=rival.GetComponent<Rigidbody>();
            body.position += Vector3.down*50;
            rival.transform.position=body.position;
            rival.GetComponent<DontGoThroughThings>()?.ResetSweep(body.position);
            forcedFall=true;
        }
        if(elapsed<210 && rival.enabled) return;
        bool pass=!errors && !KartKeyboard.AgentControl && !human.Controls.Autonomous && idleTravel<2 && drivenTravel>5 && gridOK && forcedFall && progress.CompletedLaps==3 && OnRoad(rival.GetComponent<Player>());
        Debug.Log($"DUEL CHECK idle={idleTravel:F2} driven={drivenTravel:F2} checkpoints={progress.CheckpointsPassed} laps={progress.CompletedLaps} t={elapsed:F1} errors={errors} pass={pass}");
        End(pass);
    }
    static bool OnRoad(Player kart)
    {
        return Physics.Raycast(kart.transform.position+Vector3.up*0.5f, Vector3.down, out var hit, 4,
            kart.mask, QueryTriggerInteraction.Ignore) && hit.collider.CompareTag("Ground");
    }

    static void End(bool pass)
    {
        KartKeyboard.TestHeld=null; SessionState.SetBool(Key+"pass",pass); SessionState.SetInt(Key,2); EditorApplication.isPlaying=false;
    }
}



