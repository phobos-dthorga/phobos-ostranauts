using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>The native install, uninstall, repair and damage contract Shipbreaker's machines have used since 0.1:
/// four forms (Installed, Loose, InstalledDmg, LooseDmg), a family condition, per-form triggers, mode-switch damage and
/// the native installables, each under the ids every saved machine and in-progress job already names. Moved from
/// Shipbreaker into Framework (0.58.0) so equipment that changes owner (the water tanks) keeps those ids exactly; the
/// output is byte-for-byte what Shipbreaker generated. Content sets names, art, stats and economy afterwards, and
/// since Framework 0.70.0 declares what the family's inventory is for through <see cref="EquipmentInventory.Apply"/>
/// (the 8 x 8 grid here is only the starting point every saved family had).</summary>
public static class MachineFamilies
{
    public static readonly string[] Forms = { "Installed", "Loose", "InstalledDmg", "LooseDmg" };
    /// <param name="conditionName">The family condition's display name.</param>
    /// <param name="name">Placeholder full and short names, replaced by the content that owns the family.</param>
    public static void Add(NativeDefinitions d, string P, string buildCategory, string conditionName, string name, string shortName)
    {
        d.Conditions.Add(P + "Machine", new JsonCond { strName = P + "Machine", strNameFriendly = conditionName,
            strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 });
        foreach (string state in Forms)
        {
            string id = P + state;
            bool installed = state.StartsWith("Installed", StringComparison.Ordinal), damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var conditions = new List<string> { "IsSalvageValueHigh=1.0x1", "IsSolid=1.0x1", "IsCategoryIndustrialProducts=1.0x1",
                "IsMechanical=1.0x1", "StatInstallProgressMax=1.0x100", "StatUninstallProgressMax=1.0x100",
                P + "Machine=1.0x1", "StatDamageMax=1.0x20", "IsContainer=1.0x1", "StatMass=1.0x160", "StatBasePrice=1.0x1600" };
            conditions.Add(installed ? "IsInstalled=1.0x1" : "IsCumbersome=1.0x1");
            if (damaged) conditions.AddRange(new[] { "IsDamaged=1.0x1", "StatRepairProgressMax=1.0x100" });
            d.Objects.Add(id, new JsonCondOwner {
                strName = id, strNameFriendly = name, strNameShort = shortName,
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
            AddInstallable(d, P, state, installed ? "Uninstall" : "Install", buildCategory);
            if (damaged) AddInstallable(d, P, state, "Repair", buildCategory);
        }
    }
    /// <summary>A socket grid one tile wider than the footprint on every side: the interior value inside, Blank on the rim.</summary>
    public static string[] Border(int footprint, string interior)
    {
        int side = footprint + 2;
        return Enumerable.Range(0, side * side).Select(i =>
            i % side > 0 && i % side < side - 1 && i / side > 0 && i / side < side - 1 ? interior : "Blank").ToArray();
    }
    public static Loot ItemLoot(string id, string item) => new Loot { strName = id, strType = "item",
        aCOs = new[] { item + "=1.0x1" }, aLoots = Array.Empty<string>() };
    private static void AddInstallable(NativeDefinitions d, string P, string state, string job, string buildCategory)
    {
        bool repair = job == "Repair", install = job == "Install";
        string source = P + state, intact = state.Replace("Dmg", ""), id = P + (repair ? intact : state) + job;
        string output = repair ? P + intact : P + state.Replace(install ? "Loose" : "Installed", install ? "Installed" : "Loose");
        d.Installables.Add(id, new JsonInstallable {
            strName = id, strActionCO = source, strActionGroup = "Work", strJobType = job.ToLowerInvariant(),
            strInteractionName = job, strInteractionTemplate = "ACT" + job + (repair ? "" : "NoSparks") + "TEMP",
            strStartInstall = install ? output : null, strBuildType = install ? buildCategory : null,
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
