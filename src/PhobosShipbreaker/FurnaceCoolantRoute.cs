using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
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
    private static int[]? PipePath(CondOwner furnace, string side, CondOwner endpoint, string endpointSide = "") =>
        NativeFluidRoute.Find(furnace, CoolantPoint(furnace, side), endpoint, CoolantPoint(endpoint, endpointSide),
            c => c.strCODef == FurnaceCooling.Conduit + "Installed", allowLockedEndpoints: true, allowDamagedEndpoints: true);

    private static bool CoolantRoute(CondOwner furnace, CondOwner endpoint, out int cells)
    {
        cells = 0;
        if (FurnaceRules.Underside(endpoint.strCODef) || !CoolingMounted(endpoint)) return false;
        string side = Get(furnace).CoolingMode;
        var path = PipePath(furnace, side, endpoint);
        if (path == null || path.Length > FurnaceCooling.RouteLimit) return false;
        // A shared circuit cannot multiply pumping or radiator capacity. Include even
        // damaged/idle endpoints; no unpaired machine may silently share this loop.
        foreach (var other in furnace.ship.GetCOs(null, false, false, true))
        {
            if (other == furnace || other == endpoint || !IsEquipment(other) || FurnaceRules.Underside(other.strCODef) ||
                other.ship != furnace.ship || !NativeFluidRoute.EndpointReady(other, true, true)) continue;
            foreach (string port in FurnaceRules.Machine(other.strCODef) ? new[] { "left", "right" } : new[] { "" })
                if (PipePath(furnace, side, other, port) != null) return false;
        }
        cells = path.Length; return true;
    }
    private static bool SetCoolingMode(CondOwner furnace, string mode, out string message)
    {
        message = Text.Get("Furnace.hot_maintenance");
        if (!FurnaceRules.Machine(furnace.strCODef) || UnsafeMaintenance(furnace)) return false;
        var s = Get(furnace);
        if (!CoolingMode(furnace).TryWrite(new Dictionary<string, string> { ["mode"] = mode }))
        { s.Protected = true; message = Text.Get("Furnace.protected"); return false; }
        s.CoolingMode = mode; s.State.Batch.Armed = false;
        message = Text.Get("Furnace.coolant_mode", Text.Get("Furnace.coolant_" + mode)); return true;
    }
}
