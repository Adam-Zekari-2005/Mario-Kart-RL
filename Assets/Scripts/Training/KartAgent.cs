using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using Unity.InferenceEngine;

[RequireComponent(typeof(KartEpisode), typeof(KartRunRecorder))]
public sealed class KartAgent : Agent
{
    public const int ObservationCount = 31;
    KartEpisode episode;
    TrackProgress progress;
    Rigidbody body;
    Player player;
    Transform points;
    bool finished;
    int decisions;
    System.Random randomPolicy;
    static readonly float[] angles = { -90, -60, -40, -20, 0, 20, 40, 60, 90 };

    protected override void OnEnable()
    {
        var behavior = GetComponent<BehaviorParameters>();
        behavior.InferenceDevice = InferenceDevice.Burst;
        behavior.DeterministicInference = true;
        if (KartRunSettings.Mode == "random" || KartRunSettings.Mode == "keyboard")
            behavior.BehaviorType = BehaviorType.HeuristicOnly;
        else if (KartRunSettings.Mode == "model")
        {
            var model = Resources.Load<ModelAsset>("Kart");
            if (!model) throw new System.InvalidOperationException("Modele absent : importer Kart.onnx dans Assets/Resources avant de compiler.");
            behavior.Model = model;
            behavior.BehaviorType = BehaviorType.InferenceOnly;
        }
        base.OnEnable();
    }

    public override void Initialize()
    {
        Application.runInBackground = true;
        episode = GetComponent<KartEpisode>();
        if (!GetComponent<KartLearningProfile>()) gameObject.AddComponent<KartLearningProfile>();
        progress = GetComponent<TrackProgress>();
        body = GetComponent<Rigidbody>();
        player = GetComponent<Player>();
        player.Controls.SetAutonomous(true);
        points = GetComponent<LapCounter>().checkpoints;
        randomPolicy = new System.Random(KartRunSettings.Seed);
        episode.RewardChanged += ReceiveReward;
        episode.EpisodeFinished += Finish;
        player.Controls.SetRaceActions(0, 0, false);
    }
    void ReceiveReward(float delta) { if (!finished) AddReward(delta); }
    void Finish(KartEpisode result)
    {
        finished = true;
        player.Controls.SetRaceActions(0, 0, false);
        Academy.Instance.StatsRecorder.Add("Kart/Checkpoints", progress.CheckpointsPassed);
        Academy.Instance.StatsRecorder.Add("Kart/LapCompleted", progress.CompletedLaps > 0 ? 1 : 0);
        Academy.Instance.StatsRecorder.Add("Kart/Collisions", result.Collisions);
        Academy.Instance.StatsRecorder.Add("Kart/GoalReached", result.LastReason == "objectif checkpoints atteint" ? 1 : 0);
        // The scene is reloaded by KartEpisode. Disabling sends exactly one
        // terminal transition through Agent.OnDisable, without creating an
        // empty second episode immediately before destroying this agent.
        enabled = false;
    }
    void FixedUpdate()
    {
        if (finished || !progress.IsReady || !RACE_MANAGER.RACE_STARTED) return;
        if ((KartRunSettings.Mode == "train" || KartRunSettings.Mode == "evaluate") && !Academy.Instance.IsCommunicatorOn)
        {
            finished = true;
            player.Controls.SetRaceActions(0, 0, false);
            Debug.LogError("Python n'est pas connecte : essai arrete pour eviter une fausse session d'apprentissage.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(2);
#endif
            return;
        }
        // 10 decisions/s at the project's 50 physics steps/s; hold between decisions.
        if (decisions++ % 5 == 0) { KartRunSettings.TotalDecisions++; RequestDecision(); }
        else RequestAction();
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        ObserveKart(sensor, player, progress, body, points);
    }

    // Both the training agent and the race opponent encode the same inputs.
    public static void ObserveKart(VectorSensor sensor, Player player, TrackProgress progress, Rigidbody body, Transform points)
    {
        var transform = player.transform;
        progress.Sample();
        sensor.AddObservation(Vector3.ClampMagnitude(transform.InverseTransformDirection(body.linearVelocity) / 100f, 1));
        var items = player.GetComponent<ItemManager>();
        var route = items ? items.path : null;
        var tactics = player.GetComponent<RivalRaceTactics>();
        if (tactics && tactics.enabled && tactics.NavigationPath) route = tactics.NavigationPath;
        Vector3 roadDirection = KartRunSettings.Argument("duel-navigation", "1") == "0" ? progress.TrackDirection :
            PathDirection(transform.position, body.linearVelocity.magnitude, route, progress.TrackDirection);
        sensor.AddObservation(transform.InverseTransformDirection(roadDirection));
        var target = points.GetChild(progress.NextCheckpoint);
        sensor.AddObservation(Vector3.ClampMagnitude(transform.InverseTransformPoint(target.position) / 100f, 1));
        sensor.AddObservation(Mathf.Clamp(progress.ForwardSpeed / 100f, -1, 1));
        sensor.AddObservation(KartEpisode.IsOnGrass(transform));
        sensor.AddObservation(player.grounded);
        Vector3 nextDirection = (points.GetChild((progress.NextCheckpoint + 1) % points.childCount).position - target.position).normalized;
        sensor.AddObservation(transform.InverseTransformDirection(nextDirection));
        foreach (float angle in angles)
        {
            Vector3 direction = Quaternion.AngleAxis(angle, transform.up) * transform.forward;
            float distance = 40;
            foreach (var hit in Physics.RaycastAll(transform.position + transform.up, direction, 40, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && Mathf.Abs(Vector3.Dot(hit.normal, transform.up)) < 0.7f)
                    distance = Mathf.Min(distance, hit.distance);
            sensor.AddObservation(distance / 40f);
        }
        sensor.AddObservation(player.DriftSide);
        sensor.AddObservation(player.DriftIntent);
        sensor.AddObservation(player.DriftCharge);
        sensor.AddObservation(Mathf.Clamp01(player.Boost_time / 3f));
        float ahead = Mathf.Clamp(body.linearVelocity.magnitude * .35f, 10, 30);
        foreach (float side in new[] { -6f, 0f, 6f })
            sensor.AddObservation(RoadAhead(player, ahead, side));
    }
    // A road surface is sensed before the kart reaches it. This supplies
    // perception to PPO; it does not choose or correct the policy's steering.
    public static float RoadAhead(Player kart, float forward, float side)
    {
        Vector3 point = kart.transform.position + kart.transform.forward * forward + kart.transform.right * side;
        float nearest = float.PositiveInfinity, road = -1;
        foreach (var hit in Physics.RaycastAll(point + Vector3.up * 8, Vector3.down, 20, kart.mask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(kart.transform) || hit.normal.y < .4f || hit.distance >= nearest) continue;
            if (!hit.collider.CompareTag("Ground") && !hit.collider.CompareTag("Dirt")) continue;
            nearest = hit.distance;
            road = hit.collider.CompareTag("Ground") ? 1 : 0;
        }
        return road;
    }
    // A route observation, not an autopilot: PPO still chooses every action.
    // The ordered checkpoints remain responsible for validating race progress.
    public static Vector3 PathDirection(Vector3 position, float speed, Transform path, Vector3 fallback)
    {
        if (!path || path.childCount < 2) return fallback;
        float closest = float.PositiveInfinity;
        int segmentIndex = 0;
        Vector3 projected = position;
        for (int i = 0; i < path.childCount; i++)
        {
            Vector3 a = path.GetChild(i).position, b = path.GetChild((i + 1) % path.childCount).position;
            Vector3 segment = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(position - a, segment) / Mathf.Max(segment.sqrMagnitude, .0001f));
            Vector3 point = a + segment * t;
            float distance = (position - point).sqrMagnitude;
            if (distance >= closest) continue;
            closest = distance; projected = point; segmentIndex = i;
        }
        float remaining = Mathf.Clamp(speed * .35f, 12, 32);
        Vector3 target = projected;
        for (int i = 0; i < path.childCount; i++)
        {
            Vector3 end = path.GetChild((segmentIndex + i + 1) % path.childCount).position;
            float distance = Vector3.Distance(target, end);
            if (distance >= remaining) { target = Vector3.MoveTowards(target, end, remaining); break; }
            remaining -= distance;
            target = end;
        }
        Vector3 direction = target - position;
        return direction.sqrMagnitude > .001f ? direction.normalized : fallback;
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (finished || !RACE_MANAGER.RACE_STARTED) return;
        player.Controls.SetRaceActions(actions.ContinuousActions[1], ForwardThrottle(actions.ContinuousActions[0]), actions.DiscreteActions[0] == 1);
    }
    // PPO still outputs [-1, 1]. Map it to coast/forward [0, 1], never reverse.
    // The original Player slows naturally when the accelerator is released.
    public static float ForwardThrottle(float action) => Mathf.Clamp01((action + 1f) * 0.5f);
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuous = actionsOut.ContinuousActions;
        var discrete = actionsOut.DiscreteActions;
        if (KartRunSettings.Mode == "random")
        {
            continuous[0] = (float)randomPolicy.NextDouble() * 2 - 1;
            continuous[1] = (float)randomPolicy.NextDouble() * 2 - 1;
            discrete[0] = randomPolicy.Next(2);
            return;
        }
        continuous[0] = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.W) ? 1 : -1;
        continuous[1] = (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ? 1 : 0) -
            (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.A) ? 1 : 0);
        discrete[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
    protected override void OnDisable()
    {
        if (episode) { episode.RewardChanged -= ReceiveReward; episode.EpisodeFinished -= Finish; }
        KartKeyboard.AgentControl = false;
        if (player) player.Controls.SetAutonomous(false);
        base.OnDisable();
    }
    void OnGUI()
    {
        GUI.Box(new Rect(12, 350, 850, 85), "Pilote IA - suivi de l'apprentissage");
        bool connected = Academy.Instance.IsCommunicatorOn;
        GUI.Label(new Rect(24, 375, 825, 55),
            (KartRunSettings.Mode == "model" ? "Modele autonome : l'IA pilote sans Python (aucun apprentissage)." :
                connected ? "Python connecte : l'IA choisit les commandes du kart." : "Python non connecte : aucun apprentissage en cours.") +
            $"\nDecisions depuis le lancement : {KartRunSettings.TotalDecisions} | Essais termines : {KartRunSettings.Episodes} | Marche arriere desactivee");
    }
}
