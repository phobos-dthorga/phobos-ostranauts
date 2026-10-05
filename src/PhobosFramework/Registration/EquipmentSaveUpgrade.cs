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
    // Definition id -> whether the price follows the saved item's remaining mass.
    private static readonly Dictionary<string, bool> Prices = new Dictionary<string, bool>(StringComparer.Ordinal);
    internal static void BeginLoad() { Rules.Clear(); Prices.Clear(); }
    public static void Register(NativeDefinitions d, string savedId, string definitionId, double legacyMount = 100, double legacyRepair = 100)
    {
        d.Conditions[Marker] = new JsonCond { strName = Marker, strNameFriendly = Text.Get("EquipmentSaveUpgrade.equipment_economy_revision"), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        var definition = d.Objects[definitionId];
        MaintenanceDefinitions.SetStat(definition, Marker, 1);
        Rules[savedId] = new Rule { Definition = definition, LegacyMount = legacyMount, LegacyRepair = legacyRepair };
    }

    /// <summary>Saved items of this definition take its current price on every load (Framework 0.68.0, for the owner's
    /// retroactive refining prices of 1 October 2026): materials whose price is a balance decision, not a property of
    /// the item. Idempotent and markerless, so a later price change reaches saved items too. Works for a native
    /// definition amended in place, because the live definition is read at load. With <paramref name="byMass"/>
    /// (Framework 0.102.0) a part-used charge, whose own code prices what is left in it, takes the current price in
    /// proportion to its remaining mass, never more than a full one.</summary>
    public static void FollowPrice(string definitionId, bool byMass = false)
    {
        if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("A definition id is required.");
        Prices[definitionId] = byMass;
    }
    /// <summary>The definition's price for what is left of a charge: full price at full mass or more.</summary>
    internal static double ScaledPrice(double price, double fullKg, double savedKg) =>
        fullKg > 0 && savedKg > 0 ? price * Math.Min(savedKg, fullKg) / fullKg : price;

    internal static JsonCondOwnerSave Upgrade(JsonCondOwnerSave saved)
    {
        if (saved?.strCODef == null) return saved!;
        if (Rules.TryGetValue(saved.strCODef, out var rule)) saved = Economy(saved, rule);
        return Prices.TryGetValue(saved.strCODef, out bool byMass) ? CurrentPrice(saved, byMass) : saved;
    }
    private static JsonCondOwnerSave CurrentPrice(JsonCondOwnerSave saved, bool byMass)
    {
        if (DataHandler.dictCOs == null || !DataHandler.dictCOs.TryGetValue(saved.strCODef, out var definition)) return saved;
        var starting = definition.aStartingConds ?? Array.Empty<string>();
        double full = Amount(starting, "StatBasePrice"), price = full;
        var old = saved.aConds ?? Array.Empty<string>();
        if (byMass && old.Any(s => s.StartsWith("StatMass=", StringComparison.Ordinal)))
            price = ScaledPrice(full, Amount(starting, "StatMass"), Amount(old, "StatMass"));
        bool written = old.Any(s => s.StartsWith("StatBasePrice=", StringComparison.Ordinal));
        // A DEFAULT-compressed save without its own price term already reads the definition's full price.
        if (price <= 0 || !written && old.Contains("DEFAULT") && Math.Abs(price - full) < 1e-9 || written && Math.Abs(Amount(old, "StatBasePrice") - price) < 1e-9) return saved;
        var copy = NativeDefinitions.Clone(saved);
        var values = old.ToList();
        Replace(values, "StatBasePrice", price);
        copy.aConds = values.ToArray(); copy.aCondReveals = null;
        return copy;
    }
    private static JsonCondOwnerSave Economy(JsonCondOwnerSave saved, Rule rule)
    {
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
