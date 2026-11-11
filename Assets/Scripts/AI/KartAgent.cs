using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public interface IKartTrackDirection
{
    bool TryGetDirection(Vector3 position, out Vector3 worldDirection);
}

[DisallowMultipleComponent, RequireComponent(typeof(Player))]
public sealed class KartAgent : Agent
{
    public const int ObservationCount = 9;
    public const string Behavior = "KartPPO";
    [Tooltip("Optional track direction provider. Defaults to the next ordered checkpoint.")]
    public MonoBehaviour trackProvider;
    public LayerMask obstacleLayers = ~0;
    static readonly float[] bearings = { -75, -35, 0, 35, 75 };
    const float Range = 35;
    Player kart;
    Rigidbody body;
    LapCounter laps;

    public override void Initialize()
    {
        kart = GetComponent<Player>();
        body = GetComponent<Rigidbody>();
        laps = GetComponent<LapCounter>();
        kart.Controls.SetAutonomous(true);
    }

    protected override void OnDisable()
    {
        if (kart) kart.Controls.SetAutonomous(false);
        base.OnDisable();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (kart) kart.Controls.SetAutonomous(true);
    }

    public override void OnEpisodeBegin() => kart.Controls.SetActions(0, 0);

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 velocity = transform.InverseTransformDirection(body.linearVelocity);
        sensor.AddObservation(Mathf.Clamp(velocity.z / 120f, -1, 1));
        sensor.AddObservation(Mathf.Clamp(velocity.x / 120f, -1, 1));
        Vector3 heading = Vector3.zero;
        if (!(trackProvider is IKartTrackDirection provider && provider.TryGetDirection(body.position, out heading))
            && laps && laps.checkpoints && laps.checkpoints.childCount > 0)
        {
            var next = laps.checkpoints.GetChild(laps.currentCheckpointVal % laps.checkpoints.childCount);
            heading = next.position - body.position;
        }
        Vector3 local = transform.InverseTransformDirection(Vector3.ProjectOnPlane(heading, transform.up).normalized);
        sensor.AddObservation(local.x);
        sensor.AddObservation(local.z);
        foreach (float angle in bearings)
        {
            Vector3 ray = Quaternion.AngleAxis(angle, transform.up) * transform.forward;
            float distance = Range;
            foreach (var hit in Physics.RaycastAll(body.position + transform.up * .5f, ray, Range, obstacleLayers, QueryTriggerInteraction.Ignore))
                if (hit.collider.attachedRigidbody != body && !hit.transform.IsChildOf(transform) && Vector3.Dot(hit.normal, transform.up) < .65f)
                    distance = Mathf.Min(distance, hit.distance);
            sensor.AddObservation(distance / Range);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        kart.Controls.SetActions(actions.ContinuousActions[0], actions.ContinuousActions[1]);
    }

    // Predictable smoke-test policy while no trained model is available.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        actions[0] = -.35f;
        actions[1] = .65f;
    }
}
