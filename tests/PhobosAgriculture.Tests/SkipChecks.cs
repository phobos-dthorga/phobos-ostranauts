using System;
using PhobosAgriculture.Core;

/// <summary>Agriculture 0.65.0: what held a crop back, and the time-skip rules (owner decision, 6 October 2026).</summary>
internal static class SkipChecks
{
    internal static void Run(Action<bool, string> check)
    {
        void Near(double a, double b, string m) => check(Math.Abs(a - b) < 1e-9, m + $": {a} vs {b}");
        check(SkipRoom.Lenient(true, false), "A skip with the setting off uses the lenient rules");
        check(!SkipRoom.Lenient(true, true) && !SkipRoom.Lenient(false, false) && !SkipRoom.Lenient(false, true), "Play, or a skip with room conditions on, uses the ordinary rules");

        var potato = Crop.Get("potato");
        CropState Planted() { var s = new CropState { Water = 20, Nutrients = .5 }; s.Plant(potato, 1); return s; }
        // Enough CO2 for an hour's growth with room to spare.
        const double plentyCo2 = 10, oxygen = 10;
        double hourCo2 = potato.Carbon * 44 / 30 / potato.Hours;

        var full = Planted().Step(1, potato.KW, plentyCo2, oxygen, true);
        check(full.Limit == GrowthLimit.None && !full.Stressed, "A crop with everything grows at its own rate, unhindered");
        var paused = Planted(); paused.Running = false;
        var pausedStep = paused.Step(1, potato.KW, plentyCo2, oxygen, true);
        check(pausedStep.Limit == GrowthLimit.Paused && pausedStep.Stressed, "A paused rack is named as the cause");
        var dim = Planted().Step(1, potato.KW * .2, plentyCo2, oxygen, true);
        check(dim.Limit == GrowthLimit.Power && dim.Stressed, "A fifth of the lamps' power is named");
        var dry = Planted(); dry.Water = 0;
        var dryStep = dry.Step(1, potato.KW, plentyCo2, oxygen, true);
        check(dryStep.Limit == GrowthLimit.Water && dryStep.Stressed, "An empty reservoir is named");
        var unfed = Planted(); unfed.Nutrients = 0;
        var unfedStep = unfed.Step(1, potato.KW, plentyCo2, oxygen, true);
        check(unfedStep.Limit == GrowthLimit.Nutrients && unfedStep.Stressed, "No nutrients is named");
        var hot = Planted().Step(1, potato.KW, plentyCo2, oxygen, false);
        check(hot.Limit == GrowthLimit.Room && hot.Stressed, "A room outside the crop's limits is named");

        // Short of CO2: a tenth of an hour's need. Ordinary rules: stress, then health loss after the grace hours.
        var starved = Planted(); var waiting = Planted();
        for (int hour = 0; hour < 10; hour++)
        {
            double massS = starved.ContentsMass, massW = waiting.ContentsMass;
            var s = starved.Step(1, potato.KW, hourCo2 * .1, oxygen, true);
            var w = waiting.Step(1, potato.KW, hourCo2 * .1, oxygen, true, carbonShortfallHarmless: true);
            check(s.Limit == GrowthLimit.CarbonDioxide && s.Stressed, "A CO2 shortfall is named and counts as stress in play");
            check(w.Limit == GrowthLimit.CarbonDioxide && !w.Stressed, "In a lenient skip the same shortfall is named but harmless");
            Near(starved.ContentsMass - massS + s.CO2Kg + s.OxygenKg + s.VapourKg, 0, "The stalled step conserves mass");
            Near(waiting.ContentsMass - massW + w.CO2Kg + w.OxygenKg + w.VapourKg, 0, "The waiting step conserves mass");
            check(w.CO2Kg >= -hourCo2 * .1 - 1e-12, "The waiting crop takes no more CO2 than the room holds");
        }
        check(starved.Health < 1 && starved.DarkHours > 9, "Ten stalled hours in play cost health");
        check(waiting.Health == 1 && waiting.DarkHours == 0, "Ten waiting hours in a skip cost nothing");
        Near(waiting.Progress, starved.Progress, "Both grew exactly what the CO2 allowed");
        // The exemption covers only CO2: a dry rack in a skip is still stressed.
        var drySkip = Planted(); drySkip.Water = 0;
        check(drySkip.Step(1, potato.KW, plentyCo2, oxygen, true, carbonShortfallHarmless: true).Stressed, "A dry rack is stressed in a skip too");
    }
}
