using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

/// <summary>The shared charge machine's pure parts (Manufacturing 0.17.0): the V4's record stays byte-identical, an
/// explicit machine's record carries its selection, the catalog keeps each machine's own revisions, the feed rule is
/// derived from the recipes exactly as the V4's old switch was, and the equipment pack holds the V4's shape.</summary>
internal static class ChargeChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        // The V4 record writes exactly the eight keys every saved V4 carries, and refuses a selection field.
        var keys = new RefineryState().Save().Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        check(keys.SequenceEqual(new[] { "charge", "cycles", "emitted", "progress", "recipe", "revision", "running", "wait" }), "The V4 record keeps its eight keys: " + string.Join(",", keys));
        throws(() => RefineryState.Read(new Dictionary<string, string> { ["selected"] = "2" }), "A V4 record refuses a selection field");
        var chosen = new ChargeState(true) { Selected = 3 };
        var back = ChargeState.Read(chosen.Save(), true);
        check(back.Selected == 3 && chosen.Save().ContainsKey("selected") && back.ExplicitSelection, "An explicit machine saves and restores its selected recipe");
        back.RecipeId = "x"; back.Revision = 3; back.Charge.Add("a"); back.Clear();
        check(back.Selected == 3 && !back.Bound, "Clearing a charge keeps the selection for the next one");
        throws(() => ChargeState.Read(new Dictionary<string, string> { ["selected"] = "-1" }, true), "A negative selection is refused");

        // The feed rule is derived from the recipes and equals the V4's old identity switch, steel gating included.
        var expected = new Dictionary<string, double> { [RefineryRules.Hydrates] = 10, [RefineryRules.Iron] = 20, [RefineryRules.Carbides] = 10,
            [Materials.ClayHydrates] = Materials.ClayKg, [Materials.AmmoniumSaltCrust] = Materials.CrustKg };
        foreach (var pair in expected)
            check(RefineryRules.FeedKg(pair.Key, false) == pair.Value && RefineryRules.FeedKg(pair.Key, true) == pair.Value, "Feed unit mass from the recipes: " + pair.Key);
        check(RefineryRules.FeedKg(Materials.NickelIronIngot, false) == Materials.IngotKg && RefineryRules.FeedKg(Materials.NickelIronIngot, true) == Materials.IngotKg &&
              RefineryRules.FeedKg(Materials.CarbonStock, false) == Materials.CarbonKg && RefineryRules.FeedKg(Materials.CarbonStock, true) == Materials.CarbonKg,
            "Nickel-iron and carbon stock enter the feed with or without Shipbreaker, for nickel steel (Manufacturing 0.26.0)");
        check(RefineryRules.FeedKg(RefineryRules.Gangue, true) == null && RefineryRules.FeedKg("ItmScrapSteel", true) == null && RefineryRules.FeedKg(null, true) == null, "Products and foreign items are not feed");

        // The equipment pack holds the V4's shape exactly as the old constants did.
        check(RefineryRules.Footprint == 4 && RefineryRules.FeedCapacity == 6 && RefineryRules.MachineKg == 180 && RefineryRules.WorkingKW == 24 && RefineryRules.IdleKW == 0.1 &&
              RefineryRules.RoomHeatFraction == 0.15, "The V4's shape comes from the equipment pack unchanged");
        var shape = Equipment.Entry(RefineryRules.Prefix);
        check(shape.installTab == "APPS" && shape.art == "PhobosVolatilesRefinery" &&
              Phobos.Ostranauts.Framework.Data.EquipmentSchema.MapPoints(shape).SequenceEqual(new[] { "use,0,-40", "PowerA,-24,24", "PowerB,24,24" }), "The V4's art, tab and points are unchanged");

        // The catalog keeps each machine's own revisions and the machines' load rules.
        var view = ChargeCatalog.For(ChargeCatalog.Refinery);
        check(view.All.Count == RefineryRecipes.All.Count && view.All.All(r => r.Machine == ChargeCatalog.Refinery), "The V4 view holds only refinery recipes");
        check(ChargeCatalog.PrefixOf(ChargeCatalog.Refinery) == RefineryRules.Prefix, "Each catalog machine names its equipment");
        var ore = new ChargeInput(RefineryRules.Hydrates, 1, 10);
        var drawn = new ChargeRecipe("refinery", "test-draw", 99, new[] { ore, new ChargeInput(ManufacturingRules.Water, 1, 2) },
            new[] { new ProductSpec(RefineryRules.Gangue, 4, 3) }, null, 60, false, null, new Dictionary<string, double> { [ManufacturingRules.Water] = 20 }, -1);
        check(drawn.ItemInputs.Count == 1 && drawn.Draws.Single().Id == ManufacturingRules.Water && drawn.Units == 1 && drawn.ChargeKg == 12 && drawn.Circulates[ManufacturingRules.Water] == 20,
            "A recipe separates feed items from drawn commodities and keeps its circulating volume out of the balance");
        check(Math.Abs(drawn.ReactionKWhOver(30) + 0.5) < 1e-12 && drawn.ReactionKWhOver(-5) == 0, "Reaction heat accrues with progress, never backwards");
        throws(() => new ChargeRecipe("refinery", "melt-draw", 98, new[] { ore, new ChargeInput(ManufacturingRules.Water, 1, 2) }, new[] { new ProductSpec(RefineryRules.Gangue, 4, 3) },
            null, 60, true, null, null, 0), "A melt draws nothing from a vessel");
        throws(() => new ChargeRecipe("refinery", "only-water", 97, new[] { new ChargeInput(ManufacturingRules.Water, 1, 2) }, new[] { new ProductSpec(ManufacturingRules.Water, 1, 2) },
            null, 60, false, null, null, 0), "A charge binds at least one feed item");
        throws(() => new ChargeRecipe("refinery", "loop-item", 96, new[] { ore }, new[] { new ProductSpec(RefineryRules.Gangue, 3, 3), new ProductSpec(ManufacturingRules.Water, 1, 1) },
            null, 60, false, null, new Dictionary<string, double> { [RefineryRules.Gangue] = 1 }, 0), "Only a commodity circulates");
        var big = new ChargeRecipe("refinery", "big", 95, new[] { new ChargeInput(RefineryRules.Hydrates, 7, 10) }, new[] { new ProductSpec(RefineryRules.Gangue, 70, 1) }, null, 60, false, null, null, 0);
        throws(() => ChargeCatalog.Check(new[] { big }), "A charge must fit its machine's feed bin");
        var odd = new ChargeRecipe("refinery", "odd", 94, new[] { new ChargeInput(RefineryRules.Hydrates, 1, 9) }, new[] { new ProductSpec(RefineryRules.Gangue, 3, 3) }, null, 60, false, null, null, 0);
        throws(() => ChargeCatalog.Check(new[] { RefineryRecipes.Hydrates, odd }), "A feed identity has one unit mass per machine");
    }
}
