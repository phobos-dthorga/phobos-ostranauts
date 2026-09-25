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
        if (own.IsDocked() || own.IsMoored() || own.TowBraceSecured(target.strRegID)) return "Docking.attached";
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

    internal static bool Attach(CondOwner console, Ship target, string ownPort, string targetPort, float throttle, double dt)
    {
        var own = console.ship;
        if (!ConsoleOpen(console) || console.HasCond("IsDamagedSoftware") || Check(own, target, ownPort, targetPort, checkFit: true) != null ||
            !Read(own, target, throttle, dt, out var command) || !command.Ready) return false;
        if (target.IsStation() || target.IsSubStation())
        {
            var breakLocks = AccessTools.Method(typeof(GUIDockSys), "BreakAllLocks");
            if (breakLocks == null) return false;
            breakLocks.Invoke(GUIDockSys.instance, null);
        }
        // The normal attachment API owns pressure/room placement, dock groups and fees.
        // Reach it only after our distance, velocity, heading and port checks.
        GUIDockSys.instance!.CheckForCrimeIllegalSalvagingOKLG(target.strRegID, CrewSim.coPlayer);
        var attached = CrewSim.DockShip(own, target.strRegID, targetPort, ownPort, own.Comms.Clearance!);
        return attached != null && own.IsDockedWith(target);
    }

    // Called only after the saved flight is marked complete, so native autosaves cannot capture a live maneuver.
    internal static void NotifyAttached(Ship target)
    {
        var crew = CrewSim.GetSelectedCrew();
        if (crew != null)
        {
            crew.ZeroCondAmount("TutorialNavDockWithDerelictWaiting");
            crew.ZeroCondAmount("TutorialNavSeriesInProgress");
            MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(crew.strID);
        }
        GUIDockSys.DockEvent.Invoke(target.strRegID);
        BeatManager.AutoSaveBeforePirateEncounter(target);
        GUIMFDPageHost.OnRequestMFDChange?.Invoke(new MFDDockInfo(GUIMFDPageHost.DefaultCommsScreen));
    }
}
