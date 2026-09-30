using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;

/// <summary>The pure part of a multi-vessel settlement: legs netted per commodity and the checks a vessel must pass.</summary>
internal static class SettlementChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        var needs = SettlementPlan.Build(new[]
        {
            new SettlementLeg("water", SettlementRole.Draw, 0.22), new SettlementLeg("water", SettlementRole.Circulate, 20),
            new SettlementLeg("ammonia", SettlementRole.Draw, 0.03), new SettlementLeg("carbon dioxide", SettlementRole.Deposit, 0.26),
            new SettlementLeg("water", SettlementRole.Deposit, 0.1)
        });
        var water = needs.Single(n => n.Commodity == "water");
        check(needs.Count == 3 && Math.Abs(water.DrawKg - 0.22) < 1e-12 && Math.Abs(water.DepositKg - 0.1) < 1e-12 && Math.Abs(water.CirculateKg - 20) < 1e-12,
            "Legs net per commodity: draws, deposits and a circulating volume on the same vessel");
        check(Math.Abs(water.NetKg + 0.12) < 1e-12 && Math.Abs(water.NeedAvailableKg - 20.22) < 1e-12 && water.NeedHeadroomKg == 0 && water.Changes,
            "A vessel must offer the draw plus the circulating volume; only the net change is settled");
        var loopOnly = SettlementPlan.Build(new[] { new SettlementLeg("water", SettlementRole.Circulate, 20) }).Single();
        check(!loopOnly.Changes && loopOnly.NeedAvailableKg == 20, "A circulating volume alone never changes the vessel");
        var co2 = needs.Single(n => n.Commodity == "carbon dioxide");
        BulkVesselSnapshot Vessel(double service, double catchKg = 0, double reserve = 0, double capacity = 100, bool isProtected = false) =>
            new("v", "s", "water", service, catchKg, reserve, capacity, 1, isProtected);
        check(SettlementPlan.Check(water, Vessel(30), false) == SettlementRefusal.None, "A vessel with enough water settles");
        check(SettlementPlan.Check(water, Vessel(30, reserve: 15), false) == SettlementRefusal.Short, "The vessel's reserve is not available to a charge");
        check(SettlementPlan.Check(water, Vessel(30, isProtected: true), false) == SettlementRefusal.Protected, "A protected vessel refuses");
        check(SettlementPlan.Check(water, Vessel(30), true) == SettlementRefusal.Busy, "A vessel held by another transfer refuses");
        check(SettlementPlan.Check(water, Vessel(30, catchKg: 1), false) == SettlementRefusal.Catch, "Contents in the catch chamber must be recovered first");
        check(SettlementPlan.Check(co2, Vessel(99.8), false) == SettlementRefusal.Full && SettlementPlan.Check(co2, Vessel(99.7), false) == SettlementRefusal.None,
            "A deposit needs room for its net gain");
        throws(() => new SettlementLeg("water", SettlementRole.Draw, 0), "A leg moves a positive mass");
        throws(() => new SettlementLeg("", SettlementRole.Deposit, 1), "A leg names its commodity");
    }
}
