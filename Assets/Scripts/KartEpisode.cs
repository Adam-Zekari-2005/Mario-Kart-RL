using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Keyboard learning environment. Scene reload gives a complete, reproducible reset.</summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(TrackProgress), typeof(Player))]
public sealed class KartEpisode : MonoBehaviour
{
    public event System.Action<float> RewardChanged;
    public event System.Action<KartEpisode> EpisodeFinished;
    public bool IsEnding => ending;
    [Min(0)] public int checkpointGoal;
    public float checkpointGoalBonus = 10f;
    [Min(0)] public float rewardPerMetre = 0.01f;
    public float lapBonus = 100;
    public float failurePenalty = -10;
    [Min(1)] public float stuckSeconds = 15;
    [Min(1)] public float maximumSeconds = 300;
    [Min(0)] public float timePenaltyPerSecond = 0.2f;
    [Min(0)] public float grassPenaltyPerSecond = 1f;
    [Min(0)] public float collisionPenalty = 2f;
    [Min(0)] public float minimumImpactSpeed = 2f;
    [Min(0)] public float collisionCooldown = 0.75f;
    public int Collisions { get; private set; }
    public float GrassSeconds { get; private set; }
    public float CollisionCost { get; private set; }
    public float GrassCost { get; private set; }
    public float TimeCost { get; private set; }
    public float Score { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public string LastReason { get; private set; }
    TrackProgress progress;
    OutOfBounds recovery;
    float bestProgress, noProgressTime;
    int rewardedLaps;
    bool ending;
    float nextCollisionTime;
    static string previousResult = "Aucun essai termine";
    static int episodeNumber;

    void Start()
    {
        progress = GetComponent<TrackProgress>();
        recovery = GetComponent<OutOfBounds>();
        var assist = GetComponent<SoloPlayerAssist>();
        if (assist) assist.enabled = false;
        episodeNumber++;
    }

    void LateUpdate()
    {
        if (ending || !progress.IsReady || !RACE_MANAGER.RACE_STARTED) return;
        if (Input.GetKeyDown(KeyCode.F5)) { EndEpisode("recommence manuellement", false); return; }
        if (recovery && (recovery.FellInWater || recovery.outOfBounds || recovery.PlayerBeingMoved))
        { EndEpisode("sortie de piste", true); return; }
        progress.Sample();
        float advance = Mathf.Max(0, progress.ProgressMetres - bestProgress);
        bestProgress = Mathf.Max(bestProgress, progress.ProgressMetres);
        float timeCost = timePenaltyPerSecond * Time.deltaTime;
        TimeCost += timeCost;
        AddScore(advance * rewardPerMetre - timeCost);
        if (IsOnGrass())
        {
            GrassSeconds += Time.deltaTime;
            float grassCost = grassPenaltyPerSecond * Time.deltaTime;
            GrassCost += grassCost;
            AddScore(-grassCost);
        }
        noProgressTime = advance > 0.01f ? 0 : noProgressTime + Time.deltaTime;
        ElapsedSeconds += Time.deltaTime;
        if (progress.CompletedLaps > rewardedLaps)
        {
            AddScore((progress.CompletedLaps - rewardedLaps) * lapBonus);
            rewardedLaps = progress.CompletedLaps;
            EndEpisode("tour termine", false);
        }
        else if (checkpointGoal > 0 && progress.CheckpointsPassed >= checkpointGoal)
        {
            AddScore(checkpointGoalBonus);
            EndEpisode("objectif checkpoints atteint", false);
        }
        else if (noProgressTime >= stuckSeconds) EndEpisode("sans progression", true);
        else if (ElapsedSeconds >= maximumSeconds) EndEpisode("temps ecoule", true);
        else if (RACE_MANAGER.RACE_COMPLETED) EndEpisode("course terminee", false);
    }

    public void EndEpisode(string reason, bool failed)
    {
        if (ending) return;
        ending = true;
        if (failed) AddScore(failurePenalty);
        LastReason = reason;
        previousResult = $"Essai {episodeNumber} : {reason} | score {Score:F2} | {ElapsedSeconds:F1} s | {progress.CheckpointsPassed} checkpoints | {progress.CompletedLaps} tour(s)\n" +
            $"Chocs : {Collisions} (-{CollisionCost:F2}) | Gazon : {GrassSeconds:F1} s (-{GrassCost:F2}) | Temps : -{TimeCost:F2}";
        Debug.Log("[KartEpisode] " + previousResult, this);
        EpisodeFinished?.Invoke(this);
        if (KartRunSettings.StopRequested) return;
        // Reload all kart, item, physics, animation, race and checkpoint state together.
        RACE_MANAGER.RACE_STARTED = false;
        RACE_MANAGER.RACE_COMPLETED = false;
        RACE_MANAGER.raceFinishStuff = false;
        RACE_MANAGER.countDownTime = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnGUI()
    {
        GUI.Box(new Rect(12, 175, 850, 165), "Environnement d'apprentissage - essai " + episodeNumber);
        GUI.Label(new Rect(24, 202, 825, 135),
            $"Score : {Score:F2} | Temps : {ElapsedSeconds:F1} s | Checkpoints : {(progress ? progress.CheckpointsPassed : 0)}" +
            (checkpointGoal > 0 ? $" / {checkpointGoal} (objectif partiel)\n" : "\n") +
            $"Progression maximale : {bestProgress:F1} unites | F5 : nouvel essai\n" +
            $"Chocs : {Collisions} (-{CollisionCost:F2}) | Gazon : {GrassSeconds:F1} s (-{GrassCost:F2}) | Temps : -{TimeCost:F2}\n" + previousResult);
    }

    public bool IsOnGrass() => IsOnGrass(transform);

    public static bool IsOnGrass(Transform transform)
    {
        // Use the closest surface under the kart, ignoring its own colliders and triggers.
        // No surface during a jump means no grass penalty.
        float nearest = float.PositiveInfinity;
        Collider surface = null;
        foreach (var hit in Physics.RaycastAll(transform.position + transform.up * 0.5f,
                     -transform.up, 3f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
            nearest = hit.distance;
            surface = hit.collider;
        }
        return surface && surface.CompareTag("Dirt");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (ending || !RACE_MANAGER.RACE_STARTED || RACE_MANAGER.RACE_COMPLETED || Time.time < nextCollisionTime) return;
        foreach (var contact in collision.contacts)
        {
            // Horizontal ground/landing contacts are not obstacle impacts. A wall may
            // share a mesh or tag with the road, so classify by contact normal.
            if (Mathf.Abs(Vector3.Dot(contact.normal, transform.up)) > 0.5f) continue;
            float impact = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal));
            if (impact < minimumImpactSpeed) continue;
            Collisions++;
            CollisionCost += collisionPenalty;
            AddScore(-collisionPenalty);
            nextCollisionTime = Time.time + collisionCooldown;
            break;
        }
    }

    void AddScore(float delta)
    {
        Score += delta;
        RewardChanged?.Invoke(delta);
    }
}

