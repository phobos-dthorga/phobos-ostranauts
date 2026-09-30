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

        // Manufacturing 0.25.0: the line holds its own acid, drained into Framework's canister, which pours into an AT tank's rack.
        var held = LineContents.Families.FirstOrDefault(f => f.Family.Id == AcidLineRules.FamilyId);
        var acid = held?.Of(LiquidStores.SulfuricAcid);
        check(held != null && held.Prefix == AcidLineRules.Prefix && !held.Gas && acid != null, "The acid line is a holding line family for sulfuric acid");
        check(acid != null && Math.Abs(acid.KgPerTile - Math.PI * 0.0125 * 0.0125 * LiquidStores.AcidDensityKgPerM3) < 1e-12 && Math.Abs(acid.KgPerTile - 0.9012) < 1e-3,
            "A segment holds one metre of 25 mm bore of 98% acid (0.90 kg)");
        check(acid != null && acid.MistSpecies == "H2SO4" && acid.MistFraction == LiquidStores.MistFraction && Math.Abs(acid.CanisterKg - 20e-3 * LiquidStores.AcidDensityKgPerM3) < 1e-9,
            "Damage releases the tanks' mist fraction as the game's H2SO4, and a 20 litre canister holds 36.7 kg");
        foreach (string form in new[] { "Installed", "InstalledDmg" })
            check(d.Objects[AcidLineRules.Prefix + form].aInteractions.Contains(LineContents.DrainAction) && d.Objects[AcidLineRules.Prefix + form].aInteractions.Contains(LineContents.ReopenAction) &&
                !d.Objects[AcidLineRules.Prefix + form].aInteractions.Contains(LineContents.VentAction), "An installed acid segment offers Drain and Return to service: " + form);
        foreach (var store in LiquidStores.All)
            foreach (string form in LineDefinitions.Forms)
            {
                var co = d.Objects[store.Prefix + form];
                check(co.strContainerCT == Phobos.Ostranauts.Framework.Items.DrainCanisterDefinitions.RackTrigger && co.nContainerWidth == 2 && co.nContainerHeight == 2 && co.aInteractions.Contains("Inventory"),
                    "An acid tank racks drain canisters and nothing else: " + store.Prefix + form);
            }
    }
}
