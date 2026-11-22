using System;
using UnityEngine;

public static class KartDriveInputChecks
{
    public static void Run()
    {
        var first = new KartDriveInput();
        var second = new KartDriveInput();
        first.SetAutonomous(true);
        second.SetAutonomous(true);
        first.SetRaceActions(.6f, .8f, true);
        first.BeginPhysicsStep();
        if (!first.GetKeyDown(KeyCode.V) || !first.GetKeyDown(KeyCode.V) || second.GetKey(KeyCode.Space))
            throw new Exception("Button edges or per-kart input isolation failed.");
        // Many rendered reads must not duplicate the press on the next physics tick.
        for (int i = 0; i < 144; i++) first.GetKeyDown(KeyCode.V);
        first.BeginPhysicsStep();
        if (first.GetKeyDown(KeyCode.V) || !first.GetKey(KeyCode.V)) throw new Exception("Repeated drift press.");
        first.SetRaceActions(-.6f, 0, false);
        first.BeginPhysicsStep();
        if (!first.GetKeyUp(KeyCode.V) || first.GetKey(KeyCode.Space) || first.GetKey(KeyCode.S))
            throw new Exception("Drift release or coast failed.");
        first.BeginPhysicsStep();
        if (first.GetKeyUp(KeyCode.V)) throw new Exception("Repeated release.");
        first.SetRaceActions(.001f, .5f, true);
        first.BeginPhysicsStep();
        if (first.GetAxis("Horizontal") != 0 || first.GetKey(KeyCode.RightArrow) || first.SteeringGain != 0 || first.GetKey(KeyCode.V))
            throw new Exception("Neutral steering still applies drift forces.");
        first.SetRaceActions(.5f, .5f, true);
        first.BeginPhysicsStep();
        if (first.GetAxis("Horizontal") != .5f || first.SteeringGain != .5f || first.Throttle != .5f || !first.GetKey(KeyCode.RightArrow) || !first.GetKeyDown(KeyCode.V))
            throw new Exception("Analog race controls lost their strength.");
        Debug.Log("KartDriveInputChecks PASS v2: analog strengths, neutral drift excluded, isolated physics-clock edges.");
    }
}
