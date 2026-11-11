using UnityEngine;

public static class KartForwardControlChecks
{
    public static void Run()
    {
        bool originalControl = KartKeyboard.AgentControl;
        float originalThrottle = KartKeyboard.AgentThrottle, originalTurn = KartKeyboard.AgentSteering;
        bool originalDrift = KartKeyboard.AgentDrift;
        try
        {
            foreach (float action in new[] { -10f, -1f, -0.5f, 0f, 0.5f, 1f, 10f })
            {
                float throttle = KartAgent.ForwardThrottle(action);
                KartKeyboard.SetAgentInput(throttle, 0.25f, false);
                if (throttle < 0 || throttle > 1 || KartKeyboard.GetKey(KeyCode.S) || KartKeyboard.GetKey(KeyCode.DownArrow))
                    throw new System.Exception("La commande IA peut encore activer le recul.");
            }
            KartKeyboard.SetAgentInput(KartAgent.ForwardThrottle(-1), 0, false);
            if (KartKeyboard.GetKey(KeyCode.Space)) throw new System.Exception("Le kart ne peut pas relacher l'accelerateur.");
            KartKeyboard.SetAgentInput(KartAgent.ForwardThrottle(1), -0.75f, false);
            if (!KartKeyboard.GetKey(KeyCode.Space) || KartKeyboard.GetAxis("Horizontal") != -0.75f)
                throw new System.Exception("Acceleration ou direction incorrecte.");
            Debug.Log("KartForwardControlChecks PASS : aucun recul, acceleration et ralentissement disponibles.");
        }
        finally
        {
            KartKeyboard.SetAgentInput(originalThrottle, originalTurn, originalDrift);
            KartKeyboard.AgentControl = originalControl;
        }
    }
}
