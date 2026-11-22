using UnityEditor;
using UnityEditor.SceneManagement;

public static class KartDuelPreview
{
    [MenuItem("Mario Kart/IA/Jouer contre Luigi (modele PPO)")]
    public static void Play()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        System.Environment.SetEnvironmentVariable("KART_TRACK", "moo");
        System.Environment.SetEnvironmentVariable("KART_MODE", "model");
        EditorSceneManager.OpenScene("Assets/Scenes/MooMooMeadows.unity");
        EditorApplication.isPlaying = true;
    }
}
