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
    internal static string Status { get; private set; } = Text.Get("Content.waiting_for_mod_data");
    internal static string DependencyStatus { get; private set; } = Text.Get("Content.dependency_inspection_awaits_native_data_loading");
    private static bool definitionsRegistered;

    internal static bool IsMachine(string? id) => id == Installed || id == Loose ||
        id == Installed + "Dmg" || id == Loose + "Dmg";

    internal static void Register(Action<string> log)
    {
        Ready = false;
        definitionsRegistered = false;
        DependencyStatus = Text.Get("Content.dependency_inspection_did_not_complete");
        try
        {
            var problems = NativeAdapter.Inspect(out string versions);
            DependencyStatus = versions + "\n" + (problems.Count == 0 ? Text.Get("Content.native_definition_precheck_passed") : string.Join("\n", problems));
            if (problems.Count != 0)
            {
                Status = Text.Get("Content.dependency_check_failed_use_phobosshipbreaker_dependencies_for", problems[0]);
                log(Status + "\n" + DependencyStatus);
                return;
            }
            var prepared = Prepare(Plugin.Options.ControlsKey.ToString(), Plugin.Options.CycleSeconds, Plugin.Options.IdleKW, Plugin.Options.WorkingKW, Plugin.Options.CollectorKW);
            prepared.Publish();
            ConstructionRegistry.RegisterPack(Plugin.Id, NativeAdapter.RecipePath);
            definitionsRegistered = true;
            Status = Text.Get("Content.shipbreaker_definitions_registered_awaiting_construction_recipe_checks");
            log(Status + "\n" + DependencyStatus);
        }
        catch (Exception ex)
        {
            Status = Text.Get("Content.shipbreaker_registration_failed", ex.Message);
            DependencyStatus += "\n" + Status;
            log(Text.Get("Content.shipbreaker_disabled", ex));
        }
    }

    internal static NativeDefinitions Prepare(string controlsKey = "F9", double cycleSeconds = 60, double idleKW = 0.12, double workingKW = 30, double collectorKW = CollectorRules.WorkingKW)
    {
        var prepared = MachineDefinitions.Create();

        foreach (string condition in new[] { ProcessRules.Progress, ProcessRules.Revision, ProcessRules.Duration, ProcessRules.Working })
            prepared.Conditions[condition] = new JsonCond { strName = condition,
                strNameFriendly = condition, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };

        foreach (string variant in new[] { Installed, Loose, Installed + "Dmg", Loose + "Dmg" })
        {
            var co = prepared.Objects[variant];
            co.strNameFriendly = co.strNameShort = Text.Get("Content.phobos_powered_dismantling_fixture", (variant.EndsWith("Dmg", StringComparison.Ordinal) ? Text.Get("Content.damaged") : ""));
            co.strDesc = Text.Get("Content.x_industrial_fixture_kg_feed_up_to", controlsKey, cycleSeconds, workingKW, ProcessRules.Footprint, ProcessRules.MachineKg, ProcessRules.FeedCapacity, ProcessRules.InputKg - ProcessRules.LegacyResidueKg, ProcessRules.LegacyResidueKg);
            co.nContainerWidth = co.nContainerHeight = ProcessRules.OutputSize;
            co.inventoryWidth = co.inventoryHeight = ProcessRules.Footprint;
            co.nStackLimit = 1;
            SetStat(co, "StatMass", ProcessRules.MachineKg);
            SetStat(co, "StatBasePrice", variant.EndsWith("Dmg", StringComparison.Ordinal) ? 400 : 1600);
            co.mapPoints = new[] { "use,0,-40", "PowerA,-24,24", "PowerB,24,24" };
            // A positioned child slot loses its native title/tab. Let Inventory open
            // the feed as a named window instead of an unlabeled grid behind the crew.
            co.dictSlotsLayout = new Dictionary<string, Vector3> { ["self"] = Vector3.zero };

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
        bin.strNameFriendly = bin.strNameShort = Text.Get("Content.wall_panel_feed_panels_kg", ProcessRules.FeedCapacity, ProcessRules.FeedCapacity * ProcessRules.InputKg);
        bin.strDesc = Text.Get("Content.ordinary_loose_wall_panels_only_up_to", ProcessRules.FeedCapacity);
        prepared.Slots[InputSlot].strNameFriendly = Text.Get("Content.wall_panel_feed");
        prepared.Slots[InputSlot].bHide = true;
        var power = prepared.Power[Prefix + "Power"];
        power.fAmount = idleKW / Phobos.Ostranauts.Framework.Units.SecondsPerHour;
        power.strOverrideCond = ProcessRules.Working;
        power.fOverrideAmount = workingKW / Phobos.Ostranauts.Framework.Units.SecondsPerHour;

        RegisterAssemblySection(prepared);
        var residue = NativeDefinitions.Clone(DataHandler.dictCOs["ItmScrapTrash"]);
        // Keep Phobos residue art independent of the native trash definition.
        var residueItem = NativeDefinitions.Clone(DataHandler.dictItemDefs[residue.strItemDef]);
        residue.strName = residue.strItemDef = ProcessRules.Residue;
        residueItem.strName = residue.strItemDef;
        residue.strNameFriendly = residue.strNameShort = Text.Get("Content.mixed_panel_residue_kg", ProcessRules.LegacyResidueKg);
        residue.strDesc = Text.Get("Content.retained_panel_material_not_recovered_as_useful");
        residue.aStartingConds = new[] { "IsSolid=1.0x1", "StatMass=1.0x13", "StatBasePrice=1.0x0" };
        residue.aUpdateCommands = Array.Empty<string>();
        residue.nStackLimit = 1;
        residue.mapSlotEffects = new[] { "heldL", "HeldItmDefaultL", "heldR", "HeldItmDefaultR" };
        ApplyArtwork(residue, residueItem, ProcessRules.Residue);
        prepared.Items[residueItem.strName] = residueItem;
        prepared.Objects[residue.strName] = residue;
        IntakeDefinitions.Add(prepared);
        CollectorDefinitions.Add(prepared, collectorKW);
        EquipmentEconomy.Apply(prepared);
        return prepared;
    }

    internal static void ConfirmRecipes(Action<string> log)
    {
        if (!definitionsRegistered) return;
        var missing = DependencyContract.MissingRecipes(id => DataHandler.dictInteractions?.ContainsKey(id) == true);
        Ready = missing.Count == 0 && ConstructionRegistry.Ready(Plugin.Id);
        Status = Ready ? Text.Get("Content.shipbreaker_definitions_ready") :
            Text.Get("Content.construction_registration_incomplete_processing_disabled_use_phobosshipbreaker");
        DependencyStatus += "\n" + (Ready ? Text.Get("Content.all_five_construction_recipes_registered_in_game")
            : Text.Get("Content.inspect_the_phobos_framework_log_and_matching", string.Join("\n", missing), ConstructionRegistry.Status(Plugin.Id)));
        log(Status + "\n" + DependencyStatus);
    }

    private static void RegisterAssemblySection(NativeDefinitions prepared)
    {
        // Two tangible 80 kg sections keep every craft within our bounded 100-unit
        // input limit without reducing the full machine's mass or material bill.
        var section = NativeDefinitions.Clone(DataHandler.dictCOs["ItmScrapSteel"]);
        var item = NativeDefinitions.Clone(DataHandler.dictItemDefs[section.strItemDef]);
        section.strName = section.strItemDef = ProcessRules.AssemblySection;
        section.strNameFriendly = section.strNameShort = Text.Get("Content.dismantling_fixture_assembly_section");
        section.strDesc = Text.Get("Content.an_unpowered_kg_x_assembly_section_combine", ProcessRules.AssemblySectionKg, ProcessRules.Footprint);
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

    internal static void ApplyArtwork(JsonCondOwner owner, JsonItemDef item, string name, string damaged = "blank")
    {
        const string path = "phobos/shipbreaker/";
        item.strImg = path + name;
        item.strImgNorm = path + name + "Normal";
        item.strImgDamaged = damaged == "blank" ? "blank" : path + damaged;
        owner.strPortraitImg = path + name + "Portrait";
    }

    internal static void SetStat(JsonCondOwner co, string name, double value) => co.aStartingConds =
        co.aStartingConds.Where(s => !s.StartsWith(name + "=", StringComparison.Ordinal))
            .Concat(new[] { name + "=1.0x" + value.ToString(CultureInfo.InvariantCulture) }).ToArray();

    private static string[] Border(string interior)
    {
        int side = ProcessRules.Footprint + 2;
        return Enumerable.Range(0, side * side).Select(i =>
            i % side > 0 && i % side < side - 1 && i / side > 0 && i / side < side - 1 ? interior : "Blank").ToArray();
    }

}
