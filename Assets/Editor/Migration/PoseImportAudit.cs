using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PoseImportAudit
{
    public static void Run()
    {
        var report = new StringBuilder();
        const string model = "Assets/Models/Characters/Mario.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(model);
        foreach (var clip in importer.defaultClipAnimations)
            report.AppendLine($"TAKE {clip.name} {clip.takeName} {clip.firstFrame}..{clip.lastFrame}");
        foreach (var clip in importer.clipAnimations)
            report.AppendLine($"CLIP {clip.name} {clip.takeName} {clip.firstFrame}..{clip.lastFrame}");
        foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>())
        {
            report.AppendLine($"CURVES {clip.name} length={clip.length}");
            foreach (var b in AnimationUtility.GetCurveBindings(clip).Where(b => b.path.EndsWith("/Bone") || b.path.EndsWith("/Bone_048")))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, b);
                report.AppendLine($"{b.path} {b.propertyName}: {curve.Evaluate(0)} -> {curve.Evaluate(clip.length)}");
            }
        }
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            var r = c.GetComponent<Renderer>();
            if (c.name.Contains("Jump") || c.CompareTag("GliderPanel") ||
                (!c.isTrigger && (r == null || !r.enabled)))
                report.AppendLine($"COLLIDER {c.name} type={c.GetType().Name} trigger={c.isTrigger} bounds={c.bounds} position={c.transform.position} id={GlobalObjectId.GetGlobalObjectIdSlow(c)}");
        }
        File.WriteAllText("Migration/pose-import-audit.txt", report.ToString());
        EditorSceneManager.OpenScene("Assets/Scenes/WhiteScreen.unity");
        EditorApplication.Exit(0);
    }
}
