using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>The <c>equipment</c> schema: the shape of a content mod's machines, keyed by definition prefix: footprint,
/// dry mass, idle and working power, the share of the working power that warms the room, feed-bin cells, art name,
/// INSTALL tab and named use points. Identities, ports, records and behaviour stay in code. A saved machine's mass
/// and footprint are part of its record, so for now the shipped entries are a read-only reference: a player file
/// that changes one is refused, and the loader says so.</summary>
public sealed class EquipmentPack : DataPack
{
    public Dictionary<string, EquipmentEntry> equipment = new(StringComparer.Ordinal);
}

public sealed class EquipmentEntry
{
    public string? notes;
    /// <summary>How the owner builds it (for example <c>charge-machine</c>).</summary>
    public string kind = "machine";
    /// <summary>Square footprint in tiles.</summary>
    public int footprint;
    public double massKg;
    public double idleKW, workingKW;
    /// <summary>The share of the working power that ends in the room's air, 0 to 1.</summary>
    public double roomHeatFraction;
    /// <summary>Feed-bin cells, for machines that take a charge of items; zero for none.</summary>
    public int feedCells;
    public string? art;
    /// <summary>The native INSTALL tab (Framework <c>InstallMenu</c> names, for example <c>APPS</c>).</summary>
    public string installTab = "APPS";
    /// <summary>Named map points in pixels from the machine's centre (use, power points), as the game's mapPoints write them.</summary>
    public Dictionary<string, int[]> points = new(StringComparer.Ordinal);
}

public sealed class EquipmentContext
{
    public IReadOnlyCollection<string> Known { get; }
    public IReadOnlyCollection<string>? Kinds { get; set; }
    public IReadOnlyCollection<string>? InstallTabs { get; set; }
    /// <summary>The shipped entries; any overlay that differs from them is refused while the pack is read-only.</summary>
    public EquipmentPack? Baseline { get; set; }
    public EquipmentContext(IReadOnlyCollection<string> known) { Known = known ?? throw new ArgumentNullException(nameof(known)); }
}

public static class EquipmentSchema
{
    public const string Name = "equipment";
    public const int MaximumFootprint = 12, MaximumFeedCells = 64, MaximumPointOffset = 512;
    public const double MaximumMassKg = 100000, MaximumKW = 10000;
    public static void Validate(EquipmentPack pack, EquipmentContext context)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (context == null) throw new ArgumentNullException(nameof(context));
        foreach (string prefix in context.Known)
            if (!pack.equipment.ContainsKey(prefix)) throw new ArgumentException(Text.Get("EquipmentSchema.missing", prefix));
        foreach (var pair in pack.equipment)
        {
            string prefix = pair.Key; var e = pair.Value;
            if (!context.Known.Contains(prefix)) throw new ArgumentException(Text.Get("EquipmentSchema.unknown", prefix));
            if (context.Kinds != null && !context.Kinds.Contains(e.kind)) throw new ArgumentException(Text.Get("EquipmentSchema.kind", prefix, e.kind));
            if (e.footprint < 1 || e.footprint > MaximumFootprint) throw new ArgumentException(Text.Get("EquipmentSchema.footprint", prefix, MaximumFootprint));
            if (!Finite(e.massKg) || e.massKg <= 0 || e.massKg > MaximumMassKg) throw new ArgumentException(Text.Get("EquipmentSchema.mass", prefix, MaximumMassKg));
            if (!Finite(e.idleKW) || e.idleKW < 0 || !Finite(e.workingKW) || e.workingKW <= 0 || e.workingKW > MaximumKW || e.idleKW > e.workingKW)
                throw new ArgumentException(Text.Get("EquipmentSchema.power", prefix, MaximumKW));
            if (!Finite(e.roomHeatFraction) || e.roomHeatFraction < 0 || e.roomHeatFraction > 1) throw new ArgumentException(Text.Get("EquipmentSchema.heat", prefix));
            if (e.feedCells < 0 || e.feedCells > MaximumFeedCells) throw new ArgumentException(Text.Get("EquipmentSchema.feed", prefix, MaximumFeedCells));
            if (context.InstallTabs != null && !context.InstallTabs.Contains(e.installTab)) throw new ArgumentException(Text.Get("EquipmentSchema.tab", prefix, e.installTab));
            foreach (var point in e.points)
                if (string.IsNullOrWhiteSpace(point.Key) || point.Value == null || point.Value.Length != 2 || point.Value.Any(v => Math.Abs(v) > MaximumPointOffset))
                    throw new ArgumentException(Text.Get("EquipmentSchema.point", prefix, point.Key));
            if (context.Baseline != null && context.Baseline.equipment.TryGetValue(prefix, out var shipped) && !Same(shipped, e))
                throw new ArgumentException(Text.Get("EquipmentSchema.read_only", prefix));
        }
    }
    /// <summary>Whether two entries describe the same machine shape (notes aside).</summary>
    public static bool Same(EquipmentEntry a, EquipmentEntry b) =>
        a.kind == b.kind && a.footprint == b.footprint && a.massKg == b.massKg && a.idleKW == b.idleKW && a.workingKW == b.workingKW &&
        a.roomHeatFraction == b.roomHeatFraction && a.feedCells == b.feedCells && a.art == b.art && a.installTab == b.installTab &&
        a.points.Count == b.points.Count && a.points.All(p => b.points.TryGetValue(p.Key, out var q) && q != null && p.Value.SequenceEqual(q));
    /// <summary>The game's mapPoints entries for an entry, in the order the file lists them ("name,x,y").</summary>
    public static string[] MapPoints(EquipmentEntry e) => e.points.Select(p => p.Key + "," + p.Value[0] + "," + p.Value[1]).ToArray();
    private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
}
