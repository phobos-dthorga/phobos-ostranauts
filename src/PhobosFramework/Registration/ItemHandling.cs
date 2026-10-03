using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Opt-in native item transport actions; content owns which objects are bulky.</summary>
public static class ItemHandling
{
    private static readonly HashSet<string> Bulky = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Fixtures = new(StringComparer.Ordinal);
    private static ConditionalWeakTable<CondOwner, object> LegacyHands = new();
    private static readonly string[] Transport = { "PickupItem", "PickupItemStack", "DropItem", "DropItemStack" };
    internal static void BeginLoad() { Bulky.Clear(); Fixtures.Clear(); LegacyHands = new(); }
    public static void Cumbersome(NativeDefinitions definitions, string id)
    {
        MaintenanceDefinitions.SetStat(definitions.Objects[id], "IsCumbersome", 1);
        Bulky.Add(id);
    }
    /// <summary>An installed fixture kept out of inventory windows, as the game's own installed power conduit is: hidden
    /// from the ground inventory and never pocketable (Framework 0.73.0). Before, installed pipe segments showed in the
    /// ground inventory and could be dragged out of a line there, past the Uninstall job and the drain-first guard.
    /// Saved objects are corrected as they load, like bulky ones; nothing else about them changes.</summary>
    public static void Fixture(NativeDefinitions definitions, string id)
    {
        var co = definitions.Objects[id];
        co.aStartingConds = WithoutFlags(co.aStartingConds, "IsPocketable", "IsHiddenInv").Concat(new[] { "IsHiddenInv=1x1" }).ToArray();
        Fixtures.Add(id);
    }
    public static bool IsFixture(string? id) => id != null && Fixtures.Contains(id);
    private static IEnumerable<string> WithoutFlags(IEnumerable<string>? conds, params string[] names) =>
        (conds ?? Array.Empty<string>()).Where(s => !names.Any(n => s.StartsWith(n + "=", StringComparison.Ordinal)));
    public static void Apply(NativeDefinitions definitions)
    {
        foreach (var co in definitions.Objects.Values)
        {
            if (!co.strName.StartsWith("Phobos", StringComparison.Ordinal) || co.strType != "Item") continue;
            var flags = new HashSet<string>((co.aStartingConds ?? Array.Empty<string>()).Select(s => s.Split('=')[0]));
            if (flags.Contains("IsSystem")) continue; // Internal compartments are not movable cargo.
            bool installed = flags.Contains("IsInstalled"), bulky = flags.Contains("IsCumbersome");
            var actions = (co.aInteractions ?? Array.Empty<string>()).Where(a =>
                !Transport.Contains(a) || !installed && (co.nStackLimit > 1 || a != "PickupItemStack" && a != "DropItemStack")).ToList();
            if (!installed)
            {
                actions.AddRange(new[] { "DropItem", "PickupItem" });
                if (co.nStackLimit > 1) actions.AddRange(new[] { "DropItemStack", "PickupItemStack" });
                if (bulky) co.mapSlotEffects = new[] { "drag", "Blank" };
                else if (co.mapSlotEffects == null || co.mapSlotEffects.Length == 0 ||
                    co.mapSlotEffects.SequenceEqual(new[] { "drag", "Blank" }))
                    co.mapSlotEffects = new[] { "heldL", "HeldItmDefaultL", "heldR", "HeldItmDefaultR" };
            }
            else co.mapSlotEffects = Array.Empty<string>();
            co.aInteractions = actions.Distinct(StringComparer.Ordinal).ToArray();
        }
    }
    // Correct only declared handling flags in a detached DTO. No identity, location,
    // contents, wear, recipe/progress, value or mass is rewritten.
    internal static JsonCondOwnerSave Upgrade(JsonCondOwnerSave saved)
    {
        if (saved == null) return saved!;
        var conds = saved.aConds ?? Array.Empty<string>();
        bool bulky = Bulky.Contains(saved.strCODef) && EquipmentSaveUpgrade.Amount(conds, "IsCumbersome") <= 0;
        // A compressed save (DEFAULT) takes the current definition's flags as it loads; only a listed old flag needs help.
        bool fixture = Fixtures.Contains(saved.strCODef) && (EquipmentSaveUpgrade.Amount(conds, "IsPocketable") > 0 ||
            EquipmentSaveUpgrade.Amount(conds, "IsHiddenInv") <= 0 && !conds.Contains("DEFAULT"));
        if (!bulky && !fixture) return saved;
        var copy = NativeDefinitions.Clone(saved);
        IEnumerable<string> next = copy.aConds ?? Array.Empty<string>();
        if (bulky) next = WithoutFlags(next, "IsCumbersome").Concat(new[] { "IsCumbersome=1x1" });
        if (fixture) next = WithoutFlags(next, "IsPocketable", "IsHiddenInv").Concat(new[] { "IsHiddenInv=1x1" });
        copy.aConds = next.ToArray();
        copy.aCondZeroes = copy.aCondZeroes?.Where(s => !(bulky && s == "IsCumbersome") && !(fixture && s == "IsHiddenInv")).ToArray();
        copy.aCondReveals = null;
        return copy;
    }
    internal static bool LegacyHand(JsonCondOwnerSave? saved) => saved != null && Bulky.Contains(saved.strCODef) &&
        (saved.strSlotName == "heldL" || saved.strSlotName == "heldR");
    internal static void RestoreLegacyHand(CondOwner item, JsonCondOwnerSave? saved)
    {
        if (!LegacyHand(saved)) return;
        // Native ship loading must be able to restore the exact saved hand slot.
        // Keep only that slot until a successful native unslot, then retire it.
        string slot = saved!.strSlotName;
        var effect = DataHandler.GetSlotEffect(slot == "heldL" ? "HeldItmDefaultL" : "HeldItmDefaultR");
        if (effect == null) return;
        effect.strSlotPrimary = slot; item.mapSlotEffects[slot] = effect;
        LegacyHands.GetValue(item, _ => new object());
    }
    internal static void RetireLegacyHand(CondOwner? item)
    {
        if (item == null || item.slotNow != null || !LegacyHands.Remove(item)) return;
        item.mapSlotEffects.Remove("heldL"); item.mapSlotEffects.Remove("heldR");
    }
}

[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.SetData))]
internal static class ItemHandlingLoadPatch
{
    private static void Prefix(ref JsonCondOwnerSave jCOSIn) { if (jCOSIn != null) jCOSIn = ItemHandling.Upgrade(jCOSIn); }
    private static void Postfix(CondOwner __instance, JsonCondOwnerSave jCOSIn) => ItemHandling.RestoreLegacyHand(__instance, jCOSIn);
}
[HarmonyPatch(typeof(Slots), nameof(Slots.UnSlotItem), new[] { typeof(string), typeof(CondOwner), typeof(bool) })]
internal static class ItemHandlingUnslotPatch
{
    private static void Postfix(CondOwner __result) => ItemHandling.RetireLegacyHand(__result);
}
