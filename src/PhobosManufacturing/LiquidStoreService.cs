using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The Lixivar acid tanks over Framework's bulk vessel: status, owner-confirmed acceptance, recovery of the
/// bund after repair, pouring into another tank of the same liquid within one tile, and the hazard: when a tank is
/// damaged or destroyed, an authored mist fraction of its service contents reaches the room as the game's own H2SO4
/// (its poisoning bands apply) and the crew are warned; the bund keeps the rest. Native destruction is never blocked.</summary>
internal static class LiquidStoreService
{
    private static LiquidStore Store(CondOwner co) => LiquidStores.For(co.strCODef) ?? throw new InvalidOperationException("Not a liquid store: " + co.strCODef);
    private static string T(LiquidStore store, string key, params object[] args) => Text.Get(store.Family.TextPrefix + "." + key, args);
    internal static string Describe(CondOwner co)
    {
        var store = Store(co);
        if (BulkVessel.Protected(co)) return Text.Get("Store.protected");
        var s = BulkVessel.Snapshot(co);
        // Every machine paired with the tank, from any mod (tanks are shared since Manufacturing 0.23.0).
        string names = string.Join(", ", LinkChoices.LinkedNames(co));
        string text = T(store, "level", s.ServiceKg, s.CapacityKg) + "\n" + Text.Get("Store.linked", names.Length == 0 ? ConsoleText.Get("not_selected") : names);
        if (s.CatchKg > 1e-8) text += "\n" + T(store, "bund", s.CatchKg);
        if (co.HasCond("IsDamaged")) text += "\n" + T(store, "damaged");
        return text;
    }
    /// <summary>Why a tank may not be moved or dismantled now: an unreadable record, or acid still in it or its bund.</summary>
    internal static string? MaintenanceReason(CondOwner co) => BulkVessel.Protected(co) ? Text.Get("Maintenance.protected") :
        BulkVessel.Snapshot(co).ServiceKg + BulkVessel.Snapshot(co).CatchKg > 1e-8 ? Text.Get("Maintenance.acid") : null;
    internal static EquipmentState State(CondOwner co) => BulkVessel.Protected(co) || co.HasCond("IsDamaged") || co.HasCond("IsLocked") ? EquipmentState.Blocked : EquipmentState.Ready;
    /// <summary>Other tanks of the same liquid within one tile that can take some of this one's contents.</summary>
    internal static IEnumerable<CondOwner> PourTargets(CondOwner co) => BulkVessels.Aboard(co.ship, Store(co).Commodity).Where(v => v != co && LiquidStores.IsFamily(v.strCODef) && BulkVessels.Adjacent(co, v));
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Text.Get("Store.protected");
        if (!Content.Ready) { message = Content.Status; return false; }
        message = Content.Access(co, binding) ?? ""; if (message.Length > 0) return false;
        var store = Store(co);
        if (action == "accept") { bool ok = BulkVessel.Accept(co, Plugin.Log); message = Text.Get(ok ? "Store.accept_done" : "Store.accept_unavailable"); return ok; }
        if (BulkVessel.Protected(co)) return false;
        if (action == "recover")
        {
            if (co.HasCond("IsDamaged")) { message = T(store, "repair_first"); return false; }
            var s = BulkVessel.Read(co); if (s.CatchKg <= 0) { message = T(store, "bund_empty"); return false; }
            s.Recover(); BulkVessel.Save(co, s); message = T(store, "recovered"); return true;
        }
        if (action.StartsWith("pour:", StringComparison.Ordinal))
        {
            var target = PourTargets(co).FirstOrDefault(v => v.strID == action.Substring(5));
            if (target == null || co.HasCond("IsDamaged") || target.HasCond("IsDamaged")) { message = T(store, "pour_missing"); return false; }
            double moved = LiquidTransferGuard.Commit(new BulkVessel.Endpoint(co), new BulkVessel.Endpoint(target), BulkVessel.Snapshot(co).ServiceKg, BulkVessel.Guard(co), BulkVessel.Guard(target)).ReceivedKg;
            message = T(store, "poured", moved, target.strNameFriendly); return moved > 0;
        }
        message = Describe(co); return action == "status";
    }
    /// <summary>The mist a damage switch or a destruction releases: drained from the service contents first (so
    /// Framework isolates only the rest), emitted into the room air, logged and announced.</summary>
    internal static void Mist(CondOwner co, string logKey)
    {
        if (co == null || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || BulkVessel.Protected(co)) return;
        try
        {
            var store = Store(co);
            double mist = LiquidStores.MistKg(BulkVessel.Snapshot(co).ServiceKg);
            if (mist <= 1e-9) return;
            var air = RoomHeat.Read(co);
            double released = BulkVessel.Drain(co, mist, T(store, "mist_reason"));
            if (released <= 0) return;
            if (air != null) RoomGas.Emit(air, store.Family.MistSpecies, released);
            PlayerNotices.Post(co.ship, "PhobosManufacturing.acid", NoticeLevel.Caution, T(store, logKey, co.strNameFriendly, released * 1000));
        }
        catch (Exception e) { Plugin.Log(e.ToString()); }
    }
}

// An intact acid tank switching to its damaged form mists before Framework moves its contents into the bund.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
[HarmonyBefore(FrameworkInfo.PluginId)]
internal static class LiquidStoreDamagePatch
{
    private static void Prefix(CondOwner __instance, CondOwner coNew)
    {
        if (__instance == null || coNew == null || !LiquidStores.IsFamily(__instance.strCODef) || __instance.HasCond("IsDamaged") || !coNew.HasCond("IsDamaged")) return;
        LiquidStoreService.Mist(__instance, "damaged_log");
    }
}
