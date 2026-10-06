using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing.Core;

/// <summary>Manufacturing 0.56.0: the "Load feed by crew" switch sits on the intact installed charge machines and the
/// RM-1 only; the L2 keeps its own bottle order and no other form or machine gets the switch.</summary>
internal static class CrewFeedNativeChecks
{
    internal static void Run(NativeDefinitions manufacturing, Action<bool, string> check)
    {
        check(manufacturing.Interactions.TryGetValue(CrewFeedRules.FeedOrder, out var order) && order.strRaiseUI == null && order.strTitle.Length > 0,
            "The loading switch is a native interaction that opens no window");
        var expected = new[] { "PhobosVolatilesRefinery", "PhobosReactionMassFeeder" };
        var carrying = manufacturing.Objects.Values.Where(o => o.aInteractions?.Contains(CrewFeedRules.FeedOrder) == true).Select(o => o.strName).ToArray();
        check(carrying.Length == 7 && carrying.All(n => n.EndsWith("Installed", StringComparison.Ordinal)),
            "The loading switch is on the seven intact installed machines only: " + string.Join(", ", carrying));
        check(expected.All(p => carrying.Contains(p + "Installed")), "The V4 and the RM-1 carry the loading switch");
        check(!manufacturing.Objects[FillerRules.Installed].aInteractions.Contains(CrewFeedRules.FeedOrder) &&
              manufacturing.Objects[FillerRules.Installed].aInteractions.Contains(FillerRules.BottleOrder), "The L2 keeps its own bottle order and no loading switch");
    }
}
