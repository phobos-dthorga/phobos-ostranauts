using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosAgriculture.Core;

namespace PhobosAgriculture;

internal static partial class Service
{
    private const string WaterPortId = "PhobosAgriculture.Water";
    internal static LiquidTransferGuard WaterGuard(CondOwner co) => new(co.mapGUIPropMaps, "AgricultureWaterTransfer", Plugin.Id);
    private static MaterialPort WaterPort(CondOwner co) => new(co.strID, WaterPortId, co.mapGUIPropMaps);
    private static ObjectStateStore WaterMode(CondOwner co) => new(co.mapGUIPropMaps, "AgricultureWaterMode", Plugin.Id, 1);
    private static void ReadWaterMode(Session s)
    {
        var status = WaterMode(s.Object).Read(out var fields);
        if (status == SavedStateStatus.Missing) return; // Existing racks keep their original provider route.
        if (status != SavedStateStatus.Ready || fields.Count != 1 || !fields.TryGetValue("mode", out var mode) || (mode != "legacy" && mode != "routed"))
        { s.Protected = true; return; }
        s.Routed = mode == "routed";
        if (s.Routed && (Definitions.IsCooker(s.Object) || IrrigationDefinitions.IsSupply(s.Object))) s.Protected = true;
    }
    private static void SetWaterMode(Session s, bool routed)
    {
        if (!WaterMode(s.Object).TryWrite(new Dictionary<string, string> { ["mode"] = routed ? "routed" : "legacy" }))
            throw new InvalidOperationException("Protected irrigation mode.");
        s.Routed = routed; s.State.Receiving = false;
    }
    private static CondOwner? WaterPeer(CondOwner co)
    {
        var link = PortPairing.Read(WaterPort(co));
        return link.State == PortLinkState.Linked ? Resolve(link.PeerObjectId) : null;
    }
    private static bool Paused(Session s) => !s.State.Running && !s.State.Receiving;
    internal static string[] Actions(CondOwner co) => IrrigationDefinitions.IsSupply(co)
        ? new[] { "start", "pause", "receive", "pause-receive", "unlink-water", "mix-potato", "mix-lettuce", "water-only" }
        : Definitions.IsCooker(co) ? new[] { "start", "pause", "cancel" }
        : new[] { "start", "pause", "receive", "pause-receive", "water-routed", "water-legacy", "unlink-water" };
    internal static IEnumerable<CondOwner> WaterCandidates(CondOwner co) =>
        co.ship.GetCOs(null, false, false, true).Where(c => c != co && c.ship == co.ship && Definitions.Machine(c) &&
            !Definitions.IsCooker(c) && IrrigationDefinitions.IsSupply(c) != IrrigationDefinitions.IsSupply(co) && NativeFluidRoute.EndpointReady(c));

    private static bool? WaterCommand(Session s, string action, out string message)
    {
        message = "";
        if (!action.StartsWith("link-water:", StringComparison.Ordinal) && action != "unlink-water" && action != "water-routed" && action != "water-legacy") return null;
        var co = s.Object;
        if (Definitions.IsCooker(co) || !Paused(s) || !NativeFluidRoute.EndpointReady(co)) { message = Text.Get("water_pause"); return false; }
        if (action == "water-routed" || action == "water-legacy")
        {
            if (IrrigationDefinitions.IsSupply(co)) { message = Text.Get("help"); return false; }
            var peer = WaterPeer(co);
            if (peer != null && Definitions.Machine(peer) && !Paused(Get(peer))) { message = Text.Get("water_pause"); return false; }
            if (s.Solution.TotalKg > NutrientSolution.Tolerance) { message = Text.Get("solution_switch"); return false; }
            s.Solution.Profile = NutrientSolution.None; Save(s);
            SetWaterMode(s, action == "water-routed"); message = Describe(co); return true;
        }
        var other = action == "unlink-water" ? WaterPeer(co) : Resolve(action.Substring("link-water:".Length));
        if (other != null && (!Definitions.Machine(other) || Definitions.IsCooker(other) || co.ship != other.ship ||
            IrrigationDefinitions.IsSupply(co) == IrrigationDefinitions.IsSupply(other) || !NativeFluidRoute.EndpointReady(other) ||
            Get(other).Protected || WaterGuard(other).Protected || !Paused(Get(other))))
        { message = Text.Get("water_pause"); return false; }
        if (action == "unlink-water")
        {
            PortPairing.Unlink(WaterPort(co), other == null ? null : WaterPort(other));
            // Deliberately retain routed mode; unlink never re-enables a bypass.
            message = Describe(co); return true;
        }
        if (other == null) { message = Text.Get("water_missing"); return false; }
        var source = IrrigationDefinitions.IsSupply(co) ? co : other;
        var rack = source == co ? other : co;
        var sourceState = Get(source); var rackState = Get(rack);
        if (rackState.Solution.TotalKg > NutrientSolution.Tolerance && rackState.Solution.Profile != sourceState.Solution.Profile ||
            sourceState.Solution.Enabled && rackState.State.CropId.Length > 0 && NutrientSolution.CropId(sourceState.Solution.Profile) != rackState.State.CropId)
        { message = Text.Get("solution_incompatible"); return false; }
        if (!PortPairing.TryLink(WaterPort(source), WaterPort(rack), out message)) return false;
        rackState.Solution.Profile = sourceState.Solution.Profile; Save(rackState);
        SetWaterMode(rackState, true); message = Describe(co); return true;
    }
    private static Session? Destination(Session source, bool requireReceiving)
    {
        var co = source.Object; var peer = WaterPeer(co);
        if (!NativeFluidRoute.EndpointReady(co) || WaterGuard(co).Protected || source.Protected || peer == null ||
            !Definitions.Machine(peer) || Definitions.IsCooker(peer) || IrrigationDefinitions.IsSupply(peer) || peer.ship != co.ship ||
            !NativeFluidRoute.EndpointReady(peer) || !PortPairing.Matches(WaterPort(co), WaterPort(peer))) return null;
        var target = Get(peer);
        return target.Protected || WaterGuard(peer).Protected || !target.Routed || !CompatibleSolution(source, target) || requireReceiving && !target.State.Receiving ? null : target;
    }
    private static int[]? Route(Session source, Session target)
    {
        bool Segment(CondOwner c) => c.strCODef == IrrigationDefinitions.Pipe + "Installed";
        var path = NativeFluidRoute.Find(source.Object, IrrigationDefinitions.Outlet, target.Object, IrrigationDefinitions.Inlet, Segment);
        if (path == null) return null;
        // First slice has one pump authority per connected component. Additional independent
        // circuits are fine; joining two live source outlets blocks delivery instead of multiplying flow.
        foreach (var other in source.Object.ship.GetCOs(null, false, false, true))
            if (other != source.Object && Definitions.Machine(other) && IrrigationDefinitions.IsSupply(other) &&
                other.ship == source.Object.ship && NativeFluidRoute.EndpointReady(other) &&
                NativeFluidRoute.Find(source.Object, IrrigationDefinitions.Outlet, other, IrrigationDefinitions.Outlet, Segment) != null) return null;
        return path;
    }
    private static double SupplyDemand(Session s)
    {
        if (!NativeFluidRoute.EndpointReady(s.Object) || s.Protected || WaterGuard(s.Object).Protected) return 0;
        if (s.State.Receiving && ShipsWaterSupply.Available && ProviderHeadroom(s) > NutrientSolution.Tolerance) return IrrigationDefinitions.PumpKW;
        if (!s.State.Running) return 0;
        if (s.Solution.BlendAllowance(s.State, 1) > NutrientSolution.Tolerance) return IrrigationDefinitions.PumpKW;
        var target = Destination(s, true);
        return target != null && CanDeliver(s, target) && Route(s, target) != null ? IrrigationDefinitions.PumpKW : 0;
    }
    private static void Pump(Session s, double elapsed, double energy)
    {
        if (!NativeFluidRoute.EndpointReady(s.Object) || s.Protected || WaterGuard(s.Object).Protected) return;
        double budget = LiquidDeliveryBudget.Kilograms(elapsed, energy, IrrigationDefinitions.RateKgPerSecond, IrrigationDefinitions.EnergyKWhPerKg);
        if (budget <= 0) return;
        // Outgoing feed, mixing and provider filling share ONE measured pump budget.
        if (s.State.Running)
        {
            var target = Destination(s, true);
            if (target != null && CanDeliver(s, target) && Route(s, target) != null)
            {
                try { budget -= DeliverLiquid(s, target, budget); }
                catch (Exception error) { Fault(target.Object, error); throw; }
            }
            else s.Notice = Text.Get("water_no_route");
            if (budget > NutrientSolution.Tolerance && s.Solution.Enabled)
            {
                double mixed = s.Solution.Blend(s.State, budget); budget -= mixed;
                if (mixed > 0) { Save(s); s.Notice = Text.Get("solution_mixed", mixed); }
            }
        }
        if (s.State.Receiving && budget > NutrientSolution.Tolerance)
            ShipsWaterSupply.Refill(s.Object.ship, new Reservoir(s), Math.Min(budget, ProviderHeadroom(s)), Plugin.ReserveLitres.Value, WaterGuard(s.Object));
    }
    private static string DescribeWaterRoute(Session s)
    {
        if (Definitions.IsCooker(s.Object)) return "";
        var link = PortPairing.Read(WaterPort(s.Object));
        return Text.Get("water_route_status", Text.Get(s.Routed ? "water_routed_mode" : "water_legacy_mode"),
            link.State == PortLinkState.Linked ? link.PeerObjectId : Text.Get(link.State == PortLinkState.Invalid ? "protected" : "water_unlinked")) + "\n" + DescribeSolution(s);
    }
    private static string DescribeSupply(Session s)
    {
        var target = Destination(s, false); var route = target == null ? null : Route(s, target);
        return Text.Get("water_supply_status", s.State.Water, IrrigationDefinitions.CapacityKg, Text.Get(s.State.Running ? "running" : "paused"),
            Text.Get(s.State.Receiving ? "receiving" : "manual"), target?.Object.strID ?? Text.Get("water_unlinked"),
            route == null ? Text.Get("water_no_route") : Text.Get("water_route_length", route.Length),
            s.Protected || WaterGuard(s.Object).Protected ? Text.Get("protected") : s.Notice) + "\n" +
            (StarSystem.fEpoch - s.LastPower <= 5 ? Text.Get("power_reading", s.DeliveredKW) : Text.Get("power_unknown")) + "\n" + DescribeSolution(s);
    }
}
