using System;
using System.Collections.Generic;
using System.Globalization;
using Ostranauts.ShipGUIs.NavStation;
using Ostranauts.ShipGUIs.Utilities;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Owns only an already-running native reactor's flight controls. FusionIC still
// owns ignition, reactant use, heat, wear, power generation and actual thrust.
internal sealed class TorchDriveController
{
    internal const string Panel = "Panel A", Cycle = "slidCycle", Flow = "slidFlow", Ratio = "knobRatio";
    private Ship? ship;
    private CondOwner? reactor;
    private readonly Dictionary<string, string> idle = new(), commanded = new();
    private double forceLimit, leaseUntil, commandHeading;
    private bool writing;
    internal string Reason { get; private set; } = "Torch.rcs";
    internal bool HasPendingBurn => forceLimit > 0;
    internal void Align() { Cut(); Reason = "Torch.aligning"; }
    internal bool Owns(Ship? other) => ship != null && ship == other;
    internal bool OwnsReactor(CondOwner? other) => reactor != null && reactor == other;
    internal static bool ThrustRequested(Ship other) => other.IsUsingTorchDrive ||
        (other.Reactor != null && Parse(other.Reactor, Cycle) > 0);

    internal bool ChangedByPilot(Ship other, string key, string value) => !writing && Owns(other) &&
        (key == Cycle || key == Flow || key == Ratio) && Read(reactor!, key) != value;

    internal bool ControlsChanged => reactor != null && Ready(reactor) && !IsNoWake(reactor) &&
        commanded.Count == 3 && (Read(reactor, Cycle) != commanded[Cycle] ||
            Read(reactor, Flow) != commanded[Flow] || Read(reactor, Ratio) != commanded[Ratio]);

    internal bool Available(Ship candidate, bool preferred, double dt, out double acceleration)
    {
        acceleration = 0;
        Reason = "Torch.rcs";
        if (!preferred || !Plugin.PreferTorch.Value) return false;
        var core = candidate.Reactor;
        Reason = "Torch.unavailable";
        if (core == null || !Ready(core) || candidate.bCheckFusion || candidate.IsDocked() ||
            !candidate.bFusionReactorRunning || !ArrivalBrake.Finite(candidate.fShallowFusionRemain) ||
            candidate.fShallowFusionRemain <= dt + TorchRules.ZoneRefreshSeconds ||
            core.GetComponent<FusionIC>() == null || !ValidControls(core)) return false;
        if (!Owns(candidate) && ThrustRequested(candidate)) return false;
        if (reactor != null && reactor != core) return false;
        double maxG = Plugin.TorchMaximumG.Value;
        double full = candidate.GetMaxTorchThrust(NavModTorchDrive.GetLimiterSafetyMax(candidate)) / AutoNavCore.M_TO_AU;
        if (!TorchRules.Finite(full, maxG) || full <= 0 || maxG <= 0 || maxG > 2) return false;
        acceleration = Math.Min(full, maxG * TorchRules.StandardGravity);
        Reason = "Torch.zone";
        if (IsNoWake(core) || !ZoneClear(candidate, acceleration, dt)) return false;
        Reason = "Torch.ready";
        return true;
    }

    internal bool Burn(Ship candidate, double acceleration, double dt)
    {
        if (!Available(candidate, true, dt, out double maximum) || !ArrivalBrake.Finite(acceleration) || acceleration <= 0)
        { Cut(); return false; }
        var core = candidate.Reactor;
        if (ship == null)
        {
            ship = candidate; reactor = core;
            foreach (string key in new[] { Flow, Ratio }) idle[key] = Read(core, key);
            idle[Cycle] = "0";
        }
        acceleration = Math.Min(acceleration, maximum);
        // Native limiter is nonlinear: invert it instead of treating CYCLE as
        // a linear throttle. Keep the native 2 g safety ceiling even if overridden manually.
        float low = 0, high = NavModTorchDrive.GetLimiterSafetyMax(candidate);
        double temperature = core.GetCondAmount("StatICCoreTemp") / TorchRules.NativeCoreTemperature;
        for (int i = 0; i < TorchRules.LimiterSearchIterations; i++)
        {
            float mid = (low + high) / 2;
            if (candidate.GetMaxTorchThrust(mid) / AutoNavCore.M_TO_AU * temperature > acceleration) high = mid;
            else low = mid;
        }
        double flow = NavData.GetFLOWforCYCLE(core, low);
        if (!TorchRules.Finite(flow, candidate.Mass) || flow < 0 || flow > 1 || candidate.Mass <= 0 || low <= 0)
        { Reason = "Torch.unavailable"; Cut(); return false; }
        forceLimit = acceleration * candidate.Mass;
        commandHeading = candidate.objSS.fRot;
        leaseUntil = StarSystem.fEpoch + dt + TorchRules.ZoneRefreshSeconds;
        Write(Ratio, "1"); Write(Flow, Number(flow)); Write(Cycle, Number(low));
        core.GetComponent<FusionIC>().CatchUp(); // Native scheduler decides whether a fuel/heat update is due.
        if (!Ready(core) || IsNoWake(core)) { Reason = "Torch.unavailable"; Cut(); return false; }
        // Carry forward only thrust already supplied by native FusionIC. Reorient
        // and cap it for this physics step; this never manufactures positive thrust.
        double supplied = candidate.objSS.vAccIn.magnitude / AutoNavCore.M_TO_AU * candidate.Mass;
        if (!ArrivalBrake.Finite(supplied)) { Cut(); return false; }
        candidate.SetThrust(Math.Min(supplied, forceLimit));
        Reason = candidate.IsUsingTorchDrive ? "Torch.burning" : "Torch.waiting";
        return candidate.IsUsingTorchDrive;
    }

    // Fusion runs on a different update boundary to ShipSitu.TimeAdvance. A
    // short-lived permission prevents an old reactor command extending a burn.
    internal void FilterThrust(Ship candidate, ref double force)
    {
        if (!Owns(candidate) || force <= 0) return;
        try { CheckThrust(candidate, ref force); }
        catch { force = 0; Reason = "Torch.zone"; } // Unknown legality must not escape into FusionIC.Update.
    }

    private void CheckThrust(Ship candidate, ref double force)
    {
        if (candidate.objSS == null || candidate.bDestroyed) { force = 0; return; }
        double horizon = leaseUntil - StarSystem.fEpoch;
        double maxG = Plugin.TorchMaximumG.Value;
        double headingError = Math.Atan2(Math.Sin(candidate.objSS.fRot - commandHeading), Math.Cos(candidate.objSS.fRot - commandHeading));
        if (!TorchRules.Finite(force, horizon, maxG, candidate.Mass) || maxG <= 0 || maxG > 2 || candidate.Mass <= 0 || forceLimit <= 0 || horizon <= 0 ||
            CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || !AutoNavCore.Engaged ||
            !Plugin.Enabled.Value || !Plugin.PreferTorch.Value || reactor == null || !Ready(reactor) ||
            IsNoWake(reactor) || candidate.IsDocked() ||
            !TorchRules.Aligned(headingError, candidate.objSS.fW, horizon) ||
            !ZoneClear(candidate, forceLimit / candidate.Mass, horizon)) force = 0;
        else force = Math.Min(force, Math.Min(forceLimit, maxG * TorchRules.StandardGravity * candidate.Mass));
    }

    internal void Cut()
    {
        forceLimit = 0; leaseUntil = 0;
        if (Reason == "Torch.burning" || Reason == "Torch.ready" || Reason == "Torch.waiting" || Reason == "Torch.aligning") Reason = "Torch.rcs";
        if (ship == null || reactor == null) return;
        // Never restart a stopped/damaged reactor while relinquishing controls.
        try
        {
            if (!reactor.bDestroyed)
            {
                Write(Cycle, "0");
                if (Ready(reactor)) { Write(Flow, idle[Flow]); Write(Ratio, IsNoWake(reactor) ? "0" : idle[Ratio]); }
            }
        }
        finally { ship.SetThrust(0); }
    }

    internal void Release() { try { Cut(); } finally { Reset(); } }
    internal void YieldToPilot()
    {
        // Called before a native manual command executes. Clear OUR cycle, but
        // retain the current flow/mode so the incoming cycle command can work.
        forceLimit = leaseUntil = 0;
        try { if (reactor != null && !reactor.bDestroyed) Write(Cycle, "0"); }
        finally { try { ship?.SetThrust(0); } finally { Reset(); } }
    }
    // World teardown: references only, no writes to the departing save/world.
    internal void Reset()
    {
        ship = null; reactor = null; idle.Clear(); commanded.Clear(); forceLimit = leaseUntil = 0;
        Reason = "Torch.rcs";
    }

    internal void PrepareSavedControls(CondOwner co, JsonItem saved)
    {
        if (!OwnsReactor(co) || saved?.aGPMSettings == null) return;
        foreach (var map in saved.aGPMSettings)
        {
            if (map.strName != Panel) continue;
            var props = DataHandler.ConvertStringArrayToDict(map.dictGUIPropMap);
            foreach (var entry in idle) props[entry.Key] = entry.Value;
            map.dictGUIPropMap = DataHandler.ConvertDictToStringArray(props);
        }
    }

    private static bool Ready(CondOwner core)
    {
        double temperature = core.GetCondAmount("StatICCoreTemp") / TorchRules.NativeCoreTemperature;
        return !core.bDestroyed && core.HasCond("IsInstalled") && core.HasCond("IsReadyFusion") &&
            !core.HasCond("IsOff") && !core.HasCond("IsOverrideOff") && !core.HasCond("IsShuttingDown") && !core.HasCond("IsDamaged") &&
            ArrivalBrake.Finite(temperature) && Math.Abs(temperature - 1) <= TorchRules.CoreTemperatureTolerance;
    }

    private bool ZoneClear(Ship candidate, double acceleration, double dt)
    {
        if (CrewSim.system == null || candidate.objSS == null || !TorchRules.Finite(dt, acceleration, StarSystem.fEpoch) || dt <= 0) return false;
        // Native legality remains authoritative; our projected check additionally
        // prevents crossing a boundary during one accelerated simulation step.
        if (CrewSim.system.IsWithinNoWakeRangeOfAnyStation(candidate.objSS, StarSystem.fEpoch)) return false;
        var own = candidate.objSS;
        double horizon = dt + TorchRules.ZoneRefreshSeconds;
        foreach (var station in CrewSim.system.GetStations())
        {
            if (station == candidate || station.bDestroyed || station.IsNotAFullStation) continue;
            var other = station.objSS;
            if (other == null) return false;
            double otherAcceleration = (other.vAccEx.magnitude + other.vAccIn.magnitude + other.vAccRCS.magnitude) / AutoNavCore.M_TO_AU;
            double ownAcceleration = own.vAccEx.magnitude / AutoNavCore.M_TO_AU + acceleration;
            if (!TorchRules.ZoneClear((own.vPosx - other.vPosx) / AutoNavCore.M_TO_AU,
                (own.vPosy - other.vPosy) / AutoNavCore.M_TO_AU,
                (own.vVelX - other.vVelX) / AutoNavCore.M_TO_AU, (own.vVelY - other.vVelY) / AutoNavCore.M_TO_AU,
                ownAcceleration + otherAcceleration, horizon,
                // Native updates orbit-locked station positions at the new global
                // epoch before all free ships have taken their step. Inflate for
                // that whole-step position uncertainty rather than assuming simultaneity.
                Math.Sqrt(other.vVelX * other.vVelX + other.vVelY * other.vVelY) / AutoNavCore.M_TO_AU * horizon)) return false;
        }
        return true;
    }

    private void Write(string key, string value)
    {
        writing = true;
        try { reactor!.ApplyGPMChanges(new[] { Panel + "," + key + "," + value }); commanded[key] = value; }
        finally { writing = false; }
    }
    private static string Read(CondOwner core, string key) => core.GetGPMInfo(Panel, key);
    private static double Parse(CondOwner core, string key) => double.TryParse(Read(core, key), NumberStyles.Float,
        CultureInfo.InvariantCulture, out var value) ? value : double.NaN;
    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static bool IsNoWake(CondOwner core) => !bool.TryParse(Read(core, "bNWZ"), out bool restricted) || restricted;
    private static bool ValidControls(CondOwner core) => TorchRules.Finite(Parse(core, Cycle), Parse(core, Flow), Parse(core, Ratio)) &&
        Parse(core, Cycle) >= 0 && Parse(core, Cycle) <= 1 && Parse(core, Flow) >= 0 && Parse(core, Flow) <= 1 &&
        (Parse(core, Ratio) == 0 || Parse(core, Ratio) == 1);
}
