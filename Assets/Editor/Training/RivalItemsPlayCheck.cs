using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RivalItemsPlayCheck
{
    const string Key="RivalItemsPlayCheck";
    static ItemManager items, humanItems;
    static Player kart;
    static int index=-1, before;
    static float deadline, next;
    static bool failed;
    static Sprite humanSprite;
    static readonly List<string> results=new List<string>();
    static RivalItemsPlayCheck() { EditorApplication.update+=Tick; Application.logMessageReceived+=Log; }
    static void Log(string text,string stack,LogType type)
    { if(SessionState.GetInt(Key,0)==1 && (type==LogType.Exception || type==LogType.Error)) failed=true; }
    public static void Run()
    {
        SessionState.SetInt(Key,1);
        SessionState.SetString(Key+"deadline",DateTime.UtcNow.AddMinutes(4).Ticks.ToString());
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        int stage=SessionState.GetInt(Key,0);
        if(stage==0)return;
        if(stage==2 && !EditorApplication.isPlayingOrWillChangePlaymode)
        { SessionState.SetInt(Key,0);EditorApplication.Exit(SessionState.GetBool(Key+"pass",false)?0:1);return; }
        if(stage!=1)return;
        if(DateTime.UtcNow.Ticks>long.Parse(SessionState.GetString(Key+"deadline","0"))) { End(false);return; }
        if(!EditorApplication.isPlaying)return;
        AudioListener.volume=0;Time.timeScale=5;Time.captureFramerate=60;Application.targetFrameRate=-1;
        if(!RACE_MANAGER.RACE_STARTED)return;
        if(!items)
        {
            var rival=UnityEngine.Object.FindAnyObjectByType<MooMooRival>();
            if(!rival)return;
            kart=rival.GetComponent<Player>();items=rival.GetComponent<ItemManager>();
            humanItems=GameObject.FindGameObjectWithTag("Player").GetComponent<ItemManager>();
            humanSprite=humanItems.your_item.sprite;
            if(items.ItemUI==humanItems.ItemUI || items.your_item==humanItems.your_item) { End(false);return; }
            rival.enabled=false;kart.enabled=false;
            var body=kart.GetComponent<Rigidbody>();body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;
            items.AutoUseItems=true;next=Time.time+1;
        }
        if(Time.time<next)return;
        if(index>=0)
        {
            if(items.AutomaticItemsUsed<=before || kart.hasitem || items.isBullet || items.StarPowerUp)
            { if(Time.time>deadline)End(false);return; }
            if(humanItems.your_item.sprite!=humanSprite) { End(false);return; }
            results.Add($"PASS item {index}: {items.item_gameobjects[index].name}, uses={items.AutomaticItemsUsed-before}");
        }
        index++;
        if(index>=items.item_gameobjects.Length) { End(!failed);return; }
        var counter=kart.GetComponent<LapCounter>();
        var distribution=kart.GetComponent<ItemDistributionManager>();
        bool found=false;
        for(int rank=1;rank<=8 && !found;rank++)
            for(int seed=0;seed<100;seed++)
            {
                counter.Position=rank;UnityEngine.Random.InitState(seed);
                if(distribution.getItemNumber()!=index)continue;
                UnityEngine.Random.InitState(seed);found=true;break;
            }
        if(!found) { End(false);return; }
        before=items.AutomaticItemsUsed;
        kart.hasitem=true;
        typeof(ItemManager).GetField("start_select",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(items,true);
        items.StartCoroutine("Item_Select");
        deadline=Time.time+24;next=Time.time+4.5f;
    }
    static void End(bool pass)
    {
        results.Add($"RIVAL ITEMS CHECK pass={pass}, exceptions={failed}, tested={results.Count}");
        Directory.CreateDirectory("Training/Reports");
        File.WriteAllLines("Training/Reports/rival-items-check.txt",results);
        Debug.Log(results[results.Count-1]);
        SessionState.SetBool(Key+"pass",pass);SessionState.SetInt(Key,2);EditorApplication.isPlaying=false;
    }
}
