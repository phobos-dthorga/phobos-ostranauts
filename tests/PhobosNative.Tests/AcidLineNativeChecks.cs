using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing.Core;

/// <summary>The Lixivar acid line (Manufacturing 0.24.0) against the game's own data: sulfuric acid rides its own network
/// family, the LC-3, the SA-3 and every AT tank size carry an acid port by Framework's rule, the segment installs from
/// HVAC with its economy bills, and a wetted segment's spill conserves mass.</summary>
internal static class AcidLineNativeChecks
{
    internal static void Run(NativeDefinitions d, Action<bool, string> check)
    {
        var family = LineFamilies.For(LiquidStores.SulfuricAcid);
        check(family != null && family.Id == AcidLineRules.FamilyId && family.IsNetwork && family.AdjacencyJoins && family.Label?.Invoke() == "acid line",
            "Sulfuric acid rides the acid line, a network touching equipment joins");
        check(LineFamilies.For("water")?.Id == LineFamilies.ProcessWaterId && LineFamilies.For(ManufacturingRules.Hydrogen)?.Id == LineFamilies.GasId,
            "Assigning acid leaves water and the gases on their own lines");
        var ported = new[] { (LeachRules.Prefix, Equipment.Entry(LeachRules.Prefix).footprint), (AcidPlantRules.Prefix, Equipment.Entry(AcidPlantRules.Prefix).footprint) }
            .Concat(LiquidStores.All.Select(s => (s.Prefix, s.Footprint))).ToArray();
        check(ported.Length == 5, "The LC-3, the SA-3 and the three AT sizes carry acid ports");
        foreach (var (prefix, footprint) in ported)
        {
            var port = LinePorts.Acid(footprint);
            foreach (string form in LineDefinitions.Forms)
            {
                var co = d.Objects[prefix + form];
                check(co.mapPoints.Contains(LinePorts.AcidPoint + "," + port.X + "," + port.Y) && LinePorts.Points(AcidLineRules.FamilyId, prefix + form).Contains(LinePorts.AcidPoint),
                    "The acid port is the +X side, one row below the gas port: " + prefix + form);
            }
            var gas = LinePorts.Gas(footprint);
            check(port.Socket != gas.Socket && port.Y == gas.Y - 16, "The acid port sits one row below the gas port on its own tile: " + prefix);
        }
        foreach (string form in LineDefinitions.Forms)
        {
            var co = d.Objects[AcidLineRules.Prefix + form];
            check(co.strNameFriendly.StartsWith("Phobos' Lixivar Acid Line", StringComparison.Ordinal), "The segment carries the Lixivar name: " + form);
            check(EquipmentSaveUpgrade.Amount(co.aStartingConds, "StatMass") == AcidLineRules.SegmentKg && (form.EndsWith("Dmg", StringComparison.Ordinal) || EquipmentSaveUpgrade.Amount(co.aStartingConds, "StatBasePrice") == 6),
                "A segment weighs one kilogram and costs 6 intact: " + form);
            check(d.Installables.TryGetValue(AcidLineRules.Prefix + form + "Dismantle", out var dismantle) && dismantle.aLootCOs.All(l => l.StartsWith("PhobosAcidLineWaste", StringComparison.Ordinal)),
                "Dismantling returns only retained acid-line waste: " + form);
        }
        check(d.Installables[AcidLineRules.Prefix + "LooseInstall"].strBuildType == InstallMenu.Hvac, "The acid line installs from INSTALL, HVAC");
        check(d.Installables[AcidLineRules.Prefix + "InstalledDmgRepair"].aInputs.Contains("TIsScrapSteel=1x1"), "Repair takes a steel scrap");
        check(Economy.SupplyKeys.SequenceEqual(new[] { AcidLineRules.Prefix }) && Economy.Pack.factionKiosks?.tiers[AcidLineRules.Prefix] == "Neutral",
            "The acid line is Manufacturing's one supply, at the faction kiosks at any standing");

        // The spill: up to the hold-up, from the tank's service contents; mist plus bund is exactly what left service.
        check(AcidLineRules.Spill(0) == (0, 0, 0) && AcidLineRules.Spill(double.NaN) == (0, 0, 0), "A dry or unreadable tank spills nothing");
        foreach (double service in new[] { 0.25, 1, 700 })
        {
            var s = AcidLineRules.Spill(service);
            check(s.Released == Math.Min(service, AcidLineRules.HoldUpKg) && Math.Abs(s.Mist + s.Bund - s.Released) < 1e-12 && Math.Abs(s.Mist - s.Released * LiquidStores.MistFraction) < 1e-15,
                "A wetted segment spills its hold-up at most, the tank's mist fraction into the room and the rest into the bund: " + service);
        }
        check(Math.Abs(Math.PI * 0.0125 * 0.0125 * LiquidStores.AcidDensityKgPerM3 - 0.9012) < 1e-3, "The authored hold-up rounds one metre of 25 mm bore line of 98% acid (0.90 kg)");
    }
}
