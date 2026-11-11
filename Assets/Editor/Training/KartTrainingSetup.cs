using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public static class KartTrainingSetup
{
    public static bool CpuOnlyBuild { get; private set; }
    public static bool HeadlessBuild { get; private set; }
    const string Folder = "Assets/Scenes/Training";
    static void Configure(GameObject kart, string name, int observations, ActionSpec actions)
    {
        var parameters = kart.GetComponent<BehaviorParameters>() ?? kart.AddComponent<BehaviorParameters>();
        parameters.BehaviorName = name;
        parameters.BrainParameters.VectorObservationSize = observations;
        parameters.BrainParameters.NumStackedVectorObservations = 1;
        parameters.BrainParameters.ActionSpec = actions;
        parameters.BehaviorType = BehaviorType.Default;
    }
    [MenuItem("Mario Kart/Apprentissage/Preparer les scenes IA")]
    public static void Prepare()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        CopyTrack("Assets/Scenes/MooMooMeadows.unity", "MooMooTraining");
        CopyTrack("Assets/Scenes/Mario Circuit.unity", "MarioCircuitTraining");
        var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("TrainingBoot").AddComponent<KartTrainingBoot>();
        EditorSceneManager.SaveScene(boot, Folder + "/KartTrainingBoot.unity");
        PrepareBall();
        EditorSceneManager.OpenScene("Assets/Scenes/WhiteScreen.unity");
    }
    static void CopyTrack(string source, string name)
    {
        string target = Folder + "/" + name + ".unity";
        if (File.Exists(target)) EditorSceneManager.OpenScene(target);
        else
        {
            EditorSceneManager.OpenScene(source);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), target, true);
            EditorSceneManager.OpenScene(target);
        }
        var player = Object.FindAnyObjectByType<Player>();
        if (!player) throw new System.Exception("Joueur introuvable dans " + target);
        if (!player.GetComponent<TrackProgress>()) player.gameObject.AddComponent<TrackProgress>();
        if (!player.GetComponent<KartEpisode>()) player.gameObject.AddComponent<KartEpisode>();
        Configure(player.gameObject, "Kart", KartAgent.ObservationCount, new ActionSpec(2, new[] { 2 }));
        if (!player.GetComponent<KartRunRecorder>()) player.gameObject.AddComponent<KartRunRecorder>();
        if (!player.GetComponent<KartAgent>()) player.gameObject.AddComponent<KartAgent>();
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        if (!EditorSceneManager.SaveScene(player.gameObject.scene, target))
            throw new System.Exception("Impossible d'enregistrer " + target);
        string agentGuid = AssetDatabase.AssetPathToGUID("Assets/Scripts/Training/KartAgent.cs");
        if (!File.ReadAllText(target).Contains(agentGuid))
            throw new System.Exception("Composant KartAgent absent de " + target);
    }
    static void PrepareBall()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // Recreate the geometry for the unmodified official 3DBall agent script.
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "3DBall Platform";
        platform.transform.localScale = new Vector3(5, 0.2f, 5);
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.position = new Vector3(0, 4, 0);
        ball.AddComponent<Rigidbody>();
        Configure(platform, "3DBall", 8, ActionSpec.MakeContinuous(2));
        var agent = platform.AddComponent<Ball3DAgent>();
        agent.ball = ball;
        agent.useVecObs = true;
        agent.MaxStep = 5000;
        var requester = platform.AddComponent<DecisionRequester>();
        requester.DecisionPeriod = 5;
        requester.TakeActionsBetweenDecisions = true;
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 8, -12);
        camera.transform.LookAt(Vector3.up);
        new GameObject("Light").AddComponent<Light>().type = LightType.Directional;
        EditorSceneManager.SaveScene(scene, Folder + "/Connection3DBall.unity");
    }
    public static void PrepareAndBuildSample()
    {
        TrackProgressChecks.Run();
        Prepare();
        Build(new[] { Folder + "/Connection3DBall.unity" }, "Builds/TrainingSample/Connection3DBall.exe");
    }
    [MenuItem("Mario Kart/Apprentissage/Compiler le jeu IA")]
    public static void BuildKart()
    {
        BuildKartVersion(false);
    }
    [MenuItem("Mario Kart/Apprentissage/Compiler les essais IA sans affichage")]
    public static void BuildHeadless()
    {
        BuildKartVersion(true);
    }
    static void BuildKartVersion(bool headless)
    {
        Prepare();
        TrackProgressChecks.Run();
        KartKeyboard.SetAgentInput(1, -0.5f, true);
        if (!KartKeyboard.GetKey(KeyCode.Space) || KartKeyboard.GetKey(KeyCode.S) ||
            KartKeyboard.GetAxis("Horizontal") != -0.5f || !KartKeyboard.GetKey(KeyCode.V))
            throw new System.Exception("Commandes IA incorrectes");
        KartKeyboard.SetAgentInput(-1, 0.75f, false);
        if (!KartKeyboard.GetKey(KeyCode.S) || KartKeyboard.GetKey(KeyCode.Space) ||
            KartKeyboard.GetAxis("Horizontal") != 0.75f || KartKeyboard.GetKey(KeyCode.V))
            throw new System.Exception("Commandes de recul IA incorrectes");
        KartKeyboard.AgentControl = false;
        Debug.Log("Kart control checks PASS");
        CpuOnlyBuild = true;
        HeadlessBuild = headless;
        try
        {
            Build(new[] { Folder + "/KartTrainingBoot.unity", Folder + "/MooMooTraining.unity", Folder + "/MarioCircuitTraining.unity" },
                headless ? "Builds/TrainingHeadless/KartTrainingHeadless.exe" : "Builds/Training/KartTraining.exe");
        }
        finally { CpuOnlyBuild = HeadlessBuild = false; }
    }
    static void Build(string[] scenes, string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(scenes, output, BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Compilation echouee : " + report.summary.result);
        Debug.Log("TRAINING BUILD PASS: " + output);
    }
}
