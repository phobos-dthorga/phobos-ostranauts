using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Data;
using PhobosExchange;
using PhobosExchange.Core;

/// <summary>Phobos Exchange 0.1.0 and Framework 0.128.0 against the installed game: the market members the exchange reads,
/// the ledger call it settles through, the game's debug switch the test-command gate reads, and every driver in the
/// embedded company list against the game's own market data (the station has a cargo market, prices the category and
/// makes or uses it, so no driver is dead). Nothing here runs a game session.</summary>
internal static class ExchangeNativeChecks
{
    internal static void Run(string game, Action<bool, string> check)
    {
        const BindingFlags Public = BindingFlags.Public | BindingFlags.Static;
        check(typeof(MarketManager).GetMethod(nameof(MarketManager.GetShipMarket), Public, null, new[] { typeof(string) }, null)?.ReturnType == typeof(ShipMarket),
            "The game's MarketManager.GetShipMarket(string) returns a ShipMarket");
        check(typeof(MarketManager).GetMethod(nameof(MarketManager.GetStatusForShip), Public, null, new[] { typeof(string) }, null)?.ReturnType == typeof(string),
            "The game's MarketManager.GetStatusForShip(string) gives a station's market description");
        check(typeof(ShipMarket).GetProperty(nameof(ShipMarket.PriceModifiers))?.PropertyType == typeof(Dictionary<string, float>),
            "The game's ShipMarket.PriceModifiers is the category-to-factor table the drivers read");
        check(typeof(DataHandler).GetMethod(nameof(DataHandler.GetDataCoCollection), Public, null, new[] { typeof(string) }, null) != null &&
              typeof(DataCoCollection).GetProperty(nameof(DataCoCollection.FriendlyName))?.PropertyType == typeof(string), "Categories and their friendly names are readable");
        check(typeof(Ledger).GetMethod(nameof(Ledger.RecordTransaction), Public, null,
            new[] { typeof(CondOwner), typeof(string), typeof(double), typeof(string), typeof(string), typeof(LedgerLI) }, null) != null, "Trades settle through the game's Ledger.RecordTransaction");
        check(typeof(CrewSim).GetField(nameof(CrewSim.bEnableDebugCommands), Public)?.FieldType == typeof(bool), "The test-command gate reads the game's own unlockdebug switch");
        check(typeof(Ship).GetField(nameof(Ship.publicName))?.FieldType == typeof(string), "Station names come from the game's Ship.publicName");

        var pack = DataPacks.LoadText<ExchangePack>(DataPacks.ShippedText(Companies.Source), "", ExchangeRules.Owner, ExchangeSchema.Name, ExchangeSchema.Validate);
        check(pack.companies.Count > 0, "The embedded company list loads");
        string market = Path.Combine(game, "Ostranauts_Data", "StreamingAssets", "data", "market");
        var collections = JArray.Parse(File.ReadAllText(Path.Combine(market, "CoCollections", "cocollections.json"))).Select(c => (string)c["strName"]!).ToHashSet(StringComparer.Ordinal);
        var actors = JArray.Parse(File.ReadAllText(Path.Combine(market, "Markets", "market_actor_configs.json"))).ToDictionary(a => (string)a["strName"]!, a => a, StringComparer.Ordinal);
        var maps = JArray.Parse(File.ReadAllText(Path.Combine(market, "Production", "production_maps.json"))).ToDictionary(p => (string)p["strName"]!, p => p, StringComparer.Ordinal);
        foreach (var pair in pack.companies)
            foreach (var d in pair.Value.drivers)
            {
                string where = pair.Key + " " + d.station + " " + d.category;
                check(collections.Contains(d.category), "Every driver's category is one of the game's: " + where);
                check(actors.TryGetValue(d.station + "CargoKiosk", out var actor), "Every driver's station has a cargo market: " + where);
                if (actor == null) continue;
                var priced = (actor["aVirtualInventorySize"] as JArray ?? new JArray()).Select(x => ((string)x!).Split('=')[0]).ToHashSet(StringComparer.Ordinal);
                check(priced.Contains(d.category), "The station's market prices the driver's category: " + where);
                bool traded = false;
                foreach (var name in (actor["aSupplyDemandMaps"] as JArray ?? new JArray()).Select(x => (string)x!))
                {
                    if (!maps.TryGetValue(name, out var map)) continue;
                    if ((string?)map["strOutputCollection"] == d.category) traded = true;
                    if ((map["aInputCollections"] as JArray ?? new JArray()).Any(x => ((string)x!).Split('=')[0] == d.category)) traded = true;
                }
                check(traded, "The station makes or uses the driver's category, so its factor moves: " + where);
            }
    }
}
