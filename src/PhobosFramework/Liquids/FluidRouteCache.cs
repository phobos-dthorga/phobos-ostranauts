using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Inventory;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>One content-owned family of fluid segments (irrigation pipe, coolant conduit, gas line): its stable id
/// and the definition test for an intact installed segment. A network family (Framework 0.56.0) also names the
/// map points of each participant's ports for the family, and may let touching participants join as if piped.
/// Exactly one instance per id: snapshots are keyed by id.</summary>
public sealed class FluidSegmentFamily
{
    public string Id { get; }
    public Func<CondOwner, bool> Compatible { get; }
    /// <summary>The object's port map points for this family, or null or empty when it is not a participant.</summary>
    public Func<CondOwner, IReadOnlyList<string>?>? Ports { get; }
    /// <summary>Whether participants within one tile of each other join one network (the owner's touching rule).</summary>
    public bool AdjacencyJoins { get; }
    public bool IsNetwork => Ports != null;
    /// <summary>The line's name in running text ("the water line"), for link choices; null when it has none.</summary>
    public Func<string>? Label { get; set; }
    public FluidSegmentFamily(string id, Func<CondOwner, bool> compatible) : this(id, compatible, null, false) { }
    public FluidSegmentFamily(string id, Func<CondOwner, bool> compatible, Func<CondOwner, IReadOnlyList<string>?>? ports, bool adjacencyJoins)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A segment family needs an id.");
        if (adjacencyJoins && ports == null) throw new ArgumentException("Only a network family with participant ports can join touching participants.");
        Id = id; Compatible = compatible ?? throw new ArgumentNullException(nameof(compatible)); Ports = ports; AdjacencyJoins = adjacencyJoins;
    }
}

/// <summary>Routes through a segment family with the topology read once per ship and reused for a couple of real
/// seconds (<see cref="RecheckSeconds"/>), or until a relevant mode switch, a destruction or an object joining or
/// leaving a ship invalidates it. Endpoints are still checked fresh on every call (installed, intact, unlocked,
/// owned, aligned, on the grid), so only the pipe and floor layout is remembered, never who may pump. This is the
/// FF2 decision of the 29 September 2026 performance pass: it supersedes the earlier "no cross-frame cache" rule
/// for fluid topology only. A route that just broke is noticed within the recheck interval; nothing is created.
///
/// Framework 0.56.0: one object scan builds every registered family for a ship; a new snapshot's recheck starts
/// consumed (one build per change, not two); a mode switch invalidates only its own ship, and only when the object
/// is a segment, a participant, a floor or a wall before or after the switch (doors, crew faces and most equipment
/// no longer flush every ship). Network families record their participants for <see cref="LineReach"/>.
///
/// Framework 0.69.0 (owner decision, 1 October 2026): a participant joins every open segment under it or directly
/// beside it, on any side, the way a conveyor belt joins equipment. The single port tile of 0.57.0 lies inside that
/// ring, so every layout that joined before still does; the port points now only mark who takes part in a family
/// and where the joint is drawn.</summary>
public static class FluidRouteCache
{
    public const double RecheckSeconds = 2;
    private sealed class FamilySnapshot
    {
        internal FluidTopology Topology = null!; internal int VisitLimit;
        internal readonly Dictionary<CondOwner, int> Participants = new();
        internal CondOwner[] ParticipantObjects = Array.Empty<CondOwner>();
        internal IReadOnlyDictionary<int, CondOwner> Segments = new Dictionary<int, CondOwner>();
        internal HashSet<int> Closed = new();
        internal int[][] JoinCells = Array.Empty<int[]>();
    }
    private sealed class ShipSnapshot { internal readonly Dictionary<string, FamilySnapshot> Families = new(StringComparer.Ordinal); internal readonly Cadence Cadence = new(RecheckSeconds); internal bool Built; }
    private static readonly Dictionary<Ship, ShipSnapshot> snapshots = new();
    private static readonly Dictionary<string, FluidSegmentFamily> registered = new(StringComparer.Ordinal);
    private static readonly List<CondOwner> cellObjects = new();
    internal static int Rebuilds;

    /// <summary>Makes a family known so the ship scan builds it with the others. Implicit on first use; a second
    /// instance under the same id is refused (snapshots are keyed by id).</summary>
    public static void Register(FluidSegmentFamily family)
    {
        if (family == null) throw new ArgumentNullException(nameof(family));
        if (registered.TryGetValue(family.Id, out var known))
        {
            if (!ReferenceEquals(known, family)) throw new ArgumentException("A second segment family instance uses the id " + family.Id + ".");
            return;
        }
        registered[family.Id] = family;
        foreach (var ship in snapshots.Values) ship.Built = false;
    }
    public static IReadOnlyCollection<FluidSegmentFamily> Families => registered.Values;

    public static int[]? Find(CondOwner source, string outlet, CondOwner destination, string inlet, FluidSegmentFamily family, int visitLimit = GridRoute.DefaultVisitLimit)
    {
        if (!NativeFluidRoute.EndpointReady(source) || !NativeFluidRoute.EndpointReady(destination)) return null;
        return Find(source, source.GetPos(outlet), destination, destination.GetPos(inlet), family, visitLimit);
    }
    public static int[]? Find(CondOwner source, Vector2 outletPoint, CondOwner destination, Vector2 inletPoint, FluidSegmentFamily family,
        int visitLimit = GridRoute.DefaultVisitLimit, bool allowLockedEndpoints = false, bool allowDamagedEndpoints = false)
    {
        if (!NativeFluidRoute.EndpointReady(source, allowLockedEndpoints, allowDamagedEndpoints) || !NativeFluidRoute.EndpointReady(destination, allowLockedEndpoints, allowDamagedEndpoints) ||
            source == destination || source.ship != destination.ship) return null;
        var ship = source.ship;
        if (ship.nCols < 1 || ship.nRows < 1 || !NativeFluidRoute.Aligned(source) || !NativeFluidRoute.Aligned(destination)) return null;
        int start = NativeFluidRoute.CellAt(ship, outletPoint), goal = NativeFluidRoute.CellAt(ship, inletPoint);
        if (start < 0 || goal < 0) return null;
        return Topology(ship, family, visitLimit).Path(start, goal, visitLimit);
    }
    /// <summary>Whether <paramref name="other"/>'s port cell lies on the same connected run of segments as the
    /// source's outlet: the circuit-exclusion question the callers used to answer with one more search per endpoint.</summary>
    public static bool SharesCircuit(CondOwner source, Vector2 outletPoint, CondOwner other, Vector2 otherPoint, FluidSegmentFamily family,
        int visitLimit = GridRoute.DefaultVisitLimit, bool allowLockedEndpoints = false, bool allowDamagedEndpoints = false)
    {
        if (!NativeFluidRoute.EndpointReady(source, allowLockedEndpoints, allowDamagedEndpoints) || !NativeFluidRoute.EndpointReady(other, allowLockedEndpoints, allowDamagedEndpoints) ||
            source == other || source.ship != other.ship) return false;
        var ship = source.ship;
        if (ship.nCols < 1 || ship.nRows < 1 || !NativeFluidRoute.Aligned(source) || !NativeFluidRoute.Aligned(other)) return false;
        int start = NativeFluidRoute.CellAt(ship, outletPoint), cell = NativeFluidRoute.CellAt(ship, otherPoint);
        return start >= 0 && cell >= 0 && Topology(ship, family, visitLimit).Connected(start, cell);
    }
    /// <summary>The current topology snapshot for a ship and family, rebuilt when its recheck is due.</summary>
    public static FluidTopology Topology(Ship ship, FluidSegmentFamily family, int visitLimit = GridRoute.DefaultVisitLimit) => Snapshot(ship, family, visitLimit).Topology;
    /// <summary>A network family's participant index for an object in the current snapshot, or -1 (not a ready,
    /// aligned participant with a port of the family).</summary>
    public static int ParticipantOf(Ship ship, FluidSegmentFamily family, CondOwner co) =>
        co != null && Snapshot(ship, family, GridRoute.DefaultVisitLimit).Participants.TryGetValue(co, out int k) ? k : -1;
    /// <summary>The objects behind a network family's participant indices in the current snapshot.</summary>
    public static IReadOnlyList<CondOwner> Participants(Ship ship, FluidSegmentFamily family) => Snapshot(ship, family, GridRoute.DefaultVisitLimit).ParticipantObjects;
    /// <summary>The segment behind each cell that carries fluid in the current snapshot (Framework 0.63.0): intact,
    /// installed, open and over sound floor.</summary>
    public static IReadOnlyDictionary<int, CondOwner> Segments(Ship ship, FluidSegmentFamily family) => Snapshot(ship, family, GridRoute.DefaultVisitLimit).Segments;
    /// <summary>What lies on a participant's join cells in the current snapshot (Framework 0.69.0), for explaining a
    /// link that is not offered: an open segment that carries fluid, and a closed (drained) one. Both false for an
    /// object that is not a ready participant of the family.</summary>
    public static (bool Open, bool Closed) Touches(Ship ship, FluidSegmentFamily family, CondOwner co)
    {
        if (ship == null || family == null || co == null || !family.IsNetwork || ship.nCols < 1 || ship.nRows < 1) return (false, false);
        var snapshot = Snapshot(ship, family, GridRoute.DefaultVisitLimit);
        if (!snapshot.Participants.TryGetValue(co, out int k) || k >= snapshot.JoinCells.Length) return (false, false);
        bool open = false, closed = false;
        foreach (int cell in snapshot.JoinCells[k]) { open |= snapshot.Topology.Allowed(cell); closed |= snapshot.Closed.Contains(cell); }
        return (open, closed);
    }

    private static FamilySnapshot Snapshot(Ship ship, FluidSegmentFamily family, int visitLimit)
    {
        if (ship == null) throw new ArgumentNullException(nameof(ship));
        Register(family);
        if (!snapshots.TryGetValue(ship, out var s)) snapshots[ship] = s = new ShipSnapshot();
        if (!s.Built || s.Cadence.Due())
        {
            BuildAll(ship, s);
            s.Built = true; s.Cadence.Invalidate(); s.Cadence.Due(); // the recheck starts consumed: one build per change
        }
        if (!s.Families.TryGetValue(family.Id, out var f) || f.VisitLimit != visitLimit)
            s.Families[family.Id] = f = BuildOne(ship, family, visitLimit);
        return f;
    }
    private sealed class Collector
    {
        internal readonly FluidSegmentFamily Family; internal int Segments; internal readonly List<int> Allowed = new();
        internal readonly Dictionary<int, CondOwner> SegmentObjects = new();
        internal readonly HashSet<int> Closed = new();
        internal readonly List<CondOwner> Participants = new(); internal readonly List<IReadOnlyList<int>> Ports = new();
        internal Collector(FluidSegmentFamily family) { Family = family; }
    }
    private static void BuildAll(Ship ship, ShipSnapshot s)
    {
        s.Families.Clear();
        if (registered.Count == 0) return;
        var collectors = registered.Values.Select(f => new Collector(f)).ToArray();
        Scan(ship, collectors);
        foreach (var c in collectors) s.Families[c.Family.Id] = Finish(ship, c, GridRoute.DefaultVisitLimit);
    }
    private static FamilySnapshot BuildOne(Ship ship, FluidSegmentFamily family, int visitLimit)
    {
        var collector = new Collector(family);
        Scan(ship, new[] { collector });
        return Finish(ship, collector, visitLimit);
    }
    private static void Scan(Ship ship, Collector[] collectors)
    {
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.FluidRouteFind);
        Rebuilds++;
        var objects = ship.GetCOs(null, false, false, true);
        Diagnostics.Performance.Increment(Diagnostics.Performance.FluidRouteObjects, objects.Count);
        foreach (var co in objects)
        {
            if (co == null || co.ship != ship) continue;
            int ready = 0; // 0 unknown, 1 ready and aligned, -1 not
            bool Ready() { if (ready == 0) ready = NativeFluidRoute.EndpointReady(co) && NativeFluidRoute.Aligned(co) ? 1 : -1; return ready > 0; }
            // Any pipe under or beside the equipment joins it (owner decision, 1 October 2026): its own tiles and the
            // tiles north, south, east and west of them, read once and shared by every family it has a port for.
            int[]? join = null;
            int[] Join() => join ??= FluidTopology.OnOrBeside(ship.nCols, ship.nRows, NativeFluidRoute.FootprintCells(co));
            foreach (var c in collectors)
            {
                // The cheap definition tests first; the endpoint checks cost several conditions and an ownership lookup.
                if (c.Family.Compatible(co))
                {
                    if (!Ready()) continue;
                    int cell = NativeFluidRoute.CellAt(ship, co.GetPos());
                    if (cell < 0) continue;
                    c.Segments++;
                    // A closed (drained) segment carries nothing and joins nothing until its run returns to service.
                    if (LineContents.IsClosed(co)) c.Closed.Add(cell);
                    else if (NativeFluidRoute.SoundFloor(ship, cell, cellObjects)) { c.Allowed.Add(cell); c.SegmentObjects[cell] = co; }
                }
                // A port of the family makes the object a participant; where the pipe may meet it is its whole edge.
                else if (c.Family.Ports?.Invoke(co) is { Count: > 0 } && Ready())
                {
                    c.Participants.Add(co); c.Ports.Add(Join());
                }
            }
        }
    }
    private static FamilySnapshot Finish(Ship ship, Collector c, int visitLimit)
    {
        var joins = new List<(int, int)>();
        if (c.Family.AdjacencyJoins)
            for (int a = 0; a < c.Participants.Count; a++)
                for (int b = a + 1; b < c.Participants.Count; b++)
                    if (BulkVessels.Adjacent(c.Participants[a], c.Participants[b])) joins.Add((a, b));
        var snapshot = new FamilySnapshot
        {
            Topology = FluidTopology.Build(ship.nCols, ship.nRows, c.Segments, c.Allowed, visitLimit, c.Ports, joins),
            VisitLimit = visitLimit, ParticipantObjects = c.Participants.ToArray(), Segments = c.SegmentObjects,
            Closed = c.Closed, JoinCells = c.Ports.Select(p => p.ToArray()).ToArray()
        };
        for (int k = 0; k < c.Participants.Count; k++) snapshot.Participants[c.Participants[k]] = k;
        return snapshot;
    }
    /// <summary>Forgets one ship's snapshots (an object joined or left it) or every snapshot.</summary>
    public static void Invalidate(Ship? ship) { if (ship != null) snapshots.Remove(ship); }
    public static void InvalidateAll() => snapshots.Clear();
    /// <summary>Whether a mode switch of this object can change a route: a segment, a participant, a floor or a wall.</summary>
    internal static bool Relevant(CondOwner? co)
    {
        if (co == null) return false;
        foreach (var family in registered.Values)
            if (family.Compatible(co) || family.Ports?.Invoke(co) is { Count: > 0 }) return true;
        return co.HasCond("IsFloor") || co.HasCond("IsWall");
    }

    // Mode switches (damage, repair, installation), destruction and objects joining or leaving a ship can all change
    // the pipe layout; the layout is then read again on the next route request. Relevance is tested before and after
    // the switch: an intact segment that becomes damaged is a segment before, a repaired one after.
    [HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
    private static class ModeSwitchPatch
    {
        private static void Prefix(CondOwner __instance, out (Ship? Ship, bool Relevant) __state) =>
            __state = (__instance?.ship, snapshots.Count > 0 && Relevant(__instance));
        private static void Postfix(CondOwner __instance, (Ship? Ship, bool Relevant) __state)
        {
            if (snapshots.Count == 0) return;
            if (__state.Relevant || Relevant(__instance)) { Invalidate(__state.Ship); Invalidate(__instance?.ship); }
        }
    }
    [HarmonyPatch(typeof(CondOwner), nameof(CondOwner.Destroy))]
    private static class DestroyPatch { private static void Prefix(CondOwner __instance) { if (snapshots.Count > 0) Invalidate(__instance?.ship); } }
    // Ship.AddCO has two overloads; each is named by its parameter types or Harmony cannot resolve the patch and
    // aborts the whole plugin's PatchAll (PatchResolutionChecks in the native suite resolves every patch).
    [HarmonyPatch(typeof(Ship), nameof(Ship.AddCO), typeof(CondOwner), typeof(bool))]
    private static class AddPatch { private static void Postfix(Ship __instance) { if (snapshots.Count > 0) Invalidate(__instance); } }
    [HarmonyPatch(typeof(Ship), nameof(Ship.AddCO), typeof(CondOwner), typeof(bool), typeof(bool))]
    private static class AddSkipPatch { private static void Postfix(Ship __instance) { if (snapshots.Count > 0) Invalidate(__instance); } }
    [HarmonyPatch(typeof(Ship), nameof(Ship.RemoveCO))]
    private static class RemovePatch { private static void Postfix(Ship __instance) { if (snapshots.Count > 0) Invalidate(__instance); } }
    [HarmonyPatch]
    private static class ReloadPatch
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods() =>
            typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
        private static void Prefix() => InvalidateAll();
    }
}
