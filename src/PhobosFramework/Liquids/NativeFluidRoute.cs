using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Fresh cardinal route through a content-owned segment family over structural flooring.
/// Uses native named points, never native electrical flood state or dock-inclusive lookup.</summary>
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

    // Explicit world points also support old saved equipment without newly authored mapPoints.
    // Thermal consumers may retain a physical route through locked/damaged endpoints;
    // segments always require intact installation and authorization.
    public static int[]? Find(CondOwner source, UnityEngine.Vector2 outlet, CondOwner destination, UnityEngine.Vector2 inlet,
        Func<CondOwner, bool> compatibleSegment, int visitLimit = 4096, bool allowLockedEndpoints = false, bool allowDamagedEndpoints = false)
    {
        if (!EndpointReady(source, allowLockedEndpoints, allowDamagedEndpoints) || !EndpointReady(destination, allowLockedEndpoints, allowDamagedEndpoints) || source == destination || source.ship != destination.ship) return null;
        var ship = source.ship;
        if (ship.nCols < 1 || ship.nRows < 1) return null;
        // Off-grid endpoints must never round into an apparently connected cell.
        bool Aligned(CondOwner co) => Math.Abs(Math.IEEERemainder(co.Item.TF.eulerAngles.z, 90)) < .01;
        if (!Aligned(source) || !Aligned(destination)) return null;
        int CellAt(UnityEngine.Vector2 point)
        {
            int i = ship.GetTileIndexAtWorldCoords1(point);
            if (i < 0 || i >= (long)ship.nCols * ship.nRows) return -1;
            var tile = ship.GetTileByIndex(i);
            return tile != null && Math.Abs(tile.tf.position.x - point.x) < .01 && Math.Abs(tile.tf.position.y - point.y) < .01 ? i : -1;
        }
        int start = CellAt(outlet);
        int goal = CellAt(inlet);
        if (start < 0 || goal < 0) return null;
        var occupied = new HashSet<int>();
        foreach (var co in ship.GetCOs(null, false, false, true))
            if (co.ship == ship && EndpointReady(co) && compatibleSegment(co) && Aligned(co))
            {
                int cell = CellAt(co.GetPos());
                if (cell >= 0) occupied.Add(cell);
            }
        // A bounded successful search must not hide a remote second source behind an
        // unvisited tail. Consumers can therefore use this same walk for circuit exclusion.
        if (occupied.Count > visitLimit) return null;
        bool Allowed(int i)
        {
            if (!occupied.Contains(i)) return false;
            var tile = ship.GetTileByIndex(i); var props = tile?.coProps;
            if (props == null || props.ship != ship || !props.HasCond("IsFloor") || props.HasCond("IsWall") ||
                props.HasCond("IsFloorFlex") || props.HasCond("IsEVATile")) return false;
            var objects = new List<CondOwner>();
            ship.GetCOsAtWorldCoords1(tile!.tf.position, null, false, true, objects);
            return objects.Any(c => c.ship == ship && !c.bDestroyed && c.HasCond("IsFloor") && c.HasCond("IsInstalled") && !c.HasCond("IsDamaged"));
        }
        return GridRoute.Find(ship.nCols, ship.nRows, new[] { start }, new HashSet<int> { goal }, Allowed, visitLimit);
    }
}
