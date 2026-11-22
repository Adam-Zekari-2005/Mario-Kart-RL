using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opt-in measurement of the actual race opponent, not the solo training agent.
public sealed class DuelBenchmark : MonoBehaviour
{
    MooMooRival rival;
    TrackProgress progress;
    float elapsed, idle, best, grass;
    float earlyGrass;
    readonly float[] grassByCheckpoint = new float[21];
    int collisions;
    bool finished;
    float nextImpact;
    Player human;
    static int trial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        trial = 0;
        if (KartRunSettings.Argument("duel-test", "0") != "1") return;
        SceneManager.sceneLoaded += Loaded;
    }
    static void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MooMooMeadows") new GameObject("Duel measurement").AddComponent<DuelBenchmark>();
    }
    IEnumerator Start()
    {
        yield return null;
        rival = FindAnyObjectByType<MooMooRival>();
        if (!rival) throw new InvalidOperationException("No PPO rival in the real race scene.");
        progress = rival.GetComponent<TrackProgress>();
        human = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        if (KartRunSettings.Argument("duel-human", "idle") == "launch") human.Controls.SetAutonomous(true);
        rival.gameObject.AddComponent<DuelImpactCounter>().owner = this;
        int fps = int.Parse(KartRunSettings.Argument("duel-fps", "60"));
        Time.timeScale = 1;
        Time.captureFramerate = fps;
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
        AudioListener.volume = 0;
        var race = FindAnyObjectByType<RACE_MANAGER>();
        race.StopAllCoroutines();
        GameObject.Find("SceneEntryCamera")?.SetActive(false);
        GameObject.Find("FadeInOut")?.SetActive(false);
        race.enableAllSets();
        race.CourseNameUI.SetActive(false);
        race.CountDownTimer.SetActive(false);
        race.BackCam.SetActive(false);
        race.FrontFPCam.SetActive(false);
        race.FrontCam.GetComponent<UtilityFunctions>()?.StopAllCoroutines();
        race.FrontCam.SetActive(true);
        race.FrontCam.GetComponent<Animator>().enabled = false;
        race.FrontCam.GetComponent<Camera>().enabled = true;
        foreach (var player in FindObjectsByType<Player>(FindObjectsSortMode.None))
            player.GetComponent<PlayerSounds>().SceneEntryFinished = true;
        RACE_MANAGER.RACE_COMPLETED = false;
        RACE_MANAGER.RACE_STARTED = true;
    }
    void Update()
    {
        if (!rival || !progress.IsReady || finished || !RACE_MANAGER.RACE_STARTED) return;
        elapsed += Time.deltaTime;
        if (human.Controls.Autonomous) human.Controls.SetRaceActions(0, elapsed < 5 ? 1 : 0, false);
        progress.Sample();
        if (progress.ProgressMetres > best + .1f) { best = progress.ProgressMetres; idle = 0; }
        else idle += Time.deltaTime;
        if (KartEpisode.IsOnGrass(rival.transform))
        {
            grass += Time.deltaTime;
            if (progress.NextCheckpoint < grassByCheckpoint.Length) grassByCheckpoint[progress.NextCheckpoint] += Time.deltaTime;
            if (progress.NextCheckpoint <= 4) earlyGrass += Time.deltaTime;
        }
        if (progress.CompletedLaps >= 3 || idle > 15 || elapsed > 240)
            Finish(progress.CompletedLaps >= 3 ? "finished" : idle > 15 ? "stuck" : "timeout");
    }
    void Finish(string reason)
    {
        finished = true;
        string path = KartRunSettings.Argument("duel-output", "Training/Results/duel.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var items = rival.GetComponent<ItemManager>();
        var tactics = rival.GetComponent<RivalRaceTactics>();
        if (!File.Exists(path)) File.WriteAllText(path, "trial,seed,fps,reason,laps,checkpoints,seconds,collisions,grass_seconds,recoveries,human,early_grass_seconds,items_collected,items_used,ramps,boosts,planned_bonuses,cruise_requests,tricks,grass_by_checkpoint\n");
        File.AppendAllText(path, string.Join(",", trial, KartRunSettings.BaseSeed + trial,
            Time.captureFramerate, reason, progress.CompletedLaps, progress.CheckpointsPassed,
            elapsed.ToString("F3", CultureInfo.InvariantCulture), collisions,
            grass.ToString("F3", CultureInfo.InvariantCulture), rival.RecoveryCount,
            KartRunSettings.Argument("duel-human", "idle"), earlyGrass.ToString("F3", CultureInfo.InvariantCulture),
            items.ItemsCollected, items.AutomaticItemsUsed, tactics ? tactics.RampPasses : 0,
            tactics ? tactics.BoostPasses : 0, tactics ? tactics.PlannedBonuses : 0,
            tactics ? tactics.CruiseRequests : 0, tactics ? tactics.TricksPerformed : 0,
            string.Join(";", System.Array.ConvertAll(grassByCheckpoint, x => x.ToString("F2", CultureInfo.InvariantCulture)))) + "\n");
        Debug.Log($"DUEL BENCHMARK {trial}: {reason}, laps={progress.CompletedLaps}, cp={progress.CheckpointsPassed}, time={elapsed:F1}");
        trial++;
        if (trial >= int.Parse(KartRunSettings.Argument("duel-trials", "5"))) Application.Quit();
        else
        {
            RACE_MANAGER.RACE_STARTED = RACE_MANAGER.RACE_COMPLETED = false;
            UnityEngine.Random.InitState(KartRunSettings.BaseSeed + trial);
            SceneManager.LoadScene("MooMooMeadows");
        }
    }
    public void Impact(Collision hit)
    {
        if (!RACE_MANAGER.RACE_STARTED || elapsed < nextImpact) return;
        foreach (var contact in hit.contacts)
            if (Mathf.Abs(Vector3.Dot(contact.normal, rival.transform.up)) < .5f && Mathf.Abs(Vector3.Dot(hit.relativeVelocity, contact.normal)) >= 2)
            {
                collisions++; nextImpact = elapsed + .75f;
                Debug.Log($"DUEL IMPACT cp={progress.NextCheckpoint}, obstacle={hit.collider.name}, tag={hit.collider.tag}");
                break;
            }
    }
}

public sealed class DuelImpactCounter : MonoBehaviour
{
    public DuelBenchmark owner;
    void OnCollisionEnter(Collision collision) => owner.Impact(collision);
}
