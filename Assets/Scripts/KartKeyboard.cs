using UnityEngine;

// Adapter for the original game's logical controls. Physics stays unchanged.
public static class KartKeyboard
{
    // Optional single-kart policy input. Logical controls retain the original physics.
    public static bool AgentControl;
    public static float AgentThrottle, AgentSteering;
    public static bool AgentDrift;
    static bool previousDrift, sampledDrift;
    static int agentFrame = -1;
    public static void SetAgentInput(float throttle, float turn, bool drift)
    {
        AgentControl = true;
        AgentThrottle = Mathf.Clamp(throttle, -1, 1);
        AgentSteering = Mathf.Clamp(turn, -1, 1);
        AgentDrift = drift;
    }
    static bool AgentKey(KeyCode key)
    {
        if (key == KeyCode.Space) return AgentThrottle > 0.1f;
        if (key == KeyCode.S || key == KeyCode.DownArrow) return AgentThrottle < -0.1f;
        if (key == KeyCode.LeftArrow) return AgentSteering < -0.1f;
        if (key == KeyCode.RightArrow) return AgentSteering > 0.1f;
        if (key == KeyCode.V) return AgentDrift;
        return false;
    }
    static void SampleAgentButtons()
    {
        if (agentFrame == Time.frameCount) return;
        agentFrame = Time.frameCount;
        previousDrift = sampledDrift;
        sampledDrift = AgentDrift;
    }
    static readonly KeyCode[] Accelerate={KeyCode.UpArrow,KeyCode.Z,KeyCode.W};
    static readonly KeyCode[] Reverse={KeyCode.DownArrow,KeyCode.S};
    static readonly KeyCode[] Left={KeyCode.LeftArrow,KeyCode.Q,KeyCode.A};
    static readonly KeyCode[] Right={KeyCode.RightArrow,KeyCode.D};
    static readonly KeyCode[] Drift={KeyCode.Space};
    static readonly KeyCode[] Item={KeyCode.LeftShift,KeyCode.RightShift};
    static readonly KeyCode[] BackItem={KeyCode.LeftControl,KeyCode.RightControl};
    static int frame=-1;
    static float steering;
#if UNITY_EDITOR
    public static System.Func<KeyCode,bool> TestHeld;
    public static System.Func<KeyCode,bool> TestDown;
    public static System.Func<KeyCode,bool> TestUp;
    static int testFrame=-1;
    static System.Collections.Generic.HashSet<KeyCode> testNow=new System.Collections.Generic.HashSet<KeyCode>(),testBefore=new System.Collections.Generic.HashSet<KeyCode>();
    static void ReadTestFrame()
    {
        if(testFrame==Time.frameCount)return;
        testFrame=Time.frameCount;
        var old=testBefore;testBefore=testNow;testNow=old;testNow.Clear();
        foreach(KeyCode key in System.Enum.GetValues(typeof(KeyCode)))if(TestHeld(key))testNow.Add(key);
    }
#endif
    static KeyCode[] Keys(KeyCode logical)
    {
        switch(logical)
        {
            case KeyCode.Space:return Accelerate;
            case KeyCode.S:return Reverse;
            case KeyCode.LeftArrow:return Left;
            case KeyCode.RightArrow:return Right;
            case KeyCode.V:return Drift;
            case KeyCode.RightShift:return Item;
            case KeyCode.LeftControl:return BackItem;
            default:return null;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){frame=-1;steering=0;
        AgentControl=false;AgentThrottle=AgentSteering=0;AgentDrift=false;
        agentFrame=-1;previousDrift=sampledDrift=false;
#if UNITY_EDITOR
        TestHeld=null;TestDown=null;TestUp=null;
        testFrame=-1;testNow.Clear();testBefore.Clear();
#endif
    }
    static bool Held(KeyCode key)
    {
#if UNITY_EDITOR
        if(TestHeld!=null){ReadTestFrame();return testNow.Contains(key);}
#endif
        return Input.GetKey(key);
    }
    static bool Down(KeyCode key)
    {
#if UNITY_EDITOR
        if(TestDown!=null)return TestDown(key);
        if(TestHeld!=null){ReadTestFrame();return testNow.Contains(key)&&!testBefore.Contains(key);}
#endif
        return Input.GetKeyDown(key);
    }
    static bool Up(KeyCode key)
    {
#if UNITY_EDITOR
        if(TestUp!=null)return TestUp(key);
        if(TestHeld!=null){ReadTestFrame();return !testNow.Contains(key)&&testBefore.Contains(key);}
#endif
        return Input.GetKeyUp(key);
    }
    public static bool GetKey(KeyCode logical)
    {
        if (AgentControl) return AgentKey(logical);
        var keys=Keys(logical);if(keys==null)return Held(logical);
        foreach(var key in keys)if(Held(key))return true;return false;
    }
    public static bool GetKeyDown(KeyCode logical)
    {
        if (AgentControl) { SampleAgentButtons(); return logical == KeyCode.V && sampledDrift && !previousDrift; }
        var keys=Keys(logical);if(keys==null)return Down(logical);
        foreach(var key in keys)if(Down(key))return true;return false;
    }
    public static bool GetKeyUp(KeyCode logical)
    {
        if (AgentControl) { SampleAgentButtons(); return logical == KeyCode.V && !sampledDrift && previousDrift; }
        if(GetKey(logical))return false;
        var keys=Keys(logical);if(keys==null)return Up(logical);
        foreach(var key in keys)if(Up(key))return true;return false;
    }
    public static float GetAxis(string axis)
    {
        if (AgentControl && axis == "Horizontal") return AgentSteering;
        if(axis!="Horizontal")return Input.GetAxis(axis);
        if(frame!=Time.frameCount)
        {
            frame=Time.frameCount;
            float target=(GetKey(KeyCode.RightArrow)?1:0)-(GetKey(KeyCode.LeftArrow)?1:0);
            steering=Mathf.MoveTowards(steering,target,7*Time.deltaTime);
        }
        return steering;
    }
}
