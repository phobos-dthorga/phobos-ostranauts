using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

/// <summary>Ordinary-product outputs into one explicitly chosen native store. These addresses are
/// new: existing ResidueOut, MetalsOut and furnace meanings are unchanged.</summary>
public static class StorageRules
{
    public const string ProductsOut = "PhobosShipbreaker.ProductsOut", SteelOut = "PhobosShipbreaker.SteelOut";
    public const string Unloading = "PhobosShipbreakerUnloading";
    // Products already carried by another published address never use the storage output.
    private static readonly HashSet<string> RoutedElsewhere = new(StringComparer.Ordinal)
    { ProcessRules.Residue, ReclaimerRules.Feedstock, ReclaimerRules.Reject };
    public static string? Port(string? machine) =>
        ReclaimerRules.IsFamily(machine) ? SteelOut : RoutingRules.IsProcessorFamily(machine) ? ProductsOut : null;
    /// <summary>Derived from every saved recipe revision of every feed family, so products of historic jobs remain
    /// eligible; the light families' terminal rejects leave this way too.</summary>
    public static IReadOnlyList<ProductSpec> Carried(string? machine)
    {
        bool reclaimer = ReclaimerRules.IsFamily(machine);
        IEnumerable<ProductSpec> products = reclaimer ? ReclaimerRules.Recipes.Recipes.SelectMany(r => r.Products) :
            RoutingRules.IsProcessorFamily(machine) ? FeedFamilies.AllProducts() : Array.Empty<ProductSpec>();
        return products
            // R4 aluminium keeps its separate MetalsOut route to the furnace; no implicit second consumer.
            .Where(p => !RoutedElsewhere.Contains(p.Id) && !(reclaimer && p.Id == FurnaceMaterialRules.Aluminium))
            .GroupBy(p => (p.Id, p.Kg)).Select(g => g.First()).ToArray();
    }
    public static bool Carries(string? machine, string? item, double kg, bool detached, bool empty, bool unstacked) =>
        detached && empty && unstacked && item != null && Carried(machine).Any(p => p.Id == item && ProcessRules.MassMatches(kg, p.Kg));
}

/// <summary>The sender-owned storage selection. The chosen native store is passive and keeps no link data.</summary>
public sealed class StorageSelection
{
    public const string StoreName = "ShipbreakerStorageRoute";
    public const int Schema = 1;
    public string Port = "", StoreId = "", ShipId = "";
    public Dictionary<string, string> Save() => new(StringComparer.Ordinal) { ["port"] = Port, ["store"] = StoreId, ["ship"] = ShipId };
    public static bool TryLoad(IReadOnlyDictionary<string, string> fields, string? expectedPort, out StorageSelection selection)
    {
        selection = new StorageSelection();
        foreach (string key in new[] { "port", "store", "ship" })
            if (!fields.TryGetValue(key, out var value) || !ObjectStateStore.SafeValue(value)) { selection = new StorageSelection(); return false; }
        selection.Port = fields["port"]; selection.StoreId = fields["store"]; selection.ShipId = fields["ship"];
        // A record for another address is never reinterpreted as this machine's storage output.
        return expectedPort != null && selection.Port == expectedPort;
    }
}
