using System;
using System.Collections.Generic;
using System.Globalization;
using Ostranauts.ShipGUIs.Utilities;

namespace Phobos.Ostranauts.Framework.Processing;

/// <summary>Facts about the game's fusion reactor and the rules its own long-range course plot applies
/// when it drives Flow and Cycle. Pure: no game types, so consumers and their offline checks share one copy.
/// Native evidence (Ostranauts 1.0.1.5, read locally, not redistributed): FusionIC.FUSION_PERIOD,
/// FUSION_CORE_IDEAL_MEV, FusionIC.Run wall damage, NavData.TimeAdvance, NavData.GetAdjustedFLOW,
/// NavData.GetFLOWforCYCLE and the NavModTorchDrive flow slider.</summary>
public static class ReactorRules
{
    /// <summary>FusionIC runs its fuel, heat and thrust update on this cadence, not every physics step.</summary>
    public const double FusionPeriodSeconds = 0.27;
    /// <summary>Core temperature the reactor is designed to hold; thrust and power scale with the ratio to it.</summary>
    public const double IdealCoreMeV = 0.725;
    /// <summary>The course plot rescales flow toward the ideal once the core is this far off (fraction of ideal).</summary>
    public const double CoreCorrectionBand = 0.05;
    /// <summary>The course plot aborts once the core ratio leaves 1 minus/plus this band.</summary>
    public const double CoreAbortBand = 0.2;
    /// <summary>FusionIC.Run adds wall damage whenever the core temperature exceeds this.</summary>
    public const double WallDamageMeV = 0.75;
    /// <summary>A pilot move of the flow slider suspends automatic flow adjustment for this long.</summary>
    public const double PilotFlowGraceSeconds = 2;
    /// <summary>Per-update flow nudge toward the thrust target once the core is inside the correction band.</summary>
    public const double FlowStep = 0.001;
    /// <summary>Thrust agreement within this fraction of the target needs no flow nudge.</summary>
    public const double ThrustAgreementFraction = 0.001;
    /// <summary>GetFLOWforCYCLE: cycle scaled by theoretical over actual pellet capacity, times this.</summary>
    public const double FlowForCycleFraction = 0.9;

    public const string Panel = "Panel A", Cycle = "slidCycle", Flow = "slidFlow", Ratio = "knobRatio",
        NoWake = "bNWZ", FlowResume = "fFlowEpochResume";
    public const string CoreTemperature = "StatICCoreTemp", PelletMax = "StatICPellMax", PelletMaxTheory = "StatICPellMaxTheory";
    public static readonly IReadOnlyList<string> ControlKeys = new[] { Cycle, Flow, Ratio };

    public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    /// <summary>Core temperature as a fraction of the ideal; NaN when the reading is unusable.</summary>
    public static double CoreRatio(double temperatureMeV) => Finite(temperatureMeV) && temperatureMeV > 0 ? temperatureMeV / IdealCoreMeV : double.NaN;
    public static bool WithinBand(double coreRatio, double band) => Finite(coreRatio) && Finite(band) && Math.Abs(coreRatio - 1) <= band;

    /// <summary>Mirror of NavData.GetFLOWforCYCLE, clamped to the slider range. Zero when the capacities are unusable.</summary>
    public static double InitialFlow(double cycle, double pelletMaxTheory, double pelletMax)
    {
        if (!Finite(cycle) || !Finite(pelletMaxTheory) || !Finite(pelletMax) || cycle <= 0 || pelletMax <= 0 || pelletMaxTheory <= 0) return 0;
        double flow = cycle * pelletMaxTheory / pelletMax * FlowForCycleFraction;
        return Finite(flow) ? Math.Max(0, Math.Min(1, flow)) : 0;
    }

    /// <summary>The course plot's hot-side correction band starts above the game's wall-damage temperature, so
    /// the hot side here corrects from that temperature instead (a tighter, safer band); the cold side is vanilla.</summary>
    public static double HotCorrectionBand => Math.Min(CoreCorrectionBand, WallDamageMeV / IdealCoreMeV - 1);

    /// <summary>Mirror of NavData.GetAdjustedFLOW: rescale flow toward the ideal core while the core is off by more
    /// than the correction band, otherwise nudge it toward the thrust target. Accelerations share any one unit.</summary>
    public static double AdjustFlow(double coreRatio, double flowNow, double actualAcceleration, double targetAcceleration)
    {
        if (!Finite(flowNow)) return 0;
        if (Finite(coreRatio) && coreRatio > 0 && flowNow != 0 &&
            (coreRatio - 1 > HotCorrectionBand || 1 - coreRatio > CoreCorrectionBand)) return Clamp(flowNow / coreRatio);
        if (!Finite(actualAcceleration) || !Finite(targetAcceleration)) return Clamp(flowNow);
        if (Math.Abs(targetAcceleration - actualAcceleration) < Math.Abs(targetAcceleration) * ThrustAgreementFraction) return Clamp(flowNow);
        return Clamp(targetAcceleration > actualAcceleration ? flowNow + FlowStep : flowNow - FlowStep);
    }

    /// <summary>The reactor is installed, lit, and not shut down or damaged: it can take flight controls back,
    /// whatever its core is doing. Never true for a reactor that automation would have to restart.</summary>
    public static bool Intact(IReactorState reactor) =>
        reactor != null && !reactor.Destroyed && reactor.Has("IsInstalled") && reactor.Has("IsReadyFusion") &&
        !reactor.Has("IsOff") && !reactor.Has("IsOverrideOff") && !reactor.Has("IsShuttingDown") && !reactor.Has("IsDamaged");
    /// <summary>Intact and with its core inside the abort band: fit to be commanded.</summary>
    public static bool Ready(IReactorState reactor) => Intact(reactor) && WithinBand(CoreRatio(reactor.CoreTemperatureMeV), CoreAbortBand);
    /// <summary>Missing no-wake state is treated as restricted: the reactor's own check writes it every second.</summary>
    public static bool IsNoWake(IReactorPanel panel) => !bool.TryParse(panel.Read(NoWake), out bool restricted) || restricted;
    /// <summary>NavModTorchDrive stamps this when the pilot moves the flow slider; the course plot then leaves flow alone.</summary>
    public static bool PilotTouchedFlow(IReactorPanel panel, double now) =>
        double.TryParse(panel.Read(FlowResume), NumberStyles.Float, CultureInfo.InvariantCulture, out double resume) && Finite(now) && now < resume;
    public static bool ValidControls(IReactorPanel panel)
    {
        double cycle = Number(panel, Cycle), flow = Number(panel, Flow), ratio = Number(panel, Ratio);
        return Finite(cycle) && Finite(flow) && Finite(ratio) && cycle >= 0 && cycle <= 1 && flow >= 0 && flow <= 1 && (ratio == 0 || ratio == 1);
    }
    public static double Number(IReactorPanel panel, string key) =>
        double.TryParse(panel.Read(key), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : double.NaN;
    public static string Text(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static double Clamp(double flow) => Finite(flow) ? Math.Max(0, Math.Min(1, flow)) : 0;
}

/// <summary>The reactor's control panel, read and written by key. Implemented over the game's CondOwner at runtime
/// and over a dictionary in offline checks.</summary>
public interface IReactorPanel
{
    /// <summary>Current value of a Panel A key, or an empty string when absent.</summary>
    string Read(string key);
    /// <summary>Writes one Panel A key through the game's own property-map change path.</summary>
    void Write(string key, string value);
}

/// <summary>Reactor state the readiness rule needs in addition to the panel.</summary>
public interface IReactorState : IReactorPanel
{
    bool Destroyed { get; }
    bool Has(string condition);
    double CoreTemperatureMeV { get; }
}

/// <summary>Owned reactor flight controls: remembers what automation commanded and what the reactor idled at, so
/// the owner can tell its own writes from a pilot's, and can hand the pre-ownership settings back on release.
/// Policy (when to burn, how much) belongs to the consumer.</summary>
public sealed class ReactorControls
{
    private readonly IReactorPanel panel;
    private readonly Dictionary<string, string> idle = new(StringComparer.Ordinal), commanded = new(StringComparer.Ordinal);
    private bool writing;
    public ReactorControls(IReactorPanel panel) { this.panel = panel ?? throw new ArgumentNullException(nameof(panel)); }

    /// <summary>True while one of this owner's writes is in progress: an observed change is then not a pilot's.</summary>
    public bool Writing => writing;
    /// <summary>The flow and mode the reactor idled at before ownership; cycle idles at zero.</summary>
    public IReadOnlyDictionary<string, string> Idle => idle;

    /// <summary>Records the current flow and mode as the idle settings to restore later. Idempotent.</summary>
    public void Adopt()
    {
        if (idle.Count > 0) return;
        idle[ReactorRules.Flow] = panel.Read(ReactorRules.Flow);
        idle[ReactorRules.Ratio] = panel.Read(ReactorRules.Ratio);
        idle[ReactorRules.Cycle] = "0";
    }

    public void Write(string key, double value) => Write(key, ReactorRules.Text(value));
    public void Write(string key, string value)
    {
        writing = true;
        try { panel.Write(key, value); commanded[key] = value; }
        finally { writing = false; }
    }
    /// <summary>Writes only when the panel does not already hold the value; returns whether a write happened.</summary>
    public bool WriteIfChanged(string key, string value)
    {
        if (panel.Read(key) == value) { commanded[key] = value; return false; }
        Write(key, value);
        return true;
    }

    /// <summary>An incoming external write to a flight control whose value differs from the current one, and not our own.</summary>
    public bool ChangedByPilot(string key, string value) =>
        !writing && (key == ReactorRules.Cycle || key == ReactorRules.Flow || key == ReactorRules.Ratio) && panel.Read(key) != value;
    /// <summary>The panel no longer holds all three values this owner last commanded.</summary>
    public bool Differs
    {
        get
        {
            if (commanded.Count != 3) return false;
            foreach (string key in ReactorRules.ControlKeys) if (panel.Read(key) != commanded[key]) return true;
            return false;
        }
    }

    /// <summary>Cycle to zero; flow and mode back to idle when the reactor can take them. A no-wake zone keeps the
    /// game's own mode interlock (ratio 0) instead of the idle mode.</summary>
    public void RestoreIdle(bool restoreFlowAndMode, bool noWake)
    {
        Write(ReactorRules.Cycle, "0");
        if (!restoreFlowAndMode || idle.Count == 0) return;
        Write(ReactorRules.Flow, idle[ReactorRules.Flow]);
        Write(ReactorRules.Ratio, noWake ? "0" : idle[ReactorRules.Ratio]);
    }
    /// <summary>Clears only the cycle, leaving flow and mode for an incoming pilot command.</summary>
    public void ClearCycle() => Write(ReactorRules.Cycle, "0");
    /// <summary>Forgets ownership without writing anything (world teardown).</summary>
    public void Forget() { idle.Clear(); commanded.Clear(); }
}

/// <summary>Game-facing reactor reads and the vanilla flow helpers over a CondOwner. Never ignites, repairs or
/// refuels a reactor; FusionIC keeps fuel, heat, wear, power and actual thrust.</summary>
public static class NativeReactor
{
    public static IReactorState Panel(CondOwner core) => new CondOwnerReactor(core);
    public static double CoreRatio(CondOwner core) => core == null ? double.NaN : ReactorRules.CoreRatio(core.GetCondAmount(ReactorRules.CoreTemperature));
    public static bool Intact(CondOwner core) => core != null && ReactorRules.Intact(Panel(core));
    public static bool Ready(CondOwner core) => core != null && ReactorRules.Ready(Panel(core));
    public static bool IsNoWake(CondOwner core) => core == null || ReactorRules.IsNoWake(Panel(core));
    public static bool PilotTouchedFlow(CondOwner core, double now) => core != null && ReactorRules.PilotTouchedFlow(Panel(core), now);
    public static bool ValidControls(CondOwner core) => core != null && ReactorRules.ValidControls(Panel(core));
    /// <summary>The game's own flow-for-cycle rule.</summary>
    public static double InitialFlow(CondOwner core, double cycle) => core == null ? 0 : NavData.GetFLOWforCYCLE(core, cycle);
    /// <summary>The game's own course-plot flow adjustment; accelerations in the game's AU/s² as it uses them.</summary>
    public static double AdjustFlow(CondOwner core, double flowNow, double actualAcceleration, double targetAcceleration) =>
        core == null ? 0 : NavData.GetAdjustedFLOW(core, flowNow, actualAcceleration, targetAcceleration);

    private sealed class CondOwnerReactor : IReactorState
    {
        private readonly CondOwner core;
        internal CondOwnerReactor(CondOwner core) { this.core = core ?? throw new ArgumentNullException(nameof(core)); }
        public bool Destroyed => core.bDestroyed;
        public bool Has(string condition) => core.HasCond(condition);
        public double CoreTemperatureMeV => core.GetCondAmount(ReactorRules.CoreTemperature);
        public string Read(string key) => core.GetGPMInfo(ReactorRules.Panel, key) ?? "";
        public void Write(string key, string value) => core.ApplyGPMChanges(new[] { ReactorRules.Panel + "," + key + "," + value });
    }
}
