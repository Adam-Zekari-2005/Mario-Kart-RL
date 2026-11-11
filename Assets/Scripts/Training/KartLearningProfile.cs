using System.Collections;
using UnityEngine;

/// <summary>Opt-in settings for the learning scenes; manual race settings stay unchanged.</summary>
public sealed class KartLearningProfile : MonoBehaviour
{
    public static string Profile => KartRunSettings.Argument("kart-profile", "original");
    void Awake()
    {
        if (Profile != "balanced") return;
        var episode = GetComponent<KartEpisode>();
        episode.rewardPerMetre = 0.05f;
        episode.collisionPenalty = 0.25f;
        episode.grassPenaltyPerSecond = 0.2f;
        episode.timePenaltyPerSecond = 0.02f;
        episode.failurePenalty = -2f;
        episode.stuckSeconds = 5f;
        episode.maximumSeconds = 90f;
        episode.checkpointGoal = int.TryParse(KartRunSettings.Argument("kart-goal", "6"), out int goal) ? Mathf.Max(0, goal) : 6;
        episode.checkpointGoalBonus = 10f;
    }
    IEnumerator Start()
    {
        if (Profile != "balanced") yield break;
        // Let the race/player/camera Start methods initialize before bypassing only
        // the opening cinematic. This does not teleport or guide the kart.
        yield return null;
        var race = Object.FindAnyObjectByType<RACE_MANAGER>();
        if (!race) yield break;
        race.StopAllCoroutines();
        if (Application.isBatchMode)
        {
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            AudioListener.volume = 0;
            if (KartRunSettings.Mode == "model")
            {
                Time.timeScale = 1;
                Time.captureFramerate = 60;
            }
        }
        var entry = GameObject.Find("SceneEntryCamera");
        if (entry) entry.SetActive(false);
        var fade = GameObject.Find("FadeInOut");
        if (fade) fade.SetActive(false);
        race.enableAllSets();
        race.CourseNameUI.SetActive(false);
        race.CountDownTimer.SetActive(false);
        race.CoinCounter.SetActive(true);
        race.LapCounter.SetActive(true);
        race.MiniMap.SetActive(true);
        race.BackCam.SetActive(false);
        race.FrontFPCam.SetActive(false);
        // The imported camera schedules its own deactivation after 0.2 seconds
        // for the normal cinematic. Cancel that schedule when skipping the intro.
        var cameraUtility = race.FrontCam.GetComponent<UtilityFunctions>();
        if (cameraUtility) cameraUtility.StopAllCoroutines();
        race.FrontCam.SetActive(true);
        var animator = race.FrontCam.GetComponent<Animator>();
        if (animator) animator.enabled = false;
        race.FrontCam.GetComponent<Camera>().enabled = true;
        GetComponent<PlayerSounds>().SceneEntryFinished = true;
        RACE_MANAGER.RACE_COMPLETED = false;
        RACE_MANAGER.RACE_STARTED = true;
        if (race.trolleySystem) race.trolleySystem.InstantiateFirstTram();
    }
}
