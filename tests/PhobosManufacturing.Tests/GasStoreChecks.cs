using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

/// <summary>The gas store size ladder, the new oxygen, nitrogen and carbon dioxide stores, and the L2 filling
/// station's rules and record, on numbers alone.</summary>
internal static class GasStoreChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        // The ladder: five families, three sizes each, the small size unchanged.
        check(GasStores.Families.Count == 7 && GasStores.All.Count == 21 && GasStores.All.Select(s => s.Prefix).Distinct().Count() == 21,
            "Seven gas families in three sizes, twenty-one distinct definitions");
        check(GasStores.Hydrogen.Prefix == HydrogenRules.Prefix && GasStores.Hydrogen.CapacityKg == 24 && GasStores.Hydrogen.Spec.Record == HydrogenRules.Record &&
              GasStores.Methane.Prefix == MethaneRules.Prefix && GasStores.Methane.CapacityKg == 160 && GasStores.Methane.Spec.Record == MethaneRules.Record,
            "The original H2 and M2 keep their identities, capacities and saved records");
        check(GasStores.All.Select(s => s.Spec.Record).Concat(GasStores.All.Select(s => s.Spec.Journal)).Concat(GasStores.All.Select(s => s.Spec.Guard)).Distinct().Count() == 63,
            "Every size keeps its own record, journal and guard");
        // The definition index answers exactly as the query over every size did, for every form of every size.
        foreach (var store in GasStores.All)
            foreach (string form in new[] { "Installed", "InstalledDmg", "Loose", "LooseDmg" })
                check(GasStores.For(store.Prefix + form) == store && GasStores.All.FirstOrDefault(f => f.IsFamily(store.Prefix + form)) == store, "Indexed lookup matches the size ladder: " + store.Prefix + form);
        check(GasStores.For("ItmCanisterO2Installed") == null && GasStores.For(null) == null && GasStores.For(GasStores.Hydrogen.Prefix + "InstalledExtra") == null, "Foreign and extended ids belong to no store");
        for (int i = 0; i < 1000; i++) GasStores.IsFamily("ItmAirPumpInstalled");
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 42000; i++) GasStores.IsFamily("ItmAirPumpInstalled");
        check(GC.GetAllocatedBytesForCurrentThread() == before, "Classifying a foreign appliance allocates nothing on the test runtime");
        foreach (var family in GasStores.Families)
        {
            var sizes = family.Sizes;
            check(sizes[0].Footprint == 2 && sizes[1].Footprint == 3 && sizes[2].Footprint == 4, "Sizes are 2 x 2, 3 x 3 and 4 x 4: " + family.Commodity);
            check(sizes[0].CapacityKg < sizes[1].CapacityKg && sizes[1].CapacityKg < sizes[2].CapacityKg && sizes[1].CapacityKg / sizes[1].Price > sizes[0].CapacityKg / sizes[0].Price &&
                  sizes[2].CapacityKg / sizes[2].Price > sizes[1].CapacityKg / sizes[1].Price, "Bigger stores hold more for less per kilogram: " + family.Commodity);
            check(sizes.All(s => s.Spec.DamagePolicy == VesselDamagePolicy.Leak && s.LeakKgPerHour > 0 && s.Spec.Commodity == family.Commodity), "Every size leaks when damaged: " + family.Commodity);
            check(sizes.All(s => GasStores.For(s.Installed + "Dmg") == s && GasStores.For(s.Prefix + "Loose") == s), "Every form resolves to its own size: " + family.Commodity);
        }
        check(GasStores.HydrogenFamily.Sizes[1].CapacityKg == 59 && GasStores.HydrogenFamily.Sizes[2].CapacityKg == 115 &&
              GasStores.MethaneFamily.Sizes[1].CapacityKg == 395 && GasStores.MethaneFamily.Sizes[2].CapacityKg == 770, "H3/H4 and M3/M4 capacities follow the ladder");
        check(HydrogenRules.AnySize("PhobosHydrogenStoreLargeInstalled") && !HydrogenRules.IsFamily("PhobosHydrogenStoreLargeInstalled"), "Any hydrogen size feeds a K2; the small family stays exact");
        // The new families: native gases that leak into the room, sized from the canister vessel at 80% of ideal moles.
        double moles = NativeGasCanister.CapacityMoles(0.787, 41400, 293) * GasStores.UsableMolesFraction;
        foreach (var (family, species) in new[] { (GasStores.OxygenFamily, "O2"), (GasStores.NitrogenFamily, "N2"), (GasStores.CarbonDioxideFamily, "CO2") })
        {
            check(family.Species == species && family.LeaksIntoRoom && family.Small.LeakSpecies == species && family.Fuel == null, "A leak goes into the room as the game's " + species);
            check(Math.Abs(family.SmallCapacityKg - NativeGasCanister.Kilograms(species, moles)) / family.SmallCapacityKg < 0.01, "The small " + species + " store holds 80% of the canister vessel's ideal moles");
            check(!family.Small.IsFuel, species + " is not burned as a fuel");
            throws(() => family.Small.Burn(1, 1), "Burning a non-fuel is refused: " + species);
        }
        check(GasStores.Hydrogen.LeakSpecies == null, "Hydrogen still leaks to space, never into a room");
        // Ammonia: kept liquefied (609 kg/m3 at 20 C) in the same vessel at an 80% fill; leaks into the room as the game's NH3.
        var ammonia = GasStores.AmmoniaFamily;
        check(ammonia.Species == "NH3" && ammonia.LeaksIntoRoom && ammonia.Fuel == null && ammonia.Model == "Q" && ammonia.Commodity == ManufacturingRules.Ammonia,
            "The ammonia store holds the game's NH3, leaks into the room and is not modelled as a deflagrating fuel");
        check(Math.Abs(ammonia.SmallCapacityKg - 0.787 * 609 * 0.8) < 5 && ammonia.SmallCapacityKg > NativeGasCanister.Kilograms("NH3", moles), "Liquefied ammonia fills the vessel far beyond its ideal-gas moles");
        check(Math.Abs(ManifoldRules.Ratio("ammonia") - 1.409) < .005, "The P1 burns stored ammonia in the RCS (owner decision) at its cold-gas worth, about 1.41 times nitrogen per kilogram");
        check(GasStores.FamilyOf("carbon dioxide") == GasStores.CarbonDioxideFamily && GasStores.FamilyOf("water") == null, "Commodities resolve to their family");
        check(Math.Abs(ManifoldRules.Ratio("nitrogen") - 1) < 1e-12 && Math.Abs(ManifoldRules.Ratio("oxygen") - 0.935) < 0.01, "The P1 values nitrogen and oxygen stores at their RCS worth");
        // The line port: neighbouring tile on +X, in the middle row, with the joint on the footprint tile beside it.
        check(GasStores.Hydrogen.Outlet == (24, 8, 1), "The 2 x 2 port is unchanged");
        check(GasStores.HydrogenFamily.Sizes[1].Outlet == (32, 0, 5) && GasStores.HydrogenFamily.Sizes[2].Outlet == (40, 8, 7), "3 x 3 and 4 x 4 ports sit beside the middle row");

        // The L2: compression energy, fill arithmetic and its saved record.
        double o2Canister = FillerRules.KWhPerKg("O2", 41400), o2Bottle = FillerRules.KWhPerKg("O2", 20684);
        check(Math.Abs(o2Canister - 0.060) < 0.003 && o2Bottle < o2Canister, "Isothermal compression to a canister's rating is about 0.06 kWh per kg of oxygen; a bottle's lower rating costs less");
        check(FillerRules.KWhPerKg("CO2", 41400) < FillerRules.KWhPerKg("N2", 41400), "Heavier gases need less work per kilogram");
        check(FillerRules.KWhPerKg("O2", 5000) > 0, "Even a low-rated vessel costs a little electricity");
        check(FillerRules.KgFor(0.3, 0.06, 100) == 5 && FillerRules.KgFor(3, 0.06, 2) == 2 && FillerRules.KgFor(0, 0.06, 2) == 0, "A step moves what its electricity pays for, no more than wanted");
        check(FillerRules.Commodity("N2") == "nitrogen" && FillerRules.Commodity("H2") == "hydrogen" && FillerRules.Commodity("Plasma") == null, "Gases map to store commodities");
        throws(() => FillerRules.KWhPerKg("H2", 41400), "Hydrogen has no native vessel to fill");
        var state = new FillerState();
        check(state.Mode == FillerMode.Fill && state.Links.Count == 0, "A new station fills and has no links");
        check(state.Link("StoreA", FillerLinkKind.Store) && !state.Links[0].Enabled && state.Link("CanA", FillerLinkKind.Target) && state.Links[1].Enabled && !state.Link("CanA", FillerLinkKind.Target),
            "Stores link switched off; canisters link as fill targets; each once");
        check(state.Role("CanA", FillerLinkKind.Source) && state.OfKind(FillerLinkKind.Source).Single().Id == "CanA" && !state.Role("StoreA", FillerLinkKind.Target), "Canisters change role; stores do not");
        for (int i = 0; i < 3; i++) state.Link("Can" + i, FillerLinkKind.Target);
        check(!state.Link("CanX", FillerLinkKind.Target) && state.Link("StoreB", FillerLinkKind.Store), "Four canisters at most, stores counted separately");
        state.Mode = FillerMode.Decant; state.FilledKg = 12.5; state.Switch("StoreA", true);
        var back = FillerState.Read(state.Save());
        check(back.Mode == FillerMode.Decant && back.FilledKg == 12.5 && back.Links.Count == state.Links.Count && back.Links.First(l => l.Id == "StoreA").Enabled &&
              back.Links.First(l => l.Id == "CanA").Kind == FillerLinkKind.Source, "The record round-trips");
        throws(() => FillerState.Read(new Dictionary<string, string> { ["mode"] = "fill", ["links"] = "A|q|1", ["filled"] = "0", ["decanted"] = "0" }), "An unknown link kind is refused");
        throws(() => FillerState.Read(new Dictionary<string, string> { ["mode"] = "spin", ["links"] = "", ["filled"] = "0", ["decanted"] = "0" }), "An unknown mode is refused");
        throws(() => FillerState.Read(new Dictionary<string, string> { ["mode"] = "fill", ["links"] = "", ["filled"] = "-1", ["decanted"] = "0" }), "A negative total is refused");
        // Keep suit bottles charged: a bottle the station has filled counts as charged, so crew never fetch it again.
        check(FillerRules.Charged(FillerRules.FillFraction) && FillerRules.Charged(FillerRules.ChargedFraction) && !FillerRules.Charged(0.5) && !FillerRules.Charged(0),
            "A bottle filled by the station counts as charged; a half-empty one does not");
        check(FillerRules.ChargedFraction < FillerRules.FillFraction, "The charged threshold sits below the station's own stop, so a filled bottle is never fetched back");
    }
}
