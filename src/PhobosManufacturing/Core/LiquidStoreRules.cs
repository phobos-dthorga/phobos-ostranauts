using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>One liquid store family (Manufacturing 0.19.0): a liquid the game has no canister or room gas for in bulk,
/// kept as a kilogram record in the ladder sizes. Deliberately not a <see cref="GasFamily"/>: the P1 manifold, the L2
/// filler, the A2 regulator and the RCS feed key off the gas stores, and a gas store's damage releases its contents
/// into the room. A liquid store is bunded instead: damage isolates the contents in the bund (Framework's Isolate
/// policy). A liquid with a native room species (sulfuric acid) mists an authored fraction into the room; a liquid
/// with none that burns (ethanol, Manufacturing 0.38.0) can catch fire instead: an authored share of a damaged tank's
/// contents burns when the room has oxygen and an ignition source, and the bund keeps the rest.</summary>
public sealed class LiquidFamily
{
    public string SmallPrefix { get; }
    public string Commodity { get; }
    /// <summary>The native room species its mist is (H2SO4 for sulfuric acid), or null for a liquid that does not mist.</summary>
    public string? MistSpecies { get; }
    /// <summary>The share of a damaged tank's contents that mists into the room (0 when it does not mist).</summary>
    public double MistFraction { get; }
    /// <summary>How the liquid burns, or null when it does not.</summary>
    public Combustion? Fuel { get; }
    /// <summary>The share of a damaged tank's contents that burns when the room lets it (0 when it does not burn).</summary>
    public double BurnShare { get; }
    /// <summary>Its density, which sets what a line segment holds.</summary>
    public double DensityKgPerM3 { get; }
    /// <summary>An authored station price per kilogram, or null to use the game's own price for <see cref="MistSpecies"/>.</summary>
    public double? PricePerKg { get; }
    /// <summary>Whether the refuelling kiosk sells it; every family is bought back.</summary>
    public bool StationSells { get; }
    /// <summary>The game colour of its row on the right-click card.</summary>
    public string ContentsColor { get; }
    /// <summary>Its warnings' notice id and its maintenance text key (Maintenance.&lt;key&gt;).</summary>
    public string NoticeId { get; }
    public string MaintenanceKey { get; }
    public string TextPrefix { get; }
    public string Record { get; }
    public string Journal { get; }
    public string Guard { get; }
    public int SmallFootprint => 2;
    public double SmallCapacityKg => Vessels.Entry(SmallPrefix).capacityKg ?? 0;
    public double SmallDryKg => Vessels.Entry(SmallPrefix).dryKg;
    public double SmallPrice => Economy.Price(SmallPrefix);
    public IReadOnlyList<LiquidStore> Sizes { get; }
    public LiquidFamily(string smallPrefix, string commodity, string textPrefix, string record, string journal, string guard, double densityKgPerM3,
        string contentsColor, string noticeId, string maintenanceKey, bool stationSells, string? mistSpecies = null, double mistFraction = 0,
        Combustion? fuel = null, double burnShare = 0, double? pricePerKg = null)
    {
        if (mistSpecies != null && !NativeGasCanister.IsRoomSpecies(mistSpecies)) throw new ArgumentException("A liquid's mist must be a game room species.");
        if ((mistSpecies == null) != (mistFraction == 0) || mistFraction < 0 || mistFraction > 1) throw new ArgumentException("A mist needs its species and its fraction.");
        if ((fuel == null) != (burnShare == 0) || burnShare < 0 || burnShare > 1) throw new ArgumentException("A fuel needs its burn share.");
        if (mistSpecies == null && pricePerKg == null) throw new ArgumentException("A liquid without a game species needs its own price.");
        if (!(densityKgPerM3 > 0) || pricePerKg is double p && !(p > 0)) throw new ArgumentException("Invalid liquid family figures.");
        SmallPrefix = smallPrefix; Commodity = commodity; TextPrefix = textPrefix; Record = record; Journal = journal; Guard = guard;
        DensityKgPerM3 = densityKgPerM3; ContentsColor = contentsColor; NoticeId = noticeId; MaintenanceKey = maintenanceKey; StationSells = stationSells;
        MistSpecies = mistSpecies; MistFraction = mistFraction; Fuel = fuel; BurnShare = burnShare; PricePerKg = pricePerKg;
        Sizes = BulkVesselSizes.All.Select(s => new LiquidStore(this, s)).ToArray();
    }
    /// <summary>The mist a damaged tank releases from its service contents.</summary>
    public double MistKg(double serviceKg) => MistSpecies == null || !ManufacturingRules.Finite(serviceKg) || serviceKg <= 0 ? 0 : serviceKg * MistFraction;
    /// <summary>The fuel a damaged tank offers to a fire from its service contents.</summary>
    public double BurnKg(double serviceKg) => Fuel == null || !ManufacturingRules.Finite(serviceKg) || serviceKg <= 0 ? 0 : serviceKg * BurnShare;
    public LiquidStore Small => Sizes[0];
}

/// <summary>One size of one liquid store family: a Framework bulk vessel with the Isolate damage policy.</summary>
public sealed class LiquidStore
{
    public LiquidFamily Family { get; }
    public VesselSize Size { get; }
    public int Footprint { get; }
    public string Prefix { get; }
    public string Installed => Prefix + "Installed";
    public string Commodity => Family.Commodity;
    public double CapacityKg => Spec.CapacityKg;
    public double DryKg => Spec.DryKg;
    public double Price => BulkVesselSizes.Scale(Family.SmallPrice, BulkVesselSizes.PriceFactor(Family.SmallFootprint, Size));
    private BulkVesselSpec? spec; private VesselPack? specFrom;
    public BulkVesselSpec Spec
    {
        get
        {
            var pack = Vessels.Pack;
            if (spec == null || !ReferenceEquals(specFrom, pack))
            {
                spec = BulkVesselSizes.Spec(Family.SmallPrefix, Family.SmallFootprint, Size, Family.Commodity, Family.SmallCapacityKg, Family.SmallDryKg, ManufacturingRules.Owner,
                    Family.Record, Family.Journal, Family.Guard, VesselDamagePolicy.Isolate);
                specFrom = pack;
            }
            return spec;
        }
    }
    public string NameKey => Family.TextPrefix + ".name" + (Size == VesselSize.Small ? "" : "_" + Size.ToString().ToLowerInvariant());
    public string ArtSuffix => Size == VesselSize.Small ? "" : Size.ToString();
    public bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    internal LiquidStore(LiquidFamily family, VesselSize size)
    {
        Family = family; Size = size; Footprint = BulkVesselSizes.Footprint(family.SmallFootprint, size); Prefix = BulkVesselSizes.Prefix(family.SmallPrefix, size);
    }
}

/// <summary>The liquid store families (sulfuric acid; ethanol since Manufacturing 0.38.0) and their shared rules.</summary>
public static class LiquidStores
{
    public const string SulfuricAcid = "sulfuric acid";
    /// <summary>98 wt% sulfuric acid is about 1,836 kg/m3 at 20 C (CRC Handbook of Chemistry and Physics; to be
    /// re-checked before quoting). The acid tanks use the gas stores' 0.787 m3 vessel at an 80% fill: 1,156 kg, authored
    /// 1,150 kg in the vessels data pack. The housing is bunded carbon steel, which 98% acid passivates.</summary>
    public const double AcidDensityKgPerM3 = 1836, VesselVolumeM3 = 0.787, FillFraction = 0.8;
    /// <summary>The authored share of a damaged tank's contents that reaches the room as mist, the rest held by the
    /// bund: 1e-4, the order of the airborne release fractions the US Department of Energy handbook DOE-HDBK-3010-94
    /// gives for spilled liquids (value not re-read; an authored gameplay figure, not a measurement).</summary>
    public const double MistFraction = 1e-4;
    public static readonly LiquidFamily AcidFamily = new("PhobosAcidTank", SulfuricAcid, "Acid", "ManufacturingAcid", "ManufacturingAcidWork", "ManufacturingAcidTransfer",
        AcidDensityKgPerM3, "H2SO4Yellow", "PhobosManufacturing.acid", "acid", stationSells: true, mistSpecies: "H2SO4", mistFraction: MistFraction);
    public const string Ethanol = "ethanol";
    /// <summary>Ethanol at 20 C, 789.3 kg/m3 (CRC Handbook of Chemistry and Physics; to be re-checked before quoting). The
    /// Alembrine Cask tanks (Manufacturing 0.38.0) use the shared 0.787 m3 vessel at the acid tanks' 80% fill: 497 kg,
    /// authored 495 kg in the vessels data pack.</summary>
    public const double EthanolDensityKgPerM3 = 789.3;
    /// <summary>Ethanol's combustion, C2H5OH + 3 O2 -> 2 CO2 + 3 H2O (IUPAC 2013 molar masses): 2.084 kg of oxygen and 1.911 kg
    /// of carbon dioxide a kilogram, releasing its standard enthalpy of combustion, 1,366.8 kJ/mol (NIST Chemistry WebBook),
    /// 29,670 kJ/kg. The water vapour has no game species and leaves with the blast.</summary>
    public static readonly Combustion EthanolCombustion = new(29670, 3 * 31.998 / 46.068, new Dictionary<string, double>(StringComparer.Ordinal) { ["CO2"] = 2 * 44.009 / 46.068 });
    /// <summary>The share of a damaged ethanol tank's contents that burns when the room has oxygen and an ignition source
    /// (authored): a spill catches as a pool fire, and most of it runs into the bund before the fire takes it.</summary>
    public const double EthanolBurnShare = 0.05;
    /// <summary>Ethanol's station price per kilogram (authored; the game has no ethanol): above process water (10 cr/kg),
    /// as a refined fuel, and well below what the bottler makes of it. Stations only buy it back.</summary>
    public const double EthanolPricePerKg = 20;
    public static readonly LiquidFamily EthanolFamily = new("PhobosEthanolTank", Ethanol, "Ethanol", "ManufacturingEthanol", "ManufacturingEthanolWork", "ManufacturingEthanolTransfer",
        EthanolDensityKgPerM3, "BlueXenon", "PhobosManufacturing.ethanol", "ethanol", stationSells: false, fuel: EthanolCombustion, burnShare: EthanolBurnShare,
        pricePerKg: EthanolPricePerKg);
    public static readonly IReadOnlyList<LiquidFamily> Families = new[] { AcidFamily, EthanolFamily };
    public static readonly IReadOnlyList<LiquidStore> All = Families.SelectMany(f => f.Sizes).ToArray();
    private static readonly DefinitionIndex<LiquidStore> index = BuildIndex();
    private static DefinitionIndex<LiquidStore> BuildIndex() { var i = new DefinitionIndex<LiquidStore>(); foreach (var store in All) i.Add(store.Prefix, store); return i; }
    public static LiquidStore? For(string? definition) => index.Get(definition);
    public static bool IsFamily(string? definition) => For(definition) != null;
    public static LiquidFamily? FamilyOf(string? commodity)
    {
        for (int i = 0; i < Families.Count; i++) if (Families[i].Commodity == commodity) return Families[i];
        return null;
    }
    public static bool Holds(string? definition, string commodity) => For(definition)?.Commodity == commodity;
    /// <summary>The capacity the density and fill imply for the vessel, before the authored rounding.</summary>
    public static double AcidCapacityKg => AcidDensityKgPerM3 * VesselVolumeM3 * FillFraction;
    /// <summary>The mist a damaged acid tank releases from its service contents.</summary>
    public static double MistKg(double serviceKg) => AcidFamily.MistKg(serviceKg);
}
