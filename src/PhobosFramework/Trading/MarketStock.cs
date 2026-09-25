using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Trading;

public enum StockCondition { Pristine, Refurbished, Worn, Broken }

/// <summary>Additive, bounded native merchant offers. Existing merchant inventories are not refreshed.</summary>
public static class MarketStock
{
    public static double AvailabilityMultiplier { get; internal set; } = 1;
    internal sealed class Offer
    {
        internal string Item = "";
        internal StockCondition Condition;
    }
    private static readonly Dictionary<string, Offer> Offers = new Dictionary<string, Offer>(StringComparer.Ordinal);
    private static ConditionalWeakTable<CondOwner, Offer> fresh = new ConditionalWeakTable<CondOwner, Offer>();
    internal static void BeginLoad() { Offers.Clear(); fresh = new ConditionalWeakTable<CondOwner, Offer>(); }

    public static void Add(NativeDefinitions d, string merchantLoot, string offerId, string itemId,
        double probability, StockCondition condition) => Add(d, merchantLoot, offerId, itemId, probability, condition, 1);

    // Keep the old public overload and rare world-loot quantities unchanged.
    public const int MaximumOfferQuantity = 256;
    public static void Add(NativeDefinitions d, string merchantLoot, string offerId, string itemId,
        double probability, StockCondition condition, int quantity)
    {
        if (quantity < 1 || quantity > MaximumOfferQuantity)
            throw new ArgumentOutOfRangeException(nameof(quantity), Text.Get("MarketStock.invalid_merchant_offer"));
        if (string.IsNullOrWhiteSpace(offerId) || !offerId.StartsWith("Phobos", StringComparison.Ordinal) ||
            double.IsNaN(probability) || probability <= 0 || probability > 1) throw new ArgumentException(Text.Get("MarketStock.invalid_merchant_offer"));
        AdditiveLoot.SetItemChoice(d, merchantLoot, offerId, new Dictionary<string, double>
            { [itemId] = Math.Min(1, probability * AvailabilityMultiplier) });
        // One offer roll selects a finite lot, not quantity independent probability rolls.
        // Assign through aCOs so the native parser refreshes its cached LootUnits.
        d.Loot[offerId].aCOs = new[] { itemId + "=" + Math.Min(1, probability * AvailabilityMultiplier).ToString("R", CultureInfo.InvariantCulture)
            + "x" + quantity.ToString(CultureInfo.InvariantCulture) };
        Offers[offerId] = new Offer { Item = itemId, Condition = condition };
    }

    public static double WearFraction(StockCondition condition) => condition == StockCondition.Worn ? .15 : 0;
    internal static void Generated(string loot, List<CondOwner> items)
    {
        if (!Offers.TryGetValue(loot, out var offer)) return;
        foreach (var item in items)
        {
            if (item == null || item.bDestroyed || item.strCODef != offer.Item) continue;
            fresh.Remove(item); fresh.Add(item, offer);
            Apply(item, offer);
        }
    }
    internal static void Stocked(List<CondOwner> items)
    {
        if (items == null) return;
        foreach (var item in items)
        {
            if (item == null || !fresh.TryGetValue(item, out var offer)) continue;
            fresh.Remove(item);
            if (!item.bDestroyed) Apply(item, offer);
        }
    }
    private static void Apply(CondOwner item, Offer offer)
    {
        item.SetCondAmount("IsPristine", offer.Condition == StockCondition.Pristine ? 1 : 0);
        // Broken offers use the actual damaged definition, never a cosmetic wear flag.
        item.SetCondAmount("StatDamage", item.GetCondAmount("StatDamageMax") * WearFraction(offer.Condition));
    }
}

[HarmonyPatch(typeof(Loot), nameof(Loot.GetCOLoot))]
internal static class StockGenerationPatch
{
    private static void Postfix(Loot __instance, List<CondOwner> __result) => MarketStock.Generated(__instance.strName, __result);
}

// Native Trader automatically grants IsPristine to every functional item. Reapply
// the chosen condition only to freshly generated registered offers, then forget it.
[HarmonyPatch(typeof(Trader), "AddNewItems", new[] { typeof(List<CondOwner>) })]
internal static class StockConditionPatch
{
    private static void Postfix(List<CondOwner> aCOs) => MarketStock.Stocked(aCOs);
}
