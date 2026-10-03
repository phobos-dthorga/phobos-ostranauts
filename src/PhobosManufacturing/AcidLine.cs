using System;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Items;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The Lixivar acid line's network family and native definitions (Manufacturing 0.24.0): the segment forms on
/// Framework's shared pattern in the acid lane, its bills from the economy pack, and acid ports on the LC-3, the SA-3
/// and every AT tank size. Sulfuric acid is assigned to this family, so every acid link and pour reaches through
/// touching equipment or the acid line (the owner's link rule), never across open floor.</summary>
internal static class AcidLine
{
    internal static readonly FluidSegmentFamily Family = new(AcidLineRules.FamilyId, c => c.strCODef == AcidLineRules.Installed,
        c => LinePorts.Points(AcidLineRules.FamilyId, c.strCODef), adjacencyJoins: true) { Label = () => Text.Get("AcidLine.label") };

    internal static LineSegmentSpec Spec()
    {
        string name = Text.Get("AcidLine.name");
        return new LineSegmentSpec
        {
            Prefix = AcidLineRules.Prefix, Name = name, DamagedName = name + Text.Get("AcidLine.damaged"), Description = Text.Get("AcidLine.description"),
            Art = Definitions.ImagePath + AcidLineRules.Art, Present = AcidLineRules.Present, Intact = AcidLineRules.Intact, Kg = AcidLineRules.SegmentKg,
            Price = Economy.SupplyPrice(AcidLineRules.Prefix), InstallTab = InstallMenu.Hvac, LooseStack = SharedLines.LooseStack, Layer = LineLayers.Acid,
            Family = AcidLineRules.FamilyId
        };
    }

    internal static void Add(NativeDefinitions d)
    {
        LineFamilies.Assign(LiquidStores.SulfuricAcid, Family);
        var spec = Spec();
        LineDefinitions.Add(d, spec);
        // The line holds its acid until drained (Manufacturing 0.25.0): 0.9 kg a tile, with the tank-damage mist fraction
        // reaching the room as H2SO4 when a segment is damaged or destroyed; crew drain it into Framework's drain canister.
        LineContents.Declare(Family, AcidLineRules.Prefix, new[] { AcidLineRules.HeldAcid() });
        LineContents.OfferActions(d, AcidLineRules.Prefix, gas: false);
        // Bills from the pack, as Framework's own lines: repair with its material, dismantling to retained waste
        // (acid-wetted lining is not recovered as clean metal), the full segment mass.
        var supply = Economy.Pack.supplies[AcidLineRules.Prefix];
        string waste = supply.remainder ?? AcidLineRules.Prefix + "Waste";
        MaintenanceDefinitions.Remainder(d, waste, Text.Get("AcidLine.waste"), AcidLineRules.SegmentKg);
        foreach (string form in LineDefinitions.Forms)
        {
            string id = AcidLineRules.Prefix + form; var co = d.Objects[id];
            if (form.EndsWith("Dmg", StringComparison.Ordinal))
            {
                MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", supply.repairWork);
                var repair = d.Installables[id + "Repair"];
                repair.aInputs = EconomyStock.RepairInputs(supply.repairBill);
                MaintenanceDefinitions.Repair(d, repair);
            }
            MaintenanceDefinitions.Dismantle(d, id, supply.dismantleWork, new[] { waste });
            EquipmentSaveUpgrade.Register(d, id, id);
        }
        // Acid ports by Framework's rule: the +X side, one row below the gas port.
        void Port(string prefix, int footprint)
        {
            var port = LinePorts.Acid(footprint);
            LineDefinitions.AddPort(d, prefix, spec, LinePorts.AcidPoint, port.X, port.Y, port.Socket);
        }
        Port(LeachRules.Prefix, Equipment.Entry(LeachRules.Prefix).footprint);
        Port(AcidPlantRules.Prefix, Equipment.Entry(AcidPlantRules.Prefix).footprint);
        foreach (var store in LiquidStores.All) Port(store.Prefix, store.Footprint);
    }
}
