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
        ("PhobosPropellantLine", LineLayers.Gas, "PhobosPropellantLinePresent"),
        ("PhobosVerdemorrowWaterConduit", LineLayers.Irrigation, "PhobosWaterConduitPresent"),
        ("PhobosFurnaceCoolantConduit", LineLayers.Coolant, "PhobosFurnaceCoolantSegment"),
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
