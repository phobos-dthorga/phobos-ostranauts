using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Native installable appliance family. All names, ratings, sprites and bills are content-owned.</summary>
public static class ApplianceDefinitions
{
    public static void Add(NativeDefinitions d, string prefix, string name, string description, int size, double kg, double price, string image, string controls, double kw)
    {
        d.Conditions[prefix + "Machine"] = new JsonCond { strName = prefix + "Machine", strNameFriendly = name, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        d.Power[prefix + "Power"] = new JsonPowerInfo { strName = prefix + "Power", strUsePowerCT = "TIsReadyUsePower", aInputPts = new[] { "PowerA", "PowerB" }, bAllowExtPower = true, fAmount = kw / 3600 };
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            string id = prefix + form; bool installed = form.StartsWith("Installed"), damaged = form.EndsWith("Dmg");
            var conds = new List<string> { "IsSolid=1x1", "IsMechanical=1x1", "IsCategoryIndustrialProducts=1x1", "IsContainer=1x1", prefix + "Machine=1x1", "StatDamageMax=1x20", "StatInstallProgressMax=1x1000", "StatUninstallProgressMax=1x800" };
            conds.Add(installed ? "IsInstalled=1x1" : "IsCumbersome=1x1");
            if (damaged) conds.AddRange(new[] { "IsDamaged=1x1", "StatRepairProgressMax=1x2400" });
            var co = new JsonCondOwner { strName = id, strItemDef = id, strType = "Item", strNameFriendly = name, strNameShort = name,
                strDesc = description, nStackLimit = 1, nContainerWidth = 8, nContainerHeight = 8, inventoryWidth = size, inventoryHeight = size,
                strContainerCT = "TIsFitContainerSolid", aInteractions = installed ? new[] { "Inventory", controls } : new[] { "Inventory" },
                mapGUIPropMaps = new[] { "GUIInv", "Inventory" }, aStartingConds = conds.ToArray(), mapSlotEffects = new[] { "drag", "Blank" },
                mapPoints = new[] { "use,0," + (-8 * size - 8), "PowerA," + (-8 * size + 8) + "," + (8 * size - 8), "PowerB," + (8 * size - 8) + "," + (8 * size - 8) },
                jsonPI = installed && !damaged ? prefix + "Power" : null, aTickers = installed && !damaged ? new[] { "Power" } : Array.Empty<string>(),
                aUpdateCommands = new[] { "Destructable,StatDamage," + (damaged ? "ACTDefaultDestroy" : prefix + "Damage" + form) + ",StatDamageMax,1.0" }, strPortraitImg = image };
            MaintenanceDefinitions.SetStat(co, "StatMass", kg); MaintenanceDefinitions.SetStat(co, "StatBasePrice", damaged ? price * .2 : price);
            d.Objects[id] = co;
            int border = size + 2;
            string[] Grid(string inside) => Enumerable.Range(0, border * border).Select(i => i % border > 0 && i % border < border - 1 && i / border > 0 && i / border < border - 1 ? inside : "Blank").ToArray();
            d.Items[id] = new JsonItemDef { strName = id, strImg = image, strImgNorm = image + "Normal", strImgDamaged = "blank", strDmgColor = "DamageTintDefault", fZScale = .5f, nCols = size,
                aSocketAdds = Enumerable.Repeat(installed ? "TILFixtureAdds" : "TILItemAdds", size * size).ToArray(), aSocketReqs = Grid(installed ? "TILFloor" : "Blank"), aSocketForbids = Grid(installed ? "TILObstruction" : "TILItemForbids") };
            d.Loot[id] = new Loot { strName = id, strType = "item", aCOs = new[] { id + "=1x1" }, aLoots = Array.Empty<string>() };
            d.Triggers[id + "Test"] = new CondTrigger { strName = id + "Test", fChance = 1, fCount = 1, bAND = true,
                aReqs = new[] { prefix + "Machine" }.Concat(installed ? new[] { "IsInstalled" } : Array.Empty<string>()).Concat(damaged ? new[] { "IsDamaged" } : Array.Empty<string>()).ToArray(),
                aForbids = (installed ? Array.Empty<string>() : new[] { "IsInstalled" }).Concat(damaged ? Array.Empty<string>() : new[] { "IsDamaged" }).ToArray(), aTriggers = Array.Empty<string>() };
            if (!damaged)
            {
                string change = prefix + "Damage" + form;
                d.Interactions[change] = new JsonInteraction { strName = change, strThemType = "Self", bIgnoreFeelings = true, objLootModeSwitch = id + "Dmg", aLootItms = Array.Empty<string>() };
                d.Loot[change] = new Loot { strName = change, strType = "interaction", aCOs = new[] { change + "=1x1" }, aLoots = Array.Empty<string>() };
                MaintenanceDefinitions.Restore(d, id);
            }
            foreach (string job in damaged ? new[] { installed ? "Uninstall" : "Install", "Repair" } : new[] { installed ? "Uninstall" : "Install" })
            {
                bool install = job == "Install", repair = job == "Repair";
                var work = MaintenanceDefinitions.Work(id + job, id, "ACT" + job + (repair ? "" : "NoSparks") + "TEMP", id + "Test", "MISC");
                work.strJobType = job.ToLowerInvariant(); work.strInteractionName = job;
                string output = repair ? id.Replace("Dmg", "") : prefix + form.Replace(install ? "Loose" : "Installed", install ? "Installed" : "Loose");
                work.strStartInstall = install ? output : null; work.strBuildType = install ? "MIS" : null;
                work.aInputs = repair ? new[] { "TIsPartsMechSmall=1x1", "TIsScrapAluminum=1x1" } : install ? new[] { id + "Test=1x1" } : Array.Empty<string>();
                work.aLootCOs = new[] { output }; work.strAllowLootCTsThem = "COND" + job + "Progressx5"; work.strProgressStat = "Stat" + job + "Progress";
                d.Installables[work.strName] = work;
                if (repair) MaintenanceDefinitions.ReturnRepairMaterials(d, work);
            }
        }
    }
}
