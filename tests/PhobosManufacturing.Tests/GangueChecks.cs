using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using PhobosManufacturing.Core;

/// <summary>The gangue wash (Manufacturing 0.44.0): four outcomes of one charge, chosen through the outcomes pack by the
/// lumps' own identities; every outcome balances; outcome recipes are never offered by themselves.</summary>
internal static class GangueChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        var leach = ChargeCatalog.For(ChargeCatalog.Leach);
        var wash = leach.ById("gangue-wash")!;
        var table = ChargeOutcomes.Pack.tables["gangue-wash"].outcomes;
        check(table.Count == 4 && table.Values.Sum() == 100 && table["gangue-wash"] == 50 && table["gangue-wash-steel"] == 30 && table["gangue-wash-aluminium"] == 15 && table["gangue-wash-nickel-iron"] == 5,
            "The shipped gangue table is lean: 50 tailings only, 30 steel, 15 aluminium, 5 nickel-iron in a hundred");
        foreach (string id in table.Keys)
        {
            var r = leach.ById(id)!;
            check(Math.Abs(r.ChargeKg - 13) < 1e-9 && Math.Abs(r.Products.Sum(p => p.Kg * p.Count) - 13) < 1e-9 && r.Units == 4 && r.Seconds == 1200 &&
                  r.Draws.Single().Id == LiquidStores.SulfuricAcid && r.Circulates[ManufacturingRules.Water] == 10,
                "Every outcome is four lumps and a kilogram of acid in, 13 kg out, in 20 minutes: " + id);
            check(r.Products.Count(p => p.Id == Materials.WashedTailings) == 1 && ChargeOutcomes.BaseOf(id) == "gangue-wash", "Every outcome leaves one stack of tailings and shows as the gangue wash: " + id);
        }
        check(Materials.IsTerminal(Materials.WashedTailings) && Materials.ById(Materials.WashedTailings)!.Stack >= 13, "Washed tailings are a terminal remainder that stacks a whole wash in one cell");
        var offered = leach.Available(_ => true).Select(r => r.Id).ToArray();
        check(offered.Contains("gangue-wash") && !offered.Any(id => id.StartsWith("gangue-wash-", StringComparison.Ordinal)) &&
              !ChargeOutcomes.IsHidden("gangue-wash") && ChargeOutcomes.IsHidden("gangue-wash-steel") && !ChargeOutcomes.IsHidden("olivine-epsom"),
            "Only the gangue wash itself is offered; its outcomes are never chosen directly");
        var lumps = new[] { "lump-4", "lump-1", "lump-3", "lump-2" };
        var result = ChargeOutcomes.Resolve(wash, lumps);
        check(result.Id == ChargeOutcomes.Resolve(wash, lumps.Reverse()).Id && table.ContainsKey(result.Id), "The same four lumps always wash out the same, in any order");
        check(ReferenceEquals(ChargeOutcomes.Resolve(leach.ById("olivine-epsom")!, lumps), leach.ById("olivine-epsom")), "A recipe with no table is itself");
        var seen = Enumerable.Range(0, 400).Select(i => ChargeOutcomes.Resolve(wash, Enumerable.Range(0, 4).Select(n => "set" + i + "-" + n)).Id).Distinct().Count();
        check(seen == 4, "Different lumps reach every outcome");
        // Worth: gangue 2 cr a lump and acid at the game's own price; the expected result is modest, the rare ingot is the recorded exception.
        double acid = 3.1, input = 4 * 2 + acid, steel = 3.6, aluminium = 1.1, ingot = Materials.IngotPrice;
        double expected = (30 * 2 * steel + 15 * 2 * aluminium + 5 * ingot) / 100;
        check(expected < 1.5 * input && expected > 0.5 * input, $"A wash is expected to return about its cost, not a windfall: {expected:F1} cr from {input:F1} cr");
        // A player table that names a recipe with another charge is refused.
        var facts = ChargeOutcomes.Facts(ChargeCatalog.Pack);
        var bad = new OutcomePack(); bad.tables["gangue-wash"] = new OutcomeTable { outcomes = { ["gangue-wash"] = 1, ["olivine-epsom"] = 1 } };
        throws(() => OutcomeSchema.Validate(bad, id => facts.TryGetValue(id, out var f) ? f : null), "An outcome with a different charge is refused");
    }
}

/// <summary>The regolith leach (Manufacturing 0.50.0; owner request, 5 October 2026): one lump of the game's loose
/// regolith, acid and water into Epsom salt and tailings, with an outcome table held to the bought-stock rule because
/// a merchant sells regolith.</summary>
internal static class RegolithLeachChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var leach = ChargeCatalog.For(ChargeCatalog.Leach);
        var baseRecipe = leach.ById("regolith-leach")!;
        var table = ChargeOutcomes.Pack.tables["regolith-leach"].outcomes;
        check(table.Count == 4 && table.Values.Sum() == 100 && table["regolith-leach"] == 72 && table["regolith-leach-steel"] == 22 && table["regolith-leach-silicates"] == 4 && table["regolith-leach-nickel-iron"] == 2,
            "The shipped regolith table: 72 salt and tailings only, 22 scrap steel, 4 a Silicates chunk, 2 nickel-iron in a hundred");
        foreach (string id in table.Keys)
        {
            var r = leach.ById(id)!;
            check(Math.Abs(r.ChargeKg - 22.728) < 1e-9 && Math.Abs(r.Products.Sum(p => p.Kg * p.Count) - 22.728) < 1e-9 && r.Units == 1 && r.Seconds == 1800 &&
                  r.ItemInputs.Single().Id == LeachRules.Regolith && r.ItemInputs.Single().Kg == 20 && r.Draws.Count == 2 && r.Circulates.Count == 0,
                "Every outcome is one 20 kg lump, 1.080 kg of acid and 1.648 kg of water in, 22.728 kg out, in 30 minutes: " + id);
            check(r.Products.Single(p => p.Id == Materials.EpsomSalt).Count == 4 && r.Products.Count(p => p.Id == Materials.WashedTailings) == 1 &&
                  r.Products.Single(p => p.Id == Materials.WashedTailings).Count <= Materials.ById(Materials.WashedTailings)!.Stack && ChargeOutcomes.BaseOf(id) == "regolith-leach",
                "Every outcome gives four Epsom salt and one stack of tailings, and shows as the regolith leach: " + id);
        }
        // The Epsom salt is the olivine recipe at one eighth: the same acid and salt per mole.
        var olivine = leach.ById("olivine-epsom")!;
        double acid = baseRecipe.Draws.Single(d => d.Id == LiquidStores.SulfuricAcid).Kg, olivineAcid = olivine.Draws.Single(d => d.Id == LiquidStores.SulfuricAcid).Kg;
        check(Math.Abs(acid - olivineAcid / 8) < 0.001 && olivine.Products.Single(p => p.Id == Materials.EpsomSalt).Count == 8 * 4, "The salt and acid are the olivine recipe's, at one eighth");
        var offered = leach.Available(_ => true).Select(r => r.Id).ToArray();
        check(offered.Contains("regolith-leach") && !offered.Any(id => id.StartsWith("regolith-leach-", StringComparison.Ordinal)), "Only the leach itself is offered; its outcomes are never chosen directly");
        check(LeachRules.FeedIds.Contains(LeachRules.Regolith) && LeachRecipes.FeedKg(LeachRules.Regolith, baseRecipe, false) == 20 && LeachRecipes.FeedKg(LeachRules.Gangue, baseRecipe, false) == null,
            "With the leach selected the LC-3 takes a 20 kg lump and nothing else");
        var seen = Enumerable.Range(0, 2000).Select(i => ChargeOutcomes.Resolve(baseRecipe, new[] { "lump" + i }).Id).Distinct().Count();
        check(seen == 4 && ChargeOutcomes.Resolve(baseRecipe, new[] { "one" }).Id == ChargeOutcomes.Resolve(baseRecipe, new[] { "one" }).Id, "Different lumps reach every outcome, and one lump always leaches the same");
        // Worth at base prices. A prospector sells regolith, so the table is held to the bought-stock rule: at most 1.25 x.
        double lump = 35, acidPrice = 3.1, water = 10, salt = Materials.ById(Materials.EpsomSalt)!.Price, steel = 3.6, silicates = 200, ingot = Materials.IngotPrice;
        double cost = lump + acid * acidPrice + 1.648 * water;
        double expected = 4 * salt + (22 * 3 * steel + 4 * silicates + 2 * ingot) / 100;
        check(expected <= 1.25 * cost && expected >= cost, $"A leach is expected to return a little over its cost, within the bought-stock rule: {expected:F1} cr from {cost:F1} cr");
    }
}
