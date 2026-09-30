using System;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Items;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Framework 0.57.0 owns items for the first time: the gas line (moved from Manufacturing with every saved
/// identity, name and bill unchanged) and the new process-water line. Both are ordinary pipe supply, published before
/// any content mod, with their art in Framework's own package.</summary>
internal static class FrameworkItemChecks
{
    internal static void Run(NativeDefinitions d, string repo, Action<bool, string> check)
    {
        double Stat(JsonCondOwner co, string stat) => double.Parse(co.aStartingConds.Single(s => s.StartsWith(stat + "=", StringComparison.Ordinal)).Split('x').Last(), System.Globalization.CultureInfo.InvariantCulture);
        foreach (var (prefix, name) in new[] { (LineFamilies.GasPrefix, "Phobos' Fennmark Gas Line"), (LineFamilies.ProcessWaterPrefix, "Phobos' Process Water Line") })
            foreach (string form in LineDefinitions.Forms)
            {
                bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
                check(d.Objects.TryGetValue(prefix + form, out var co), "Framework defines " + prefix + form);
                if (co == null) continue;
                check(co.strNameFriendly.StartsWith(name, StringComparison.Ordinal), "Line keeps its player name: " + prefix + form + " = " + co.strNameFriendly);
                check(Stat(co, "StatMass") == SharedLines.Kg && (damaged ? Stat(co, "StatBasePrice") < ItemEconomy.SupplyPrice(prefix) : Stat(co, "StatBasePrice") == ItemEconomy.SupplyPrice(prefix)),
                    "Ordinary pipe supply at its authored price: " + prefix + form);
                check(d.Installables.ContainsKey(prefix + form + "Dismantle") && (!damaged || d.Installables[prefix + form + "Repair"].aInputs.SequenceEqual(new[] { "TIsScrapAluminum=1x1" })),
                    "Dismantles to its waste and repairs with one aluminium: " + prefix + form);
                var item = d.Items[co.strItemDef];
                check(File.Exists(Path.Combine(repo, "mods/PhobosFramework/images", item.strImg + ".png")) && File.Exists(Path.Combine(repo, "mods/PhobosFramework/images", item.strImgNorm + ".png")),
                    "Line art ships in Framework's package: " + item.strImg);
            }
        check(d.Objects.ContainsKey("PhobosPropellantLineWaste") && d.Objects.ContainsKey("PhobosProcessWaterLineWaste"), "Each line dismantles to its own retained waste");
        foreach (string line in new[] { LineFamilies.GasPrefix, LineFamilies.ProcessWaterPrefix })
            check(d.Loot.Keys.Count(k => k.StartsWith("PhobosStock_Supply_", StringComparison.Ordinal) && k.EndsWith(line + "Loose", StringComparison.Ordinal)) == 4,
                "Four merchants stock the line in supply lots: " + line);
        var tiers = ItemEconomy.Pack.factionKiosks!.tiers;
        check(tiers[LineFamilies.GasPrefix] == "Neutral" && tiers[LineFamilies.ProcessWaterPrefix] == "Neutral" && tiers[WaterTanks.BasePrefix] == "Warm",
            "Both lines sit at Neutral at the faction kiosks, and every silo size at Warm as under Shipbreaker");
    }
}
