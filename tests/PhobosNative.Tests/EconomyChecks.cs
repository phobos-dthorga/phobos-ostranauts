using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

internal static class EconomyChecks
{
    internal static void Run(string repo, Action<bool,string> check, Action<Action,string> throws)
    {
        double Stat(string id, string stat) => EquipmentSaveUpgrade.Amount(DataHandler.dictCOs[id].aStartingConds, stat);
        double Sum(string[] products, string stat) => products.Sum(id => Stat(id, stat));
        var definitions = Content.Prepare(); definitions.Publish();
        foreach (var spec in EquipmentEconomy.Machines)
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            string id = spec.Prefix + state;
            var dismantle = definitions.Installables[id + "Dismantle"];
            check(Math.Abs(Sum(dismantle.aLootCOs,"StatMass") - Stat(id,"StatMass")) < .000001, "Whole equipment salvage conserves mass: " + id);
            check(Sum(dismantle.aLootCOs,"StatBasePrice") < Stat(id,"StatBasePrice") * .25, "Buying equipment for immediate scrap is not profitable: " + id);
            check(dismantle.strProgressStat == "StatDismantleProgress" && Stat(id,"StatDismantleProgressMax") >= 300, "Large equipment requires sustained disassembly: " + id);
            check(MaintenanceSafety.Actions.ContainsKey("ACT"+id+"DismantleAllow") && MaintenanceSafety.Actions.ContainsKey("MS"+id+"Dismantle"), "Dismantle guards cover work and finish: " + id);
            if (state.EndsWith("Dmg"))
            {
                var repair = definitions.Installables[spec.Prefix + state.Replace("Dmg", "") + "Repair"];
                check(repair.aLootCOs.Length == 1 && MaintenanceSafety.Repairs["MS" + repair.strName] == repair.aLootCOs[0], "Repair returns real replaced mass at finish: " + id);
                double inputKg = spec.RepairBill.Select((count,i) => count * Stat(EquipmentEconomy.Materials[i], "StatMass")).Sum();
                check(MaintenanceSafety.SpentPartUnits(inputKg) * .5 == inputKg, "Repair bill fits retained half-kg service packs: " + id);
            }
            else
            {
                var restore = definitions.Installables[id + "Restore"];
                check(restore.bNoDestructable && restore.strAllowLootCTsThem == "CONDUndamageProgress" && restore.aInputs.Length == 0,
                    "Restore changes wear in place without a second damage handler: " + id);
            }
        }
        foreach (double kg in new[] { 0, .5, 1, 1.5, 3, 4, 6, 10, 100 })
            check(MaintenanceSafety.SpentPartUnits(kg) * .5 == kg, "Repair retains material including legacy 1.5 kg bill");
        foreach (double kg in new[] { -.5, .4, 101, double.NaN, double.PositiveInfinity })
            check(MaintenanceSafety.SpentPartUnits(kg) == -1, "Unrepresentable repair lot is blocked before effects");

        check(Stat(ProcessRules.Residue,"StatBasePrice") == .01, "Panel residue does not invoke native zero-price mass fallback");
        check(StockCondition.Worn != StockCondition.Broken && MarketStock.WearFraction(StockCondition.Worn) == .15, "Used functional stock is light wear, separate from broken definitions");
        string merchant = "ItmOKLGSupplyKioskInv";
        var original = DataHandler.dictLoot[merchant];
        original.aLoots = original.aLoots.Concat(new[]{"ForeignTestStock=1x1"}).ToArray();
        string[] untouched = original.aCOs.ToArray();
        var repeat = Content.Prepare(); repeat.Publish();
        var updated = DataHandler.dictLoot[merchant];
        check(updated.aCOs.SequenceEqual(untouched) && updated.aLoots.Contains("ForeignTestStock=1x1"), "Stock append preserves native and third-party branches");
        check(updated.aLoots.Length == updated.aLoots.Distinct().Count(), "Repeated content loading adds no duplicate stock branches");
        check(updated.aLoots.Length == original.aLoots.Length, "Repeat stock registration does not grow merchant table");
        foreach (var offer in repeat.Loot.Values.Where(l => l.strName.StartsWith("PhobosStock_")))
            check(offer.aCOs.Length == 1 && offer.aCOs[0].EndsWith("x1"), "Each merchant offer is bounded to one object");
        foreach (string id in EquipmentEconomy.Machines.Select(s => s.Prefix + "Loose").Concat(new[]{ProcessRules.AssemblySection, ReclaimerRules.Section}))
        {
            var item = new DataCO(DataHandler.dictCOs[id]);
            check(DataHandler.dictCTs["TIsBarterSanDiegoHalvorsonSell"].TriggeredDataCO(item,false), "Industrial seller permits the stocked equipment: " + id);
            check(DataHandler.dictCTs["TIsBarterOKLGFixerSell"].TriggeredDataCO(item,false), "Fixer can sell our usable equipment: " + id);
            check(DataHandler.dictCTs["TIsBarterOKLGFixerBuy"].TriggeredDataCO(item,false), "Native high-value resale route exists: " + id);
            check(!DataHandler.dictCTs["TIsBarterOKLGSupplyKiosk"].TriggeredDataCO(item,false), "Keep native licensed/high-value resale restriction: " + id);
        }
        throws(() => MarketStock.Add(new NativeDefinitions(), "MissingMerchant", "PhobosMissingOffer", Content.Loose, .2, StockCondition.Worn), "Missing merchant is explicit, not silently unstocked");
        foreach (double probability in new[] { 0, -1, double.NaN, 1.1 })
            throws(() => MarketStock.Add(new NativeDefinitions(), merchant, "PhobosInvalidOffer", Content.Loose, probability, StockCondition.Worn), "Invalid stock chance rejected");

        // Native compressed and explicit saves both occur. Do not edit the caller's DTO.
        foreach (string[] conds in new[] {
            new[]{"DEFAULT", "StatRepairProgress=1x45"},
            new[]{"StatBasePrice=1x400", "StatRepairProgressMax=1x100", "StatRepairProgress=1x45", "StatDamage=1x3"} })
        {
            var save = new JsonCondOwnerSave { strCODef = Content.Loose + "Dmg", strID = "existing-machine", aConds = conds, aCondReveals = new int[conds.Length * 2], aLot = new[]{"repair-part-a","repair-part-b"}, strSlotName = "own-slot" };
            string before = JsonConvert.SerializeObject(save);
            var upgraded = EquipmentSaveUpgrade.Upgrade(save);
            check(EquipmentSaveUpgrade.Amount(upgraded.aConds,"StatBasePrice") == 3000, "Existing equipment gets current price");
            check(EquipmentSaveUpgrade.Amount(upgraded.aConds,"StatRepairProgressMax") == 100 && EquipmentSaveUpgrade.Amount(upgraded.aConds,"StatRepairProgress") == 45, "Existing work retains progress and original finish threshold");
            check(EquipmentSaveUpgrade.Amount(upgraded.aConds,"StatDismantleProgressMax") == 1000, "Old full-stat saves gain new maintenance actions");
            check(JsonConvert.SerializeObject(save) == before && upgraded.strID == save.strID, "Migration preserves original DTO and object identity");
            check(upgraded.aLot.SequenceEqual(save.aLot) && upgraded.strSlotName == save.strSlotName, "Migration preserves gathered repair-material references and slot ownership");
            check(ReferenceEquals(EquipmentSaveUpgrade.Upgrade(upgraded), upgraded), "Migration runs once per item");
        }

        foreach (var overlay in JsonConvert.DeserializeObject<JsonCOOverlay[]>(File.ReadAllText(Path.Combine(repo,"mods/PhobosAutoNav/data/cooverlays/phobos_approach_assist.json")))!)
            DataHandler.dictCOOverlays[overlay.strName] = overlay;
        var nav = PhobosAutoNav.EquipmentContent.Prepare(); nav.Publish();
        foreach (var equipment in nav.Objects.Values)
            check(equipment.strNameFriendly.StartsWith("Phobos' ", StringComparison.Ordinal), "Navigation module and retained material use branded native names");
        check(MaintenanceSafety.ResolveFinish("PhobosNavModAutoNavDmg", "MSNavModMoboRepair") == "MSPhobosAutoNavBoardDmgRepair", "Saved Auto Nav repair uses actual waste accounting");
        check(MaintenanceSafety.ResolveFinish("PhobosNavModAutoNav", "MSNavModMoboDismantle") == "MSPhobosAutoNavBoardDismantle", "Saved Auto Nav dismantle uses the 0.4 kg output");
        check(MaintenanceSafety.ResolveFinish("ItmNavModMobo", "MSNavModMoboDismantle") == "MSNavModMoboDismantle", "Generic vanilla job is not globally redirected");
        foreach (string id in new[]{PhobosAutoNav.EquipmentContent.Base,PhobosAutoNav.EquipmentContent.Base+"Dmg"})
        {
            check(Stat(id,"StatMass") == .4 && Sum(nav.Installables[id+"Dismantle"].aLootCOs,"StatMass") == .4, "Auto Nav disassembly cannot manufacture native half-kg electronics");
            check(!nav.Objects[id].aUpdateCommands.Any(c => c.Contains("ACTNavModMoboDamage")), "Auto Nav never falls back to a generic damaged board");
            var data = new DataCO(nav.Objects[id]);
            check(data.HasCond("IsPolaris") && data.HasCond("IsNavMod") && data.HasCond("IsCategoryControlSystems"),
                "Polaris compatibility and native trade categories survive the private board definition");
            foreach (string trigger in new[] { "TIsBarterSanDiegoPolarisBuy", "TIsBarterSanDiegoPolarisSell",
                "TIsBarterOKLGSupplyKiosk", "TIsBarterOKLGSupplyKioskSell", "TIsBarterVORBScrapKiosk", "TIsBarterVORBScrapKioskSell" })
                check(DataHandler.dictCTs[trigger].TriggeredDataCO(data, false), "Auto Nav can be traded through native filter: " + trigger + "/" + id);
            var slotted = NativeDefinitions.Clone(nav.Objects[id]);
            MaintenanceDefinitions.SetStat(slotted, "IsSlotted", 1);
            check(!DataHandler.dictCTs["TIsBarterSanDiegoPolarisSell"].TriggeredDataCO(new DataCO(slotted), false),
                "Polaris stock preserves the native slotted-equipment restriction");
            var dismantle = nav.Installables[id + "Dismantle"];
            check(MaintenanceSafety.Actions.ContainsKey("ACT" + id + "DismantleAllow") && MaintenanceSafety.Actions.ContainsKey("MS" + id + "Dismantle"),
                "Auto Nav dismantle guards cover work and finish");
            check(dismantle.aToolCTsUse.Contains("TIsToolMortorq") && dismantle.aToolCTsUse.Contains("TIsToolSoldering"),
                "Auto Nav native salvage requires the service tools");
        }
        var navRepair = nav.Installables[PhobosAutoNav.EquipmentContent.Base + "DmgRepair"];
        check(navRepair.aInputs.SequenceEqual(new[] { "TIsPartsElecSmall=1x2" }) && navRepair.aLootCOs.SequenceEqual(new[] { "PhobosNavModAutoNav" }),
            "Repair consumes the electronics bill and restores the same module family without extra whole-item loot");
        check(MaintenanceSafety.Repairs["MS" + navRepair.strName] == "PhobosNavModAutoNav"
            && MaintenanceSafety.SpentPartUnits(2 * Stat("ItmPartsElecSmall01", "StatMass")) * .5 == 1,
            "Repair returns the actual one-kg bill as spent parts");
        var navRestore = nav.Installables[PhobosAutoNav.EquipmentContent.Base + "Restore"];
        check(navRestore.bNoDestructable && navRestore.aInputs.Length == 0 && navRestore.strAllowLootCTsThem == "CONDUndamageProgress",
            "Restore remains in-place wear maintenance without a material bill");
        var navOffers = nav.Loot.Values.Where(l => l.strName.StartsWith("PhobosAutoNavStock_", StringComparison.Ordinal)).ToArray();
        check(navOffers.Length == 4 && navOffers.All(l => l.aCOs.Length == 1 && l.aCOs[0].EndsWith("x1", StringComparison.Ordinal)),
            "Four acquisition routes each generate at most one module");
        var polarisStock = DataHandler.dictLoot["ItmTraderSanDiegoPolarisInv"];
        var repeatNav = PhobosAutoNav.EquipmentContent.Prepare(); repeatNav.Publish();
        check(DataHandler.dictLoot["ItmTraderSanDiegoPolarisInv"].aCOs.SequenceEqual(polarisStock.aCOs)
            && DataHandler.dictLoot["ItmTraderSanDiegoPolarisInv"].aLoots.SequenceEqual(polarisStock.aLoots),
            "Registering Auto Nav again preserves native Polaris stock without duplicating its branch");
        var pack = JsonConvert.DeserializeObject<RecipePack>(File.ReadAllText(Path.Combine(repo,"mods/PhobosAutoNav/framework/recipes.json")))!;
        ConstructionRegistry.Register("AutoNavEconomyTest",pack.recipes);
        check(DataHandler.dictInteractions["PhobosCraft_PhobosBuildAutoNav"].aLootItms.Any(s=>s.StartsWith("Use,")), "Assembly requires reusable tools through native fetching");
        check(Math.Abs(pack.recipes[0].ingredients.Sum(i=>i.count*i.unitMassKg)-pack.recipes[0].outputs.Sum(i=>i.count*i.unitMassKg))<.000001, "Auto Nav construction retains exact offcut mass");
        check(DataHandler.dictInteractions.ContainsKey("PhobosCraft_PhobosBuildAutoNav"), "Native overlay output works through shared construction registry");
        // Inspect generated native jobs too; no game session or Unity object creation.
        foreach (var job in nav.Installables.Values)
        {
            Installables.Create(job);
            check(DataHandler.dictInteractions.ContainsKey("ACT"+job.strName), "Auto Nav maintenance action generated: "+job.strName);
        }
        FrameworkLifecycle.Complete();
        check(ConstructionRegistry.Ready("AutoNavEconomyTest"), "Auto Nav assembly registration finishes");
    }
}
