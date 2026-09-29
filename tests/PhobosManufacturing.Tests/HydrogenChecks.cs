using System;
using Phobos.Ostranauts.Framework.Liquids;
using PhobosManufacturing.Core;

/// <summary>The hydrogen store's declaration and the deflagration arithmetic on numbers alone.</summary>
internal static class HydrogenChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        var spec = HydrogenRules.Spec;
        check(spec.Commodity == "hydrogen" && spec.CapacityKg == 24 && spec.DryKg == 160 && spec.DamagePolicy == VesselDamagePolicy.Leak && spec.LeakKgPerHour == 2 && spec.Owner == ManufacturingRules.Owner,
            "The store holds 24 kg of hydrogen in a 160 kg housing and leaks 2 kg an hour when damaged");
        check(spec.LeakKg(0.5, 24) == 1 && spec.LeakKg(100, 3) == 3, "A leak is bounded by the contents");
        check(HydrogenRules.Fate(21, true, false, false) == HydrogenFate.Deflagrate && HydrogenRules.Fate(21, false, true, false) == HydrogenFate.Deflagrate && HydrogenRules.Fate(21, false, false, true) == HydrogenFate.Deflagrate,
            "A fire, a working hearth or a sparking device ignites a release in air");
        check(HydrogenRules.Fate(21, false, false, false) == HydrogenFate.Leak && HydrogenRules.Fate(4.9, true, false, false) == HydrogenFate.Leak && HydrogenRules.Fate(double.NaN, true, true, true) == HydrogenFate.Leak,
            "Without ignition, without oxygen, or without a reading, hydrogen only leaks");
        var burn = HydrogenRules.Burn(1, 100);
        check(burn.BurnedKg == 1 && burn.OxygenKg == 8 && Math.Abs(burn.EnergyKJ - 141900) < 1e-9 && burn.LostKg == 0 && burn.Size == "Small", "One kilogram burns with eight of oxygen for 141.9 MJ");
        burn = HydrogenRules.Burn(24, 40);
        check(burn.BurnedKg == 5 && burn.OxygenKg == 40 && burn.LostKg == 19 && burn.Size == "Medium", "A full store in a small room burns only what the oxygen allows; the rest is lost");
        check(HydrogenRules.Burn(24, 1000).Size == "Large" && HydrogenRules.Burn(0, 100).BurnedKg == 0 && HydrogenRules.Burn(3, 0).BurnedKg == 0, "Sizes follow the burned mass; nothing burns without hydrogen or oxygen");
        check(HydrogenRules.DeflagrationDefinition(burn) == "SysPhobosDeflagrationMedium", "The explosion definition follows the size");
        throws(() => HydrogenRules.Burn(-1, 10), "Negative hydrogen is refused");
        throws(() => HydrogenRules.Burn(1, double.NaN), "Invalid oxygen is refused");
    }
}
