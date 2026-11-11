using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class KartTrainingBoot : MonoBehaviour
{
    void Start()
    {
        Random.InitState(KartRunSettings.Seed);
        string track = KartRunSettings.Argument("kart-track", "moo");
        SceneManager.LoadScene(track == "mario" ? "MarioCircuitTraining" : "MooMooTraining");
    }
}
