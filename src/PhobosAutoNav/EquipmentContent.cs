using System;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

namespace PhobosAutoNav;

internal static class EquipmentContent
{
    internal const string Base = "PhobosAutoNavBoard", Residue = "PhobosAutoNavBoardResidue", Offcut = "PhobosAutoNavBoardOffcut";
    internal static string Status { get; private set; } = "Awaiting content registration";
    internal static void Register()
    {
        try
        {
            Prepare().Publish();
            var mod = DataHandler.dictModInfos.Values.FirstOrDefault(m => m.strName == "Phobos Auto Nav" && !m.GetIsDisabled());
            if (mod == null) throw new InvalidOperationException("Enable the matching Phobos Auto Nav native package.");
            string path = Path.Combine(mod.GetDirectory(), "framework", "recipes.json");
            ConstructionRegistry.RegisterPack(Plugin.Id, path);
            Status = "Economy and maintenance definitions registered";
        }
        catch (Exception ex) { Status = "Economy registration failed: " + ex.Message; throw; }
    }

    internal static NativeDefinitions Prepare()
    {
        var d = new NativeDefinitions();
        foreach (bool damaged in new[] { false, true })
        {
            string id = Base + (damaged ? "Dmg" : ""), module = NavigationService.ModuleId + (damaged ? "Dmg" : "");
            var co = NativeDefinitions.Clone(DataHandler.dictCOs[damaged ? "ItmNavModMoboDmg" : "ItmNavModMobo"]);
            co.strName = id;
            co.strNameFriendly = co.strNameShort = damaged ? "Phobos Auto Nav (Damaged)" : "Phobos Auto Nav";
            co.aInteractions = new[] { "DropItem", "PickupItem" };
            MaintenanceDefinitions.SetStat(co, "StatBasePrice", damaged ? 900 : 3600);
            MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", 900);
            co.aUpdateCommands = damaged ? new[] { "Destructable,StatDamage,ACTDefaultDestroy,StatDamageMax,1.0" } :
                new[] { "Destructable,StatDamage,PhobosAutoNavDamage,StatDamageMax,1.0" };
            d.Objects.Add(id, co);
            d.Loot.Add(module, new Loot { strName = module, strType = "item", aCOs = new[] { module + "=1x1" }, aLoots = Array.Empty<string>() });
            if (damaged)
            {
                var repair = MaintenanceDefinitions.Work(id + "Repair", id, "ACTRepairTEMP", "TIsRepairableNotContained", "CTRL");
                repair.strJobType = "repair"; repair.strAllowLootCTsThem = "CONDRepairProgressx5";
                repair.strProgressStat = "StatRepairProgress";
                repair.aInputs = new[] { "TIsPartsElecSmall=1x2" };
                repair.aLootCOs = new[] { NavigationService.ModuleId, "ItmScrapTrash" };
                d.Installables.Add(repair.strName, repair);
                MaintenanceDefinitions.ReturnRepairMaterials(d, repair);
            }
            else MaintenanceDefinitions.Restore(d, id, "CTRL");
            MaintenanceDefinitions.Dismantle(d, id, 100, new[] { Residue }, "CTRL");
            EquipmentSaveUpgrade.Register(d, module, id, legacyRepair: 480);
            MaintenanceDefinitions.LegacyFinish(module, "MSNavModMoboDismantle", "MS" + id + "Dismantle");
            if (damaged) MaintenanceDefinitions.LegacyFinish(module, "MSNavModMoboRepair", "MS" + id + "Repair");
        }
        d.Interactions.Add("PhobosAutoNavModeDamage", new JsonInteraction { strName = "PhobosAutoNavModeDamage",
            strThemType = "Self", bIgnoreFeelings = true, objLootModeSwitch = NavigationService.DamagedId, aLootItms = Array.Empty<string>() });
        d.Loot.Add("PhobosAutoNavDamage", new Loot { strName = "PhobosAutoNavDamage", strType = "interaction",
            aCOs = new[] { "PhobosAutoNavModeDamage=1x1" }, aLoots = Array.Empty<string>() });
        MaintenanceDefinitions.Remainder(d, Residue, "Auto Nav board residue (0.4 kg)", .4);
        MaintenanceDefinitions.Remainder(d, Offcut, "Auto Nav assembly offcuts (0.6 kg)", .6);
        Offer("ItmOKLGFixer", "Used", NavigationService.ModuleId, .30, StockCondition.Worn);
        Offer("ItmOKLGSupplyKioskInv", "Broken", NavigationService.DamagedId, .25, StockCondition.Broken);
        Offer("ItmTraderSanDiegoPolarisInv", "New", NavigationService.ModuleId, .60, StockCondition.Pristine);
        Offer("ItmVORBScrapKioskInv", "Refurb", NavigationService.ModuleId, .20, StockCondition.Refurbished);
        return d;

        void Offer(string merchant, string tag, string item, double chance, StockCondition condition) =>
            MarketStock.Add(d, merchant, "PhobosAutoNavStock_" + tag, item, chance, condition);
    }
}
