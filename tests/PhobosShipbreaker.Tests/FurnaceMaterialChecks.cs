using System;
using System.Collections.Generic;
using System.Linq;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Inventory;

internal static class FurnaceMaterialChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string furnace = FurnaceRules.Prefix + "Installed";
        var r4 = new Dictionary<string, Dictionary<string, string>>();
        var f6 = new Dictionary<string, Dictionary<string, string>>();
        var residue = new MaterialPort("R4", RoutingRules.OutputPort(ReclaimerRules.Installed), r4);
        var oldCollector = new MaterialPort("old", RoutingRules.CollectorIn, new());
        var metals = new MaterialPort("R4", RoutingRules.SourcePort(ReclaimerRules.Installed, furnace), r4);
        var input = new MaterialPort("F6", RoutingRules.InputPort(furnace), f6);
        var output = new MaterialPort("F6", RoutingRules.OutputPort(furnace), f6);
        var newCollector = new MaterialPort("new", RoutingRules.CollectorIn, new());
        var cooling = new MaterialPort("F6", "PhobosFurnace.Cooling", f6);
        var radiator = new MaterialPort("radiator", "PhobosFurnace.Cooling", new());
        check(PortPairing.TryLink(residue, oldCollector, out _) && PortPairing.TryLink(metals, input, out _) &&
            PortPairing.TryLink(output, newCollector, out _) && PortPairing.TryLink(cooling, radiator, out _), "R4 residue, aluminium and F6 output/cooling coexist");
        check(!PortPairing.TryLink(metals, new MaterialPort("secondF6", input.PortId, new()), out _) &&
            !PortPairing.TryLink(new MaterialPort("secondR4", metals.PortId, new()), input, out _), "No duplicate consumer or source");
        check(!RoutingRules.CanConnect(ReclaimerRules.Installed, RoutingRules.SendPort, furnace) &&
            RoutingRules.CanConnect(ReclaimerRules.Installed, RoutingRules.MetalsOut, furnace), "Legacy send stays residue-only");
        check(!RoutingRules.CanConnect(RoutingRules.Processor, furnace) && !RoutingRules.CanConnect(CollectorRules.Installed, furnace) &&
            !RoutingRules.CanConnect(furnace, ReclaimerRules.Installed), "Only supported directed furnace routes exist");
        SavedPortFilter.Set(newCollector, RoutingRules.FilterIds("furnace-products"));
        check(RoutingRules.CompatibleFilter(CollectorRules.Installed, SavedPortFilter.Read(newCollector)), "Explicit collector product filter admitted");
        check(!new ItemDefinitionFilter(RoutingRules.FilterIds("all")).Allows(FurnaceRules.Blank), "All does not become a wildcard");
        check(!SavedPortFilter.Read(newCollector).Allows(ReclaimerRules.Reject, new ItemDefinitionFilter(RoutingRules.FilterIds("all"))), "Furnace collector leaves rejects intact");
        SavedPortFilter.Set(input, RoutingRules.FilterIds("all"));
        check(!RoutingRules.CompatibleFilter(furnace, SavedPortFilter.Read(input)), "Broad/old-residue filter fails closed on F6");
        PortPairing.Unlink(input, metals);
        check(PortPairing.Matches(residue, oldCollector) && PortPairing.Matches(output, newCollector) && PortPairing.Matches(cooling, radiator), "Removing aluminium route preserves all other ports");
        foreach (string state in new[] { "Installed", "InstalledDmg", "Loose", "LooseDmg" })
            check(RoutingRules.InputPort(FurnaceRules.Prefix + state) == input.PortId && RoutingRules.OutputPort(FurnaceRules.Prefix + state) == output.PortId,
                "Damage/removal retains logical addresses");
        foreach (string verb in new[] { "unlink", "controls" }) check(RoutingCommand.Parse("phobosroute " + verb + " r4 metals").Argument == "metals", "Explicit metals commands");

        foreach (double mass in new[] { 0, .9, 1, 2, double.NaN, double.PositiveInfinity })
            check(FurnaceMaterialRules.Feed(FurnaceMaterialRules.Aluminium, mass, true, true, true, true) == (mass == 1), "Exact one-kilogram feed only");
        foreach (string id in new[] { "ItmScrapSteel", ProcessRules.Residue, ReclaimerRules.Reject, "itmscrapaluminum" })
            check(!FurnaceMaterialRules.Feed(id, 1, true, true, true, true), "Exact aluminium identity only");
        for (int mask = 0; mask < 16; mask++)
            check(FurnaceMaterialRules.Feed(FurnaceMaterialRules.Aluminium, 1, (mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0, (mask & 8) != 0) == (mask == 15), "Installed, nested, stacked and repair-lot feeds fail closed");
        check(FurnaceMaterialRules.Product(FurnaceRules.Blank, 19) && FurnaceMaterialRules.Product(FurnaceRules.Remainder, 1) &&
            !FurnaceMaterialRules.Product(FurnaceRules.Blank, 18) && !FurnaceMaterialRules.Product(FurnaceRules.Housing, 18), "Only released first-cycle products at exact mass");
        for (int count = 0; count <= 21; count++) check(FurnaceMaterialRules.ChargeFull(count) == (count >= 20), "Twenty-piece stop");
        foreach (FurnacePhase phase in Enum.GetValues(typeof(FurnacePhase)))
        for (int mask = 0; mask < 8; mask++)
            check(FurnaceMaterialRules.Accessible(phase, (mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0) ==
                (phase == FurnacePhase.Idle && mask == 1), "Captive, hot, protected and mutating contents remain inaccessible");
        var expected = new[] { (-2.5, -2.5, 2.5, -2.5), (2.5, -2.5, 2.5, 2.5), (2.5, 2.5, -2.5, 2.5), (-2.5, 2.5, -2.5, -2.5) };
        for (int rotation = 0; rotation < 4; rotation++)
        {
            var a = FurnaceMaterialRules.Point(true, rotation * 90); var b = FurnaceMaterialRules.Point(false, rotation * 90); var e = expected[rotation];
            check(Math.Abs(a.X-e.Item1)+Math.Abs(a.Y-e.Item2)+Math.Abs(b.X-e.Item3)+Math.Abs(b.Y-e.Item4)<1e-8, "Material corners rotate with F6");
        }
        // A blank really fills the existing tray. Four slots are not four blanks.
        var grid = new bool[CollectorRules.StorageSide, CollectorRules.StorageSide];
        check(BatchPlacement.Plan(grid, new[] { new ItemSize(2,2) }) != null &&
            BatchPlacement.Plan(grid, new[] { new ItemSize(2,2), new ItemSize(1,1) }) == null, "Full collector retains second item upstream");
        foreach (bool routed in new[] { false, true })
        foreach (double seconds in new[] { .1, 1, 2, 60 })
        foreach (double headroom in new[] { .001, 1, 30, 18000 })
        {
            double instruments = Math.Min(FurnaceRules.InstrumentKW * seconds, headroom);
            double pumpRequest = routed ? Math.Min(FurnaceCooling.PumpKW * seconds, Math.Max(0, headroom-instruments)) : 0;
            double request = FurnaceMaterialRules.MotorRequest(seconds, RoutingRules.FeedKW, headroom, instruments+pumpRequest);
            for (int fraction = 0; fraction <= 10; fraction++)
            {
                double receipt = (instruments+pumpRequest+request)*fraction/10;
                double pump = routed ? Math.Min(FurnaceCooling.PumpKW * seconds, Math.Max(0, receipt-FurnaceRules.InstrumentKW*seconds)) : 0;
                double motor = FurnaceMaterialRules.MotorReceipt(receipt, request, FurnaceRules.InstrumentKW*seconds, pump);
                var b = new FurnaceBatch { SinkKJ = FurnaceRules.SinkCapacity*(FurnaceRules.SinkMaxK-FurnaceRules.ReferenceK)-headroom };
                double before = b.TotalKJ;
                b.Receive(receipt-pump-motor, seconds); b.SinkKJ += motor;
                if (routed) b.Circulate(seconds, pump);
                check(Math.Abs(b.TotalKJ-before-receipt)<1e-7 && b.SinkK <= FurnaceRules.SinkMaxK+1e-7 && b.HotKJ == 0 && !b.Armed,
                    "Partial receipt conserved in finite sink; feeding never heats a batch");
                check(motor >= 0 && motor <= request+1e-8 && motor/RoutingRules.FeedKW <= seconds, "Paid motor time bounded by actual receipt");
            }
        }
        var clock = new TransferClock("exact-item", RoutingRules.FeedSeconds);
        check(clock.Advance("exact-item", 1, true) && !clock.Complete && !clock.Advance("replacement", 1, true) && clock.Progress == 1, "Replacement cargo cannot inherit work");
        check(clock.Advance("exact-item", 30, false) && clock.Progress == 1 && !clock.Advance("exact-item", 61, true), "No free power or long-interval progress");
        check(new TransferClock("exact-item", RoutingRules.FeedSeconds).Progress == 0, "Reload reconstructs no transfer credit");
    }
}
