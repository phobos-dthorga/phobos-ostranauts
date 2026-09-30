using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>The <c>vessels</c> schema: the small size of every bulk vessel or bin family a mod ships, keyed by its
/// definition prefix: what it holds, how much, its dry housing mass, its leak rate when damaged, and for item-grid
/// bins the cells per tile side. Medium and large sizes follow the Framework ladder (<c>Liquids.BulkVesselSizes</c>),
/// which stays in code because existing saves are checked against it. Changing a capacity or dry mass makes an
/// existing vessel Protected with an Accept path; the loader does not hide that.</summary>
public sealed class VesselPack : DataPack
{
    public Dictionary<string, VesselFamilyEntry> families = new(StringComparer.Ordinal);
}

public sealed class VesselFamilyEntry
{
    public string? notes;
    /// <summary>How the owner builds it: <c>gas-store</c>, <c>silo</c>, <c>reservoir</c>, <c>supply</c>, <c>bin</c> or another kind the owner names.</summary>
    public string kind = "vessel";
    /// <summary>The commodity held as a kilogram record; absent for an item-grid bin.</summary>
    public string? commodity;
    /// <summary>Usable capacity of the small size in kilograms; absent for an item-grid bin.</summary>
    public double? capacityKg;
    /// <summary>Dry housing mass of the small size.</summary>
    public double dryKg;
    /// <summary>Kilograms an hour a damaged small size loses; zero for a vessel that does not leak.</summary>
    public double leakKgPerHour;
    /// <summary>Item-grid cells per tile side, for bins.</summary>
    public int? cellsPerTileSide;
}

public sealed class VesselContext
{
    public IReadOnlyCollection<string> Known { get; }
    public IReadOnlyCollection<string>? Kinds { get; set; }
    /// <summary>Kinds that hold items rather than a commodity record (no capacity, cells instead).</summary>
    public IReadOnlyCollection<string> ItemKinds { get; set; } = new[] { "bin" };
    public VesselContext(IReadOnlyCollection<string> known) { Known = known ?? throw new ArgumentNullException(nameof(known)); }
}

public static class VesselSchema
{
    public const string Name = "vessels";
    public const double MaximumCapacityKg = 100000, MaximumDryKg = 10000, MaximumLeakKgPerHour = 1000;
    public const int MaximumCellsPerTileSide = 8;
    public static void Validate(VesselPack pack, VesselContext context)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (context == null) throw new ArgumentNullException(nameof(context));
        foreach (string prefix in context.Known)
            if (!pack.families.ContainsKey(prefix)) throw new ArgumentException(Text.Get("VesselSchema.missing", prefix));
        foreach (var pair in pack.families)
        {
            string prefix = pair.Key; var v = pair.Value;
            if (!context.Known.Contains(prefix)) throw new ArgumentException(Text.Get("VesselSchema.unknown", prefix));
            if (context.Kinds != null && !context.Kinds.Contains(v.kind)) throw new ArgumentException(Text.Get("VesselSchema.kind", prefix, v.kind));
            bool items = context.ItemKinds.Contains(v.kind);
            if (items)
            {
                if (v.capacityKg != null || v.commodity != null) throw new ArgumentException(Text.Get("VesselSchema.bin_fields", prefix));
                if (v.cellsPerTileSide is not int cells || cells < 1 || cells > MaximumCellsPerTileSide) throw new ArgumentException(Text.Get("VesselSchema.cells", prefix, MaximumCellsPerTileSide));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(v.commodity)) throw new ArgumentException(Text.Get("VesselSchema.commodity", prefix));
                if (v.capacityKg is not double capacity || !Finite(capacity) || capacity <= 0 || capacity > MaximumCapacityKg) throw new ArgumentException(Text.Get("VesselSchema.capacity", prefix, MaximumCapacityKg));
                if (v.cellsPerTileSide != null) throw new ArgumentException(Text.Get("VesselSchema.vessel_fields", prefix));
            }
            if (!Finite(v.dryKg) || v.dryKg <= 0 || v.dryKg > MaximumDryKg) throw new ArgumentException(Text.Get("VesselSchema.dry", prefix, MaximumDryKg));
            if (!Finite(v.leakKgPerHour) || v.leakKgPerHour < 0 || v.leakKgPerHour > MaximumLeakKgPerHour) throw new ArgumentException(Text.Get("VesselSchema.leak", prefix, MaximumLeakKgPerHour));
        }
    }
    private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
}
