using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>The shared line networks every Phobos mod uses (Framework 0.57.0): the process-water line and the gas line
/// (the Fennmark propellant line, which moved here from Manufacturing with its saved identities unchanged). Both let
/// touching equipment join as if piped, so chains of touching machines and stores reach across the ship. Content mods
/// may add their own families (Manufacturing's acid line) and say which family carries each bulk commodity; a
/// commodity with no family links by touching only.</summary>
public static class LineFamilies
{
    public const string ProcessWaterId = "PhobosFramework.ProcessWater", GasId = "PhobosFramework.GasLine";
    public const string ProcessWaterPrefix = "PhobosProcessWaterLine", GasPrefix = "PhobosPropellantLine";
    public const string ProcessWaterPresent = ProcessWaterPrefix + "Present", ProcessWaterIntact = ProcessWaterPrefix + "Intact";
    public const string GasPresent = GasPrefix + "Present", GasIntact = GasPrefix + "Intact";
    /// <summary>The bulk commodity the process-water line carries (every registered water vessel uses it).</summary>
    public const string Water = LineCommodities.Water;

    public static readonly FluidSegmentFamily ProcessWater = new(ProcessWaterId, c => c.strCODef == ProcessWaterPrefix + "Installed",
        c => LinePorts.Points(ProcessWaterId, c.strCODef), adjacencyJoins: true) { Label = () => Text.Get("LineFamilies.water") };
    public static readonly FluidSegmentFamily Gas = new(GasId, c => c.strCODef == GasPrefix + "Installed",
        c => LinePorts.Points(GasId, c.strCODef), adjacencyJoins: true) { Label = () => Text.Get("LineFamilies.gas") };

    private static readonly Dictionary<string, FluidSegmentFamily> byId = new(StringComparer.Ordinal) { [ProcessWaterId] = ProcessWater, [GasId] = Gas };

    /// <summary>Says which line carries a bulk commodity. The same answer twice is harmless; a different one is refused.</summary>
    public static void Assign(string commodity, FluidSegmentFamily family)
    {
        if (family == null || !family.IsNetwork) throw new ArgumentException("A commodity is carried by a network family.");
        LineCommodities.Assign(commodity, family.Id);
        byId[family.Id] = family;
    }
    /// <summary>The line that carries a commodity, or null (touching only).</summary>
    public static FluidSegmentFamily? For(string? commodity) => ById(LineCommodities.For(commodity));
    public static FluidSegmentFamily? ById(string? id) => id != null && byId.TryGetValue(id, out var family) ? family : null;
}

/// <summary>Which line family carries each bulk commodity, by family id (no game types, so offline checks use it too).</summary>
public static class LineCommodities
{
    public const string Water = "water";
    private static readonly Dictionary<string, string> families = new(StringComparer.Ordinal) { [Water] = LineFamilies.ProcessWaterId };
    public static void Assign(string commodity, string familyId)
    {
        if (string.IsNullOrEmpty(commodity) || string.IsNullOrEmpty(familyId)) throw new ArgumentException("A commodity and a line family are required.");
        if (families.TryGetValue(commodity, out var known) && known != familyId) throw new ArgumentException("The commodity " + commodity + " is already carried by " + known + ".");
        families[commodity] = familyId;
    }
    public static string? For(string? commodity) => commodity != null && families.TryGetValue(commodity, out var id) ? id : null;
}
