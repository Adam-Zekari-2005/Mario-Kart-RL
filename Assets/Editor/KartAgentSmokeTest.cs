using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class KartAgentSmokeTest
{
    const string Key = "KartPPO.Smoke";
    static Player human, ai;
    static float start;
    static Vector3 origin, forward, humanOrigin;
    static bool failed;
    static KartAgentSmokeTest() { EditorApplication.update += Tick; }

    [MenuItem("Mario Kart/IA/Tester joueur et IA (Moo Moo Meadows)")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        SessionState.SetInt(Key, 1);
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        int stage = SessionState.GetInt(Key, 0);
        if (stage == 0) return;
        if (stage == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetInt(Key, 0);
            EditorSceneManager.OpenScene("Assets/Scenes/WhiteScreen.unity");
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetBool(Key+"Error", false) ? 1 : 0);
            return;
        }
        if (!EditorApplication.isPlaying || !RACE_MANAGER.RACE_STARTED) return;
        try
        {
            if (stage == 1)
            {
                human = Object.FindAnyObjectByType<Player>();
                ai = KartAgentSetup.SpawnBeside(human);
                KartKeyboard.TestHeld = key => key == KeyCode.UpArrow || key == KeyCode.RightArrow;
                start = Time.time; origin = ai.transform.position; forward = ai.transform.forward; humanOrigin = human.transform.position;
                SessionState.SetInt(Key, 2);
                return;
            }
            if (stage != 2 || Time.time-start < 2) return;
            float travel = Vector3.Distance(origin, ai.transform.position);
            float turn = Vector3.SignedAngle(forward, ai.transform.forward, Vector3.up);
            float humanTravel = Vector3.Distance(humanOrigin, human.transform.position);
            float humanTurn = Vector3.SignedAngle(forward, human.transform.forward, Vector3.up);
            failed = !ai.Controls.Autonomous || human.Controls.Autonomous || travel < 2 || turn > -1 || humanTravel < 2 || humanTurn < 1;
            Debug.Log($"KartPPO test: AI travel={travel:F2} turn={turn:F2}, human travel={humanTravel:F2} turn={humanTurn:F2}, passed={!failed}");
        }
        catch (Exception error) { failed = true; Debug.LogException(error); }
        KartKeyboard.TestHeld = null;
        SessionState.SetBool(Key+"Error", failed);
        SessionState.SetInt(Key, 3);
        EditorApplication.isPlaying = false;
    }
}
