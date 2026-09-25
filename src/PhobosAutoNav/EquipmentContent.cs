using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal static class EquipmentContent
{
    internal const string Base = "PhobosAutoNavBoard", Residue = "PhobosAutoNavBoardResidue", Offcut = "PhobosAutoNavBoardOffcut";
    internal static string Status { get; private set; } = Text.Get("EquipmentContent.awaiting_content_registration");
    internal static void Register()
    {
        try
        {
            Text.EnsureLoaded();
            foreach (string id in new[] { NavigationService.ModuleId, NavigationService.DamagedId })
            {
                if (!DataHandler.dictCOOverlays.TryGetValue(id, out var overlay)) continue;
                var localized = NativeDefinitions.Clone(overlay);
                localized.strNameFriendly = Text.Get("Overlay." + id + ".name");
                localized.strNameShort = localized.strNameFriendly;
                localized.strDesc = Text.Get("Overlay." + id + ".description");
                DataHandler.dictCOOverlays[id] = localized;
            }
            Prepare(Plugin.SalvageEnabled.Value, Plugin.SalvageChance.Value).Publish();
            var mod = DataHandler.dictModInfos.Values.FirstOrDefault(m => m.strName == "Phobos Auto Nav" && !m.GetIsDisabled());
            if (mod == null) throw new InvalidOperationException(Text.Get("EquipmentContent.enable_the_matching_phobos_auto_nav_native"));
            string path = Path.Combine(mod.GetDirectory(), "framework", "recipes.json");
            ConstructionRegistry.RegisterPack(Plugin.Id, path);
            Status = Text.Get("EquipmentContent.economy_and_maintenance_definitions_registered");
        }
        catch (Exception ex) { Status = Text.Get("EquipmentContent.economy_registration_failed", ex.Message); throw; }
    }

    internal static NativeDefinitions Prepare(bool salvageEnabled = true, double salvageChance = EquipmentRules.SalvageChance)
    {
        var d = new NativeDefinitions();
        foreach (bool damaged in new[] { false, true })
        {
            string id = Base + (damaged ? "Dmg" : ""), module = NavigationService.ModuleId + (damaged ? "Dmg" : "");
            var co = NativeDefinitions.Clone(DataHandler.dictCOs[damaged ? "ItmNavModMoboDmg" : "ItmNavModMobo"]);
            co.strName = id;
            co.strNameFriendly = co.strNameShort = Text.Get("Overlay." + module + ".name");
            co.strDesc = Text.Get("Overlay." + module + ".description");
            co.aInteractions = new[] { "DropItem", "PickupItem" };
            MaintenanceDefinitions.SetStat(co, "StatBasePrice", damaged ? EquipmentRules.BrokenPrice : EquipmentRules.FunctionalPrice);
            MaintenanceDefinitions.SetStat(co, "StatMass", EquipmentRules.ModuleMassKg);
            MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", EquipmentRules.RepairProgress);
            co.aUpdateCommands = damaged ? new[] { "Destructable,StatDamage,ACTDefaultDestroy,StatDamageMax,1.0" } :
                new[] { "Destructable,StatDamage,PhobosAutoNavDamage,StatDamageMax,1.0" };
            d.Objects.Add(id, co);
            d.Loot.Add(module, new Loot { strName = module, strType = "item", aCOs = new[] { module + "=1x1" }, aLoots = Array.Empty<string>() });
            if (damaged)
            {
                var repair = MaintenanceDefinitions.Work(id + "Repair", id, "ACTRepairTEMP", "TIsRepairableNotContained", "CTRL");
                repair.strJobType = "repair"; repair.strAllowLootCTsThem = "CONDRepairProgressx5";
                repair.strProgressStat = "StatRepairProgress";
                repair.aInputs = new[] { EquipmentRules.ElectronicsTrigger + "=1x" + EquipmentRules.RepairElectronicsCount };
                repair.aLootCOs = new[] { NavigationService.ModuleId };
                d.Installables.Add(repair.strName, repair);
                MaintenanceDefinitions.ReturnRepairMaterials(d, repair);
            }
            else MaintenanceDefinitions.Restore(d, id, "CTRL");
            MaintenanceDefinitions.Dismantle(d, id, EquipmentRules.DismantleProgress, new[] { Residue }, "CTRL");
            EquipmentSaveUpgrade.Register(d, module, id, legacyRepair: EquipmentRules.LegacyRepairProgress);
            MaintenanceDefinitions.LegacyFinish(module, "MSNavModMoboDismantle", "MS" + id + "Dismantle");
            if (damaged) MaintenanceDefinitions.LegacyFinish(module, "MSNavModMoboRepair", "MS" + id + "Repair");
        }
        d.Interactions.Add("PhobosAutoNavModeDamage", new JsonInteraction { strName = "PhobosAutoNavModeDamage",
            strThemType = "Self", bIgnoreFeelings = true, objLootModeSwitch = NavigationService.DamagedId, aLootItms = Array.Empty<string>() });
        d.Loot.Add("PhobosAutoNavDamage", new Loot { strName = "PhobosAutoNavDamage", strType = "interaction",
            aCOs = new[] { "PhobosAutoNavModeDamage=1x1" }, aLoots = Array.Empty<string>() });
        MaintenanceDefinitions.Remainder(d, Residue, Text.Get("EquipmentContent.auto_nav_board_residue_kg"), EquipmentRules.ModuleMassKg);
        MaintenanceDefinitions.Remainder(d, Offcut, Text.Get("EquipmentContent.auto_nav_assembly_offcuts_kg"), EquipmentRules.AssemblyOffcutKg);
        Offer("ItmOKLGFixer", "Used", NavigationService.ModuleId, EquipmentRules.FixerWornChance, StockCondition.Worn);
        Offer("ItmOKLGSupplyKioskInv", "Broken", NavigationService.DamagedId, EquipmentRules.KLegBrokenChance, StockCondition.Broken);
        Offer("ItmTraderSanDiegoPolarisInv", "New", NavigationService.ModuleId, EquipmentRules.PolarisPristineChance, StockCondition.Pristine);
        Offer("ItmVORBScrapKioskInv", "Refurb", NavigationService.ModuleId, EquipmentRules.VenusRefurbishedChance, StockCondition.Refurbished);
        foreach (string table in EquipmentRules.SalvageTables)
        {
            double chance = salvageEnabled ? salvageChance : 0;
            double intact = table.EndsWith("Dmg", StringComparison.Ordinal) ? 0 : chance * (1 - EquipmentRules.DamagedSalvageShare);
            AdditiveLoot.SetItemChoice(d, table, "PhobosAutoNavSalvage_" + table, new Dictionary<string, double>
            {
                [NavigationService.ModuleId] = intact,
                [NavigationService.DamagedId] = chance - intact
            });
        }
        return d;

        void Offer(string merchant, string tag, string item, double chance, StockCondition condition) =>
            MarketStock.Add(d, merchant, "PhobosAutoNavStock_" + tag, item, chance, condition);
    }
}
