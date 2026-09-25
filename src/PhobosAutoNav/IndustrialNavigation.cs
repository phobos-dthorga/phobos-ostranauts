using System;
using System.Linq;
using PhobosAutoNav.Core;
using static PhobosAutoNav.Core.DockingRules;

namespace PhobosAutoNav;

/// <summary>Exclusive, transient terminal-flight permission. Callers retain their own mission records.
/// Readiness is a fresh observation, never permission to teleport, cut or bypass native attachment.</summary>
public enum IndustrialMove { CaptureApproach, Egress, Transit }
public enum IndustrialStatus { Approaching, Holding, Ready, Blocked, Suspended }

public static class IndustrialNavigation
{
    public static event Action<Ship>? ManualTakeover;
    internal static void Yield(Ship own)
    {
        // A consumer failure must never prevent the native manual command or other consumers yielding.
        foreach(Action<Ship> handler in ManualTakeover?.GetInvocationList()??Array.Empty<Delegate>())
            try {handler(own);} catch { }
    }
    public static bool CanTrack(Ship own,string target) => NativeContactReader.Read(own,target).Usable;
    public static bool RouteCost(CondOwner console,string module,string target,double bearing,double gap,out double cost) =>
        Plugin.Service!.IndustrialRouteCost(console,module,target,bearing,gap,out cost);
    public static string? ModuleId(CondOwner console) => console?.GetCOsSafe(true)
        .Where(c => !c.bDestroyed && !c.HasCond("IsDamaged") &&
            (c.strCODef == NavigationService.ModuleId || c.strCODef == NavigationService.PursuitId))
        .OrderBy(c => c.strID, StringComparer.Ordinal).FirstOrDefault()?.strID;

    public static bool Request(string permission, CondOwner console, string moduleId, string targetId,
        double facingDegrees, Func<string?> bindingProblem, out string message) =>
        Plugin.Service!.RequestIndustrial(permission, console, moduleId, targetId, facingDegrees, bindingProblem, out message);
    public static bool Observe(string permission, out bool ready, out string message) =>
        Plugin.Service!.ObserveIndustrial(permission, out ready, out message);
    public static bool RequestMove(string permission, CondOwner console, string moduleId, string targetId,
        IndustrialMove move, double bearingDegrees, double hullGapM, double facingDegrees,
        Func<string?> bindingProblem, out string message) => Plugin.Service!.RequestIndustrialMove(permission,
            console, moduleId, targetId, move, bearingDegrees, hullGapM, facingDegrees, bindingProblem, out message);
    public static IndustrialStatus Status(string permission, out string message) => Plugin.Service!.IndustrialState(permission, out message);
    public static void Release(string permission) => Plugin.Service!.ReleaseIndustrial(permission);
}

internal sealed partial class NavigationService
{
    private sealed class IndustrialFlight
    {
        internal string Permission = "", Module = "", Target = "", Player = "", Ship = "";
        internal CondOwner Console = null!;
        internal Ship Carrier = null!;
        internal Func<string?> BindingProblem = null!;
        internal double Facing, Elapsed, Stable, Bearing, Gap;
        internal IndustrialMove Move;
        internal readonly MotionTrack Track = new();
        internal bool Ready;
    }
    private IndustrialFlight? industrial;
    private string industrialNotice = "";
    internal bool IndustrialOwns(ShipSitu situ) => industrial?.Carrier.objSS == situ;

    internal bool RequestIndustrial(string permission, CondOwner co, string module, string target,
        double facing, Func<string?> bindingProblem, out string message, IndustrialMove move = IndustrialMove.CaptureApproach, double bearing = 0, double gap = 0)
    {
        message = Text.Get("Industrial.busy");
        if (industrial != null || AutoNavCore.Engaged || autoAim || OtherControllerBusy() ||
            string.IsNullOrWhiteSpace(permission) || bindingProblem == null || !ArrivalBrake.Finite(facing) ||
            CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || !Plugin.Enabled.Value) return false;
        message = HardwareProblem(co) ?? bindingProblem() ?? "";
        if (message.Length != 0) return false;
        if (co.HasCond("IsDamagedSoftware")) { message = Text.Get("Docking.software"); return false; }
        if (DisplaySnapshot(co) != null || !CanReplaceFlight(co) || !HasIndustrialModule(co, module))
        { message = Text.Get("Industrial.busy"); return false; }
        var contact = NativeContactReader.Read(co.ship, target);
        if (!contact.Usable) { message = Text.Get(contact.MessageKey); return false; }
        var candidate = new IndustrialFlight { Permission = permission, Console = co, Carrier = co.ship, Module = module,
            Target = target, Player = CrewSim.coPlayer.strID, Ship = co.ship.strRegID,
            Facing = facing * Math.PI / 180, BindingProblem = bindingProblem, Move = move, Bearing = bearing * Math.PI / 180, Gap = gap };
        if (!IndustrialRead(candidate, DockingRules.MaximumStep, false, out _) ||
            !(move == IndustrialMove.CaptureApproach ? DockingAdapter.HasFuel(co.ship, CrewSim.system.GetShipByRegID(target)!, ReadThrottle(co)) :
              ArrivalBrake.Finite(co.ship.DeltaVRemainingRCS) && co.ship.DeltaVRemainingRCS/AutoNavCore.M_TO_AU >= DepartureRules.MinimumReserveMS))
        { message = Text.Get("Docking.unsafe"); return false; }
        CeaseFire(); Torch.Release(); industrial = candidate;
        co.ship.UnlockFromOrbit(); co.ship.objSS.ResetNavData(); CrewSim.ResetTimeScale();
        industrialNotice = message = Text.Get("Industrial.approaching");
        return true;
    }
    internal bool RequestIndustrialMove(string permission, CondOwner co, string module, string target,
        IndustrialMove move, double bearing, double gap, double facing, Func<string?> binding, out string message)
    {
        message = Text.Get("Docking.unsafe");
        if (!Enum.IsDefined(typeof(IndustrialMove), move) || !ArrivalBrake.Finite(bearing) ||
            !ArrivalBrake.Finite(gap) || gap < 1000 || gap > DockingRules.MaximumHullGapM) return false;
        if (!RequestIndustrial(permission, co, module, target, facing, binding, out message, move, bearing, gap)) return false;
        industrial!.Move = move; industrial.Bearing = bearing * Math.PI / 180; industrial.Gap = gap;
        return true;
    }
    internal IndustrialStatus IndustrialState(string permission, out string message)
    {
        if (!ObserveIndustrial(permission, out bool ready, out message)) return IndustrialStatus.Suspended;
        if (avoidanceActive) return avoidanceBlocked?IndustrialStatus.Blocked:IndustrialStatus.Approaching;
        return ready ? IndustrialStatus.Ready : industrial!.Stable < StableMotionSeconds ? IndustrialStatus.Holding : IndustrialStatus.Approaching;
    }
    private static bool HasIndustrialModule(CondOwner co, string id) => co.GetCOsSafe(true).Any(c =>
        c.strID == id && !c.bDestroyed && !c.HasCond("IsDamaged") && (HasId(c, ModuleId) || HasId(c, PursuitId)));
    private string? IndustrialProblem(IndustrialFlight f)
    {
        if (!Plugin.Enabled.Value || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading ||
            CrewSim.coPlayer?.strID != f.Player || f.Console.ship != f.Carrier || f.Carrier.strRegID != f.Ship ||
            !HasIndustrialModule(f.Console, f.Module) || AutoNavCore.Engaged || autoAim ||
            OtherControllerBusyExceptIndustrial()) return Text.Get("Persistence.binding_changed");
        var problem = HardwareProblem(f.Console) ?? f.BindingProblem();
        if (f.Console.HasCond("IsDamagedSoftware")) return Text.Get("Docking.software");
        if (problem != null) return problem;
        var contact = NativeContactReader.Read(f.Console.ship, f.Target);
        return contact.Usable ? null : Text.Get(contact.MessageKey);
    }
    private static bool IndustrialRead(IndustrialFlight f, double dt, bool hold, out DockingCommand command)
    {
        command = default;
        var own = f.Carrier; var target = CrewSim.system?.GetShipByRegID(f.Target);
        if (target?.objSS == null || target == own || target.bDestroyed || f.Move == IndustrialMove.CaptureApproach && (target.IsDocked() || target.IsMoored())) return false;
        var a = own.objSS; var b = target.objSS;
        if (a == null) return false;
        double hull = CollisionManager.GetCollisionDistanceAU(own, target) / AutoNavCore.M_TO_AU;
        var offset = new NavVector((b.vPosx - a.vPosx) / AutoNavCore.M_TO_AU, (b.vPosy - a.vPosy) / AutoNavCore.M_TO_AU);
        var velocity = new NavVector((a.vVelX - b.vVelX) / AutoNavCore.M_TO_AU, (a.vVelY - b.vVelY) / AutoNavCore.M_TO_AU);
        if (f.Move != IndustrialMove.CaptureApproach)
        {
            var goal = offset + new NavVector(Math.Cos(f.Bearing), Math.Sin(f.Bearing)) * (hull + f.Gap);
            var desired = goal.Unit * Math.Min(CruiseMS, Math.Sqrt(Math.Max(0, goal.Length * own.RCSAccelMax / AutoNavCore.M_TO_AU * ReadThrottle(f.Console) * .25)));
            if (goal.Length < .25) desired = default;
            var demand = (desired - velocity) / Math.Max(dt, 2);
            double full = own.RCSAccelMax / AutoNavCore.M_TO_AU, cos = Math.Cos(a.fRot), sin = Math.Sin(a.fRot);
            if (!RcsBudget.TryLimit((demand.X * cos + demand.Y * sin) / full,
                (-demand.X * sin + demand.Y * cos) / full, Math.Max(-.15, Math.Min(.15, -a.fW / 2)),
                ReadThrottle(f.Console), RcsBudget.CombinedRotationShare, out var bounded)) return false;
            command = new DockingCommand(bounded.X, bounded.Y, bounded.Turn,
                goal.Length < 10 && offset.Length - hull >= f.Gap - 1 && velocity.Length <= ClampSpeedMS,
                offset.Length - hull, velocity.Length, 0);
            return true;
        }
        return hold ? DockingRules.TryHold(offset, velocity, f.Track.Acceleration, a.fRot, a.fW, hull,
            own.RCSAccelMax / AutoNavCore.M_TO_AU, ReadThrottle(f.Console), dt, out command) :
            DockingRules.TryGuide(offset.X, offset.Y, velocity.X, velocity.Y, a.fRot, a.fW, hull,
                own.RCSAccelMax / AutoNavCore.M_TO_AU, ReadThrottle(f.Console), dt, out command, f.Track.Acceleration, f.Facing);
    }
    internal void TickIndustrial(double dt, bool afterPhysics)
    {
        var f = industrial;
        if (f == null || CrewSim.Paused || dt == 0) return;
        try
        {
            string? problem = !ArrivalBrake.Finite(dt) || dt <= 0 || dt > DockingRules.MaximumStep ||
                f.Elapsed >= DockingRules.MaximumSeconds ? Text.Get("Docking.step") : IndustrialProblem(f);
            if (problem != null) { EndIndustrial(problem); return; }
            var own = f.Carrier; var target = CrewSim.system.GetShipByRegID(f.Target)!;
            f.Track.Observe(new NavVector((target.objSS.vVelX - own.objSS.vVelX) / AutoNavCore.M_TO_AU,
                (target.objSS.vVelY - own.objSS.vVelY) / AutoNavCore.M_TO_AU), StarSystem.fEpoch,
                new NavVector(own.objSS.vAccRCS.x / AutoNavCore.M_TO_AU, own.objSS.vAccRCS.y / AutoNavCore.M_TO_AU));
            bool stable = f.Track.Samples >= 2 && f.Track.ErrorMS < MaximumResidualMS &&
                f.Track.Acceleration.Length < own.RCSAccelMax / AutoNavCore.M_TO_AU * ReadThrottle(f.Console) * TerminalAuthorityShare &&
                ArrivalBrake.Finite(target.objSS.fW) && Math.Abs(target.objSS.fW) <= DockingRules.ClampSpinRadians;
            if (!stable) f.Stable = 0;
            else if (!afterPhysics) f.Stable += dt;
            bool hold = f.Stable < StableMotionSeconds;
            if (!IndustrialRead(f, dt, hold, out var command))
            {
                f.Stable = 0; hold = true;
                if (!IndustrialRead(f, dt, true, out command)) { EndIndustrial(Text.Get("Docking.unsafe")); return; }
            }
            f.Ready = afterPhysics && !hold && command.Ready;
            industrialNotice = Text.Get(f.Ready ? "Industrial.ready" : hold ? "Docking.holding" : "Docking.progress", command.HullGapM, command.SpeedMS);
            if (afterPhysics) return;
            issuing = true;
            try { own.Maneuver((float)command.X, (float)command.Y, (float)command.Turn, 0, (float)dt); }
            finally { issuing = false; }
            f.Elapsed += dt;
        }
        catch (Exception ex) { log(ex.ToString()); EndIndustrial(Text.Get("Docking.error")); }
    }
    internal bool ObserveIndustrial(string permission, out bool ready, out string message)
    {
        ready = false; message = industrialNotice;
        var f = industrial;
        if (f == null || f.Permission != permission) return false;
        string? problem = IndustrialProblem(f);
        if (problem != null) { EndIndustrial(problem); message = problem; return false; }
        if (avoidanceActive) { message = avoidanceNotice; return true; }
        ready = f.Ready && f.Stable >= StableMotionSeconds && IndustrialRead(f, DockingRules.MaximumStep, false, out var command) && command.Ready;
        return true;
    }
    internal void ReleaseIndustrial(string permission) { if (industrial?.Permission == permission) EndIndustrial(Text.Get("Industrial.released")); }
    private void EndIndustrial(string reason)
    {
        var f = industrial; industrial = null; industrialNotice = reason;
        if (f?.Carrier.objSS == null || f.Carrier.bDestroyed) return;
        issuing = true;
        try { f.Carrier.Maneuver(0, 0, 0, 0, 0); }
        catch (Exception ex) { log(ex.ToString()); }
        finally { issuing = false; }
    }
}
