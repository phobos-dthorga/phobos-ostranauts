using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Framework 0.56.0 line families on the shared pattern: each family has its own draw layer and forbids only
/// its own presence, so different families share a tile; the PDA paint filter counts our lines as Conduits and not
/// as Equipment, judged by the game's own trigger evaluation.</summary>
internal static class LineNativeChecks
{
    private static readonly (string Prefix, float Layer, string Presence)[] Families =
    {
        ("PhobosProcessWaterLine", LineLayers.ProcessWater, "PhobosProcessWaterLinePresent"),
        ("PhobosPropellantLine", LineLayers.Gas, "PhobosPropellantLinePresent"),
        ("PhobosVerdemorrowWaterConduit", LineLayers.Irrigation, "PhobosWaterConduitPresent"),
        ("PhobosFurnaceCoolantConduit", LineLayers.Coolant, "PhobosFurnaceCoolantSegment"),
        (PhobosManufacturing.Core.AcidLineRules.Prefix, LineLayers.Acid, PhobosManufacturing.Core.AcidLineRules.Present),
        (Phobos.Ostranauts.Framework.Inventory.BeltNetwork.Prefix, LineLayers.Belt, Phobos.Ostranauts.Framework.Inventory.BeltNetwork.Present),
    };
    internal static void Run(IEnumerable<NativeDefinitions> definitions, Action<bool, string> check)
    {
        var sets = definitions.ToArray();
        JsonItemDef Item(string id) => sets.Select(d => d.Items.TryGetValue(id, out var item) ? item : null).First(i => i != null)!;
        Loot LootOf(string id) => sets.Select(d => d.Loot.TryGetValue(id, out var loot) ? loot : null).First(l => l != null)!;
        check(LineLayers.All.Distinct().Count() == LineLayers.All.Count && LineLayers.All.All(z => z > 1.02f && z < 1.1f),
            "Every line family has its own draw layer, between the game's power conduit (1.02) and cargo pods (1.1)");
        foreach (var (prefix, layer, presence) in Families)
            foreach (string form in LineDefinitions.Forms)
            {
                var item = Item(prefix + form);
                check(item.fZScale == layer, "Line drawn in its own layer: " + prefix + form);
                if (!form.StartsWith("Installed", StringComparison.Ordinal)) continue;
                string forbid = item.aSocketForbids[4];
                check(LootOf(forbid).aCOs.All(c => c.StartsWith(presence + "=", StringComparison.Ordinal)) && item.aSocketForbids.Where((_, i) => i != 4).All(f => f == "Blank"),
                    "A segment forbids only its own family's presence: " + prefix + form);
                check(item.aSocketReqs[4] == "TILFloor" && !LootOf(item.aSocketAdds[0]).aCOs.Any(c => c.StartsWith("IsObstruction", StringComparison.Ordinal) || c.StartsWith("IsPowerConduit", StringComparison.Ordinal))
                    && (LootOf(item.aSocketAdds[0]).aLoots ?? Array.Empty<string>()).All(l => !l.StartsWith("TILFixtureAdds", StringComparison.Ordinal)),
                    "A segment sits on floor and adds no obstruction or power conduit, so other families share its tile: " + prefix + form);
            }
        check(Families.All(f => LineJobFilter.MachineConditions.Contains(f.Prefix + "Machine")), "Every line family is known to the paint filter");

        // Framework 0.57.0 ports: every registered port is a map point on its definition, its installed forms draw the
        // family's joint beside it, and no port point ever becomes an electrical input.
        JsonCondOwner? Object(string id) => sets.Select(d => d.Objects.TryGetValue(id, out var co) ? co : null).FirstOrDefault(c => c != null);
        var water = Phobos.Ostranauts.Framework.Liquids.LineFamilies.ProcessWater; var gas = Phobos.Ostranauts.Framework.Liquids.LineFamilies.Gas;
        check(water.IsNetwork && water.AdjacencyJoins && gas.IsNetwork && gas.AdjacencyJoins, "Both shared lines are networks that touching equipment joins");
        check(Phobos.Ostranauts.Framework.Liquids.LineFamilies.For("water") == water && Phobos.Ostranauts.Framework.Liquids.LineFamilies.For("hydrogen") == gas &&
              Phobos.Ostranauts.Framework.Liquids.LineFamilies.For("methane") == gas && Phobos.Ostranauts.Framework.Liquids.LineFamilies.For("crop nutrients") == null,
            "Water rides the water line, the gases the gas line, and the hoppers' nutrients link by touching only");
        foreach (var (family, presence) in new[] { (Phobos.Ostranauts.Framework.Liquids.LineFamilies.ProcessWaterId, "PhobosProcessWaterLinePresent"),
                     (Phobos.Ostranauts.Framework.Liquids.LineFamilies.GasId, "PhobosPropellantLinePresent"),
                     (PhobosManufacturing.Core.AcidLineRules.FamilyId, PhobosManufacturing.Core.AcidLineRules.Present) })
        {
            var ported = Phobos.Ostranauts.Framework.Liquids.LinePorts.Definitions(family).ToArray();
            check(ported.Length >= 8, "Equipment carries ports of " + family + " (" + ported.Length + " forms)");
            foreach (string id in ported)
            {
                var co = Object(id);
                if (co == null) continue;
                foreach (string point in Phobos.Ostranauts.Framework.Liquids.LinePorts.Points(family, id))
                    check(co.mapPoints.Any(p => p.StartsWith(point + ",", StringComparison.Ordinal)), "Port is a map point: " + id + " / " + point);
                if (!id.Contains("Installed")) continue;
                var item = Item(id);
                check(item.aSocketAdds.Any(s => FindLoot(s)?.aCOs?.Any(c => c.StartsWith(presence + "=", StringComparison.Ordinal)) == true), "Installed equipment draws the joint beside its port: " + id);
                // Framework 0.69.0: a pipe under or beside the equipment joins it, read from the square footprint the
                // touching rule also uses. Only the joint tiles carry the family's presence, so only they refuse that
                // family's pipe under installed equipment; every other tile under it takes pipe.
                check(item.nCols > 0 && item.aSocketAdds.Length == item.nCols * item.nCols && co.inventoryWidth == item.nCols,
                    "Ported equipment has a square footprint that matches its touching size: " + id);
                check(item.aSocketAdds.Count(s => FindLoot(s)?.aCOs?.Any(c => c.StartsWith(presence + "=", StringComparison.Ordinal)) == true) ==
                      Phobos.Ostranauts.Framework.Liquids.LinePorts.Points(family, id).Count, "Only the joint tile refuses its own family's pipe: " + id);
            }
        }
        Loot? FindLoot(string id) => sets.Select(d => d.Loot.TryGetValue(id, out var loot) ? loot : null).FirstOrDefault(l => l != null) ??
            (DataHandler.dictLoot.TryGetValue(id, out var native) ? native : null);
        foreach (var power in sets.SelectMany(d => d.Power))
            check((power.Value.aInputPts ?? Array.Empty<string>()).All(p => p.StartsWith("Power", StringComparison.Ordinal)), "Only Power points are electrical inputs: " + power.Key);
        foreach (var store in PhobosManufacturing.Core.GasStores.All)
            check(store.Outlet == Phobos.Ostranauts.Framework.Liquids.LinePorts.Gas(store.Footprint), "A gas store's outlet is the shared gas port: " + store.Prefix);

        // The game's composite paint filter, as the PDA builds it, adjusted by our postfix.
        var added = LineJobFilter.Triggers();
        foreach (var t in added) DataHandler.dictCTs[t.strName] = t;
        try
        {
            DataCO Data(string id) => new(sets.Select(d => d.Objects.TryGetValue(id, out var co) ? co : null).FirstOrDefault(c => c != null) ?? DataHandler.dictCOs[id]);
            var pipe = Data("PhobosPropellantLineInstalled"); var coolant = Data("PhobosFurnaceCoolantConduitInstalled");
            var machine = Data(PhobosManufacturing.Core.RefineryRules.Prefix + "Installed");
            check(DataHandler.dictCTs[LineJobFilter.InstalledLine].TriggeredDataCO(pipe, false) && DataHandler.dictCTs[LineJobFilter.InstalledLine].TriggeredDataCO(coolant, false) &&
                  !DataHandler.dictCTs[LineJobFilter.InstalledLine].TriggeredDataCO(machine, false), "Our installed-line trigger picks lines and no machine");
            CondTrigger Filter(params string[] triggers) => new() { strName = "GUIJobFilter", fChance = 1, fCount = 1, bAND = false, aReqs = Array.Empty<string>(), aForbids = Array.Empty<string>(), aTriggers = triggers };
            var equipment = Filter("TIsInstalledEquipment"); LineJobFilter.Apply(equipment, conduits: false);
            check(!equipment.TriggeredDataCO(pipe, false) && equipment.TriggeredDataCO(machine, false), "Equipment painting takes the machine and spares the pipe under it");
            var conduits = Filter("TIsConduit00Installed"); LineJobFilter.Apply(conduits, conduits: true);
            check(conduits.TriggeredDataCO(pipe, false) && !conduits.TriggeredDataCO(machine, false), "Conduit painting takes our lines as well as power conduit, and no machine");
        }
        finally { foreach (var t in added) DataHandler.dictCTs.Remove(t.strName); }

        // Framework 0.100.0 (owner report, 5 October 2026): the lines data pack decides where a segment counts, and its
        // shipped rule lets one count inside a wall. Judged on the game's own walls, the ones the owner's line ran through:
        // each makes its tile IsWall and carries IsWall itself, so the rule's wall support matches it, and a wall is not
        // one of the game's floors, so only that support does.
        var lineRule = Phobos.Ostranauts.Framework.Liquids.LinePlacement.For(null);
        foreach (string wallId in new[] { "ItmWall1x1", "ItmWallPlastic1x1" })
        {
            var wallCo = new DataCO(DataHandler.dictCOs[wallId]);
            var wallTile = Phobos.Ostranauts.Framework.Construction.NativePlaceholders.TileConditions(wallId) ?? Array.Empty<string>();
            var standing = new[] { new Phobos.Ostranauts.Framework.Liquids.LinePlacement.Standing(Phobos.Ostranauts.Framework.Construction.NativeFloors.IsFloorDefinition(wallId), wallCo.HasCond) };
            check(wallTile.Contains("IsWall") && wallCo.HasCond("IsWall") && !Phobos.Ostranauts.Framework.Construction.NativeFloors.IsFloorDefinition(wallId) &&
                  Phobos.Ostranauts.Framework.Liquids.LinePlacement.Supported(lineRule, c => wallTile.Contains(c), standing),
                "A line segment laid in this wall counts, by the shipped lines rule: " + wallId);
        }
        foreach (string named in lineRule.forbiddenTiles.Concat(lineRule.supports.Select(x => x.tile)).Concat(lineRule.supports.Select(x => x.@object)).Where(x => x != Phobos.Ostranauts.Framework.Data.LineSchema.Floor))
            check(DataHandler.dictConds.ContainsKey(named), "The shipped lines rule names a condition the game has: " + named);
        // Framework 0.73.0: a segment counts only over an installed floor object, and the lines, the cache and G4
        // reclamation all recognise that object through NativeFloors. Judged on the game's own data: until 0.72.0 the
        // object test asked for IsFloor, which only the tile carries, and no segment ever counted.
        var floorsFound = Phobos.Ostranauts.Framework.Construction.NativeFloors.IsFloorDefinition;
        var installed = DataHandler.dictCOs.Values.Where(x => (x.aStartingConds ?? Array.Empty<string>()).Any(c => c.StartsWith("IsInstalled=", StringComparison.Ordinal))).ToArray();
        var makesFloor = installed.Where(x => Phobos.Ostranauts.Framework.Construction.NativePlaceholders.TileConditions(x.strName)?.Contains("IsFloor") == true).ToArray();
        check(makesFloor.Length > 20, "The game ships objects that make their tiles floor (" + makesFloor.Length + ")");
        check(makesFloor.All(x => floorsFound(x.strName)), "Every native object that makes floor is a floor object for the lines");
        check(makesFloor.All(x => !new DataCO(x).HasCond(Phobos.Ostranauts.Framework.Construction.NativeFloors.TileMark)),
            "Native floor objects do not carry the tile's IsFloor mark, so an object test must not ask for it");
        foreach (string id in new[] { "ItmFloorGrate01", "ItmFloorMSSLFWhite01", "ItmFloorCAYL01", "ItmFloorGrate4x401", "ItmFloorRock02" })
            check(!DataHandler.dictCOs.ContainsKey(id) || floorsFound(id), "A native floor the lines sit on: " + id);
        foreach (string id in new[] { "ItmFloorLabelArrow01", PhobosShipbreaker.Core.ProcessRules.Wall, "PhobosPropellantLineInstalled" })
            check(!floorsFound(id), "Not a floor: " + id);
        check(DataHandler.dictLoot["TILFloor"].aCOs.Any(c => c.StartsWith("IsFloor=", StringComparison.Ordinal)), "The tile gets IsFloor from the game's floor socket loot");

        // Framework 0.73.0: a laid segment is a fixture like the game's installed power conduit, hidden from the ground
        // inventory and never pocketable; loose sections stay pocketable. Saved segments are corrected as they load.
        var conduit = DataHandler.dictCOs["ItmConduit00"].aStartingConds.Select(c => c.Split('=')[0]).ToArray();
        check(conduit.Contains("IsHiddenInv") && !conduit.Contains("IsPocketable"), "The game's installed power conduit is hidden from inventories and not pocketable");
        foreach (var (prefix, _, _) in Families)
            foreach (string form in LineDefinitions.Forms)
            {
                var co = Object(prefix + form)!;
                var flags = co.aStartingConds.Select(c => c.Split('=')[0]).ToArray();
                bool laid = form.StartsWith("Installed", StringComparison.Ordinal);
                check(laid ? flags.Contains("IsHiddenInv") && !flags.Contains("IsPocketable") && ItemHandling.IsFixture(prefix + form)
                           : flags.Contains("IsPocketable") && !flags.Contains("IsHiddenInv"), "Segment handling matches the game's conduit: " + prefix + form);
            }
        var savedSegment = new JsonCondOwnerSave { strID = "segment", strCODef = "PhobosPropellantLineInstalled",
            aConds = new[] { "IsInstalled=1.0x1", "StatMass=1.0x1", "IsPocketable=1.0x1", "StatDamage=1.0x0.003" } };
        var upgraded = ItemHandling.Upgrade(savedSegment);
        check(upgraded != savedSegment && EquipmentSaveUpgrade.Amount(upgraded.aConds, "IsPocketable") == 0 && EquipmentSaveUpgrade.Amount(upgraded.aConds, "IsHiddenInv") == 1 &&
              upgraded.aConds.Contains("StatDamage=1.0x0.003") && upgraded.aConds.Contains("StatMass=1.0x1") && upgraded.strID == "segment",
            "A saved pocketable segment loads hidden, with its identity, wear and mass unchanged");
        check(savedSegment.aConds.Contains("IsPocketable=1.0x1") && ReferenceEquals(upgraded, ItemHandling.Upgrade(upgraded)), "The save record is untouched and the correction is idempotent");
        var compressed = new JsonCondOwnerSave { strCODef = "PhobosPropellantLineInstalled", aConds = new[] { "DEFAULT", "StatDamage=1.0x0.1" } };
        check(ReferenceEquals(compressed, ItemHandling.Upgrade(compressed)), "A compressed saved segment takes the new flags from its definition, unchanged");
        var looseSegment = new JsonCondOwnerSave { strCODef = "PhobosPropellantLineLoose", aConds = new[] { "IsPocketable=1.0x1" } };
        check(ReferenceEquals(looseSegment, ItemHandling.Upgrade(looseSegment)), "A loose section keeps its pocketable flag");
    }
}
