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
        if (episode) { episode.enabled = false; Destroy(episode); }
        var assist = rival.GetComponent<SoloPlayerAssist>();
        if (assist) assist.enabled = false;
        foreach (var camera in rival.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        foreach (var listener in rival.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
        Resources.Load<DuelDriverAppearance>("DuelLuigi").Apply(rival);
        Physics.SyncTransforms();
        var parameters = rival.gameObject.AddComponent<BehaviorParameters>();
        parameters.BehaviorName = "Kart";
        parameters.BrainParameters.VectorObservationSize = KartAgent.ObservationCount;
        parameters.BrainParameters.ActionSpec = new ActionSpec(2, new[] { 2 });
        parameters.Model = model;
        parameters.InferenceDevice = InferenceDevice.Burst;
        parameters.DeterministicInference = true;
        parameters.BehaviorType = BehaviorType.InferenceOnly;
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
    }

    void FixedUpdate()
    {
        if (!progress.IsReady || !RACE_MANAGER.RACE_STARTED) return;
        if (progress.CompletedLaps >= raceLaps || RACE_MANAGER.RACE_COMPLETED)
        {
            kart.Controls.SetRaceActions(0, 0, false);
            kart.enabled = false;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            enabled = false;
            return;
        }
        if (Physics.Raycast(body.position + Vector3.up * 0.5f, Vector3.down, out var ground, 4f,
            kart.mask, QueryTriggerInteraction.Ignore) && ground.normal.y > 0.4f)
        {
            offTrackTime = 0;
            if (ground.collider.CompareTag("Ground") && progress.ForwardSpeed > 1 && step % 25 == 0)
            {
                safePosition = ground.point + Vector3.up * 0.7f;
                safeRotation = Quaternion.LookRotation(progress.TrackDirection, Vector3.up);
            }
        }
        else if (KartEpisode.IsOnGrass(transform))
        {
            offTrackTime = 0;
        }
        else if ((offTrackTime += Time.fixedDeltaTime) > 2.5f)
        {
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.position = safePosition;
            body.rotation = safeRotation;
            transform.SetPositionAndRotation(safePosition, safeRotation);
            if (sweep) sweep.ResetSweep(safePosition);
            kart.currentspeed = kart.Boost_time = 0;
            kart.stopDrift();
            offTrackTime = 0;
        }
        if (step++ % 5 == 0) RequestDecision();
        else RequestAction();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        KartAgent.ObserveKart(sensor, kart, progress, body, laps.checkpoints);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!RACE_MANAGER.RACE_STARTED || RACE_MANAGER.RACE_COMPLETED) return;
        kart.Controls.SetRaceActions(actions.ContinuousActions[1], KartAgent.ForwardThrottle(actions.ContinuousActions[0]), actions.DiscreteActions[0] == 1);
    }
}








