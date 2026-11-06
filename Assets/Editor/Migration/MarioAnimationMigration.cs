using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Retarget the legacy secondary clips against the converted FBX's seated pose.
public static class MarioAnimationMigration
{
    const string Model = "Assets/Models/Characters/Mario.fbx";
    const string Legacy = "Assets/ANimations/MorePlayerAnimations/";
    const string Output = "Assets/ANimations/MigratedMario";

    public static void Run()
    {
        Apply();
        EditorApplication.Exit(0);
    }

    public static void RunWithCollisionAudit()
    {
        Apply();
        JumpCollisionAudit.Run();
    }

    static void Apply()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(Model);
        var clips = importer.clipAnimations;
        foreach (var clip in clips)
        {
            var take = importer.defaultClipAnimations.FirstOrDefault(t => t.takeName == clip.takeName);
            if (take == null) continue;
            // The old Blender importer used -1-based ranges. A -1..0 range
            // produces an empty clip in the FBX importer, restoring the old rig pose.
            clip.firstFrame = take.firstFrame;
            clip.lastFrame = take.lastFrame;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        foreach (var name in new[] { "Normal", "NormalWithItem", "Reverse", "BoostWithItemNormal" })
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Single(c => c.name == name);
            if (!AnimationUtility.GetCurveBindings(clip).Any(b => b.path == "Armature/Bone"))
                throw new InvalidOperationException("Missing body animation curves: " + name);
        }

        if (!AssetDatabase.IsValidFolder(Output))
            AssetDatabase.CreateFolder("Assets/ANimations", "MigratedMario");
        var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
        try
        {
            var normal = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Single(c => c.name == "Normal");
            normal.SampleAnimation(instance, 0);
            Retarget(instance, "Moustache.controller", "MoustacheIdle", new[] { "MoustacheIdle", "Moustache" });
            Retarget(instance, "hAIR.controller", "IdleHair", new[] { "IdleHair", "Hair" });
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        AssetDatabase.SaveAssets();
        Debug.Log("Mario animation ranges and secondary clips repaired.");
    }

    static void Retarget(GameObject model, string controllerName, string idleName, string[] names)
    {
        var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Legacy + idleName + ".anim");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Legacy + controllerName);
        foreach (var name in names)
        {
            var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(Legacy + name + ".anim");
            var targetPath = Output + "/" + name + ".anim";
            var target = AssetDatabase.LoadAssetAtPath<AnimationClip>(targetPath);
            if (target == null)
            {
                target = UnityEngine.Object.Instantiate(source);
                AssetDatabase.CreateAsset(target, targetPath);
            }
            else EditorUtility.CopySerialized(source, target);
            target.name = name + " (Unity6)";
            var bindings = AnimationUtility.GetCurveBindings(source);
            foreach (var path in bindings.Select(b => b.path).Distinct())
            {
                var bone = model.GetComponentsInChildren<Transform>().Single(t => t.name == path);
                // Both idle and moving states explicitly own rotation and scale.
                // Otherwise Animator's Write Defaults restores the pre-conversion rig.
                {
                    var reference = AnimationUtility.GetCurveBindings(idle).Any(b => b.path == path && b.propertyName.StartsWith("localEulerAngles"))
                        ? idle : source;
                    var rest = ReadVector(reference, path, "localEulerAnglesRaw", 0);
                    var correction = bone.localRotation * Quaternion.Inverse(Quaternion.Euler(rest));
                    var curves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
                    int frames = Math.Max(1, Mathf.CeilToInt(source.length * 60));
                    Quaternion previous = Quaternion.identity;
                    for (int frame = 0; frame <= frames; frame++)
                    {
                        float time = source.length * frame / frames;
                        var q = correction * Quaternion.Euler(ReadVector(source, path, "localEulerAnglesRaw", time));
                        if (frame > 0 && Quaternion.Dot(previous, q) < 0) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                        previous = q;
                        curves[0].AddKey(time, q.x); curves[1].AddKey(time, q.y);
                        curves[2].AddKey(time, q.z); curves[3].AddKey(time, q.w);
                    }
                    foreach (var binding in bindings.Where(b => b.path == path && b.propertyName.Contains("Euler")))
                        AnimationUtility.SetEditorCurve(target, binding, null);
                    for (int axis = 0; axis < 4; axis++)
                        AnimationUtility.SetEditorCurve(target, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + "xyzw"[axis]), curves[axis]);
                    target.EnsureQuaternionContinuity();
                }
                for (int axis = 0; axis < 3; axis++)
                {
                    var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale." + "xyz"[axis]);
                    var rest = AnimationUtility.GetEditorCurve(idle, binding)?.Evaluate(0) ?? 1;
                    var curve = AnimationUtility.GetEditorCurve(source, binding);
                    if (curve == null)
                        curve = AnimationCurve.Constant(0, source.length, bone.localScale[axis]);
                    else
                    {
                        float ratio = bone.localScale[axis] / rest;
                        curve.keys = curve.keys.Select(k => new Keyframe(k.time, k.value * ratio, k.inTangent * ratio, k.outTangent * ratio)).ToArray();
                    }
                    AnimationUtility.SetEditorCurve(target, binding, curve);
                }
            }
            foreach (var layer in controller.layers)
                foreach (var state in layer.stateMachine.states)
                    if (state.state.motion == source || state.state.motion == target)
                        state.state.motion = target;
            EditorUtility.SetDirty(target);
        }
        EditorUtility.SetDirty(controller);
    }

    static Vector3 ReadVector(AnimationClip clip, string path, string property, float time)
    {
        var value = Vector3.zero;
        for (int axis = 0; axis < 3; axis++)
            value[axis] = AnimationUtility.GetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property + "." + "xyz"[axis]))?.Evaluate(time) ?? 0;
        return value;
    }
}
