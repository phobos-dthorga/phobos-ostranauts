using System;
using System.Collections.Generic;
using System.Linq;
using PhobosShipbreaker.Core;

internal static class StorageChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string d4 = RoutingRules.Processor, r4 = ReclaimerRules.Installed, f6 = FurnaceRules.Prefix + "Installed";
        foreach (var id in new[] { d4, d4 + "Dmg", "PhobosShipbreakerLoose", "PhobosShipbreakerLooseDmg" })
            check(StorageRules.Port(id) == StorageRules.ProductsOut, $"D4 family {id} keeps one products output");
        foreach (var id in new[] { r4, r4 + "Dmg", ReclaimerRules.Prefix + "Loose", ReclaimerRules.Prefix + "LooseDmg" })
            check(StorageRules.Port(id) == StorageRules.SteelOut, $"R4 family {id} keeps one steel output");
        foreach (var id in new[] { f6, CollectorRules.Installed, IntakeRules.Grabber, "ItmLocker01", null })
            check(StorageRules.Port(id) == null, $"{id ?? "null"} has no storage output");

        // New addresses never reuse or change a published port meaning.
        var published = new[] { RoutingRules.SendPort, RoutingRules.CollectorIn, RoutingRules.ReclaimerIn, RoutingRules.MetalsOut,
            RoutingRules.FurnaceIn, RoutingRules.FurnaceOut, "PhobosFurnace.Cooling" };
        check(!published.Contains(StorageRules.ProductsOut) && !published.Contains(StorageRules.SteelOut) && StorageRules.ProductsOut != StorageRules.SteelOut,
            "Storage outputs are new, distinct addresses");
        check(RoutingRules.OutputPort(d4) == RoutingRules.SendPort && RoutingRules.OutputPort(r4) == RoutingRules.SendPort &&
            RoutingRules.OutputPort(r4, true) == RoutingRules.MetalsOut, "Existing residue and aluminium outputs keep their meaning");

        var d4Products = StorageRules.Carried(d4).Select(p => (p.Id, p.Kg)).OrderBy(p => p.Id).ToArray();
        check(d4Products.SequenceEqual(new[] { ("ItmPartsMechSmall01", .5), ("ItmScrapAluminum", 1.0), ("ItmScrapCarbonFiber", 1.0), ("ItmScrapPlastic", .3), ("ItmScrapSteel", 1.0),
                (FeedFamilies.AeroReject, FeedFamilies.AeroRejectKg), (FeedFamilies.DuraWalReject, FeedFamilies.DuraWalRejectKg), (FeedFamilies.FloorReject, FeedFamilies.FloorRejectKg),
                (FeedFamilies.WhippleReject, FeedFamilies.WhippleRejectKg), (FeedFamilies.WindowReject, FeedFamilies.WindowRejectKg) }),
            "D4 storage carries every family's ordinary products and the light families' terminal rejects, including aluminium that has no furnace route");
        check(StorageRules.Carried(r4).Select(p => (p.Id, p.Kg)).SequenceEqual(new[] { ("ItmScrapSteel", 1.0) }),
            "R4 storage carries steel only; aluminium keeps its furnace route and rejects their residue route");

        // Every product of every saved revision has exactly one output address: nothing unrouted by design, nothing twice.
        var catalogs = FeedFamilies.All.SelectMany(f => f.AcceptedMasses().Select(kg => (d4, f.Catalog(kg)))).Concat(new[] { (r4, ReclaimerRules.Recipes) });
        foreach (var (machine, catalog) in catalogs)
        foreach (var recipe in catalog.Recipes)
        foreach (var product in recipe.Products)
        {
            bool storage = StorageRules.Carried(machine).Any(p => p.Id == product.Id && p.Kg == product.Kg);
            bool residue = product.Id == ProcessRules.Residue || product.Id == ReclaimerRules.Feedstock || product.Id == ReclaimerRules.Reject;
            bool metals = machine == r4 && product.Id == FurnaceMaterialRules.Aluminium;
            check(new[] { storage, residue, metals }.Count(x => x) == 1, $"{machine} revision {recipe.Revision} {product.Id} has one output address");
        }

        for (int mask = 0; mask < 8; mask++)
            check(StorageRules.Carries(d4, "ItmScrapSteel", 1, (mask & 1) != 0, (mask & 2) != 0, (mask & 4) != 0) == (mask == 7),
                "Installed, nested and stacked products stay in the tray");
        check(!StorageRules.Carries(d4, "ItmScrapSteel", 2, true, true, true) && !StorageRules.Carries(d4, "ItmPartsMechSmall01", 1, true, true, true),
            "Exact product mass is required");
        check(!StorageRules.Carries(d4, ReclaimerRules.Feedstock, ReclaimerRules.InputKg, true, true, true) &&
            !StorageRules.Carries(d4, ProcessRules.Residue, ProcessRules.LegacyResidueKg, true, true, true), "Residue never leaves through storage");
        check(!StorageRules.Carries(r4, FurnaceMaterialRules.Aluminium, 1, true, true, true) && !StorageRules.Carries(r4, ReclaimerRules.Reject, ReclaimerRules.RejectKg, true, true, true),
            "R4 aluminium and rejects never leave through storage");
        check(!StorageRules.Carries(f6, FurnaceRules.Blank, FurnaceRules.BlankKg, true, true, true), "Furnace products are not a storage output");

        var selection = new StorageSelection { Port = StorageRules.ProductsOut, StoreId = "locker-1", ShipId = "OKLG" };
        var fields = selection.Save();
        check(StorageSelection.TryLoad(fields, StorageRules.ProductsOut, out var loaded) && loaded.StoreId == "locker-1" && loaded.ShipId == "OKLG",
            "Storage selection round-trips");
        check(!StorageSelection.TryLoad(fields, StorageRules.SteelOut, out _) && !StorageSelection.TryLoad(fields, null, out _),
            "A selection for another address is never reinterpreted");
        foreach (var key in new[] { "port", "store", "ship" })
        {
            var missing = new Dictionary<string, string>(fields); missing.Remove(key);
            var unsafeValue = new Dictionary<string, string>(fields) { [key] = "a=b" };
            check(!StorageSelection.TryLoad(missing, StorageRules.ProductsOut, out _) && !StorageSelection.TryLoad(unsafeValue, StorageRules.ProductsOut, out _),
                $"Damaged selection field {key} is protected");
        }

        check(RoutingRules.DemandKW(true, true, 12, 2) == RoutingRules.DemandKW(true, true, false, 12, ReclaimerRules.IdleKW, 2) &&
            RoutingRules.DemandKW(false, true, 12, 2) == ReclaimerRules.IdleKW + 2, "Existing reclaimer feed demand is unchanged");
        check(RoutingRules.DemandKW(false, false, true, 30, .12, 2) == .12 + 2 && RoutingRules.DemandKW(true, true, true, 12, .1, 2) == 12 + 2 + 2,
            "Unloading adds one feeder load to idle or working demand");

        var store = RoutingCommand.Parse("phobosroute store D4-ID Locker-Mixed");
        check(store.Action == "store" && store.ObjectId == "D4-ID" && store.Argument == "Locker-Mixed", "Store keeps the full, case-sensitive store ID");
        foreach (var action in new[] { "stores", "unstore", "unload", "pause-unload" })
            check(RoutingCommand.Parse($"phobosroute {action} R4-ID").Action == action && RoutingCommand.Parse($"phobosroute {action}").Action == "invalid",
                $"{action} needs exactly one sender ID");
        check(RoutingCommand.Parse("phobosroute store D4-ID").Action == "invalid", "Store needs a store ID");
    }
}
