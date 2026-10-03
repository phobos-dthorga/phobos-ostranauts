using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Native installable appliance family. All names, ratings, sprites and bills are content-owned.</summary>
public static class ApplianceDefinitions
{
    /// <summary>Bind registered presentation-only variants without changing physical definitions.</summary>
    public static void ApplyStateArtwork(NativeDefinitions d, string prefix, string imageBase)
    {
        d.Items[d.Objects[prefix + "Installed"].strItemDef].strImgDamaged = imageBase + "Damaged";
        foreach (string form in new[] { "InstalledDmg", "Loose", "LooseDmg" })
        {
            string suffix = form == "InstalledDmg" ? "Damaged" : form == "LooseDmg" ? "LooseDamaged" : "Loose";
            var owner = d.Objects[prefix + form];
            var item = d.Items[owner.strItemDef];
            owner.strPortraitImg = item.strImg = imageBase + suffix;
            item.strImgNorm = item.strImg + "Normal";
            item.strImgDamaged = imageBase + (form == "InstalledDmg" ? "Damaged" : "LooseDamaged");
        }
    }

    /// <summary>A hidden feed compartment on an appliance family: a locked system bin admitted by
    /// <paramref name="containerTrigger"/>, its slot, and the loot that fits it. Content narrows admission
    /// further with its own container patch; the bin's cells are <paramref name="cells"/> x 1.</summary>
    public static void AddFeedBin(NativeDefinitions d, string prefix, string containerTrigger, int cells, string name)
    {
        if (cells < 1) throw new ArgumentException("A feed bin needs at least one cell.");
        string bin = prefix + "InputBin", slot = prefix + "Input", loot = prefix + "Compartments";
        d.Objects[bin] = new JsonCondOwner { strName = bin, strNameFriendly = name, strNameShort = name, strType = "Item", strItemDef = "Blank",
            strPortraitImg = "blank", strContainerCT = containerTrigger, nStackLimit = 1, bSlotLocked = true, nContainerWidth = cells, nContainerHeight = 1,
            aInteractions = Array.Empty<string>(), aStartingConds = new[] { "IsContainer=1x1", "IsSystem=1x1" }, mapSlotEffects = new[] { slot, "Blank" } };
        d.Slots[slot] = new JsonSlot { strName = slot, strNameFriendly = name, strHitboxImage = "blank", nItems = 1, nDepth = 15, bCarried = true, bHide = true };
        d.Loot[loot] = new Loot { strName = loot, strType = "item", aCOs = new[] { bin + "=1x1" }, aLoots = Array.Empty<string>() };
        EquipmentInventory.Declare(d.Objects[bin], InventorySpec.Feed(cells, 1, containerTrigger));
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            var co = d.Objects[prefix + form];
            co.strLoot = loot; co.aSlotsWeHave = new[] { slot };
            // The feed opens as its own titled window beside the ordinary tray.
            co.dictSlotsLayout = new Dictionary<string, UnityEngine.Vector3> { ["self"] = UnityEngine.Vector3.zero };
        }
    }

    /// <summary>A visible restricted rack in place of the appliance's ordinary tray: the same native Inventory
    /// window, <paramref name="width"/> x <paramref name="height"/> cells, admitting only what
    /// <paramref name="containerTrigger"/> accepts (the game applies it on every form, loose or installed).</summary>
    public static void SetRack(NativeDefinitions d, string prefix, string containerTrigger, int width, int height)
    {
        if (width < 1 || height < 1 || string.IsNullOrWhiteSpace(containerTrigger)) throw new ArgumentException("A rack needs a trigger and at least one cell.");
        // Since Framework 0.70.0 a rack is one of the declared inventory roles.
        EquipmentInventory.Apply(d, prefix, InventorySpec.ServiceRack(width, height, containerTrigger));
    }

    /// <summary>Idle and working electrical demand on the family's power info: the working amount applies while
    /// the machine carries <paramref name="workingCondition"/>, which content sets from its own service.</summary>
    public static void SetPowerOverride(NativeDefinitions d, string prefix, double idleKW, double workingKW, string workingCondition, params string[] inputPoints)
    {
        if (idleKW < 0 || workingKW <= 0 || double.IsNaN(idleKW) || double.IsNaN(workingKW)) throw new ArgumentException("Invalid electrical demand.");
        var power = d.Power[prefix + "Power"];
        if (inputPoints.Length > 0) power.aInputPts = inputPoints;
        power.fAmount = idleKW / Units.SecondsPerHour;
        power.strOverrideCond = workingCondition;
        power.fOverrideAmount = workingKW / Units.SecondsPerHour;
    }

    // Preserve the existing public signature for already-compiled content consumers.
    public static void Add(NativeDefinitions d, string prefix, string name, string description, int size, double kg, double price, string image, string controls, double kw) =>
        Add(d, prefix, name, description, size, kg, price, image, controls, kw, InstallMenu.Appliances);

    public static void Add(NativeDefinitions d, string prefix, string name, string description, int size, double kg, double price, string image, string controls, double kw, string buildCategory)
    {
        d.Conditions[prefix + "Machine"] = new JsonCond { strName = prefix + "Machine", strNameFriendly = name, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        // The game sets and clears IsPowered only for power info that names a power-on interaction; a
        // self-targeted no-op is the native pattern (see Shipbreaker's machinery), so the appliance's
        // power state, the crew console and the game's own power display all follow the real connection.
        d.Power[prefix + "Power"] = new JsonPowerInfo { strName = prefix + "Power", strUsePowerCT = "TIsReadyUsePower", aInputPts = new[] { "PowerA", "PowerB" }, bAllowExtPower = true, fAmount = kw / 3600,
            strIntPowerOn = prefix + "PowerChange", strIntPowerOff = prefix + "PowerChange" };
        d.Interactions[prefix + "PowerChange"] = new JsonInteraction { strName = prefix + "PowerChange", strThemType = "Self", bIgnoreFeelings = true, aLootItms = Array.Empty<string>() };
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
                work.strStartInstall = install ? output : null; work.strBuildType = install ? buildCategory : null;
                work.aInputs = repair ? new[] { "TIsPartsMechSmall=1x1", "TIsScrapAluminum=1x1" } : install ? new[] { id + "Test=1x1" } : Array.Empty<string>();
                work.aLootCOs = new[] { output }; work.strAllowLootCTsThem = "COND" + job + "Progressx5"; work.strProgressStat = "Stat" + job + "Progress";
                d.Installables[work.strName] = work;
                if (repair) MaintenanceDefinitions.Repair(d, work);
            }
        }
    }
}
