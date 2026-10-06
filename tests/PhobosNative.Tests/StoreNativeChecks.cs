using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Framework 0.116.0: the shipped <c>stores</c> pack judged against the game's own definitions. Every ship
/// weapon, charger, scrubber, nav console and toilet is left out of store pickers; the game's racks, bins and fridge,
/// and every installed Phobos container, still count as stores.</summary>
internal static class StoreNativeChecks
{
    internal static void Run(IEnumerable<NativeDefinitions> phobos, Action<bool, string> check)
    {
        var rule = StoreRules.Pack;
        Func<string, bool> Has(JsonCondOwner d)
        {
            var names = new HashSet<string>((d.aStartingConds ?? Array.Empty<string>()).Select(c => c.Split('=')[0]), StringComparer.Ordinal);
            return names.Contains;
        }
        bool Excluded(JsonCondOwner d) => StoreRules.Excluded(rule, Has(d), d.aInteractions, d.strContainerCT);
        bool Installed(JsonCondOwner d) => Has(d)("IsInstalled") && !string.IsNullOrEmpty(d.strContainerCT);
        var game = DataHandler.dictCOs.Values.Where(Installed).ToArray();
        var weapons = game.Where(d => Has(d)("IsShipWeapon")).ToArray();
        check(weapons.Length >= 20 && weapons.All(Excluded), "Every installed ship weapon with a magazine is left out of the store pickers");
        foreach (string id in new[] { "ItmShipWeaponPDC02", "ItmToilet01", "ItmAtmoScrubber01", "ItmStationNav", "ItmChargerBattery04", "ItmSink01", "ItmVendingMachineCoffee" })
            check(DataHandler.dictCOs.TryGetValue(id, out var d) && Excluded(d), "Not a store: " + id);
        foreach (string id in new[] { "ItmRack1x401", "ItmRack1x201", "ItmRack2x2C01", "ItmStorageBin2x101", "ItmStorageBinFloor1x101", "ItmFridge01", "ItmSecretCompartment01" })
            check(DataHandler.dictCOs.TryGetValue(id, out var d) && !Excluded(d), "Still a store: " + id);
        // Ours: no installed Phobos container is turned away by the shipped rules, except the legacy compartments that
        // take no new cargo (the radiator, thermal port and A2 regulator: anything left in them is only recovered).
        var refused = phobos.SelectMany(s => s.Objects.Values).Where(Installed).Where(Excluded)
            .Where(d => !d.strContainerCT.EndsWith("NoNewCargo", StringComparison.Ordinal))
            .Select(d => d.strName + " (" + d.strContainerCT + ", " + string.Join("/", d.aInteractions ?? Array.Empty<string>()) + ")").ToArray();
        check(refused.Length == 0, "Phobos installed containers still count as stores: " + string.Join("; ", refused));
    }
}
