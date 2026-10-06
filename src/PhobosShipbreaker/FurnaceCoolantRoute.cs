using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

internal static partial class FurnaceService
{
    private static ObjectStateStore CoolingMode(CondOwner co) => new(co.mapGUIPropMaps, "FurnaceCoolingMode", Text.Owner, 1);
    private static void ReadCoolingMode(Session s)
    {
        if (!FurnaceRules.Machine(s.Object.strCODef)) return;
        var status = CoolingMode(s.Object).Read(out var fields);
        if (status == SavedStateStatus.Missing) return; // Historic installations remain direct.
        if (status != SavedStateStatus.Ready || !FurnaceCooling.TryReadMode(fields, out var mode)) { s.Protected = true; return; }
        s.CoolingMode = mode;
    }
    private static bool Routed(CondOwner furnace) => Get(furnace).CoolingMode != FurnaceCooling.Direct;
    // Shipbreaker 0.80.0 (owner go, 5 October 2026: the rule the irrigation pipe took): conduit joins a furnace it runs
    // under or right beside, on any side, and a radiator anywhere along its mounting wall or the row inside it. It used
    // to join at one side fitting and one service point. A damaged or locked furnace or radiator still joins, so a hot
    // loop keeps its path; the conduit itself must be intact.
    private static int[] FurnaceJoin(CondOwner furnace) =>
        FluidTopology.OnOrBeside(furnace.ship.nCols, furnace.ship.nRows, Phobos.Ostranauts.Framework.Inventory.BeltNetwork.FootprintCells(furnace));
    private static int[] RadiatorJoin(CondOwner radiator)
    {
        var ship = radiator.ship; var p = radiator.GetPos(); var cells = new List<int>();
        foreach (var offset in FurnaceCooling.RadiatorJoinOffsets())
        {
            var r = IntakeRules.Rotate(offset.X, offset.Y, radiator.tf.eulerAngles.z);
            int cell = ship.GetTileIndexAtWorldCoords1(new Vector2((float)(p.x + r.X), (float)(p.y + r.Y)));
            if (cell >= 0 && cell < ship.nCols * ship.nRows && !cells.Contains(cell)) cells.Add(cell);
        }
        return cells.ToArray();
    }
    private static bool Square(CondOwner co) => co.Item != null && IntakeRules.SameAngle(co.tf.eulerAngles.z, 0, 90);
    internal static readonly FluidSegmentFamily CoolantConduits = new("PhobosShipbreaker.Coolant", c => c.strCODef == FurnaceCooling.Conduit + "Installed");
    /// <summary>The conduit as a holding line (Shipbreaker 0.58.0), declared with the definitions; the pump fills it.</summary>
    internal static LineHoldUpFamily? CoolantHolding;
    // The conduit segments on a furnace's circuit and whether they are all full, once per furnace per step.
    private static readonly StepMemo<CondOwner, IReadOnlyList<CondOwner>> circuits = new();
    private static IReadOnlyList<CondOwner> CoolantCircuit(Session s)
    {
        var furnace = s.Object;
        if (CoolantHolding == null || !s.Coolant.Enabled || !Routed(furnace)) return Array.Empty<CondOwner>();
        long step = NativeSteps.Frame;
        if (circuits.TryGet(step, furnace, out var circuit)) return circuit;
        var peer = SelectedCooling(furnace);
        var path = peer != null && CoolantRoute(furnace, peer, out _) ? PipePath(furnace, peer) : null;
        circuit = path == null ? Array.Empty<CondOwner>() : LineContents.Circuit(furnace.ship, CoolantHolding, path);
        circuits.Set(step, furnace, circuit);
        return circuit;
    }
    private static bool CircuitFull(Session s) => CoolantHolding == null || CoolantCircuit(s) is { Count: > 0 } c && LineContents.Full(c, CoolantHolding);
    /// <summary>The pump primes the circuit from the reservoir's surplus above its base charge: mass leaves the furnace's
    /// charge and joins the conduit segments, each with its own record and mass.</summary>
    private static void PrimeCircuit(Session s, double pumpSeconds)
    {
        if (CoolantHolding == null || !s.Coolant.Enabled || pumpSeconds <= 0 || s.Coolant.SurplusKg <= 1e-9) return;
        var circuit = CoolantCircuit(s);
        if (circuit.Count == 0) return;
        double kg = s.Coolant.PrimeKg(pumpSeconds, LineContents.Room(circuit, CoolantHolding, CoolantCharge.Commodity));
        if (kg <= 1e-9) return;
        double used = LineContents.Top(circuit, CoolantHolding, CoolantCharge.Commodity, kg);
        s.Coolant.CleanKg = Math.Max(0, s.Coolant.CleanKg - used);
    }
    /// <summary>The shortest run of conduit from a tile under or beside the furnace to one of the radiator's join tiles,
    /// or null. Read from the ship's cached conduit layout; a route is remembered for the life of that layout.</summary>
    private static int[]? PipePath(CondOwner furnace, CondOwner endpoint)
    {
        if (furnace == endpoint || furnace.ship == null || furnace.ship != endpoint.ship || !NativeFluidRoute.EndpointReady(furnace, true, true) ||
            !NativeFluidRoute.EndpointReady(endpoint, true, true) || !Square(furnace) || !Square(endpoint)) return null;
        var ship = furnace.ship;
        if (ship.nCols < 1 || ship.nRows < 1) return null;
        var topology = FluidRouteCache.Topology(ship, CoolantConduits);
        if (topology.Overflow) return null;
        var goals = RadiatorJoin(endpoint).Where(topology.Allowed).ToArray();
        if (goals.Length == 0) return null;
        int[]? best = null;
        foreach (int start in FurnaceJoin(furnace))
        {
            if (!topology.Allowed(start)) continue;
            foreach (int goal in goals)
            {
                if (topology.ComponentOf(start) != topology.ComponentOf(goal)) continue;
                var path = topology.Path(start, goal);
                if (path != null && (best == null || path.Length < best.Length)) best = path;
            }
        }
        return best;
    }
    // Advance, admission, settlement and the collector checks ask for the same route several times in one power
    // step; one answer per furnace and endpoint per step.
    private static readonly StepMemo<(CondOwner, CondOwner), int> routes = new();
    private static bool CoolantRoute(CondOwner furnace, CondOwner endpoint, out int cells)
    {
        long step = NativeSteps.Frame;
        if (!routes.TryGet(step, (furnace, endpoint), out cells)) { cells = CoolantRouteNow(furnace, endpoint); routes.Set(step, (furnace, endpoint), cells); }
        return cells > 0;
    }
    private static int CoolantRouteNow(CondOwner furnace, CondOwner endpoint)
    {
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.FurnaceRoute);
        if (FurnaceRules.Underside(endpoint.strCODef) || !CoolingMounted(endpoint)) return 0;
        var path = PipePath(furnace, endpoint);
        // Furnaces and radiators may share one run of conduit (Shipbreaker 0.80.0; until then a second furnace or
        // radiator on the run stopped every loop on it, which ruled out furnaces set side by side). Nothing is
        // multiplied by sharing: each furnace still pays for its own pump and rejects heat only into the one radiator
        // it is paired with, and pairing stays one furnace to one radiator.
        return path == null || path.Length > FurnaceCooling.RouteLimit ? 0 : path.Length;
    }
    private static bool SetCoolingMode(CondOwner furnace, string mode, out string message)
    {
        message = Text.Get("Industry.unsupported_action");
        if (!FurnaceRules.Machine(furnace.strCODef)) return false;
        // The one condition that blocks, by name (Shipbreaker 0.85.0).
        if (ChangeReason(furnace) is string blocked) { message = blocked; return false; }
        var s = Get(furnace);
        if(s.Coolant.Enabled && mode=="direct") {message=Text.Get("Furnace.coolant_drain_first");return false;}
        if (!CoolingMode(furnace).TryWrite(new Dictionary<string, string> { ["mode"] = mode }))
        { s.Protected = true; message = Text.Get("Furnace.protected"); return false; }
        s.CoolingMode = mode; s.State.Batch.Armed = false; circuits.Invalidate(); routes.Invalidate();
        message = Text.Get("Furnace.coolant_mode", Text.Get("Furnace.coolant_" + mode)); return true;
    }
}
