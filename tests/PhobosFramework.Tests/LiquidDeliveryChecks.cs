using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Liquids;

internal static class LiquidDeliveryChecks
{
    internal static void Run(Action<bool, string> check)
    {
        void Near(double a, double b, string name) => check(Math.Abs(a-b) < 1e-9, name);
        Near(LiquidDeliveryBudget.Kilograms(10, .0002, .05, .001), .2, "Partial power limits actual delivery");
        Near(LiquidDeliveryBudget.Kilograms(10, 1, .05, .001), .5, "Flow budget limits excess power");
        foreach (double gap in new[] { -1d, 0, 3601, double.NaN, double.PositiveInfinity })
            Near(LiquidDeliveryBudget.Kilograms(gap, 1, .05, .001), 0, "No catch-up for invalid or unloaded interval");
        Near(LiquidDeliveryBudget.Kilograms(10, 0, .05, .001), 0, "No electricity means no pumped water");
        var sm = new Dictionary<string, Dictionary<string, string>>(); var dm = new Dictionary<string, Dictionary<string, string>>();
        LiquidTransferGuard Guard(Dictionary<string, Dictionary<string, string>> maps) => new(maps, "test", "test.owner");
        var sg = Guard(sm); var dg = Guard(dm);
        var source = new Reservoir("source", 5); var target = new Reservoir("target", 19.9);
        var receipt = LiquidTransferGuard.Commit(source, target, 2, sg, dg);
        Near(receipt.ReceivedKg, .1, "Capacity leaves excess at sender");
        Near(source.QuantityKg + target.QuantityKg, 24.9, "Guarded transfer conserves mass");
        check(!Guard(sm).Protected && !Guard(dm).Protected, "Successful journals permit future work after reload");
        target.Kg = 0; target.FailAfterWrite = true;
        bool threw = false; try { LiquidTransferGuard.Commit(source, target, 1, sg, dg); } catch { threw = true; }
        check(threw && Guard(sm).Protected && Guard(dm).Protected, "Ambiguous adapter failure persists at BOTH endpoints");
        double retained = source.QuantityKg + target.QuantityKg;
        try { LiquidTransferGuard.Commit(source, target, 1, Guard(sm), Guard(dm)); } catch { }
        Near(source.QuantityKg + target.QuantityKg, retained, "Reloaded pending journal prevents a second debit");
        Near(target.QuantityKg, 1, "Evidence retains quantity already accepted before exception");
        var foreign = new Dictionary<string, Dictionary<string,string>> { ["PhobosState.test"] = new() { ["schema"] = "9", ["owner"] = "test.owner", ["data.state"] = "clear" } };
        check(Guard(foreign).Protected && foreign["PhobosState.test"]["schema"] == "9", "Future journal remains untouched");
        sm.Clear(); dm.Clear(); target.FailAfterWrite = false; target.Ship = "other";
        threw = false; try { LiquidTransferGuard.Commit(source,target,1,sg,dg); } catch { threw = true; }
        check(threw && sm.Count == 0 && dm.Count == 0, "Foreign-ship transfer rejected before journal mutation");
        target.Ship = "ship"; target.FailAfterWrite = true; source.FailAfterWrite = true;
        threw = false; try { LiquidTransferGuard.Commit(source,target,1,sg,dg); } catch { threw = true; }
        check(threw && Guard(sm).Protected && Guard(dm).Protected, "Failure during debit protects both endpoints too");
    }
    private sealed class Reservoir : ILiquidReservoir
    {
        public double Kg; public bool FailAfterWrite; public string Ship = "ship";
        public Reservoir(string id, double kg) { Identity = id; Kg = kg; }
        public string Identity { get; }
        public string ShipId => Ship;
        public string Commodity => "water";
        public double QuantityKg => Kg;
        public double CapacityKg => 20;
        public void SetQuantity(double kg) { Kg = kg; if (FailAfterWrite) throw new InvalidOperationException("Interrupted after native write"); }
    }
}
