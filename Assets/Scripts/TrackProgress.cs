using UnityEngine;

/// <summary>Ordered checkpoint progress, independent of the race ranking counters.</summary>
[RequireComponent(typeof(LapCounter), typeof(Rigidbody))]
public sealed class TrackProgress : MonoBehaviour
{
    public event System.Action<int> CheckpointPassed;
    public int NextCheckpoint { get; private set; }
    public int CheckpointsPassed { get; private set; }
    public int CompletedLaps { get; private set; }
    public float ProgressMetres { get; private set; }
    public float ForwardSpeed { get; private set; }
    public Vector3 TrackDirection { get; private set; }
    public bool IsReady { get; private set; }
    Transform points;
    Rigidbody body;
    float[] lengths;
    float passedDistance, origin;
    int passedThisLap;

    void Start()
    {
        points = GetComponent<LapCounter>().checkpoints;
        body = GetComponent<Rigidbody>();
        if (!points || points.childCount < 2)
        {
            Debug.LogError("TrackProgress : au moins deux checkpoints sont requis.", this);
            enabled = false;
            return;
        }
        lengths = new float[points.childCount];
        for (int i = 0; i < lengths.Length; i++)
            lengths[i] = Vector3.Distance(Previous(i).position, points.GetChild(i).position);
        Sample();
        origin = ProgressMetres;
        IsReady = true;
    }

    Transform Previous(int index) => points.GetChild((index + points.childCount - 1) % points.childCount);

    public void Sample()
    {
        if (!points || lengths == null) return;
        Vector3 a = Previous(NextCheckpoint).position;
        Vector3 segment = points.GetChild(NextCheckpoint).position - a;
        TrackDirection = segment.normalized;
        ForwardSpeed = Vector3.Dot(body.linearVelocity, TrackDirection);
        float along = Mathf.Clamp(Vector3.Dot(transform.position - a, TrackDirection), 0, lengths[NextCheckpoint]);
        ProgressMetres = passedDistance + along - origin;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsReady || !RACE_MANAGER.RACE_STARTED || RACE_MANAGER.RACE_COMPLETED) return;
        Sample();
        // Reject backwards crossings and require every checkpoint in hierarchy order.
        if (ForwardSpeed <= 0.1f) return;
        if (other.transform == points.GetChild(NextCheckpoint))
        {
            passedDistance += lengths[NextCheckpoint];
            NextCheckpoint = (NextCheckpoint + 1) % points.childCount;
            CheckpointsPassed++;
            passedThisLap++;
            CheckpointPassed?.Invoke((NextCheckpoint + points.childCount - 1) % points.childCount);
        }
        else if (other.CompareTag("NextLapCollider") && passedThisLap >= points.childCount)
        {
            CompletedLaps++;
            passedThisLap = 0;
        }
    }
}
