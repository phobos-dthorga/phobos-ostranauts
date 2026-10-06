using System;
using System.Collections.Generic;
using System.Linq;
using PhobosManufacturing.Core;

/// <summary>Manufacturing 0.56.0 "Load feed by crew": what crew bring a charge machine, on the shipped recipes.</summary>
internal static class CrewFeedChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var refinery = ChargeCatalog.For(ChargeCatalog.Refinery);
        var products = new HashSet<string>(refinery.All.SelectMany(r => r.Products).Where(p => !ChargeCommodities.Is(p.Id)).Select(p => p.Id), StringComparer.Ordinal);
        var candidates = CrewFeedRules.Candidates(refinery.Available(_ => false), products.Contains);
        string[] Ids(IEnumerable<ChargeRecipe> recipes) => recipes.Select(r => r.Id).ToArray();
        check(Ids(candidates).Contains("hydrates") && Ids(candidates).Contains("scrubber-reactivation"), "A refinery's ore and filter charges are crew charges");
        check(!Ids(candidates).Contains("nickel-steel") && !Ids(candidates).Contains("methane-pyrolysis") && !Ids(candidates).Contains("carbon-burn"),
            "Charges made of the refinery's own products (steel, pyrolysis, carbon burn) stay the player's to start");
        check(!Ids(candidates).Contains("regolith-bake") &&
              Ids(CrewFeedRules.Candidates(refinery.Available(k => k == ChargeCatalog.ChosenPrefix + ChargeCatalog.RegolithBake), products.Contains)).Contains("regolith-bake"),
            "Loose regolith is a crew charge only on a refinery told to take it");
        var none = new Dictionary<string, int>(StringComparer.Ordinal);
        var empty = CrewFeedRules.Plan(candidates, none);
        check(empty.Ready == null && empty.Short.Count == candidates.Count && empty.Short[0].Units == 1, "An empty refinery wants feed, the one-block charges first");
        check(CrewFeedRules.Plan(candidates, new Dictionary<string, int> { ["ItmMineral11"] = 1 }).Ready?.Id == "hydrates", "One block of hydrates is a whole charge");
        var filters = CrewFeedRules.Plan(candidates, new Dictionary<string, int> { ["ItmFilterCO201Dmg"] = 2 });
        check(filters.Ready == null && filters.Short[0].Recipe.Id != "scrubber-reactivation" && filters.Short.First(s => s.Recipe.Id == "scrubber-reactivation").Units == 2,
            "Two spent cartridges are half a reactivation charge: two more are wanted, and a one-block charge is still closer");
        var fermenter = ChargeCatalog.For(ChargeCatalog.Fermenter);
        var mash = CrewFeedRules.Candidates(fermenter.Available(k => k == ChargeCatalog.SugarCropsRequirement), _ => false);
        var beets = CrewFeedRules.Plan(mash, new Dictionary<string, int> { ["PhobosVerdemorrowSugarBeets"] = 4 });
        check(beets.Ready == null && beets.Short[0].Recipe.Id == "beet-mash" && beets.Short[0].Missing.Single().Key == "PhobosVerdemorrowSugarBeets" &&
              beets.Short[0].Missing.Single().Value == 2, "Four beets of six: crew bring the last two");
        check(CrewFeedRules.Candidates(fermenter.Available(_ => false), _ => false).Count == 0, "A fermenter without Agriculture's crops has no crew charge");
        check(CrewFeedRules.FeederWants(0) && CrewFeedRules.FeederWants(1) && !CrewFeedRules.FeederWants(CrewFeedRules.FeederWaiting), "Crew keep two remainders waiting in an RM-1");
        check(CrewFeedRules.FeedOrder.StartsWith("Phobos", StringComparison.Ordinal) && CrewFeedRules.LoadRecipe != CrewFeedRules.RemainderRecipe, "Order identities are stable and distinct");
    }
}
