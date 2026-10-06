using System;
using System.Collections.Generic;
using System.Linq;

namespace PhobosManufacturing.Core;

/// <summary>What crew bring a charge machine under its "Load feed by crew" order (Manufacturing 0.56.0), on numbers
/// alone. The machine's candidate charges come in its own preference order (the largest charge first, then the lowest
/// revision, as <see cref="ChargeRecipeView.Match(IEnumerable{string}, Func{string, bool})"/> binds them); a charge made
/// of things the machine makes itself (steel from its own ingots) is never a crew charge, as the machine never repeats
/// one by itself. Crew load one charge at a time: once what waits in the machine makes a whole charge they bring no
/// more, and otherwise they bring what the closest charge still lacks.</summary>
public static class CrewFeedRules
{
    /// <summary>The order recipes, saved in the Framework crew-order record. Stable once saves hold them.</summary>
    public const string LoadRecipe = "load-feed", RemainderRecipe = "load-remainders";
    /// <summary>The right-click switch's native interaction id. Stable once saves hold it.</summary>
    public const string FeedOrder = "PhobosManufacturingFeedOrder";
    /// <summary>Remainders crew keep waiting in an RM-1's inventory: half its four cells, so a hand still has room.</summary>
    public const int FeederWaiting = 2;

    /// <summary>The charges crew may load: those with item feed, none of it made by this machine.</summary>
    public static IReadOnlyList<ChargeRecipe> Candidates(IEnumerable<ChargeRecipe> available, Func<string, bool> ownProduct) =>
        available.Where(r => r.ItemInputs.Count > 0 && !r.ItemInputs.Any(i => ownProduct(i.Id)))
            .OrderByDescending(r => r.Units).ThenBy(r => r.Revision).ToArray();

    /// <summary>One charge still short of feed, and what it lacks (identity, units).</summary>
    public sealed class Shortfall
    {
        public ChargeRecipe Recipe { get; }
        public IReadOnlyList<KeyValuePair<string, int>> Missing { get; }
        public int Units => Missing.Sum(m => m.Value);
        internal Shortfall(ChargeRecipe recipe, IReadOnlyList<KeyValuePair<string, int>> missing) { Recipe = recipe; Missing = missing; }
    }
    /// <summary>The plan for the units waiting in the machine: a whole charge (<see cref="Ready"/>), or the charges
    /// still short of feed, closest first.</summary>
    public sealed class FeedPlan
    {
        public ChargeRecipe? Ready { get; }
        public IReadOnlyList<Shortfall> Short { get; }
        internal FeedPlan(ChargeRecipe? ready, IReadOnlyList<Shortfall> shortfalls) { Ready = ready; Short = shortfalls; }
    }

    public static FeedPlan Plan(IReadOnlyList<ChargeRecipe> candidates, IReadOnlyDictionary<string, int> waiting)
    {
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));
        if (waiting == null) throw new ArgumentNullException(nameof(waiting));
        int Have(string id) => waiting.TryGetValue(id, out int n) ? n : 0;
        foreach (var recipe in candidates)
            if (recipe.ItemInputs.All(i => Have(i.Id) >= i.Count)) return new FeedPlan(recipe, Array.Empty<Shortfall>());
        var shortfalls = candidates.Select(r => new Shortfall(r, r.ItemInputs.GroupBy(i => i.Id, StringComparer.Ordinal)
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Sum(i => i.Count) - Have(g.Key))).Where(p => p.Value > 0).ToArray()))
            // A stable sort: equally close charges keep the machine's own preference order.
            .Select((s, n) => (s, n)).OrderBy(p => p.s.Units).ThenBy(p => p.n).Select(p => p.s).ToArray();
        return new FeedPlan(null, shortfalls);
    }

    /// <summary>Whether crew should bring the RM-1 another remainder: fewer than <see cref="FeederWaiting"/> wait in it.</summary>
    public static bool FeederWants(int waiting) => waiting < FeederWaiting;
}
