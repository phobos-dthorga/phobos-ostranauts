using System;
using BepInEx.Configuration;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>Read once at startup. Running jobs keep their saved duration.</summary>
internal sealed class Settings
{
    internal double CycleSeconds { get; }
    internal double WorkingKW { get; }
    internal double IdleKW { get; }
    internal bool ContinueQueue { get; }
    internal KeyCode ControlsKey { get; }

    internal Settings(ConfigFile config)
    {
        CycleSeconds = Number(config, "Processing", "CycleSeconds", ProcessRules.CycleSeconds, 10, 3600,
            "Seconds of powered work for a NEW panel. Started panels keep their saved duration. Restart the game after editing.");
        IdleKW = Number(config, "Power", "IdleKilowatts", ProcessRules.IdleKW, 0.01, 5,
            "Idle electrical demand in kW. Must remain positive for native power control. Restart after editing.");
        WorkingKW = Math.Max(IdleKW, Number(config, "Power", "WorkingKilowatts", ProcessRules.ActiveKW, 1, 1000,
            "Working electrical demand in kW, including idle demand. Cannot be below idle demand. Applies to all panels after restart."));
        ContinueQueue = config.Bind("Processing", "ContinueQueue", true,
            "Automatically start the next loaded panel after completion. False processes one panel per Start command. Restart after editing.").Value;
        ControlsKey = config.Bind("Controls", "WindowKey", KeyCode.F9,
            "Key to open the fixture controls while not typing. Choose an unused key; restart after editing.").Value;
    }

    private static double Number(ConfigFile config, string section, string key, double fallback, double min, double max, string description)
    {
        double value = config.Bind(section, key, fallback,
            new ConfigDescription(description, new AcceptableValueRange<double>(min, max))).Value;
        return double.IsNaN(value) || double.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
    }
}
