using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(KartEpisode))]
public sealed class KartRunRecorder : MonoBehaviour
{
    KartEpisode episode;
    TrackProgress progress;
    readonly StringBuilder crossings = new StringBuilder();
    void Start()
    {
        episode = GetComponent<KartEpisode>();
        progress = GetComponent<TrackProgress>();
        episode.EpisodeFinished += Record;
        progress.CheckpointPassed += Checkpoint;
    }
    void Checkpoint(int index)
    {
        if (crossings.Length > 0) crossings.Append(';');
        crossings.Append(index).Append('@').Append(episode.ElapsedSeconds.ToString("F3", CultureInfo.InvariantCulture));
    }
    static string Cell(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    void Record(KartEpisode result)
    {
        string folder = KartRunSettings.Argument("kart-output", "Training/Results");
        string runFile = KartRunSettings.Run;
        foreach (char c in Path.GetInvalidFileNameChars()) runFile = runFile.Replace(c, '_');
        string path = Path.Combine(folder, "episodes-" + runFile + ".csv");
        try
        {
            Directory.CreateDirectory(folder);
            if (!File.Exists(path)) File.WriteAllText(path,
                "utc,run,mode,track,seed,episode,reason,score,seconds,checkpoints,laps,collisions,grass_seconds,collision_cost,grass_cost,time_cost,crossings,profile,checkpoint_goal\n");
            string F(float value) => value.ToString("F5", CultureInfo.InvariantCulture);
            string line = string.Join(",", Cell(DateTime.UtcNow.ToString("O")), Cell(KartRunSettings.Run),
                Cell(KartRunSettings.Mode), Cell(SceneManager.GetActiveScene().name), KartRunSettings.Seed,
                KartRunSettings.Episodes + 1, Cell(result.LastReason), F(result.Score), F(result.ElapsedSeconds),
                progress.CheckpointsPassed, progress.CompletedLaps, result.Collisions, F(result.GrassSeconds),
                F(result.CollisionCost), F(result.GrassCost), F(result.TimeCost), Cell(crossings.ToString()), Cell(KartLearningProfile.Profile), result.checkpointGoal);
            File.AppendAllText(path, line + "\n");
        }
        catch (Exception error) { Debug.LogError("Impossible d'enregistrer les resultats : " + error.Message); }
        KartRunSettings.Episodes++;
        UnityEngine.Random.InitState(KartRunSettings.Seed);
        if (KartRunSettings.Limit > 0 && KartRunSettings.Episodes >= KartRunSettings.Limit)
        {
            KartRunSettings.StopRequested = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
    void OnDestroy()
    {
        if (episode) episode.EpisodeFinished -= Record;
        if (progress) progress.CheckpointPassed -= Checkpoint;
    }
}
