using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>What the L2 is doing: filling vessels from its sources, or decanting them back into bulk stores.</summary>
public enum FillerMode { Fill, Decant }

/// <summary>The kind of one saved link: a native canister it fills, a native canister it draws from, or a bulk store.</summary>
public enum FillerLinkKind { Target, Source, Store }

/// <summary>The Fennmark L2 Canister Filling Station: a powered 2 x 2 booster that tops up the game's own gas vessels
/// (suit O2 bottles in its rack, and O2, N2 and CO2 canisters installed beside it) to a safe fraction of their rated
/// pressure, from bulk stores or a native canister beside it; in Decant mode it empties them back into bulk stores.
/// The game's own air pump has no cut-off; this one stops at 99%, counting everything inside the vessel.</summary>
public static class FillerRules
{
    public const string Prefix = "PhobosCanisterFiller", Installed = Prefix + "Installed";
    public const string Record = "ManufacturingFiller", Journal = "ManufacturingFillerWork";
    public const int Footprint = 2, RackCells = 4, MaxCanisters = 4, MaxStores = 4;
    public const double MachineKg = 120, Price = 26000, IdleKW = 0.05, WorkingKW = 3;
    public const string RackTrigger = "TIsFitContainerPhobosCanisterFillerRack";
    public const string Inlet = "PhobosGasLineIn";
    /// <summary>Stops every fill at this fraction of the vessel's rating (the Framework safe fill).</summary>
    public const double FillFraction = NativeGasVessel.SafeFillFraction;
    /// <summary>The booster's suction pressure (authored): gas leaves the store at 10 MPa and is compressed to the
    /// vessel's rating. Isothermal compression work, W = (R T / M) ln(P2 / P1), at an authored 50% efficiency.</summary>
    public const double SuctionKPa = 10000, Efficiency = 0.5, ReferenceKelvin = 293;
    /// <summary>All electricity ends as heat in the room: the gas warms on compression and cools to the room.</summary>
    public const double RoomHeatFraction = 1;
    public const double RecheckSeconds = 5;
    public static readonly IReadOnlyList<string> Species = new[] { "O2", "N2", "CO2" };
    /// <summary>The crew order "Keep suit bottles charged": its right-click toggle, its recipe, and the fill below which
    /// crew bring a loose suit bottle to the rack (authored; a bottle above it counts as charged).</summary>
    public const string BottleOrder = "PhobosCanisterFillerBottleOrder", BottleRecipe = "charge-bottles";
    public const double ChargedFraction = 0.9;
    public static bool Charged(double fillFraction) => fillFraction >= ChargedFraction;
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    /// <summary>Electricity to compress one kilogram of a gas from the suction pressure to <paramref name="targetKPa"/>
    /// (never less than a minimum for the valves and cooler when the target is below the suction pressure).</summary>
    public static double KWhPerKg(string species, double targetKPa)
    {
        if (!NativeGasCanister.IsRoomSpecies(species) || !ManufacturingRules.Finite(targetKPa) || targetKPa <= 0) throw new ArgumentException("Invalid compression.");
        double rtPerKg = NativeGasCanister.GasConstantKJPerMolK * ReferenceKelvin / NativeGasCanister.KgPerMol[species];
        double ratio = Math.Max(targetKPa / SuctionKPa, MinimumRatio);
        return rtPerKg * Math.Log(ratio) / Efficiency / 3600;
    }
    /// <summary>A floor on the pressure ratio so a low-rated vessel still costs a little electricity per kilogram.</summary>
    public const double MinimumRatio = 1.5;
    /// <summary>Kilograms a step of <paramref name="suppliedKWh"/> can move, never more than <paramref name="wantedKg"/>.</summary>
    public static double KgFor(double suppliedKWh, double kwhPerKg, double wantedKg) =>
        !ManufacturingRules.Finite(suppliedKWh) || suppliedKWh <= 0 || !(kwhPerKg > 0) || !(wantedKg > 0) ? 0 : Math.Min(wantedKg, suppliedKWh / kwhPerKg);
    /// <summary>The bulk store commodity for a game gas species, or null.</summary>
    public static string? Commodity(string? species) => GasStores.Families.FirstOrDefault(f => f.Species == species)?.Commodity;
}

/// <summary>One saved link on the L2.</summary>
public sealed class FillerLink
{
    public string Id { get; }
    public FillerLinkKind Kind { get; internal set; }
    public bool Enabled { get; internal set; }
    public FillerLink(string id, FillerLinkKind kind, bool enabled) { Id = id; Kind = kind; Enabled = enabled; }
}

/// <summary>The L2's saved choices: mode and links (canisters it fills or draws from, and bulk stores). Running
/// permission is not saved: after a reload the station waits for Start.</summary>
public sealed class FillerState
{
    public FillerMode Mode { get; set; } = FillerMode.Fill;
    private readonly List<FillerLink> links = new();
    public IReadOnlyList<FillerLink> Links => links;
    public double FilledKg { get; set; }
    public double DecantedKg { get; set; }
    public IEnumerable<FillerLink> OfKind(FillerLinkKind kind) => links.Where(l => l.Kind == kind);
    /// <summary>Adds a link switched off (stores) or as a fill target (canisters); false when full or already linked.</summary>
    public bool Link(string id, FillerLinkKind kind)
    {
        if (!ProcessorState.SafeId(id) || links.Any(l => l.Id == id)) return false;
        bool store = kind == FillerLinkKind.Store;
        if (links.Count(l => (l.Kind == FillerLinkKind.Store) == store) >= (store ? FillerRules.MaxStores : FillerRules.MaxCanisters)) return false;
        links.Add(new FillerLink(id, kind, !store));
        return true;
    }
    public bool Unlink(string id) => links.RemoveAll(l => l.Id == id) > 0;
    public bool Switch(string id, bool on) { var l = links.FirstOrDefault(x => x.Id == id); if (l == null) return false; l.Enabled = on; return true; }
    /// <summary>A linked canister becomes a fill target or a source; stores cannot change role.</summary>
    public bool Role(string id, FillerLinkKind kind)
    {
        var l = links.FirstOrDefault(x => x.Id == id);
        if (l == null || l.Kind == FillerLinkKind.Store || kind == FillerLinkKind.Store) return false;
        l.Kind = kind; return true;
    }
    public Dictionary<string, string> Save() => new()
    {
        ["mode"] = Mode == FillerMode.Decant ? "decant" : "fill",
        ["links"] = string.Join(";", links.Select(l => l.Id + "|" + (l.Kind == FillerLinkKind.Store ? "g" : l.Kind == FillerLinkKind.Source ? "s" : "t") + "|" + (l.Enabled ? "1" : "0"))),
        ["filled"] = FilledKg.ToString("R", CultureInfo.InvariantCulture),
        ["decanted"] = DecantedKg.ToString("R", CultureInfo.InvariantCulture)
    };
    public static FillerState Read(IReadOnlyDictionary<string, string> fields)
    {
        var s = new FillerState();
        if (fields.Count != 4 || !fields.TryGetValue("mode", out var mode) || !fields.TryGetValue("links", out var list)) throw new FormatException("Invalid filling station record.");
        s.Mode = mode == "decant" ? FillerMode.Decant : mode == "fill" ? FillerMode.Fill : throw new FormatException("Invalid filling station mode.");
        s.FilledKg = Amount(fields, "filled"); s.DecantedKg = Amount(fields, "decanted");
        foreach (var entry in list.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var part = entry.Split('|');
            if (part.Length != 3 || !ProcessorState.SafeId(part[0]) || s.links.Any(l => l.Id == part[0])) throw new FormatException("Invalid filling station link.");
            var kind = part[1] == "g" ? FillerLinkKind.Store : part[1] == "s" ? FillerLinkKind.Source : part[1] == "t" ? FillerLinkKind.Target : throw new FormatException("Invalid filling station link.");
            s.links.Add(new FillerLink(part[0], kind, part[2] == "1" ? true : part[2] == "0" ? false : throw new FormatException("Invalid filling station link.")));
        }
        if (s.links.Count(l => l.Kind == FillerLinkKind.Store) > FillerRules.MaxStores || s.links.Count(l => l.Kind != FillerLinkKind.Store) > FillerRules.MaxCanisters)
            throw new FormatException("Too many filling station links.");
        return s;
    }
    private static double Amount(IReadOnlyDictionary<string, string> fields, string key) =>
        fields.TryGetValue(key, out var text) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && ManufacturingRules.Finite(v) && v >= 0 ? v : throw new FormatException("Invalid filling station total.");
}
