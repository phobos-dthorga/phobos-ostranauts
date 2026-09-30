using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal static class EquipmentContent
{
    internal const string Base = "PhobosAutoNavBoard", Residue = "PhobosAutoNavBoardResidue", Offcut = "PhobosAutoNavBoardOffcut";
    internal const int RemnantStackLimit = 10;
    internal static string Status { get; private set; } = Text.Get("EquipmentContent.awaiting_content_registration");
    private static JsonModInfo? NativePackage => DataHandler.dictModInfos.Values.FirstOrDefault(m => m.strName == "Phobos Auto Nav" && !m.GetIsDisabled());
    internal static bool NativePackageEnabled => NativePackage != null;
    internal static void Register()
    {
        try
        {
            Text.EnsureLoaded();
            var mod = NativePackage;
            if (mod == null) throw new InvalidOperationException(Text.Get("EquipmentContent.enable_the_matching_phobos_auto_nav_native"));
            foreach (string id in new[] { NavigationService.ModuleId, NavigationService.DamagedId, NavigationService.PursuitId, NavigationService.PursuitDamagedId, NavigationService.FireControlId, NavigationService.FireControlDamagedId })
            {
                if (!DataHandler.dictCOOverlays.TryGetValue(id, out var overlay)) continue;
                var localized = NativeDefinitions.Clone(overlay);
                localized.strNameFriendly = Text.Get("Overlay." + id + ".name");
                localized.strNameShort = localized.strNameFriendly;
                localized.strDesc = Text.Get("Overlay." + id + ".description");
                DataHandler.dictCOOverlays[id] = localized;
            }
            Prepare(Plugin.SalvageEnabled.Value, Plugin.SalvageChance.Value).Publish();
            string path = Path.Combine(mod.GetDirectory(), "framework", "recipes.json");
            ConstructionRegistry.RegisterPack(Plugin.Id, path);
            Status = Text.Get("EquipmentContent.economy_and_maintenance_definitions_registered");
        }
        catch (Exception ex) { Status = Text.Get("EquipmentContent.economy_registration_failed", ex.Message); throw; }
    }

    internal static NativeDefinitions Prepare(bool salvageEnabled = true, double? salvageChance = null)
    {
        var d = new NativeDefinitions();
        foreach (int model in new[] { 1, 2, 3 })
        {
            bool pursuit = model != 1;
            string board = model == 3 ? "PhobosFireControlBoard" : pursuit ? "PhobosPursuitBoard" : Base;
            string prefix = model == 3 ? "PhobosFireControl" : pursuit ? "PhobosPursuit" : "PhobosAutoNav";
            string moduleType = model == 3 ? NavigationService.FireControlId : pursuit ? NavigationService.PursuitId : NavigationService.ModuleId;
            foreach (bool damaged in new[] { false, true })
            {
                string id = board + (damaged ? "Dmg" : ""), module = moduleType + (damaged ? "Dmg" : "");
                var co = NativeDefinitions.Clone(DataHandler.dictCOs[damaged ? "ItmNavModMoboDmg" : "ItmNavModMobo"]);
                co.strName = id;
                co.strNameFriendly = co.strNameShort = Text.Get("Overlay." + module + ".name");
                co.strDesc = Text.Get("Overlay." + module + ".description");
                co.aInteractions = new[] { "DropItem", "PickupItem" };
                var entry = AutoNavEconomy.Entry(moduleType);
                MaintenanceDefinitions.SetStat(co, "StatBasePrice", damaged ? entry.BrokenPriceOrDefault : entry.price);
                MaintenanceDefinitions.SetStat(co, "StatMass", EquipmentRules.ModuleMassKg);
                MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", entry.work.repair);
                co.aUpdateCommands = damaged ? new[] { "Destructable,StatDamage,ACTDefaultDestroy,StatDamageMax,1.0" } :
                    new[] { "Destructable,StatDamage," + prefix + "Damage,StatDamageMax,1.0" };
                d.Objects.Add(id, co);
                d.Loot.Add(module, new Loot { strName = module, strType = "item", aCOs = new[] { module + "=1x1" }, aLoots = Array.Empty<string>() });
                if (damaged)
                {
                    var repair = MaintenanceDefinitions.Work(id + "Repair", id, "ACTRepairTEMP", "TIsRepairableNotContained", "CTRL");
                    repair.strJobType = "repair"; repair.strAllowLootCTsThem = "CONDRepairProgressx5";
                    repair.strProgressStat = "StatRepairProgress";
                    repair.aInputs = EconomyStock.RepairInputs(entry.repairBill);
                    repair.aLootCOs = new[] { moduleType };
                    d.Installables.Add(repair.strName, repair);
                    MaintenanceDefinitions.ReturnRepairMaterials(d, repair);
                }
                else MaintenanceDefinitions.Restore(d, id, "CTRL");
                MaintenanceDefinitions.Dismantle(d, id, entry.work.dismantle, new[] { Residue }, "CTRL");
                EquipmentSaveUpgrade.Register(d, module, id, legacyRepair: EquipmentRules.LegacyRepairProgress);
                MaintenanceDefinitions.LegacyFinish(module, "MSNavModMoboDismantle", "MS" + id + "Dismantle");
                if (damaged) MaintenanceDefinitions.LegacyFinish(module, "MSNavModMoboRepair", "MS" + id + "Repair");
            }
            d.Interactions.Add(prefix + "ModeDamage", new JsonInteraction { strName = prefix + "ModeDamage",
                strThemType = "Self", bIgnoreFeelings = true, objLootModeSwitch = moduleType + "Dmg", aLootItms = Array.Empty<string>() });
            d.Loot.Add(prefix + "Damage", new Loot { strName = prefix + "Damage", strType = "interaction",
                aCOs = new[] { prefix + "ModeDamage=1x1" }, aLoots = Array.Empty<string>() });
        }
        MaintenanceDefinitions.Remainder(d, Residue, Text.Get("EquipmentContent.auto_nav_board_residue_kg"), EquipmentRules.ModuleMassKg);
        MaintenanceDefinitions.Remainder(d, Offcut, Text.Get("EquipmentContent.auto_nav_assembly_offcuts_kg"), EquipmentRules.AssemblyOffcutKg);
        d.Objects[Residue].nStackLimit = d.Objects[Offcut].nStackLimit = RemnantStackLimit;
        EconomyStock.AddOffers(d, AutoNavEconomy.Pack, AutoNavEconomy.Sales);
        foreach (string table in AutoNavEconomy.SalvageTables)
        {
            double chance = salvageEnabled ? salvageChance ?? AutoNavEconomy.SalvageChance : 0;
            double intact = table.EndsWith("Dmg", StringComparison.Ordinal) ? 0 : chance * (1 - AutoNavEconomy.DamagedSalvageShare);
            AdditiveLoot.SetItemChoice(d, table, AutoNavEconomy.Salvage.branch + "_" + table, new Dictionary<string, double>
            {
                [NavigationService.ModuleId] = intact / 3,
                [NavigationService.PursuitId] = intact / 3,
                [NavigationService.FireControlId] = intact / 3,
                [NavigationService.DamagedId] = (chance - intact) / 3,
                [NavigationService.PursuitId + "Dmg"] = (chance - intact) / 3,
                [NavigationService.FireControlId + "Dmg"] = (chance - intact) / 3
            });
        }
        RegionalEconomy.Apply(d);
        ItemHandling.Apply(d);
        return d;
    }
}
