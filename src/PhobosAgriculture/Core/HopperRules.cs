using System;

namespace PhobosAgriculture.Core;

/// <summary>The Verdemorrow Groundwork E-series nutrient hopper (Agriculture 0.27.0, feedstock round three): a passive
/// store of formulated dry crop nutrients, kept as a kilogram record in the three ladder sizes E2, E3 and E4. The
/// refuelling kiosk fills it (owner decision, 30 September 2026), and a W2 within one tile doses its dry-nutrient
/// reservoir from it while mixing. Its contents are the same aggregate nutrient figure the crop model uses.</summary>
public static class HopperRules
{
    /// <summary>The commodity the hopper holds; any registered vessel of it is a hopper as far as a W2 is concerned.</summary>
    public const string Commodity = "crop nutrients";
    public const string Prefix = "PhobosVerdemorrowGroundworkE2";
    public const int SmallFootprint = 2;
    public const string Record = "AgricultureHopper", Journal = "AgricultureHopperWork", Guard = "AgricultureHopperTransfer";
    public const string Kind = "hopper";
    /// <summary>The kiosk sells in whole kilograms.</summary>
    public const double KioskStepKg = 1;
    /// <summary>The kiosk's price per kilogram is the bulk nutrient charge's own (750 cr for 500 g: 1,500 cr/kg), so a
    /// kilogram bought loose or in bulk costs the same. The makeup salts (750 cr/kg) are only half a feed.</summary>
    public static double PricePerKg(double chargePrice, double chargeKg)
    {
        if (!CropState.Finite(chargePrice) || !CropState.Finite(chargeKg) || chargePrice <= 0 || chargeKg <= 0) throw new ArgumentException("Invalid nutrient charge price.");
        return chargePrice / chargeKg;
    }
    /// <summary>How much a W2 may take from a hopper for this mixing step: what the step needs, what the hopper holds.</summary>
    public static double DoseKg(double allowanceKg, double availableKg) =>
        !CropState.Finite(allowanceKg) || !CropState.Finite(availableKg) ? 0 : Math.Max(0, Math.Min(allowanceKg, availableKg));
}
