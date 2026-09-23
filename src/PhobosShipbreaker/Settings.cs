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
    internal double TransferSeconds { get; }
    internal double CollectorSeconds { get; }
    internal double CollectorKW { get; }
    internal bool CollectorContinue { get; }
    internal KeyCode ControlsKey { get; }

    internal Settings(ConfigFile config)
    {
        CycleSeconds = Number(config, "Processing", "CycleSeconds", ProcessRules.CycleSeconds, 10, 3600,
            "Seconds of powered work for a NEW panel. Started panels keep their saved duration. Restart the game after editing.");
        TransferSeconds = Number(config, "Intake", "TransferSeconds", IntakeRules.TransferSeconds, 1, 60,
            "Powered seconds to move one detached wall through the chute. Pending moves stay in the grabber; reload resets only this short delay. Restart after editing.");
        CollectorSeconds = Number(config, "Collector", "TransferSeconds", CollectorRules.CycleSeconds, 1, 60,
            "Powered seconds per residue packet. Pausing retains this short clock in-session; reload resets the clock and pauses; saved endpoint links remain. Restart after editing.");
        CollectorKW = Number(config, "Collector", "WorkingKilowatts", CollectorRules.WorkingKW, CollectorRules.IdleKW, 100,
            "Total collector operating power in kW; idle is 0.05 kW. Restart after editing.");
        CollectorContinue = config.Bind("Collector", "ContinueQueue", true,
            "Continue collecting residue after each transfer. False collects one packet per Start. Always paused after reload. Restart after editing.").Value;
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
