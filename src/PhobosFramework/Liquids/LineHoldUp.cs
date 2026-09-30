using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>How much one line segment holds (Framework 0.63.0; owner decisions, 1 October 2026: lines hold their
/// contents until drained, realistic hold-up, any mix of gases in a gas line). A segment is one tile, taken as one metre
/// of pipe; its hold-up is the bore's inner volume filled with the liquid, or with gas at the authored line pressure.
/// Authored geometry and pressure, not a hydraulic simulation: no flow resistance, pressure drop or temperature.</summary>
public static class LineGeometry
{
    /// <summary>The length of pipe one tile holds, authored at one metre (the game gives no tile size in metres).</summary>
    public const double TileMetres = 1;
    /// <summary>The inner bore of the shared process-water, gas and acid lines, authored at 25 mm (1 inch nominal).</summary>
    public const double SharedBoreMm = 25;
    /// <summary>The authored working pressure a gas line holds, 1,000 kPa (10 bar), in the range of the game's own
    /// canister ratings; at the game's reference temperature, <see cref="NativeGasCanister.ReferenceKelvin"/>.</summary>
    public const double GasPressureKPa = 1000;

    /// <summary>The inner volume of one tile of pipe of a bore, in cubic metres.</summary>
    public static double VolumeM3(double boreMm)
    {
        if (!Finite(boreMm) || boreMm <= 0) throw new ArgumentException("Invalid bore.");
        double radius = boreMm / 2000;
        return Math.PI * radius * radius * TileMetres;
    }
    /// <summary>A liquid's hold-up per tile: the bore's volume at the liquid's density (kg per cubic metre).</summary>
    public static double LiquidKgPerTile(double boreMm, double densityKgPerM3) =>
        !Finite(densityKgPerM3) || densityKgPerM3 <= 0 ? throw new ArgumentException("Invalid density.") : VolumeM3(boreMm) * densityKgPerM3;
    /// <summary>A gas's hold-up per tile at the line pressure, by the ideal gas law with the game's own gas constant and
    /// molar mass (kg per mol): n = PV / RT.</summary>
    public static double GasKgPerTile(double boreMm, double kgPerMol) =>
        !Finite(kgPerMol) || kgPerMol <= 0 ? throw new ArgumentException("Invalid molar mass.") :
            GasPressureKPa * VolumeM3(boreMm) / (NativeGasCanister.GasConstantKJPerMolK * NativeGasCanister.ReferenceKelvin) * kgPerMol;
    internal static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
}

/// <summary>One commodity a line family can hold: its hold-up per tile and what happens when it is released. A liquid
/// is drained into a drain canister (<see cref="CanisterKg"/>); a gas is vented, its native species into the room
/// (none: overboard, as hydrogen has no room condition in the game). A liquid may declare a mist: the fraction that
/// reaches the room as a native species when its segment is damaged or destroyed (sulfuric acid).</summary>
public sealed class LineCommodity
{
    public string Name { get; }
    public double KgPerTile { get; }
    public bool Gas { get; }
    /// <summary>The native room species a vented gas becomes, or null (overboard).</summary>
    public string? RoomSpecies { get; }
    /// <summary>What one drain canister holds of a liquid (its volume at the liquid's density); zero for a gas.</summary>
    public double CanisterKg { get; }
    public string? MistSpecies { get; }
    public double MistFraction { get; }
    private LineCommodity(string name, double kgPerTile, bool gas, string? roomSpecies, double canisterKg, string? mistSpecies, double mistFraction)
    {
        if (string.IsNullOrWhiteSpace(name) || !LineGeometry.Finite(kgPerTile) || kgPerTile <= 0 || !LineGeometry.Finite(canisterKg) || canisterKg < 0 ||
            !LineGeometry.Finite(mistFraction) || mistFraction < 0 || mistFraction > 1 || (mistFraction > 0) != (mistSpecies != null) ||
            (roomSpecies != null && !NativeGasCanister.IsRoomSpecies(roomSpecies)) || (mistSpecies != null && !NativeGasCanister.IsRoomSpecies(mistSpecies)))
            throw new ArgumentException("Invalid line commodity " + name + ".");
        Name = name; KgPerTile = kgPerTile; Gas = gas; RoomSpecies = roomSpecies; CanisterKg = canisterKg; MistSpecies = mistSpecies; MistFraction = mistFraction;
    }
    /// <summary>A liquid at a density (kg per cubic metre) in a bore, with an optional mist on damage.</summary>
    public static LineCommodity Liquid(string name, double densityKgPerM3, double boreMm = LineGeometry.SharedBoreMm, string? mistSpecies = null, double mistFraction = 0) =>
        new(name, LineGeometry.LiquidKgPerTile(boreMm, densityKgPerM3), false, null, DrainCanisterRules.VolumeM3 * densityKgPerM3, mistSpecies, mistFraction);
    /// <summary>A gas of a native species (its molar mass from the game) or, with <paramref name="kgPerMol"/>, a gas the
    /// game lacks, vented overboard.</summary>
    public static LineCommodity GasOf(string name, string? roomSpecies, double? kgPerMol = null, double boreMm = LineGeometry.SharedBoreMm)
    {
        double molar = kgPerMol ?? (roomSpecies != null && NativeGasCanister.KgPerMol.TryGetValue(roomSpecies, out var m) ? m : throw new ArgumentException("No molar mass for " + name + "."));
        return new(name, LineGeometry.GasKgPerTile(boreMm, molar), true, roomSpecies, 0, null, 0);
    }
    /// <summary>The kilograms a damaged or destroyed segment's contents release as mist.</summary>
    public double MistKg(double heldKg) => heldKg > 0 && LineGeometry.Finite(heldKg) ? heldKg * MistFraction : 0;
}

/// <summary>The drain canister's authored size (Framework 0.63.0): 20 litres, the common jerrycan size, in a 3 kg
/// lined steel housing; one liquid at a time.</summary>
public static class DrainCanisterRules
{
    public const string Id = "PhobosLineDrainCanister";
    public const double Litres = 20, VolumeM3 = Litres / 1000, DryKg = 3;
    /// <summary>How near a loose canister must lie to the segment being drained, in tiles (Chebyshev).</summary>
    public const int ReachTiles = 2;
}

/// <summary>What one segment holds: kilograms by commodity, and whether its run is closed (drained or being drained,
/// so its store no longer refills it and no link reaches through it). Pure; the saved form is <see cref="Save"/>.</summary>
public sealed class LineMixture
{
    private readonly Dictionary<string, double> kg = new(StringComparer.Ordinal);
    public bool Closed { get; set; }
    public IReadOnlyDictionary<string, double> Kilograms => kg;
    public double TotalKg => kg.Values.Sum();
    public bool Empty => TotalKg <= Tolerance;
    public const double Tolerance = 1e-9;
    public double Of(string commodity) => kg.TryGetValue(commodity, out var v) ? v : 0;
    /// <summary>How full the segment is, as a fraction of its volume: each commodity's mass over its own hold-up.</summary>
    public double Fraction(Func<string, LineCommodity?> commodity)
    {
        double f = 0;
        foreach (var pair in kg) { var c = commodity(pair.Key); if (c != null) f += pair.Value / c.KgPerTile; }
        return f;
    }
    /// <summary>The kilograms of a commodity the segment still has room for.</summary>
    public double Room(LineCommodity c, Func<string, LineCommodity?> commodity) => Math.Max(0, (1 - Fraction(commodity)) * c.KgPerTile);
    public void Add(string commodity, double amount)
    {
        if (!LineGeometry.Finite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (amount <= 0) return;
        kg[commodity] = Of(commodity) + amount;
    }
    /// <summary>Takes up to <paramref name="amount"/> of a commodity and returns what was taken.</summary>
    public double Take(string commodity, double amount)
    {
        if (!LineGeometry.Finite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        double held = Of(commodity), taken = Math.Min(held, amount);
        if (held - taken <= Tolerance) kg.Remove(commodity); else kg[commodity] = held - taken;
        return taken;
    }
    public Dictionary<string, string> Save()
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal) { ["state"] = Closed ? "closed" : "open" };
        int i = 0;
        foreach (var pair in kg.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            fields["c" + i] = pair.Key; fields["kg" + i] = pair.Value.ToString("R", CultureInfo.InvariantCulture); i++;
        }
        return fields;
    }
    public static LineMixture Read(IReadOnlyDictionary<string, string> fields)
    {
        if (!fields.TryGetValue("state", out var state) || state != "open" && state != "closed") throw new ArgumentException("Unknown line contents record.");
        var m = new LineMixture { Closed = state == "closed" };
        int count = (fields.Count - 1) / 2;
        if (fields.Count != 1 + 2 * count) throw new ArgumentException("Unknown line contents record.");
        for (int i = 0; i < count; i++)
        {
            if (!fields.TryGetValue("c" + i, out var name) || !fields.TryGetValue("kg" + i, out var text) ||
                !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double amount) || !LineGeometry.Finite(amount) || amount < 0 || m.kg.ContainsKey(name))
                throw new ArgumentException("Invalid line contents record.");
            if (amount > 0) m.kg[name] = amount;
        }
        return m;
    }
}

/// <summary>The pure arithmetic of filling and draining a run of segments, shared by the native service and the checks.</summary>
public static class LinePlanner
{
    /// <summary>Tops up each segment (in the order given) from the sources' available kilograms of each commodity. A
    /// liquid line holds one commodity; a gas line takes an even share of its remaining volume from each gas on offer,
    /// then whatever is left from any gas that still has some. Returns the kilograms each commodity supplied.</summary>
    public static Dictionary<string, double> Fill(IReadOnlyList<LineMixture> segments, IDictionary<string, double> available, Func<string, LineCommodity?> commodity)
    {
        var drawn = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var segment in segments)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                var offered = available.Where(p => p.Value > LineMixture.Tolerance && commodity(p.Key) != null).Select(p => p.Key).OrderBy(k => k, StringComparer.Ordinal).ToArray();
                if (offered.Length == 0) break;
                double free = Math.Max(0, 1 - segment.Fraction(commodity));
                if (free <= LineMixture.Tolerance) break;
                double share = pass == 0 ? free / offered.Length : free;
                foreach (string name in offered)
                {
                    var c = commodity(name)!;
                    double room = Math.Min(share * c.KgPerTile, segment.Room(c, commodity));
                    double amount = Math.Min(room, available[name]);
                    if (amount <= LineMixture.Tolerance) continue;
                    segment.Add(name, amount); available[name] -= amount;
                    drawn[name] = (drawn.TryGetValue(name, out var d) ? d : 0) + amount;
                    if (pass == 1) break;
                }
            }
        }
        return drawn;
    }
    /// <summary>Drains one liquid from the segments (nearest first) into a canister with <paramref name="roomKg"/> free,
    /// and returns the kilograms taken from each segment.</summary>
    public static double[] Drain(IReadOnlyList<LineMixture> segments, string liquid, double roomKg)
    {
        if (!LineGeometry.Finite(roomKg) || roomKg < 0) throw new ArgumentOutOfRangeException(nameof(roomKg));
        var taken = new double[segments.Count];
        for (int i = 0; i < segments.Count && roomKg > LineMixture.Tolerance; i++)
        {
            taken[i] = segments[i].Take(liquid, roomKg);
            roomKg -= taken[i];
        }
        return taken;
    }
}
