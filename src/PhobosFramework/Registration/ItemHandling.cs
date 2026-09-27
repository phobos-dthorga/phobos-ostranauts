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
    private static ConditionalWeakTable<CondOwner, object> LegacyHands = new();
    private static readonly string[] Transport = { "PickupItem", "PickupItemStack", "DropItem", "DropItemStack" };
    internal static void BeginLoad() { Bulky.Clear(); LegacyHands = new(); }
    public static void Cumbersome(NativeDefinitions definitions, string id)
    {
        MaintenanceDefinitions.SetStat(definitions.Objects[id], "IsCumbersome", 1);
        Bulky.Add(id);
    }
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
        if (saved == null || !Bulky.Contains(saved.strCODef) ||
            EquipmentSaveUpgrade.Amount(saved.aConds ?? Array.Empty<string>(), "IsCumbersome") > 0) return saved!;
        var copy = NativeDefinitions.Clone(saved);
        copy.aConds = (copy.aConds ?? Array.Empty<string>()).Where(s => !s.StartsWith("IsCumbersome=", StringComparison.Ordinal))
            .Concat(new[] { "IsCumbersome=1x1" }).ToArray();
        copy.aCondZeroes = copy.aCondZeroes?.Where(s => s != "IsCumbersome").ToArray();
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
