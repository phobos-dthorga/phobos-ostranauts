using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Bootstrap;
using Phobos.Ostranauts.Framework;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class NativeAdapter
{
    internal static string RecipePath { get; private set; } = "";
    internal static List<string> Inspect(out string versions)
    {
        var version = Chainloader.PluginInfos.TryGetValue(FrameworkInfo.PluginId, out var info) && info.Instance != null
            ? info.Metadata.Version : null;
        var data = DataHandler.dictModInfos.Values.FirstOrDefault(m => m.strName == "Phobos Shipbreaker" && !m.GetIsDisabled());
        versions = "Phobos Framework plugin: " + (version?.ToString() ?? "not loaded") +
            " (minimum " + DependencyContract.MinimumFramework + ")\nConstruction and machinery use Phobos Framework and native game definitions.\n" +
            "Crafting Framework and Salvage Workshop are optional. Auto Nav is optional for future positioning.";
        var problems = DependencyContract.MissingDefinitions(Contains);
        string? frameworkProblem = DependencyContract.FrameworkProblem(version);
        if (frameworkProblem != null) problems.Insert(0, frameworkProblem);
        RecipePath = "";
        if (data == null) problems.Insert(0, "Enable the Phobos Shipbreaker native data package.");
        else
        {
            RecipePath = Path.Combine(data.GetDirectory(), "framework", "recipes.json");
            if (!File.Exists(RecipePath)) problems.Add("Phobos construction pack missing. Update the plugin and native data together.");
        }
        foreach (string id in DependencyContract.Materials)
            if (DataHandler.dictCOs.TryGetValue(id, out var co) &&
                (string.IsNullOrEmpty(co.strItemDef) || !DataHandler.dictItemDefs.ContainsKey(co.strItemDef)))
                problems.Add(id + ": missing native item definition.");
        return problems;
    }
    private static bool Contains(string table, string id)
    {
        switch (table)
        {
            case "objects": return DataHandler.dictCOs.ContainsKey(id);
            case "items": return DataHandler.dictItemDefs.ContainsKey(id);
            case "conditions": return DataHandler.dictConds.ContainsKey(id);
            case "triggers": return DataHandler.dictCTs.ContainsKey(id);
            case "loot": return DataHandler.dictLoot.ContainsKey(id);
            case "interactions": return DataHandler.dictInteractions.ContainsKey(id);
            default: throw new ArgumentException("Unknown native table: " + table);
        }
    }
}
