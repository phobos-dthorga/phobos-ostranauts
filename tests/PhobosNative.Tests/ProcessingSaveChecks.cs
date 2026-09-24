using System;
using System.Linq;
using Newtonsoft.Json;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

internal static class ProcessingSaveChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        var v1 = ProcessRecipes.WallPanels.Current;
        // Local-only synthetic revision, using real scrap definitions to exercise
        // native output dimensions without registering another runtime recipe.
        var v2 = new ProcessRecipe(2, new[] { new ProductSpec("ItmScrapSteel", 24, 1) });
        var future = new ProcessRecipeCatalog(2, new[] { v1, v2 });
        var saved = new JsonItem { strName = ProcessRules.Wall, strID = "saved-panel-123" };
        saved.SetCondAmount(ProcessRules.Progress, 22.25);
        saved.SetCondAmount(ProcessRules.Revision, 1);
        saved.SetCondAmount(ProcessRules.Duration, 90);
        JsonItem RoundTrip(JsonItem item) => JsonConvert.DeserializeObject<JsonItem>(JsonConvert.SerializeObject(item))!;
        ProcessJob Read(JsonItem item) => ProcessJob.CreateOrResume(future, item.strID,
            item.GetCondAmountOverride(ProcessRules.Progress) ?? 0,
            item.GetCondAmountOverride(ProcessRules.Revision) ?? 0,
            item.GetCondAmountOverride(ProcessRules.Duration) ?? 0, 180);
        var loaded = RoundTrip(saved);
        string before = JsonConvert.SerializeObject(loaded);
        var job = Read(loaded);
        check(job.InputId == saved.strID && job.Progress == 22.25 && job.Duration == 90 && job.Recipe == v1,
            "Native condition-override save schema retains panel identity, fractional progress, recipe and duration");
        check(JsonConvert.SerializeObject(loaded) == before, "Reading saved work does not stamp or migrate its fields");
        var oldSizes = ProcessingService.OutputSizes(job.Recipe)!;
        var newSizes = ProcessingService.OutputSizes(future.Current)!;
        check(oldSizes.Count == 13 && newSizes.Count == 24 && oldSizes.All(s => s.Width > 0 && s.Height > 0),
            "Runtime space check uses bound job recipe and native product dimensions, not the new default");

        loaded.aCondOverrides = loaded.aCondOverrides.Where(c => c.CondName != ProcessRules.Duration).ToArray();
        check(Read(RoundTrip(loaded)).Duration == 60, "Native old save without duration resolves to original 60 seconds");
        loaded.SetCondAmount(ProcessRules.Revision, 1.5);
        loaded = RoundTrip(loaded);
        before = JsonConvert.SerializeObject(loaded);
        throws(() => Read(loaded), "Fractional native revision cannot be rounded to a known recipe");
        check(JsonConvert.SerializeObject(loaded) == before, "Unknown native record remains intact after rejection");
        loaded.SetCondAmount(ProcessRules.Progress, 0);
        loaded.SetCondAmount(ProcessRules.Revision, 0);
        loaded.SetCondAmount(ProcessRules.Duration, 0);
        job = Read(RoundTrip(loaded));
        check(job.Recipe == v2 && job.Progress == 0 && job.Duration == 180,
            "Explicitly cleared native fields select current recipe on next Start");
        loaded.SetCondAmount(ProcessRules.Progress, -1);
        throws(() => Read(RoundTrip(loaded)), "Native signed progress override stays invalid instead of resetting");
    }
}
