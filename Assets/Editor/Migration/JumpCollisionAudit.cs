using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class JumpCollisionAudit
{
    // Regression for the invisible helper cube protruding beyond the ramp entrance.
    public static void VerifyBlockFix()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
            Physics.SyncTransforms();
            var block = Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None)
                .Single(c => GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId == 1550386877);
            if (block.enabled) throw new System.Exception("The invisible helper collider is still enabled.");
            var road = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Single(c => c.name == "load__mm_road");
            var ramp = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).Single(c => c.name == "DashPanelShape_mm_jumpboard6__mm_jumpboard2");
            if (!road.enabled || !ramp.enabled || road.isTrigger || ramp.isTrigger)
                throw new System.Exception("Road/ramp must retain their solid colliders.");
            var boost = GameObject.Find("BoostPanel").GetComponent<Collider>();
            if (!boost.enabled || !boost.isTrigger) throw new System.Exception("Boost trigger was changed.");
            var report = new StringBuilder();
            int cleared = 0, supported = 0;
            float closestObstruction = float.PositiveInfinity;
            Vector3 witness = default, direction = default;
            for (int x = -8; x <= 8; x++)
            for (int z = -12; z <= 12; z++)
            {
                var above = block.transform.position + new Vector3(x, 10, z);
                if (!road.Raycast(new Ray(above, Vector3.down), out var ground, 30)) continue;
                supported++;
                var start = ground.point + Vector3.up * .4f;
                foreach (var dir in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                {
                    block.enabled = true;
                    Physics.SyncTransforms();
                    bool overlaps = Physics.CheckSphere(start, .17f, ~0, QueryTriggerInteraction.Ignore);
                    bool blocked = Physics.SphereCast(start, .15f, dir, out var before, 3, ~0, QueryTriggerInteraction.Ignore) && before.collider == block && !overlaps;
                    block.enabled = false;
                    Physics.SyncTransforms();
                    if (blocked && !Physics.SphereCast(start, .15f, dir, out _, 3, ~0, QueryTriggerInteraction.Ignore))
                    {
                        cleared++;
                        if (before.distance > .2f && before.distance < closestObstruction
                            && Vector3.Dot(before.normal, -dir) > .7f)
                        {
                            closestObstruction = before.distance;
                            witness = start; direction = dir;
                        }
                        report.AppendLine($"Cleared passage: start={start}, direction={dir}, former hit distance={before.distance}");
                    }
                }
            }
            if (cleared == 0) throw new System.Exception("No blocked-to-clear passage demonstrated.");
            if (float.IsPositiveInfinity(closestObstruction)) throw new System.Exception("No frontal collision witness found.");
            // Simulate a rigid body along an actual formerly obstructed passage.
            var previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            float[] distances = new float[2];
            try
            {
                for (int run = 0; run < 2; run++)
                {
                    block.enabled = run == 0;
                    var probe = new GameObject("Collision regression probe");
                    probe.transform.position = witness;
                    probe.AddComponent<SphereCollider>().radius = .15f;
                    var body = probe.AddComponent<Rigidbody>();
                    body.useGravity = false;
                    body.linearDamping = 0;
                    body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
                    body.constraints |= Mathf.Abs(direction.x) > .5f
                        ? RigidbodyConstraints.FreezePositionZ : RigidbodyConstraints.FreezePositionX;
                    Physics.SyncTransforms();
                    body.linearVelocity = direction * 2;
                    for (int step = 0; step < 75; step++)
                    {
                        Physics.Simulate(.02f);
                    }
                    distances[run] = Vector3.Dot(body.position - witness, direction);
                    Object.DestroyImmediate(probe);
                }
            }
            finally { block.enabled = false; Physics.simulationMode = previousMode; }
            report.AppendLine($"Rigid body travel before={distances[0]:F3}, after={distances[1]:F3}");
            // Allow small contact/precision losses at this scene's large world coordinates;
            // require traversal well beyond the reproduced obstruction, not exact free-flight distance.
            if (distances[1] < 2.5f || distances[1] < distances[0] + 1f)
                throw new System.Exception("Rigid body crossing did not improve as expected: " + report);
            report.AppendLine($"PASS: {cleared} formerly blocked casts cleared; {supported} road samples supported; ramp and boost preserved.");
            File.WriteAllText("Migration/jump-block-regression.txt", report.ToString());
            Debug.Log(report.ToString());
            EditorApplication.Exit(0);
        }
        catch (System.Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    public static void ProbeBlock()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        Physics.SyncTransforms();
        var block = Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None)
            .Single(c => GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId == 1550386877);
        var report = new StringBuilder();
        for (int x = -4; x <= 4; x++)
        for (int z = -8; z <= 8; z += 2)
        {
            var origin = block.bounds.center + new Vector3(x, 15, z);
            var hits = Physics.RaycastAll(origin, Vector3.down, 40, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance);
            report.AppendLine($"{origin}: " + string.Join("; ", hits.Select(h => $"{h.collider.name} y={h.point.y:F3} normal={h.normal}")));
        }
        File.WriteAllText("Migration/block-probe.txt", report.ToString());
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", Color.magenta);
        block.GetComponent<Renderer>().enabled = true;
        block.GetComponent<Renderer>().sharedMaterial = material;
        var camera = new GameObject("Audit Camera").AddComponent<Camera>();
        var center = GameObject.Find("BoostPanel").transform.position;
        camera.farClipPlane = 2000;
        camera.transform.position = center + new Vector3(25, 30, -30);
        camera.transform.LookAt(center);
        MigrationAudit.Capture(camera, "Migration/Previews/JumpBlock.png");
        EditorApplication.Exit(0);
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        Physics.SyncTransforms();
        var panel = GameObject.Find("BoostPanel");
        var report = new StringBuilder();
        var center = panel.transform.position;
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if ((c.bounds.ClosestPoint(center) - center).sqrMagnitude > 60 * 60) continue;
            var renderer = c.GetComponent<Renderer>();
            report.AppendLine($"{c.name} enabled={c.enabled} trigger={c.isTrigger} mesh={(c is MeshCollider mc && mc.sharedMesh ? mc.sharedMesh.name : "none")} renderer={(renderer ? renderer.enabled.ToString() : "none")} materials={string.Join(",", renderer == null ? new string[0] : renderer.sharedMaterials.Select(m => m ? m.name : "null"))} bounds={c.bounds} id={GlobalObjectId.GetGlobalObjectIdSlow(c)}");
        }
        var flame = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None)
            .Single(c => c.name == "DashPanelShape_mm_jumpboard6__PanelFlame");
        // Sample horizontal rays around the ramp to identify the exact blocking surfaces.
        for (int x = -24; x <= 24; x += 2)
            for (int z = -24; z <= 24; z += 2)
            {
                var start = center + new Vector3(x, -2, z);
                var ray = new Ray(start, Vector3.forward);
                if (flame.Raycast(ray, out var hit, 4))
                    report.AppendLine($"FLAME HIT start={start} hit={hit.point} normal={hit.normal}");
            }
        File.WriteAllText("Migration/jump-collision-audit.txt", report.ToString());
        var go = new GameObject("Audit Camera");
        var camera = go.AddComponent<Camera>();
        camera.farClipPlane = 2000;
        camera.transform.position = center + new Vector3(25, 30, -30);
        camera.transform.LookAt(center);
        MigrationAudit.Capture(camera, "Migration/Previews/JumpCollision.png");
        var overlay = new GameObject("Collider preview");
        overlay.transform.SetPositionAndRotation(flame.transform.position, flame.transform.rotation);
        overlay.transform.localScale = flame.transform.lossyScale;
        overlay.AddComponent<MeshFilter>().sharedMesh = flame.sharedMesh;
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", Color.cyan);
        overlay.AddComponent<MeshRenderer>().sharedMaterial = material;
        MigrationAudit.Capture(camera, "Migration/Previews/JumpCollisionCollider.png");
        Object.DestroyImmediate(overlay);
        Object.DestroyImmediate(material);
        Object.DestroyImmediate(go);
        EditorSceneManager.OpenScene("Assets/Scenes/WhiteScreen.unity");
        EditorApplication.Exit(0);
    }
}
