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

/// <summary>The liquid tanks over Framework's bulk vessel (the Lixivar acid tanks; the Alembrine ethanol casks since
/// Manufacturing 0.38.0): status, owner-confirmed acceptance, recovery of the bund after repair, pouring into another
/// tank of the same liquid it touches or shares its line with (Manufacturing 0.24.0), and the hazard. When a tank is
/// damaged or destroyed, an acid tank mists an authored fraction of its service contents into the room as the game's
/// own H2SO4 (its poisoning bands apply); an ethanol cask offers an authored share to a fire, which burns only with
/// oxygen and an ignition source in the room. The crew are warned and the bund keeps the rest. Native destruction is
/// never blocked.</summary>
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
        BulkVessel.Snapshot(co).ServiceKg + BulkVessel.Snapshot(co).CatchKg > 1e-8 ? Text.Get("Maintenance." + Store(co).Family.MaintenanceKey) : null;
    internal static EquipmentState State(CondOwner co) => BulkVessel.Protected(co) || co.HasCond("IsDamaged") || co.HasCond("IsLocked") ? EquipmentState.Blocked : EquipmentState.Ready;
    /// <summary>The line that carries this tank's liquid (the acid or ethanol line), or null (touching only).</summary>
    internal static FluidSegmentFamily? Line(CondOwner co) => LineFamilies.For(Store(co).Commodity);
    /// <summary>Other tanks of the same liquid this one reaches, touching or on its line, that can take some of its contents.</summary>
    internal static IEnumerable<CondOwner> PourTargets(CondOwner co)
    {
        var line = Line(co);
        return BulkVessels.Aboard(co.ship, Store(co).Commodity).Where(v => v != co && LiquidStores.IsFamily(v.strCODef) && LineReach.Of(co, v, line) != LineReachKind.None);
    }
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
    /// <summary>What a damage switch or a destruction releases, drained from the service contents first (so Framework
    /// isolates only the rest): an acid's mist into the room air, or ethanol burning when the room lets it. Logged and
    /// announced.</summary>
    internal static void Hazard(CondOwner co, string logKey)
    {
        if (co == null || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || BulkVessel.Protected(co)) return;
        try
        {
            var store = Store(co);
            if (store.Family.Fuel != null) { Burn(co, store, logKey); return; }
            double mist = store.Family.MistKg(BulkVessel.Snapshot(co).ServiceKg);
            if (mist <= 1e-9) return;
            var air = RoomHeat.Read(co);
            double released = BulkVessel.Drain(co, mist, T(store, "mist_reason"));
            if (released <= 0) return;
            if (air != null) RoomGas.Emit(air, store.Family.MistSpecies!, released);
            PlayerNotices.Post(co.ship, store.Family.NoticeId, NoticeLevel.Caution, T(store, logKey, co.strNameFriendly, released * 1000));
        }
        catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    /// <summary>A damaged or destroyed ethanol cask: its burn share catches when the room has oxygen and an ignition
    /// source (a fire, a working still or hearth, a sparking device), through the game's own explosion; otherwise the
    /// spill stays in the bund and the crew are told to keep sparks away.</summary>
    private static void Burn(CondOwner co, LiquidStore store, string logKey)
    {
        var fuel = store.Family.Fuel!;
        double kg = store.Family.BurnKg(BulkVessel.Snapshot(co).ServiceKg);
        if (kg <= 1e-9) return;
        var burn = StoreService.IgniteSpill(co, fuel, kg, take => BulkVessel.Drain(co, take, T(store, "burn_reason")));
        string message = burn == null ? T(store, logKey + "_spill", co.strNameFriendly) : T(store, logKey + "_fire", co.strNameFriendly, burn.BurnedKg, burn.EnergyKJ / 1000);
        Plugin.Log(message);
        PlayerNotices.Post(co.ship, store.Family.NoticeId, NoticeLevel.Caution, message);
    }
}

// An intact liquid tank switching to its damaged form mists or burns before Framework moves its contents into the bund.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
[HarmonyBefore(FrameworkInfo.PluginId)]
internal static class LiquidStoreDamagePatch
{
    private static void Prefix(CondOwner __instance, CondOwner coNew)
    {
        if (__instance == null || coNew == null || !LiquidStores.IsFamily(__instance.strCODef) || __instance.HasCond("IsDamaged") || !coNew.HasCond("IsDamaged")) return;
        LiquidStoreService.Hazard(__instance, "damaged_log");
    }
}

// A damaged ethanol line segment keeps its ethanol (Framework's LineContents: no game species to mist). When the room has
// oxygen and an ignition source, the burn share of what it holds catches through the game's own explosion (Manufacturing
// 0.38.0; owner decision, 4 October 2026). Runs after Framework has written the damaged segment's contents.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
[HarmonyAfter(FrameworkInfo.PluginId)]
internal static class EthanolLineFirePatch
{
    private static void Prefix(CondOwner __instance, out bool __state) => __state = __instance != null && !__instance.HasCond("IsDamaged");
    private static void Postfix(CondOwner __instance, CondOwner coNew, bool __state)
    {
        if (!__state || coNew == null || !coNew.HasCond("IsDamaged") || !EthanolLineRules.Rules.IsFamily(coNew.strCODef)) return;
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return;
        try
        {
            var family = LiquidStores.EthanolFamily; var m = LineContents.Read(coNew);
            double kg = family.BurnKg(m?.Of(family.Commodity) ?? 0);
            if (m == null || kg <= 1e-9) return;
            var burn = StoreService.IgniteSpill(coNew, family.Fuel!, kg, take => { double taken = m.Take(family.Commodity, take); return LineContents.Write(coNew, m) ? taken : 0; });
            if (burn == null) return;
            string message = Text.Get("EthanolLine.fire", coNew.strNameFriendly, burn.BurnedKg * 1000, burn.EnergyKJ / 1000);
            Plugin.Log(message);
            PlayerNotices.Post(coNew.ship, family.NoticeId, NoticeLevel.Caution, message);
        }
        catch (Exception e) { Plugin.Log(e.ToString()); }
    }
}
