using Ostranauts.Trading;
using System;
using System.Linq;
using Newtonsoft.Json;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

internal static class ReclaimerNativeChecks
{
    internal static void Run(NativeDefinitions d, Action<bool, string> check)
    {
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        check(typeof(Powered).GetMethod("GatherPower", flags, null, new[] { typeof(double), typeof(System.Collections.Generic.List<CondOwner>) }, null)?.ReturnType == typeof(double),
            "Inspected native brownout receipt hook remains available");
        check(typeof(Powered).GetMethod("UsePower", flags, null, new[] { typeof(CondOwner), typeof(double) }, null) != null,
            "Native electrical receipt and heat preflight hook remain available");
        var machine = d.Objects[ReclaimerRules.Installed]; var item = d.Items[machine.strItemDef];
        check(item.nCols == 4 && item.aSocketAdds.Length == 16 && machine.nContainerWidth == 8 && machine.nContainerHeight == 8,
            "Reclaimer starts with full 4x4 footprint and separate 8x8 output");
        check(machine.aInteractions.Contains(IndustrialRules.LocalControls) && machine.aSlotsWeHave.Contains(ReclaimerRules.InputSlot), "Reclaimer has ordinary control panel and private feed");
        var feed = d.Objects[ReclaimerRules.InputBin]; var trigger = DataHandler.dictCTs[feed.strContainerCT];
        check(feed.nContainerWidth * feed.nContainerHeight == 4 && d.Slots[ReclaimerRules.InputSlot].bHide, "Feed holds four actual packets and keeps ordinary Inventory for output");
        check(trigger.TriggeredDataCO(new DataCO(d.Objects[ReclaimerRules.Feedstock]), false), "Native trigger accepts identified R2 feed");
        foreach (string id in new[] { ProcessRules.Residue, ReclaimerRules.Reject, ProcessRules.Wall, "ItmScrapSteel" })
            check(!trigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[id]), false), "Native feed rejects incompatible material: " + id);
        foreach (var recipe in ProcessRecipes.WallPanels.Recipes.Concat(ReclaimerRules.Recipes.Recipes))
        {
            var sizes = ProcessingService.OutputSizes(recipe);
            check(sizes != null && BatchPlacement.Plan(new bool[8,8], sizes) != null, "Actual complete output batch fits product inventory");
            check(BatchPlacement.Plan(new bool[1,1], sizes!) == null, "Whole output batch cannot reuse one available cell");
            check(recipe.Products.All(p => ProcessMaterial.MassMatches(new DataCO(DataHandler.dictCOs[p.Id]).GetCondAmount("StatMass"), p.Kg)), "Product mass agrees with native definitions");
        }
        var save = new JsonItem { strName = ReclaimerRules.Feedstock, strID = "reclaimer-save-packet" };
        save.SetCondAmount(ProcessRules.Revision, 1); save.SetCondAmount(ProcessRules.Progress, 37.5); save.SetCondAmount(ProcessRules.Duration, 120);
        var loaded = JsonConvert.DeserializeObject<JsonItem>(JsonConvert.SerializeObject(save))!;
        var resumed = ProcessJob.CreateOrResume(ReclaimerRules.Recipes, loaded.strID, (loaded.GetCondAmountOverride(ProcessRules.Progress) ?? 0),
            (loaded.GetCondAmountOverride(ProcessRules.Revision) ?? 0), (loaded.GetCondAmountOverride(ProcessRules.Duration) ?? 0), 180);
        check(resumed.Progress == 37.5 && resumed.Duration == 120, "Native save DTO retains reclaimer job contract");
        var roomSave = new JsonCondOwnerSave { fDGasTemp = 1.2345 };
        check(JsonConvert.DeserializeObject<JsonCondOwnerSave>(JsonConvert.SerializeObject(roomSave))!.fDGasTemp == 1.2345, "Native heat accumulator participates in save data");
    }
}
