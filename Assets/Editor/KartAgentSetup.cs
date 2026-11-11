using UnityEditor;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;

public static class KartAgentSetup
{
    [MenuItem("Mario Kart/IA/Ajouter un kart IA en Play")]
    static void SpawnInPlay()
    {
        var human = GameObject.FindGameObjectWithTag("Player");
        if (!human || !human.TryGetComponent<Player>(out var player) || player.Controls.Autonomous)
            throw new System.InvalidOperationException("Lancer un circuit avec le kart joueur.");
        SpawnBeside(player);
    }

    [MenuItem("Mario Kart/IA/Ajouter un kart IA en Play", true)]
    static bool CanSpawn() => EditorApplication.isPlaying && RACE_MANAGER.RACE_STARTED;

    public static Player SpawnBeside(Player human)
    {
        var ai = Object.Instantiate(human, human.transform.position - human.transform.right * 5, human.transform.rotation);
        ai.name = "Kart IA (démo)";
        ai.tag = "Untagged";
        Configure(ai.gameObject);
        return ai;
    }
    [MenuItem("Mario Kart/IA/Configurer le kart sélectionné")]
    static void ConfigureSelected()
    {
        var kart = Selection.activeGameObject;
        if (!kart || !kart.GetComponent<Player>())
            throw new System.InvalidOperationException("Sélectionner la racine du kart avec le composant Player.");
        Configure(kart);
    }

    public static KartAgent Configure(GameObject kart)
    {
        var behavior = kart.GetComponent<BehaviorParameters>() ?? Undo.AddComponent<BehaviorParameters>(kart);
        Undo.RecordObject(behavior, "Configurer KartPPO");
        behavior.BehaviorName = KartAgent.Behavior;
        behavior.BrainParameters.VectorObservationSize = KartAgent.ObservationCount;
        behavior.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(2);
        var agent = kart.GetComponent<KartAgent>() ?? Undo.AddComponent<KartAgent>(kart);
        var requester = kart.GetComponent<DecisionRequester>() ?? Undo.AddComponent<DecisionRequester>(kart);
        Undo.RecordObject(requester, "Configurer décisions");
        requester.DecisionPeriod = 5;
        requester.TakeActionsBetweenDecisions = true;
        var assist = kart.GetComponent<SoloPlayerAssist>();
        if (assist) { Undo.RecordObject(assist, "Désactiver assistance clavier du kart IA"); assist.enabled = false; }
        return agent;
    }
}
