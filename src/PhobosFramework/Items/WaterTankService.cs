using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Trading;

namespace Phobos.Ostranauts.Framework.Items;

/// <summary>Commands for the water tanks (Framework 0.58.0; Shipbreaker's silo service before): status, reserve,
/// recovery after repair, owner-confirmed acceptance, and the optional Ship's Water draw and waste deposit. Water in
/// and out goes through Framework's guarded transfers; the tank itself runs nothing.</summary>
public static class WaterTankService
{
    public const string Owner = "phobosgekko.ostranauts.framework.tanks";
    public const double AccessTiles = 2.5;
    /// <summary>Drinking water Ship's Water keeps for the crew when a tank draws from it (Framework's setting).</summary>
    public static double CrewReserveKg { get; set; } = WaterTanks.DefaultCrewReserveKg;
    public static Action<string> Log { get; set; } = _ => { };
    /// <summary>The station Bulk supplies offer: process water into any installed tank at the authored price; a
    /// quote is bounded by the chosen tank's room and the largest tank's steps.</summary>
    public static readonly VesselSupplyProvider Supplies = new(Owner, () => FrameworkItems.Ready ?
        new[] { (new BulkSupplyOffer("framework.water", Text.Get("WaterTanks.offer"), Text.Get("WaterTanks.unit_kg"), WaterTanks.WaterPricePerKg, WaterTanks.PurchaseStepKg,
            (int)Math.Ceiling(WaterTanks.All.Max(t => t.CapacityKg) / WaterTanks.PurchaseStepKg)), (IReadOnlyList<string>)WaterTanks.All.Select(t => t.Prefix).ToArray()) } :
        Array.Empty<(BulkSupplyOffer, IReadOnlyList<string>)>());

    public static string? Access(CondOwner co, ConsoleBinding? binding)
    {
        var actor = CrewWork.Actor ?? CrewSim.GetSelectedCrew();
        if (actor == null || actor.bDestroyed || actor.HasCond("IsDead") || actor.HasCond("Unconscious") || co.bDestroyed || co.ship == null || actor.ship != co.ship)
            return Text.Get("WaterTanks.access");
        if (binding == null) return CrewWork.LocalAccess(actor, co, AccessTiles) ? null : Text.Get("WaterTanks.access");
        return ConsoleAuthority.Check(co, binding, AccessTiles, actor: actor) == ConsoleAccessFailure.None ? null : Text.Get("WaterTanks.access");
    }
    public static string Describe(CondOwner co)
    {
        if (BulkVessel.Protected(co)) return Text.Get("WaterTanks.protected");
        var s = BulkVessel.Snapshot(co);
        // Every machine linked to the tank, from any mod.
        string linked = string.Join(", ", LinkChoices.LinkedNames(co));
        if (linked.Length == 0) linked = ConsoleText.Get("not_selected");
        // Ship's Water tanks count only when they touch the silo or share its water line (Framework 0.59.0).
        string reach = ShipsWaterSupply.Available ? "\n" + Text.Get("WaterTanks.shipswater_reach", ShipsWaterSupply.ReachableTanks(co).Count, ShipsWaterSupply.ReachableTanks(co, waste: true).Count) : "";
        return Text.Get("WaterTanks.status", s.ServiceKg, s.CatchKg, s.CapacityKg, s.ReserveKg, linked) + reach + (s.CatchKg > 0 ? "\n" + Text.Get("WaterTanks.catch_wait") : "");
    }
    public static EquipmentState State(CondOwner co) => BulkVessel.Protected(co) || co.HasCond("IsDamaged") || co.HasCond("IsLocked") || BulkVessel.Snapshot(co).CatchKg > 0 ? EquipmentState.Blocked : EquipmentState.Ready;
    /// <summary>Why removal work is refused at offer time: a protected tank stays in place, and a tank still holding
    /// water is not dismantled (that would delete the water). Uninstalling carries the water along.</summary>
    public static string? MaintenanceReason(CondOwner co, bool dismantle)
    {
        if (!WaterTanks.IsTank(co)) return null;
        if (BulkVessel.Protected(co)) return Text.Get("WaterTanks.maintenance_protected");
        var s = BulkVessel.Snapshot(co);
        return dismantle && s.ServiceKg + s.CatchKg > 1e-8 ? Text.Get("WaterTanks.maintenance_water") : null;
    }
    public static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Text.Get("WaterTanks.protected");
        if (!FrameworkItems.Ready) return false;
        message = Access(co, binding) ?? "";
        if (message.Length > 0) return false;
        if (action == "accept") { bool ok = BulkVessel.Accept(co, Log); message = Text.Get(ok ? "WaterTanks.accept_done" : "WaterTanks.accept_unavailable"); return ok; }
        if (action == "pause") { message = Text.Get("WaterTanks.nothing_to_pause"); return true; }
        if (action == "status") { message = Describe(co); return true; }
        if (BulkVessel.Protected(co)) { message = Text.Get("WaterTanks.protected"); return false; }
        if (!NativeFluidRoute.EndpointReady(co) || CommodityReservations.Held(co.strID)) { message = Text.Get("WaterTanks.not_ready"); return false; }
        try
        {
            var spec = BulkVessel.Spec(co);
            if (action == "recover")
            {
                var s = BulkVessel.Read(co, spec);
                if (s.CatchKg <= 0) { message = Text.Get("WaterTanks.nothing_to_recover"); return false; }
                double trapped = s.CatchKg; s.Recover(); BulkVessel.Save(co, spec, s);
                message = Text.Get("WaterTanks.recovered", trapped); return true;
            }
            string verb = new[] { "reserve:", "draw:", "to-waste:" }.FirstOrDefault(v => action.StartsWith(v, StringComparison.Ordinal)) ?? "";
            if (verb.Length == 0) { message = Text.Get("WaterTanks.unsupported"); return false; }
            if (!double.TryParse(action.Substring(verb.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out double kg) || !WaterTanks.ValidAmount(kg, spec.CapacityKg))
            { message = Text.Get("WaterTanks.invalid_amount"); return false; }
            if (verb == "reserve:") { var s = BulkVessel.Read(co, spec); s.SetReserve(kg); BulkVessel.Save(co, spec, s); message = Text.Get("WaterTanks.done"); return true; }
            if (!ShipsWaterSupply.Available) { message = Text.Get("WaterTanks.no_shipswater"); return false; }
            if (kg <= 0) { message = Text.Get("WaterTanks.invalid_amount"); return false; }
            var endpoint = new BulkVessel.Endpoint(co); var guard = BulkVessel.Guard(co);
            if (verb == "draw:")
            {
                if (ShipsWaterSupply.ReachableTanks(co).Count == 0) { message = Text.Get("WaterTanks.no_tank_in_reach"); return false; }
                double got = ShipsWaterSupply.Refill(co, endpoint, kg, CrewReserveKg, guard);
                message = got > 0 ? Text.Get("WaterTanks.drawn", got, CrewReserveKg) : Text.Get("WaterTanks.nothing_drawn");
                return got > 0;
            }
            // The reserve is what the player keeps aboard; a deposit never takes from it.
            double request = Math.Min(kg, BulkVessel.Snapshot(co).AvailableKg);
            if (ShipsWaterSupply.ReachableTanks(co, waste: true).Count == 0) { message = Text.Get("WaterTanks.no_waste_in_reach"); return false; }
            double sent = request <= 0 ? 0 : ShipsWaterSupply.DepositWaste(co, endpoint, request, guard);
            message = sent > 0 ? Text.Get("WaterTanks.deposited", sent) : Text.Get("WaterTanks.nothing_deposited");
            return sent > 0;
        }
        catch (Exception e) { Log(e.ToString()); message = Text.Get("WaterTanks.protected"); return false; }
    }
}

/// <summary>The tanks' Control Panel and console presentation on Framework's shared host. Choices are Framework's;
/// every application goes through <see cref="WaterTankService"/>.</summary>
public sealed class WaterTankProvider : IEquipmentProvider, IEquipmentPanelFields
{
    public const string PanelKey = "PhobosFrameworkTankPanel", Group = "tank";
    public string Id => WaterTankService.Owner;
    public IReadOnlyList<string> Definitions { get; } = Array.AsReadOnly(WaterTanks.All.SelectMany(t => new[] { t.Installed, t.Installed + "Dmg" }).ToArray());
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    public IEnumerable<EquipmentField> Fields(CondOwner co)
    {
        if (!WaterTanks.IsTank(co) || BulkVessel.Protected(co)) yield break;
        var s = BulkVessel.Snapshot(co);
        yield return new(Text.Get("WaterTanks.reserve_field"), Text.Get("WaterTanks.kg", s.ReserveKg),
            WaterTanks.ReserveChoicesFor(s.CapacityKg).Select(n => ("reserve:" + N(n), Text.Get("WaterTanks.kg", n))), "reserve:" + N(s.ReserveKg));
        if (!ShipsWaterSupply.Available) yield break;
        yield return new(Text.Get("WaterTanks.draw_field"), Text.Get("WaterTanks.kg", s.AvailableKg), WaterTanks.TransferChoices.Select(n => ("draw:" + N(n), Text.Get("WaterTanks.kg", n))));
        yield return new(Text.Get("WaterTanks.waste_field"), Text.Get("WaterTanks.kg", s.AvailableKg), WaterTanks.TransferChoices.Select(n => ("to-waste:" + N(n), Text.Get("WaterTanks.kg", n))));
    }
    public bool IsConfiguration(string action) => action.StartsWith("reserve:", StringComparison.Ordinal) || action.StartsWith("draw:", StringComparison.Ordinal) ||
        action.StartsWith("to-waste:", StringComparison.Ordinal);
    public string ConfigurationStamp(CondOwner co) => Controls.ConfigurationStamp.For(co, new[] { "PhobosMaterialPort." }.Concat(WaterTanks.All.Select(t => "PhobosState." + t.Record)).ToArray());
    public bool ApplyConfiguration(CondOwner co, ConsoleBinding? binding, string expected, string action, out string reason)
    {
        reason = ConsoleText.Get("stale");
        if (co.bDestroyed || expected != ConfigurationStamp(co) || !IsConfiguration(action)) return false;
        return WaterTankService.Command(co, binding, action, out reason);
    }
    public EquipmentSnapshot Snapshot(CondOwner co)
    {
        var actions = new List<EquipmentAction>();
        if (BulkVessel.Protected(co)) actions.Add(new EquipmentAction("accept", Text.Get("WaterTanks.action_accept")));
        else if (BulkVessel.Snapshot(co).CatchKg > 0) actions.Add(new EquipmentAction("recover", Text.Get("WaterTanks.action_recover")));
        return new EquipmentSnapshot(co.strID, co.strNameFriendly, Group, new EquipmentActivity(WaterTankService.State(co), WaterTankService.Describe(co)), actions);
    }
    public bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message) => WaterTankService.Command(co, binding, action, out message);

    private static readonly WaterTankProvider instance = new();
    /// <summary>Registers the provider, its console group and its Control Panel on the shared host (once, from the plugin).</summary>
    public static void Register(Action<string> log)
    {
        EquipmentProviders.Register(instance);
        EquipmentProviders.RegisterGroup(Group, () => Text.Get("WaterTanks.group"));
        ProviderPanel.Register(new ProviderPanelSpec
        {
            Key = PanelKey, Provider = instance, Handles = WaterTanks.IsTank, Access = co => WaterTankService.Access(co, null),
            Resolve = id => CrewWork.Resolve(id), Text = key => Text.Get("WaterTanks.panel_" + key), Log = log
        });
    }
}

// The tank's own Control Panel interaction opens the shared host for the selected crew member.
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class WaterTankControlsPatch
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (isCancelIa || __instance.strName != WaterTanks.Controls || !WaterTanks.IsTank(__instance.objThem)) return;
        if (__instance.objUs == CrewSim.GetSelectedCrew()) ProviderPanel.Show(WaterTankProvider.PanelKey, __instance.objThem);
    }
}

// Removal work on a tank is refused when it is offered, never by blocking native destruction (owner decision,
// 28 September 2026): a protected tank stays put, and a tank holding water is not dismantled. A refused finish still
// closes the game's task.
[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class WaterTankMaintenanceOffer
{
    private static void Postfix(Interaction __instance, CondOwner objUs, CondOwner objThem, ref bool __result)
    {
        var reason = __result ? WaterTankMaintenanceFinish.Reason(__instance.strName, objUs, objThem) : null;
        if (reason != null) { __instance.AddFailReason("main", reason); __result = false; }
    }
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class WaterTankMaintenanceFinish
{
    internal static string? Reason(string action, CondOwner? us, CondOwner? them)
    {
        // This runs for every offer and completion in the game: a tank is recognised before any name search.
        var tank = us != null && WaterTanks.IsTank(us.strCODef) ? us : them != null && WaterTanks.IsTank(them.strCODef) ? them : null;
        if (tank == null || action == null) return null;
        bool dismantle = action.IndexOf("Dismantle", StringComparison.OrdinalIgnoreCase) >= 0;
        bool removal = dismantle || action.IndexOf("Uninstall", StringComparison.OrdinalIgnoreCase) >= 0;
        return removal ? WaterTankService.MaintenanceReason(tank, dismantle) : null;
    }
    private static bool Prefix(Interaction __instance)
    {
        var reason = Reason(__instance.strName, __instance.objUs, __instance.objThem);
        return reason == null || Registration.NativeEffects.Refuse(__instance, reason);
    }
}
