using Phobos.Ostranauts.Framework.Registration;

namespace PhobosShipbreaker.Core;

/// <summary>The S3 process-water silo: a passive 3 x 3 vessel whose water is a Framework bulk vessel record,
/// never a native stat the station kiosk or the reactor could read as fuel. Process grade only; drinking water
/// stays with Ship's Water, which a silo may draw from and return to through its waste tanks.</summary>
public static class SiloRules
{
    public const string Prefix = "PhobosProcessSilo", Installed = Prefix + "Installed";
    /// <summary>The commodity id the R3 reservoir uses too, so guarded transfers between them stay compatible.</summary>
    public const string Commodity = "water";
    public const string Record = "ShipbreakerSilo", Journal = "ShipbreakerSiloWork", Guard = "ShipbreakerSiloTransfer";
    public const int Footprint = 3;
    // Authored: a 3 x 3 sealed vessel of 1,000 kg water (one cubic metre) in a 240 kg housing.
    public const double CapacityKg = 1000, DryKg = 240, Price = 4800;
    public const double WaterPricePerKg = 10, PurchaseStepKg = 10;
    public const int PurchaseSteps = 100;
    /// <summary>Drinking water Ship's Water keeps for the crew when a silo draws from its tanks (a setting).</summary>
    public const double DefaultCrewReserveKg = 50;
    public static readonly double[] ReserveChoices = { 0, 100, 250, 500, 1000 };
    public static readonly double[] TransferChoices = { 50, 100, 250, 500 };
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    /// <summary>A whole number of kilograms within the silo's capacity.</summary>
    public static bool ValidAmount(double kg) => !double.IsNaN(kg) && !double.IsInfinity(kg) && kg >= 0 && kg <= CapacityKg && kg == System.Math.Floor(kg);
}
