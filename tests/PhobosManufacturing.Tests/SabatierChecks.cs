using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

/// <summary>The K2 Sabatier reactor's chemistry, heat and saved record, and the M2 methane store, on numbers alone.</summary>
internal static class SabatierChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        // The panel's cycle meter (Manufacturing 0.48.0): a share done and minutes left, not kWh that read like a countdown.
        check(ManufacturingRules.PercentDone(0.34, 1.2) == 28 && ManufacturingRules.MinutesLeft(0.34, 1.2, 1.2) == 43, "A K2 cycle at 0.34 of 1.20 kWh is 28% done with about 43 minutes left");
        check(ManufacturingRules.PercentDone(0, 1.2) == 0 && ManufacturingRules.PercentDone(1.1999, 1.2) == 99 && ManufacturingRules.PercentDone(1.2, 1.2) == 100, "The meter reads 100% only when the cycle has finished");
        check(ManufacturingRules.MinutesLeft(1.199, 1.2, 1.2) == 1 && ManufacturingRules.MinutesLeft(1.2, 1.2, 1.2) == 0 && ManufacturingRules.MinutesLeft(0, 6, 6) == 60, "Minutes left never read zero while work remains, and a fresh X2 cycle is an hour");
        check(ManufacturingRules.PercentDone(double.NaN, 1.2) == 0 && ManufacturingRules.MinutesLeft(0.5, 1.2, 0) == 0 && ManufacturingRules.PercentDone(-1, 1.2) == 0, "Unreadable figures show nothing done, never a fault");
        // CO2 + 4 H2 -> CH4 + 2 H2O with the game's molar masses; water is the balancing remainder.
        check(SabatierRules.Balanced(), "Every cycle conserves mass and makes the stoichiometric water within 0.01%");
        check(Math.Abs(SabatierRules.HydrogenMolesPerCycle - 62.007) < 0.01 && Math.Abs(SabatierRules.CarbonDioxideMolesPerCycle * 4 - SabatierRules.HydrogenMolesPerCycle) < 1e-9,
            "0.125 kg of hydrogen is 62.0 mol, reacting with a quarter as many moles of CO2");
        check(Math.Abs(SabatierRules.CarbonDioxideKgPerCycle - 0.6822) < 0.001 && Math.Abs(SabatierRules.MethaneKgPerCycle - 0.2487) < 0.001 && Math.Abs(SabatierRules.WaterKgPerCycle - 0.5585) < 0.001,
            "Each cycle uses 0.682 kg of CO2 and makes 0.249 kg of methane and 0.559 kg of water");
        check(SabatierRules.HydrogenKgPerCycle == ProcessorRules.HydrogenKgPerCycle, "One reactor cycle consumes exactly one X2 cycle's hydrogen");
        // The loop: of the X2's 1.125 kg of water, the reactor returns about half, as the ISS system does.
        double recovered = SabatierRules.WaterKgPerCycle / ProcessorRules.WaterKgPerCycle;
        check(recovered > 0.45 && recovered < 0.55, "Electrolysis plus Sabatier returns about half the water");
        // Heat: NIST formation enthalpies with the water condensed.
        check(Math.Abs(SabatierRules.ReactionKJPerMol - 253.02) < 1e-9, "The reaction releases 253.02 kJ per mole of CO2 with liquid water (NIST)");
        check(Math.Abs(SabatierRules.ReactionKWhPerCycle - 1.0896) < 0.001 && Math.Abs(SabatierRules.RoomHeatKW(true) - (SabatierRules.WorkingKW + SabatierRules.ReactionKW)) < 1e-12 &&
            SabatierRules.RoomHeatKW(false) == SabatierRules.IdleKW, "The working reactor warms its room with its electricity and 1.09 kWh of reaction heat per cycle");
        var (cycle, complete) = SabatierRules.Advance(0, 10);
        check(cycle == SabatierRules.CycleKWh && complete, "Surplus energy completes one cycle and is not banked");
        (cycle, complete) = SabatierRules.Advance(0.5, 0.2);
        check(Math.Abs(cycle - 0.7) < 1e-12 && !complete, "Partial supply advances the cycle");
        throws(() => SabatierRules.Advance(SabatierRules.CycleKWh, 0.1), "A completed cycle cannot be advanced");
        throws(() => SabatierRules.Advance(0, double.NaN), "Invalid energy is refused");
        check(ProcessorRules.Advance(0, 100).CycleKWh == ProcessorRules.CycleKWh, "The X2 still uses the shared one-cycle rule");

        // The saved record: charge, convert, deliver.
        var s = new SabatierState();
        check(!s.Charged && !s.HoldsProducts && s.HeldKg == 0, "A new reactor holds nothing");
        throws(() => s.Convert(), "An uncharged reactor cannot convert");
        s.HydrogenKg = SabatierRules.HydrogenKgPerCycle; s.CarbonDioxideKg = SabatierRules.CarbonDioxideKgPerCycle; s.CycleKWh = 0.4;
        double before = s.HeldKg;
        s.Convert();
        check(Math.Abs(s.HeldKg - before) < 1e-12 && s.HoldsProducts && s.HydrogenKg == 0 && s.CarbonDioxideKg == 0 && s.CycleKWh == 0 && s.Cycles == 1 &&
            Math.Abs(s.ConsumedCarbonDioxideKg - SabatierRules.CarbonDioxideKgPerCycle) < 1e-12, "Conversion turns reactants into products mass for mass and closes the cycle");
        throws(() => s.Convert(), "Products must leave before the next conversion");
        s.Canister = "ItmRTACO2abc";
        var round = SabatierState.Read(s.Save());
        check(round.WaterKg == s.WaterKg && round.MethaneKg == s.MethaneKg && round.Cycles == 1 && round.Canister == "ItmRTACO2abc" && round.ConsumedCarbonDioxideKg == s.ConsumedCarbonDioxideKg,
            "The record round-trips");
        check(SabatierState.Read(new SabatierState().Save()).Canister == "", "No canister round-trips as none");
        foreach (var bad in new[] {
            new Dictionary<string, string> { ["h2"] = "0.2" }, new Dictionary<string, string> { ["co2"] = "-1" }, new Dictionary<string, string> { ["water"] = "5" },
            new Dictionary<string, string> { ["cycle"] = "1.2" }, new Dictionary<string, string> { ["cycle"] = "0.1", ["water"] = "0.1" },
            new Dictionary<string, string> { ["unknown"] = "1" }, new Dictionary<string, string> { ["ch4"] = "NaN" } })
            throws(() => SabatierState.Read(bad), "A corrupt record is refused: " + string.Join(",", bad.Select(p => p.Key + "=" + p.Value)));
        check(SabatierRules.IsFamily("PhobosSabatierReactorInstalledDmg") && !SabatierRules.IsFamily("PhobosChemicalProcessorInstalled"), "Family identity");
        var ports = new[] { SabatierRules.WaterOutPort, SabatierRules.VesselInPort, SabatierRules.HydrogenInPort, SabatierRules.StoreOutPort, SabatierRules.MethaneOutPort,
            ProcessorRules.WaterInPort, ProcessorRules.VesselOutPort, ProcessorRules.HydrogenOutPort, RefineryRules.VesselPort };
        check(ports.Distinct().Count() == ports.Length && ports.All(p => p.StartsWith("PhobosManufacturing.", StringComparison.Ordinal)),
            "Every vessel-side port is distinct, so one vessel can serve a refinery, a cell and a reactor at once");

        // The methane store and its burn.
        var methane = GasStores.Methane;
        check(methane.Spec.Commodity == "methane" && methane.CapacityKg == 160 && methane.DryKg == 160 && methane.Spec.DamagePolicy == VesselDamagePolicy.Leak && methane.LeakSpecies == "CH4",
            "The methane store holds 160 kg, leaks when damaged, and leaks into the room as the game's CH4");
        check(GasStores.Hydrogen.LeakSpecies == null && GasStores.For("PhobosMethaneStoreLooseDmg") == methane && GasStores.For("PhobosHydrogenStoreInstalled") == GasStores.Hydrogen && GasStores.For("PhobosVolatilesRefineryInstalled") == null,
            "Hydrogen leaks to space; families resolve to their fuel");
        check(Math.Abs(methane.Family.Fuel!.OxygenPerFuel - 3.989) < 0.001 && Math.Abs(methane.Family.Fuel!.RoomProductsPerKg["CO2"] - 2.743) < 0.001 && Math.Abs(methane.Family.Fuel!.HeatingKJPerKg / 1000 - 55.51) < 0.02,
            "CH4 + 2 O2 -> CO2 + 2 H2O: 3.99 kg of oxygen and 2.74 kg of CO2 per kg, 55.5 MJ/kg (NIST)");
        var burn = methane.Burn(10, 20);
        check(Math.Abs(burn.BurnedKg - 20 / methane.Family.Fuel!.OxygenPerFuel) < 1e-9 && Math.Abs(burn.OxygenKg - 20) < 1e-9 && Math.Abs(burn.RoomProductsKg["CO2"] - burn.BurnedKg * 2.7433) < 0.01 &&
            Math.Abs(burn.LostKg + burn.BurnedKg - 10) < 1e-12, "A methane burn is bounded by the room's oxygen and leaves carbon dioxide");
        check(GasStores.SizeFor(283799) == "Small" && GasStores.SizeFor(283800) == "Medium" && GasStores.SizeFor(1135200) == "Large",
            "Blast size follows energy: the energy of 2 kg and 8 kg of hydrogen");
        check(methane.Burn(160, 1e6).Size == "Large" && methane.Burn(4, 1e6).Size == "Small", "A full methane store is a large blast; a few kilograms a small one");
        check(HydrogenRules.Burn(1, 100).RoomProductsKg.Count == 0, "Burning hydrogen adds no game gas (its water vapour has no species)");
        throws(() => methane.Burn(double.PositiveInfinity, 1), "Invalid fuel is refused");
        check(methane.Spec.Journal != GasStores.Hydrogen.Spec.Journal && methane.Spec.Guard != GasStores.Hydrogen.Spec.Guard && methane.Spec.Record != GasStores.Hydrogen.Spec.Record,
            "The two stores keep separate records, journals and guards");
        check(NativeGasCanister.IsRoomSpecies("CH4") && NativeGasCanister.IsRoomSpecies("CO2"), "Methane and CO2 are the game's own gases");
    }
}
