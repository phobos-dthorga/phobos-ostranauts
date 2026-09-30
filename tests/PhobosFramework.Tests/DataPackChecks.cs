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
            pack.lots["equipment"] = 8; pack.lots["supplies"] = 128; pack.chanceFloors["equipment"] = .85; pack.chanceFloors["supplies"] = .95;
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
        Bad(p => p.equipment["PhobosThing"].lot = "crates", "Lots must be named in the lots table");
        Bad(p => p.equipment["PhobosThing"].forms = "sheet", "Forms must be machine, item or single");
        Bad(p => p.equipment["PhobosThing"].offerScale = 0, "Offer scale must be positive");
        Bad(p => p.worldLoot[0].items = new Dictionary<string, double> { ["PhobosThing"] = .8, ["PhobosOther"] = .3 }, "Explicit loot items add up to 1 at most");
        // Sections need only a price, dismantle work and salvage; a remainder lets salvage weigh less, never more.
        var wide = new EconomyContext(new[] { "PhobosThing", "PhobosPart" }, new[] { "PhobosPipe" }) { MassOf = p => p == "PhobosThing" || p == "PhobosPart" ? 10 : null, MaterialMassOf = context.MaterialMassOf };
        var withSection = Good();
        withSection.equipment["PhobosPart"] = new EquipmentEconomyEntry { kind = "section", forms = "single", price = 10, work = new WorkEntry { dismantle = 1 }, salvage = { ["ItmScrapSteel"] = 10 }, offers = false, lot = "supplies" };
        EconomySchema.Validate(withSection, wide); check(true, "A section validates with a price, dismantle work and salvage");
        withSection.equipment["PhobosPart"].salvage["ItmScrapSteel"] = 9; throws(() => EconomySchema.Validate(withSection, wide), "Section salvage must weigh what the section weighs");
        var remainder = Good(); remainder.equipment["PhobosThing"].salvageRemainder = true; remainder.equipment["PhobosThing"].salvage["ItmScrapSteel"] = 2;
        EconomySchema.Validate(remainder, context); check(true, "With a remainder the salvage may weigh less than the machine");
        remainder.equipment["PhobosThing"].salvage["ItmScrapSteel"] = 20; throws(() => EconomySchema.Validate(remainder, context), "With a remainder the salvage may not weigh more than the machine");
        // Stock classification: machines by prefix, supplies by prefix, regional items exactly, each to its lot and floor.
        var stock = Good(); stock.regional!.items["PhobosIngot"] = new RegionalItemEntry { chance = .5, lot = "supplies" };
        check(EconomyStock.Quantity(stock, "PhobosThingLoose") == 8 && EconomyStock.Quantity(stock, "PhobosThingMediumLooseDmg") == 8 && EconomyStock.Quantity(stock, "PhobosPipeLoose") == 128
            && EconomyStock.Quantity(stock, "PhobosIngot") == 128 && EconomyStock.Chance(stock, "PhobosPipeLoose", 0) == .95 && EconomyStock.Chance(stock, "PhobosThingLoose", .9) == .9 && EconomyStock.Chance(stock, "PhobosThingLoose", 0) == .85,
            "Items resolve to their lot and floor");
        var sales = new[] { EquipmentSale.Of("PhobosThing", stock.equipment["PhobosThing"]), EquipmentSale.Size("PhobosThingMedium", stock.equipment["PhobosThing"]) };
        var spread = EconomyStock.LootChances(stock.worldLoot[0], sales);
        check(spread.Count == 2 && Math.Abs(spread["PhobosThingLooseDmg"] - .05 * .75) < 1e-12 && Math.Abs(spread["PhobosThingLoose"] - .05 * .25) < 1e-12, "World loot spreads over the small sizes only, the broken share to the damaged form");

        // The recipe schema: mass conservation and the game's gases on every file.
        var recipeContext = new RecipeContext { Machines = new[] { "kiln" }, Requirements = new[] { "other-mod" }, UnitMassOf = id => id == "ItmOre" ? 10 : null, IsCommodity = id => id == "water", ReferenceK = 298.15 };
        RecipePack Recipes()
        {
            var pack = new RecipePack { schemaVersion = 1, schema = "process-recipes" };
            pack.recipes["bake"] = new RecipeEntry
            {
                machine = "kiln", revision = 1, seconds = 600,
                inputs = { new RecipeUnit { id = "ItmOre", count = 1, kg = 10 } },
                products = { new RecipeUnit { id = "water", count = 1, kg = 1 }, new RecipeUnit { id = "ItmMiningTrash", count = 3, kg = 3 } }
            };
            return pack;
        }
        RecipeSchema.Validate(Recipes(), recipeContext);
        check(true, "A balanced recipe validates");
        void BadRecipe(Action<RecipePack> mutate, string message) { var pack = Recipes(); mutate(pack); throws(() => RecipeSchema.Validate(pack, recipeContext), message); }
        BadRecipe(p => p.recipes["bake"].products[0].kg = 2, "A recipe that creates mass is refused");
        BadRecipe(p => p.recipes["bake"].offGas["H2"] = .1, "Hydrogen is not a room gas and is refused as off-gas");
        BadRecipe(p => { p.recipes["bake"].offGas["CO2"] = 1; p.recipes["bake"].products[0].kg = 0; }, "Off-gas counts toward the balance and units need positive mass");
        BadRecipe(p => p.recipes["bake"].inputs[0].kg = 9, "A unit's kg must match the known item mass");
        BadRecipe(p => p.recipes["bake"].machine = "oven", "Unknown machines are refused");
        BadRecipe(p => p.recipes["bake"].requires.Add("nothing"), "Unknown requirement keys are refused");
        BadRecipe(p => p.recipes["bake"].seconds = 0, "Seconds stay within the process job bounds");
        BadRecipe(p => p.recipes["Bake!"] = p.recipes["bake"], "Recipe ids are letters, digits and hyphens");
        BadRecipe(p => { var again = Recipes().recipes["bake"]; p.recipes["bake2"] = again; }, "One revision per machine");
        BadRecipe(p => p.recipes["bake"].thermal = new ThermalEntry { meltK = 900, targetK = 800, solidCp = 1, liquidCp = 1, latentKJ = 1, holdSeconds = 1 }, "A thermal target must be above the melt");
        // A circulating working volume is a commodity outside the mass balance; reaction heat is bounded.
        var circulating = Recipes(); circulating.recipes["bake"].circulates["water"] = 20; circulating.recipes["bake"].reactionKWh = -3;
        RecipeSchema.Validate(circulating, recipeContext); check(true, "A circulating commodity and an absorbed reaction heat validate without touching the balance");
        BadRecipe(p => p.recipes["bake"].circulates["ItmOre"] = 1, "Only a commodity can circulate");
        BadRecipe(p => p.recipes["bake"].circulates["water"] = 0, "A circulating volume is above zero");
        BadRecipe(p => p.recipes["bake"].reactionKWh = 5000, "Reaction heat is bounded");

        // The freeze: the same canonical text and digest as scripts/freeze-recipes.py (tests/test_data_packs.py holds the twin).
        var sample = Newtonsoft.Json.Linq.JObject.Parse("{\"notes\":\"x\",\"machine\":\"test\",\"revision\":1,\"inputs\":[{\"id\":\"A\",\"count\":2,\"kg\":1.5}],\"products\":[{\"id\":\"B\",\"count\":1,\"kg\":3}],\"melt\":false}");
        check(RecipeFreeze.Canonical(sample) == "{\"inputs\":[{\"count\":2,\"id\":\"A\",\"kg\":1.5}],\"machine\":\"test\",\"melt\":false,\"products\":[{\"count\":1,\"id\":\"B\",\"kg\":3}],\"revision\":1}",
            "Canonical form drops notes, sorts keys and strips whitespace: " + RecipeFreeze.Canonical(sample));
        check(RecipeFreeze.Hash(sample) == "49f8679fceadf55956c05d1013dad41f13c41b907a56bf53add8faee176b5381", "The digest agrees with the Python freezer: " + RecipeFreeze.Hash(sample));
        var raw = Newtonsoft.Json.Linq.JObject.Parse("{\"schemaVersion\":1,\"schema\":\"process-recipes\",\"recipes\":{\"one\":{\"machine\":\"test\",\"revision\":1,\"inputs\":[{\"id\":\"A\",\"count\":2,\"kg\":1.5}],\"products\":[{\"id\":\"B\",\"count\":1,\"kg\":3}],\"melt\":false,\"notes\":\"changed notes do not matter\"}}}");
        var frozen = RecipeFreeze.Parse("{\"schemaVersion\":1,\"revisions\":{\"test@1\":\"49f8679fceadf55956c05d1013dad41f13c41b907a56bf53add8faee176b5381\"}}");
        RecipeFreeze.Enforce(raw, frozen);
        check(true, "A frozen revision with the same content passes, whatever its notes say");
        var edited = (Newtonsoft.Json.Linq.JObject)raw.DeepClone(); edited["recipes"]!["one"]!["products"]![0]!["kg"] = 2.5; edited["recipes"]!["one"]!["inputs"]![0]!["kg"] = 1.25;
        throws(() => RecipeFreeze.Enforce(edited, frozen), "A changed frozen revision is refused even when it still balances");
        var removed = (Newtonsoft.Json.Linq.JObject)raw.DeepClone(); ((Newtonsoft.Json.Linq.JObject)removed["recipes"]!).Remove("one");
        throws(() => RecipeFreeze.Enforce(removed, frozen), "A frozen revision cannot disappear");
        var added = (Newtonsoft.Json.Linq.JObject)raw.DeepClone(); added["recipes"]!["two"] = Newtonsoft.Json.Linq.JObject.Parse("{\"machine\":\"test\",\"revision\":2,\"inputs\":[],\"products\":[]}");
        RecipeFreeze.Enforce(added, frozen);
        check(true, "A new revision beside the frozen ones passes the freeze (the schema judges its content)");

        // The materials schema.
        var materialContext = new MaterialContext(new[] { "PhobosThing" }) { Kinds = new[] { "stock" } };
        MaterialPack Materials() { var pack = new MaterialPack { schemaVersion = 1, schema = "materials" }; pack.materials["PhobosThing"] = new MaterialEntry { kg = 1, price = 2, stack = 10, side = 1 }; return pack; }
        MaterialSchema.Validate(Materials(), materialContext);
        check(true, "A complete materials pack validates");
        void BadMaterial(Action<MaterialPack> mutate, string message) { var pack = Materials(); mutate(pack); throws(() => MaterialSchema.Validate(pack, materialContext), message); }
        BadMaterial(p => p.materials.Remove("PhobosThing"), "Every known material needs an entry");
        BadMaterial(p => p.materials["PhobosOther"] = new MaterialEntry { kg = 1, price = 1 }, "Materials cannot be added by a file");
        BadMaterial(p => p.materials["PhobosThing"].kind = "gas", "Unknown material kinds are refused");
        BadMaterial(p => p.materials["PhobosThing"].kg = 0, "Mass must be positive");
        BadMaterial(p => p.materials["PhobosThing"].stack = 0, "Stack is 1 or more");

        // The vessels schema: every known family has an entry, vessels need a commodity and a capacity, bins need cells instead.
        var vesselContext = new VesselContext(new[] { "PhobosTank", "PhobosBin" }) { Kinds = new[] { "silo", "bin" } };
        VesselPack Vessels()
        {
            var pack = new VesselPack { schemaVersion = 1, schema = "vessels" };
            pack.families["PhobosTank"] = new VesselFamilyEntry { kind = "silo", commodity = "water", capacityKg = 1000, dryKg = 240 };
            pack.families["PhobosBin"] = new VesselFamilyEntry { kind = "bin", dryKg = 60, cellsPerTileSide = 2 };
            return pack;
        }
        VesselSchema.Validate(Vessels(), vesselContext);
        check(true, "A complete vessels pack validates");
        void BadVessel(Action<VesselPack> mutate, string message) { var pack = Vessels(); mutate(pack); throws(() => VesselSchema.Validate(pack, vesselContext), message); }
        BadVessel(p => p.families.Remove("PhobosBin"), "Every known vessel family needs an entry");
        BadVessel(p => p.families["PhobosOther"] = new VesselFamilyEntry { kind = "silo", commodity = "water", capacityKg = 1, dryKg = 1 }, "Vessels cannot be added by a file");
        BadVessel(p => p.families["PhobosTank"].kind = "reservoir", "Unknown vessel kinds are refused");
        BadVessel(p => p.families["PhobosTank"].capacityKg = 0, "A vessel needs a positive capacity");
        BadVessel(p => p.families["PhobosTank"].commodity = null, "A vessel needs its commodity");
        BadVessel(p => p.families["PhobosTank"].cellsPerTileSide = 2, "Cells belong to bins only");
        BadVessel(p => p.families["PhobosBin"].capacityKg = 5, "A bin has no capacity");
        BadVessel(p => p.families["PhobosBin"].cellsPerTileSide = 0, "A bin needs at least one cell per tile side");
        BadVessel(p => p.families["PhobosBin"].dryKg = 0, "Dry mass must be positive");
        BadVessel(p => p.families["PhobosTank"].leakKgPerHour = -1, "Leak rate cannot be negative");

        // The equipment schema: shapes in range, known machines only, and a read-only baseline.
        EquipmentPack Shapes()
        {
            var pack = new EquipmentPack { schemaVersion = 1, schema = "equipment" };
            pack.equipment["PhobosMill"] = new EquipmentEntry { kind = "charge-machine", footprint = 3, massKg = 200, idleKW = 0.1, workingKW = 12, roomHeatFraction = 0.15, feedCells = 4,
                points = { ["use"] = new[] { 0, -32 }, ["PowerA"] = new[] { 0, 16 } } };
            return pack;
        }
        var shapeContext = new EquipmentContext(new[] { "PhobosMill" }) { Kinds = new[] { "charge-machine" }, InstallTabs = new[] { "APPS" }, Baseline = Shapes() };
        EquipmentSchema.Validate(Shapes(), shapeContext); check(true, "An unchanged equipment pack validates against its baseline");
        check(EquipmentSchema.MapPoints(Shapes().equipment["PhobosMill"]).SequenceEqual(new[] { "use,0,-32", "PowerA,0,16" }), "Map points keep the file's order and the game's name,x,y form");
        void BadShape(Action<EquipmentPack> mutate, string message) { var pack = Shapes(); mutate(pack); throws(() => EquipmentSchema.Validate(pack, shapeContext), message); }
        BadShape(p => p.equipment["PhobosMill"].massKg = 250, "A changed machine shape is refused while the pack is read-only");
        BadShape(p => p.equipment.Remove("PhobosMill"), "Every known machine needs an entry");
        BadShape(p => p.equipment["PhobosOther"] = Shapes().equipment["PhobosMill"], "Machines cannot be added in a file");
        var free = new EquipmentContext(new[] { "PhobosMill" }) { Kinds = new[] { "charge-machine" }, InstallTabs = new[] { "APPS" } };
        void BadFree(Action<EquipmentPack> mutate, string message) { var pack = Shapes(); mutate(pack); throws(() => EquipmentSchema.Validate(pack, free), message); }
        BadFree(p => p.equipment["PhobosMill"].footprint = 0, "A footprint is at least one tile");
        BadFree(p => p.equipment["PhobosMill"].idleKW = 20, "Idle power cannot exceed working power");
        BadFree(p => p.equipment["PhobosMill"].roomHeatFraction = 1.5, "The room-heat share is a fraction");
        BadFree(p => p.equipment["PhobosMill"].installTab = "NOPE", "Unknown INSTALL tabs are refused");
        BadFree(p => p.equipment["PhobosMill"].points["use"] = new[] { 0 }, "A point needs two offsets");
    }
}
