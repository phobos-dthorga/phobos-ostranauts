using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Inventory;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Fresh cardinal route through a content-owned segment family over structural flooring.
/// Uses native named points, never native electrical flood state or dock-inclusive lookup. This is the uncached
/// search; <see cref="FluidRouteCache"/> answers the same question from a topology snapshot for callers that ask
/// every power step.</summary>
public static class NativeFluidRoute
{
    public static bool EndpointReady(CondOwner? co) => EndpointReady(co, false, false);
    public static bool EndpointReady(CondOwner? co, bool allowLocked, bool allowDamaged) => co != null && !co.bDestroyed && co.ship != null &&
        (int)co.ship.LoadState >= 2 && co.objCOParent == null && co.Item != null && co.HasCond("IsInstalled") &&
        (allowDamaged || !co.HasCond("IsDamaged")) && (allowLocked || !co.HasCond("IsLocked") && co.objContainer?.Locked != true) &&
        CrewSim.coPlayer != null && CrewSim.system?.GetShipOwner(co.ship.strRegID) == CrewSim.coPlayer.strID;

    public static int[]? Find(CondOwner source, string outlet, CondOwner destination, string inlet,
        Func<CondOwner, bool> compatibleSegment, int visitLimit = 4096)
    {
        if (!EndpointReady(source) || !EndpointReady(destination)) return null;
        return Find(source, source.GetPos(outlet), destination, destination.GetPos(inlet), compatibleSegment, visitLimit);
    }

    // Off-grid endpoints must never round into an apparently connected cell.
    internal static bool Aligned(CondOwner co) => Math.Abs(Math.IEEERemainder(co.Item.TF.eulerAngles.z, 90)) < .01;
    internal static int CellAt(Ship ship, UnityEngine.Vector2 point)
    {
        int i = ship.GetTileIndexAtWorldCoords1(point);
        if (i < 0 || i >= (long)ship.nCols * ship.nRows) return -1;
        var tile = ship.GetTileByIndex(i);
        return tile != null && Math.Abs(tile.tf.position.x - point.x) < .01 && Math.Abs(tile.tf.position.y - point.y) < .01 ? i : -1;
    }
    /// <summary>Sound structural floor under a segment cell: a floor tile that is not a wall, flex floor or EVA tile,
    /// with an intact installed floor object on it. <paramref name="buffer"/> is reused between calls.</summary>
    internal static bool SoundFloor(Ship ship, int cell, List<CondOwner> buffer)
    {
        var tile = ship.GetTileByIndex(cell); var props = tile?.coProps;
        if (props == null || props.ship != ship || !props.HasCond("IsFloor") || props.HasCond("IsWall") ||
            props.HasCond("IsFloorFlex") || props.HasCond("IsEVATile")) return false;
        buffer.Clear();
        ship.GetCOsAtWorldCoords1(tile!.tf.position, null, false, true, buffer);
        foreach (var c in buffer)
            if (c.ship == ship && !c.bDestroyed && c.HasCond("IsFloor") && c.HasCond("IsInstalled") && !c.HasCond("IsDamaged")) return true;
        return false;
    }
    private static readonly List<CondOwner> cellObjects = new();

    // Explicit world points also support old saved equipment without newly authored mapPoints.
    // Thermal consumers may retain a physical route through locked/damaged endpoints;
    // segments always require intact installation and authorization.
    public static int[]? Find(CondOwner source, UnityEngine.Vector2 outlet, CondOwner destination, UnityEngine.Vector2 inlet,
        Func<CondOwner, bool> compatibleSegment, int visitLimit = 4096, bool allowLockedEndpoints = false, bool allowDamagedEndpoints = false)
    {
        if (!EndpointReady(source, allowLockedEndpoints, allowDamagedEndpoints) || !EndpointReady(destination, allowLockedEndpoints, allowDamagedEndpoints) || source == destination || source.ship != destination.ship) return null;
        var ship = source.ship;
        if (ship.nCols < 1 || ship.nRows < 1) return null;
        if (!Aligned(source) || !Aligned(destination)) return null;
        int start = CellAt(ship, outlet);
        int goal = CellAt(ship, inlet);
        if (start < 0 || goal < 0) return null;
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.FluidRouteFind);
        var occupied = new HashSet<int>();
        var objects = ship.GetCOs(null, false, false, true);
        Diagnostics.Performance.Increment(Diagnostics.Performance.FluidRouteObjects, objects.Count);
        foreach (var co in objects)
            // The cheap definition test first; the endpoint checks cost several conditions and an ownership lookup.
            if (co.ship == ship && compatibleSegment(co) && EndpointReady(co) && Aligned(co))
            {
                int cell = CellAt(ship, co.GetPos());
                if (cell >= 0) occupied.Add(cell);
            }
        // A bounded successful search must not hide a remote second source behind an
        // unvisited tail. Consumers can therefore use this same walk for circuit exclusion.
        if (occupied.Count > visitLimit) return null;
        bool Allowed(int i) => occupied.Contains(i) && SoundFloor(ship, i, cellObjects);
        return GridRoute.Find(ship.nCols, ship.nRows, new[] { start }, new HashSet<int> { goal }, Allowed, visitLimit);
    }
}
