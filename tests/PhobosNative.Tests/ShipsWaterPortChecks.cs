using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Ship's Water tanks on the process-water network (Framework 0.59.0) and the Agriculture rack's water port
/// (Agriculture 0.32.0). Ship's Water is not loaded here, so the amendment runs on stand-in definitions shaped like the
/// inspected 0.16.1 tanks (square footprints of plain fixture adds), never on the game's own tables.</summary>
internal static class ShipsWaterPortChecks
{
    internal static void Run(NativeDefinitions agriculture, Action<bool, string> check)
    {
        // Apply() is gated on the pinned plugin through BepInEx's chainloader, which cannot start outside the game; the
        // amendment itself is checked here on stand-in tables.
        check(ShipsWaterPorts.Amend(null, null) == 0, "Without definition tables nothing is amended");
        check(ShipsWaterPorts.Tanks.Count == 12 && ShipsWaterPorts.Tanks.All(t => t.Footprint is >= 1 and <= 3),
            "Six installed tank forms, water and waste, each intact and damaged, of one to three tiles");
        var objects = new Dictionary<string, JsonCondOwner>(StringComparer.Ordinal);
        var items = new Dictionary<string, JsonItemDef>(StringComparer.Ordinal);
        foreach (var (id, footprint) in ShipsWaterPorts.Tanks)
        {
            objects[id] = new JsonCondOwner { strName = id, strItemDef = id, mapPoints = footprint == 1 ? new[] { "use,0,-16" } : null };
            items[id] = new JsonItemDef { strName = id, nCols = footprint, fZScale = .75f, aSocketAdds = Enumerable.Repeat("TILFixtureAdds", footprint * footprint).ToArray() };
        }
        // A tank whose shape is not the inspected one, and one whose port tile already adds something else, stay untouched.
        items["ItmWaterTankWaste"].nCols = 4;
        items["ItmWaterTankMediumDmg"].aSocketAdds[LinePorts.Water(2).Socket] = "SomeoneElsesLoot";
        var line = Phobos.Ostranauts.Framework.Items.SharedLines.ProcessWaterSpec();
        int amended = ShipsWaterPorts.Amend(objects, items);
        check(amended == ShipsWaterPorts.Tanks.Count - 2, "Every inspected tank gains a port; a differently shaped or occupied one does not (" + amended + ")");
        check(objects["ItmWaterTankWaste"].mapPoints == null && items["ItmWaterTankMediumDmg"].aSocketAdds[LinePorts.Water(2).Socket] == "SomeoneElsesLoot" &&
              !(objects["ItmWaterTankMediumDmg"].mapPoints ?? Array.Empty<string>()).Any(p => p.StartsWith(LinePorts.WaterPoint + ",", StringComparison.Ordinal)),
            "A refused amendment changes nothing");
        foreach (var (id, footprint) in ShipsWaterPorts.Tanks.Where(t => t.Definition != "ItmWaterTankWaste" && t.Definition != "ItmWaterTankMediumDmg"))
        {
            var port = LinePorts.Water(footprint);
            check(objects[id].mapPoints.Contains(LinePorts.WaterPoint + "," + port.X + "," + port.Y) && (footprint != 1 || objects[id].mapPoints.Contains("use,0,-16")),
                "The water port is added by the shared rule and the tank's own points stay: " + id);
            check(items[id].aSocketAdds[port.Socket] == line.Fixture && items[id].aSocketAdds.Count(s => s == "TILFixtureAdds") == footprint * footprint - 1,
                "Only the tile beside the port draws the water line's joint: " + id);
            check(items[id].ctSpriteSheet == null && items[id].fZScale == .75f && LineJoints.Extra(id).Contains(line.Sprite),
                "The tank's own sprite and draw order stay; the joint is redrawn by Framework's postfix: " + id);
            check(LinePorts.Points(LineFamilies.ProcessWaterId, id).SequenceEqual(new[] { LinePorts.WaterPoint }), "The tank is a participant of the process-water network: " + id);
        }
        var points = objects.ToDictionary(p => p.Key, p => string.Join("|", p.Value.mapPoints ?? Array.Empty<string>()));
        check(ShipsWaterPorts.Amend(objects, items) == amended && objects.All(p => string.Join("|", p.Value.mapPoints ?? Array.Empty<string>()) == points[p.Key]),
            "Amending again (a content reload) changes nothing further");

        // The Agriculture rack's water port shares the irrigation inlet's tile; the footprint tile beside it draws both joints.
        string rack = PhobosAgriculture.Definitions.Rack;
        var rackPort = LinePorts.Water(PhobosAgriculture.IrrigationDefinitions.RackFootprint);
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            var co = agriculture.Objects[rack + form];
            check(co.mapPoints.Contains(LinePorts.WaterPoint + "," + rackPort.X + "," + rackPort.Y) && co.mapPoints.Contains(PhobosAgriculture.IrrigationDefinitions.Inlet + "," + rackPort.X + "," + rackPort.Y),
                "The rack's water port and irrigation inlet share the tile beside the middle of its -X side: " + form);
            check(LinePorts.Has(LineFamilies.ProcessWaterId, rack + form), "The rack is a participant of the process-water network: " + form);
            if (!form.StartsWith("Installed", StringComparison.Ordinal)) continue;
            string socket = agriculture.Items[rack + form].aSocketAdds[rackPort.Socket];
            var loot = agriculture.Loot.TryGetValue(socket, out var l) ? l : DataHandler.dictLoot.TryGetValue(socket, out var n) ? n : null;
            check(loot != null && loot.aCOs.Any(c => c.StartsWith(PhobosAgriculture.IrrigationDefinitions.Segment + "=", StringComparison.Ordinal)) &&
                  loot.aCOs.Any(c => c.StartsWith("PhobosProcessWaterLinePresent=", StringComparison.Ordinal)),
                "The rack's port tile draws both the irrigation and the process-water joint: " + form);
            check(agriculture.Items[rack + form].ctSpriteSheet == PhobosAgriculture.IrrigationDefinitions.Pipe + "Sprite",
                "The irrigation line keeps driving the rack's own joint refresh: " + form);
        }
    }
}
