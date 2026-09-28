using System;
using System.Linq;
using HarmonyLib;
using Ostranauts.Core;
using Ostranauts.Objectives;
using Ostranauts.ShipGUIs.MFD;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Native boundary only. No invented clearance, position writes, free refuelling or AI pilot registration.
internal static class DockingAdapter
{
    internal static string? Check(Ship own, Ship? target, string ownPort, string targetPort, bool checkFit)
    {
        if (target == null || target == own || target.bDestroyed || target.HideFromSystem || target.IsStationHidden() || target.objSS == null)
            return "Docking.target_lost";
        // The game's clamp button allows docking a further ship while one is attached if a port is still open.
        if (own.IsDocked() && own.GetOpenDockingPorts().Count == 0 || own.IsMoored() || own.TowBraceSecured(target.strRegID)) return "Docking.attached";
        var clearance = own.Comms?.Clearance;
        if (clearance == null || clearance.TargetRegId != target.strRegID || clearance.ClearanceType != "DOCK" || clearance.DockID != targetPort)
            return "Docking.clearance";
        if (CrewSim.system.IsInAtmo(target) || target.IsGroundStation()) return "Docking.space_only";
        if (!own.GetOpenDockingPorts().Contains(ownPort) || !target.GetOpenDockingPorts().Contains(targetPort)) return "Docking.ports";
        if (checkFit && !(target.GetAvailableDockingPorts(own, earlyOut: false)?.Any(pair => pair.Item1 == targetPort && pair.Item2 == ownPort) ?? false))
            return "Docking.fit";
        return null;
    }

    internal static string? SelectPorts(Ship own, Ship target, out string ownPort, out string targetPort)
    {
        ownPort = ""; targetPort = own.Comms?.Clearance?.DockID ?? "";
        var clearance = own.Comms?.Clearance;
        if (clearance == null || clearance.TargetRegId != target.strRegID || clearance.ClearanceType != "DOCK") return "Docking.clearance";
        string assigned = targetPort;
        var pairs = target.GetAvailableDockingPorts(own, earlyOut: false);
        var pair = pairs?.Where(p => p.Item1 == assigned)
            .OrderByDescending(p => p.Item2 == own.PrimaryDockingPortID).ThenBy(p => p.Item2, StringComparer.Ordinal).FirstOrDefault();
        ownPort = pair?.Item2 ?? "";
        return Check(own, target, ownPort, targetPort, checkFit: false);
    }

    // Presentation only: native GetAvailableDockingPorts builds both full hull
    // grids and tests overlays. Commands still call SelectPorts for a fresh fit.
    internal static string? ReadAvailablePorts(Ship own, Ship target, out string ownPort, out string targetPort)
    {
        ownPort = ""; targetPort = own.Comms?.Clearance?.DockID ?? "";
        var clearance = own.Comms?.Clearance;
        if (clearance == null || clearance.TargetRegId != target.strRegID || clearance.ClearanceType != "DOCK") return "Docking.clearance";
        var ports = own.GetOpenDockingPorts();
        ownPort = ports.Contains(own.PrimaryDockingPortID) ? own.PrimaryDockingPortID : ports.OrderBy(p => p, StringComparer.Ordinal).FirstOrDefault() ?? "";
        return Check(own, target, ownPort, targetPort, checkFit: false);
    }

    internal static bool Read(Ship own, Ship target, float throttle, double dt, out DockingCommand command, NavVector targetAcceleration = default, bool hold = false)
    {
        var a = own.objSS; var b = target.objSS;
        command = default;
        if (a == null || b == null) return false;
        if (hold) return DockingRules.TryHold(new NavVector((b.vPosx - a.vPosx) / AutoNavCore.M_TO_AU,
            (b.vPosy - a.vPosy) / AutoNavCore.M_TO_AU), new NavVector((a.vVelX - b.vVelX) / AutoNavCore.M_TO_AU,
            (a.vVelY - b.vVelY) / AutoNavCore.M_TO_AU), targetAcceleration, a.fRot, a.fW,
            CollisionManager.GetCollisionDistanceAU(own, target) / AutoNavCore.M_TO_AU,
            own.RCSAccelMax / AutoNavCore.M_TO_AU, throttle, dt, out command);
        // Called at the StarSystem.Update boundary, never between individual ships' updates.
        return DockingRules.TryGuide((b.vPosx - a.vPosx) / AutoNavCore.M_TO_AU,
            (b.vPosy - a.vPosy) / AutoNavCore.M_TO_AU,
            (a.vVelX - b.vVelX) / AutoNavCore.M_TO_AU, (a.vVelY - b.vVelY) / AutoNavCore.M_TO_AU,
            a.fRot, a.fW, CollisionManager.GetCollisionDistanceAU(own, target) / AutoNavCore.M_TO_AU,
            own.RCSAccelMax / AutoNavCore.M_TO_AU, throttle, dt, out command, targetAcceleration);
    }

    internal static bool ConsoleOpen(CondOwner console) => GUIDockSys.instance != null &&
        GUIDockSys.instance.bActive && GUIDockSys.instance.COSelf == console;

    internal static bool HasFuel(Ship own, Ship target, float throttle)
    {
        if (!Read(own, target, throttle, DockingRules.MaximumStep, out var motion)) return false;
        double peakSpeed = Math.Min(DockingRules.CruiseMS,
            Math.Sqrt(Math.Max(0, own.RCSAccelMax / AutoNavCore.M_TO_AU * throttle * motion.HullGapM)));
        const double correctionReserveMS = 2;
        double budget = motion.SpeedMS + 2 * peakSpeed + correctionReserveMS;
        return ArrivalBrake.Finite(own.DeltaVRemainingRCS) && own.DeltaVRemainingRCS / AutoNavCore.M_TO_AU >= budget;
    }

    internal enum ClampResult { NotYet, Started, Refused }
    // The docking console's private button members, resolved when needed so a changed native integration
    // refuses cleanly instead of attaching through an assumption.
    private static System.Reflection.MethodInfo? NativeCanDock => AccessTools.Method(typeof(GUIDockSys), "CanDock");
    private static System.Reflection.MethodInfo? NativeClamp => AccessTools.Method(typeof(GUIDockSys), "ClampEngage");
    private static string? NativeClearedShip => AccessTools.Property(typeof(GUIDockSys), "ClearedShipRegID")?.GetValue(GUIDockSys.instance) as string;

    /// <summary>The game's own clamp button. Its CanDock geometry admits the clamp and its ClampEngage
    /// sequence attaches: alignment, port choice, crime checks, docking events, autosave and the MFD change
    /// all stay native. Refused when our clearance, port or motion checks fail or the native members are
    /// missing; NotYet while the game's own geometry is not satisfied; Started once the native sequence is
    /// running over the next frames.</summary>
    internal static ClampResult Clamp(CondOwner console, Ship target, string ownPort, string targetPort, float throttle, double dt)
    {
        var own = console.ship;
        var canDock = NativeCanDock; var clamp = NativeClamp;
        if (!ConsoleOpen(console) || console.HasCond("IsDamagedSoftware") || Check(own, target, ownPort, targetPort, checkFit: true) != null ||
            !Read(own, target, throttle, dt, out var command) || !command.Ready || canDock == null || clamp == null ||
            NativeClearedShip != target.strRegID) return ClampResult.Refused;
        if (!(bool)canDock.Invoke(GUIDockSys.instance, null)!) return ClampResult.NotYet;
        clamp.Invoke(GUIDockSys.instance, new object[] { false });
        return ClampResult.Started;
    }

    /// <summary>The game's own clamp release for a docked ship, when its docking console is open here and
    /// cleared for that ship: stolen-ship check, grace period, free-pass reset, undock event, achievement and
    /// ring rotation stay native and run over the next frames. False when that button would not release.</summary>
    internal static bool ReleaseClamps(CondOwner console, Ship peer)
    {
        var own = console.ship;
        var clamp = NativeClamp;
        if (!ConsoleOpen(console) || console.HasCond("IsDamagedSoftware") || clamp == null ||
            own.Comms?.Clearance == null || !own.IsDockedWith(peer) || own.TowBraceSecured(peer.strRegID) ||
            NativeClearedShip != peer.strRegID) return false;
        clamp.Invoke(GUIDockSys.instance, new object[] { false });
        return true;
    }
}
