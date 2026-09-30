using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

/// <summary>The Lixivar SA-3 and acid tanks (Manufacturing 0.19.0): the roast against its reactions (IUPAC 2013 molar
/// masses), its energy and heat, the nodule's material, and the liquid store rules.</summary>
internal static class AcidPlantChecks
{
    private const double FeS = 0.087905, Fe2NiP = 0.201357, O2 = 0.031998, H2O = 0.018015, H2SO4 = 0.098072, Fe2O3 = 0.159687, NiO = 0.074692, H3PO4 = 0.097994;
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        var r = AcidPlantRecipes.Roast;
        double troilite = 7.000 / FeS, phosphide = 1.058 / Fe2NiP;
        double oxygen = (2.25 * troilite + 3.25 * phosphide) * O2, water = (troilite + 1.5 * phosphide) * H2O;
        double acid = troilite * H2SO4, phosphoric = phosphide * H3PO4, calcine = (troilite / 2 * Fe2O3 + phosphide * (Fe2O3 + NiO)) + 1.942;
        check(Math.Abs(r.Draws.Single(i => i.Id == ManufacturingRules.Oxygen).Kg - oxygen) < .005 && Math.Abs(r.Draws.Single(i => i.Id == ManufacturingRules.Water).Kg - water) < .005,
            "The roast draws 6.28 kg of oxygen and 1.58 kg of water for 79.63 mol of troilite and 5.25 mol of schreibersite");
        check(Math.Abs(r.Deposits.Single().Kg - acid) < .005 && r.Deposits.Single().Id == LiquidStores.SulfuricAcid &&
              Math.Abs(r.Products.Single(p => p.Id == Materials.PhosphoricAcidFlask).Kg - phosphoric) < .005 && Math.Abs(r.Products.Single(p => p.Id == Materials.RoastedCalcine).Kg - calcine) < .01,
            "It makes 7.81 kg of sulfuric acid for a tank, a 0.515 kg phosphoric acid flask and 9.535 kg of roasted calcine");
        check(Math.Abs(r.ChargeKg - (r.Products.Sum(p => p.Kg * p.Count) + r.OffGasKg)) < 1e-9 && r.OffGas.Count == 0 && !r.Melt && r.Units == 1,
            "The roast conserves mass, breathes nothing into the room and is no melt");
        // Formation enthalpies (kJ/mol): FeS -100.0, Fe2O3 -824.2, H2SO4(l) -814.0, H2O(l) -285.83, NiO -239.7, H3PO4(l) -1271.7, Fe2NiP about -160 (estimate).
        double sulfideKJ = (0.5 * -824.2 + -814.0) - (-100.0 + -285.83), phosphideKJ = (-824.2 + -239.7 + -1271.7) - (-160 + 1.5 * -285.83);
        check(Math.Abs(r.ReactionKWh - -(troilite * sulfideKJ + phosphide * phosphideKJ) / 3600) < .1 && r.ReactionKWh > 20,
            "The roast releases about 21 kWh into the room, on top of its electricity");
        check(Math.Abs(r.EnergyKWh - 4) < 1e-9 && AcidPlantRules.WorkingKW == 4 && AcidPlantRules.FeedCapacity == 2 && r.Seconds == ProcessJob.MaxSeconds,
            "The SA-3 draws 4 kWh a charge over its one-hour roast");
        check(Materials.ById(Materials.SulfideNodule)!.Mined && Materials.IsTerminal(Materials.RoastedCalcine) && !Materials.IsTerminal(Materials.PhosphoricAcidFlask),
            "The nodule is mined, the calcine terminal, the flask feed");
        check(Materials.ById(Materials.PhosphoricAcidFlask)!.Price + Materials.ById(Materials.RoastedCalcine)!.Price <= 1.5 * Materials.ById(Materials.SulfideNodule)!.Price,
            "The roast's sellable products stay within half again the nodule, before the oxygen and water");
        check(ChargeCommodities.Is(LiquidStores.SulfuricAcid) && LiquidStores.FamilyOf(LiquidStores.SulfuricAcid) == LiquidStores.AcidFamily && GasStores.FamilyOf(LiquidStores.SulfuricAcid) == null,
            "Sulfuric acid is a charge commodity in a liquid store, never a gas store");
        check(LiquidStores.All.Count == 3 && LiquidStores.All.All(s => s.Spec.DamagePolicy == Phobos.Ostranauts.Framework.Liquids.VesselDamagePolicy.Isolate) &&
              LiquidStores.All.Select(s => s.Prefix).Distinct().Count() == 3 && LiquidStores.All.All(s => !GasStores.IsFamily(s.Installed)),
            "Three acid tank sizes, each isolating on damage, none a gas store");
        check(LiquidStores.MistKg(1000) == .1 && LiquidStores.MistKg(-1) == 0 && LiquidStores.MistKg(double.NaN) == 0, "A damaged tank mists a ten-thousandth of its service acid");
        throws(() => new LiquidFamily("PhobosX", "x", "H2", "X", "r", "j", "g"), "A liquid's mist must be a game room species");
        var ports = new[] { AcidPlantRules.OxygenPort, AcidPlantRules.WaterPort, AcidPlantRules.AcidPort, AcidPlantRules.VesselPort, LeachRules.VesselPort, RefineryRules.VesselPort };
        check(ports.Distinct().Count() == ports.Length, "The SA-3's ports are its own");
    }
}
