using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Processing;

/// <summary>The shared bulk-vessel size ladder and the safe-fill arithmetic for native gas vessels, on numbers alone.</summary>
internal static class VesselSizeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        void Reject(Action action, string label) { bool failed = false; try { action(); } catch { failed = true; } check(failed, label); }
        check(BulkVesselSizes.Prefix("PhobosHydrogenStore", VesselSize.Small) == "PhobosHydrogenStore" && BulkVesselSizes.Name("ManufacturingHydrogen", VesselSize.Small) == "ManufacturingHydrogen",
            "The small size keeps the family's original prefix and record names (saves untouched)");
        check(BulkVesselSizes.Prefix("PhobosHydrogenStore", VesselSize.Medium) == "PhobosHydrogenStoreMedium" && BulkVesselSizes.Name("ManufacturingHydrogen", VesselSize.Large) == "ManufacturingHydrogenLarge",
            "Medium and large append their size to the prefix and to every record name");
        check(BulkVesselSizes.Footprint(2, VesselSize.Medium) == 3 && BulkVesselSizes.Footprint(2, VesselSize.Large) == 4 && BulkVesselSizes.Footprint(3, VesselSize.Large) == 5,
            "Each size is one tile wider than the one below");
        check(BulkVesselSizes.SizeOf("PhobosHydrogenStoreMediumInstalledDmg", "PhobosHydrogenStore") == VesselSize.Medium &&
              BulkVesselSizes.SizeOf("PhobosHydrogenStoreLoose", "PhobosHydrogenStore") == VesselSize.Small &&
              BulkVesselSizes.SizeOf("PhobosHydrogenStoreHugeInstalled", "PhobosHydrogenStore") == null && BulkVesselSizes.SizeOf(null, "PhobosHydrogenStore") == null,
            "A definition's size is read from its exact family prefix; anything else is outside the ladder");
        check(BulkVesselSizes.CapacityFactor(2, VesselSize.Small) == 1 && Math.Abs(BulkVesselSizes.CapacityFactor(2, VesselSize.Medium) - 2.475) < 1e-12 &&
              Math.Abs(BulkVesselSizes.CapacityFactor(2, VesselSize.Large) - 4.8) < 1e-12, "Capacity follows floor area plus 10% per step");
        check(BulkVesselSizes.DryFactor(2, VesselSize.Large) < BulkVesselSizes.CapacityFactor(2, VesselSize.Large) &&
              BulkVesselSizes.PriceFactor(2, VesselSize.Large) < BulkVesselSizes.CapacityFactor(2, VesselSize.Large),
            "Bigger vessels cost less per kilogram of capacity, in housing and in price");
        check(BulkVesselSizes.Round(59.4) == 59 && BulkVesselSizes.Round(396) == 395 && BulkVesselSizes.Round(3333.3) == 3330 && BulkVesselSizes.Round(0.2) == 1,
            "Scaled quantities round to whole kilograms, fives or tens");
        check(BulkVesselSizes.Scale(24, 1) == 24, "The small size's own value is never re-rounded");
        var spec = BulkVesselSizes.Spec("PhobosHydrogenStore", 2, VesselSize.Medium, "hydrogen", 24, 160, "owner", "ManufacturingHydrogen", "ManufacturingHydrogenWork",
            "ManufacturingHydrogenTransfer", VesselDamagePolicy.Leak, 4);
        check(spec.Family == "PhobosHydrogenStoreMedium" && spec.CapacityKg == 59 && spec.DryKg == 305 && spec.Record == "ManufacturingHydrogenMedium" &&
              spec.Journal == "ManufacturingHydrogenWorkMedium" && spec.Guard == "ManufacturingHydrogenTransferMedium" && spec.DamagePolicy == VesselDamagePolicy.Leak,
            "One size's declaration carries its own prefix, scaled ratings and distinct records");
        check(BulkVesselSizes.All.Select(s => BulkVesselSizes.Name("R", s)).Distinct().Count() == 3, "Every size's records are distinct");
        Reject(() => BulkVesselSizes.Footprint(0, VesselSize.Small), "A footprint below one tile is refused");
        Reject(() => BulkVesselSizes.Prefix(" ", VesselSize.Medium), "A blank family is refused");
        Reject(() => BulkVesselSizes.Round(double.NaN), "An invalid quantity is refused");

        // Safe fill: stop at a fraction of the rating, counting everything inside.
        double bottle = NativeGasCanister.CapacityMoles(0.003, 20684, 293);
        check(Math.Abs(NativeGasVessel.Headroom(0, bottle, NativeGasVessel.SafeFillFraction) - bottle * .99) < 1e-9, "An empty bottle takes 99% of its rating");
        check(NativeGasVessel.Headroom(bottle, bottle, .99) == 0 && NativeGasVessel.Headroom(bottle * 1.2, bottle, .99) == 0, "A full or overfilled vessel takes nothing");
        // Filled to 99% in vacuum, a bottle's pressure difference stays below the game's burst threshold (rating + 150 kPa).
        check(20684 * NativeGasVessel.SafeFillFraction < 20684 + 150 && 41400 * NativeGasVessel.SafeFillFraction < 41400 + 150, "The safe fill sits below the burst margin even in vacuum");
        Reject(() => NativeGasVessel.Headroom(0, bottle, 1.01), "A fill above the rating is refused");
        Reject(() => NativeGasVessel.Headroom(-1, bottle, .99), "A negative content is refused");
    }
}
