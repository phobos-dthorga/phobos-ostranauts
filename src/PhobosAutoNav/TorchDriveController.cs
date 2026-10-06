using System;
using System.Collections.Generic;
using Ostranauts.ShipGUIs.NavStation;
using Phobos.Ostranauts.Framework.Processing;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

/// <summary>Where a requested burn stands after this step.</summary>
internal enum BurnState
{
    /// <summary>No burn: controls are idle or being handed back; RCS carries the correction.</summary>
    Refused,
    /// <summary>Controls are written and the lease is live; the reactor's own update has not delivered thrust yet.</summary>
    Pending,
    /// <summary>The reactor is delivering thrust under this controller's cap.</summary>
    Burning
}

// Owns only an already-running native reactor's flight controls, driving Flow and Cycle the way the
// game's long-range course plot does (Framework ReactorRules). FusionIC still owns ignition, reactant
// use, heat, wear, power generation and actual thrust.
internal sealed partial class TorchDriveController
{
    partial void CheckObstacleBurn(Ship candidate,double acceleration,double dt,ref bool allowed);
    internal const string Panel = ReactorRules.Panel, Cycle = ReactorRules.Cycle, Flow = ReactorRules.Flow, Ratio = ReactorRules.Ratio;
    private Ship? ship;
    private CondOwner? reactor;
    private ReactorControls? controls;
    private double forceLimit, leaseUntil, commandHeading, pendingSince = double.NaN, holdoffUntil = double.NegativeInfinity;
    private bool coreFault;
    internal string Reason { get; private set; } = "Torch.rcs";
    internal ContactReading? ContactLoss { get; private set; }
    // Set by the plugin: a native sensor refresh right after Auto Nav switched sensors on withholds
    // the burn for that step instead of latching a contact loss.
    internal static Func<Ship, ContactReading, bool>? SensorSettling { get; set; }
    // Set by the plugin: tells the crew (log line, nav-map banner) that the torch stopped for a reactor reason.
    internal static Action<Ship, string, string>? Notify { get; set; }
    internal bool HasPendingBurn => forceLimit > 0;
    /// <summary>The last cycle and flow this controller commanded, for instruments; NaN when idle.</summary>
    internal double CommandedCycle { get; private set; } = double.NaN;
    internal double CommandedFlow { get; private set; } = double.NaN;
    internal void Align() { Cut(); Reason = "Torch.aligning"; }
    internal bool Owns(Ship? other) => ship != null && ship == other;
    internal bool OwnsReactor(CondOwner? other) => reactor != null && reactor == other;
    internal static bool ThrustRequested(Ship other) => other.IsUsingTorchDrive ||
        (other.Reactor != null && ReactorRules.Number(PanelOf(other.Reactor), Cycle) > 0);

    internal bool ChangedByPilot(Ship other, string key, string value) => Owns(other) && controls != null && controls.ChangedByPilot(key, value);

    internal bool ControlsChanged => reactor != null && controls != null && Ready(reactor) && !IsNoWake(reactor) && controls.Differs;

    internal bool Available(Ship candidate, bool preferred, double dt, out double acceleration)
    {
        acceleration = 0;
        Reason = "Torch.rcs";
        if (ContactLoss is ContactReading lost) { Reason = lost.MessageKey; return false; }
        if (!preferred || !Plugin.PreferTorch.Value) return false;
        var core = candidate.Reactor;
        Reason = "Torch.unavailable";
        if (core == null || candidate.bCheckFusion || TowFlight.Problem(candidate) != null ||
            !candidate.bFusionReactorRunning || !ArrivalBrake.Finite(candidate.fShallowFusionRemain) ||
            candidate.fShallowFusionRemain <= dt + TorchRules.ZoneRefreshSeconds ||
            core.GetComponent<FusionIC>() == null || !ValidControls(core)) return false;
        if (!CoreUsable(core)) { Reason = "Torch.core"; return false; }
        if (!Ready(core)) return false;
        if (!Owns(candidate) && ThrustRequested(candidate)) return false;
        if (reactor != null && reactor != core) return false;
        double maxG = Plugin.TorchMaximumG.Value;
        double full = TorchCycle.Acceleration(candidate, TorchSafetyOn(candidate)) / AutoNavCore.M_TO_AU;
        if (!TorchRules.Finite(full, maxG) || full <= 0 || maxG <= 0 || maxG > 2) return false;
        acceleration = Math.Min(full, maxG * TorchRules.StandardGravity);
        Reason = "Torch.zone";
        if (IsNoWake(core) || !ZoneClear(candidate, acceleration, dt)) return false;
        Reason = "Torch.ready";
        return true;
    }

    // The course plot aborts outside the 0.8-1.2 core band. Here that stops the torch (RCS carries on)
    // and new burns wait until the core is back inside the correction band, so a marginal core cannot
    // flap the reactor controls every step.
    private bool CoreUsable(CondOwner core)
    {
        double ratio = ReactorRules.CoreRatio(core.GetCondAmount(ReactorRules.CoreTemperature));
        if (coreFault)
        {
            if (!ReactorRules.WithinBand(ratio, ReactorRules.CoreCorrectionBand)) return false;
            coreFault = false;
        }
        return ReactorRules.WithinBand(ratio, ReactorRules.CoreAbortBand);
    }

    // The console's torch safety keeps the game's 2 g limiter; a pilot who has switched it off may use the whole slider.
    private static float CycleLimit(Ship candidate) => TorchCycle.Limit(candidate, TorchSafetyOn(candidate));
    private static bool TorchSafetyOn(Ship candidate)
    {
        try
        {
            var console = Plugin.Service.Console;
            if (console == null || console.ship != candidate || !console.mapGUIPropMaps.TryGetValue(Panel, out var map) ||
                !map.TryGetValue("bTorchSafety", out var safe) || !bool.TryParse(safe, out bool on)) return true;
            return on;
        }
        catch { return true; }
    }

    internal BurnState Burn(Ship candidate, double acceleration, double dt)
    {
        if (!Available(candidate, true, dt, out double maximum) || !ArrivalBrake.Finite(acceleration) || acceleration <= 0)
        { Cut(); return BurnState.Refused; }
        if (StarSystem.fEpoch < holdoffUntil) { Reason = "Torch.unavailable"; return BurnState.Refused; }
        var core = candidate.Reactor;
        if (ship == null)
        {
            ship = candidate; reactor = core;
            controls = new ReactorControls(PanelOf(core));
            controls.Adopt();
        }
        acceleration = Math.Min(acceleration, maximum);
        bool obstacleAllowed=true; CheckObstacleBurn(candidate,acceleration,dt,ref obstacleAllowed);
        if(!obstacleAllowed) { Cut(); Reason="Avoidance.torch"; return BurnState.Refused; }
        // Native limiter is nonlinear: invert it instead of treating CYCLE as
        // a linear throttle. Keep the native 2 g safety ceiling unless the pilot has released it.
        float low = 0, high = CycleLimit(candidate);
        double temperature = core.GetCondAmount(ReactorRules.CoreTemperature) / TorchRules.NativeCoreTemperature;
        for (int i = 0; i < TorchRules.LimiterSearchIterations; i++)
        {
            float mid = (low + high) / 2;
            if (candidate.GetMaxTorchThrust(mid) / AutoNavCore.M_TO_AU * temperature > acceleration) high = mid;
            else low = mid;
        }
        if (!TorchRules.Finite(low, candidate.Mass) || candidate.Mass <= 0 || low <= 0)
        { Reason = "Torch.unavailable"; Cut(); return BurnState.Refused; }
        double flow = RegulatedFlow(candidate, core, low);
        if (!TorchRules.Finite(flow) || flow < 0 || flow > 1)
        { Reason = "Torch.unavailable"; Cut(); return BurnState.Refused; }
        bool starting = forceLimit <= 0;
        forceLimit = acceleration * candidate.Mass;
        commandHeading = candidate.objSS.fRot;
        leaseUntil = StarSystem.fEpoch + dt + TorchRules.ZoneRefreshSeconds;
        controls!.WriteIfChanged(Ratio, "1");
        controls.WriteIfChanged(Flow, ReactorRules.Text(flow));
        controls.WriteIfChanged(Cycle, ReactorRules.Text(low));
        CommandedCycle = low; CommandedFlow = flow;
        core.GetComponent<FusionIC>().CatchUp(); // Native scheduler decides whether a fuel/heat update is due.
        if (!Ready(core) || IsNoWake(core)) { Reason = "Torch.unavailable"; Cut(); return BurnState.Refused; }
        // Carry forward only thrust already supplied by native FusionIC. Reorient
        // and cap it for this physics step; this never manufactures positive thrust.
        double supplied = candidate.objSS.vAccIn.magnitude / AutoNavCore.M_TO_AU * candidate.Mass;
        if (!ArrivalBrake.Finite(supplied)) { Cut(); return BurnState.Refused; }
        if (supplied > 0)
        {
            candidate.SetThrust(Math.Min(supplied, forceLimit));
            if (candidate.IsUsingTorchDrive) { pendingSince = double.NaN; Reason = "Torch.burning"; return BurnState.Burning; }
        }
        // The command stays written for the reactor's own next update; a reactor that accepts the
        // controls but delivers nothing within the startup wait falls back to RCS for a while.
        if (starting || double.IsNaN(pendingSince)) pendingSince = StarSystem.fEpoch;
        if (StarSystem.fEpoch - pendingSince > TorchRules.StartupWaitSeconds)
        {
            holdoffUntil = StarSystem.fEpoch + TorchRules.StartupHoldoffSeconds;
            Cut(); Reason = "Torch.unavailable"; return BurnState.Refused;
        }
        Reason = "Torch.waiting";
        return BurnState.Pending;
    }

    // Flow follows the course plot: the native flow-for-cycle value when a burn starts, then the vanilla
    // adjustment toward the ideal core and the thrust target. A pilot who has just moved the flow slider
    // keeps it for the same grace the game gives.
    private double RegulatedFlow(Ship candidate, CondOwner core, double cycle)
    {
        var panel = PanelOf(core);
        double current = ReactorRules.Number(panel, Flow);
        if (forceLimit <= 0 || !TorchRules.Finite(current))
            return ReactorRules.InitialFlow(cycle, core.GetCondAmount(ReactorRules.PelletMaxTheory), core.GetCondAmount(ReactorRules.PelletMax));
        if (ReactorRules.PilotTouchedFlow(panel, StarSystem.fEpoch)) return current;
        double ratio = ReactorRules.CoreRatio(core.GetCondAmount(ReactorRules.CoreTemperature));
        return ReactorRules.AdjustFlow(ratio, current, candidate.objSS.vAccIn.magnitude, candidate.GetMaxTorchThrust((float)cycle));
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
        var sensing = ContactLoss ?? NativeContactReader.Read(candidate, AutoNavCore.EngagedTarget?.ShipId);
        if (!sensing.Usable)
        {
            if (ContactLoss == null && SensorSettling?.Invoke(candidate, sensing) == true) { force = 0; Reason = sensing.MessageKey; return; }
            // Do not allow a brief reacquisition to revive an earlier burn.
            // The navigation update consumes this loss and records suspension.
            ContactLoss = sensing; force = forceLimit = leaseUntil = 0;
            Reason = sensing.MessageKey; return;
        }
        double horizon = leaseUntil - StarSystem.fEpoch;
        bool obstacleAllowed=true; CheckObstacleBurn(candidate,force/candidate.Mass,Math.Max(.001,horizon),ref obstacleAllowed);
        if(!obstacleAllowed) { force=0; Reason="Avoidance.torch"; return; }
        double maxG = Plugin.TorchMaximumG.Value;
        double headingError = Math.Atan2(Math.Sin(candidate.objSS.fRot - commandHeading), Math.Cos(candidate.objSS.fRot - commandHeading));
        if (!TorchRules.Finite(force, horizon, maxG, candidate.Mass) || maxG <= 0 || maxG > 2 || candidate.Mass <= 0 || forceLimit <= 0 || horizon <= 0 ||
            CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || !AutoNavCore.Engaged ||
            !Plugin.Enabled.Value || !Plugin.PreferTorch.Value || reactor == null || !Ready(reactor) ||
            IsNoWake(reactor) || TowFlight.Problem(candidate) != null ||
            !TorchRules.Aligned(headingError, candidate.objSS.fW, horizon) ||
            !ZoneClear(candidate, forceLimit / candidate.Mass, horizon)) force = 0;
        else force = Math.Min(force, Math.Min(forceLimit, maxG * TorchRules.StandardGravity * candidate.Mass));
    }

    internal void Cut()
    {
        forceLimit = 0; leaseUntil = 0; pendingSince = double.NaN;
        CommandedCycle = CommandedFlow = double.NaN;
        if (Reason == "Torch.burning" || Reason == "Torch.ready" || Reason == "Torch.waiting" || Reason == "Torch.aligning") Reason = "Torch.rcs";
        if (ship == null || reactor == null || controls == null) return;
        // Never restart a stopped/damaged reactor while relinquishing controls. An intact reactor takes its
        // idle flow and mode back whatever its core is doing: the idle settings are what kept it stable.
        try
        {
            if (!reactor.bDestroyed) controls.RestoreIdle(ReactorRules.Intact(PanelOf(reactor)), IsNoWake(reactor));
        }
        finally { ship.SetThrust(0); }
    }

    /// <summary>Called each guarded step while this controller owns the reactor: a core that has left the
    /// abort band stops the torch, tells the crew once and keeps RCS guidance going.</summary>
    internal void WatchCore(Ship candidate)
    {
        if (!Owns(candidate) || reactor == null || coreFault) return;
        double ratio = ReactorRules.CoreRatio(reactor.GetCondAmount(ReactorRules.CoreTemperature));
        if (ReactorRules.WithinBand(ratio, ReactorRules.CoreAbortBand)) return;
        coreFault = true;
        Cut(); Reason = "Torch.core";
        try { Notify?.Invoke(candidate, "Torch.core", Text.Get("Torch.core_log", ratio)); } catch { }
    }

    internal void Release() { try { Cut(); } finally { Reset(); } }
    internal void YieldToPilot()
    {
        // Called before a native manual command executes. Clear OUR cycle, but
        // retain the current flow/mode so the incoming cycle command can work.
        forceLimit = leaseUntil = 0; pendingSince = double.NaN;
        try { if (reactor != null && !reactor.bDestroyed) controls?.ClearCycle(); }
        finally { try { ship?.SetThrust(0); } finally { Reset(); } }
    }
    // World teardown: references only, no writes to the departing save/world.
    internal void Reset()
    {
        controls?.Forget();
        ship = null; reactor = null; controls = null; forceLimit = leaseUntil = 0; pendingSince = double.NaN;
        holdoffUntil = double.NegativeInfinity; coreFault = false;
        CommandedCycle = CommandedFlow = double.NaN;
        ContactLoss = null;
        Reason = "Torch.rcs";
    }

    internal void PrepareSavedControls(CondOwner co, JsonItem saved)
    {
        if (!OwnsReactor(co) || controls == null || saved?.aGPMSettings == null) return;
        foreach (var map in saved.aGPMSettings)
        {
            if (map.strName != Panel) continue;
            var props = DataHandler.ConvertStringArrayToDict(map.dictGUIPropMap);
            foreach (var entry in controls.Idle) props[entry.Key] = entry.Value;
            map.dictGUIPropMap = DataHandler.ConvertDictToStringArray(props);
        }
    }

    internal static bool Ready(CondOwner core) => ReactorRules.Ready(PanelOf(core));

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
                Math.Sqrt(other.vVelX * other.vVelX + other.vVelY * other.vVelY) / AutoNavCore.M_TO_AU * horizon +
                (candidate.IsDocked() ? TowFlight.RadiusAU(candidate) / AutoNavCore.M_TO_AU : 0))) return false;
        }
        return true;
    }

    private static IReactorState PanelOf(CondOwner core) => new CondOwnerReactor(core);
    private static bool IsNoWake(CondOwner core) => ReactorRules.IsNoWake(PanelOf(core));
    private static bool ValidControls(CondOwner core) => ReactorRules.ValidControls(PanelOf(core));

    // The same adapter Framework's NativeReactor uses, over this assembly's view of the reactor
    // (offline checks substitute their reactor double here).
    private sealed class CondOwnerReactor : IReactorState
    {
        private readonly CondOwner core;
        internal CondOwnerReactor(CondOwner core) { this.core = core; }
        public bool Destroyed => core.bDestroyed;
        public bool Has(string condition) => core.HasCond(condition);
        public double CoreTemperatureMeV => core.GetCondAmount(ReactorRules.CoreTemperature);
        public string Read(string key) => core.GetGPMInfo(Panel, key) ?? "";
        public void Write(string key, string value) => core.ApplyGPMChanges(new[] { Panel + "," + key + "," + value });
    }
}
