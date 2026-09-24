using Phobos.Ostranauts.Framework.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Inventory;

internal static class RecipeChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        var live = ProcessRecipes.WallPanels;
        check(live.Current.Revision == 2 && live.Recipes.Count == 2,
            "R2 producer enabled alongside the usable reclaimer; R1 remains registered");
        var v1 = live.Recipes.Single(r => r.Revision == 1);
        // Historical save contract, independent of whichever revision becomes current later.
        check(v1.Products.Select(p => (p.Id, p.Count, p.Kg)).SequenceEqual(new[] {
            ("ItmPartsMechSmall01", 2, .5), ("ItmScrapAluminum", 2, 1.0),
            ("ItmScrapCarbonFiber", 2, 1.0), ("ItmScrapSteel", 6, 1.0),
            ("PhobosShipbreakerResidue", 1, 13.0)
        }), "Revision 1 retains its exact shipped product identities, counts and masses");

        // Synthetic revision: only test code knows these outputs. Not a gameplay recipe.
        var source = new[] { new ProductSpec("TestOnlyNewResidue", 1, 13), new ProductSpec("ItmScrapSteel", 11, 1) };
        var v2 = new ProcessRecipe(2, ProcessRules.InputKg, source);
        var future = new ProcessRecipeCatalog(2, new[] { v1, v2 });
        source[0] = new ProductSpec("MutatedResidue", 1, 13);
        check(v2.Products[0].Id == "TestOnlyNewResidue", "Caller cannot mutate a published recipe through its source array");
        throws(() => ((IList<ProductSpec>)v2.Products)[0] = source[0], "Published product collection is read-only");
        throws(() => new ProcessRecipeCatalog(2, new[] { v1 }), "Missing active revision is a registration error");
        throws(() => new ProcessRecipeCatalog(1, new[] { v1, v1 }), "Duplicate revisions cannot overwrite historical outputs");
        throws(() => new ProcessRecipe(2, ProcessRules.InputKg, new[] { new ProductSpec("X", 1, 23) }), "Recipe rejects lost mass");
        throws(() => new ProcessRecipe(2, ProcessRules.InputKg, new[] { new ProductSpec("X", 1, 25) }), "Recipe rejects invented mass");
        throws(() => new ProcessRecipe(2, ProcessRules.InputKg, new[] { new ProductSpec("X", -1, -24) }), "Negative counts and masses cannot balance a recipe");

        ProcessJob Restore(double progress, double revision, double duration, double newSeconds = 180) =>
            ProcessJob.CreateOrResume(future, "old-panel", progress, revision, duration, newSeconds);
        var old = Restore(30, 1, 90);
        check(old.Recipe == v1 && old.Duration == 90 && old.Progress == 30,
            "Changing default recipe and cycle time does not change a started job");
        check(Restore(0, 1, 90).Recipe == v1, "Started but zero-progress panel still retains its old recipe");
        var legacy = Restore(15, 1, 0);
        check(legacy.Recipe == v1 && legacy.Duration == 60, "Pre-duration revision-1 save uses historical 60 seconds");
        check(Restore(15, 1, 60, double.NaN).Duration == 60, "Started job does not read new-job duration settings");
        var fresh = Restore(0, 0, 0);
        check(fresh.Recipe == v2 && fresh.Duration == 180 && fresh.Progress == 0,
            "Fresh or explicitly cancelled panel selects new recipe and duration");
        check(Restore(20, 2, 180).Recipe == v2, "Known second revision resumes independently of revision 1");
        foreach (double revision in new[] { -1, 1.5, 3, double.NaN, double.PositiveInfinity })
            throws(() => Restore(0, revision, 0), "Unknown/fractional/nonfinite revision never becomes fresh stock: " + revision);
        foreach (double progress in new[] { -1, 61, double.NaN, double.PositiveInfinity })
            throws(() => Restore(progress, 1, 60), "Corrupt saved progress is not clamped: " + progress);
        foreach (double duration in new[] { -1, .5, 3601, double.NaN, double.PositiveInfinity })
            throws(() => Restore(0, 1, duration), "Corrupt saved duration is not replaced: " + duration);
        throws(() => Restore(1, 0, 0), "Orphaned progress is not restarted");
        throws(() => Restore(0, 0, 90), "Orphaned duration is not restarted");
        throws(() => Restore(0, 2, 0), "New revisions do not inherit the legacy missing-duration exception");

        check(old.MatchesSaved("old-panel", 30, 1, 90), "Live binding matches the exact saved job");
        check(!old.MatchesSaved("another-panel", 30, 1, 90) && !old.MatchesSaved("old-panel", 29, 1, 90) &&
            !old.MatchesSaved("old-panel", 30, 2, 90) && !old.MatchesSaved("old-panel", 30, 1, 180),
            "Changing input, revision, duration or progress cannot be overwritten by a stale session");
        old.Advance("old-panel", 30, true, true);
        old.Pause();
        var resumed = Restore(old.Progress, old.Recipe.Revision, old.Duration);
        resumed.Advance("old-panel", 30, false, true);
        check(resumed.Progress == 60 && !resumed.Complete, "Resumed old job does not credit unpowered time");
        resumed.Advance("old-panel", 30, true, true);
        var completedReload = Restore(resumed.Progress, resumed.Recipe.Revision, resumed.Duration);
        check(completedReload.Complete && completedReload.Recipe == v1, "Completed-but-undelivered reload retains old recipe");
        var delivery = new FakeDelivery { Recipe = completedReload.Recipe, Fits = false };
        check(BatchDelivery.Commit(delivery) == DeliveryResult.Blocked && delivery.InputPresent && delivery.ProductIds.Count == 0,
            "Blocked old-recipe delivery retains input without replacement material");
        delivery.Fits = true;
        BatchDelivery.Commit(delivery);
        check(delivery.ProductIds.Count == 13 && delivery.ProductIds.Count(id => id == "PhobosShipbreakerResidue") == 1 &&
            !delivery.ProductIds.Contains("TestOnlyNewResidue"), "Old completed job delivers old residue even after default revision changes");
        BatchDelivery.Commit(delivery);
        check(delivery.ProductIds.Count == 13, "Retry after old-recipe completion cannot duplicate output");
        var freshDelivery = new FakeDelivery { Recipe = fresh.Recipe };
        BatchDelivery.Commit(freshDelivery);
        check(freshDelivery.ProductIds.Contains("TestOnlyNewResidue") && !freshDelivery.ProductIds.Contains("PhobosShipbreakerResidue"),
            "New recipe's delivery stays separate from historical outputs");
    }
}
