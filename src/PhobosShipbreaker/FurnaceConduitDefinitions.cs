using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class FurnaceConduitDefinitions
{
    internal const string Segment = "PhobosFurnaceCoolantSegment";
    internal const string Art = "phobos/shipbreaker/FurnaceCoolantPipe";
    internal static void Add(NativeDefinitions d)
    {
        string p = FurnaceCooling.Conduit;
        d.Conditions[Segment] = new JsonCond { strName = Segment, strNameFriendly = Text.Get("Furnace.coolant_pipe"), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        d.Triggers[p + "Sprite"] = new CondTrigger { strName = p + "Sprite", fChance = 1, fCount = 1, bAND = true,
            aReqs = new[] { Segment }, aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
        d.Loot[p + "Adds"] = new Loot { strName = p + "Adds", strType = "condition", aCOs = new[] { Segment + "=1x1" }, aLoots = Array.Empty<string>() };
        ApplianceDefinitions.Add(d, p, Text.Get("Furnace.coolant_pipe"), Text.Get("Furnace.coolant_pipe_desc", FurnaceCooling.RouteLimit), 1, 1, 3, Art, "Inventory", 0, InstallMenu.Hvac);
        d.Power.Remove(p + "Power");
        string waste = p + "Waste";
        MaintenanceDefinitions.Remainder(d, waste, Text.Get("Furnace.coolant_waste"), 1);
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            bool installed = form.StartsWith("Installed");
            var co = d.Objects[p + form]; var item = d.Items[co.strItemDef];
            co.jsonPI = null; co.aTickers = Array.Empty<string>(); co.aInteractions = Array.Empty<string>();
            co.mapPoints = new[] { "use,0,-16" }; co.mapGUIPropMaps = Array.Empty<string>();
            co.nContainerWidth = co.nContainerHeight = 0; co.strContainerCT = null;
            co.aStartingConds = co.aStartingConds.Where(s => !s.StartsWith("IsContainer=") && !s.StartsWith("IsCumbersome="))
                .Concat(new[] { "IsPocketable=1x1" }).ToArray();
            MaintenanceDefinitions.SetStat(co, "StatInstallProgressMax", 50);
            MaintenanceDefinitions.SetStat(co, "StatUninstallProgressMax", 50);
            MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", 50);
            item.fZScale = 1.01f;
            if (installed)
            {
                item.strImg = Art + "Sheet"; item.strImgNorm = item.strImg + "Normal";
                item.bHasSpriteSheet = true; item.ctSpriteSheet = p + "Sprite";
                item.aSocketAdds = new[] { p + "Adds" };
                item.aSocketForbids = Enumerable.Range(0, 9).Select(i => i == 4 ? p + "Adds" : "Blank").ToArray();
                item.aSocketReqs = Enumerable.Range(0, 9).Select(i => i == 4 ? "TILFloor" : "Blank").ToArray();
            }
            // Small sealed joints are serviced mechanically. Replacement metal remains
            // accounted by the Framework repair helper registered by ApplianceDefinitions.
            if (form.EndsWith("Dmg"))
                d.Installables[co.strName + "Repair"].aInputs = new[] { "TIsScrapAluminum=1x1" };
            else EquipmentEconomy.SetRestoreRate(d, co.strName, p + "RestoreProgress", 1);
            MaintenanceDefinitions.Dismantle(d, co.strName, 20, new[] { waste });
        }
        foreach (string merchant in new[] { "ItmOKLGSupplyKioskInv", "ItmOKLGFixer", "ItmTraderSanDiegoHalvorsonInv" })
            MarketStock.Add(d, merchant, p + "Offer_" + merchant, p + "Loose", 1, StockCondition.Pristine, StockQuantities.Pipes);
        // Native sheet sockets show the furnace connector independently of electricity.
        d.Loot[p + "Fixture"] = new Loot { strName = p + "Fixture", strType = "condition", aCOs = new[] { Segment + "=1x1" }, aLoots = new[] { "TILFixtureAdds=1x1" } };
        foreach (string form in new[] { "Installed", "InstalledDmg" })
        {
            var item = d.Items[FurnaceRules.Prefix + form];
            // Row at local y=+0.5, left/right boundary of the 6x6 body.
            item.aSocketAdds[12] = item.aSocketAdds[17] = p + "Fixture";
            item.ctSpriteSheet = p + "Sprite";
        }
    }
}
