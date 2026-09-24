using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Opt-in first-economy migration of save DTO copies. Never edits save files.</summary>
public static class EquipmentSaveUpgrade
{
    public const string Marker = "PhobosEquipmentEconomyV1";
    private sealed class Rule
    {
        internal JsonCondOwner Definition = null!;
        internal double LegacyMount, LegacyRepair;
    }
    private static readonly Dictionary<string, Rule> Rules = new Dictionary<string, Rule>(StringComparer.Ordinal);
    internal static void BeginLoad() => Rules.Clear();
    public static void Register(NativeDefinitions d, string savedId, string definitionId, double legacyMount = 100, double legacyRepair = 100)
    {
        d.Conditions[Marker] = new JsonCond { strName = Marker, strNameFriendly = Text.Get("EquipmentSaveUpgrade.equipment_economy_revision"), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        var definition = d.Objects[definitionId];
        MaintenanceDefinitions.SetStat(definition, Marker, 1);
        Rules[savedId] = new Rule { Definition = definition, LegacyMount = legacyMount, LegacyRepair = legacyRepair };
    }

    internal static JsonCondOwnerSave Upgrade(JsonCondOwnerSave saved)
    {
        if (saved?.strCODef == null || !Rules.TryGetValue(saved.strCODef, out var rule)) return saved!;
        var old = saved.aConds ?? Array.Empty<string>();
        if (Amount(old, Marker) >= 1) return saved;
        var copy = NativeDefinitions.Clone(saved);
        var values = old.Contains("DEFAULT")
            ? rule.Definition.aStartingConds.Concat(old.Where(s => s != "DEFAULT")).ToList() : old.ToList();
        foreach (string stat in new[] { "StatBasePrice", "StatDismantleProgressMax", "StatInstallProgressMax", "StatUninstallProgressMax", "StatRepairProgressMax" })
        {
            double value = Amount(rule.Definition.aStartingConds, stat);
            if (value <= 0) continue;
            string progress = stat.EndsWith("Max", StringComparison.Ordinal) ? stat.Substring(0, stat.Length - 3) : "";
            if (progress != "" && Amount(old, progress) > 0)
            {
                // Grandfather work already in progress, including DEFAULT-compressed saves.
                double previous = Amount(old, stat);
                value = previous > 0 ? previous : stat == "StatRepairProgressMax" ? rule.LegacyRepair : rule.LegacyMount;
            }
            Replace(values, stat, value);
        }
        Replace(values, Marker, 1);
        copy.aConds = values.ToArray(); copy.aCondReveals = null;
        return copy;
    }
    private static void Replace(List<string> values, string stat, double value)
    {
        values.RemoveAll(s => s.StartsWith(stat + "=", StringComparison.Ordinal));
        values.Add(stat + "=1.0x" + value.ToString(CultureInfo.InvariantCulture));
    }
    internal static double Amount(IEnumerable<string> values, string stat)
    {
        string? term = values.FirstOrDefault(s => s.StartsWith(stat + "=", StringComparison.Ordinal));
        if (term == null) return 0;
        string amount = term.Substring(term.LastIndexOf('x') + 1);
        return double.TryParse(amount, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : 0;
    }
}

[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.SetData))]
internal static class EquipmentLoadPatch
{
    private static void Prefix(ref JsonCondOwnerSave jCOSIn)
    {
        if (jCOSIn != null) jCOSIn = EquipmentSaveUpgrade.Upgrade(jCOSIn);
    }
}
