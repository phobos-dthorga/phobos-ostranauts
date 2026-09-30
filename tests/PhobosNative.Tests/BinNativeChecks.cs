using System;
using System.Linq;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

/// <summary>The Rivetline material bins against the game's own data: every form of every size keeps its grid and
/// admission rule, stays unpowered and off the Control Panel, admits every mined material the game defines (and
/// Manufacturing's clay chunk) through the game's own rules, and refuses everything else, including every product
/// the D4 and R4 send to storage, so leaving bins out of that list loses nothing.</summary>
internal static class BinNativeChecks
{
    internal static void Run(NativeDefinitions d, Action<bool, string> check)
    {
        double Stat(JsonCondOwner co, string key) => EquipmentSaveUpgrade.Amount(co.aStartingConds, key);
        bool Has(JsonCondOwner co, string key) => co.aStartingConds.Any(s => s.Split('=')[0] == key);
        var trigger = DataHandler.dictCTs[BinRules.Trigger];
        check(trigger.aTriggers.SequenceEqual(new[] { BinRules.NativeSolid, BinRules.NativeMiningOutput }) && trigger.bAND, "The bin rule is the game's solid-container rule and mined-material rule together");
        foreach (var size in BinRules.Sizes)
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            bool damaged = state.EndsWith("Dmg", StringComparison.Ordinal), installed = state.StartsWith("Installed", StringComparison.Ordinal);
            string id = size.Prefix + state; var co = d.Objects[id]; var item = d.Items[co.strItemDef];
            check(item.nCols == size.Footprint && item.aSocketAdds.Length == size.Footprint * size.Footprint && co.inventoryWidth == size.Footprint && co.inventoryHeight == size.Footprint,
                "Each bin occupies its own footprint: " + id);
            check(co.strContainerCT == BinRules.Trigger && co.nContainerWidth == size.Grid && co.nContainerHeight == size.Grid && Has(co, "IsContainer") && co.aInteractions.Contains("Inventory"),
                "Every form keeps the same grid and rule, so a mode switch carries the contents: " + id);
            check(co.jsonPI == null && co.aTickers.Length == 0 && co.aSlotsWeHave.Length == 0 && co.strLoot == "Blank", "A bin is unpowered, with no feed or hidden compartment: " + id);
            check(!co.aInteractions.Contains(IndustrialRules.LocalControls) && !co.aInteractions.Contains(IndustrialRules.FeedOrder) && IndustrialRules.Group(id) == "",
                "A bin has no Control Panel or loading order and stays off the C1: " + id);
            check(!Has(co, "IsStorageFurniture") && Has(co, installed ? "IsInstalled" : "IsCumbersome"), "No plot-targeted storage mark; loose bins are cumbersome: " + id);
            check(Stat(co, "StatMass") == size.DryKg && Stat(co, "StatBasePrice") == (damaged ? (int)size.Price / 4 : (int)size.Price), "Each bin begins empty at its housing mass and authored price: " + id);
            check(co.strNameFriendly.StartsWith("Phobos' Rivetline Y" + size.Footprint + " ", StringComparison.Ordinal), "Each bin carries its Rivetline Y model: " + id);
            check(d.Installables.ContainsKey(id + "Dismantle") && (damaged || d.Installables.ContainsKey(id + "Restore")), "Each bin has the native maintenance jobs: " + id);
        }
        bool Admits(string id) => trigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[id]), false);
        foreach (string mined in new[] { "ItmMineral01", "ItmMineral02", "ItmMineral03", "ItmMineral04", "ItmMineral05", "ItmMineral06", "ItmMineral07",
                     "ItmMineral08", "ItmMineral09", "ItmMineral10", "ItmMineral11", "ItmMineral79", "ItmMineralStone01", "ItmMiningTrash",
                     "ItmIce01", "ItmIce02", "ItmIceTrash01", PhobosManufacturing.Core.Materials.ClayHydrates })
            check(Admits(mined), "A bin admits the game's mined material: " + mined);
        foreach (string outside in new[] { "ItmScrapSteel", "ItmScrapAluminum", "ItmPartsMechSmall01", "ItmCanisterO2Small", ProcessRules.Wall, BinRules.Prefix + "Loose" }
                     .Concat(FurnaceRecipes.Ingots))
            check(!Admits(outside), "A bin refuses anything that is not mined material: " + outside);
        foreach (string machine in new[] { Content.Prefix + "Installed", ReclaimerRules.Prefix + "Installed" })
        foreach (var product in StorageRules.Carried(machine))
            check(!Admits(product.Id), "No D4 or R4 storage product would enter a bin, so bins are not offered there: " + product.Id);
    }
}
