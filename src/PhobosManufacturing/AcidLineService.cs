using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The acid line's hazard (Manufacturing 0.24.0; owner decision, 30 September 2026). A segment is wetted while
/// its network joins an acid tank to a machine linked to that tank; a wetted segment that is damaged or destroyed spills
/// its hold-up (<see cref="AcidLineRules.Spill"/>), drawn from that tank: the mist fraction reaches the room air as the
/// game's own H2SO4 (its poisoning bands apply), the rest is held in the tank's bund until it is recovered after repair,
/// and the crew are warned. Native damage and destruction are never blocked; removal of a wetted segment is refused
/// when the work is offered.</summary>
internal static class AcidLineService
{
    /// <summary>The acid tank a wetted segment carries acid from, or null for a dry line: a tank on the segment's network
    /// with a machine linked to it on that same network. The first by id, so the choice is stable.</summary>
    internal static CondOwner? Source(CondOwner? segment)
    {
        if (segment?.ship == null || !AcidLineRules.IsFamily(segment.strCODef)) return null;
        var members = LineReach.MembersThrough(segment, AcidLine.Family);
        if (members.Count < 2) return null;
        var ids = new HashSet<string>(members.Select(m => m.strID), StringComparer.Ordinal);
        return members.Where(m => LiquidStores.Holds(m.strCODef, LiquidStores.SulfuricAcid) && SharedPorts.Peers(m).Any(ids.Contains))
            .OrderBy(t => t.strID, StringComparer.Ordinal).FirstOrDefault();
    }
    /// <summary>Why a segment may not be uninstalled or dismantled now: it carries acid between a tank and its machine.</summary>
    internal static string? MaintenanceReason(CondOwner co) =>
        co.HasCond("IsInstalled") && Source(co) is CondOwner tank ? Text.Get("AcidLine.maintenance_wetted", ObjectPresentation.Name(tank)) : null;
    /// <summary>The spill of one wetted segment: mist drained from the tank into the room, the rest into the tank's bund,
    /// one caution to the crew. A dry line, a protected tank or an empty one spills nothing.</summary>
    internal static void Spill(CondOwner segment, string logKey)
    {
        if (segment?.ship == null || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || (int)segment.ship.LoadState < 2) return;
        try
        {
            var tank = Source(segment);
            if (tank == null || BulkVessel.Protected(tank)) return;
            var spill = AcidLineRules.Spill(BulkVessel.Snapshot(tank).ServiceKg);
            if (spill.Released <= 1e-9) return;
            double misted = spill.Mist > 0 ? BulkVessel.Drain(tank, spill.Mist, Text.Get("AcidLine.mist_reason")) : 0;
            var air = RoomHeat.Read(segment);
            if (misted > 0 && air != null) RoomGas.Emit(air, LiquidStores.AcidFamily.MistSpecies, misted);
            double held = BulkVessel.Contain(tank, spill.Bund, Text.Get("AcidLine.bund_reason"));
            PlayerNotices.Post(segment.ship, "PhobosManufacturing.acid", NoticeLevel.Caution, Text.Get(logKey, ObjectPresentation.Name(tank), misted * 1000, held));
        }
        catch (Exception e) { Plugin.Log(e.ToString()); }
    }
}

// A wetted segment switching to its damaged form spills before Framework invalidates the line network, so the network
// it belonged to is still the one read.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
[HarmonyBefore(FrameworkInfo.PluginId)]
internal static class AcidLineDamagePatch
{
    private static void Prefix(CondOwner __instance, CondOwner coNew)
    {
        if (__instance == null || coNew == null || __instance.strCODef != AcidLineRules.Installed || !coNew.HasCond("IsDamaged")) return;
        AcidLineService.Spill(__instance, "AcidLine.damaged_log");
    }
}
// A wetted intact segment destroyed outright (not the old half of a mode switch) spills the same way.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.Destroy))]
[HarmonyBefore(FrameworkInfo.PluginId)]
internal static class AcidLineDestroyPatch
{
    private static void Prefix(CondOwner __instance)
    {
        if (__instance == null || __instance.strCODef != AcidLineRules.Installed || __instance.HasCond("IsModeSwitching", false)) return;
        if (CrewSim.system?.GetShipOwner(__instance.ship?.strRegID) != CrewSim.coPlayer?.strID) return;
        AcidLineService.Spill(__instance, "AcidLine.destroyed_log");
    }
}
