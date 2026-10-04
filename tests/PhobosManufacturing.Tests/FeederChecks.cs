using System;
using System.Collections.Generic;
using PhobosManufacturing.Core;

/// <summary>The Slingwright RM-1 reaction mass feeder (Manufacturing 0.43.0): the energy a kilogram takes against the
/// physics it is authored from, capacity, the saved settings, and what the thrusters may draw.</summary>
internal static class FeederChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        check(Math.Abs(FeederRules.IdealKWhPerKg - 0.068) < .001 && FeederRules.KWhPerKg > 2 * FeederRules.IdealKWhPerKg && FeederRules.KWhPerKg < 2.5 * FeederRules.IdealKWhPerKg,
            "Throwing a kilogram at 700 m/s takes 0.068 kWh; the feeder pays a little over twice that");
        check(Math.Abs(FeederRules.WorkingKW / FeederRules.KWhPerKg - 20) < 1e-9, "At 3 kW the feeder grinds 20 kg an hour");
        var (part, done) = FeederRules.Advance(0, 1, 9);
        check(!done && part == 1, "A 9 kg reject needs 1.35 kWh, so one kilowatt-hour does not finish it");
        var (full, complete) = FeederRules.Advance(part, 5, 9);
        check(complete && Math.Abs(full - 1.35) < 1e-12, "Energy beyond what the item needs is not credited");
        throws(() => FeederRules.Advance(-1, 1, 9), "Credited energy is never negative");
        throws(() => FeederRules.GrindKWh(0), "An item has mass");
        check(FeederRules.Fits(45.67, 14.33) && !FeederRules.Fits(46, 14.33) && FeederRules.Fits(0, FeederRules.CapacityKg), "An item is ground only when its whole mass fits the 60 kg record");
        check(FeederRules.ExhaustRatio == 1 && FeederRules.EquivalentKg(12) == 12, "A kilogram of reaction mass counts as a kilogram of nitrogen");
        check(FeederRules.KilogramsFor(5, 2) == 2 && FeederRules.KilogramsFor(1, 30) == 1 && FeederRules.KilogramsFor(1, 0) == 0 && FeederRules.KilogramsFor(-1, 30) == 0,
            "The thrusters take what they ask for, never more than is held, and nothing on a bad request");
        var saved = FeederState.Read(new FeederState { Feeding = false, First = false, GrindKWh = .4 }.Save());
        check(!saved.Feeding && !saved.First && Math.Abs(saved.GrindKWh - .4) < 1e-12, "The feeder's settings and work survive reload");
        var fresh = new FeederState();
        check(fresh.Feeding && fresh.First && fresh.GrindKWh == 0, "A new feeder feeds the thrusters and is burned before the canisters");
        throws(() => FeederState.Read(new Dictionary<string, string> { ["feeding"] = "yes", ["first"] = "1", ["grind"] = "0" }), "An unknown switch value is refused");
        check(Array.IndexOf(FeederRules.SettingPrefixes, "order:") >= 0 && Array.IndexOf(FeederRules.SettingPrefixes, "feeding:") >= 0, "Both panel settings are ones the panel can apply");
    }
}
