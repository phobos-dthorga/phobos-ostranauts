using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Phobos.Ostranauts.Framework.Registration;

// Data-only documentation export: no Unity scene, installation, save or inventory access.
internal static class ItemReferenceExport
{
    internal static void Write(string repo, string game, string output)
    {
        foreach (string file in Directory.GetFiles(Path.Combine(repo, "mods/PhobosAutoNav/data/cooverlays"), "*.json"))
            foreach (var overlay in JsonConvert.DeserializeObject<JsonCOOverlay[]>(File.ReadAllText(file))!)
                DataHandler.dictCOOverlays[overlay.strName] = overlay;
        var packs = new Dictionary<string, NativeDefinitions> {
            ["PhobosShipbreaker"] = PhobosShipbreaker.Content.Prepare(),
            ["PhobosAgriculture"] = PhobosAgriculture.Definitions.Prepare(),
            ["PhobosAutoNav"] = PhobosAutoNav.EquipmentContent.Prepare()
        };
        // Publish only to this audit process's in-memory dictionaries for native valuation.
        foreach (var pack in packs.Values) pack.Publish();
        var shared = new NativeDefinitions();
        shared.Objects[MaintenanceDefinitions.SpentParts] = DataHandler.dictCOs[MaintenanceDefinitions.SpentParts];
        packs["PhobosFramework"] = shared;
        packs["PhobosManufacturing"] = new NativeDefinitions();
        var installedSources = Directory.GetDirectories(Path.Combine(repo, "mods"))
            .Where(path => File.Exists(Path.Combine(path, "mod_info.json"))).Select(Path.GetFileName).ToHashSet();
        if (!installedSources.SetEquals(packs.Keys))
            throw new InvalidOperationException("Mod inventory changed; register its item-reference provider before export.");

        JsonCondOwner Resolve(string id) => DataHandler.dictCOs.TryGetValue(id, out var co) ? co :
            DataHandler.dictCOs[DataHandler.dictCOOverlays[id].strCOBase];
        object Product(string id, int count) => new { id, name = Resolve(id).strNameFriendly, count,
            baseValue = EquipmentValueAudit.Price(Resolve(id)) * count };
        object Job(JsonInstallable j) => new {
            id = j.strName, kind = j.strName.EndsWith("Restore", StringComparison.Ordinal) ? "restore" : j.strJobType,
            inputs = j.aInputs ?? Array.Empty<string>(), tools = j.aToolCTsUse ?? Array.Empty<string>(),
            outputs = (j.aLootCOs ?? Array.Empty<string>()).GroupBy(x => x).Select(g => Product(g.Key, g.Count())).ToArray()
        };
        var offers = (IDictionary)typeof(Phobos.Ostranauts.Framework.Trading.MarketStock)
            .GetField("Offers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        string Condition(string branch)
        {
            var offer = offers[branch];
            return offer == null ? "Loot" : offer.GetType().GetField("Condition", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(offer)!.ToString()!;
        }
        var mods = new List<object>();
        foreach (var pair in packs.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            string mod = pair.Key; var d = pair.Value;
            var items = d.Objects.Values.Where(c => mod == "PhobosFramework" || c.strName != MaintenanceDefinitions.SpentParts)
                .OrderBy(c => c.strName, StringComparer.Ordinal).Select(co => new {
                    id = co.strName, name = co.strNameFriendly,
                    massKg = EquipmentSaveUpgrade.Amount(co.aStartingConds, "StatMass"),
                    baseValue = EquipmentValueAudit.Price(co),
                    installed = co.aStartingConds.Any(s => s.Split('=')[0] == "IsInstalled"),
                    operatingModes = co.strName == PhobosShipbreaker.Core.IntakeRules.Grabber + "Installed" ? new[] {
                        new { name="Powered wall cutting", seconds=PhobosShipbreaker.Core.ReclamationRules.CuttingSeconds, kw=PhobosShipbreaker.Core.ReclamationRules.CuttingKW },
                        new { name="Existing G4 intake transfer", seconds=PhobosShipbreaker.Core.IntakeRules.TransferSeconds, kw=PhobosShipbreaker.Core.IntakeRules.WorkingKW }
                    } : Array.Empty<object>(),
                    installTab = d.Installables.Values.FirstOrDefault(j => j.strStartInstall == co.strName)?.strBuildType,
                    jobs = d.Installables.Values.Where(j => j.strActionCO == co.strName).OrderBy(j => j.strName).Select(Job).ToArray(),
                    aliases = mod == "PhobosAutoNav" ? DataHandler.dictCOOverlays.Values.Where(o => o.strCOBase == co.strName).Select(o => o.strName).OrderBy(x => x).ToArray() : Array.Empty<string>()
                }).ToArray();
            var sources = new List<object>();
            foreach (var table in d.Loot.Values.OrderBy(l => l.strName))
            foreach (string link in table.aLoots ?? Array.Empty<string>())
            {
                string branch = link.Split('=')[0];
                if (!branch.StartsWith("Phobos", StringComparison.Ordinal) || link != branch + "=1x1" ||
                    !d.Loot.TryGetValue(branch, out var choice) || choice.strType != "item") continue;
                foreach (string expression in choice.aCOs ?? Array.Empty<string>())
                foreach (string option in expression.Split('|'))
                {
                    var bits = option.Split('='); var weights = bits[1].Split('x');
                    sources.Add(new { table = table.strName, item = bits[0], condition = Condition(branch),
                        chance = double.Parse(weights[0], CultureInfo.InvariantCulture), count = int.Parse(weights[1], CultureInfo.InvariantCulture) });
                }
            }
            string recipesPath = Path.Combine(repo, "mods", mod, "framework/recipes.json");
            var recipes = File.Exists(recipesPath) ? JObject.Parse(File.ReadAllText(recipesPath))["recipes"]! : new JArray();
            var names = new Dictionary<string, string>();
            foreach (string id in recipes.SelectMany(r => r["ingredients"]!.Concat(r["outputs"]!)).Select(i => (string)i["item"]!).Distinct())
                names[id] = Resolve(id).strNameFriendly;
            var metadata = JArray.Parse(File.ReadAllText(Path.Combine(repo, "mods", mod, "mod_info.json")))[0];
            mods.Add(new { id = mod, name = (string)metadata["strName"]!, version = (string)metadata["strModVersion"]!,
                gameTarget = (string)metadata["strGameVersion"]!, items, sources, recipes, recipeItemNames = names });
        }
        string assembly = Path.Combine(game, "Ostranauts_Data/Managed/Assembly-CSharp.dll");
        var result = new { schemaVersion = 1, nativeAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly))).ToLowerInvariant(), mods };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output, JsonConvert.SerializeObject(result, Formatting.Indented) + "\n");
        Console.WriteLine("Exported item-reference facts from native definitions; no game session was run.");
    }
}
