using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KartEpisodeSetup
{
    public static void PrepareMooMooMeadows()
    {
        TrackProgressChecks.Run();
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        Enable();
        EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("Mario Kart/Apprentissage/Activer dans le circuit ouvert")]
    public static void Enable()
    {
        if (EditorApplication.isPlaying) return;
        var player = Object.FindAnyObjectByType<Player>();
        if (!player || !player.GetComponent<LapCounter>())
        {
            EditorUtility.DisplayDialog("Apprentissage", "Ouvre d'abord la scene MooMooMeadows ou Mario Circuit.", "OK");
            return;
        }
        if (!player.GetComponent<TrackProgress>()) Undo.AddComponent<TrackProgress>(player.gameObject);
        if (!player.GetComponent<KartEpisode>()) Undo.AddComponent<KartEpisode>(player.gameObject);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Selection.activeGameObject = player.gameObject;
        Debug.Log("Mode apprentissage active. Enregistre la scene puis lance Play. F5 recommence un essai.");
    }
}

