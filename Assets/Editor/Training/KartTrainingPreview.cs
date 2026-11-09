using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KartTrainingPreview
{
    static double nextCheck;
    [InitializeOnLoadMethod]
    static void StartRequestedPreview()
    {
        EditorApplication.update -= CheckRequest;
        EditorApplication.update += CheckRequest;
    }
    static void CheckRequest()
    {
        if (Application.isBatchMode) return;
        if (EditorApplication.timeSinceStartup < nextCheck || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextCheck = EditorApplication.timeSinceStartup + 1;
        const string requestPath = "Training/Results/start-preview.request";
        if (!System.IO.File.Exists(requestPath)) return;
        string request = System.IO.File.ReadAllText(requestPath);
        if (SessionState.GetString("KartTrainingPreview.Request", "") == request) return;
        if (request.StartsWith("stop:", System.StringComparison.Ordinal))
        {
            SessionState.SetString("KartTrainingPreview.Request", request);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetString("KartTrainingPreview.Request", request);
        Start();
    }
    [MenuItem("Mario Kart/Apprentissage/Ouvrir l'entrainement visible")]
    public static void Start()
    {
        if (EditorApplication.isPlaying) return;
        KartForwardControlChecks.Run();
        var sessionPath = "Training/Results/editor-session.json";
        var session = System.IO.File.Exists(sessionPath)
            ? JsonUtility.FromJson<EditorSession>(System.IO.File.ReadAllText(sessionPath)) : new EditorSession();
        System.Environment.SetEnvironmentVariable("KART_RUN", session.run_id);
        System.Environment.SetEnvironmentVariable("KART_MODE", session.mode);
        System.Environment.SetEnvironmentVariable("KART_SEED", session.seed.ToString());
        System.Environment.SetEnvironmentVariable("KART_PROFILE", session.profile);
        System.Environment.SetEnvironmentVariable("KART_GOAL", session.goal.ToString());
        System.Environment.SetEnvironmentVariable("KART_EPISODES", session.episodes.ToString());
        EditorSceneManager.OpenScene(session.track == "mario" ? "Assets/Scenes/Training/MarioCircuitTraining.unity" : "Assets/Scenes/Training/MooMooTraining.unity");
        var gameType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var game = EditorWindow.GetWindow(gameType);
        game.Focus();
        EditorApplication.isPlaying = true;
        EditorApplication.QueuePlayerLoopUpdate();
    }
    [System.Serializable]
    class EditorSession
    {
        public string run_id = "kart-forward-only";
        public int seed = 42;
        public string track = "moo";
        public string mode = "train";
        public string profile = "original";
        public int goal = 6;
        public int episodes;
    }

    [MenuItem("Mario Kart/Apprentissage/Tester le modele - Moo Moo Meadows")]
    public static void PlayMooModel() { PlayModel("moo"); }
    [MenuItem("Mario Kart/Apprentissage/Tester le modele - Mario Circuit")]
    public static void PlayMarioModel() { PlayModel("mario"); }
    static void PlayModel(string track)
    {
        if (EditorApplication.isPlaying) return;
        if (!System.IO.File.Exists("Assets/Resources/Kart.onnx"))
            throw new System.Exception("Aucun modele importe dans Assets/Resources/Kart.onnx.");
        System.Environment.SetEnvironmentVariable("KART_MODE", "model");
        System.Environment.SetEnvironmentVariable("KART_RUN", "model-demo-" + track);
        System.Environment.SetEnvironmentVariable("KART_PROFILE", "balanced");
        System.Environment.SetEnvironmentVariable("KART_GOAL", "0");
        System.Environment.SetEnvironmentVariable("KART_EPISODES", "0");
        System.Environment.SetEnvironmentVariable("KART_SEED", "42");
        EditorSceneManager.OpenScene(track == "mario" ? "Assets/Scenes/Training/MarioCircuitTraining.unity" : "Assets/Scenes/Training/MooMooTraining.unity");
        var game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        game.Focus();
        EditorApplication.isPlaying = true;
    }
}
