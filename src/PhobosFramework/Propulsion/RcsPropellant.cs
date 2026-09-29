using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;

namespace Phobos.Ostranauts.Framework.Propulsion;

/// <summary>What each gas is worth as cold-gas RCS remass. The game's RCS is species-blind: every kilogram removed
/// gives the same impulse at a fixed exhaust speed of 787 m/s, which is exactly nitrogen expanding to vacuum from
/// about 298 K. Ideal cold-gas expansion gives v_e = sqrt(2γ/(γ−1)·RT/M), so at the same temperature a gas is worth
/// sqrt((γ/(γ−1))/M) relative to nitrogen. All RCS quantities stay in the engine's own unit, nitrogen-equivalent
/// kilograms, so a ship running on nitrogen behaves exactly as the vanilla game.</summary>
public static class RcsPropellant
{
    /// <summary>Heat-capacity ratios near 298 K (standard gas tables; ideal-gas approximation).</summary>
    public static readonly IReadOnlyDictionary<string, double> Gamma = new Dictionary<string, double>(StringComparer.Ordinal)
    {
        ["N2"] = 1.400, ["O2"] = 1.395, ["CO2"] = 1.289, ["CH4"] = 1.304, ["H2"] = 1.405, ["NH3"] = 1.310, ["CO"] = 1.400, ["He2"] = 1.667, ["H2O"] = 1.330
    };
    public const string Reference = "N2";
    private static double Figure(string species) => Math.Sqrt(Gamma[species] / (Gamma[species] - 1) / NativeGasCanister.KgPerMol[species]);
    /// <summary>Impulse per kilogram of <paramref name="species"/> relative to nitrogen; exactly 1 for nitrogen and for
    /// any species without data, so unknown gases keep the vanilla behaviour.</summary>
    public static double ExhaustRatio(string? species)
    {
        if (species == null || species == Reference || !Gamma.ContainsKey(species) || !NativeGasCanister.KgPerMol.ContainsKey(species)) return 1;
        return Figure(species) / Figure(Reference);
    }
    /// <summary>A mixture leaves the nozzle together; its worth is the mass-weighted ratio of its parts (a stated
    /// approximation). An empty mixture is worth 1.</summary>
    public static double MixtureRatio(IEnumerable<KeyValuePair<string, double>> kilograms)
    {
        double mass = 0, weighted = 0;
        foreach (var part in kilograms)
        {
            if (double.IsNaN(part.Value) || double.IsInfinity(part.Value) || part.Value <= 0) continue;
            mass += part.Value; weighted += part.Value * ExhaustRatio(part.Key);
        }
        return mass > 0 ? weighted / mass : 1;
    }
    /// <summary>The game's per-species mole keys ("StatGasMolO2") with each gas's molar mass and worth, built once so a
    /// container reading per physics step neither substrings nor allocates.</summary>
    private static readonly Dictionary<string, (double KgPerMol, double Ratio)> moleKeys = BuildMoleKeys();
    private static Dictionary<string, (double, double)> BuildMoleKeys()
    {
        var keys = new Dictionary<string, (double, double)>(StringComparer.Ordinal);
        foreach (var pair in NativeGasCanister.KgPerMol) keys["StatGasMol" + pair.Key] = (pair.Value, ExhaustRatio(pair.Key));
        return keys;
    }
    /// <summary>The ratio of what a native gas container holds now, from its committed moles.</summary>
    internal static double ContainerRatio(GasContainer gas)
    {
        if (gas?.mapGasMols1 == null) return 1;
        double mass = 0, weighted = 0;
        foreach (var pair in gas.mapGasMols1)
        {
            if (!moleKeys.TryGetValue(pair.Key, out var species)) continue;
            double kg = pair.Value * species.KgPerMol;
            if (double.IsNaN(kg) || double.IsInfinity(kg) || kg <= 0) continue;
            mass += kg; weighted += kg * species.Ratio;
        }
        return mass > 0 ? weighted / mass : 1;
    }

    // Feeds are a copy-on-write array: registration is rare, lookups happen on every RCS query.
    private static volatile IRcsPropellantFeed[] feeds = Array.Empty<IRcsPropellantFeed>();
    /// <summary>Registers a content feed (a manifold drawing from bulk stores). One per owner id.</summary>
    public static void Register(IRcsPropellantFeed feed)
    {
        if (feed == null || string.IsNullOrWhiteSpace(feed.Id)) throw new ArgumentException("Invalid propellant feed.");
        lock (Gamma) feeds = feeds.Where(f => f.Id != feed.Id).Append(feed).ToArray();
    }
    public static void Unregister(string id) { lock (Gamma) feeds = feeds.Where(f => f.Id != id).ToArray(); }
    internal static IRcsPropellantFeed? FeedFor(CondOwner co)
    {
        var current = feeds;
        for (int i = 0; i < current.Length; i++)
        {
            try { if (current[i].IsFeed(co)) return current[i]; }
            catch { }
        }
        return null;
    }
    internal static bool AnyFeeds => feeds.Length > 0;
}

/// <summary>A content object on a regulator's gas-input tile that supplies RCS remass from somewhere the engine
/// cannot see (a bulk store). Every quantity is in nitrogen-equivalent kilograms; the feed converts to its own
/// gases' kilograms with <see cref="RcsPropellant.ExhaustRatio"/>. Offer must never supply more than asked and must
/// take the matching mass from its sources.</summary>
public interface IRcsPropellantFeed
{
    string Id { get; }
    bool IsFeed(CondOwner co);
    /// <summary>Whether this feed is drawn before the regulators' native canisters.</summary>
    bool DrawFirst(CondOwner feed);
    double Offer(CondOwner feed, double equivalentKg);
    double ReserveEquivalentKg(CondOwner feed);
    double CapacityEquivalentKg(CondOwner feed);
}
