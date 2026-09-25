using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

internal static class RegionalEconomyChecks
{
    internal static void Run(string game, Action<bool, string> check, Action<Action, string> throws)
    {
        string root = Path.Combine(game, "Ostranauts_Data/StreamingAssets/data");
        var markets = JArray.Parse(File.ReadAllText(Path.Combine(root, "market/Markets/market_actor_configs.json")))
            .Select(x => (string)x["strName"]!).Where(x => x.EndsWith("CargoKiosk", StringComparison.Ordinal))
            .Select(x => x.Replace("CargoKiosk", "")).ToHashSet();
        check(markets.Count == 19, "Review coverage when vanilla market inventory changes");
        foreach (string region in markets)
            check(DataHandler.dictLoot.ContainsKey(RegionalMarkets.SupplyTable(region)), "Real vanilla retail endpoint: " + region);
        var newMarkets = markets.Except(new[] { "OKLG", "VORB", "MLAB", "JPTN" }).ToHashSet();
        var packs = new (Func<NativeDefinitions> Prepare, (string Region, double Factor)[] Profiles)[] {
            (() => PhobosShipbreaker.Content.Prepare(), PhobosShipbreaker.RegionalEconomy.Profiles),
            (() => PhobosAgriculture.Definitions.Prepare(), PhobosAgriculture.RegionalEconomy.Profiles),
            (() => PhobosAutoNav.EquipmentContent.Prepare(), PhobosAutoNav.RegionalEconomy.Profiles)
        };
        foreach (var (prepare, profiles) in packs)
        {
            var pack = prepare();
            check(profiles.Select(p => p.Region).ToHashSet().SetEquals(newMarkets), "Content covers every additional placed vanilla supply/scrap endpoint");
            check(profiles.Length == newMarkets.Count, "No duplicate regional profiles");
            foreach (var profile in profiles)
            {
                string table = RegionalMarkets.SupplyTable(profile.Region);
                var native = DataHandler.dictLoot[table];
                var added = pack.Loot[table];
                check((native.aCOs ?? Array.Empty<string>()).SequenceEqual(added.aCOs), "Preserve native direct stock");
                check((native.aLoots ?? Array.Empty<string>()).All(x => added.aLoots.Contains(x)), "Preserve native and foreign stock branches");
                foreach (var branch in pack.Loot.Values.Where(x => x.strName.StartsWith("PhobosRegional_" + profile.Region + "_", StringComparison.Ordinal)))
                {
                    check(added.aLoots.Count(x => x == branch.strName + "=1x1") == 1, "Regional branch occurs once on repeated preparation");
                    check(branch.aCOs.Length == 1 && branch.aCOs[0].EndsWith("x1", StringComparison.Ordinal), "At most one physical item per offer roll");
                    double chance = double.Parse(branch.aCOs[0].Split('=')[1].Split('x')[0], CultureInfo.InvariantCulture);
                    check(chance > 0 && chance <= 1, "Bounded regional offer probability");
                }
            }
            pack.Publish();
        }
        // Exercise native collection membership and price lookup, not a second pricing implementation.
        DataHandler.dictDataCOs = DataHandler.dictCOs.ToDictionary(x => x.Key, x => new DataCO(x.Value));
        foreach (var overlay in DataHandler.dictCOOverlays.Values.Where(x => x.strName.StartsWith("Phobos", StringComparison.Ordinal)))
            if (DataHandler.dictCOs.TryGetValue(overlay.strCOBase, out var co))
                DataHandler.dictDataCOs[overlay.strName] = new DataCO(co, overlay, null);
        DataHandler.dictDataCoCollections = new Dictionary<string, DataCoCollection>();
        foreach (string file in Directory.GetFiles(Path.Combine(root, "market/CoCollections"), "*.json", SearchOption.AllDirectories))
        foreach (var definition in JsonConvert.DeserializeObject<JsonDCOCollection[]>(File.ReadAllText(file))!)
            DataHandler.dictDataCoCollections[definition.strName] = new DataCoCollection(definition);
        foreach (var (prepare, profiles) in packs)
        foreach (var branch in prepare().Loot.Values.Where(x => x.strName.StartsWith("PhobosRegional_", StringComparison.Ordinal)))
        {
            string item = branch.aCOs[0].Split('=')[0];
            string filter = branch.strName.StartsWith("PhobosRegional_OFLT_", StringComparison.Ordinal)
                ? "TIsBarterFlotillaScrapKioskSell" : "TIsBarterChargen";
            check(DataHandler.dictCTs[filter].TriggeredDataCO(DataHandler.dictDataCOs[item], false),
                "Native regional merchant sell filter accepts " + item);
        }
        foreach (var pair in new[] {
            (PhobosAutoNav.NavigationService.ModuleId, "AnyControlSystems"),
            (PhobosAutoNav.NavigationService.PursuitId, "AnyControlSystems"),
            (PhobosAutoNav.NavigationService.FireControlId, "AnyControlSystems"),
            (PhobosShipbreaker.Content.Loose, "AnyIndustrialProducts"),
            (PhobosShipbreaker.FurnaceService.CoolantStock, "AnyIndustrialProducts"),
            (PhobosAgriculture.WorkupDefinitions.Makeup, "AnyIndustrialProducts"),
            (PhobosAgriculture.Definitions.Meal, "AnyFood"),
            (PhobosAgriculture.Definitions.Raw, "AnyFood"),
            (PhobosAgriculture.Definitions.Leaves, "AnyFood") })
        {
            check(DataHandler.dictDataCoCollections[pair.Item2].IsPartOfCollection(pair.Item1), "Native market recognizes " + pair.Item1);
            var market = new ShipMarket("RegionalEconomyTest");
            market.PriceModifiers[pair.Item2] = 1.4f;
            check(Math.Abs(market.GetPriceModifierForItem(pair.Item1) - 1.4) < .00001, "Native demand factor reaches " + pair.Item1);
            market.PriceModifiers[pair.Item2] = .6f;
            check(Math.Abs(market.GetPriceModifierForItem(pair.Item1) - .6) < .00001, "Native surplus factor reaches " + pair.Item1);
        }
        check(!DataHandler.dictDataCoCollections["AnyWater"].IsPartOfCollection(PhobosShipbreaker.FurnaceService.CoolantStock), "Coolant is not bulk potable water");
        check(!DataHandler.dictDataCoCollections["AnyWater"].IsPartOfCollection(PhobosAgriculture.Definitions.Irrigation), "Root-water packages retain industrial classification");
        check(!DataHandler.dictDataCoCollections["AnyIndustrialProducts"].IsPartOfCollection(PhobosShipbreaker.Content.Prefix + "Installed"), "Installed machinery remains outside native bulk trade");
        check(!DataHandler.dictDataCoCollections["AnyIndustrialProducts"].IsPartOfCollection(PhobosShipbreaker.Content.Loose + "Dmg"), "Broken machinery retains native category exclusion");
        throws(() => RegionalMarkets.SupplyTable("Neptune"), "Unimplemented markets are not fabricated");
        string endpoint = RegionalMarkets.SupplyTable("HQCH");
        var retained = DataHandler.dictLoot[endpoint];
        DataHandler.dictLoot.Remove(endpoint);
        try {
            var missing = new NativeDefinitions();
            RegionalMarkets.Add(missing, "HQCH", "PhobosTest", .2, StockCondition.Pristine);
            check(missing.Loot.Count == 0, "Missing optional retail endpoint does not fabricate stock or abort registration");
        } finally { DataHandler.dictLoot[endpoint] = retained; }
    }
}
