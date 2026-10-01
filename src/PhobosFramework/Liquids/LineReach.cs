using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Liquids;

public enum LineReachKind { None, Adjacent, Line }

/// <summary>The owner's link rule (30 September 2026): a machine reaches a store, or another machine, only through
/// touching equipment (footprints touching or one tile apart, <see cref="BulkVessels.WithinOneTile"/>) or a line
/// network, never across open floor. The station refuelling kiosk is the only exception and does not use this.
/// Touching is tested first and needs no topology, so every placement that worked before still does. A network
/// family joins participants through the segments under or beside them (any side, Framework 0.69.0) and, where it
/// allows, through touching participants, chaining across the ship; the verdict comes from the per-ship topology snapshot
/// (<see cref="FluidRouteCache"/>, reread every two real seconds or when a relevant part changes), and both ends are
/// checked fresh on every call. A participant bridging two others that became locked or damaged since the last
/// rebuild keeps bridging until the snapshot is reread (at most two seconds).</summary>
public static class LineReach
{
    /// <summary>Whether <paramref name="a"/> reaches <paramref name="b"/>: touching, on one network of
    /// <paramref name="family"/> (null: touching only), or not at all.</summary>
    public static LineReachKind Of(CondOwner? a, CondOwner? b, FluidSegmentFamily? family)
    {
        if (a == null || b == null || a == b || a.bDestroyed || b.bDestroyed || a.ship == null || a.ship != b.ship) return LineReachKind.None;
        if (BulkVessels.Adjacent(a, b)) return LineReachKind.Adjacent;
        return OnNetwork(a, b, family) ? LineReachKind.Line : LineReachKind.None;
    }
    /// <summary>Network steps between two participants (segment cells and touching participants each count one), 0
    /// when they touch, or -1 when they do not reach each other.</summary>
    public static int Hops(CondOwner? a, CondOwner? b, FluidSegmentFamily? family)
    {
        var kind = Of(a, b, family);
        if (kind == LineReachKind.Adjacent) return 0;
        if (kind == LineReachKind.None) return -1;
        var ship = a!.ship;
        return FluidRouteCache.Topology(ship, family!).Hops(FluidRouteCache.ParticipantOf(ship, family!, a), FluidRouteCache.ParticipantOf(ship, family!, b!));
    }
    /// <summary>The candidates <paramref name="co"/> reaches, with how; one topology read for the whole list.</summary>
    public static IEnumerable<(CondOwner Object, LineReachKind Kind)> Reachable(CondOwner co, IEnumerable<CondOwner> candidates, FluidSegmentFamily? family)
    {
        foreach (var candidate in candidates)
        {
            var kind = Of(co, candidate, family);
            if (kind != LineReachKind.None) yield return (candidate, kind);
        }
    }
    /// <summary>Every participant on the network that runs through <paramref name="segment"/>, a line segment of
    /// <paramref name="family"/> (Framework 0.60.0): what a damaged or removed segment was joining. Read from the cached
    /// snapshot, so a segment that is switching to its damaged form still answers for the network it belonged to.</summary>
    public static IReadOnlyList<CondOwner> MembersThrough(CondOwner? segment, FluidSegmentFamily family)
    {
        if (segment?.ship == null || family == null || !family.IsNetwork) return Array.Empty<CondOwner>();
        var ship = segment.ship;
        int cell = NativeFluidRoute.CellAt(ship, segment.GetPos());
        if (cell < 0) return Array.Empty<CondOwner>();
        var topology = FluidRouteCache.Topology(ship, family);
        var objects = FluidRouteCache.Participants(ship, family);
        return topology.ParticipantsOn(cell).Where(k => k < objects.Count).Select(k => objects[k]).ToArray();
    }
    /// <summary>Every other participant on <paramref name="co"/>'s network of <paramref name="family"/>.</summary>
    public static IEnumerable<CondOwner> Members(CondOwner? co, FluidSegmentFamily family)
    {
        if (co?.ship == null || family == null || !family.IsNetwork) return Enumerable.Empty<CondOwner>();
        var ship = co.ship;
        int k = FluidRouteCache.ParticipantOf(ship, family, co);
        if (k < 0) return Enumerable.Empty<CondOwner>();
        var topology = FluidRouteCache.Topology(ship, family);
        var objects = FluidRouteCache.Participants(ship, family);
        return Enumerable.Range(0, objects.Count).Where(i => i != k && topology.ParticipantsConnected(k, i)).Select(i => objects[i]).ToArray();
    }
    /// <summary>A named-point route (the gas line's first form): touching, or a bounded segment route from the
    /// vessel's point to the machine's. Nothing in the Phobos mods calls it since the P1, L2, A2 and store transfers
    /// joined the network rule; it stays only for binary compatibility.</summary>
    [Obsolete("Use LineReach.Of(a, b, family): every link follows the network rule.")]
    public static LineReachKind Of(CondOwner machine, string machinePoint, CondOwner vessel, string vesselPoint, FluidSegmentFamily family, int tileLimit)
    {
        if (BulkVessels.Adjacent(machine, vessel)) return LineReachKind.Adjacent;
        var path = FluidRouteCache.Find(vessel, vesselPoint, machine, machinePoint, family);
        return path != null && path.Length <= tileLimit ? LineReachKind.Line : LineReachKind.None;
    }
    /// <summary>Why <paramref name="machine"/> does not reach <paramref name="store"/> through
    /// <paramref name="family"/> (null: touching only), or <see cref="ReachProblem.None"/> when it does
    /// (Framework 0.69.0). Read-only; the pipe facts come from the cached snapshot, at most two seconds old.</summary>
    public static ReachProblem Problem(CondOwner? machine, CondOwner? store, FluidSegmentFamily? family)
    {
        if (machine == null || store == null || machine == store || machine.ship == null) return ReachProblem.NotReady;
        var facts = new ReachFacts
        {
            Reached = Of(machine, store, family) != LineReachKind.None,
            MachineReady = NativeFluidRoute.EndpointReady(machine),
            Installed = !store.bDestroyed && store.ship == machine.ship && store.objCOParent == null && store.HasCond("IsInstalled"),
            Damaged = store.HasCond("IsDamaged"),
            Locked = store.HasCond("IsLocked") || store.objContainer?.Locked == true,
            Ready = NativeFluidRoute.EndpointReady(store)
        };
        var ship = machine.ship;
        if (family != null && family.IsNetwork && ship.nCols > 0 && ship.nRows > 0 && facts.MachineReady && facts.Ready &&
            family.Ports!(machine) is { Count: > 0 } && family.Ports(store) is { Count: > 0 })
        {
            facts.Network = true;
            facts.Overflow = FluidRouteCache.Topology(ship, family).Overflow;
            (facts.StoreOpenPipe, facts.StoreClosedPipe) = FluidRouteCache.Touches(ship, family, store);
            (facts.MachineOpenPipe, facts.MachineClosedPipe) = FluidRouteCache.Touches(ship, family, machine);
        }
        return LinkDiagnosis.Classify(facts);
    }
    private static bool OnNetwork(CondOwner a, CondOwner b, FluidSegmentFamily? family)
    {
        if (family == null || !family.IsNetwork || a.ship.nCols < 1 || a.ship.nRows < 1) return false;
        if (!NativeFluidRoute.EndpointReady(a) || !NativeFluidRoute.EndpointReady(b)) return false;
        var ship = a.ship;
        int ka = FluidRouteCache.ParticipantOf(ship, family, a), kb = FluidRouteCache.ParticipantOf(ship, family, b);
        return ka >= 0 && kb >= 0 && FluidRouteCache.Topology(ship, family).ParticipantsConnected(ka, kb);
    }
}
