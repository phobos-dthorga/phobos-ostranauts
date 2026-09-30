using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Items;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Ship's Water tanks on the process-water network (Framework 0.59.0; owner decision, 30 September 2026:
/// "touching or piped"). For the inspected Ship's Water 0.16.1 only, each installed tank definition, water and waste,
/// intact and damaged, gains a process-water port by Framework's one rule (<see cref="LinePorts.Water"/>): one map
/// point, the water line's joint on the footprint tile beside it, and the joint redraw through
/// <see cref="LineJoints"/>. The definitions are amended in place, never republished, and a definition whose footprint
/// is not the inspected one is left alone. Map points and socket adds are rebuilt from definitions when a save loads,
/// so tanks already aboard gain the port with no rewrite. The tanks still never join a Phobos vessel's pool: the port
/// only lets <see cref="ShipsWaterSupply"/> tell which tanks a Phobos machine or silo reaches.</summary>
public static class ShipsWaterPorts
{
    /// <summary>Installed tank definitions of Ship's Water 0.16.1 and their footprints (tiles per side).</summary>
    public static readonly IReadOnlyList<(string Definition, int Footprint)> Tanks = new[]
    {
        ("ItmWaterTankSmall", 1), ("ItmWaterTankSmallDmg", 1), ("ItmWaterTankMedium", 2), ("ItmWaterTankMediumDmg", 2),
        ("ItmWaterTankLarge", 3), ("ItmWaterTankLargeDmg", 3), ("ItmWaterTankWasteSmall", 1), ("ItmWaterTankWasteSmallDmg", 1),
        ("ItmWaterTankWasteMedium", 2), ("ItmWaterTankWasteMediumDmg", 2), ("ItmWaterTankWaste", 3), ("ItmWaterTankWasteDmg", 3)
    };

    /// <summary>Adds the ports when the pinned Ship's Water is loaded; returns how many definitions carry one.</summary>
    public static int Apply() => ShipsWaterSupply.Available ? Amend(DataHandler.dictCOs, DataHandler.dictItemDefs) : 0;

    /// <summary>The amendment itself, on any definition tables (the game's, or a test's). Idempotent.</summary>
    public static int Amend(IDictionary<string, JsonCondOwner>? objects, IDictionary<string, JsonItemDef>? items)
    {
        if (objects == null || items == null) return 0;
        var line = SharedLines.ProcessWaterSpec();
        int amended = 0;
        foreach (var (id, footprint) in Tanks)
        {
            if (!objects.TryGetValue(id, out var co) || co.strItemDef == null || !items.TryGetValue(co.strItemDef, out var item)) continue;
            var port = LinePorts.Water(footprint);
            if (LineDefinitions.AmendPort(co, item, id, footprint, line, LinePorts.WaterPoint, port.X, port.Y, port.Socket)) amended++;
        }
        return amended;
    }
}
