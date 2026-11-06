using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class JumpLaneAudit
{
    const string Key = "JumpLaneAudit";
    static readonly float[] offsets = { -12, -18, -24, 12, 18, 0 };
    static float started = -1, nextSample;
    static int lane;
    static Vector3 direction, origin;
    static double deadline;
    static bool boostSeen;
    static JumpLaneAudit() { EditorApplication.update += Tick; }
    public static void Run()
    {
        File.WriteAllText("Migration/jump-lane-audit.txt", "Actual kart passage audit\n");
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        SessionState.SetInt(Key, 1);
        EditorApplication.isPlaying = true;
    }
    public static void Verify()
    {
        SessionState.SetBool(Key + "verify", true);
        SessionState.SetInt(Key + "failures", 0);
        Run();
    }
    static void Tick()
    {
        int stage = SessionState.GetInt(Key, 0);
        if (stage == 0 || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (stage == 2 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetInt(Key, 0);
            EditorSceneManager.OpenScene("Assets/Scenes/WhiteScreen.unity");
            bool passed = SessionState.GetInt(Key + "failures", 0) == 0;
            File.AppendAllText("Migration/jump-lane-audit.txt", passed ? "PASS: actual kart lanes and ramp boost verified.\n" : "FAIL: see lane results.\n");
            EditorApplication.Exit(passed ? 0 : 1);
            return;
        }
        if (!EditorApplication.isPlaying || !RACE_MANAGER.RACE_STARTED) return;
        var player = Object.FindAnyObjectByType<Player>();
        if (!player) return;
        if (started < 0)
        {
            var assist = player.GetComponent<SoloPlayerAssist>();
            if (assist) assist.enabled = false;
            var panel = GameObject.Find("BoostPanel");
            direction = panel.transform.position - new Vector3(-16614.73f, 23240.35f, -11600.49f);
            direction.y = 0; direction.Normalize();
            var right = Vector3.Cross(Vector3.up, direction);
            var above = panel.transform.position - direction * 28 + right * offsets[lane] + Vector3.up * 20;
            var road = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Single(c => c.name == "load__mm_road");
            if (!road.Raycast(new Ray(above, Vector3.down), out var ground, 100))
                throw new Exception("No road under test lane " + lane);
            var body = player.GetComponent<Rigidbody>();
            origin = ground.point + Vector3.up * 1.2f;
            player.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
            body.position = origin; body.rotation = player.transform.rotation;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            var tunnellingGuard = player.GetComponent<DontGoThroughThings>();
            if (tunnellingGuard)
                typeof(DontGoThroughThings).GetField("previousPosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(tunnellingGuard, origin);
            player.currentspeed = 0;
            Physics.SyncTransforms();
            started = Time.time; nextSample = 0;
            boostSeen = false;
            deadline = EditorApplication.timeSinceStartup + 90;
            File.AppendAllText("Migration/jump-lane-audit.txt", $"LANE {lane} offset={offsets[lane]} origin={origin} direction={direction}\n");
        }
        float elapsed = Time.time - started;
        KartKeyboard.TestHeld = k => k == KeyCode.UpArrow;
        boostSeen |= player.Boost_time > 0;
        if (elapsed >= nextSample)
        {
            nextSample += .5f;
            var body = player.GetComponent<Rigidbody>();
            var colliders = player.GetComponentsInChildren<Collider>();
            var bounds = colliders.First(c => !c.isTrigger).bounds;
            var hits = Physics.BoxCastAll(bounds.center, bounds.extents * .9f, player.transform.forward, Quaternion.identity, 2, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => !h.collider.transform.IsChildOf(player.transform));
            string line = $"t={elapsed:F2} pos={player.transform.position} progress={Vector3.Dot(player.transform.position-origin,direction):F2} speed={body.linearVelocity.magnitude:F2} forward={player.transform.forward} obstacles=" + string.Join("; ", hits.Select(h => $"{h.collider.name} distance={h.distance:F2} normal={h.normal}"));
            File.AppendAllText("Migration/jump-lane-audit.txt", line + "\n");
        }
        if (elapsed < 2.5f && EditorApplication.timeSinceStartup < deadline) return;
        float progress = Vector3.Dot(player.transform.position - origin, direction);
        bool ok = progress > 35 && progress < 200 && (offsets[lane] != 0 || boostSeen);
        File.AppendAllText("Migration/jump-lane-audit.txt", $"RESULT lane={lane} offset={offsets[lane]} progress={progress:F2} boost={boostSeen} passed={ok}\n");
        if (SessionState.GetBool(Key + "verify", false) && !ok)
            SessionState.SetInt(Key + "failures", SessionState.GetInt(Key + "failures", 0) + 1);
        var camera = Object.FindAnyObjectByType<RACE_MANAGER>().FrontCam.GetComponent<Camera>();
        camera.transform.position = player.transform.position - player.transform.forward * 10 + Vector3.up * 5;
        camera.transform.LookAt(player.transform.position + player.transform.forward * 8);
        MigrationAudit.Capture(camera, $"Migration/Previews/JumpLane-{lane}.png");
        lane++; started = -1;
        if (lane < offsets.Length) return;
        KartKeyboard.TestHeld = null;
        SessionState.SetInt(Key, 2);
        EditorApplication.isPlaying = false;
    }
}
