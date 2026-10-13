using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class CharacterAudit
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        var player=Object.FindAnyObjectByType<Player>();
        var imported=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Mario.fbx");
        var lines=new System.Text.StringBuilder();
        foreach(var root in new[]{player.gameObject,imported})
        {
            lines.AppendLine("ROOT "+root.name);
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                lines.AppendLine($"SKIN {skin.name} mesh={skin.sharedMesh?.name} bones={skin.bones.Length} local={skin.transform.localPosition} rotation={skin.transform.localEulerAngles} scale={skin.transform.localScale} bounds={skin.localBounds}");
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s=>s.name.StartsWith("Body__"))) { lines.AppendLine("BONES "+skin.name+": "+string.Join(",",skin.bones.Select(b=>b==null?"NULL":b.name))); foreach(var b in skin.bones.Take(5)) if(b!=null) lines.AppendLine("POSE "+b.name+" pos="+b.localPosition.ToString("F5")+" rot="+b.localEulerAngles+" scale="+b.localScale.ToString("F5")); } foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(x=>x.name=="Armature"||x.name=="Skl_Root"||x.name=="Head"||x.name=="MarioModel"))
                lines.AppendLine($"BONE {t.name}: local={t.localPosition} rotation={t.localEulerAngles} scale={t.localScale}");
        }
        File.WriteAllText("Migration/character-audit.txt",lines.ToString());EditorApplication.Exit(0);
    }
}

