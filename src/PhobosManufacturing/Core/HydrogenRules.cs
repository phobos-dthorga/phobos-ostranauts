using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Liquids;

namespace PhobosManufacturing.Core;

/// <summary>The H2 Hydrogen Store: a 2 x 2 pressurised vessel holding electrolysis hydrogen as a kilogram record
/// (hydrogen has no game species and never enters a room as gas). A damaged store leaks to space, or, with
/// oxygen in the room and an ignition source, deflagrates through the game's own explosion machinery. The
/// shared gas-store rules are in <see cref="GasStores"/>.</summary>
public static class HydrogenRules
{
    public const string Prefix = "PhobosHydrogenStore", Installed = Prefix + "Installed";
    public const string Record = "ManufacturingHydrogen", Journal = "ManufacturingHydrogenWork", Guard = "ManufacturingHydrogenTransfer";
    public const int Footprint = 2;
    /// <summary>The native RTA canister volume (0.787 m3) at its rated 41.4 MPa and 293 K holds ~27 kg of ideal-gas
    /// hydrogen; authored 24 kg of usable capacity in a 160 kg housing (vessels data pack).</summary>
    public static double CapacityKg => GasStores.HydrogenFamily.SmallCapacityKg;
    public static double DryKg => GasStores.HydrogenFamily.SmallDryKg;
    public static double LeakKgPerHour => GasStores.HydrogenFamily.SmallLeakKgPerHour;
    /// <summary>Higher heating value of hydrogen, 285.83 kJ/mol of water formed (NIST), 141.9 MJ per kilogram.</summary>
    public const double HHVKJPerKg = 141900, OxygenPerHydrogen = 8;
    public const double IgnitionOxygenKPa = GasStores.IgnitionOxygenKPa;
    public const double SmallKg = 2, MediumKg = 8;
    public const string DeflagrationPrefix = GasStores.DeflagrationPrefix;
    /// <summary>The small (original) store; the medium and large sizes are in <see cref="GasStores"/>.</summary>
    public static GasStore Store => GasStores.Hydrogen;
    public static BulkVesselSpec Spec => Store.Spec;
    public static bool IsFamily(string? id) => Store.IsFamily(id);
    /// <summary>Any size of hydrogen store.</summary>
    public static bool AnySize(string? id) => GasStores.HydrogenFamily.InFamily(id);
    public static HydrogenFate Fate(double oxygenKPa, bool fireInRoom, bool hearthWorking, bool sparkingDevice) => GasStores.Fate(oxygenKPa, fireInRoom, hearthWorking, sparkingDevice);
    public static Deflagration Burn(double hydrogenKg, double oxygenAvailableKg) => Store.Burn(hydrogenKg, oxygenAvailableKg);
    public static string DeflagrationDefinition(Deflagration d) => GasStores.DeflagrationDefinition(d);
}
