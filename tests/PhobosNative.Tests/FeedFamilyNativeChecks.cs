using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

/// <summary>Feed families against the game's own data: every loose base and cosmetic overlay of the structural part
/// families resolves to its family with a mass the family has a recipe for (or a pinned refusal), the widened feed
/// rule admits the families and nothing else, and every accepted variant loses value when dismantled.</summary>
internal static class FeedFamilyNativeChecks
{
    internal static void Run(NativeDefinitions prepared, Action<bool, string> check)
    {
        var feedTrigger = DataHandler.dictCTs[prepared.Objects[Content.InputBin].strContainerCT];
        var families = FeedFamilies.All.Where(f => f.Key != "wall").ToArray();
        int variants = 0, refused = 0;
        var refusedMasses = new HashSet<double>();
        foreach (var family in families)
        foreach (string baseId in family.Bases)
        {
            check(DataHandler.dictCOs.TryGetValue(baseId, out var definition), "Family base exists in the game: " + baseId);
            double baseKg = Stat(definition!.aStartingConds, "StatMass"), basePrice = Stat(definition.aStartingConds, "StatBasePrice");
            check(family.Accepts(baseKg), $"{family.Key} accepts its base {baseId} at {baseKg} kg");
            check(feedTrigger.TriggeredDataCO(new DataCO(definition), false), "The part feed's game-level rule admits the base: " + baseId);
            check(!ReclaimerRules.IsFamily(baseId) && !DataHandler.dictCTs[prepared.Objects[ReclaimerRules.InputBin].strContainerCT].TriggeredDataCO(new DataCO(definition), false),
                "The R4 feed still refuses structural parts: " + baseId);
            check(EquipmentValueAudit.Price(definition) > Value(family.Catalog(baseKg).Current), "Dismantling loses value against the whole part: " + baseId);
            foreach (var overlay in DataHandler.dictCOOverlays.Values.Where(o => o.strCOBase == baseId))
            {
                variants++;
                check(FeedIdentity.Family(overlay.strName) == family, "Overlay resolves to its family: " + overlay.strName);
                var loot = string.IsNullOrEmpty(overlay.strCondLoot) ? Array.Empty<string>() : DataHandler.dictLoot[overlay.strCondLoot].aCOs;
                // The game clamps a condition at zero, so a delta beyond the base weighs nothing in play.
                double kg = Math.Max(0, baseKg + Delta(loot, "StatMass")), price = Math.Max(0, basePrice + Delta(loot, "StatBasePrice"));
                if (family.Accepts(kg))
                    check(price > Value(family.Catalog(kg).Current), $"Dismantling loses value for {overlay.strName} ({kg} kg, {price} cr)");
                else { refused++; refusedMasses.Add(Math.Round(kg, 3)); }
            }
        }
        check(variants == 127, "The game ships 127 cosmetic variants across the light families (117 floors, 4 DuraWal, 6 Whipple): " + variants);
        check(refused == 42 && refusedMasses.SetEquals(new[] { 0.0, 7.15 }), "Refused floor variants are exactly the 37 that weigh nothing in play and the 5 Van Hummel 7.15 kg floors: " + refused);
        // What the widened rule must not admit at the game level, and what the family rule refuses regardless.
        foreach (string outside in new[] { "ItmDoor01ClosedLoose", "ItmHatch01Loose", "ItmDockSys03ClosedLoose", "ItmFloorGrate4x401Loose", "ItmConduit00Loose", "ItmScrapSteel", "ItmChair01Loose", "ItmCanisterLH02Loose" })
        {
            if (!DataHandler.dictCOs.TryGetValue(outside, out var definition)) continue;
            check(FeedIdentity.Family(outside) == null, "No family for " + outside);
            var data = new DataCO(definition);
            bool structural = data.HasCond("IsWall") || data.HasCond("IsFloorGrate");
            check(structural == feedTrigger.TriggeredDataCO(data, false) || !feedTrigger.TriggeredDataCO(data, false), "Game-level rule admits only walls and floor grates: " + outside);
        }
        check(!feedTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs["ItmDoor01ClosedLoose"]), false) && !feedTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs["ItmScrapSteel"]), false),
            "Doors and scrap never enter the feed at the game level");
        // Product identities and masses agree with the game's definitions; batches fit the tray.
        foreach (var product in FeedFamilies.AllProducts())
        {
            var definition = prepared.Objects.TryGetValue(product.Id, out var own) ? own : DataHandler.dictCOs[product.Id];
            check(ProcessRules.MassMatches(Stat(definition.aStartingConds, "StatMass"), product.Kg), "Product mass agrees with its definition: " + product.Id);
        }
        foreach (var family in families)
        foreach (double kg in new[] { family.MinKg, family.MaxKg })
        {
            var sizes = ProcessingService.OutputSizes(family.Catalog(kg).Current);
            check(sizes != null && Phobos.Ostranauts.Framework.Inventory.BatchPlacement.Plan(new bool[ProcessRules.OutputSize, ProcessRules.OutputSize], sizes) != null,
                $"{family.Key} {kg} kg products fit an empty 8 x 8 tray");
        }
        foreach (var reject in FeedFamilies.RejectKg)
        {
            var co = prepared.Objects[reject.Key];
            check(EquipmentSaveUpgrade.Amount(co.aStartingConds, "IsCategoryTrash") > 0 && co.strNameFriendly.StartsWith("Phobos' ", StringComparison.Ordinal), "Reject is branded waste: " + reject.Key);
            check(prepared.Items.ContainsKey(reject.Key) && co.inventoryWidth == 1 && co.inventoryHeight == 1, "Reject is a one-cell item: " + reject.Key);
        }
        check(prepared.Objects[FeedFamilies.FloorReject].nStackLimit == FeedFamilies.FloorRejectStack && FeedFamilies.RejectKg.Keys.Where(k => k != FeedFamilies.FloorReject).All(k => prepared.Objects[k].nStackLimit <= 1),
            "Floor reject units stack; the fixed-mass rejects stay individual");
    }

    private static double Value(Phobos.Ostranauts.Framework.Processing.ProcessRecipe recipe) => recipe.Products.Sum(p =>
        p.Count * EquipmentValueAudit.Price(DataHandler.dictCOs.TryGetValue(p.Id, out var native) ? native : PreparedOrThrow(p.Id)));
    private static JsonCondOwner PreparedOrThrow(string id) => DataHandler.dictCOs.TryGetValue(id, out var co) ? co : throw new InvalidOperationException("Unknown product " + id);
    private static double Stat(IEnumerable<string> conditions, string name) =>
        conditions.Where(c => c.StartsWith(name + "=", StringComparison.Ordinal)).Select(Value).FirstOrDefault();
    private static double Delta(IEnumerable<string> loot, string name) => loot
        .Where(c => c.TrimStart('-').StartsWith(name + "=", StringComparison.Ordinal))
        .Sum(c => (c.StartsWith("-", StringComparison.Ordinal) ? -1 : 1) * Value(c.TrimStart('-')));
    private static double Value(string entry) => double.Parse(entry.Split('=')[1].Split('x')[1], CultureInfo.InvariantCulture);
}
