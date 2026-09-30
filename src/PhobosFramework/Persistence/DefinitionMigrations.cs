using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Persistence;

/// <summary>Load-time conversion of saved objects from a retired definition to its new equivalent (Framework 0.56.0;
/// the owner's migration rule of 30 September 2026: convert old assets automatically rather than keep legacy forms).
/// A content mod registers old id to new id, the property-map records to carry across under new names, and an
/// optional rewrite of a carried record. The conversion edits the saved data the game is about to spawn from (the
/// ship's item list and each object's saved conditions), never a save file; the next save writes the new form.
/// It is idempotent (a converted item no longer names the old id) and refuses nothing: a record the rewrite cannot
/// read is carried unchanged, and the owning service's own protection applies.</summary>
public static class DefinitionMigrations
{
    public sealed class Rule
    {
        public string OldId { get; }
        public string NewId { get; }
        /// <summary>Saved property-map names to rename (old name to new name).</summary>
        public IReadOnlyDictionary<string, string> RecordRenames { get; }
        /// <summary>Rewrites a carried record's values under its new name; null keeps them.</summary>
        public Func<string, Dictionary<string, string>, Dictionary<string, string>>? Rewrite { get; }
        /// <summary>Rewrites the object's saved conditions for the new definition (Framework 0.58.0; mass, prices and
        /// family marks differ between definitions); null keeps them.</summary>
        public Func<string[], string[]>? Conditions { get; }
        internal Rule(string oldId, string newId, IReadOnlyDictionary<string, string> renames, Func<string, Dictionary<string, string>, Dictionary<string, string>>? rewrite,
            Func<string[], string[]>? conditions)
        { OldId = oldId; NewId = newId; RecordRenames = renames; Rewrite = rewrite; Conditions = conditions; }
    }
    private static readonly Dictionary<string, Rule> rules = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> converted = new(StringComparer.Ordinal);
    internal static Action<string> Log = _ => { };
    public static void Register(string oldId, string newId, IReadOnlyDictionary<string, string>? recordRenames = null,
        Func<string, Dictionary<string, string>, Dictionary<string, string>>? rewrite = null, Func<string[], string[]>? conditions = null)
    {
        if (string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId) || oldId == newId) throw new ArgumentException("A definition migration needs two different ids.");
        // One step only: a target is never itself retired, and a retired id is never another rule's target.
        if (rules.ContainsKey(newId) || rules.Values.Any(r => r.NewId == oldId)) throw new ArgumentException("Definition migrations do not chain: " + oldId + " to " + newId);
        rules[oldId] = new Rule(oldId, newId, recordRenames ?? new Dictionary<string, string>(), rewrite, conditions);
    }
    /// <summary>A conditions rewrite for equipment that moves to another definition: the new definition's starting
    /// conditions, then every saved condition that differs from the old definition's (wear, progress, locks, marks),
    /// except the definitions' own stats and family marks, with the saved mass moved by the difference in dry mass.
    /// A save that stored only DEFAULT (the old definition unchanged) takes the new definition's.</summary>
    public static Func<string[], string[]> Retarget(string oldId, string newId, double oldDryKg, double newDryKg, params string[] definitionOnly)
    {
        var owned = new HashSet<string>(definitionOnly.Concat(new[] { "StatMass", "StatBasePrice", "StatInstallProgressMax", "StatUninstallProgressMax",
            "StatRepairProgressMax", "StatDismantleProgressMax", "StatDamageMax", "PhobosEquipmentEconomyV1" }), StringComparer.Ordinal);
        return saved =>
        {
            var oldDef = DataHandler.GetCondOwnerDef(oldId)?.aStartingConds ?? Array.Empty<string>();
            var newDef = DataHandler.GetCondOwnerDef(newId)?.aStartingConds ?? throw new InvalidOperationException("No definition " + newId);
            var old = saved.Contains("DEFAULT") ? oldDef.Concat(saved.Where(s => s != "DEFAULT")).ToArray() : saved;
            var result = new List<string>(newDef);
            void Set(string name, string entry) { result.RemoveAll(s => Name(s) == name); result.Add(entry); }
            foreach (string entry in old)
            {
                string name = Name(entry);
                // Unchanged from the old definition (same name and amount, whatever the number format): the new
                // definition's value stands.
                if (name.Length == 0 || owned.Contains(name) || Amount(oldDef, name) is double d && Amount(new[] { entry }, name) is double s && Math.Abs(d - s) < 1e-9) continue;
                Set(name, entry);
            }
            double mass = Amount(old, "StatMass") is double m ? m - oldDryKg + newDryKg : newDryKg;
            Set("StatMass", "StatMass=1.0x" + Math.Max(newDryKg, mass).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            return result.ToArray();
        };
    }
    private static string Name(string entry) { int eq = entry.IndexOf('='); return eq <= 0 ? "" : entry.Substring(0, eq); }
    private static double? Amount(IEnumerable<string> entries, string name)
    {
        foreach (string entry in entries)
        {
            if (Name(entry) != name) continue;
            string value = entry.Substring(entry.IndexOf('x') + 1);
            if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v)) return v;
        }
        return null;
    }
    public static IReadOnlyCollection<Rule> Rules => rules.Values;
    /// <summary>How many saved objects were converted per old id since the last load began (for the load notice).</summary>
    public static IReadOnlyDictionary<string, int> Converted => converted;
    /// <summary>A new data load: content registers its rules again.</summary>
    internal static void Reset() { rules.Clear(); converted.Clear(); }

    /// <summary>Converts one saved item in place; true when it named a retired definition.</summary>
    public static bool Convert(JsonItem item)
    {
        if (item?.strName == null || !rules.TryGetValue(item.strName, out var rule)) return false;
        item.strName = rule.NewId;
        if (item.aGPMSettings != null)
            foreach (var map in item.aGPMSettings)
            {
                if (map?.strName == null || !rule.RecordRenames.TryGetValue(map.strName, out string newName)) continue;
                string oldName = map.strName;
                map.strName = newName;
                if (rule.Rewrite == null || map.dictGUIPropMap == null) continue;
                try { map.dictGUIPropMap = DataHandler.ConvertDictToStringArray(rule.Rewrite(oldName, DataHandler.ConvertStringArrayToDict(map.dictGUIPropMap))); }
                catch (Exception e) { Log(Text.Get("DefinitionMigrations.rewrite_failed", item.strID ?? "", newName, e.Message)); }
            }
        converted[rule.OldId] = converted.TryGetValue(rule.OldId, out int n) ? n + 1 : 1;
        return true;
    }
    /// <summary>A copy of a saved object that names the new definition, or the same object when nothing applies.</summary>
    public static JsonCondOwnerSave Convert(JsonCondOwnerSave saved)
    {
        if (saved?.strCODef == null || !rules.TryGetValue(saved.strCODef, out var rule)) return saved!;
        var copy = Registration.NativeDefinitions.Clone(saved);
        copy.strCODef = rule.NewId;
        if (rule.Conditions != null && copy.aConds != null)
        {
            try { copy.aConds = rule.Conditions(copy.aConds); copy.aCondReveals = null; }
            catch (Exception e) { Log(Text.Get("DefinitionMigrations.conditions_failed", saved.strID ?? "", rule.NewId, e.Message)); }
        }
        return copy;
    }
    /// <summary>The ids an item list would convert (for tests and diagnostics); does not change the list.</summary>
    public static IEnumerable<string> Pending(IEnumerable<JsonItem>? items) =>
        items == null ? Enumerable.Empty<string>() : items.Where(i => i?.strName != null && rules.ContainsKey(i.strName)).Select(i => i.strName);
}

// Saved items are spawned by definition name; convert the list before the game reads it (templates are never saved
// games and are left alone).
[HarmonyPatch(typeof(Ship), "SpawnItems")]
[HarmonyPriority(Priority.First)]
internal static class DefinitionMigrationItems
{
    private static void Prefix(Ship __instance, bool bTemplateOnly)
    {
        var items = __instance?.json?.aItems;
        if (bTemplateOnly || items == null || !DefinitionMigrations.Rules.Any()) return;
        int count = 0;
        foreach (var item in items) if (DefinitionMigrations.Convert(item)) count++;
        if (count > 0) FrameworkLifecycle.Log(Text.Get("DefinitionMigrations.converted", __instance!.strRegID, count));
    }
}

// Each object's saved conditions name its definition too; convert that copy before any other upgrade reads it.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.SetData))]
[HarmonyPriority(Priority.First)]
internal static class DefinitionMigrationObjects
{
    private static void Prefix(ref JsonCondOwnerSave jCOSIn)
    {
        if (jCOSIn != null && DefinitionMigrations.Rules.Count > 0) jCOSIn = DefinitionMigrations.Convert(jCOSIn);
    }
}
