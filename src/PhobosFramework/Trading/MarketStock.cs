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
        double probability, StockCondition condition)
    {
        if (string.IsNullOrWhiteSpace(offerId) || !offerId.StartsWith("Phobos", StringComparison.Ordinal) ||
            double.IsNaN(probability) || probability <= 0 || probability > 1) throw new ArgumentException(Text.Get("MarketStock.invalid_merchant_offer"));
        if (!d.Loot.TryGetValue(merchantLoot, out var merchant))
        {
            if (!DataHandler.dictLoot.TryGetValue(merchantLoot, out var original))
                throw new ArgumentException(Text.Get("MarketStock.missing_native_merchant_loot", merchantLoot));
            merchant = NativeDefinitions.Clone(original);
            d.Loot.Add(merchantLoot, merchant);
        }
        // Preserve vanilla/foreign entries and replace only our exact branch.
        merchant.aLoots = (merchant.aLoots ?? Array.Empty<string>())
            .Where(s => !s.StartsWith(offerId + "=", StringComparison.Ordinal)).Concat(new[] { offerId + "=1x1" }).ToArray();
        d.Loot[offerId] = new Loot { strName = offerId, strType = "item",
            aCOs = new[] { itemId + "=" + Math.Min(1, probability * AvailabilityMultiplier).ToString(CultureInfo.InvariantCulture) + "x1" }, aLoots = Array.Empty<string>() };
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
