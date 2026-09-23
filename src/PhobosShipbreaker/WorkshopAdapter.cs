using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>All upstream identity, template selection and translation live here.</summary>
internal static class WorkshopAdapter
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
        Converters = new List<JsonConverter> { new VectorConverter() },
        ContractResolver = new DefinitionProperties()
    };

    private static JsonModInfo? FindMod(string workshopId, string name) => DataHandler.dictModInfos?.Values
        .FirstOrDefault(m => m != null && (!string.IsNullOrWhiteSpace(m.strWorkshopID)
            ? m.strWorkshopID.Trim() == workshopId : string.Equals(m.strName, name, StringComparison.OrdinalIgnoreCase)));

    private static bool Enabled(JsonModInfo? mod) => mod != null && !mod.GetIsDisabled();
    private static string Describe(JsonModInfo? mod) => mod == null ? "not found" :
        (string.IsNullOrWhiteSpace(mod.strModVersion) ? "version unreported" : mod.strModVersion) + (Enabled(mod) ? " enabled" : " disabled");

    internal static List<string> Inspect(out string versions)
    {
        var framework = FindMod("3798573443", "Ostranauts Crafting Framework");
        var workshop = FindMod("3798573453", "Salvage Workshop");
        var version = Chainloader.PluginInfos.TryGetValue(Plugin.FrameworkId, out var info) && info.Instance != null
            ? info.Metadata.Version : null;
        versions = "Crafting Framework plugin: " + (version?.ToString() ?? "not loaded")
            + " (minimum " + DependencyContract.MinimumFramework + ")\nCrafting Framework data: " + Describe(framework)
            + "\nSalvage Workshop data: " + Describe(workshop)
            + "\nAdapter reference: Framework/Workshop 0.8.71; other versions require the same checked definitions. No age cutoff.";
        var problems = DependencyContract.MissingDefinitions(Contains);
        string? frameworkProblem = DependencyContract.FrameworkProblem(version, Enabled(framework));
        if (frameworkProblem != null) problems.Insert(0, frameworkProblem);
        if (!Enabled(workshop)) problems.Insert(0, "Enable the Salvage Workshop native data package.");
        // Only inspect field relationships after missing definitions have been reported.
        if (problems.Count != 0) return problems;
        foreach (string variant in DependencyContract.Variants)
        {
            string id = "SWB_Sorter" + variant;
            var co = DataHandler.dictCOs[id];
            problems.AddRange(DependencyContract.MachineProblems(id, co.strItemDef, co.strLoot,
                co.aSlotsWeHave, co.jsonPI, co.aTickers, HasCondition(co, "IsContainer")));
        }
        var input = DataHandler.dictCOs["SWB_SorterInputBin"];
        if (!HasCondition(input, "IsContainer") || input.nContainerWidth <= 0 || input.nContainerHeight <= 0)
            problems.Add("SWB_SorterInputBin: valid storage container required.");
        if (DataHandler.dictSlots["SWB_SorterInput"].nItems != 1)
            problems.Add("SWB_SorterInput: expected one input-bin attachment.");
        var compartments = DataHandler.dictLoot["SWB_SorterCompartments"];
        if (!(compartments.aCOs ?? Array.Empty<string>()).SequenceEqual(new[] { "SWB_SorterInputBin=1.0x1" })
            || (compartments.aLoots?.Length ?? 0) != 0)
            problems.Add("SWB_SorterCompartments: expected exactly one input bin and no nested loot.");
        var power = DataHandler.dictPowerInfo["SWB_SorterPower"];
        if (!power.bAllowExtPower || !(power.aInputPts ?? Array.Empty<string>()).SequenceEqual(new[] { "PowerA", "PowerB" })
            || power.strIntPowerOn != "SWB_SorterPowerChange" || power.strIntPowerOff != "SWB_SorterPowerChange"
            || power.strUsePowerCT != "TIsReadyUsePower" || !string.IsNullOrEmpty(power.strRechargeCT))
            problems.Add("SWB_SorterPower: external power interface changed.");
        foreach (string material in new[] { "ItmScrapSteel", "ItmScrapTrash", "ItmScrapAluminum", "ItmScrapCarbonFiber", "ItmPartsMechSmall01", "ItmPartsElecSmall01" })
        {
            string item = DataHandler.dictCOs[material].strItemDef;
            if (string.IsNullOrEmpty(item) || !DataHandler.dictItemDefs.ContainsKey(item))
                problems.Add(material + ": missing native item definition " + item + ".");
        }
        return problems;
    }

    private static bool HasCondition(JsonCondOwner co, string condition) =>
        co.aStartingConds?.Any(s => s.StartsWith(condition + "=1.0x1", StringComparison.Ordinal)
            || s == condition + "=1x1") == true;

    private static bool Contains(string table, string id)
    {
        switch (table)
        {
            case "objects": return DataHandler.dictCOs?.ContainsKey(id) == true;
            case "items": return DataHandler.dictItemDefs?.ContainsKey(id) == true;
            case "slots": return DataHandler.dictSlots?.ContainsKey(id) == true;
            case "conditions": return DataHandler.dictConds?.ContainsKey(id) == true;
            case "triggers": return DataHandler.dictCTs?.ContainsKey(id) == true;
            case "power": return DataHandler.dictPowerInfo?.ContainsKey(id) == true;
            case "interactions": return DataHandler.dictInteractions?.ContainsKey(id) == true;
            case "loot": return DataHandler.dictLoot?.ContainsKey(id) == true;
            case "installables": return DataHandler.dictInstallables?.ContainsKey(id) == true;
            default: throw new ArgumentException("Unknown template table: " + table);
        }
    }

    private static string Rename(string text) => text
        .Replace("SWB_ModeDamage_SWB_Sorter", Content.Prefix + "ModeDamage")
        .Replace("SWB_Damage_SWB_Sorter", Content.Prefix + "Damage")
        .Replace("SWB_TSorter", Content.Prefix + "T")
        .Replace("SWB_Sorter", Content.Prefix)
        .Replace("IsSWBSorter", Content.Prefix + "Machine");

    internal static T Clone<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value, Settings), Settings)!;

    internal static Dictionary<string, T> Prepare<T>(Dictionary<string, T> table)
    {
        var result = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var pair in table.Where(p => p.Key.Contains("SWB_Sorter") ||
                     p.Key.StartsWith("SWB_TSorter", StringComparison.Ordinal) || p.Key == "IsSWBSorter"))
            result.Add(Rename(pair.Key), JsonConvert.DeserializeObject<T>(Rename(JsonConvert.SerializeObject(pair.Value, Settings)), Settings)!);
        return result;
    }

    private sealed class VectorConverter : JsonConverter
    {
        public override bool CanConvert(Type type) => type == typeof(Vector3);
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var v = (Vector3)value!;
            writer.WriteStartObject(); writer.WritePropertyName("x"); writer.WriteValue(v.x);
            writer.WritePropertyName("y"); writer.WriteValue(v.y); writer.WritePropertyName("z");
            writer.WriteValue(v.z); writer.WriteEndObject();
        }
        public override object ReadJson(JsonReader reader, Type type, object? existing, JsonSerializer serializer)
        {
            var value = JObject.Load(reader);
            return new Vector3((float)value["x"]!, (float)value["y"]!, (float)value["z"]!);
        }
    }

    private sealed class DefinitionProperties : DefaultContractResolver
    {
        // Copy loadable properties without evaluating computed game diagnostics.
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization serialization) =>
            base.CreateProperties(type, serialization).Where(p => p.Readable && p.Writable).ToList();
    }
}

/// <summary>Private copies only. No game dictionary is changed until Publish.</summary>
internal sealed class PreparedDefinitions
{
    internal readonly Dictionary<string, JsonCondOwner> Objects = WorkshopAdapter.Prepare(DataHandler.dictCOs);
    internal readonly Dictionary<string, JsonItemDef> Items = WorkshopAdapter.Prepare(DataHandler.dictItemDefs);
    internal readonly Dictionary<string, JsonSlot> Slots = WorkshopAdapter.Prepare(DataHandler.dictSlots);
    internal readonly Dictionary<string, JsonCond> Conditions = WorkshopAdapter.Prepare(DataHandler.dictConds);
    internal readonly Dictionary<string, CondTrigger> Triggers = WorkshopAdapter.Prepare(DataHandler.dictCTs);
    internal readonly Dictionary<string, JsonPowerInfo> Power = WorkshopAdapter.Prepare(DataHandler.dictPowerInfo);
    internal readonly Dictionary<string, JsonInteraction> Interactions = WorkshopAdapter.Prepare(DataHandler.dictInteractions);
    internal readonly Dictionary<string, Loot> Loot = WorkshopAdapter.Prepare(DataHandler.dictLoot);
    internal readonly Dictionary<string, JsonInstallable> Installables = WorkshopAdapter.Prepare(DataHandler.dictInstallables);

    internal void Publish()
    {
        var batch = new DefinitionTransaction();
        batch.Stage(DataHandler.dictCOs, Objects); batch.Stage(DataHandler.dictItemDefs, Items);
        batch.Stage(DataHandler.dictSlots, Slots); batch.Stage(DataHandler.dictConds, Conditions);
        batch.Stage(DataHandler.dictCTs, Triggers); batch.Stage(DataHandler.dictPowerInfo, Power);
        batch.Stage(DataHandler.dictInteractions, Interactions); batch.Stage(DataHandler.dictLoot, Loot);
        batch.Stage(DataHandler.dictInstallables, Installables);
        batch.Commit();
    }
}
