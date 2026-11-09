using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

// Only the dedicated kart training build uses CPU inference. Keep the game's
// graphics, but omit unused inference GPU kernels (including convolution kernels).
public sealed class KartInferenceShaderStripper : IPreprocessShaders, IPreprocessComputeShaders
{
    public int callbackOrder => 1000;
    static bool Exclude(Object shader) => KartTrainingSetup.HeadlessBuild || KartTrainingSetup.CpuOnlyBuild &&
        AssetDatabase.GetAssetPath(shader).StartsWith("Packages/com.unity.ai.inference/", System.StringComparison.Ordinal);

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (Exclude(shader)) data.Clear();
    }
    public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
    {
        if (Exclude(shader)) data.Clear();
    }
}
