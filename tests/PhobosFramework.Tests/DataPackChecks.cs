using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

/// <summary>The data-pack loader on its own: strict parsing, merge by name, refusal of nulls and unknown fields,
/// per-file rejection that keeps earlier files, and the economy schema's checks.</summary>
internal static class DataPackChecks
{
    private sealed class TestPack : DataPack
    {
        public Dictionary<string, Row> rows = new(StringComparer.Ordinal);
        public List<int> order = new();
    }
    private sealed class Row { public double price = 0; public int count = 1; public string? notes = null; }

    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        string shipped = "{\"schemaVersion\":1,\"schema\":\"test\",\"rows\":{\"a\":{\"price\":10,\"count\":2},\"b\":{\"price\":20}},\"order\":[1,2,3]}";
        string folder = Path.Combine(Path.GetTempPath(), "phobos-datapack-checks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            void Validate(TestPack p) { foreach (var r in p.rows) if (r.Value.price <= 0) throw new ArgumentException("price " + r.Key); }
            var plain = DataPacks.LoadText<TestPack>(shipped, folder, "test.owner", "test", Validate);
            check(plain.rows.Count == 2 && plain.rows["a"].count == 2 && plain.order.SequenceEqual(new[] { 1, 2, 3 }), "The shipped pack loads with no player files");
            // A partial player file tunes one field and adds an entry; arrays replace; a later file builds on the earlier one.
            File.WriteAllText(Path.Combine(folder, "10-tune.json"), "{\"rows\":{\"a\":{\"price\":15}},\"order\":[9]}");
            File.WriteAllText(Path.Combine(folder, "20-add.json"), "{\"schema\":\"test\",\"rows\":{\"c\":{\"price\":30,\"notes\":\"added\"}}}");
            var merged = DataPacks.LoadText<TestPack>(shipped, folder, "test.owner", "test", Validate);
            check(merged.rows["a"].price == 15 && merged.rows["a"].count == 2 && merged.rows["b"].price == 20 && merged.rows["c"].price == 30 && merged.order.SequenceEqual(new[] { 9 }),
                "Player files merge by name: the tuned field changes, untouched fields stay, new entries appear, arrays are replaced");
            // Bad files are reported and skipped, and the files before and after them still apply.
            File.WriteAllText(Path.Combine(folder, "15-null.json"), "{\"rows\":{\"b\":null}}");
            File.WriteAllText(Path.Combine(folder, "16-unknown.json"), "{\"rows\":{\"a\":{\"prize\":1}}}");
            File.WriteAllText(Path.Combine(folder, "17-invalid.json"), "{\"rows\":{\"a\":{\"price\":-1}}}");
            File.WriteAllText(Path.Combine(folder, "18-wrong-schema.json"), "{\"schema\":\"other\",\"rows\":{}}");
            File.WriteAllText(Path.Combine(folder, "19-broken.json"), "{\"rows\":");
            File.WriteAllText(Path.Combine(folder, "19-duplicate.json"), "{\"rows\":{\"a\":{\"price\":1}},\"rows\":{}}");
            DataPacks.Reset();
            var survived = DataPacks.LoadText<TestPack>(shipped, folder, "test.owner", "test", Validate);
            check(survived.rows["a"].price == 15 && survived.rows.ContainsKey("b") && survived.rows["c"].price == 30, "Rejected files change nothing; accepted files before and after them still apply");
            check(DataPacks.Problems.Count == 6 && DataPacks.Problems.All(p => p.Owner == "test.owner" && p.Schema == "test"), "Every rejected file is reported once: " + DataPacks.Problems.Count);
            check(DataPacks.Describe().Contains("2 player file(s) applied, 6 skipped"), "The console status counts applied and skipped files");
            // The shipped pack itself must be sound: a failure is a packaging fault.
            throws(() => DataPacks.LoadText<TestPack>("{\"schema\":\"test\",\"rows\":{}}", "", "o", "test", _ => { }), "A shipped pack without schemaVersion is refused");
            throws(() => DataPacks.LoadText<TestPack>("{\"schemaVersion\":2,\"schema\":\"test\",\"rows\":{}}", "", "o", "test", _ => { }), "A future schemaVersion is refused");
            throws(() => DataPacks.LoadText<TestPack>("{\"schemaVersion\":1,\"schema\":\"test\",\"rows\":{},\"extra\":1}", "", "o", "test", _ => { }), "Unknown top-level fields are refused");
            throws(() => DataPacks.LoadText<TestPack>(shipped + " trailing", "", "o", "test", _ => { }), "Trailing content is refused");
            throws(() => DataPacks.LoadText<TestPack>(shipped, "", "o", "test", p => throw new ArgumentException("no")), "A shipped pack failing its validator is refused");
        }
        finally { try { Directory.Delete(folder, true); } catch (IOException) { } }

        // The economy schema on numbers alone.
        var context = new EconomyContext(new[] { "PhobosThing" }, new[] { "PhobosPipe" })
        {
            MassOf = p => p == "PhobosThing" ? 10 : null,
            MaterialMassOf = id => id switch { "ItmScrapSteel" => 1, "ItmScrapTrash" => 1, "ItmPartsMechSmall01" => .5, _ => null }
        };
        EconomyPack Good()
        {
            var pack = new EconomyPack { schemaVersion = 1, schema = "economy" };
            pack.equipment["PhobosThing"] = new EquipmentEconomyEntry
            {
                price = 100, work = new WorkEntry { install = 1, uninstall = 1, repair = 1, dismantle = 1 }, restoreMinutes = 5,
                repairBill = { ["ItmScrapSteel"] = 1 }, salvage = { ["ItmScrapSteel"] = 8, ["ItmPartsMechSmall01"] = 2, ["ItmScrapTrash"] = 1 }, brokenSalvage = { ["ItmScrapSteel"] = 5, ["ItmScrapTrash"] = 5 }
            };
            pack.supplies["PhobosPipe"] = new SupplyEconomyEntry { price = 1, repairWork = 1, dismantleWork = 1, repairBill = { ["ItmScrapSteel"] = 1 }, merchants = { "ItmSomeKiosk" }, chance = 0 };
            pack.offerTemplates.Add(new OfferTemplate { merchant = "ItmSomeKiosk", tag = "Scrap", form = "LooseDmg", condition = "Broken", chance = .2 });
            pack.regions["OKLG"] = 1; pack.regional = new RegionalStock { baseChance = .2, refurbished = { "OFLT" } };
            pack.lots["equipment"] = 8; pack.chanceFloors["supplies"] = .95;
            pack.worldLoot.Add(new WorldLootEntry { table = "ItmLootSpawnEngineering", branch = "PhobosSalvage", chance = .05, brokenShare = .75 });
            return pack;
        }
        EconomySchema.Validate(Good(), context);
        check(true, "A complete economy pack validates");
        void Bad(Action<EconomyPack> mutate, string message) { var pack = Good(); mutate(pack); throws(() => EconomySchema.Validate(pack, context), message); }
        Bad(p => p.equipment.Remove("PhobosThing"), "Every known family needs an entry");
        Bad(p => p.equipment["PhobosOther"] = p.equipment["PhobosThing"], "Equipment cannot be added by a file");
        Bad(p => p.equipment["PhobosThing"].salvage["ItmScrapSteel"] = 9, "Salvage must weigh what the machine weighs");
        Bad(p => p.equipment["PhobosThing"].brokenSalvage["ItmScrapSteel"] = 4, "Broken salvage must weigh what the machine weighs");
        Bad(p => p.equipment["PhobosThing"].repairBill["ItmNothing"] = 1, "Unknown materials are refused");
        Bad(p => p.equipment["PhobosThing"].brokenPrice = 100, "Broken price must be below the price");
        Bad(p => p.equipment["PhobosThing"].work.repair = 0, "Work must be positive");
        Bad(p => p.offerTemplates[0].form = "Installed", "Offer form must be Loose or LooseDmg");
        Bad(p => p.offerTemplates[0].condition = "Shiny", "Offer condition must be a stock condition");
        Bad(p => p.offerTemplates[0].chance = 1.5, "Offer chance must be at most 1");
        Bad(p => p.offers["Nope"] = new OfferEntry { merchant = "m", item = "i", chance = .1 }, "Explicit offer ids must start with Phobos");
        Bad(p => p.regions["ZZZZ"] = 1, "Unknown regions are refused");
        Bad(p => p.regions["OKLG"] = 5, "Region factors are bounded");
        Bad(p => p.lots["equipment"] = 0, "Lots are 1 to 256");
        Bad(p => p.worldLoot[0].brokenShare = 2, "Broken share is a fraction");
        Bad(p => p.supplies["PhobosPipe"].merchants.Clear(), "Supplies need a merchant");
    }
}
