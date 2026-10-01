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
    }
}
