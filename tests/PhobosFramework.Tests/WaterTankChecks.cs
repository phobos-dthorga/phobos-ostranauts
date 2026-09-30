using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Items;

/// <summary>Framework 0.58.0 water tanks: the S3 to S5 moved from Shipbreaker with their ids, records and ratings, the
/// new S2 one tile narrower on the shared size rule, whole-kilogram amounts, and the crew reserve setting carried over
/// from Shipbreaker's configuration.</summary>
internal static class WaterTankChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var tanks = WaterTanks.All;
        check(tanks.Select(t => t.Model).SequenceEqual(new[] { "S2", "S3", "S4", "S5" }) && tanks.Select(t => t.Footprint).SequenceEqual(new[] { 2, 3, 4, 5 }), "Four sizes, smallest first, one tile apart");
        var s3 = WaterTanks.For("PhobosProcessSiloInstalled");
        check(s3 != null && s3.Model == "S3" && s3.Record == "ShipbreakerSilo" && s3.Journal == "ShipbreakerSiloWork" && s3.Guard == "ShipbreakerSiloTransfer" &&
            s3.CapacityKg == 1000 && s3.DryKg == 240 && s3.Price == 4800, "The S3 keeps its id, record names and ratings from Shipbreaker");
        check(WaterTanks.For("PhobosProcessSiloMediumLoose")?.Model == "S4" && WaterTanks.For("PhobosProcessSiloLargeInstalledDmg")?.Model == "S5" &&
            WaterTanks.For("PhobosProcessSiloCompactLooseDmg")?.Model == "S2" && WaterTanks.For("PhobosIceThawInstalled") == null && WaterTanks.For(null) == null,
            "Every form resolves to its own size; other machines do not");
        check(tanks[2].CapacityKg == 1960 && tanks[2].DryKg == 365 && tanks[3].CapacityKg == 3330 && tanks[3].DryKg == 465, "The S4 and S5 hold what they held under Shipbreaker");
        check(tanks[0].CapacityKg == 400 && tanks[0].DryKg == 125 && tanks[0].Price == 2950, "The S2 follows the size rule one step down: 400 kg in a 125 kg housing, 2,950 cr");
        check(Enumerable.Range(1, tanks.Count - 1).All(i => tanks[i].CapacityKg / tanks[i].Price > tanks[i - 1].CapacityKg / tanks[i - 1].Price), "Bigger tanks cost less per kilogram held");
        check(tanks.Select(t => t.Record).Concat(tanks.Select(t => t.Journal)).Concat(tanks.Select(t => t.Guard)).Distinct().Count() == 12 && tanks.All(t => t.Spec.Owner == WaterTanks.RecordOwner),
            "Every size keeps its own records, under the owner every saved silo carries");
        check(WaterTanks.ValidAmount(0, 1000) && WaterTanks.ValidAmount(1000, 1000) && !WaterTanks.ValidAmount(1000.5, 1000) && !WaterTanks.ValidAmount(-1, 1000) &&
            !WaterTanks.ValidAmount(double.NaN, 1000) && WaterTanks.ValidAmount(3000, 3330) && !WaterTanks.ValidAmount(3000, 1000), "Amounts are whole kilograms within the chosen tank's capacity");
        check(tanks.All(t => WaterTanks.ReserveChoicesFor(t.CapacityKg).All(k => WaterTanks.ValidAmount(k, t.CapacityKg))) && WaterTanks.TransferChoices.All(k => WaterTanks.ValidAmount(k, 1000)),
            "Every panel choice is a valid amount");
        check(WaterTanks.All.Max(t => t.CapacityKg) / WaterTanks.PurchaseStepKg == Math.Ceiling(WaterTanks.All.Max(t => t.CapacityKg) / WaterTanks.PurchaseStepKg), "One station quote can fill the largest empty tank");
        check(TankEconomy.Specs.All(s => s.RepairBill.All(n => n >= 0) && s.Install > 0 && s.Repair > 0 && s.Dismantle > 0 && s.RestoreMinutes > 0), "Every size has positive work and a non-negative repair bill, the S2 too");
        // The crew reserve moves with the tanks: Framework adopts Shipbreaker's value once, and only a numeric, bounded one.
        check(WaterTankSettings.Parse(new[] { "[Mining]", "CrewWaterReserveKg = 7", "[Silo]", "# note", "CrewWaterReserveKg = 125" }) == 125, "The Silo section's reserve is read");
        check(WaterTankSettings.Parse(new[] { "[Mining]", "CrewWaterReserveKg = 7" }) == null && WaterTankSettings.Parse(new[] { "[Silo]", "CrewWaterReserveKg = lots" }) == null &&
            WaterTankSettings.Parse(new[] { "[Silo]", "CrewWaterReserveKg = -3" }) == null, "Another section, unreadable or negative values are ignored");
    }
}
