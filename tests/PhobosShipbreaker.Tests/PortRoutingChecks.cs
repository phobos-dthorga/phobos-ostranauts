using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Inventory;
using PhobosShipbreaker.Core;

internal static class PortRoutingChecks
{
    internal static void Run(Action<bool, string> check)
    {
        check(RoutingRules.CanConnect(RoutingRules.Processor, ReclaimerRules.Installed), "Direct processor-to-feed route");
        check(RoutingRules.CanConnect(CollectorRules.Installed, ReclaimerRules.Installed), "Collector can buffer reclaimer feed");
        check(RoutingRules.CanConnect(ReclaimerRules.Installed, CollectorRules.Installed), "Reclaimer output can use the existing collector route");
        check(!RoutingRules.CanConnect(ReclaimerRules.Installed, ReclaimerRules.Installed) && !RoutingRules.CanConnect(CollectorRules.Installed, CollectorRules.Installed), "Unsupported machine and collector loops have no route");
        check(!RoutingRules.IsReceiver(RoutingRules.Processor) && !RoutingRules.IsSender("foreign"), "No accidental ports on original intake or foreign equipment");
        check(RoutingRules.IsReceiver(ReclaimerRules.Prefix + "LooseDmg"), "Orphaned ports remain addressable after damage/uninstallation");
        var maps = new Dictionary<string, Dictionary<string,string>>();
        var feed = new MaterialPort("reclaimer", RoutingRules.ReclaimerIn, maps);
        var products = new MaterialPort("reclaimer", RoutingRules.SendPort, maps);
        var source = new MaterialPort("processor", RoutingRules.SendPort, new());
        var collector = new MaterialPort("collector", RoutingRules.CollectorIn, new());
        check(PortPairing.TryLink(source, feed, out _) && PortPairing.TryLink(products, collector, out _), "Reclaimer simultaneously holds distinct input and output pairs");
        SavedPortFilter.Set(feed, RoutingRules.FilterIds("feed"));
        SavedPortFilter.Set(collector, RoutingRules.FilterIds("rejects"));
        check(RoutingRules.CompatibleFilter(true, SavedPortFilter.Read(feed)), "Only characterized feed is compatible");
        check(!SavedPortFilter.Read(feed).Allows(ReclaimerRules.Reject, new ItemDefinitionFilter(RoutingRules.FilterIds("all"))), "Rejects cannot be recycled through an input filter");
        SavedPortFilter.Set(feed, RoutingRules.FilterIds("all"));
        check(!RoutingRules.CompatibleFilter(true, SavedPortFilter.Read(feed)), "Tampered broad feed filter blocks instead of admitting old residue");
        SavedPortFilter.Set(feed, new[] { "unrecognised" });
        check(!RoutingRules.CompatibleFilter(false, SavedPortFilter.Read(feed)), "Unknown saved filter does not broaden collector eligibility");
        PortPairing.Unlink(feed, source);
        check(PortPairing.Matches(products, collector), "Unlink input preserves reject destination");
        check(RoutingRules.DemandKW(false, false, 12, 2) == .1 && RoutingRules.DemandKW(false, true, 12, 2) == 2.1 && RoutingRules.DemandKW(true, true, 12, 2) == 14,
            "Receiving has a separate additive energy cost during idle and processing");
        double baseDemand = 12.0 * 2 / 3600, combined = baseDemand * RoutingRules.DemandKW(true, true, 12, 2) / 12;
        check(Math.Abs(combined * 3600 / 14 - 2) < 1e-9, "Added feed energy cannot create extra processing seconds");
        check(ReclaimerRules.CoolingBudget(10000, 300, 0, 100, 14, 2, out var rise) && Math.Abs(rise - 28000 / 207000.0) < 1e-9, "Combined machinery and feed energy enter the heat budget");
        check(RoutingCommand.Parse("phoboscollector status").Action == "foreign", "Old commands remain independently available");
        check(RoutingCommand.Parse("PHOBOSROUTE link FULL-SENDER-ID FULL-RECEIVER-ID").Argument == "FULL-RECEIVER-ID", "Full IDs retain their original spelling");
        foreach (string bad in new[] { "phobosroute link a", "phobosroute unlink a", "phobosroute unlink a both", "phobosroute start", "phobosroute filter a unknown", "phobosroute status a b", "phobosroute controls a" })
            check(RoutingCommand.Parse(bad).Action == "invalid", "Ambiguous command refused: " + bad);
    }
}
