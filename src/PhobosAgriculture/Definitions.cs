using System;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

namespace PhobosAgriculture;

internal static class Definitions
{
    internal const string Rack = "PhobosVerdemorrowFirstlight4", Cooker = "PhobosVerdemorrowHearth2", Controls = "PhobosAgricultureControls";
    internal const string PotatoSeed = "PhobosVerdemorrowContinuancePotato", LettuceSeed = "PhobosVerdemorrowContinuanceLettuce", Nutrient = "PhobosVerdemorrowGroundworkNutrients", Raw = "PhobosVerdemorrowRawPotatoes", Meal = "PhobosVerdemorrowHearthPotatoes", Leaves = "PhobosVerdemorrowLettuce", Residue = "PhobosVerdemorrowCropResidue", Drainage = "PhobosVerdemorrowProcessSolution";
    internal static bool Ready;
    internal static readonly string[] Work = { "plant-potato", "plant-lettuce", "load-water", "load-nutrients", "harvest", "clear", "drain" };
    internal static string WorkId(string action) => "PhobosAgricultureWork_" + action.Replace('-', '_');
    private static readonly System.Collections.Generic.HashSet<string> Machines = new(new[] { Rack, Cooker }.SelectMany(prefix => new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" }.Select(form => prefix + form)), StringComparer.Ordinal);
    internal static bool Machine(CondOwner? co) => co != null && Machines.Contains(co.strCODef);
    internal static bool IsCooker(CondOwner co) => co.strCODef.StartsWith(Cooker, StringComparison.Ordinal);
    internal static double DryMass(CondOwner co) => IsCooker(co) ? 12 : 80;
    internal static void Load()
    {
        Ready = false;
        var mod = DataHandler.dictModInfos.Values.FirstOrDefault(m => m.strName == "Phobos Agriculture" && !m.GetIsDisabled());
        if (mod == null) throw new InvalidOperationException(Text.Get("missing_package"));
        var prepared = Prepare(); prepared.Publish(); ConstructionRegistry.RegisterPack(Plugin.Id, Path.Combine(mod.GetDirectory(), "framework", "recipes.json"));
    }
    internal static NativeDefinitions Prepare()
    {
        var d = new NativeDefinitions();
        var controls = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        controls.strName = Controls; controls.strTitle = Text.Get("controls"); controls.strDesc = controls.strTooltip = Text.Get("controls"); controls.strRaiseUI = null; controls.fTargetPointRange = 2;
        d.Interactions[Controls] = controls;
        foreach (string action in Work)
        {
            var work = NativeDefinitions.Clone(controls); work.strName = WorkId(action); work.strTitle = work.strTooltip = Text.Get(action);
            work.fDuration = action == "harvest" ? .5 : action.StartsWith("load-", StringComparison.Ordinal) ? 10d / 3600 : .25; work.strAnim = "Tablet"; work.strActionGroup = "Work";
            d.Interactions[work.strName] = work;
        }
        ApplianceDefinitions.Add(d, Rack, Text.Get("rack"), Text.Get("rack_desc"), 4, 80, 9000, "phobos/agriculture/Rack", Controls, .02);
        ApplianceDefinitions.Add(d, Cooker, Text.Get("cooker"), Text.Get("cooker_desc"), 2, 12, 2400, "phobos/agriculture/Cooker", Controls, .02);
        // The ordinary solid-inventory trigger rejects native liquid rations. The
        // rack's contained supply cassette explicitly accepts water as well.
        d.Triggers[Rack + "Supplies"] = new CondTrigger { strName = Rack + "Supplies", fChance = 1, fCount = 1, bAND = false,
            aReqs = Array.Empty<string>(), aForbids = new[] { "IsInstalled", "IsCumbersome", "IsOversized" }, aTriggers = new[] { "TIsFitContainerSolid", "TIsWater" } };
        foreach (var co in d.Objects.Values.Where(c => c.strName.StartsWith(Rack, StringComparison.Ordinal))) co.strContainerCT = Rack + "Supplies";
        foreach (var co in d.Objects.Values.Where(c => c.strName.StartsWith(Rack) && c.strName.EndsWith("Installed"))) co.aInteractions = co.aInteractions.Concat(Work.Select(WorkId)).ToArray();
        foreach (var co in d.Objects.Values.Where(c => c.strName.EndsWith("Dmg"))) co.strNameFriendly = co.strNameShort = Text.Get("damaged", co.strNameFriendly);
        Stock(d, PotatoSeed, .2, 40, "potato_seed", false); Stock(d, LettuceSeed, .005, 25, "lettuce_seed", false);
        Stock(d, Nutrient, .04, 60, "nutrients", false); Stock(d, Raw, .4, 12, "raw", false);
        Stock(d, Meal, .4, 35, "meal", true); Stock(d, Leaves, .25, 8, "leaves", true); Stock(d, Residue, .5, .01, "residue", false);
        Stock(d, Drainage, .25, .01, "drainage", false);
        foreach (string food in new[] { Meal, Leaves })
            d.Loot[food + "Effects"] = new Loot { strName = food + "Effects", strType = "trigger", aCOs = new[] { "TDnFood=1x" + (food == Meal ? 5 : 1), "TUpSatiety=1x" + (food == Meal ? 3 : 1), "TDnTeethBrushed=1x1" }, aLoots = Array.Empty<string>() };
        foreach (string prefix in new[] { Rack, Cooker })
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            // Salvage returns the full dry housing as identified service waste; no resale gain.
            string scrap = prefix + "HousingWaste";
            if (!d.Objects.ContainsKey(scrap)) MaintenanceDefinitions.Remainder(d, scrap, Text.Get("housing_waste"), prefix == Rack ? 80 : 12);
            MaintenanceDefinitions.Dismantle(d, prefix + state, 800, new[] { scrap });
        }
        foreach (string merchant in new[] { "ItmOKLGSupplyKioskInv", "ItmOKLGFixer", "ItmTraderSanDiegoHalvorsonInv" })
        foreach (string item in new[] { Rack + "Loose", Cooker + "Loose", PotatoSeed, LettuceSeed, Nutrient })
            MarketStock.Add(d, merchant, "PhobosAgricultureStock_" + merchant + "_" + item, item, item == Nutrient ? 1 : .65, StockCondition.Pristine);
        return d;
    }
    private static void Stock(NativeDefinitions d, string id, double kg, double price, string key, bool food)
    {
        var co = NativeDefinitions.Clone(DataHandler.dictCOs[food ? "ItmTrencherAcceptableAlgae" : "ItmScrapTrash"]);
        co.strName = id; co.strNameFriendly = co.strNameShort = Text.Get(key); co.strDesc = Text.Get(key + "_desc");
        co.nStackLimit = 1; co.aUpdateCommands = Array.Empty<string>(); co.aTickers = Array.Empty<string>(); co.inventoryWidth = co.inventoryHeight = 1;
        co.aStartingConds = food ? new[] { "IsSolid=1x1", "IsEdible=1x1", "IsFood=1x1", "IsCategoryFood=1x1", "IsPocketable=1x1" } : new[] { "IsSolid=1x1", "IsPocketable=1x1" };
        MaintenanceDefinitions.SetStat(co, "StatMass", kg); MaintenanceDefinitions.SetStat(co, "StatBasePrice", price);
        d.Objects[id] = co; // Refer to native item art at runtime until individual stock art is accepted.
    }
}
