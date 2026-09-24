using System;
using BepInEx.Configuration;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>Read once at startup. Running jobs keep their saved duration.</summary>
internal sealed class Settings
{
    internal double CycleSeconds { get; }
    internal double ReclaimerSeconds { get; }
    internal double ReclaimerKW { get; }
    internal double WorkingKW { get; }
    internal double IdleKW { get; }
    internal bool ContinueQueue { get; }
    internal double TransferSeconds { get; }
    internal double CollectorSeconds { get; }
    internal double CollectorKW { get; }
    internal bool CollectorContinue { get; }
    internal double FeederSeconds { get; }
    internal double FeederKW { get; }
    internal bool FeederContinue { get; }
    internal KeyCode ControlsKey { get; }

    internal Settings(ConfigFile config)
    {
        FeederSeconds = Number(config, "Routing", "FeedSeconds", RoutingRules.FeedSeconds, 1, 60, Text.Get("Routing.setting_seconds"));
        FeederKW = Number(config, "Routing", "FeedKilowatts", RoutingRules.FeedKW, .1, 100, Text.Get("Routing.setting_kw"));
        FeederContinue = config.Bind("Routing", "ContinueFeeding", true, Text.Get("Routing.setting_continue")).Value;
        ReclaimerSeconds = Number(config, "Reclaimer", "CycleSeconds", ReclaimerRules.CycleSeconds, 30, ProcessRules.MaxJobSeconds, Text.Get("Reclaimer.setting_seconds"));
        ReclaimerKW = Number(config, "Reclaimer", "WorkingKilowatts", ReclaimerRules.WorkingKW, 1, 100, Text.Get("Reclaimer.setting_power"));
        CycleSeconds = Number(config, "Processing", "CycleSeconds", ProcessRules.CycleSeconds, ProcessRules.MinimumConfiguredCycleSeconds, ProcessRules.MaxJobSeconds,
            Text.Get("Settings.seconds_of_powered_work_for_a_new"));
        TransferSeconds = Number(config, "Intake", "TransferSeconds", IntakeRules.TransferSeconds, 1, 60,
            Text.Get("Settings.powered_seconds_to_move_one_detached_wall"));
        CollectorSeconds = Number(config, "Collector", "TransferSeconds", CollectorRules.CycleSeconds, 1, 60,
            Text.Get("Settings.powered_seconds_per_residue_packet_pausing_retains"));
        CollectorKW = Number(config, "Collector", "WorkingKilowatts", CollectorRules.WorkingKW, CollectorRules.IdleKW, 100,
            Text.Get("Settings.total_collector_operating_power_in_kw_idle"));
        CollectorContinue = config.Bind("Collector", "ContinueQueue", true,
            Text.Get("Settings.continue_collecting_residue_after_each_transfer_false")).Value;
        IdleKW = Number(config, "Power", "IdleKilowatts", ProcessRules.IdleKW, 0.01, 5,
            Text.Get("Settings.idle_electrical_demand_in_kw_must_remain"));
        WorkingKW = Math.Max(IdleKW, Number(config, "Power", "WorkingKilowatts", ProcessRules.ActiveKW, 1, 1000,
            Text.Get("Settings.working_electrical_demand_in_kw_including_idle")));
        ContinueQueue = config.Bind("Processing", "ContinueQueue", true,
            Text.Get("Settings.automatically_start_the_next_loaded_panel_after")).Value;
        ControlsKey = config.Bind("Controls", "WindowKey", KeyCode.F9,
            Text.Get("Settings.key_to_open_the_fixture_controls_while")).Value;
    }

    private static double Number(ConfigFile config, string section, string key, double fallback, double min, double max, string description)
    {
        double value = config.Bind(section, key, fallback,
            new ConfigDescription(description, new AcceptableValueRange<double>(min, max))).Value;
        return double.IsNaN(value) || double.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
    }
}
