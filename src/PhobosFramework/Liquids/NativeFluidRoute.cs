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
    /// <summary>Every tile an installed object covers, as grid cells (Framework 0.69.0). The game swaps an item's width
    /// and height itself when it is rotated, so the live values are used as they are. A tile off the grid, or an
    /// object not sitting squarely on the tiles, gives no cell.</summary>
    internal static int[] FootprintCells(CondOwner co)
    {
        var ship = co?.ship; var item = co?.Item;
        if (ship == null || item == null) return Array.Empty<int>();
        int w = Math.Max(1, item.nWidthInTiles), h = Math.Max(1, item.nHeightInTiles);
        var centre = co!.GetPos(); var cells = new List<int>(w * h);
        for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
            {
                int cell = CellAt(ship, centre + new UnityEngine.Vector2(i - (w - 1) / 2f, j - (h - 1) / 2f));
                if (cell >= 0 && !cells.Contains(cell)) cells.Add(cell);
            }
        return cells.ToArray();
    }
    /// <summary>Sound support under a segment cell, by the <c>lines</c> data pack's rule for the family (Framework
    /// 0.100.0; the default rule when no family is named): the tile carries none of the rule's forbidden conditions,
    /// and something a support names stands on it, installed and intact. The shipped rule is an intact floor on a
    /// floor tile or an intact wall on a wall tile. A wall tile was refused in code before, which silently cut any
    /// line laid inside a wall, the way a ship's own conduit is. <paramref name="buffer"/> is reused between calls.</summary>
    internal static bool SoundFloor(Ship ship, int cell, List<CondOwner> buffer, string? familyId = null)
    {
        var tile = ship.GetTileByIndex(cell); var props = tile?.coProps;
        if (props == null || props.ship != ship) return false;
        var rule = LinePlacement.For(familyId);
        if (!LinePlacement.TileCarries(rule, props.HasCond)) return false;
        buffer.Clear();
        ship.GetCOsAtWorldCoords1(tile!.tf.position, null, false, true, buffer);
        standing.Clear();
        // A floor object carries IsFloorGrate, or is a floor by its definition; IsFloor is only on the tile.
        foreach (var c in buffer)
            if (c.ship == ship && !c.bDestroyed && c.HasCond("IsInstalled") && !c.HasCond("IsDamaged"))
                standing.Add(new LinePlacement.Standing(Construction.NativeFloors.IsFloorObject(c), c.HasCond));
        return LinePlacement.Supported(rule, props.HasCond, standing);
    }
    private static readonly List<CondOwner> cellObjects = new();
    private static readonly List<LinePlacement.Standing> standing = new();

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
