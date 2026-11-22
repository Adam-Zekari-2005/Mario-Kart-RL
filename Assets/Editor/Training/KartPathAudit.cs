using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KartPathAudit
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        var player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        var path = player.GetComponent<ItemManager>().path;
        int road = 0, grass = 0, missing = 0;
        for (int i = 0; i < path.childCount; i++)
        {
            Vector3 a = path.GetChild(i).position, b = path.GetChild((i + 1) % path.childCount).position;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / 5));
            for (int j = 0; j < steps; j++)
            {
                Vector3 p = Vector3.Lerp(a, b, j / (float)steps);
                float closest = float.PositiveInfinity;
                int tag = -1;
                foreach (var hit in Physics.RaycastAll(p + Vector3.up * 30, Vector3.down, 70, player.mask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.distance >= closest || hit.normal.y < .4f ||
                        (!hit.collider.CompareTag("Ground") && !hit.collider.CompareTag("Dirt"))) continue;
                    closest = hit.distance;
                    tag = hit.collider.CompareTag("Ground") ? 1 : 0;
                }
                if (tag == 1) road++; else if (tag == 0) grass++; else missing++;
            }
        }
        string result = $"path={path.name}, nodes={path.childCount}, road={road}, grass={grass}, missing={missing}";
        Directory.CreateDirectory("Training/Reports");
        File.WriteAllText("Training/Reports/duel-path-audit.txt", result);
        Debug.Log("KART PATH AUDIT " + result);
    }
}
