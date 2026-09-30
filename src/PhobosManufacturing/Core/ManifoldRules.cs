using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Propulsion;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The P1 RCS Propellant Manifold: a passive 1 x 1 valve block installed where a canister would sit, on
/// one of an RCS Intake Regulator's gas-input tiles. It feeds the thrusters from linked hydrogen and methane stores,
/// each switched on or off, drawn before or after the regulator's own canisters. Stores connect within one tile or
/// through a Fennmark propellant line. Every RCS quantity is in nitrogen-equivalent kilograms (Framework).</summary>
public static class ManifoldRules
{
    public const string Prefix = "PhobosPropellantManifold", Installed = Prefix + "Installed";
    public const string Record = "ManufacturingManifold";
    public const int Footprint = 1, MaxSources = 4;
    public const double MachineKg = 10;
    public const string Inlet = "PhobosPropellantIn", StoreOutlet = "PhobosPropellantOut";
    /// <summary>Connections are rechecked this often, never every frame.</summary>
    public const double RecheckSeconds = 5;
    public const int RouteTileLimit = 64;
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    /// <summary>The game gas each store commodity is, for its RCS worth.</summary>
    public static string? Species(string? commodity) => GasStores.FamilyOf(commodity)?.Species;
    public static double Ratio(string? commodity) => RcsPropellant.ExhaustRatio(Species(commodity));
    public static double EquivalentKg(string? commodity, double kg) => kg * Ratio(commodity);
    public static double KilogramsFor(string? commodity, double equivalentKg) => equivalentKg / Ratio(commodity);
}

/// <summary>The Fennmark propellant line: a pipe family on the shared conduit pattern with its own identity, so it
/// never joins coolant or irrigation. Hold-up and pressure drop along the line are neglected (stated).</summary>
public static class PropellantLineRules
{
    public const string Prefix = "PhobosPropellantLine", Installed = Prefix + "Installed";
    public const string Segment = "PhobosPropellantLinePresent", WorkingSegment = "PhobosPropellantLineIntact";
    public const double Kg = 1;
    public static bool IsSegment(string? id) => id == Installed;
}

/// <summary>One linked store and whether it feeds.</summary>
public sealed class ManifoldSource
{
    public string Id { get; }
    public bool Enabled { get; set; }
    public ManifoldSource(string id, bool enabled) { Id = id; Enabled = enabled; }
}

/// <summary>The P1's saved record (schema 1): the master feed switch, the draw order, and up to four linked stores
/// with their own switches (one-sided saved ids). Everything starts off: nothing is burned by surprise.</summary>
public sealed class ManifoldState
{
    public bool On;
    public bool First;
    public List<ManifoldSource> Sources = new();
    private static readonly string[] Keys = { "on", "first", "sources" };
    public static ManifoldState Read(IReadOnlyDictionary<string, string> fields)
    {
        if (fields.Keys.Any(k => Array.IndexOf(Keys, k) < 0)) throw new FormatException("Unknown manifold record field.");
        var s = new ManifoldState { On = Flag(fields, "on"), First = Flag(fields, "first") };
        if (fields.TryGetValue("sources", out var list) && list != "-")
            foreach (string part in list.Split(';'))
            {
                var bits = part.Split('|');
                if (bits.Length != 2 || !SafeId(bits[0]) || (bits[1] != "0" && bits[1] != "1")) throw new FormatException("Invalid manifold source.");
                if (s.Sources.Any(x => x.Id == bits[0])) throw new FormatException("Duplicate manifold source.");
                s.Sources.Add(new ManifoldSource(bits[0], bits[1] == "1"));
            }
        if (s.Sources.Count > ManifoldRules.MaxSources) throw new FormatException("Too many manifold sources.");
        return s;
    }
    public IReadOnlyDictionary<string, string> Save() => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["on"] = On ? "1" : "0", ["first"] = First ? "1" : "0",
        ["sources"] = Sources.Count == 0 ? "-" : string.Join(";", Sources.Select(x => x.Id + "|" + (x.Enabled ? "1" : "0")))
    };
    /// <summary>Adds a store, switched off; false when it is already linked, the list is full or the id is unsafe.</summary>
    public bool Link(string id)
    {
        if (!SafeId(id) || Sources.Count >= ManifoldRules.MaxSources || Sources.Any(x => x.Id == id)) return false;
        Sources.Add(new ManifoldSource(id, false));
        return true;
    }
    public bool Unlink(string id) => Sources.RemoveAll(x => x.Id == id) > 0;
    public bool Switch(string id, bool enabled)
    {
        var source = Sources.FirstOrDefault(x => x.Id == id);
        if (source == null) return false;
        source.Enabled = enabled; return true;
    }
    private static bool Flag(IReadOnlyDictionary<string, string> f, string key) =>
        !f.TryGetValue(key, out var v) ? false : v == "1" ? true : v == "0" ? false : throw new FormatException("Invalid flag: " + key);
    public static bool SafeId(string id) => !string.IsNullOrWhiteSpace(id) && id.IndexOf(';') < 0 && id.IndexOf('|') < 0 && ObjectStateStore.SafeValue(id);
    public static string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);
}
