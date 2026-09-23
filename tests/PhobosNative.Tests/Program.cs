using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Construction;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

string game = args[0], repo = args[1];
string native = Path.Combine(game, "Ostranauts_Data", "StreamingAssets", "data");
int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
void Throws(Action call, string message) { bool threw = false; try { call(); } catch { threw = true; } Check(threw, message); }
void Load<T>(string folder, Dictionary<string,T> destination, Func<T,string> key)
{
    foreach (string file in Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories))
        foreach (var value in JsonConvert.DeserializeObject<T[]>(File.ReadAllText(file))!) destination[key(value)] = value;
}
DataHandler.dictCOs = new(); DataHandler.dictItemDefs = new(); DataHandler.dictConds = new();
DataHandler.dictCTs = new(); DataHandler.dictInteractions = new(); DataHandler.dictLoot = new();
DataHandler.dictSlots = new(); DataHandler.dictPowerInfo = new(); DataHandler.dictInstallables = new();
Load(Path.Combine(native, "condowners"), DataHandler.dictCOs, x => x.strName);
Load(Path.Combine(native, "items"), DataHandler.dictItemDefs, x => x.strName);
Load(Path.Combine(native, "conditions"), DataHandler.dictConds, x => x.strName);
foreach (string file in Directory.GetFiles(Path.Combine(native, "conditions_simple"), "*.json"))
foreach (var record in Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(file)))
{
    var values = record["aValues"]!;
    for (int i = 0; i < values.Count(); i += 7)
    {
        string id = (string)values[i]!;
        DataHandler.dictConds[id] = new JsonCond { strName = id };
    }
}
Load(Path.Combine(native, "condtrigs"), DataHandler.dictCTs, x => x.strName);
Load(Path.Combine(native, "interactions"), DataHandler.dictInteractions, x => x.strName);
Load(Path.Combine(native, "loot"), DataHandler.dictLoot, x => x.strName);
Load(Path.Combine(repo, "mods/PhobosShipbreaker/data/conditions"), DataHandler.dictConds, x => x.strName);
Load(Path.Combine(repo, "mods/PhobosShipbreaker/data/condtrigs"), DataHandler.dictCTs, x => x.strName);
var missing = DependencyContract.MissingDefinitions((table,id) => table switch {
    "objects" => DataHandler.dictCOs.ContainsKey(id), "items" => DataHandler.dictItemDefs.ContainsKey(id),
    "conditions" => DataHandler.dictConds.ContainsKey(id), "triggers" => DataHandler.dictCTs.ContainsKey(id),
    "interactions" => DataHandler.dictInteractions.ContainsKey(id), "loot" => DataHandler.dictLoot.ContainsKey(id), _ => false });
Check(missing.Count == 0, string.Join("\n", missing));
Check(!DataHandler.dictCOs.ContainsKey("SWB_SorterInstalled"), "No Workshop templates loaded");
var prepared = Content.Prepare();
prepared.Publish();
// Exercise the game's own data-only trigger evaluator against its actual wall
// definition: the ordinary solid-container filter caused the grey inventory bug.
var wallData = new DataCO(DataHandler.dictCOs[ProcessRules.Wall]);
var feedTrigger = DataHandler.dictCTs[prepared.Objects[Content.InputBin].strContainerCT];
Check(wallData.HasCond("IsCumbersome"), "Native ordinary wall is cumbersome");
Check(!DataHandler.dictCTs["TIsFitContainerSolid"].TriggeredDataCO(wallData, false), "Reproduce the former native wall rejection");
Check(feedTrigger.TriggeredDataCO(wallData, false), "Feed accepts native ordinary loose wall despite cumbersome flag");
var floorDefinitions = DataHandler.dictCOs.Values.Where(x => x.strName.StartsWith("ItmFloor", StringComparison.Ordinal) &&
    x.strName.EndsWith("Loose", StringComparison.Ordinal)).ToArray();
Check(floorDefinitions.Length > 0 && floorDefinitions.All(x => !feedTrigger.TriggeredDataCO(new DataCO(x), false)), "Native loose floors are not panel-feed inputs");
foreach (string forbidden in new[] { "IsInstalled", "IsOversized" })
{
    var modifiedWall = new JsonCondOwner { aStartingConds = DataHandler.dictCOs[ProcessRules.Wall].aStartingConds.Concat(new[] { forbidden + "=1.0x1" }).ToArray() };
    Check(!feedTrigger.TriggeredDataCO(new DataCO(modifiedWall), false), "Feed preserves native exclusion: " + forbidden);
}
Check(!feedTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[Content.Loose]), false), "Cumbersome machinery cannot enter the wall feed");
foreach (var machine in prepared.Objects.Values.Where(x => Content.IsMachine(x.strName)))
    Check(!machine.dictSlotsLayout.ContainsKey(Content.InputSlot), "Native feed window retains its title on " + machine.strName);
Check(prepared.Objects.Count == 7 && prepared.Installables.Count == 6, "Complete independent machine family");
foreach (var co in prepared.Objects.Values)
{
    Check(DataHandler.dictItemDefs.ContainsKey(co.strItemDef), "Resolvable item: " + co.strName);
    foreach (string condition in co.aStartingConds ?? Array.Empty<string>())
        Check(DataHandler.dictConds.ContainsKey(condition.Split('=')[0]), "Resolvable condition: " + condition);
}
DataHandler.dictInstallables2 = new();
foreach (var definition in prepared.Installables.Values)
{
    Installables.Create(definition);
    Check(DataHandler.dictInteractions.ContainsKey("ACT" + definition.strName), "Native action generated: " + definition.strName);
    Check(DataHandler.dictLoot["Output" + definition.strName].aCOs.All(x => x.Split('=').Length == 2 && DataHandler.dictCOs.ContainsKey(x.Split('=')[0])), "Install/repair output resolves to our existing identity");
    Check(DataHandler.dictCOs[definition.strActionCO].aUpdateCommands.Any(x => x.StartsWith("Destructable," + definition.strProgressStat + ",MS" + definition.strName + ",")), "Progress switches use native save-compatible identities");
}
FrameworkLifecycle.Begin();
var filePath = Path.Combine(repo, "mods/PhobosShipbreaker/framework/recipes.json");
ConstructionRegistry.RegisterPack(Plugin.Id, filePath);
FrameworkLifecycle.Complete();
Check(ConstructionRegistry.Ready(Plugin.Id), ConstructionRegistry.Status(Plugin.Id));
foreach (var recipe in ConstructionRegistry.Recipes.Values)
{
    Check(recipe.Stations.Contains("ItmTable01") && recipe.Stations.Contains("ItmTable02") && !recipe.Stations.Contains("SWB_WorkbenchInstalled"), "Obtainable native assembly surfaces without Workshop");
    foreach (string alias in recipe.Recipe.legacyActionIds)
    {
        string name = alias;
        var original = new JsonInteractionSave { strName = alias, strChainStart = alias, objUs = "crew-A", objThem = "bench-B", strChainGUID = "job-C", aLootItemRemoveContract = new[] { "material-D" } };
        var save = original;
        ConstructionHooks.Translate(ref name, ref save);
        Check(name == RecipeRules.ActionId(recipe.Recipe.id) && save.strName == name && save.strChainStart == name, "Queued action translates to new provider");
        Check(save.objUs == "crew-A" && save.objThem == "bench-B" && save.strChainGUID == "job-C" && save.aLootItemRemoveContract[0] == "material-D", "Queue identity and material references are preserved");
        Check(original.strName == alias && !ReferenceEquals(save, original), "Source save DTO is untouched");
        Check(!DataHandler.dictInteractions.ContainsKey(alias), "No runtime action retains the competing OCF prefix");
    }
}
Throws(() => ConstructionRegistry.RegisterPack("LateAuthor", filePath), "Late registration rejected");
// A failed second recipe must not publish the first, its aliases or station menu.
void ClearConstruction()
{
    foreach (string key in DataHandler.dictInteractions.Keys.Where(k => k.StartsWith("PhobosCraft_")).ToArray()) DataHandler.dictInteractions.Remove(key);
    foreach (string key in DataHandler.dictLoot.Keys.Where(k => k.StartsWith("PhobosCraft")).ToArray()) DataHandler.dictLoot.Remove(key);
    foreach (string key in DataHandler.dictCTs.Keys.Where(k => k.StartsWith("PhobosCraft")).ToArray()) DataHandler.dictCTs.Remove(key);
    foreach (var co in DataHandler.dictCOs.Values)
        if (co.aInteractions != null) co.aInteractions = co.aInteractions.Where(k => !k.StartsWith("PhobosCraft_")).ToArray();
    FrameworkLifecycle.Begin();
}
ClearConstruction();
var pack = JsonConvert.DeserializeObject<RecipePack>(File.ReadAllText(filePath))!;
var broken = JsonConvert.DeserializeObject<RecipePack>(File.ReadAllText(filePath))!;
broken.recipes[1].outputs[0].unitMassKg = 170;
int interactionsBefore = DataHandler.dictInteractions.Count;
Throws(() => ConstructionRegistry.Register("BrokenAuthor", broken.recipes), "Invalid second stage rejects whole pack");
Check(DataHandler.dictInteractions.Count == interactionsBefore && ConstructionRegistry.Recipes.Count == 0 && !DataHandler.dictCOs["ItmTable01"].aInteractions.Any(k => k.StartsWith("PhobosCraft_")), "Failed registration publishes neither actions nor menu entries");
// Runtime-modified material definitions are a compatibility difference worth checking.
var steel = DataHandler.dictCOs["ItmScrapSteel"];
var originalConditions = steel.aStartingConds;
steel.aStartingConds = originalConditions.Select(x => x.StartsWith("StatMass=") ? "StatMass=1.0x2" : x).ToArray();
Throws(() => ConstructionRegistry.Register("ChangedMass", pack.recipes), "Changed native unit mass blocks registration");
Check(ConstructionRegistry.Recipes.Count == 0, "No recipe published with incorrect mass");
steel.aStartingConds = originalConditions;
// An optional foreign bench is accepted without importing its definitions or code.
DataHandler.dictCOs["SWB_WorkbenchInstalled"] = new JsonCondOwner { strName = "SWB_WorkbenchInstalled", aStartingConds = new[] { "IsInstalled=1.0x1" }, aInteractions = new[] { "ForeignCraft" } };
ConstructionRegistry.Register("GoodAuthor", pack.recipes);
pack.recipes[0].ingredients[0].count = 1;
FrameworkLifecycle.Complete();
Check(ConstructionRegistry.Ready("GoodAuthor") && ConstructionRegistry.Recipes.Values.First().Recipe.ingredients[0].count == 50, "Published recipe uses a defensive snapshot");
Check(DataHandler.dictCOs["SWB_WorkbenchInstalled"].aInteractions.Contains("ForeignCraft") && ConstructionRegistry.Recipes.Values.All(x => x.Stations.Contains("SWB_WorkbenchInstalled")), "Optional Workshop bench preserves foreign actions");
Check(ConstructionRegistry.ResolveAction("OCF_Craft_ForeignMod") == "OCF_Craft_ForeignMod", "Other authors' queues are untouched");
ClearConstruction();
DataHandler.dictInteractions["OCF_Craft_PhobosBuildShipbreaker"] = new JsonInteraction();
Throws(() => ConstructionRegistry.RegisterPack("ConflictBefore", filePath), "Existing competing provider rejected");
Check(ConstructionRegistry.Recipes.Count == 0, "Provider collision never leaves a partial pack");
DataHandler.dictInteractions.Remove("OCF_Craft_PhobosBuildShipbreaker");
ConstructionRegistry.RegisterPack("ConflictAfter", filePath);
DataHandler.dictInteractions["OCF_Craft_PhobosBuildShipbreaker"] = new JsonInteraction();
FrameworkLifecycle.Complete();
Check(!ConstructionRegistry.Ready("ConflictAfter") && ConstructionRegistry.Status("ConflictAfter").Contains("Competing provider"), "A provider registering later still blocks completion");
Console.WriteLine($"PASS: {checks} native-definition/registration checks with no OCF or Workshop loaded. No game session was run.");
