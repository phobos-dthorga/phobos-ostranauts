using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosAgriculture.Core;

namespace PhobosAgriculture;

internal static partial class Service
{
    private const int RackLimit = 8, RouteTileLimit = 64;
    private static PortBank WaterBank(CondOwner co) => new(co.strID, WaterPortId, co.mapGUIPropMaps, RackLimit);
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
    internal static string[] Actions(CondOwner co) => WorkupDefinitions.IsBench(co) ? WorkupDefinitions.Actions : IrrigationDefinitions.IsSupply(co)
        ? new[] { "start", "pause", "receive", "pause-receive", "unlink-water" }.Concat(Definitions.MixActions).Concat(new[] { "cancel-recovery", "dose-inventory", "dose-off" }).ToArray()
        : Definitions.IsCooker(co) ? new[] { "start", "pause", "cancel", "watch", "unwatch", "cue-volume" }
        : new[] { "start", "pause", "receive", "pause-receive", "water-routed", "water-legacy", "unlink-water", "watch", "unwatch", "cue-volume" };
    /// <summary>The racks a W2 can feed, or the W2s that can feed a rack. Since Agriculture 0.53.0 (owner request,
    /// 5 October 2026: the treatment the water line had) the irrigation pipe joins a W2 or rack it runs under or right
    /// beside, on any side, and a W2 and a rack within one tile of each other join without pipe, as the water, gas and
    /// acid lines do. It used to have to reach one inlet tile and one outlet tile exactly. Never across open floor.</summary>
    internal static IEnumerable<CondOwner> WaterCandidates(CondOwner co) =>
        co.ship.GetCOs(null, false, false, true).Where(c => c != co && c.ship == co.ship && WaterPeerKind(co, c) && NativeFluidRoute.EndpointReady(c) && Piped(co, c));
    private static bool WaterPeerKind(CondOwner co, CondOwner c) => Definitions.Machine(c) && !WorkupDefinitions.IsBench(c) && !Definitions.IsCooker(c) &&
        IrrigationDefinitions.IsSupply(c) != IrrigationDefinitions.IsSupply(co);
    /// <summary>Why a W2 or rack aboard is not offered in the water link picker, by the shared link note.</summary>
    internal static string WaterNote(CondOwner co) => co?.ship == null || WorkupDefinitions.IsBench(co) || Definitions.IsCooker(co) ? "" :
        LinkChoices.Note(co, WaterPipes, co.ship.GetCOs(null, false, false, true).Where(c => c != co && c.ship == co.ship && WaterPeerKind(co, c)), WaterCandidates(co));
    /// <summary>Whether a W2 and a rack are joined, either way round: touching, or on one irrigation network within the route limit.</summary>
    internal static bool Piped(CondOwner a, CondOwner b) => Reach(a, b) != null;
    /// <summary>How a W2 reaches a rack: through the pipe (the network's segments are filled before feed arrives), or
    /// directly when they are within one tile of each other. Tiles is the pipe length the flow is estimated over (at
    /// least one).</summary>
    private sealed class Branch
    {
        internal readonly int Tiles; internal readonly bool ThroughPipe;
        internal Branch(int tiles, bool throughPipe) { Tiles = tiles; ThroughPipe = throughPipe; }
    }
    private static Branch? Reach(CondOwner a, CondOwner b)
    {
        var kind = LineReach.Of(a, b, WaterPipes);
        if (kind == LineReachKind.None) return null;
        if (kind == LineReachKind.Adjacent) return new Branch(1, false);
        // Network steps count every pipe tile passed and, last, the rack itself.
        int tiles = Math.Max(1, LineReach.Hops(a, b, WaterPipes) - 1);
        return tiles > RouteTileLimit ? null : new Branch(tiles, true);
    }

    private static bool? WaterCommand(Session s, string action, out string message)
    {
        message = "";
        if (!action.StartsWith("link-water:", StringComparison.Ordinal) && action != "unlink-water" && action != "water-routed" && action != "water-legacy") return null;
        var co = s.Object;
        if (WorkupDefinitions.IsBench(co) || Definitions.IsCooker(co) || !Paused(s) || !NativeFluidRoute.EndpointReady(co)) { message = Text.Get("water_pause"); return false; }
        if (action == "water-routed" || action == "water-legacy")
        {
            if (IrrigationDefinitions.IsSupply(co)) { message = Text.Get("help"); return false; }
            var peer = WaterPeer(co);
            if (peer != null && Definitions.Machine(peer) && !Paused(Get(peer))) { message = Text.Get("water_pause"); return false; }
            if (s.Solution.TotalKg > NutrientSolution.Tolerance || !s.Line.Empty) { message = Text.Get("solution_switch"); return false; }
            s.Solution.Profile = NutrientSolution.None; Save(s);
            SetWaterMode(s, action == "water-routed"); message = Describe(co); return true;
        }
        if (action == "unlink-water" && IrrigationDefinitions.IsSupply(co))
        {
            var peers = WaterBank(co).Ports.Select(p => (Port:p, Link:PortPairing.Read(p))).ToArray();
            if (peers.Any(x => x.Link.State == PortLinkState.Linked && Resolve(x.Link.PeerObjectId) is CondOwner peer &&
                (!Definitions.Machine(peer) || peer.ship!=co.ship || !NativeFluidRoute.EndpointReady(peer) || Get(peer).Protected || WaterGuard(peer).Protected || LineGuard(peer).Protected || !Paused(Get(peer)) || !Get(peer).Line.Empty)))
            { message = Text.Get("water_pause"); return false; }
            foreach (var x in peers) { var peer=Resolve(x.Link.PeerObjectId); PortPairing.Unlink(x.Port,peer==null?null:WaterPort(peer)); }
            message=Describe(co); return true;
        }
        if(!s.Line.Empty) { message=Text.Get("line_drain"); return false; }
        var other = action == "unlink-water" ? WaterPeer(co) : Resolve(action.Substring("link-water:".Length));
        if (other != null && (!Definitions.Machine(other) || Definitions.IsCooker(other) || co.ship != other.ship ||
            IrrigationDefinitions.IsSupply(co) == IrrigationDefinitions.IsSupply(other) || !NativeFluidRoute.EndpointReady(other) ||
            Get(other).Protected || WaterGuard(other).Protected || !Paused(Get(other))))
        { message = Text.Get("water_pause"); return false; }
        if (action == "unlink-water")
        {
            PortPairing.Unlink(WaterPort(co), other == null ? null : WaterBank(other).ForReceiver(WaterPort(co)));
            // Deliberately retain routed mode; unlink never re-enables a bypass.
            message = Describe(co); return true;
        }
        if (other == null) { message = Text.Get("water_missing"); return false; }
        if (!Piped(co, other)) { message = Text.Get("water_no_conduit"); return false; }
        var source = IrrigationDefinitions.IsSupply(co) ? co : other;
        var rack = source == co ? other : co;
        var sourceState = Get(source); var rackState = Get(rack);
        if (rackState.Solution.TotalKg > NutrientSolution.Tolerance && rackState.Solution.Profile != sourceState.Solution.Profile ||
            sourceState.Solution.Enabled && rackState.State.CropId.Length > 0 && NutrientSolution.CropId(sourceState.Solution.Profile) != rackState.State.CropId)
        { message = Text.Get("solution_incompatible"); return false; }
        if (!rackState.Line.Empty) { message=Text.Get("line_drain"); return false; }
        if (!WaterBank(source).TryLink(WaterPort(rack), out message)) return false;
        rackState.Solution.Profile = sourceState.Solution.Profile; Save(rackState);
        SetWaterMode(rackState, true); message = Describe(co); return true;
    }
    private static IEnumerable<Session> Destinations(Session source, bool requireReceiving)
    {
        var co=source.Object;
        if(!NativeFluidRoute.EndpointReady(co)||source.Protected||WaterGuard(co).Protected) yield break;
        foreach(var port in (source.Bank ??= WaterBank(co)).Ports)
        {
            var link=PortPairing.Read(port); var peer=link.State==PortLinkState.Linked?Resolve(link.PeerObjectId):null;
            if(peer==null||!Definitions.Machine(peer)||Definitions.IsCooker(peer)||IrrigationDefinitions.IsSupply(peer)||peer.ship!=co.ship||!NativeFluidRoute.EndpointReady(peer)||!PortPairing.Matches(port,WaterPort(peer))) continue;
            var target=Get(peer);
            if(!target.Protected&&!WaterGuard(peer).Protected&&!LineGuard(peer).Protected&&target.Routed&&CompatibleSolution(source,target)&&(!requireReceiving||target.State.Receiving)) yield return target;
        }
    }
    internal const string WaterPipesId = "PhobosAgriculture.Water";
    /// <summary>The irrigation pipe, a network family since Agriculture 0.53.0: W2s and racks carry its port, so any
    /// pipe under or beside one joins it. A W2 and a rack within one tile of each other join directly (the shared touching
    /// rule), but touching machines do not chain separate pipe runs into one: two W2s side by side, each on its own run
    /// with its own feed, keep their runs apart, as they did before. The id is the saved one, so pipe contents and links
    /// in a save keep their meaning.</summary>
    internal static readonly FluidSegmentFamily WaterPipes = new(WaterPipesId, c => c.strCODef == IrrigationDefinitions.Pipe + "Installed",
        c => LinePorts.Points(WaterPipesId, c.strCODef), adjacencyJoins: false) { Label = () => Text.Get("water_pipe_label") };
    // A power step asks for the same routes from the demand check and the pump; one answer per step per pair.
    private static readonly Phobos.Ostranauts.Framework.Processing.StepMemo<(CondOwner, CondOwner), Branch?> routes = new();
    private static Branch? Route(Session source, Session target)
    {
        long step = Phobos.Ostranauts.Framework.Processing.NativeSteps.Frame;
        if (routes.TryGet(step, (source.Object, target.Object), out var known)) return known;
        var branch = RouteNow(source, target);
        routes.Set(step, (source.Object, target.Object), branch);
        return branch;
    }
    private static Branch? RouteNow(Session source, Session target)
    {
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Route);
        var branch = Reach(source.Object, target.Object);
        // Several W2s may share one pipe while they mix the same feed (Agriculture 0.53.0; until then one W2 per pipe
        // run). Two feeds in one pipe would only flush each other back, so a W2 that would pump a different feed into a
        // pipe another running W2 also pumps into waits, and says why. Feeding a touching rack uses no pipe.
        return branch != null && branch.ThroughPipe && FeedConflict(source) ? null : branch;
    }
    /// <summary>Whether another running W2 on this W2's irrigation network pumps a different feed through the pipe.</summary>
    private static bool FeedConflict(Session source)
    {
        string feed = FeedCommodity(source.Solution.Profile);
        foreach (var other in LineReach.Members(source.Object, WaterPipes))
        {
            if (other == source.Object || !Definitions.Machine(other) || !IrrigationDefinitions.IsSupply(other) || !NativeFluidRoute.EndpointReady(other)) continue;
            var o = Get(other);
            if (o.Protected || !o.State.Running || FeedCommodity(o.Solution.Profile) == feed) continue;
            if (Destinations(o, false).Any(t => Reach(other, t.Object) is { ThroughPipe: true })) return true;
        }
        return false;
    }
    private static double SupplyDemand(Session s)
    {
        if (!NativeFluidRoute.EndpointReady(s.Object) || s.Protected || WaterGuard(s.Object).Protected) return 0;
        if(s.RecoveryInput.Length>0) return s.State.Running?DrainageRecovery.PowerKW:0;
        if (s.State.Receiving && (BulkService.HasSelection(s.Object)?BulkService.Intake(s.Object,new Reservoir(s),1,false)>NutrientSolution.Tolerance:ShipsWaterSupply.Available && ProviderHeadroom(s) > NutrientSolution.Tolerance)) return IrrigationDefinitions.PumpKW;
        if (!s.State.Running) return 0;
        if (NutrientCharge.DoseAllowance(s.State,s.Solution,1) > NutrientSolution.Tolerance && DosingCharge(s) != null) return IrrigationDefinitions.PumpKW;
        if (s.Solution.BlendAllowance(s.State, 1) > NutrientSolution.Tolerance) return IrrigationDefinitions.PumpKW;
        return Destinations(s,true).Any(t => Route(s,t)!=null && (CanDeliver(s,t)||!t.Line.Empty)) ? IrrigationDefinitions.PumpKW : 0;
    }
    private static void Pump(Session s, double elapsed, double energy)
    {
        if (!NativeFluidRoute.EndpointReady(s.Object) || s.Protected || WaterGuard(s.Object).Protected) return;
        if(s.RecoveryInput.Length>0) { RecoveryTick(s,energy); return; }
        double budget = LiquidDeliveryBudget.Kilograms(elapsed, energy, IrrigationDefinitions.RateKgPerSecond, IrrigationDefinitions.EnergyKWhPerKg);
        if (budget <= 0) return;
        // Outgoing feed, mixing and provider filling share ONE measured pump budget.
        if (s.State.Running)
        {
            var targets=Destinations(s,true).Select(t => (Target:t, Branch:Route(s,t))).Where(x=>x.Branch!=null).ToArray();
            double share=targets.Length==0?0:HydraulicRoute.Share(budget,targets.Length);
            foreach(var branch in targets)
            {
                try { budget-=PumpLine(s,branch.Target,branch.Branch!.Tiles,branch.Branch.ThroughPipe,elapsed,share); }
                catch(Exception error) { Fault(branch.Target.Object,error); throw; }
            }
            if(targets.Length==0) s.Notice=Text.Get(FeedConflict(s)?"water_feed_conflict":"water_no_route");
            if (budget > NutrientSolution.Tolerance && s.Solution.Enabled)
            {
                Dose(s,budget);
                double mixed = s.Solution.Blend(s.State, budget); budget -= mixed;
                if (mixed > 0) { Save(s); s.Notice = Text.Get("solution_mixed", mixed); }
            }
        }
        if (s.State.Receiving && budget > NutrientSolution.Tolerance)
        {
            if(BulkService.HasSelection(s.Object))BulkService.Intake(s.Object,new Reservoir(s),budget,true);
            // Only tanks touching the W2 or on its water line (Agriculture 0.32.0, the owner's link rule).
            else if (ShipsWaterSupply.Available && ShipsWaterSupply.ReachableTanks(s.Object).Count == 0) s.Notice = Text.Get("shipswater_unreached");
            else ShipsWaterSupply.Refill(s.Object, new Reservoir(s), Math.Min(budget, ProviderHeadroom(s)), Plugin.ReserveLitres.Value, WaterGuard(s.Object));
        }
    }
    private static string DescribeWaterRoute(Session s)
    {
        if (WorkupDefinitions.IsBench(s.Object) || Definitions.IsCooker(s.Object)) return "";
        var link = PortPairing.Read(WaterPort(s.Object));
        return Text.Get("water_route_status", Text.Get(s.Routed ? "water_routed_mode" : "water_legacy_mode"),
            link.State == PortLinkState.Linked ? link.PeerObjectId : Text.Get(link.State == PortLinkState.Invalid ? "protected" : "water_unlinked")) + "\n" + DescribeSolution(s) + "\n" + DescribeLine(s) + (s.RecoveryInput.Length>0?"\n"+Text.Get("recovery_status",s.RecoveryInput,s.RecoveryEnergy)+"\n"+DescribeCartridge(s):"");
    }
    private static string DescribeBulkIntake(Session s)
    {
        if(!BulkService.HasSelection(s.Object))return "";
        if(!BulkService.TryTarget(s.Object,out double target))return "\n"+Text.Get("protected");
        return "\n"+Text.Get("bulk_intake",Phobos.Ostranauts.Framework.Controls.ObjectPresentation.Name(BulkService.Peer(s.Object)),target,Text.Get(BulkService.Intake(s.Object,new Reservoir(s),1,false)>1e-8?"bulk_intake_ready":"bulk_intake_wait"));
    }
    private static string DescribeSupply(Session s)
    {
        var targets = Destinations(s, false).ToArray(); var target=targets.FirstOrDefault(); var route = target == null ? null : Route(s, target);
        return Text.Get("water_supply_status", s.State.Water, IrrigationDefinitions.CapacityKg, Text.Get(s.State.Running ? "running" : "paused"),
            Text.Get(s.State.Receiving ? "receiving" : "manual"), targets.Length==0 ? Text.Get("water_unlinked") : string.Join(", ",targets.Select(t=>t.Object.strID)),
            route == null ? Text.Get(target != null && FeedConflict(s) ? "water_feed_conflict" : "water_no_route") :
                route.ThroughPipe ? Text.Get("water_route_length", route.Tiles) : Text.Get("water_route_touching"),
            s.Protected || WaterGuard(s.Object).Protected ? Text.Get("protected") : s.Notice) + "\n" +
            (StarSystem.fEpoch - s.LastPower <= 5 ? Text.Get("power_reading", s.DeliveredKW) : Text.Get("power_unknown")) + DescribeBulkIntake(s) + "\n" + DescribeDose(s) + "\n" + DescribeSolution(s) + "\n" + DescribeLine(s) + (s.RecoveryInput.Length>0?"\n"+Text.Get("recovery_status",s.RecoveryInput,s.RecoveryEnergy)+"\n"+DescribeCartridge(s):"");
    }
}
