using UnityEngine;

// One command source per Player. The human keeps the original keyboard mappings.
public sealed class KartDriveInput
{
    public bool Autonomous { get; private set; }
    float steering, pedal;
    bool trained, drift, sampled, previous;
    int frame = -1;

    // The shipped policy learned binary throttle and full steering strength with a 0.1 dead zone.
    public void SetRaceActions(float turn, float throttle, bool drifting)
    {
        SetActions(turn, throttle);
        trained = true;
        drift = drifting;
    }
    void SampleDrift()
    {
        if (frame == Time.frameCount) return;
        previous = sampled; sampled = drift; frame = Time.frameCount;
    }
    public float Throttle => Autonomous && !trained ? Mathf.Max(0, pedal) : 1;
    public float Brake => Autonomous ? Mathf.Max(0, -pedal) : 1;
    public float SteeringGain => Autonomous && !trained ? Mathf.Abs(steering) : 1;

    public void SetAutonomous(bool enabled)
    {
        Autonomous = enabled;
        trained = drift = sampled = previous = false; frame = -1;
        SetActions(0, 0);
    }

    public void SetActions(float turn, float drive)
    {
        steering = Mathf.Clamp(turn, -1, 1);
        pedal = Mathf.Clamp(drive, -1, 1);
    }

    public float GetAxis(string axis) => Autonomous ? (axis == "Horizontal" ? steering : 0) : KartKeyboard.GetAxis(axis);
    public bool GetKeyDown(KeyCode key) { if (!Autonomous) return KartKeyboard.GetKeyDown(key); SampleDrift(); return key == KeyCode.V && sampled && !previous; }
    public bool GetKeyUp(KeyCode key) { if (!Autonomous) return KartKeyboard.GetKeyUp(key); SampleDrift(); return key == KeyCode.V && !sampled && previous; }

    public bool GetKey(KeyCode key)
    {
        if (!Autonomous) return KartKeyboard.GetKey(key);
        float threshold = trained ? .1f : 0;
        switch (key)
        {
            case KeyCode.V: return drift;
            case KeyCode.UpArrow: return !trained && pedal > 0;
            case KeyCode.Space: return pedal > threshold;
            case KeyCode.S: case KeyCode.DownArrow: return pedal < 0;
            case KeyCode.LeftArrow: return steering < -threshold;
            case KeyCode.RightArrow: return steering > threshold;
            default: return false;
        }
    }
}
