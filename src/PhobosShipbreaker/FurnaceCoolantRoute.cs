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
    private static bool Routed(CondOwner furnace) => Get(furnace).CoolingMode != "direct";
    // Use authored local points for both new and old saved objects; no map rewrite.
    internal static Vector2 CoolantPoint(CondOwner co, string mode)
    {
        var p = co.GetPos();
        var offset = FurnaceCooling.CoolantOffset(FurnaceRules.Machine(co.strCODef), mode);
        var rotated = IntakeRules.Rotate(offset.X, offset.Y, co.tf.eulerAngles.z);
        return new Vector2((float)(p.x + rotated.X), (float)(p.y + rotated.Y));
    }
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
        var path = peer != null && CoolantRoute(furnace, peer, out _) ? PipePath(furnace, s.CoolingMode, peer) : null;
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
    private static int[]? PipePath(CondOwner furnace, string side, CondOwner endpoint, string endpointSide = "") =>
        FluidRouteCache.Find(furnace, CoolantPoint(furnace, side), endpoint, CoolantPoint(endpoint, endpointSide), CoolantConduits,
            allowLockedEndpoints: true, allowDamagedEndpoints: true);
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
        string side = Get(furnace).CoolingMode;
        var path = PipePath(furnace, side, endpoint);
        if (path == null || path.Length > FurnaceCooling.RouteLimit) return 0;
        // A shared circuit cannot multiply pumping or radiator capacity. Include even
        // damaged/idle endpoints; no unpaired machine may silently share this loop.
        var outlet = CoolantPoint(furnace, side);
        foreach (var other in furnace.ship.GetCOs(null, false, false, true))
        {
            if (other == furnace || other == endpoint || !IsEquipmentDefinition(other.strCODef) || FurnaceRules.Underside(other.strCODef) ||
                other.ship != furnace.ship || !NativeFluidRoute.EndpointReady(other, true, true)) continue;
            if (FurnaceRules.Machine(other.strCODef))
            {
                if (FluidRouteCache.SharesCircuit(furnace, outlet, other, CoolantPoint(other, "left"), CoolantConduits, allowLockedEndpoints: true, allowDamagedEndpoints: true) ||
                    FluidRouteCache.SharesCircuit(furnace, outlet, other, CoolantPoint(other, "right"), CoolantConduits, allowLockedEndpoints: true, allowDamagedEndpoints: true)) return 0;
            }
            else if (FluidRouteCache.SharesCircuit(furnace, outlet, other, CoolantPoint(other, ""), CoolantConduits, allowLockedEndpoints: true, allowDamagedEndpoints: true)) return 0;
        }
        return path.Length;
    }
    private static bool SetCoolingMode(CondOwner furnace, string mode, out string message)
    {
        message = Text.Get("Furnace.hot_maintenance");
        if (!FurnaceRules.Machine(furnace.strCODef) || UnsafeMaintenance(furnace)) return false;
        var s = Get(furnace);
        if(s.Coolant.Enabled && mode=="direct") {message=Text.Get("Furnace.charge_service");return false;}
        if (!CoolingMode(furnace).TryWrite(new Dictionary<string, string> { ["mode"] = mode }))
        { s.Protected = true; message = Text.Get("Furnace.protected"); return false; }
        s.CoolingMode = mode; s.State.Batch.Armed = false;
        message = Text.Get("Furnace.coolant_mode", Text.Get("Furnace.coolant_" + mode)); return true;
    }
}
