using System;
using UnityEngine;

public static class KartRunSettings
{
    public static string Mode => Argument("kart-mode", "train");
    public static string Run => Argument("kart-run", "local");
    public static int BaseSeed => int.TryParse(Argument("kart-seed", "42"), out var value) ? value : 42;
    public static int Limit => int.TryParse(Argument("kart-episodes", "0"), out var value) ? value : 0;
    public static int Episodes;
    public static bool StopRequested;
    public static int TotalDecisions;
    public static int Seed => BaseSeed + Episodes;
    public static string Argument(string name, string fallback)
    {
        string prefix = "--" + name + "=";
        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg.StartsWith(prefix, StringComparison.Ordinal)) return arg.Substring(prefix.Length);
        string environmentName = name.Replace('-', '_').ToUpperInvariant();
        return Environment.GetEnvironmentVariable(environmentName) ?? fallback;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Episodes = TotalDecisions = 0; StopRequested = false; }
}
