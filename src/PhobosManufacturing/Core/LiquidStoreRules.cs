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
/// policy) and only an authored mist fraction reaches the room, as the native species <see cref="MistSpecies"/>.</summary>
public sealed class LiquidFamily
{
    public string SmallPrefix { get; }
    public string Commodity { get; }
    /// <summary>The native room species its mist is (H2SO4 for sulfuric acid).</summary>
    public string MistSpecies { get; }
    public string TextPrefix { get; }
    public string Record { get; }
    public string Journal { get; }
    public string Guard { get; }
    public int SmallFootprint => 2;
    public double SmallCapacityKg => Vessels.Entry(SmallPrefix).capacityKg ?? 0;
    public double SmallDryKg => Vessels.Entry(SmallPrefix).dryKg;
    public double SmallPrice => Economy.Price(SmallPrefix);
    public IReadOnlyList<LiquidStore> Sizes { get; }
    public LiquidFamily(string smallPrefix, string commodity, string mistSpecies, string textPrefix, string record, string journal, string guard)
    {
        if (!NativeGasCanister.IsRoomSpecies(mistSpecies)) throw new ArgumentException("A liquid's mist must be a game room species.");
        SmallPrefix = smallPrefix; Commodity = commodity; MistSpecies = mistSpecies; TextPrefix = textPrefix; Record = record; Journal = journal; Guard = guard;
        Sizes = BulkVesselSizes.All.Select(s => new LiquidStore(this, s)).ToArray();
    }
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

/// <summary>The liquid store families (one so far: sulfuric acid) and their shared rules.</summary>
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
    public static readonly LiquidFamily AcidFamily = new("PhobosAcidTank", SulfuricAcid, "H2SO4", "Acid", "ManufacturingAcid", "ManufacturingAcidWork", "ManufacturingAcidTransfer");
    public static readonly IReadOnlyList<LiquidFamily> Families = new[] { AcidFamily };
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
    /// <summary>The mist a damaged tank releases from its service contents.</summary>
    public static double MistKg(double serviceKg) => !ManufacturingRules.Finite(serviceKg) || serviceKg <= 0 ? 0 : serviceKg * MistFraction;
}
