using System;
using System.Collections.Generic;
using System.Linq;

namespace PhobosShipbreaker.Core;

/// <summary>The specific upstream contract this adapter uses; no release-age cutoff.</summary>
internal static class DependencyContract
{
    internal const string MinimumFramework = "0.8.71";
    internal static readonly string[] Variants = { "Installed", "Loose", "InstalledDmg", "LooseDmg" };
    internal static readonly (string Table, string[] Names)[] Required = {
        ("objects", new[] { "SWB_WorkbenchInstalled", "SWB_SorterInstalled", "SWB_SorterLoose",
            "SWB_SorterInstalledDmg", "SWB_SorterLooseDmg", "SWB_SorterInputBin", "ItmScrapTrash",
            "ItmScrapSteel", "ItmScrapAluminum", "ItmScrapCarbonFiber", "ItmPartsMechSmall01", "ItmPartsElecSmall01" }),
        ("items", new[] { "SWB_SorterInstalled", "SWB_SorterLoose", "SWB_SorterInstalledDmg",
            "SWB_SorterLooseDmg", "ItmFloorGrate4x401", "Blank" }),
        ("slots", new[] { "SWB_SorterInput" }),
        ("conditions", new[] { "IsSWBSorter", "PhobosShipbreakerContent", "PhobosShipbreakerIsSection" }),
        ("triggers", new[] { "SWB_TSorterInstalled", "SWB_TSorterLoose", "SWB_TSorterInstalledDmg",
            "SWB_TSorterLooseDmg", "SWB_TInstalled", "PhobosShipbreakerTSection", "TIsFitContainerSolid",
            "TIsReadyUsePower", "TIsScrapSteel", "TIsScrapAluminum", "TIsPartsMechSmall", "TIsPartsElecSmall" }),
        ("power", new[] { "SWB_SorterPower" }),
        ("interactions", new[] { "SWB_ModeDamage_SWB_SorterInstalled", "SWB_ModeDamage_SWB_SorterLoose", "SWB_SorterPowerChange" }),
        ("loot", new[] { "SWB_SorterInstalled", "SWB_SorterLoose", "SWB_SorterInstalledDmg", "SWB_SorterLooseDmg",
            "SWB_Damage_SWB_SorterInstalled", "SWB_Damage_SWB_SorterLoose", "SWB_SorterCompartments" }),
        ("installables", new[] { "SWB_SorterInstalledRepair", "SWB_SorterLooseRepair", "SWB_SorterInstalledUninstall",
            "SWB_SorterLooseInstall", "SWB_SorterInstalledDmgUninstall", "SWB_SorterLooseDmgInstall" })
    };

    internal static string? FrameworkProblem(Version? loaded, bool dataEnabled)
    {
        if (loaded == null) return "Crafting Framework plugin is not loaded.";
        if (loaded < new Version(MinimumFramework)) return "Crafting Framework " + loaded + " is below required " + MinimumFramework + ".";
        return dataEnabled ? null : "Enable the Crafting Framework native data package.";
    }

    internal static List<string> MissingDefinitions(Func<string, string, bool> contains) => Required
        .SelectMany(group => group.Names.Where(name => !contains(group.Table, name))
            .Select(name => "Missing " + group.Table + ": " + name)).ToList();

    internal static readonly string[] Recipes = { "OCF_Craft_PhobosBuildShipbreakerSection", "OCF_Craft_PhobosBuildShipbreaker" };
    internal static List<string> MissingRecipes(Func<string, bool> contains) => Recipes
        .Where(id => !contains(id)).Select(id => "Missing registered recipe: " + id).ToList();

    internal static List<string> MachineProblems(string id, string? item, string? compartments,
        string[]? slots, string? power, string[]? tickers, bool container)
    {
        var problems = new List<string>();
        if (item != id) problems.Add(id + ": item definition must be " + id + ".");
        if (compartments != "SWB_SorterCompartments") problems.Add(id + ": compartment source changed.");
        if (slots == null || !slots.SequenceEqual(new[] { "SWB_SorterInput" })) problems.Add(id + ": expected one sorter input slot.");
        bool powered = id == "SWB_SorterInstalled";
        if (powered ? power != "SWB_SorterPower" : !string.IsNullOrEmpty(power)) problems.Add(id + ": power connection changed.");
        if (!(tickers ?? Array.Empty<string>()).SequenceEqual(powered ? new[] { "Power" } : Array.Empty<string>()))
            problems.Add(id + ": processing/power tickers changed.");
        if (!container) problems.Add(id + ": output container condition is absent.");
        return problems;
    }
}
