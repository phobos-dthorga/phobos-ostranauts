using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Trading;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Commands for the S3 process-water silo: status, reserve, recovery after repair, owner-confirmed
/// acceptance, and the optional Ship's Water draw and waste deposit. Water in and out of the silo goes through
/// Framework's guarded transfers; the silo itself runs nothing.</summary>
internal static class SiloService
{
    /// <summary>Every silo size's declaration, S3 first; the S3's is <see cref="Spec"/>.</summary>
    internal static readonly BulkVesselSpec[] Specs = SiloRules.Sizes.Select(s => new BulkVesselSpec(s.Prefix, SiloRules.Commodity, s.CapacityKg, s.DryKg, Plugin.Id, s.Record, s.Journal, s.Guard)).ToArray();
    internal static BulkVesselSpec Spec => Specs[0];
    /// <summary>The station Bulk supplies offer: process water into an installed S3 at the authored price.</summary>
    /// <summary>One offer fills every silo size; a quote is bounded by the chosen silo's room and the largest silo's steps.</summary>
    internal static readonly VesselSupplyProvider Supplies = new(Plugin.Id, () => Content.Ready ?
        new[] { (new BulkSupplyOffer("shipbreaker.water", Text.Get("Silo.offer"), Text.Get("Silo.unit_kg"), SiloRules.WaterPricePerKg, SiloRules.PurchaseStepKg,
            (int)Math.Ceiling(SiloRules.Sizes.Max(s => s.CapacityKg) / SiloRules.PurchaseStepKg)), (IReadOnlyList<string>)SiloRules.Sizes.Select(s => s.Prefix).ToArray()) } :
        Array.Empty<(BulkSupplyOffer, IReadOnlyList<string>)>());
    internal static IEnumerable<CondOwner> ThawUnits(CondOwner silo) => (silo.ship?.GetCOs(null, false, false, true) ?? Enumerable.Empty<CondOwner>())
        .Where(c => c != null && !c.bDestroyed && ThawRules.IsFamily(c.strCODef) && ThawService.Peer(c) == silo.strID).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    internal static string Describe(CondOwner co)
    {
        if (BulkVessel.Protected(co)) return Text.Get("Silo.protected");
        var s = BulkVessel.Snapshot(co);
        string units = string.Join(", ", ThawUnits(co).Select(ObjectPresentation.Name));
        if (units.Length == 0) units = ConsoleText.Get("not_selected");
        return Text.Get("Silo.status", s.ServiceKg, s.CatchKg, s.CapacityKg, s.ReserveKg, units) + (s.CatchKg > 0 ? "\n" + Text.Get("Silo.catch_wait") : "");
    }
    internal static EquipmentState State(CondOwner co) => BulkVessel.Protected(co) || co.HasCond("IsDamaged") || co.HasCond("IsLocked") || BulkVessel.Snapshot(co).CatchKg > 0 ? EquipmentState.Blocked : EquipmentState.Ready;
    /// <summary>Why removal work is refused at offer time: a protected silo stays in place, and a silo still
    /// holding water is not dismantled (that would delete the water). Uninstalling carries the water along.</summary>
    internal static string? MaintenanceReason(CondOwner co, bool dismantle)
    {
        if (!SiloRules.IsFamily(co.strCODef)) return null;
        if (BulkVessel.Protected(co)) return Text.Get("Maintenance.protected");
        return dismantle && BulkVessel.Snapshot(co).ServiceKg + BulkVessel.Snapshot(co).CatchKg > 1e-8 ? Text.Get("Silo.maintenance_water") : null;
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Text.Get("Silo.protected");
        if (!Content.Ready) return false;
        message = ProcessingService.AccessProblem(co, binding) ?? "";
        if (message.Length > 0) return false;
        if (action == "accept") { bool ok = BulkVessel.Accept(co, Plugin.Log); message = Text.Get(ok ? "Silo.accept_done" : "Silo.accept_unavailable"); return ok; }
        if (action == "pause") { message = Text.Get("Silo.nothing_to_pause"); return true; }
        if (action == "status") { message = Describe(co); return true; }
        if (BulkVessel.Protected(co)) { message = Text.Get("Silo.protected"); return false; }
        if (!NativeFluidRoute.EndpointReady(co) || CommodityReservations.Held(co.strID)) { message = Text.Get("Silo.not_ready"); return false; }
        try
        {
            if (action == "recover")
            {
                var s = BulkVessel.Read(co, BulkVessel.Spec(co));
                if (s.CatchKg <= 0) { message = Text.Get("Silo.nothing_to_recover"); return false; }
                double trapped = s.CatchKg; s.Recover(); BulkVessel.Save(co, BulkVessel.Spec(co), s);
                message = Text.Get("Silo.recovered", trapped); return true;
            }
            string verb = new[] { "reserve:", "draw:", "to-waste:" }.FirstOrDefault(v => action.StartsWith(v, StringComparison.Ordinal)) ?? "";
            if (verb.Length == 0) { message = Text.Get("Industry.unsupported_action"); return false; }
            if (!double.TryParse(action.Substring(verb.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out double kg) || !SiloRules.ValidAmount(kg, BulkVessel.Spec(co).CapacityKg))
            { message = Text.Get("Silo.invalid_amount"); return false; }
            if (verb == "reserve:") { var s = BulkVessel.Read(co, BulkVessel.Spec(co)); s.SetReserve(kg); BulkVessel.Save(co, BulkVessel.Spec(co), s); message = Text.Get("Silo.done"); return true; }
            if (!ShipsWaterSupply.Available) { message = Text.Get("Silo.no_shipswater"); return false; }
            if (kg <= 0) { message = Text.Get("Silo.invalid_amount"); return false; }
            var endpoint = new BulkVessel.Endpoint(co); var guard = BulkVessel.Guard(co);
            if (verb == "draw:")
            {
                double got = ShipsWaterSupply.Refill(co.ship, endpoint, kg, Plugin.Options.CrewWaterReserveKg, guard);
                message = got > 0 ? Text.Get("Silo.drawn", got, Plugin.Options.CrewWaterReserveKg) : Text.Get("Silo.nothing_drawn");
                return got > 0;
            }
            // The reserve is what the player keeps aboard; a deposit never takes from it.
            double request = Math.Min(kg, BulkVessel.Snapshot(co).AvailableKg);
            double sent = request <= 0 ? 0 : ShipsWaterSupply.DepositWaste(co.ship, endpoint, request, guard);
            message = sent > 0 ? Text.Get("Silo.deposited", sent) : Text.Get("Silo.nothing_deposited");
            return sent > 0;
        }
        catch (Exception e) { Plugin.Log(e.ToString()); message = Text.Get("Silo.protected"); return false; }
    }
}

/// <summary>Local panel and C1 presentation of the S3 and T2 through Framework's equipment-provider contract.
/// Choices are content-owned; every application goes through the checked services above.</summary>
internal sealed class VesselProvider : IEquipmentProvider, IEquipmentPanelFields
{
    public string Id => Plugin.Id;
    public IReadOnlyList<string> Definitions { get; } = Array.AsReadOnly(SiloRules.Sizes.Select(s => s.Installed).Concat(new[] { ThawRules.Installed })
        .SelectMany(id => new[] { id, id + "Dmg" }).ToArray());
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    public IEnumerable<EquipmentField> Fields(CondOwner co)
    {
        if (SiloRules.IsFamily(co.strCODef))
        {
            if (BulkVessel.Protected(co)) yield break;
            var s = BulkVessel.Snapshot(co);
            yield return new(Text.Get("Silo.reserve_field"), Text.Get("Silo.kg", s.ReserveKg), SiloRules.ReserveChoicesFor(s.CapacityKg).Select(n => ("reserve:" + N(n), Text.Get("Silo.kg", n))));
            if (!ShipsWaterSupply.Available) yield break;
            yield return new(Text.Get("Silo.draw_field"), Text.Get("Silo.kg", s.AvailableKg), SiloRules.TransferChoices.Select(n => ("draw:" + N(n), Text.Get("Silo.kg", n))));
            yield return new(Text.Get("Silo.waste_field"), Text.Get("Silo.kg", s.AvailableKg), SiloRules.TransferChoices.Select(n => ("to-waste:" + N(n), Text.Get("Silo.kg", n))));
        }
        else if (ThawRules.IsFamily(co.strCODef))
            yield return new(Text.Get("Thaw.vessel_field"), ObjectPresentation.Name(ThawService.Peer(co)),
                ThawService.Candidates(co).Select(v => ("link:" + v.strID, ObjectPresentation.Name(v))).Concat(new[] { ("link:none", Text.Get("Thaw.link_none")) }));
    }
    public bool IsConfiguration(string action) => action.StartsWith("reserve:", StringComparison.Ordinal) || action.StartsWith("draw:", StringComparison.Ordinal) ||
        action.StartsWith("to-waste:", StringComparison.Ordinal) || action.StartsWith("link:", StringComparison.Ordinal);
    public string ConfigurationStamp(CondOwner co) => Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.For(co, new[] { "PhobosMaterialPort.", "PhobosState.crew-order" }.Concat(SiloRules.Sizes.Select(s => "PhobosState." + s.Record)).ToArray());
    public bool ApplyConfiguration(CondOwner co, ConsoleBinding? binding, string expected, string action, out string reason)
    {
        reason = ConsoleText.Get("stale");
        if (co.bDestroyed || expected != ConfigurationStamp(co) || !IsConfiguration(action)) return false;
        bool saved = Command(co, binding, action, out reason);
        if (saved) Phobos.Ostranauts.Framework.Controls.ConfigurationStamp.SuspendChangedOrder(co);
        return saved;
    }
    public EquipmentSnapshot Snapshot(CondOwner co)
    {
        if (SiloRules.IsFamily(co.strCODef))
        {
            var actions = new List<EquipmentAction>();
            if (BulkVessel.Protected(co)) actions.Add(new EquipmentAction("accept", Text.Get("Silo.action_accept")));
            else if (BulkVessel.Snapshot(co).CatchKg > 0) actions.Add(new EquipmentAction("recover", Text.Get("Silo.action_recover")));
            return new EquipmentSnapshot(co.strID, co.strNameFriendly, "silo", new EquipmentActivity(SiloService.State(co), SiloService.Describe(co)), actions);
        }
        return new EquipmentSnapshot(co.strID, co.strNameFriendly, "thaw", new EquipmentActivity(ThawService.State(co), ThawService.Describe(co)),
            new[] { new EquipmentAction("start", Text.Get("Thaw.action_start")), new EquipmentAction("pause", Text.Get("Thaw.action_pause")), new EquipmentAction("cancel", Text.Get("Thaw.action_cancel")) });
    }
    public bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message) =>
        SiloRules.IsFamily(co.strCODef) ? SiloService.Command(co, binding, action, out message) : ThawService.Command(co, binding, action, out message);
}

// Removal work on a silo is refused when it is offered, never by blocking native destruction (owner decision,
// 28 September 2026): a protected silo stays put, and a silo holding water is not dismantled. A refused finish
// still closes the game's task, as the furnace's own guards do.
[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class SiloMaintenanceOffer
{
    private static void Postfix(Interaction __instance, CondOwner objUs, CondOwner objThem, ref bool __result)
    {
        var reason = __result ? SiloMaintenanceFinish.Reason(__instance.strName, objUs, objThem) : null;
        if (reason != null) { __instance.AddFailReason("main", reason); __result = false; }
    }
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class SiloMaintenanceFinish
{
    internal static string? Reason(string action, CondOwner? us, CondOwner? them)
    {
        bool dismantle = action.IndexOf("Dismantle", StringComparison.OrdinalIgnoreCase) >= 0;
        bool removal = dismantle || action.IndexOf("Uninstall", StringComparison.OrdinalIgnoreCase) >= 0;
        if (!removal) return null;
        var silo = new[] { us, them }.FirstOrDefault(c => c != null && SiloRules.IsFamily(c.strCODef));
        return silo == null ? null : SiloService.MaintenanceReason(silo, dismantle);
    }
    private static bool Prefix(Interaction __instance)
    {
        var reason = Reason(__instance.strName, __instance.objUs, __instance.objThem);
        return reason == null || Phobos.Ostranauts.Framework.Registration.NativeEffects.Refuse(__instance, reason);
    }
}
