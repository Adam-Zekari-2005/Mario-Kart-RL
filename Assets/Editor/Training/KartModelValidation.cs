using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.InferenceEngine;

public static class KartModelValidation
{
    public static void VerifyAndBuild()
    {
        Run();
        KartTrainingSetup.BuildHeadless();
    }

    [MenuItem("Mario Kart/Apprentissage/Verifier le modele exporte")]
    public static void Run()
    {
        const string path = "Assets/Resources/Kart.onnx";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var asset = AssetDatabase.LoadAssetAtPath<ModelAsset>(path);
        if (!asset) throw new System.Exception("Modele absent : " + path);
        var model = ModelLoader.Load(asset);
        using var worker = new Worker(model, BackendType.CPU);
        using var observations = new Tensor<float>(new TensorShape(1, KartAgent.ObservationCount), new float[KartAgent.ObservationCount]);
        using var masks = new Tensor<float>(new TensorShape(1, 2), new[] { 1f, 1f });
        worker.SetInput("obs_0", observations);
        worker.SetInput("action_masks", masks);
        worker.Schedule();
        using var continuous = ((Tensor<float>)worker.PeekOutput("deterministic_continuous_actions")).ReadbackAndClone();
        using var discrete = ((Tensor<int>)worker.PeekOutput("deterministic_discrete_actions")).ReadbackAndClone();
        float[] values = continuous.DownloadToArray();
        int[] choices = discrete.DownloadToArray();
        if (values.Length != 2 || values.Any(v => float.IsNaN(v) || float.IsInfinity(v)) ||
            choices.Length != 1 || choices[0] < 0 || choices[0] > 1)
            throw new System.Exception("Sorties du modele invalides.");
        Directory.CreateDirectory("Training/Reports");
        File.WriteAllText("Training/Reports/model-validation.json", JsonUtility.ToJson(new Result
        {
            unity = Application.unityVersion,
            observation_count = KartAgent.ObservationCount,
            continuous_actions = values,
            discrete_actions = choices,
            cpu_inference_passed = true
        }, true));
        Debug.Log("KartModelValidation PASS : import ONNX et inference CPU Unity verifies.");
    }
    [System.Serializable]
    class Result
    {
        public string unity;
        public int observation_count;
        public float[] continuous_actions;
        public int[] discrete_actions;
        public bool cpu_inference_passed;
    }
}
