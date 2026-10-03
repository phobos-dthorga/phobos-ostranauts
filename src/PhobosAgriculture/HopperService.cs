using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Liquids;

namespace PhobosAgriculture;

/// <summary>The nutrient hopper over Framework's bulk vessel: its status, owner-confirmed acceptance, recovery of
/// contents a repair left in the catch chamber, and the W2 side: which hoppers a W2 may dose from, and the guarded
/// transfer of a mixing step's nutrients into the W2's dry-nutrient reservoir. Nothing is ever unloaded as items.</summary>
internal static class HopperService
{
    internal static bool Protected(CondOwner co) => BulkVessel.Protected(co);
    internal static StoredCommodity Read(CondOwner co) => BulkVessel.Read(co);
    /// <summary>Whether a hopper can dose now: intact, installed, ready, readable, no catch-chamber contents, not held.</summary>
    internal static bool Ready(CondOwner hopper) => Ready(hopper, null);
    /// <summary>As <see cref="Ready(CondOwner)"/>, not counting a reservation <paramref name="operation"/> holds itself: a station
    /// purchase reserves the hopper and then checks it once more (Agriculture 0.39.0).</summary>
    internal static bool Ready(CondOwner hopper, string? operation) => HopperDefinitions.IsHopper(hopper) && !hopper.HasCond("IsDamaged") && NativeFluidRoute.EndpointReady(hopper) &&
        !Protected(hopper) && !CommodityReservations.HeldByOther(hopper.strID, operation) && Read(hopper).CatchKg <= 1e-8;
    internal static double Available(CondOwner hopper) => Ready(hopper) ? Read(hopper).AvailableKg : 0;
    /// <summary>Whether this hopper can dose this W2 now: same ship, within one tile, both ready, nutrients to give.</summary>
    internal static bool CanDose(CondOwner hopper, CondOwner w2) => hopper.ship != null && hopper.ship == w2.ship && Ready(hopper) && hopper.HasCond("IsInstalled") &&
        NativeFluidRoute.EndpointReady(w2) && BulkVessels.Adjacent(hopper, w2) && Read(hopper).AvailableKg > 1e-10;
    /// <summary>Hoppers within one tile of a W2 on the same ship (Framework's shared vessel rule). Scans the ship: for
    /// choosing a source, not for the power step.</summary>
    internal static IEnumerable<CondOwner> Near(CondOwner w2) => w2.ship == null ? Enumerable.Empty<CondOwner>() :
        BulkVessels.Aboard(w2.ship, HopperRules.Commodity).Where(h => HopperDefinitions.IsHopper(h) && BulkVessels.Adjacent(h, w2) && NativeFluidRoute.EndpointReady(w2))
            .OrderBy(h => h.strID, StringComparer.Ordinal);
    /// <summary>Moves up to <paramref name="kg"/> from the hopper into the W2's dry-nutrient reservoir under both
    /// transfer guards; returns what arrived.</summary>
    internal static double Dose(CondOwner hopper, CondOwner w2, ILiquidReservoir dryNutrients, double kg, LiquidTransferGuard w2Guard)
    {
        double amount = HopperRules.DoseKg(kg, Available(hopper));
        if (amount <= 1e-10) return 0;
        return LiquidTransferGuard.Commit(new BulkVessel.Endpoint(hopper), dryNutrients, amount, BulkVessel.Guard(hopper), w2Guard).ReceivedKg;
    }
    /// <summary>Why a hopper may not be moved or dismantled now: an unreadable record, or nutrients still in it.</summary>
    internal static string? RemovalReason(CondOwner co) => Protected(co) ? Text.Get("Maintenance.protected") : Read(co).TotalKg > 1e-8 ? Text.Get("Maintenance.hopper") : null;
    internal static string Describe(CondOwner co)
    {
        if (Protected(co)) return Text.Get("protected");
        var s = Read(co);
        return Text.Get("hopper_status", s.AvailableKg, s.CatchKg, BulkVessels.Of(co)?.CapacityKg ?? 0) + (s.CatchKg > 0 ? "\n" + Text.Get("hopper_catch_wait") : "");
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string reason)
    {
        reason = Text.Get("protected"); if (!Definitions.Ready) return false;
        reason = Service.Access(co, binding) ?? ""; if (reason.Length > 0) return false;
        if (action == "bulk-accept") { bool accepted = BulkVessel.Accept(co, Plugin.Log); reason = Text.Get(accepted ? "accept_done" : "accept_unavailable"); return accepted; }
        if (action == "pause") { CrewWork.ManualStop(co); reason = Text.Get("paused"); return true; }
        if (Protected(co)) return false;
        if (HopperDefinitions.Work.Contains(action) && binding == null)
        { CrewSim.GetSelectedCrew().QueueInteraction(co, DataHandler.GetInteraction(HopperDefinitions.WorkId(action))); reason = Text.Get("queued"); return true; }
        reason = Describe(co); return action == "status";
    }
    /// <summary>Crew work: after repair, contents the damage isolated go back into the hopper.</summary>
    internal static bool Recover(CondOwner co, CondOwner actor)
    {
        if (!Definitions.Ready || Service.Access(co, null, actor) != null || co.HasCond("IsDamaged") || Protected(co) || CommodityReservations.Held(co.strID)) return false;
        try { var s = Read(co); if (s.CatchKg <= 0) return false; s.Recover(); BulkVessel.Save(co, s); return true; }
        catch (Exception e) { Plugin.Log(e.ToString()); return false; }
    }
}
