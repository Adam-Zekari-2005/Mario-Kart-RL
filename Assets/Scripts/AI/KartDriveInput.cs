using UnityEngine;

// One command source per Player. The human keeps the original keyboard mappings.
public sealed class KartDriveInput
{
    public bool Autonomous { get; private set; }
    float steering, pedal;
    public float Throttle => Autonomous ? Mathf.Max(0, pedal) : 1;
    public float Brake => Autonomous ? Mathf.Max(0, -pedal) : 1;
    public float SteeringGain => Autonomous ? Mathf.Abs(steering) : 1;

    public void SetAutonomous(bool enabled)
    {
        Autonomous = enabled;
        SetActions(0, 0);
    }

    public void SetActions(float turn, float drive)
    {
        steering = Mathf.Clamp(turn, -1, 1);
        pedal = Mathf.Clamp(drive, -1, 1);
    }

    public float GetAxis(string axis) => Autonomous ? (axis == "Horizontal" ? steering : 0) : KartKeyboard.GetAxis(axis);
    public bool GetKeyDown(KeyCode key) => !Autonomous && KartKeyboard.GetKeyDown(key);
    public bool GetKeyUp(KeyCode key) => !Autonomous && KartKeyboard.GetKeyUp(key);

    public bool GetKey(KeyCode key)
    {
        if (!Autonomous) return KartKeyboard.GetKey(key);
        switch (key)
        {
            case KeyCode.Space: case KeyCode.UpArrow: return pedal > 0;
            case KeyCode.S: case KeyCode.DownArrow: return pedal < 0;
            case KeyCode.LeftArrow: return steering < 0;
            case KeyCode.RightArrow: return steering > 0;
            default: return false;
        }
    }
}
