using System;
using PhobosShipbreaker.Core;

/// <summary>The crew loading order behind "Load feed by crew": which work each family runs and how far.</summary>
internal static class CrewOrderChecks
{
    internal static void Run(Action<bool, string> check)
    {
        check(CrewOrderRules.FeedRecipe("PhobosShipbreakerInstalled") == "process" && CrewOrderRules.FeedRecipe("PhobosScrapReclaimerInstalled") == "process",
            "D4 and R4 loading orders run the processing work");
        check(CrewOrderRules.FeedRecipe(FurnaceRules.Prefix + "Installed") == "housing", "The F6 loading order runs the housing work");
        check(CrewOrderRules.FeedRecipe(ThawRules.Installed) == "thaw" && CrewOrderRules.FeedRecipe(ThawRules.Installed + "Dmg") == "thaw", "The T2 loading order runs the thaw work");
        foreach (string other in new[] { CollectorRules.Installed, IntakeRules.Grabber + "Installed", FurnaceRules.Radiator + "Installed", IndustrialRules.Prefix + "Installed", "PhobosProcessSiloInstalled", "ItmWall1x1", "", null! })
            check(CrewOrderRules.FeedRecipe(other) == null, "No loading order for equipment without a feed: " + other);
        check(CrewOrderRules.LoadStock == 256, "A loading order runs to the standing-order maximum, so crew keep going until products have nowhere to go");
        check(IndustrialRules.FeedOrder.StartsWith("Phobos", StringComparison.Ordinal) && IndustrialRules.FeedOrder != IndustrialRules.LocalControls,
            "The loading toggle is its own Phobos action");
    }
}
