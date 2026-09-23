using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhobosShipbreaker.Core;
using UnityEngine;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosShipbreaker;

internal static class Content
{
    internal const string Prefix = "PhobosShipbreaker";
    internal const string Installed = Prefix + "Installed";
    internal const string Loose = Prefix + "Loose";
    internal const string InputBin = Prefix + "InputBin";
    internal const string InputSlot = Prefix + "Input";
    internal static bool Ready { get; private set; }
    internal static string Status { get; private set; } = "Waiting for mod data";
    internal static string DependencyStatus { get; private set; } = "Dependency inspection awaits native data loading.";
    private static bool definitionsRegistered;

    internal static bool IsMachine(string? id) => id == Installed || id == Loose ||
        id == Installed + "Dmg" || id == Loose + "Dmg";

    internal static void Register(Action<string> log)
    {
        Ready = false;
        definitionsRegistered = false;
        DependencyStatus = "Dependency inspection did not complete.";
        try
        {
            var problems = NativeAdapter.Inspect(out string versions);
            DependencyStatus = versions + "\n" + (problems.Count == 0 ? "Native definition precheck passed." : string.Join("\n", problems));
            if (problems.Count != 0)
            {
                Status = "Dependency check failed: " + problems[0] + " Use phobosshipbreaker dependencies for details.";
                log(Status + "\n" + DependencyStatus);
                return;
            }
            var prepared = Prepare(Plugin.Options.ControlsKey.ToString(), Plugin.Options.CycleSeconds, Plugin.Options.IdleKW, Plugin.Options.WorkingKW);
            prepared.Publish();
            ConstructionRegistry.RegisterPack(Plugin.Id, NativeAdapter.RecipePath);
            definitionsRegistered = true;
            Status = "Shipbreaker definitions registered; awaiting construction recipe checks.";
            log(Status + "\n" + DependencyStatus);
        }
        catch (Exception ex)
        {
            Status = "Shipbreaker registration failed: " + ex.Message;
            DependencyStatus += "\n" + Status;
            log("Shipbreaker disabled: " + ex);
        }
    }

    internal static NativeDefinitions Prepare(string controlsKey = "F9", double cycleSeconds = 60, double idleKW = 0.12, double workingKW = 30)
    {
        var prepared = MachineDefinitions.Create();

        foreach (string condition in new[] { ProcessRules.Progress, ProcessRules.Revision, ProcessRules.Duration, ProcessRules.Working })
            prepared.Conditions[condition] = new JsonCond { strName = condition,
                strNameFriendly = condition, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };

        foreach (string variant in new[] { Installed, Loose, Installed + "Dmg", Loose + "Dmg" })
        {
            var co = prepared.Objects[variant];
            co.strNameFriendly = co.strNameShort = "Phobos Powered Dismantling Fixture" +
                (variant.EndsWith("Dmg", StringComparison.Ordinal) ? " (Damaged)" : "");
            co.strDesc = "4 x 4 industrial fixture; 160 kg. Feed up to four ordinary loose wall panels. " +
                controlsKey + " opens controls while nearby. A new batch takes " + cycleSeconds +
                " seconds at " + workingKW + " kW and recovers 11 kg of parts/scrap " +
                "plus 13 kg of retained mixed residue. Separate 8 x 8 output tray. Reload paused.";
            co.nContainerWidth = co.nContainerHeight = ProcessRules.OutputSize;
            co.inventoryWidth = co.inventoryHeight = ProcessRules.Footprint;
            co.nStackLimit = 1;
            SetStat(co, "StatMass", ProcessRules.MachineKg);
            SetStat(co, "StatBasePrice", variant.EndsWith("Dmg", StringComparison.Ordinal) ? 400 : 1600);
            co.mapPoints = new[] { "use,0,-40", "PowerA,-24,24", "PowerB,24,24" };
            co.dictSlotsLayout = new Dictionary<string, Vector3> {
                ["self"] = Vector3.zero, [InputSlot] = new Vector3(-112, 0, 0) };

            var item = prepared.Items[variant];
            string intact = variant.StartsWith(Installed, StringComparison.Ordinal) ? Installed : Loose;
            ApplyArtwork(co, item, variant, intact + "Dmg");
            item.nCols = ProcessRules.Footprint;
            bool installed = variant.StartsWith(Installed, StringComparison.Ordinal);
            item.aSocketAdds = Enumerable.Repeat(installed ? "TILFixtureAdds" : "TILItemAdds",
                ProcessRules.Footprint * ProcessRules.Footprint).ToArray();
            item.aSocketForbids = Border(installed ? "TILObstruction" : "TILItemForbids");
            item.aSocketReqs = Border(installed ? "TILFloor" : "Blank");
        }
        var bin = prepared.Objects[InputBin];
        bin.strNameFriendly = bin.strNameShort = "Wall-panel feed (4 panels / 96 kg)";
        bin.strDesc = "Ordinary loose wall panels only. Up to four separate, empty panels; no stacks.";
        bin.strContainerCT = "TIsFitContainerSolid";
        prepared.Slots[InputSlot].strNameFriendly = "Wall-panel feed";
        var power = prepared.Power[Prefix + "Power"];
        power.fAmount = idleKW / 3600;
        power.strOverrideCond = ProcessRules.Working;
        power.fOverrideAmount = workingKW / 3600;

        RegisterAssemblySection(prepared);
        var residue = NativeDefinitions.Clone(DataHandler.dictCOs["ItmScrapTrash"]);
        // Keep Phobos residue art independent of the native trash definition.
        var residueItem = NativeDefinitions.Clone(DataHandler.dictItemDefs[residue.strItemDef]);
        residue.strName = residue.strItemDef = ProcessRules.Residue;
        residueItem.strName = residue.strItemDef;
        residue.strNameFriendly = residue.strNameShort = "Mixed panel residue (13 kg)";
        residue.strDesc = "Retained panel material not recovered as useful stock. Not ordinary sortable trash. " +
            "Keep, haul or jettison as a physical item; no refining recipe yet.";
        residue.aStartingConds = new[] { "IsSolid=1.0x1", "StatMass=1.0x13", "StatBasePrice=1.0x0" };
        residue.aUpdateCommands = Array.Empty<string>();
        residue.nStackLimit = 1;
        residue.mapSlotEffects = new[] { "heldL", "HeldItmDefaultL", "heldR", "HeldItmDefaultR" };
        ApplyArtwork(residue, residueItem, ProcessRules.Residue);
        prepared.Items[residueItem.strName] = residueItem;
        prepared.Objects[residue.strName] = residue;
        return prepared;
    }

    internal static void ConfirmRecipes(Action<string> log)
    {
        if (!definitionsRegistered) return;
        var missing = DependencyContract.MissingRecipes(id => DataHandler.dictInteractions?.ContainsKey(id) == true);
        Ready = missing.Count == 0 && ConstructionRegistry.Ready(Plugin.Id);
        Status = Ready ? "Shipbreaker definitions ready" :
            "Construction registration incomplete; processing disabled. Use phobosshipbreaker dependencies.";
        DependencyStatus += "\n" + (Ready ? "Both construction recipes registered. In-game compatibility remains unverified."
            : string.Join("\n", missing) + "\n" + ConstructionRegistry.Status(Plugin.Id) + "\nInspect the Phobos Framework log and matching Phobos plugin/data package.");
        log(Status + "\n" + DependencyStatus);
    }

    private static void RegisterAssemblySection(NativeDefinitions prepared)
    {
        // Two tangible 80 kg sections keep every craft within our bounded 100-unit
        // input limit without reducing the full machine's mass or material bill.
        var section = NativeDefinitions.Clone(DataHandler.dictCOs["ItmScrapSteel"]);
        var item = NativeDefinitions.Clone(DataHandler.dictItemDefs[section.strItemDef]);
        section.strName = section.strItemDef = ProcessRules.AssemblySection;
        section.strNameFriendly = section.strNameShort = "Dismantling Fixture Assembly Section";
        section.strDesc = "An unpowered 80 kg, 4 x 4 assembly section. Combine two at an installed table " +
            "or supported workbench to finish one powered dismantling fixture. Not ordinary scrap; cannot process panels by itself.";
        section.inventoryWidth = section.inventoryHeight = ProcessRules.Footprint;
        section.nStackLimit = 1;
        section.aStartingConds = new[] { "IsSolid=1.0x1", "IsRigid=1.0x1",
            ProcessRules.AssemblySectionCondition + "=1.0x1", "StatBasePrice=1.0x800" };
        SetStat(section, "StatMass", ProcessRules.AssemblySectionKg);
        section.aUpdateCommands = Array.Empty<string>();
        section.mapChargeProfiles = Array.Empty<string>();
        section.mapSlotEffects = new[] { "heldL", "HeldItmDefaultL", "heldR", "HeldItmDefaultR" };
        item.strName = section.strItemDef;
        ApplyArtwork(section, item, ProcessRules.AssemblySection);
        item.nCols = ProcessRules.Footprint;
        item.aSocketAdds = Enumerable.Repeat("TILItemAdds", ProcessRules.Footprint * ProcessRules.Footprint).ToArray();
        item.aSocketForbids = Border("TILItemForbids");
        item.aSocketReqs = Border("Blank");
        prepared.Items[item.strName] = item;
        prepared.Objects[section.strName] = section;
    }

    private static void ApplyArtwork(JsonCondOwner owner, JsonItemDef item, string name, string damaged = "blank")
    {
        const string path = "phobos/shipbreaker/";
        item.strImg = path + name;
        item.strImgNorm = path + name + "Normal";
        item.strImgDamaged = damaged == "blank" ? "blank" : path + damaged;
        owner.strPortraitImg = path + name + "Portrait";
    }

    private static void SetStat(JsonCondOwner co, string name, double value) => co.aStartingConds =
        co.aStartingConds.Where(s => !s.StartsWith(name + "=", StringComparison.Ordinal))
            .Concat(new[] { name + "=1.0x" + value.ToString(CultureInfo.InvariantCulture) }).ToArray();

    private static string[] Border(string interior)
    {
        int side = ProcessRules.Footprint + 2;
        return Enumerable.Range(0, side * side).Select(i =>
            i % side > 0 && i % side < side - 1 && i / side > 0 && i / side < side - 1 ? interior : "Blank").ToArray();
    }

}
