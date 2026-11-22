using Unity.InferenceEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Player), typeof(TrackProgress))]
public sealed class MooMooRival : Agent
{
    const string RaceScene = "Assets/Scenes/MooMooMeadows.unity";
    Player kart;
    TrackProgress progress;
    Rigidbody body;
    LapCounter laps;
    int step, raceLaps;
    Vector3 safePosition;
    Quaternion safeRotation;
    float offTrackTime;
    DontGoThroughThings sweep;
    KartEpisode learningEpisode;
    float stuckTime, furthestProgress;
    public int RecoveryCount { get; private set; }
    public static bool LearningSession => KartRunSettings.Argument("kart-track", "moo") == "duel" &&
        (KartRunSettings.Mode == "train" || KartRunSettings.Mode == "evaluate");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.path != RaceScene) return;
        var human = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        // Reward collection and episode resets belong to the separate training scenes.
        var humanEpisode = human.GetComponent<KartEpisode>();
        if (humanEpisode) { humanEpisode.enabled = false; Destroy(humanEpisode); }
        var model = Resources.Load<ModelAsset>("Kart");
        if (!model) { Debug.LogError("Modèle du rival introuvable : Resources/Kart.onnx"); return; }

        // Centre of the rear white grid bracket in the course mesh's local coordinates.
        var grid = GameObject.Find("gate_WhiteLine18__mm_StartGrad").transform;
        var slot = grid.TransformPoint(new Vector3(-53.7761f, 14.8467f, 5.8137f));
        slot -= human.transform.forward * 1.9f; // Align the nose with the painted stripe.
        slot.y = human.transform.position.y;
        var rival = Instantiate(human, slot, human.transform.rotation);
        rival.name = "Luigi - rival PPO";
        rival.tag = "Untagged";
        var episode = rival.GetComponent<KartEpisode>();
        if (LearningSession)
        {
            if (!episode) episode = rival.gameObject.AddComponent<KartEpisode>();
            episode.enabled = true;
            episode.targetLaps = 3;
            episode.checkpointGoal = 0;
            episode.rewardPerMetre = .05f;
            episode.collisionPenalty = 2f;
            episode.grassPenaltyPerSecond = 1f;
            episode.rewardProgressOnGrass = false;
            episode.timePenaltyPerSecond = .05f;
            episode.failurePenalty = -5;
            episode.stuckSeconds = 6;
            episode.maximumSeconds = 180;
            rival.gameObject.AddComponent<KartRunRecorder>();
            rival.gameObject.AddComponent<KartLearningProfile>();
            // Train the actual opponent from its own grid position, including
            // small variations; half of the episodes keep the exact race start.
            if (KartRunSettings.Mode == "train" && UnityEngine.Random.value < .5f)
            {
                rival.transform.position += rival.transform.right * UnityEngine.Random.Range(-2f, 2f);
                rival.transform.Rotate(0, UnityEngine.Random.Range(-8f, 8f), 0);
            }
        }
        else if (episode) { episode.enabled = false; Destroy(episode); }
        var assist = rival.GetComponent<SoloPlayerAssist>();
        if (assist) assist.enabled = false;
        foreach (var camera in rival.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        foreach (var listener in rival.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
        Resources.Load<DuelDriverAppearance>("DuelLuigi").Apply(rival);
        rival.gameObject.AddComponent<RivalRaceTactics>();
        Physics.SyncTransforms();
        var parameters = rival.gameObject.AddComponent<BehaviorParameters>();
        parameters.BehaviorName = "Kart";
        parameters.BrainParameters.VectorObservationSize = KartAgent.ObservationCount;
        parameters.BrainParameters.ActionSpec = new ActionSpec(2, new[] { 2 });
        parameters.Model = LearningSession ? null : model;
        parameters.InferenceDevice = InferenceDevice.Burst;
        parameters.DeterministicInference = true;
        parameters.BehaviorType = LearningSession ? BehaviorType.Default : BehaviorType.InferenceOnly;
        rival.gameObject.AddComponent<MooMooRival>();
        var race = Object.FindAnyObjectByType<RACE_MANAGER>();
        race.lapCounters.Add(rival.GetComponent<LapCounter>());
    }

    public override void Initialize()
    {
        kart = GetComponent<Player>();
        progress = GetComponent<TrackProgress>();
        body = GetComponent<Rigidbody>();
        laps = GetComponent<LapCounter>();
        raceLaps = Object.FindAnyObjectByType<RACE_MANAGER>().MAXLAPS;
        kart.Controls.SetAutonomous(true);
        safePosition = body.position;
        safeRotation = body.rotation;
        sweep = GetComponent<DontGoThroughThings>();
        if (LearningSession)
        {
            learningEpisode = GetComponent<KartEpisode>();
            learningEpisode.RewardChanged += Reward;
            learningEpisode.EpisodeFinished += FinishLearning;
        }
    }

    void Reward(float value) => AddReward(value);
    void FinishLearning(KartEpisode result)
    {
        kart.Controls.SetRaceActions(0, 0, false);
        Academy.Instance.StatsRecorder.Add("Duel/Laps", progress.CompletedLaps);
        Academy.Instance.StatsRecorder.Add("Duel/Checkpoints", progress.CheckpointsPassed);
        enabled = false;
    }
    protected override void OnDisable()
    {
        if (learningEpisode)
        {
            learningEpisode.RewardChanged -= Reward;
            learningEpisode.EpisodeFinished -= FinishLearning;
        }
        base.OnDisable();
    }

    void FixedUpdate()
    {
        if (!progress.IsReady || !RACE_MANAGER.RACE_STARTED) return;
        progress.Sample();
        if (progress.ProgressMetres > furthestProgress + .1f)
        { furthestProgress = progress.ProgressMetres; stuckTime = 0; }
        else stuckTime += Time.fixedDeltaTime;
        if (LearningSession && !Academy.Instance.IsCommunicatorOn)
        {
            Debug.LogError("Duel learning requires the Python trainer.");
            Application.Quit(2);
            return;
        }
        if (!LearningSession && (progress.CompletedLaps >= raceLaps || RACE_MANAGER.RACE_COMPLETED))
        {
            kart.Controls.SetRaceActions(0, 0, false);
            GetComponent<ItemManager>().AutoUseItems = false;
            kart.enabled = false;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            enabled = false;
            return;
        }
        if (kart.GLIDER_FLY || kart.JUMP_PANEL || GetComponent<ItemManager>().isBullet)
        {
            offTrackTime = 0;
        }
        else if (Physics.Raycast(body.position + Vector3.up * 0.5f, Vector3.down, out var ground, 4f,
            kart.mask, QueryTriggerInteraction.Ignore) && ground.normal.y > 0.4f)
        {
            offTrackTime = 0;
            Vector3 toCheckpoint = laps.checkpoints.GetChild(progress.NextCheckpoint).position - body.position;
            if (ground.collider.CompareTag("Ground") && progress.ForwardSpeed > 1 && step % 25 == 0 &&
                stuckTime < .2f && Vector3.Dot(toCheckpoint, progress.TrackDirection) > 3 &&
                Vector3.Dot(transform.forward, progress.TrackDirection) > .4f)
            {
                safePosition = ground.point + Vector3.up * 0.7f;
                safeRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, ground.normal).normalized, ground.normal);
            }
        }
        else if (KartEpisode.IsOnGrass(transform))
        {
            offTrackTime = 0;
        }
        else if ((offTrackTime += Time.fixedDeltaTime) > 2.5f)
        {
            if (learningEpisode) { learningEpisode.EndEpisode("sortie piste", true); return; }
            if (KartRunSettings.Argument("duel-rescue", "1") != "0") Recover();
        }
        // A collision on the grass or against a wall must not freeze the rival
        // for the rest of the race. This is explicit recovery, not a learned action.
        if (!learningEpisode && stuckTime > 8 && KartRunSettings.Argument("duel-rescue", "1") != "0") Recover();
        if (step++ % 5 == 0) RequestDecision();
        else RequestAction();
    }

    void Recover()
    {
        RecoveryCount++;
        body.isKinematic = false;
        body.linearVelocity = body.angularVelocity = Vector3.zero;
        body.position = safePosition;
        body.rotation = safeRotation;
        transform.SetPositionAndRotation(safePosition, safeRotation);
        if (sweep) sweep.ResetSweep(safePosition);
        var bounds = GetComponent<OutOfBounds>();
        if (bounds) bounds.FellInWater = bounds.outOfBounds = bounds.PlayerBeingMoved = false;
        kart.currentspeed = kart.Boost_time = 0;
        kart.stopDrift();
        kart.Controls.SetAutonomous(true);
        kart.Controls.SetRaceActions(0, 0, false);
        offTrackTime = stuckTime = 0;
        Physics.SyncTransforms();
        Debug.Log("PPO rival recovered after leaving the track or becoming stuck.");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // ML-Agents requests a final observation during scene teardown, when
        // checkpoint children may already have been destroyed.
        if (!isActiveAndEnabled)
        {
            for (int i = 0; i < 31; i++) sensor.AddObservation(0f);
            return;
        }
        KartAgent.ObserveKart(sensor, kart, progress, body, laps.checkpoints);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!RACE_MANAGER.RACE_STARTED || RACE_MANAGER.RACE_COMPLETED) return;
        float throttle = KartAgent.ForwardThrottle(actions.ContinuousActions[0]);
        var tactics = GetComponent<RivalRaceTactics>();
        if (tactics) throttle = tactics.AdjustThrottle(throttle);
        kart.Controls.SetRaceActions(actions.ContinuousActions[1], throttle, actions.DiscreteActions[0] == 1);
    }
}








