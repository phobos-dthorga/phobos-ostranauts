using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Items;

/// <summary>The line segments Framework owns (Framework 0.57.0): the Fennmark gas line, moved from Manufacturing with
/// every saved identity, condition and name unchanged, and the new process-water line. Both are ordinary 1 x 1 pipe
/// on the shared segment pattern, each in its own lane and draw depth, installed from INSTALL > HVAC. Content mods add
/// ports on their equipment with <see cref="LineDefinitions.AddPort"/> and these specs.</summary>
public static class SharedLines
{
    public const string ImagePath = "phobos/framework/";
    public const string GasArt = "PropellantPipe", ProcessWaterArt = "ProcessWaterPipe";
    public const double Kg = 1;
    public const int LooseStack = 10;
    public static LineSegmentSpec GasSpec() => Spec(LineFamilies.GasPrefix, "SharedLines.gas", GasArt, LineFamilies.GasPresent, LineFamilies.GasIntact, LineLayers.Gas, LineFamilies.GasId);
    public static LineSegmentSpec ProcessWaterSpec() => Spec(LineFamilies.ProcessWaterPrefix, "SharedLines.water", ProcessWaterArt, LineFamilies.ProcessWaterPresent, LineFamilies.ProcessWaterIntact,
        LineLayers.ProcessWater, LineFamilies.ProcessWaterId);
    public static IEnumerable<LineSegmentSpec> All() { yield return GasSpec(); yield return ProcessWaterSpec(); }
    private static LineSegmentSpec Spec(string prefix, string key, string art, string present, string intact, float layer, string family)
    {
        string name = Text.Get(key + "_name");
        return new LineSegmentSpec
        {
            Prefix = prefix, Name = name, DamagedName = name + Text.Get("SharedLines.damaged"), Description = Text.Get(key + "_description"), Art = ImagePath + art,
            Present = present, Intact = intact, Kg = Kg, Price = ItemEconomy.SupplyPrice(prefix), InstallTab = InstallMenu.Hvac, LooseStack = LooseStack, Layer = layer, Family = family
        };
    }
    /// <summary>Publishes both families, then their bills: repair with the pack's material, dismantling to each line's
    /// own retained waste (its full mass), and the saved-object upgrade.</summary>
    internal static void Add(NativeDefinitions d)
    {
        foreach (var spec in All())
        {
            LineDefinitions.Add(d, spec);
            var supply = ItemEconomy.Pack.supplies[spec.Prefix];
            string waste = supply.remainder ?? spec.Prefix + "Waste";
            MaintenanceDefinitions.Remainder(d, waste, Text.Get(spec.Prefix == LineFamilies.GasPrefix ? "SharedLines.gas_waste" : "SharedLines.water_waste"), Kg);
            foreach (string form in LineDefinitions.Forms)
            {
                string id = spec.Prefix + form; var co = d.Objects[id];
                if (form.EndsWith("Dmg", StringComparison.Ordinal))
                {
                    MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", supply.repairWork);
                    var repair = d.Installables[id + "Repair"];
                    repair.aInputs = EconomyStock.RepairInputs(supply.repairBill);
                    MaintenanceDefinitions.ReturnRepairMaterials(d, repair);
                }
                MaintenanceDefinitions.Dismantle(d, id, supply.dismantleWork, new[] { waste });
                EquipmentSaveUpgrade.Register(d, id, id);
            }
        }
    }
}
