using System;
using PhobosAgriculture.Core;

/// <summary>Agriculture 0.59.0: misting a crop in a room too hot for it.</summary>
internal static class MistingChecks
{
    internal static void Run(Action<bool, string> check)
    {
        void Near(double a, double b, string m) => check(Math.Abs(a - b) < 1e-9, m + $": {a} vs {b}");
        void Reject(Action action, string m) { bool refused = false; try { action(); } catch (ArgumentException) { refused = true; } check(refused, m); }
        var rules = new MistingRules();
        check(rules.MaxCoolingC == 4 && rules.WaterKgPerHourPerC == .15 && rules.DamageShareBeyond == .5 && rules.ReserveKg == 2, "Default misting: 4 C, 0.15 kg an hour per degree, half damage beyond, 2 kg reserve");

        var inside = Misting.Plan(3, rules, 1, 20);
        check(inside.Covered && inside.DamageScale == 1, "Three degrees over the ceiling is covered");
        Near(inside.WaterKg, .45, "Covering 3 C for an hour takes 0.45 kg"); Near(inside.CoolingC, 3, "It holds the crop 3 C below the room");
        var edge = Misting.Plan(4, rules, .5, 20);
        check(edge.Covered, "The band's edge is still covered"); Near(edge.WaterKg, .3, "Half an hour at 4 C takes 0.3 kg");
        var beyond = Misting.Plan(7, rules, 1, 20);
        check(!beyond.Covered, "Seven degrees over is more than misting can cover");
        Near(beyond.WaterKg, .6, "Beyond the band misting runs at its full 4 C rate"); Near(beyond.DamageScale, .5, "and halves the damage");
        var short1 = Misting.Plan(3, rules, 1, 2.2);
        check(!short1.Covered, "Short of water the room is not covered");
        Near(short1.WaterKg, .2, "Only water above the 2 kg reserve is used"); Near(short1.DamageScale, 1 - .5 * (.2 / .45), "Damage is reduced by the share of water it got");
        var dry = Misting.Plan(3, rules, 1, 2);
        check(dry.WaterKg == 0 && !dry.Covered && dry.DamageScale == 1, "At the reserve misting does nothing");
        check(Misting.Plan(0, rules, 1, 20).WaterKg == 0 && Misting.Plan(-2, rules, 1, 20).WaterKg == 0, "A room within limits is not misted");
        check(Misting.Plan(3, new MistingRules { MaxCoolingC = 0 }, 1, 20).WaterKg == 0, "A crop with no misting limit is not misted");
        check(Misting.Plan(double.NaN, rules, 1, 20).WaterKg == 0 && Misting.Plan(3, rules, 0, 20).WaterKg == 0, "Bad or empty steps take nothing");

        // The pack: shared figures and one crop's own limit.
        var g = new GrowthEntry { misting = new MistingEntry { waterKgPerHourPerC = .2, reserveKg = 1 } };
        g.crops["lettuce"] = new CropGrowthEntry { misting = new CropMistingEntry { maxCoolingC = 3 } };
        var lettuce = Growth.MistingOf(g, "lettuce"); var potato = Growth.MistingOf(g, "potato");
        check(lettuce.MaxCoolingC == 3 && lettuce.WaterKgPerHourPerC == .2 && lettuce.ReserveKg == 1, "A crop's own limit replaces only the cooling");
        check(potato.MaxCoolingC == 4 && Growth.MistingOf(null, null).MaxCoolingC == 4, "Other crops and no section keep the defaults");
        var ids = new System.Collections.Generic.HashSet<string> { "lettuce", "potato" };
        Growth.Validate(g, ids);
        Reject(() => Growth.Validate(new GrowthEntry { misting = new MistingEntry { maxCoolingC = 20 } }, ids), "More than 15 C of misting is refused");
        Reject(() => Growth.Validate(new GrowthEntry { misting = new MistingEntry { waterKgPerHourPerC = 0 } }, ids), "Misting that takes no water is refused");
        Reject(() => Growth.Validate(new GrowthEntry { misting = new MistingEntry { damageShareBeyond = 1.5 } }, ids), "A damage share above 1 is refused");
        Reject(() => Growth.Validate(new GrowthEntry { misting = new MistingEntry { reserveKg = 25 } }, ids), "A reserve larger than the rack is refused");
        { var bad = new GrowthEntry(); bad.crops["lettuce"] = new CropGrowthEntry { misting = new CropMistingEntry { maxCoolingC = -1 } }; Reject(() => Growth.Validate(bad, ids), "A crop's negative misting limit is refused"); }

        // Out of its room, a misted crop loses health at the scaled rate.
        CropState Outside(double scale)
        {
            var s = new CropState { Water = 20, Nutrients = .5 }; s.Plant(Crop.Get("potato"), 1);
            for (int h = 0; h < 4; h++) s.Step(1, .75, 10, 10, false, 1, null, scale);
            return s;
        }
        Near(1 - Outside(.5).Health, (1 - Outside(1).Health) / 2, "Half the damage share loses half the health");
        Reject(() => new CropState().Step(1, 0, 1, 1, false, 1, null, 1.5), "A damage scale above 1 is refused");
        Near(CropState.LatentKWhPerKg, 2.45 / 3.6, "Evaporating a kilogram takes 0.68 kWh from the room");
    }
}
