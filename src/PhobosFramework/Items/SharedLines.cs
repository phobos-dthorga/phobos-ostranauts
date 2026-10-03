using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Items;

/// <summary>The line segments Framework owns (Framework 0.57.0): the Fennmark gas line, moved from Manufacturing with
/// every saved identity, condition and name unchanged, the new process-water line, and (0.61.0) the Rivetline conveyor
/// belt, which carries items instead of fluid and lies in the lowest lane. Both are ordinary 1 x 1 pipe
/// on the shared segment pattern, each in its own lane and draw depth, installed from INSTALL > HVAC. Content mods add
/// ports on their equipment with <see cref="LineDefinitions.AddPort"/> and these specs.</summary>
public static class SharedLines
{
    public const string ImagePath = "phobos/framework/";
    public const string GasArt = "PropellantPipe", ProcessWaterArt = "ProcessWaterPipe", BeltArt = "ConveyorBelt";
    public const double Kg = 1;
    /// <summary>A belt segment: a steel frame, rollers and belting a tile long.</summary>
    public const double BeltKg = 4;
    public const int LooseStack = 10;
    public static LineSegmentSpec GasSpec() => Spec(LineFamilies.GasPrefix, "SharedLines.gas", GasArt, LineFamilies.GasPresent, LineFamilies.GasIntact, LineLayers.Gas, LineFamilies.GasId);
    public static LineSegmentSpec ProcessWaterSpec() => Spec(LineFamilies.ProcessWaterPrefix, "SharedLines.water", ProcessWaterArt, LineFamilies.ProcessWaterPresent, LineFamilies.ProcessWaterIntact,
        LineLayers.ProcessWater, LineFamilies.ProcessWaterId);
    public static LineSegmentSpec BeltSpec() => Spec(Inventory.BeltNetwork.Prefix, "SharedLines.belt", BeltArt, Inventory.BeltNetwork.Present, Inventory.BeltNetwork.Intact,
        LineLayers.Belt, null, BeltKg, InstallMenu.Miscellaneous);
    public static IEnumerable<LineSegmentSpec> All() { yield return GasSpec(); yield return ProcessWaterSpec(); yield return BeltSpec(); }
    private static LineSegmentSpec Spec(string prefix, string key, string art, string present, string intact, float layer, string? family, double kg = Kg, string? tab = null)
    {
        string name = Text.Get(key + "_name");
        return new LineSegmentSpec
        {
            Prefix = prefix, Name = name, DamagedName = name + Text.Get("SharedLines.damaged"), Description = Text.Get(key + "_description"), Art = ImagePath + art,
            Present = present, Intact = intact, Kg = kg, Price = ItemEconomy.SupplyPrice(prefix), InstallTab = tab ?? InstallMenu.Hvac, LooseStack = LooseStack, Layer = layer, Family = family
        };
    }
    /// <summary>Water's density at 20 C, 998.2 kg per cubic metre (NIST Chemistry WebBook, fluid properties of water),
    /// for the process-water line's hold-up and the drain canister.</summary>
    public const double WaterDensityKgPerM3 = 998.2;
    /// <summary>The gases the shared gas line holds (Framework 0.63.0), by the bulk commodity names content assigns to it,
    /// each at its native species' molar mass; hydrogen has no room condition in the game and vents overboard.</summary>
    public static IEnumerable<LineCommodity> Gases()
    {
        yield return LineCommodity.GasOf("hydrogen", null, Processing.NativeGasCanister.KgPerMol["H2"]);
        yield return LineCommodity.GasOf("methane", "CH4");
        yield return LineCommodity.GasOf("oxygen", "O2");
        yield return LineCommodity.GasOf("nitrogen", "N2");
        yield return LineCommodity.GasOf("carbon dioxide", "CO2");
        yield return LineCommodity.GasOf("ammonia", "NH3");
    }
    /// <summary>Declares what the process-water and gas lines hold (Framework 0.63.0): lines hold their contents until drained.</summary>
    internal static void DeclareHoldUps()
    {
        LineContents.Declare(LineFamilies.ProcessWater, LineFamilies.ProcessWaterPrefix, new[] { LineCommodity.Liquid(LineFamilies.Water, WaterDensityKgPerM3) });
        LineContents.Declare(LineFamilies.Gas, LineFamilies.GasPrefix, Gases());
    }
    /// <summary>Publishes the families, then their bills: repair with the pack's material, dismantling to each line's
    /// own retained waste (its full mass), and the saved-object upgrade. The two pipes also offer the crew's drain (or
    /// vent) and return-to-service actions.</summary>
    internal static void Add(NativeDefinitions d)
    {
        foreach (var spec in All())
        {
            LineDefinitions.Add(d, spec);
            if (spec.Family != null) LineContents.OfferActions(d, spec.Prefix, spec.Family == LineFamilies.GasId);
            var supply = ItemEconomy.Pack.supplies[spec.Prefix];
            string waste = supply.remainder ?? spec.Prefix + "Waste";
            MaintenanceDefinitions.Remainder(d, waste, Text.Get(spec.Prefix == LineFamilies.GasPrefix ? "SharedLines.gas_waste" : spec.Prefix == LineFamilies.ProcessWaterPrefix ? "SharedLines.water_waste" : "SharedLines.belt_waste"), spec.Kg);
            foreach (string form in LineDefinitions.Forms)
            {
                string id = spec.Prefix + form; var co = d.Objects[id];
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
        }
    }
}
