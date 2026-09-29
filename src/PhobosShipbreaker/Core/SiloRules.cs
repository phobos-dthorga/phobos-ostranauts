using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosShipbreaker.Core;

/// <summary>One size of the process-water silo: S3 (3 x 3), S4 (4 x 4) or S5 (5 x 5), scaled from the S3 by
/// Framework's shared size ladder. The S3 keeps its original identity and saved record.</summary>
public sealed class SiloSize
{
    public VesselSize Size { get; }
    public string Prefix { get; }
    public string Installed => Prefix + "Installed";
    public int Footprint { get; }
    public double CapacityKg { get; }
    public double DryKg { get; }
    public double Price { get; }
    public string Record { get; }
    public string Journal { get; }
    public string Guard { get; }
    /// <summary>The translation key of this size's name (its own Rivetline model in the naming map).</summary>
    public string NameKey => "Silo.name" + (Size == VesselSize.Small ? "" : "_" + Size.ToString().ToLowerInvariant());
    public string Art => Prefix;
    internal SiloSize(VesselSize size)
    {
        Size = size; Prefix = BulkVesselSizes.Prefix(SiloRules.Prefix, size); Footprint = BulkVesselSizes.Footprint(SiloRules.Footprint, size);
        CapacityKg = BulkVesselSizes.Scale(SiloRules.CapacityKg, BulkVesselSizes.CapacityFactor(SiloRules.Footprint, size));
        DryKg = BulkVesselSizes.Scale(SiloRules.DryKg, BulkVesselSizes.DryFactor(SiloRules.Footprint, size));
        Price = BulkVesselSizes.Scale(SiloRules.Price, BulkVesselSizes.PriceFactor(SiloRules.Footprint, size));
        Record = BulkVesselSizes.Name(SiloRules.Record, size); Journal = BulkVesselSizes.Name(SiloRules.Journal, size); Guard = BulkVesselSizes.Name(SiloRules.Guard, size);
    }
    public bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
}

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
    /// <summary>Reserve steps for a silo of any size: none, a tenth, a quarter, a half and all of it (the S3's own steps).</summary>
    public static double[] ReserveChoicesFor(double capacityKg) => new[] { 0, .1, .25, .5, 1 }.Select(f => System.Math.Round(capacityKg * f)).ToArray();
    public static readonly double[] TransferChoices = { 50, 100, 250, 500 };
    /// <summary>Any size of the silo family.</summary>
    public static bool IsFamily(string? id) => BulkVesselSizes.InLadder(id, Prefix);
    /// <summary>The S3, S4 and S5, smallest first.</summary>
    public static readonly IReadOnlyList<SiloSize> Sizes = BulkVesselSizes.All.Select(s => new SiloSize(s)).ToArray();
    public static SiloSize? For(string? id) => Sizes.FirstOrDefault(s => s.IsFamily(id));
    /// <summary>A whole number of kilograms within the S3's capacity.</summary>
    public static bool ValidAmount(double kg) => ValidAmount(kg, CapacityKg);
    /// <summary>A whole number of kilograms within a silo's capacity.</summary>
    public static bool ValidAmount(double kg, double capacityKg) => !double.IsNaN(kg) && !double.IsInfinity(kg) && kg >= 0 && kg <= capacityKg && kg == System.Math.Floor(kg);
}
