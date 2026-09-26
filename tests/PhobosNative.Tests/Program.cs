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
PairingSaveChecks.Run(Check);
FlightHubNativeChecks.Run(Check);
void Load<T>(string folder, Dictionary<string,T> destination, Func<T,string> key)
{
    foreach (string file in Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories))
        foreach (var value in JsonConvert.DeserializeObject<T[]>(File.ReadAllText(file))!) destination[key(value)] = value;
}
DataHandler.dictCOs = new(); DataHandler.dictItemDefs = new(); DataHandler.dictConds = new();
DataHandler.dictCTs = new(); DataHandler.dictInteractions = new(); DataHandler.dictLoot = new();
DataHandler.dictSlots = new(); DataHandler.dictPowerInfo = new(); DataHandler.dictInstallables = new();
DataHandler.dictCOOverlays = new();
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
PlaceholderHealthChecks.Run(Check);
SavedGridHeaderChecks.Run(Check);
if (args.Length == 5 && args[2] == "--audit-room-grid")
{
    SavedGridHeaderChecks.Audit(args[3], args[4]);
    return;
}
if (args.Length == 5 && args[2] == "--audit-placeholder-health")
{
    PlaceholderHealthChecks.Audit(args[3], args[4]);
    return;
}
if (args.Length == 4 && args[2] == "--export-item-reference")
{
    ItemReferenceExport.Write(repo, game, args[3]);
    return;
}
var missing = DependencyContract.MissingDefinitions((table,id) => table switch {
    "objects" => DataHandler.dictCOs.ContainsKey(id), "items" => DataHandler.dictItemDefs.ContainsKey(id),
    "conditions" => DataHandler.dictConds.ContainsKey(id), "triggers" => DataHandler.dictCTs.ContainsKey(id),
    "interactions" => DataHandler.dictInteractions.ContainsKey(id), "loot" => DataHandler.dictLoot.ContainsKey(id), _ => false });
Check(missing.Count == 0, string.Join("\n", missing));
Check(!DataHandler.dictCOs.ContainsKey("SWB_SorterInstalled"), "No Workshop templates loaded");
var agriculture = PhobosAgriculture.Definitions.Prepare();
AgricultureNativeChecks.Run(agriculture, repo, Check, Throws);
NutrientProductionNativeChecks.Run(agriculture, game, Check);
if (args.Length > 3) AgricultureEconomyAudit.Write(agriculture, repo, args[3]);
foreach (var co in agriculture.Objects.Values) {
    Check(co.strNameFriendly.StartsWith("Phobos' ", StringComparison.Ordinal), "Agriculture names are branded: " + co.strName);
    Check(agriculture.Items.ContainsKey(co.strItemDef) || DataHandler.dictItemDefs.ContainsKey(co.strItemDef), "Agriculture item reference exists: " + co.strName);
}
foreach (var name in new[]{ "PhobosVerdemorrowFirstlight4Installed", "PhobosVerdemorrowHearth2Installed" }) {
    Check(agriculture.Objects[name].aTickers.Contains("Power"), "Agriculture native power ticker");
    Check(agriculture.Objects[name].aInteractions.Contains("PhobosAgricultureControls"), "Agriculture local controls");
}
var farmRecipes = JsonConvert.DeserializeObject<RecipePack>(File.ReadAllText(Path.Combine(repo, "mods/PhobosAgriculture/framework/recipes.json")))!;
foreach (var recipe in farmRecipes.recipes) { RecipeRules.Validate(recipe); Check(true, "Agriculture recipe balance"); }
Check(agriculture.Loot["PhobosVerdemorrowLettuceEffects"].aCOs.Contains("TDnFood=1x1"), "Lettuce does not grant ordinary five-unit hunger effect");
Check(agriculture.Loot["PhobosVerdemorrowHearthPotatoesEffects"].aCOs.Contains("TDnFood=1x5"), "Potato meal has explicit hunger effect");
if (args.Length > 2 && args[2] == "--agriculture-only")
{
    Console.WriteLine($"PASS: {checks} Agriculture/persistence/native-definition checks; economic report generated without preparing unrelated content. No game session was run.");
    return;
}
var prepared = Content.Prepare();
PlaceholderLoadChecks.Run(prepared, Check);
foreach (var equipment in prepared.Objects.Values)
    Check(equipment.strNameFriendly.StartsWith("Phobos' ", StringComparison.Ordinal), "Branded native machine, section or material: " + equipment.strName);
prepared.Publish();
ProcessingSaveChecks.Run(Check, Throws);
ReclaimerNativeChecks.Run(prepared, Check);
IndustrialNativeChecks.Run(prepared, repo, Check);
ObservationNativeChecks.Run(Check);
FurnaceCoolingNativeChecks.Run(prepared, repo, Check);
FurnaceMaterialNativeChecks.Run(prepared, Check);
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
Check(prepared.Objects.Count == 56 && prepared.Installables.Count == 123, $"Nine machine families, coolant conduit, charge chamber and preserved/new material identities ({prepared.Objects.Count} objects, {prepared.Installables.Count} actions)");
var furnaceItem = prepared.Items[FurnaceRules.Prefix + "Installed"];
var furnaceFeed = DataHandler.dictCTs[prepared.Objects[FurnaceRules.Feed].strContainerCT];
Check(furnaceItem.nCols == 6 && furnaceItem.aSocketAdds.Length == 36 && furnaceItem.aSocketReqs.Length == 64, "F6 occupies six by six native tiles");
Check(furnaceFeed.TriggeredDataCO(new DataCO(DataHandler.dictCOs["ItmScrapAluminum"]), false), "Native aluminium enters the furnace feed trigger");
Check(!furnaceFeed.TriggeredDataCO(new DataCO(DataHandler.dictCOs["ItmScrapSteel"]), false), "Steel is excluded from the first casting recipe");
var aluminium = DataHandler.dictCOs["ItmScrapAluminum"]; var aluminiumItem = DataHandler.dictItemDefs[aluminium.strItemDef];
int aluminiumWidth = aluminium.inventoryWidth > 0 ? aluminium.inventoryWidth : aluminiumItem.nCols;
int aluminiumHeight = aluminium.inventoryHeight > 0 ? aluminium.inventoryHeight : aluminiumItem.aSocketAdds.Length / aluminiumItem.nCols;
Check((prepared.Objects[FurnaceRules.Feed].nContainerWidth / aluminiumWidth) * (prepared.Objects[FurnaceRules.Feed].nContainerHeight / aluminiumHeight) >= FurnaceRules.ChargeUnits, "Twenty unstacked native aluminium footprints fit the finite chamber inventory");
Check(typeof(CondOwner).GetMethod(nameof(CondOwner.CanStackOnItem))?.ReturnType == typeof(int), "Native stack-capacity hook used to retain individual charge identities resolves");
var radiatorItem = prepared.Items[FurnaceRules.Radiator + "Installed"];
Check(radiatorItem.nCols == 6 && radiatorItem.aSocketAdds.Length == 24 && radiatorItem.aSocketReqs.Count(s => s == "TILWall") == 6, "Radiator occupies six by four exterior tiles with six hull supports");
foreach (var item in prepared.Items.Values.Where(i => i.strName.StartsWith(FurnaceRules.Prefix)))
    Check(File.Exists(Path.Combine(repo, "mods/PhobosShipbreaker/images", item.strImg + ".png")) && File.Exists(Path.Combine(repo, "mods/PhobosShipbreaker/images", item.strImgNorm + ".png")), "Furnace derivative resolves: " + item.strName);
Check(prepared.Slots[Content.InputSlot].bHide, "Ordinary processor Inventory no longer exposes two grids");
var grabber = prepared.Objects[IntakeRules.Grabber + "Installed"];
var grabberTrigger = DataHandler.dictCTs[grabber.strContainerCT];
Check(grabberTrigger.TriggeredDataCO(wallData, false), "Native grabber inventory accepts the cumbersome wall");
Check(grabberTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs["ItmScrapSteel"]), false), "Cumbersome-capable grabber also accepts small solids");
var chuteItem = prepared.Items[IntakeRules.Chute + "Installed"];
var grabberItem = prepared.Items[IntakeRules.Grabber + "Installed"];
Check(chuteItem.nCols == 4 && chuteItem.aSocketAdds.Length == 4 && chuteItem.aSocketReqs.Length == 18, "Chute is one row of four native tiles");
Check(grabberItem.nCols == 4 && grabberItem.aSocketAdds.Length == 12 && grabberItem.aSocketReqs.Length == 30, "Grabber is four by three with native padding");
Check(chuteItem.aSocketReqs.Count(x => x == "TILWall") == 4 && chuteItem.aSocketAdds.All(x => x == "TILWallDecoAdds"), "Chute requires existing walls and cannot create its own pressure boundary");
Check(grabberItem.aSocketReqs.Skip(25).Take(4).All(x => x == "TILWall") && !grabberItem.aSocketReqs.Contains("TILFloor"), "Exterior grabber requires the adjacent rear wall row, not interior flooring");
Check(!prepared.Loot["PhobosHullChuteForbids"].aCOs.Any(x => x.StartsWith("IsWall=") || x.StartsWith("IsConduit=")), "Chute does not forbid its wall support or separate electrical conduit");
foreach (var item in prepared.Items.Values)
foreach (string socket in (item.aSocketAdds ?? Array.Empty<string>()).Concat(item.aSocketReqs ?? Array.Empty<string>()).Concat(item.aSocketForbids ?? Array.Empty<string>()))
    Check(DataHandler.dictLoot.ContainsKey(socket), "Native placement socket resolves: " + item.strName + " / " + socket);
foreach (var item in prepared.Items.Values.Where(i => i.strName.StartsWith(IntakeRules.Chute) || i.strName.StartsWith(IntakeRules.Grabber)))
    Check(File.Exists(Path.Combine(repo, "mods/PhobosShipbreaker/images", item.strImg + ".png")) && File.Exists(Path.Combine(repo, "mods/PhobosShipbreaker/images", item.strImgNorm + ".png")), "Runtime hull artwork exists: " + item.strName);
var collector = prepared.Objects[CollectorRules.Installed];
var collectorItem = prepared.Items[collector.strItemDef];
Check(collector.nContainerWidth == 2 && collector.nContainerHeight == 2, "Finite four-cell collection chamber");
Check(collectorItem.nCols == 2 && collectorItem.aSocketAdds.Length == 2 && collectorItem.aSocketReqs.Length == 12, "Collector full intended 2 x 1 footprint and padded sockets");
Check(collectorItem.aSocketReqs.Count(s=>s=="TILWall")==2 && collectorItem.aSocketAdds.All(s=>s=="TILWallDecoAdds"), "Collector mounts over intact walls without creating floor or pressure portal");
Check(collector.aInteractions.Contains(IndustrialRules.LocalControls) && prepared.Interactions[CollectorRules.Controls].strRaiseUI == null, "Own control-panel action does not accidentally open native inventory UI");
var residueData = new DataCO(prepared.Objects[ProcessRules.Residue]);
Check(DataHandler.dictCTs[collector.strContainerCT].TriggeredDataCO(residueData,false), "Native container accepts residue before the runtime exact filter");
var residueItem = prepared.Items[ProcessRules.Residue];
int residueWidth = prepared.Objects[ProcessRules.Residue].inventoryWidth;
int residueHeight = prepared.Objects[ProcessRules.Residue].inventoryHeight;
if (residueWidth == 0) residueWidth = residueItem.nCols;
if (residueHeight == 0) residueHeight = residueItem.aSocketAdds.Length / residueItem.nCols;
Check(residueWidth == 1 && residueHeight == 1, "Four actual residue footprints fit the collection chamber");
Check(File.Exists(Path.Combine(repo,"mods/PhobosShipbreaker/images",collectorItem.strImg+".png")), "Collector runtime art packaged");
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
    if (!definition.bNoDestructable)
    {
        Check(DataHandler.dictLoot["Output" + definition.strName].aCOs.All(x => x.Split('=').Length == 2 && DataHandler.dictCOs.ContainsKey(x.Split('=')[0])), "Maintenance output resolves to an existing identity");
        Check(DataHandler.dictCOs[definition.strActionCO].aUpdateCommands.Any(x => x.StartsWith("Destructable," + definition.strProgressStat + ",MS" + definition.strName + ",")), "Progress switches use native save-compatible identities");
    }
}
FrameworkLifecycle.Begin();
var filePath = Path.Combine(repo, "mods/PhobosShipbreaker/framework/recipes.json");
var frameworkText = Phobos.Ostranauts.Framework.Localization.Translations.Register(FrameworkInfo.PluginId,
    typeof(FrameworkInfo).Assembly, "PhobosFramework.en.json");
frameworkText.Select("fr", "{\"ConstructionRegistry.ready\":\"Prêt\",\"ConstructionRegistry.registered\":\"Enregistré\"}");
ConstructionRegistry.RegisterPack(Plugin.Id, filePath);
FrameworkLifecycle.Complete();
Check(ConstructionRegistry.Ready(Plugin.Id), ConstructionRegistry.Status(Plugin.Id));
Check(ConstructionRegistry.Status(Plugin.Id) == "Prêt", "Translated construction status cannot change readiness logic");
frameworkText.Select("en");
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
ClearConstruction();
EconomyChecks.Run(repo, Check, Throws);
EquipmentValueAudit.Run(repo, Check, args.Length > 2 ? args[2] : null);
InstallMenuChecks.Run(agriculture, prepared, Check, Throws);
AutoNavHubChecks.Run(repo, Check, Throws);
FireNativeChecks.Run(Check);
ShipbreakerGeometryChecks.Run(Check);
var ordinaryUninstall=Directory.GetFiles(Path.Combine(native,"installables"),"*.json",SearchOption.AllDirectories)
    .SelectMany(f=>JsonConvert.DeserializeObject<JsonInstallable[]>(File.ReadAllText(f))!).Single(i=>i.strName=="Wall1x1Uninstall");
Installables.Create(ordinaryUninstall);
Check(ReclamationGeometry.NativeWallContract(),"G4 uninstallation resolves exactly one native loose ordinary wall");
var nativeManeuver=typeof(Ship).GetMethods().Single(m=>m.Name=="Maneuver");
Check(nativeManeuver.GetParameters()[4].ParameterType==typeof(float),"Final swept-command guard binds native simulation interval at index four");
RegionalEconomyChecks.Run(game, Check, Throws);
StockQuantityChecks.Run(Check, Throws);
Console.WriteLine($"PASS: {checks} native-definition/registration checks with no OCF or Workshop loaded. No game session was run.");
