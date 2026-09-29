using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Inventory;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>One content-owned family of fluid segments (irrigation pipe, coolant conduit, gas line): its stable id
/// and the definition test for an intact installed segment.</summary>
public sealed class FluidSegmentFamily
{
    public string Id { get; }
    public Func<CondOwner, bool> Compatible { get; }
    public FluidSegmentFamily(string id, Func<CondOwner, bool> compatible)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A segment family needs an id.");
        Id = id; Compatible = compatible ?? throw new ArgumentNullException(nameof(compatible));
    }
}

/// <summary>Routes through a segment family with the topology read once per ship per family and reused for a
/// couple of real seconds (<see cref="RecheckSeconds"/>), or until a mode switch, a destruction or an object joining
/// or leaving a ship invalidates it. Endpoints are still checked fresh on every call (installed, intact, unlocked,
/// owned, aligned, on the grid), so only the pipe and floor layout is remembered, never who may pump. This is the
/// FF2 decision of the 29 September 2026 performance pass: it supersedes the earlier "no cross-frame cache" rule
/// for fluid topology only. A route that just broke is noticed within the recheck interval; nothing is created.</summary>
public static class FluidRouteCache
{
    public const double RecheckSeconds = 2;
    private sealed class Snapshot { internal FluidTopology Topology = null!; internal readonly Cadence Cadence = new(RecheckSeconds); internal int VisitLimit; }
    private static readonly Dictionary<Ship, Dictionary<string, Snapshot>> snapshots = new();
    private static readonly List<CondOwner> cellObjects = new();
    private static readonly List<int> allowedCells = new();
    internal static int Rebuilds;

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
    public static FluidTopology Topology(Ship ship, FluidSegmentFamily family, int visitLimit = GridRoute.DefaultVisitLimit)
    {
        if (ship == null) throw new ArgumentNullException(nameof(ship));
        if (family == null) throw new ArgumentNullException(nameof(family));
        if (!snapshots.TryGetValue(ship, out var families)) snapshots[ship] = families = new Dictionary<string, Snapshot>(StringComparer.Ordinal);
        if (!families.TryGetValue(family.Id, out var snapshot)) families[family.Id] = snapshot = new Snapshot();
        if (snapshot.Topology == null || snapshot.VisitLimit != visitLimit || snapshot.Cadence.Due())
        {
            snapshot.Topology = Build(ship, family, visitLimit); snapshot.VisitLimit = visitLimit; Rebuilds++;
        }
        return snapshot.Topology;
    }
    private static FluidTopology Build(Ship ship, FluidSegmentFamily family, int visitLimit)
    {
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.FluidRouteFind);
        allowedCells.Clear();
        int segments = 0;
        var objects = ship.GetCOs(null, false, false, true);
        Diagnostics.Performance.Increment(Diagnostics.Performance.FluidRouteObjects, objects.Count);
        foreach (var co in objects)
        {
            // The cheap definition test first; the endpoint checks cost several conditions and an ownership lookup.
            if (co == null || co.ship != ship || !family.Compatible(co) || !NativeFluidRoute.EndpointReady(co) || !NativeFluidRoute.Aligned(co)) continue;
            int cell = NativeFluidRoute.CellAt(ship, co.GetPos());
            if (cell < 0) continue;
            segments++;
            if (NativeFluidRoute.SoundFloor(ship, cell, cellObjects)) allowedCells.Add(cell);
        }
        return FluidTopology.Build(ship.nCols, ship.nRows, segments, allowedCells, visitLimit);
    }
    /// <summary>Forgets one ship's snapshots (an object joined or left it) or every snapshot.</summary>
    public static void Invalidate(Ship? ship) { if (ship != null) snapshots.Remove(ship); }
    public static void InvalidateAll() => snapshots.Clear();

    // Mode switches (damage, repair, installation), destruction and objects joining or leaving a ship can all change
    // the pipe layout; the layout is then read again on the next route request.
    [HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
    private static class ModeSwitchPatch { private static void Postfix() { if (snapshots.Count > 0) InvalidateAll(); } }
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
