using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>The content mod owns the physical floor-routing model; Framework only finds a path.</summary>
internal sealed class CollectorRoute
{
    private readonly Ship ship;
    private readonly Vector2 portPosition, sourcePosition;
    private readonly double portAngle, sourceAngle;
    private readonly int columns, rows;
    private readonly int[] cells;
    internal int Length => cells.Length;
    private CollectorRoute(CondOwner port, CondOwner source, int[] cells)
    {
        ship = port.ship; portPosition = port.GetPos(); sourcePosition = source.GetPos();
        portAngle = Angle(port); sourceAngle = Angle(source);
        columns = ship.nCols; rows = ship.nRows; this.cells = cells;
    }
    private static double Angle(CondOwner co) => co.Item.TF.eulerAngles.z;
    private static Vector2 Point(CondOwner co, double x, double y)
    {
        var offset = IntakeRules.Rotate(x, y, Angle(co));
        return co.GetPos() + new Vector2((float)offset.X, (float)offset.Y);
    }
    private static bool Floor(Ship ship, int index)
    {
        var tile = ship.GetTileByIndex(index);
        var co = tile?.coProps;
        return co != null && co.ship == ship && co.HasCond("IsFloor") && !co.HasCond("IsWall") &&
            !co.HasCond("IsFloorFlex") && !co.HasCond("IsEVATile");
    }
    internal static string? MountProblem(CondOwner port)
    {
        if (ProcessingService.IsReclaimer(port))
            return port.Item != null && IntakeRules.SameAngle(Angle(port), 0, 90) ? null : Text.Get("Routing.grid_alignment");
        if (port.Item == null || !IntakeRules.SameAngle(Angle(port), 0, 90)) return Text.Get("CollectorRoute.collector_must_align_with_the_hull_grid");
        var ship = port.ship;
        for (int col = 0; col < CollectorRules.Width; col++)
        {
            var wallPos = Point(port, col - 0.5, 0);
            var wallTile = ship.GetTileAtWorldCoords1(wallPos.x, wallPos.y, false);
            if (wallTile?.coProps == null || !wallTile.coProps.HasCond("IsWall")) return Text.Get("CollectorRoute.restore_the_two_supporting_hull_walls");
            var objects = new List<CondOwner>();
            ship.GetCOsAtWorldCoords1(wallPos, null, false, true, objects);
            if (!objects.Any(w => !w.bDestroyed && w.HasCond("IsInstalled") && w.HasCond("IsWall") && !w.HasCond("IsDamaged")))
                return Text.Get("CollectorRoute.repair_the_two_supporting_hull_walls");
            var outside = Point(port, col - 0.5, 1);
            // Include docked neighbours: a vacated room or docking collar is not a clear exterior mouth.
            foreach (var neighbour in ship.GetAllDockedShips().Concat(new[] { ship }).Distinct())
            {
                var tile = neighbour.GetTileAtWorldCoords1(outside.x, outside.y, false);
                if (tile?.IsShipTileOrSub == true) return Text.Get("CollectorRoute.collector_pocket_must_face_clear_exterior_space");
                objects.Clear(); neighbour.GetCOsAtWorldCoords1(outside, null, false, true, objects);
                if (objects.Any(o => o != port && !o.bDestroyed && o.Item != null && o.objCOParent == null && o.HasCond("IsSolid")))
                    return Text.Get("CollectorRoute.collector_exterior_mouth_is_obstructed");
            }
            var inside = Point(port, col - 0.5, -1);
            if (!Floor(ship, ship.GetTileIndexAtWorldCoords1(inside))) return Text.Get("CollectorRoute.both_inboard_collector_tiles_need_structural_flooring");
        }
        return null;
    }
    internal static CollectorRoute? Find(CondOwner port, CondOwner source)
    {
        var ship = port.ship;
        if (source.ship != ship || source.Item == null || CollectorRules.IsFamily(source.strCODef) && MountProblem(source) != null) return null;
        var starts = Cells(source);
        var goals = new HashSet<int>(Cells(port));
        int[]? path = GridRoute.Find(ship.nCols, ship.nRows, starts, goals, i => Floor(ship, i));
        return path == null ? null : new CollectorRoute(port, source, path);
    }
    private static int[] Cells(CondOwner co)
    {
        var cells = new List<int>(); var ship = co.ship;
        if (CollectorRules.IsFamily(co.strCODef))
            for (int x = 0; x < CollectorRules.Width; x++) cells.Add(ship.GetTileIndexAtWorldCoords1(Point(co, x - 0.5, -1)));
        else
            for (int y = 0; y < ProcessRules.Footprint; y++)
            for (int x = 0; x < ProcessRules.Footprint; x++) cells.Add(ship.GetTileIndexAtWorldCoords1(Point(co, x - 1.5, y - 1.5)));
        return cells.ToArray();
    }
    internal bool Valid(CondOwner port, CondOwner source) => port.ship == ship && source.ship == ship &&
        (!CollectorRules.IsFamily(source.strCODef) || MountProblem(source) == null) && columns == ship.nCols && rows == ship.nRows && portPosition == port.GetPos() && sourcePosition == source.GetPos() &&
        IntakeRules.SameAngle(portAngle, Angle(port)) && IntakeRules.SameAngle(sourceAngle, Angle(source)) && cells.All(i => Floor(ship, i));
}
