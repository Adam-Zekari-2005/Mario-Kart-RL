using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class MigrationPlayAudit
{
    const string Key="MarioKartMigrationAudit";
    static readonly HashSet<string> errors=new HashSet<string>();
    static readonly Dictionary<ComputerDriver,Vector3> bots=new Dictionary<ComputerDriver,Vector3>();
    static double started;
    static float driveStart=-1,peakForward,peakReverse; static Vector3 startPosition;
    static bool driftSeen; static float peakSteering, previousElapsed;
    static bool itemPrepared,itemBoostSeen;
    static MigrationPlayAudit(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    static void Log(string message,string stack,LogType type){if(SessionState.GetBool(Key,false)&&(type==LogType.Exception||type==LogType.Error||type==LogType.Assert))errors.Add(message+"\n"+stack);}
    public static void RunSolo(){Run();SessionState.SetBool(Key+"solo",true);}
    public static void RunSoloFirst(){RunSolo();SessionState.SetInt(Key+"limit",1);}
    public static void RunRecovery(){RunSolo();SessionState.SetInt(Key+"index",2);SessionState.SetInt(Key+"limit",3);}
    public static void RunFirst(){Run();SessionState.SetInt(Key+"limit",1);}
    public static void Run()
    {
        SessionState.SetBool(Key+"solo",false);SessionState.SetInt(Key+"limit",MigrationAudit.Tracks.Length);Directory.CreateDirectory("Migration/Previews");File.WriteAllText("Migration/play-audit.txt","");
        SessionState.SetInt(Key+"index",0);SessionState.SetString(Key+"stage","next");SessionState.SetBool(Key,true);
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        int index=SessionState.GetInt(Key+"index",0);string stage=SessionState.GetString(Key+"stage","");
        if(stage=="next"&&!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if(index==SessionState.GetInt(Key+"limit",MigrationAudit.Tracks.Length)){SessionState.SetBool(Key,false);EditorSceneManager.OpenScene("Assets/Scenes/WhiteScreen.unity");Debug.Log("PLAY AUDIT COMPLETE");EditorApplication.Exit(0);return;}
            EditorSceneManager.OpenScene("Assets/Scenes/"+MigrationAudit.Tracks[index]+".unity");
            SessionState.SetString(Key+"stage","starting");EditorApplication.isPlaying=true;return;
        }
        if(stage=="starting"&&EditorApplication.isPlaying)
        {
            started=EditorApplication.timeSinceStartup;driveStart=-1;peakForward=0;peakReverse=0;peakSteering=0;driftSeen=false;previousElapsed=-1;bots.Clear();foreach(var bot in Object.FindObjectsByType<ComputerDriver>(FindObjectsSortMode.None))bots[bot]=bot.transform.position;
            itemPrepared=false;itemBoostSeen=false;SessionState.SetString(Key+"stage","playing");return;
        }
        if(stage!="playing"||!EditorApplication.isPlaying)return;
        if(SessionState.GetBool(Key+"solo",false) && RACE_MANAGER.RACE_STARTED)
        {
            var human=Object.FindAnyObjectByType<Player>();
            if(driveStart<0){driveStart=Time.time;startPosition=human.transform.position;}
            float elapsed=Time.time-driveStart;
            bool drift=elapsed>=1.9f&&elapsed<2.9f;
            KartKeyboard.TestHeld=key=>(key==KeyCode.UpArrow&&elapsed<3.5f)||(key==KeyCode.DownArrow&&elapsed>=4&&elapsed<8)||(key==KeyCode.RightArrow&&elapsed>=1.7f&&elapsed<2.9f)||(key==KeyCode.Space&&drift)||(key==KeyCode.LeftShift&&elapsed>=9.5f&&elapsed<9.8f);
            if(elapsed>=9&&!itemPrepared)
            {
                var items=human.GetComponent<ItemManager>();
                int mushroom=Array.FindIndex(items.item_gameobjects,x=>x.name=="Mushroom");
                Debug.Assert(mushroom>=0,"Mushroom item exists");
                if(mushroom>=0)
                {
                    items.current_Item="Mushroom";items.item_index=mushroom;human.hasitem=true;
                    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                    typeof(ItemManager).GetField("item_decided",flags).SetValue(items,true);
                    typeof(ItemManager).GetField("start_select",flags).SetValue(items,true);
                }
                itemPrepared=true;
            }
            if(elapsed>=9.5f&&elapsed<11)itemBoostSeen|=human.Boost_time>0&&!human.hasitem;
            KartKeyboard.TestDown=null;
            KartKeyboard.TestUp=null;
            previousElapsed=elapsed;
            peakSteering=Mathf.Max(peakSteering,KartKeyboard.GetAxis("Horizontal"));
            driftSeen|=human.drifting;
            var velocity=human.GetComponent<Rigidbody>().linearVelocity;
            float forward=Vector3.Dot(velocity,human.transform.forward);peakForward=Mathf.Max(peakForward,forward);peakReverse=Mathf.Min(peakReverse,forward);
        }
        if(Time.time<35 && EditorApplication.timeSinceStartup-started<100)return;
        if(SessionState.GetBool(Key+"solo",false))
        {
            KartKeyboard.TestHeld=null;KartKeyboard.TestDown=null;KartKeyboard.TestUp=null;
            var manager=Object.FindAnyObjectByType<RACE_MANAGER>();
            Debug.Log($"CONTROLS {MigrationAudit.Tracks[index]}: forward={peakForward:F2}, reverse={peakReverse:F2}, steering={peakSteering:F2}, driftSeen={driftSeen}");
            Debug.Assert(peakForward>1,"Accelerator moves the human kart");
            Debug.Assert(peakReverse<-.1f,"Brake/reverse moves the human kart backwards");
            Debug.Assert(peakSteering>.9f,"Arrow steering reaches its normalized target");
            Debug.Assert(itemBoostSeen,"Left Shift consumes a mushroom and starts its boost");
            if(index==0)Debug.Assert(driftSeen,"Space activates drift on the starting straight");
            Debug.Assert(Object.FindObjectsByType<ComputerDriver>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0,"No bot instances in solo scene");
            Debug.Assert(Object.FindObjectsByType<Player>(FindObjectsSortMode.None).Length==1,"Exactly one human player");
            Debug.Assert(manager.lapCounters.Count==1,"Exactly one registered racer");
            var human=Object.FindAnyObjectByType<Player>();
            foreach(var clip in human.Driver.GetCurrentAnimatorClipInfo(0))Debug.Log("IDLE CLIP "+clip.clip.name);
            var lap=human.GetComponent<LapCounter>();int checkpoint=lap.currentCheckpointVal,lapNumber=lap.LAPCOUNT,total=lap.totalCheckpointVal;
            human.GetComponent<Rigidbody>().position+=Vector3.up*100;
            Vector3 displaced=human.GetComponent<Rigidbody>().position;
            human.GetComponent<SoloPlayerAssist>().ReturnToTrack();
            Debug.Assert(human.GetComponent<Rigidbody>().linearVelocity.sqrMagnitude<.01f,"Recovery stops velocity");
            Debug.Assert(lap.currentCheckpointVal==checkpoint&&lap.LAPCOUNT==lapNumber&&lap.totalCheckpointVal==total,"Recovery preserves race progression");
            Debug.Assert(Vector3.Distance(human.GetComponent<Rigidbody>().position,displaced)>50,"Recovery leaves the displaced position");
            Debug.Assert(Vector3.Distance(human.transform.position,human.GetComponent<Rigidbody>().position)<.01f,"Recovery synchronizes the visible kart and physics");
            manager.resultsUI.createResults(manager.lapCounters);
            Debug.Assert(manager.resultsUI.transform.GetChild(0).gameObject.activeSelf,"Solo result displayed");
            for(int row=1;row<manager.resultsUI.transform.childCount;row++)Debug.Assert(!manager.resultsUI.transform.GetChild(row).gameObject.activeSelf,"Unused result row hidden");
        }
        int moved=0;foreach(var bot in bots)if(bot.Key!=null&&Vector3.Distance(bot.Key.transform.position,bot.Value)>5)moved++;
        string track=MigrationAudit.Tracks[index];
        string line=$"PLAY {track}: simulatedSeconds={Time.time:F1}, raceStarted={RACE_MANAGER.RACE_STARTED}, botsMoved={moved}/{bots.Count}, errors={errors.Count}";
        File.AppendAllText("Migration/play-audit.txt",line+"\n"+string.Join("\n",errors)+"\n");Debug.Log(line);
        var race=Object.FindAnyObjectByType<RACE_MANAGER>();var camera=race!=null&&race.FrontCam!=null?race.FrontCam.GetComponent<Camera>():Camera.main;
        if(camera!=null)
        {
            var player=Object.FindAnyObjectByType<Player>();
            camera.transform.position=player.transform.position-player.transform.forward*8+player.transform.up*4;
            camera.transform.LookAt(player.transform.position+player.transform.forward*8+player.transform.up);
            MigrationAudit.Capture(camera,"Migration/Previews/Play-"+track+".png");
        }
        SessionState.SetInt(Key+"index",index+1);SessionState.SetString(Key+"stage","next");EditorApplication.isPlaying=false;
    }
}




