using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

internal static class FurnaceMaterialNativeChecks
{
    internal static void Run(NativeDefinitions definitions, Action<bool,string> check)
    {
        var collector = definitions.Objects[CollectorRules.Installed];
        check(collector.nContainerWidth == 2 && collector.nContainerHeight == 2, "Furnace automation does not enlarge existing collectors");
        foreach (var product in FurnaceMaterialRules.ProductIds)
        {
            var data = new DataCO(definitions.Objects.TryGetValue(product, out var own) ? own : DataHandler.dictCOs[product]);
            check(DataHandler.dictCTs[collector.strContainerCT].TriggeredDataCO(data, false), "Actual native collector filter accepts the released furnace product");
            check(FurnaceMaterialRules.Product(product, data.GetCondAmount("StatMass")), "Native product mass agrees with transfer guard");
        }
        foreach (string ingot in FurnaceRecipes.Ingots)
        {
            var co = definitions.Objects[ingot]; var item = definitions.Items[co.strItemDef];
            check(co.inventoryWidth == 1 && co.inventoryHeight == 1 && item.nCols == 1 && co.nStackLimit == FurnaceRecipes.IngotStack, "Ingots are one-cell stock that stacks to ten: " + ingot);
            check(EquipmentSaveUpgrade.Amount(co.aStartingConds, "StatMass") == FurnaceRecipes.IngotKg && EquipmentSaveUpgrade.Amount(co.aStartingConds, "IsCategoryMetals") > 0 &&
                co.strNameFriendly.StartsWith("Phobos' Rivetline ", StringComparison.Ordinal), "Ingots weigh 4 kg, trade as metals and carry the Rivetline name: " + ingot);
        }
        check(EquipmentSaveUpgrade.Amount(definitions.Objects[FurnaceRecipes.AluminiumIngot].aStartingConds, "StatBasePrice") == FurnaceRecipes.AluminiumIngotPrice &&
            EquipmentSaveUpgrade.Amount(definitions.Objects[FurnaceRecipes.SteelIngot].aStartingConds, "StatBasePrice") == FurnaceRecipes.SteelIngotPrice &&
            EquipmentSaveUpgrade.Amount(definitions.Objects[FurnaceRecipes.SteelRemainder].aStartingConds, "StatBasePrice") == .01, "Authored ingot prices and the technical-minimum steel remainder");
        var feedTrigger = DataHandler.dictCTs[definitions.Objects[FurnaceRules.Feed].strContainerCT];
        check(feedTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[FurnaceRecipes.SteelScrap]), false) && feedTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[FurnaceMaterialRules.Aluminium]), false) &&
            !feedTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs["ItmScrapCarbonFiber"]), false), "The chamber admits steel and aluminium at the game level and nothing else");
        var blank = definitions.Objects[FurnaceRules.Blank];
        check(blank.inventoryWidth == 2 && blank.inventoryHeight == 2, "Blank actually occupies the collector's full inventory grid");
        foreach (string state in new[] { "Installed", "InstalledDmg", "Loose", "LooseDmg" })
        foreach (bool input in new[] { true, false })
        {
            var co = definitions.Objects[FurnaceRules.Prefix + state];
            string[] point = co.mapPoints.Single(p => p.StartsWith(input ? "MaterialIn," : "MaterialOut,")).Split(',');
            var expected = FurnaceMaterialRules.Point(input, 0);
            check(double.Parse(point[1], System.Globalization.CultureInfo.InvariantCulture)/16 == expected.X &&
                double.Parse(point[2], System.Globalization.CultureInfo.InvariantCulture)/16 == expected.Y, "Native named material point matches service geometry in every form");
        }
        Dictionary<string,Dictionary<string,string>> RoundTrip(Dictionary<string,Dictionary<string,string>> maps)
        {
            var native = new JsonItem { aGPMSettings = maps.Select(m => new JsonGUIPropMap { strName=m.Key, dictGUIPropMap=DataHandler.ConvertDictToStringArray(m.Value) }).ToArray() };
            return JsonConvert.DeserializeObject<JsonItem>(JsonConvert.SerializeObject(native))!.aGPMSettings.ToDictionary(m => m.strName, m => DataHandler.ConvertStringArrayToDict(m.dictGUIPropMap));
        }
        var f = new Dictionary<string,Dictionary<string,string>>(); var r = new Dictionary<string,Dictionary<string,string>>();
        var old = new MaterialPort("oldCollector", RoutingRules.CollectorIn, new());
        var dest = new MaterialPort("newCollector", RoutingRules.CollectorIn, new());
        var sink = new MaterialPort("radiator", "PhobosFurnace.Cooling", new());
        var feed = new MaterialPort("F6", RoutingRules.FurnaceIn, f); var output = new MaterialPort("F6", RoutingRules.FurnaceOut, f);
        var heat = new MaterialPort("F6", sink.PortId, f); var metals = new MaterialPort("R4", RoutingRules.MetalsOut, r);
        var residue = new MaterialPort("R4", RoutingRules.SendPort, r);
        check(PortPairing.TryLink(residue, old, out _) && PortPairing.TryLink(metals, feed, out _) &&
            PortPairing.TryLink(output, dest, out _) && PortPairing.TryLink(heat, sink, out _), "Save fixture has four independent pairs");
        SavedPortFilter.Set(dest, RoutingRules.FilterIds("furnace-products"));
        var batchState = new FurnaceState(); batchState.Batch.HotKJ = 1200; batchState.Batch.Armed = true;
        var store = new ObjectStateStore(f, "Furnace", Text.Owner, 1); check(store.TryWrite(batchState.Save()), "Hot batch shares native maps with material pairs");
        var loadedF = RoundTrip(f); var loadedR = RoundTrip(r);
        check(PortPairing.Matches(new MaterialPort("R4", residue.PortId, loadedR), old) &&
            PortPairing.Matches(new MaterialPort("R4", metals.PortId, loadedR), new MaterialPort("F6", feed.PortId, loadedF)) &&
            PortPairing.Matches(new MaterialPort("F6", output.PortId, loadedF), dest) &&
            PortPairing.Matches(new MaterialPort("F6", heat.PortId, loadedF), sink), "Native save conversion preserves old/new links independently");
        var loadedStore = new ObjectStateStore(loadedF, "Furnace", Text.Owner, 1);
        check(loadedStore.Read(out var fields) == SavedStateStatus.Ready && FurnaceState.TryLoad(fields, out var loaded) && loaded.Batch.HotKJ == 1200 && !loaded.Batch.Armed,
            "Material routing does not reset stored heat or restore heating permission");
    }
}
