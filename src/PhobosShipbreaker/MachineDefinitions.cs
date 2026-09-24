using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosShipbreaker;

// Phobos-authored definitions built against the native installable interface.
// Preserve saved Phobos identities; no Workshop template lookup or bundled game data.
internal static class MachineDefinitions
{
    private const string P = "PhobosShipbreaker";
    internal static NativeDefinitions Create()
    {
        var d = new NativeDefinitions();
        AddFamily(d, P);
        AddFeed(d);
        return d;
    }

    // The same native install/repair/damage contract serves all three machines.
    internal static void AddFamily(NativeDefinitions d, string P)
    {
        d.Conditions.Add(P + "Machine", new JsonCond { strName = P + "Machine", strNameFriendly = Text.Get("MachineDefinitions.dismantling_fixture"),
            strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 });
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            string id = P + state;
            bool installed = state.StartsWith("Installed", StringComparison.Ordinal), damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var conditions = new List<string> { "IsSalvageValueHigh=1.0x1", "IsSolid=1.0x1", "IsCategoryIndustrialProducts=1.0x1",
                "IsMechanical=1.0x1", "StatInstallProgressMax=1.0x100", "StatUninstallProgressMax=1.0x100",
                P + "Machine=1.0x1", "StatDamageMax=1.0x20", "IsContainer=1.0x1", "StatMass=1.0x160", "StatBasePrice=1.0x1600" };
            conditions.Add(installed ? "IsInstalled=1.0x1" : "IsCumbersome=1.0x1");
            if (damaged) conditions.AddRange(new[] { "IsDamaged=1.0x1", "StatRepairProgressMax=1.0x100" });
            d.Objects.Add(id, new JsonCondOwner {
                strName = id, strNameFriendly = Text.Get("MachineDefinitions.phobos_powered_dismantling_fixture"), strNameShort = Text.Get("MachineDefinitions.dismantling_fixture_2"),
                strType = "Item", strItemDef = id, strLoot = P + "Compartments", strContainerCT = "TIsFitContainerSolid",
                nStackLimit = 1, nContainerWidth = 8, nContainerHeight = 8, inventoryWidth = 4, inventoryHeight = 4,
                aInteractions = new[] { "Inventory" }, aStartingConds = conditions.ToArray(),
                aSlotsWeHave = new[] { P + "Input" }, mapGUIPropMaps = new[] { "GUIInv", "Inventory" },
                mapSlotEffects = new[] { "drag", "Blank" }, mapPoints = Array.Empty<string>(),
                jsonPI = installed && !damaged ? P + "Power" : null,
                aTickers = installed && !damaged ? new[] { "Power" } : Array.Empty<string>(),
                aUpdateCommands = new[] { "Destructable,StatDamage," + (damaged ? "ACTDefaultDestroy" : P + "Damage" + state) + ",StatDamageMax,1.0" }
            });
            d.Items.Add(id, new JsonItemDef { strName = id, fZScale = 0.5f, strDmgColor = "DamageTintDefault" });
            d.Loot.Add(id, ItemLoot(id, id));
            var req = new List<string> { P + "Machine" };
            var forbid = new List<string>();
            (installed ? req : forbid).Add("IsInstalled"); (damaged ? req : forbid).Add("IsDamaged");
            d.Triggers.Add(P + "T" + state, new CondTrigger { strName = P + "T" + state, fChance = 1, fCount = 1,
                bAND = true, aReqs = req.ToArray(), aForbids = forbid.ToArray(), aTriggers = Array.Empty<string>() });
            if (!damaged)
            {
                string damage = P + "ModeDamage" + state;
                d.Interactions.Add(damage, new JsonInteraction { strName = damage, strThemType = "Self", bIgnoreFeelings = true,
                    objLootModeSwitch = id + "Dmg", aLootItms = Array.Empty<string>() });
                d.Loot.Add(P + "Damage" + state, new Loot { strName = P + "Damage" + state,
                    strType = "interaction", aCOs = new[] { damage + "=1.0x1" }, aLoots = Array.Empty<string>() });
            }
            AddInstallable(d, P, state, installed ? "Uninstall" : "Install");
            if (damaged) AddInstallable(d, P, state, "Repair");
        }
    }

    internal static void AddFeed(NativeDefinitions d, string P = "PhobosShipbreaker", string feedCondition = "IsWall1x1")
    {
        // Native ordinary walls are cumbersome. Keep native containment exclusions,
        // then narrow acceptance to wall panels; FeedPatch enforces identity/mass/count.
        d.Triggers.Add(P + "TFeed", new CondTrigger { strName = P + "TFeed", fChance = 1, fCount = 1,
            bAND = true, aReqs = new[] { feedCondition }, aForbids = Array.Empty<string>(),
            aTriggers = new[] { "TIsFitContainerSolidCumbersome" } });
        d.Objects.Add(P + "InputBin", new JsonCondOwner { strName = P + "InputBin", strNameFriendly = Text.Get("MachineDefinitions.wall_panel_feed"),
            strNameShort = Text.Get("MachineDefinitions.wall_panel_feed"), strType = "Item", strItemDef = "Blank", strPortraitImg = "blank",
            strContainerCT = P + "TFeed", nStackLimit = 1, bSlotLocked = true,
            nContainerWidth = 4, nContainerHeight = 4, aInteractions = Array.Empty<string>(),
            aStartingConds = new[] { "IsContainer=1.0x1", "IsSystem=1.0x1" }, mapSlotEffects = new[] { P + "Input", "Blank" } });
        d.Slots.Add(P + "Input", new JsonSlot { strName = P + "Input", strNameFriendly = Text.Get("MachineDefinitions.wall_panel_feed"),
            strHitboxImage = "blank", nItems = 1, nDepth = 15, bCarried = true });
        d.Loot.Add(P + "Compartments", ItemLoot(P + "Compartments", P + "InputBin"));
        d.Power.Add(P + "Power", new JsonPowerInfo { strName = P + "Power", strUsePowerCT = "TIsReadyUsePower",
            aInputPts = new[] { "PowerA", "PowerB" }, bAllowExtPower = true,
            strIntPowerOn = P + "PowerChange", strIntPowerOff = P + "PowerChange" });
        d.Interactions.Add(P + "PowerChange", new JsonInteraction { strName = P + "PowerChange", strThemType = "Self",
            bIgnoreFeelings = true, aLootItms = Array.Empty<string>() });
    }
    private static Loot ItemLoot(string id, string item) => new Loot { strName = id, strType = "item",
        aCOs = new[] { item + "=1.0x1" }, aLoots = Array.Empty<string>() };
    private static void AddInstallable(NativeDefinitions d, string P, string state, string job)
    {
        bool repair = job == "Repair", install = job == "Install";
        string source = P + state, intact = state.Replace("Dmg", ""), id = P + (repair ? intact : state) + job;
        string output = repair ? P + intact : P + state.Replace(install ? "Loose" : "Installed", install ? "Installed" : "Loose");
        d.Installables.Add(id, new JsonInstallable {
            strName = id, strActionCO = source, strActionGroup = "Work", strJobType = job.ToLowerInvariant(),
            strInteractionName = job, strInteractionTemplate = "ACT" + job + (repair ? "" : "NoSparks") + "TEMP",
            strStartInstall = install ? output : null, strBuildType = install ? "MIS" : null,
            CTThem = repair ? "TIsRepairableNotContained" : P + "T" + state,
            aInputs = repair ? new[] { "TIsPartsMechSmall=1.0x1", "TIsScrapAluminum=1.0x1" } :
                install ? new[] { P + "T" + state + "=1.0x1" } : Array.Empty<string>(),
            aToolCTsUse = repair ? new[] { "TIsToolMortorq" } : Array.Empty<string>(),
            aLootCOs = new[] { output }, fDuration = 0.001f, fTargetPointRange = 2,
            strAllowLootCTsUs = "CTWorkProgressMISC", strAllowLootCTsThem = "COND" + job + "Progressx5",
            strProgressStat = "Stat" + job + "Progress", strCTThemMultCondUs = "StatInstallRateMISC",
            strCTThemMultCondTools = repair ? "IsToolMortorq" : null
        });
    }
}
