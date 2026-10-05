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
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Registration;

// Data-only documentation export: no Unity scene, installation, save or inventory access.
internal static class ItemReferenceExport
{
    internal static void Write(string repo, string game, string output)
    {
        foreach (string file in Directory.GetFiles(Path.Combine(repo, "mods/PhobosAutoNav/data/cooverlays"), "*.json"))
            foreach (var overlay in JsonConvert.DeserializeObject<JsonCOOverlay[]>(File.ReadAllText(file))!)
                DataHandler.dictCOOverlays[overlay.strName] = overlay;
        // Framework's own items publish first, as FrameworkLifecycle.Begin orders them in the game (Framework 0.57.0).
        var framework = Phobos.Ostranauts.Framework.Items.FrameworkItems.Prepare();
        framework.Publish();
        var packs = new Dictionary<string, NativeDefinitions> {
            ["PhobosShipbreaker"] = PhobosShipbreaker.Content.Prepare(),
            ["PhobosAgriculture"] = PhobosAgriculture.Definitions.Prepare(),
            ["PhobosAutoNav"] = PhobosAutoNav.EquipmentContent.Prepare()
        };
        // Shipbreaker publishes first so the steel charge's identities exist, as the game's load order gives.
        packs["PhobosShipbreaker"].Publish();
        packs["PhobosManufacturing"] = PhobosManufacturing.Content.Prepare(true);
        // War Has Been Declared adds no items, only orders on the game's navigation stations.
        packs["PhobosWarDeclared"] = PhobosWarDeclared.Content.Prepare();
        // Phobos Medical (0.1.0): the Halewright Ward-3 bed.
        packs["PhobosMedical"] = PhobosMedical.Content.Prepare();
        // Phobos Spacer Stories is data only: story files Framework reads, and no definitions of its own.
        packs["PhobosSpacerStories"] = new NativeDefinitions();
        // Publish only to this audit process's in-memory dictionaries for native valuation.
        foreach (var pack in packs.Values) pack.Publish();
        packs["PhobosFramework"] = framework;
        var installedSources = Directory.GetDirectories(Path.Combine(repo, "mods"))
            .Where(path => File.Exists(Path.Combine(path, "mod_info.json"))).Select(Path.GetFileName).ToHashSet();
        if (!installedSources.SetEquals(packs.Keys))
            throw new InvalidOperationException("Mod inventory changed; register its item-reference provider before export.");

        // Keep authored actions separate from the game's generated maintenance menu.
        // Exercise the same data-only generator that runs during native startup.
        var directActions = packs.Values.SelectMany(p => p.Objects.Values).GroupBy(c => c.strName)
            .ToDictionary(g => g.Key, g => (g.First().aInteractions ?? Array.Empty<string>()).ToArray());
        DataHandler.dictInstallables2 = new Dictionary<string, JsonInstallable>();
        foreach (var job in packs.Values.SelectMany(p => p.Installables.Values).OrderBy(j => j.strName))
            Installables.Create(job);

        JsonCondOwner Resolve(string id) => DataHandler.dictCOs.TryGetValue(id, out var co) ? co :
            DataHandler.dictCOs[DataHandler.dictCOOverlays[id].strCOBase];
        object Product(string id, int count) => new { id, name = Resolve(id).strNameFriendly, count,
            baseValue = EquipmentValueAudit.Price(Resolve(id)) * count };
        bool? TargetPass(JsonInstallable job, string? extra = null)
        {
            if (!DataHandler.dictCTs.TryGetValue(job.CTThem ?? "", out var trigger)) return null;
            var definition = NativeDefinitions.Clone(Resolve(job.strActionCO));
            if (extra != null)
                definition.aStartingConds = definition.aStartingConds.Where(s => !s.StartsWith(extra.Split('=')[0] + "=", StringComparison.Ordinal))
                    .Concat(new[] { extra }).ToArray();
            return trigger.TriggeredDataCO(new DataCO(definition), false);
        }
        object Job(JsonInstallable j) => new {
            id = j.strName, kind = j.strName.EndsWith("Restore", StringComparison.Ordinal) ? "restore" : j.strJobType,
            action = "ACT" + j.strName,
            generated = DataHandler.dictInteractions.ContainsKey("ACT" + j.strName),
            attached = (Resolve(j.strActionCO).aInteractions ?? Array.Empty<string>()).Contains("ACT" + j.strName),
            targetTrigger = j.CTThem,
            targetPassOnFreshDefinition = TargetPass(j),
            targetPassWithWear = TargetPass(j, "StatDamage=1x0.1"),
            targetPassInContainer = TargetPass(j, "IsInContainer=1x1"),
            inputs = j.aInputs ?? Array.Empty<string>(), tools = j.aToolCTsUse ?? Array.Empty<string>(),
            workProgress = EquipmentSaveUpgrade.Amount(Resolve(j.strActionCO).aStartingConds ?? Array.Empty<string>(), j.strProgressStat + "Max"),
            outputs = (j.aLootCOs ?? Array.Empty<string>()).GroupBy(x => x).Select(g => Product(g.Key, g.Count())).ToArray()
        };
        var offers = (IDictionary)typeof(Phobos.Ostranauts.Framework.Trading.MarketStock)
            .GetField("Offers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        string Condition(string branch)
        {
            var offer = offers[branch];
            if (offer == null) return "Loot";
            string condition = offer.GetType().GetField("Condition", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(offer)!.ToString()!;
            // Faction kiosk offers carry the reputation mark their stock receives (Neutral carries none).
            var mark = (string?)offer.GetType().GetField("Mark", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(offer);
            if (branch.StartsWith("PhobosFaction_", StringComparison.Ordinal))
                condition += ", " + (mark == null ? "Neutral" : mark.Substring(Phobos.Ostranauts.Framework.Trading.FactionKiosks.MarkPrefix.Length)) + " standing";
            return condition;
        }
        var mods = new List<object>();
        foreach (var pair in packs.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            string mod = pair.Key; var d = pair.Value;
            var items = d.Objects.Values.Where(c => mod == "PhobosFramework" || c.strName != MaintenanceDefinitions.SpentParts)
                .OrderBy(c => c.strName, StringComparer.Ordinal).Select(co => new {
                    id = co.strName, name = co.strNameFriendly,
                    stackLimit = Math.Max(1, co.nStackLimit),
                    actions = directActions[co.strName],
                    missingDirectActions = directActions[co.strName].Where(a => !DataHandler.dictInteractions.ContainsKey(a)).ToArray(),
                    container = new {
                        trigger = co.strContainerCT,
                        width = co.nContainerWidth, height = co.nContainerHeight,
                        flag = (co.aStartingConds ?? Array.Empty<string>()).Any(s => s.Split('=')[0] == "IsContainer"),
                        inventoryAction = directActions[co.strName].Contains("Inventory"),
                        ownedSlots = co.aSlotsWeHave ?? Array.Empty<string>(),
                        loot = co.strLoot
                    },
                    handlingFlags = (co.aStartingConds ?? Array.Empty<string>()).Select(s => s.Split('=')[0]).Where(s => new[] { "IsInstalled", "IsCumbersome", "IsSystem", "IsPocketable", "IsDamaged" }.Contains(s)).OrderBy(s => s).ToArray(),
                    slots = (co.mapSlotEffects ?? Array.Empty<string>()).Where((s, i) => i % 2 == 0).ToArray(),
                    massKg = EquipmentSaveUpgrade.Amount(co.aStartingConds ?? Array.Empty<string>(), "StatMass"),
                    baseValue = EquipmentValueAudit.Price(co),
                    installed = (co.aStartingConds ?? Array.Empty<string>()).Any(s => s.Split('=')[0] == "IsInstalled"),
                    operatingModes = co.strName == PhobosShipbreaker.Core.IntakeRules.Grabber + "Installed" ? new[] {
                        new { name="Powered wall cutting", seconds=PhobosShipbreaker.Core.ReclamationRules.CuttingSeconds, kw=PhobosShipbreaker.Core.ReclamationRules.CuttingKW },
                        new { name="Existing G4 intake transfer", seconds=PhobosShipbreaker.Core.IntakeRules.TransferSeconds, kw=PhobosShipbreaker.Core.IntakeRules.WorkingKW }
                    } : Array.Empty<object>(),
                    installTab = d.Installables.Values.FirstOrDefault(j => j.strStartInstall == co.strName)?.strBuildType,
                    jobs = d.Installables.Values.Where(j => j.strActionCO == co.strName).OrderBy(j => j.strName).Select(Job).ToArray(),
                    aliases = mod == "PhobosAutoNav" ? DataHandler.dictCOOverlays.Values.Where(o => o.strCOBase == co.strName).Select(o => o.strName).OrderBy(x => x).ToArray() : Array.Empty<string>()
                }).ToArray();
            var sources = new List<object>();
            // Native tables are amended in place at publication; the set records which of its branches
            // each table links. Walk the live table so link order matches the game's.
            foreach (var linked in d.LootBranches.OrderBy(p => p.Key, StringComparer.Ordinal))
            foreach (string link in DataHandler.dictLoot.TryGetValue(linked.Key, out var table) ? table.aLoots ?? Array.Empty<string>() : Array.Empty<string>())
            {
                string branch = link.Split('=')[0];
                if (!linked.Value.Contains(branch) || link != branch + "=1x1" ||
                    !d.Loot.TryGetValue(branch, out var choice) || choice.strType != "item") continue;
                foreach (string expression in choice.aCOs ?? Array.Empty<string>())
                foreach (string option in expression.Split('|'))
                {
                    var bits = option.Split('='); var weights = bits[1].Split('x');
                    sources.Add(new { table = linked.Key, item = bits[0], condition = Condition(branch),
                        chance = double.Parse(weights[0], CultureInfo.InvariantCulture), count = int.Parse(weights[1], CultureInfo.InvariantCulture) });
                }
            }
            // Carved shares of native tables: only this mod's own objects are its documented sources.
            foreach (var carved in d.LootCarves.OrderBy(p => p.Key, StringComparer.Ordinal))
            foreach (var share in carved.Value.OrderBy(p => p.Key, StringComparer.Ordinal))
                if (d.Objects.ContainsKey(share.Key) && share.Value.Share > 0)
                    sources.Add(new { table = carved.Key, item = share.Key, condition = "Loot", chance = share.Value.Share, count = 1 });
            string recipesPath = Path.Combine(repo, "mods", mod, "framework/recipes.json");
            var recipes = File.Exists(recipesPath) ? JObject.Parse(File.ReadAllText(recipesPath))["recipes"]! : new JArray();
            foreach (var recipe in recipes)
                recipe["retiredFromMenus"] = (bool?)recipe["retired"] == true;
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
