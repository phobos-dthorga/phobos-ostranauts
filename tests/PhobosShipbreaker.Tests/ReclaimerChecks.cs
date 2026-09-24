using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;

internal static class ReclaimerChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        var recipe = ReclaimerRules.Recipes.Current;
        check(recipe.InputKg == 13 && recipe.Products.Sum(p => p.Count * p.Kg) == 13, "Reclaimer retains all 13 kg");
        check(recipe.Products.Single(p => p.Id == "ItmScrapSteel").Count == 3 &&
            recipe.Products.Single(p => p.Id == "ItmScrapAluminum").Count == 1 &&
            recipe.Products.Single(p => p.Id == ReclaimerRules.Reject).Kg == 9,
            "Recovery follows the authored constituent budget, including unrecovered steel in rejects");
        check(!recipe.Products.Any(p => p.Id == ReclaimerRules.Feedstock || p.Id == ProcessRules.Residue), "No output can be rerolled as feed");
        var wall = ProcessRecipes.WallPanels.Current;
        check(wall.Products.Any(p => p.Id == ReclaimerRules.Feedstock && p.Kg == recipe.InputKg) &&
            !wall.Products.Any(p => p.Id == ProcessRules.Residue), "New producer and actual consumer agree");
        var old = ProcessJob.CreateOrResume(ProcessRecipes.WallPanels, "old", 0, 1, 60, 120);
        check(old.Recipe.Products.Any(p => p.Id == ProcessRules.Residue), "Zero-progress legacy wall job still yields legacy residue");
        var job = ProcessJob.CreateOrResume(ReclaimerRules.Recipes, "packet", 0, 0, 0, 120);
        job.Advance("packet", 30, true, true); job.Advance("packet", 20, false, true);
        check(job.Progress == 30, "Reclaimer blackout earns no work"); job.Pause();
        var resumed = ProcessJob.CreateOrResume(ReclaimerRules.Recipes, "packet", job.Progress, 1, job.Duration, 240);
        check(resumed.Duration == 120 && resumed.Progress == 30, "Reclaimer reload retains old duration despite changed settings");
        throws(() => ProcessJob.CreateOrResume(ReclaimerRules.Recipes, "packet", 30, 1, 0, 120), "Reclaimer never inherits wall-only missing duration exception");
        throws(() => ProcessJob.CreateOrResume(ReclaimerRules.Recipes, "packet", 0, 2, 120, 120), "Wall recipe revision cannot become a reclaimer job");
        foreach (var pair in new[] { (ProcessRules.Residue, 13d), (ReclaimerRules.Feedstock, 13d), (ReclaimerRules.Reject, 9d) })
        {
            check(CollectorRules.Accepts(pair.Item1, pair.Item2, true, true, true), "Collector retains supported stream: " + pair.Item1);
            check(!CollectorRules.Accepts(pair.Item1, pair.Item2 + 1, true, true, true), "Collector rejects incorrect mass: " + pair.Item1);
        }
        check(ReclaimerRules.CoolingBudget(10000, 290, 0, 100, 12, 120, out double rise) &&
            Math.Abs(rise * 10000 * ReclaimerRules.GasHeatCapacity - 1440000) < .0001, "Every batch's 0.4 kWh becomes 1.44 MJ of room heat");
        check(!ReclaimerRules.CoolingBudget(10000, 310, 0, 100, 12, 120, out _), "Hot room blocks the full step before electricity is requested");
        check(!ReclaimerRules.CoolingBudget(10000, 290, 20, 100, 12, 120, out _), "Pending heat from another machine counts toward headroom");
        check(!ReclaimerRules.CoolingBudget(0, 290, 0, 0, 12, 120, out _), "Vacuum is not a free heat sink");
        check(!ReclaimerRules.CoolingBudget(10000, 290, 0, 9.99, 12, 120, out _), "Low pressure stops air-cooled operation");
        foreach (double value in new[] { double.NaN, double.PositiveInfinity, -1d, 3601d })
            check(!ReclaimerRules.CoolingBudget(10000, 290, 0, 100, 12, value, out _), "Invalid power interval is rejected");
    }
}
