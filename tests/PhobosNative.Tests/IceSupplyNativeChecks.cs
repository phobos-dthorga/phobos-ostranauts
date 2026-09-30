using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

/// <summary>Shipbreaker's ice supply against the game's own tables: the game's ice cluster joins the C- and S-class
/// field pickers right after its donors, the game itself already references that cluster (its unused ice picker),
/// C-class deposits carry extra water ice beside Manufacturing's clay on the shared silicates donor, and switching
/// both settings off restores the game's tables exactly.</summary>
internal static class IceSupplyNativeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string Single(string table) => DataHandler.dictLoot[table].aCOs.Single();
        check(DataHandler.dictAsteroidClusterBlueprints.TryGetValue(IceSupplyRules.IceCluster, out var ice) && ice.aAsteroids.Any(a => a.StartsWith("Ice01=", StringComparison.Ordinal)),
            "The game's own ice cluster blueprint is loaded and builds the Ice01 asteroid");
        check(DataHandler.dictLoot["RandomAsteroidI"].aCOs.Any(e => e.Contains(IceSupplyRules.IceCluster + "=")), "The game already references its ice cluster, in a picker no star-system field uses");
        check(Single(IceSupplyRules.CFields).Contains("ClusterC02=0.15x1|ClusterI01=0.05x1"), "C-class fields carry the game's ice cluster, carved from ClusterC02");
        check(Single(IceSupplyRules.SFields).Contains("ClusterS01=0.45x1|ClusterI01=0.05x1"), "S-class fields carry the game's ice cluster, carved from ClusterS01");
        string deposits = Single(IceSupplyRules.DepositTable);
        var depositUnits = deposits.Split('|');
        int silicatesAt = Array.IndexOf(depositUnits, "ItmMineral04=0.15x1");
        check(silicatesAt >= 0 && new[] { "ItmIce01=0.05x1", "PhobosAmmoniumSaltCrust=0.05x1", "PhobosClayHydrates=0.1x1", "PhobosEvaporiteCrust=0.05x1" }
                  .All(u => depositUnits.Count(x => x == u) == 1 && Array.IndexOf(depositUnits, u) > silicatesAt && Array.IndexOf(depositUnits, u) <= silicatesAt + 4) && deposits.Contains("ItmIce01=0.1x1"),
            "C-class deposits: silicates 0.15, then carved water ice 0.05, salt crust 0.05, clay 0.10 and evaporite crust 0.05, with the game's own 0.10 water ice unchanged");

        // Methane ice: the T2 delivers the commodity Manufacturing's methane stores hold, and its corrected price
        // stays above the products at the game's own methane price.
        check(PhobosManufacturing.Core.ManufacturingRules.Methane == ThawRules.MethaneCommodity &&
              PhobosManufacturing.Core.GasStores.Families.Any(f => f.Commodity == ThawRules.MethaneCommodity), "The T2's methane goes to Manufacturing's methane store commodity");
        check(DataHandler.dictLoot["GasPrices"].aCOs.Contains("CH4=1x2.2"), "The game prices methane at 2.2 per kg, as the price correction assumes");

        var original = new[] { IceSupplyRules.CFields, IceSupplyRules.SFields, IceSupplyRules.DepositTable }
            .ToDictionary(t => t, t => LootCarveRegistry.Original(t, DataHandler.dictLoot[t])!.ToArray());
        try
        {
            Content.Prepare(iceFields: false, depositIce: false).Publish();
            check(DataHandler.dictLoot[IceSupplyRules.CFields].aCOs.SequenceEqual(original[IceSupplyRules.CFields]) &&
                  DataHandler.dictLoot[IceSupplyRules.SFields].aCOs.SequenceEqual(original[IceSupplyRules.SFields]), "Ice fields switched off: the game's field pickers are exactly restored");
            var off = Single(IceSupplyRules.DepositTable).Split('|');
            int offSilicates = Array.IndexOf(off, "ItmMineral04=0.2x1");
            check(!off.Contains("ItmIce01=0.05x1") && offSilicates >= 0 && new[] { "PhobosAmmoniumSaltCrust=0.05x1", "PhobosClayHydrates=0.1x1", "PhobosEvaporiteCrust=0.05x1" }
                      .All(u => off.Count(x => x == u) == 1 && Array.IndexOf(off, u) > offSilicates && Array.IndexOf(off, u) <= offSilicates + 3),
                "Extra deposit ice switched off: only Manufacturing's clay, salt crust and evaporite crust remain carved");
        }
        finally { Content.Prepare().Publish(); }
        check(Single(IceSupplyRules.DepositTable) == deposits, "Switching the settings back on restores the same tables");
    }
}
