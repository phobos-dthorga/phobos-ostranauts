using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

/// <summary>Faction kiosks (owner direction, 30 September 2026): every sold Phobos item is offered at the game's
/// CCRE and GalCon scrip kiosks, gated below Honored, and the in-place trigger amendment files each item under
/// exactly its tier while vanilla stock keeps the game's own tiers.</summary>
internal static class FactionKioskChecks
{
    private static readonly string[] Kiosks = { "ItmFactionKioskBCRSInv", "ItmFactionKioskBCERInv", "ItmFactionKioskMTRSInv" };

    // Mirrors the native any-of evaluation (bAND false): a forbidden condition fails, then any one requirement passes;
    // an empty requirement list passes. Inspected from Blue Bottle Games' Ostranauts 1.0.1.5 CondTrigger.Triggered.
    private static bool Passes(CondTrigger t, ISet<string> conds) =>
        !t.aForbids.Any(conds.Contains) && (t.aReqs.Length == 0 || t.aReqs.Any(conds.Contains));
    // The kiosk lists an item under the first tier trigger that matches.
    private static string TierOf(ISet<string> conds)
    {
        foreach (string name in FactionKiosks.NativeTriggers) if (Passes(DataHandler.dictCTs[name], conds)) return name;
        return "none";
    }
    private static ISet<string> Conds(string item, params string[] extra)
    {
        var set = new HashSet<string>(extra, StringComparer.Ordinal);
        if (DataHandler.dictCOs.TryGetValue(item, out var co) && co.aStartingConds != null)
            foreach (string c in co.aStartingConds) { int eq = c.IndexOf('='); set.Add(eq < 0 ? c : c.Substring(0, eq)); }
        return set;
    }

    internal static void Run(Action<bool, string> check)
    {
        var before = FactionKiosks.NativeTriggers.ToDictionary(n => n, n => (DataHandler.dictCTs[n].aReqs.ToArray(), DataHandler.dictCTs[n].aForbids.ToArray()));
        var vanilla = new Dictionary<string, string>
        {
            ["ItmToolWelder01New"] = "TIsFactionTierNeutral", ["ItmShipCladding01Loose"] = "TIsFactionTierNeutral",
            ["ItmChargerBattery04OffLoose"] = "TIsFactionTierWarm", ["ItmShipWeaponMassThrower02Loose"] = "TIsFactionTierFriendly",
            ["ItmShipWeaponPDC01Loose"] = "TIsFactionTierTrusted", ["ItmShipWeaponMissileLauncher01Loose"] = "TIsFactionTierHonored"
        };
        var vanillaBefore = vanilla.Keys.ToDictionary(k => k, k => TierOf(Conds(k)));
        foreach (var pair in vanilla) check(vanillaBefore[pair.Key] == pair.Value, "Vanilla kiosk tier anchor as inspected: " + pair.Key);

        FactionKiosks.Definitions();
        FactionKiosks.Definitions();
        foreach (FactionTier tier in Enum.GetValues(typeof(FactionTier)))
        {
            var t = DataHandler.dictCTs[FactionKiosks.NativeTriggers[(int)tier]];
            var (reqs, forbids) = before[t.strName];
            check(t.aReqs.Take(reqs.Length).SequenceEqual(reqs) && t.aForbids.Take(forbids.Length).SequenceEqual(forbids),
                "The game's own tier conditions stay first and unchanged: " + t.strName);
            check(t.aReqs.Length - reqs.Length == (tier == FactionTier.Neutral ? 0 : 1) && t.aForbids.Length - forbids.Length == 4 - (int)tier,
                "Amending twice adds each mark once: " + t.strName);
            if (FactionKiosks.Mark(tier) is { } mark) check(DataHandler.dictConds.ContainsKey(mark), "Tier mark is a registered condition: " + mark);
        }
        foreach (var pair in vanilla) check(TierOf(Conds(pair.Key)) == vanillaBefore[pair.Key], "Vanilla stock keeps its tier after the amendment: " + pair.Key);
        // A control-systems board marked Friendly leaves the Warm row it would otherwise join.
        check(TierOf(Conds("ItmNavModMobo")) == "TIsFactionTierWarm", "Unmarked nav boards stay at Warm");
        foreach (FactionTier tier in Enum.GetValues(typeof(FactionTier)))
        {
            string[] marks = FactionKiosks.Mark(tier) is { } m ? new[] { m } : Array.Empty<string>();
            check(TierOf(Conds("ItmNavModMobo", marks)) == (tier == FactionTier.Neutral ? "TIsFactionTierWarm" : FactionKiosks.NativeTriggers[(int)tier]),
                "A mark files a control-systems board exactly at its tier: " + tier);
            check(TierOf(Conds("ItmToolWelder01New", marks)) == FactionKiosks.NativeTriggers[(int)tier], "A mark files ordinary stock exactly at its tier: " + tier);
        }

        var offers = (IDictionary)typeof(MarketStock).GetField("Offers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        string? MarkOf(string branch) => (string?)offers[branch]!.GetType().GetField("Mark", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(offers[branch]);
        var packs = new (string Name, NativeDefinitions Definitions, EconomyPack Pack)[]
        {
            ("Agriculture", PhobosAgriculture.Definitions.Prepare(), PhobosAgriculture.AgricultureEconomy.Pack),
            ("Shipbreaker", PhobosShipbreaker.Content.Prepare(), PhobosShipbreaker.Core.ShipbreakerEconomy.Pack),
            ("AutoNav", PhobosAutoNav.EquipmentContent.Prepare(), PhobosAutoNav.AutoNavEconomy.Pack),
            ("Manufacturing", PhobosManufacturing.Content.Prepare(true), PhobosManufacturing.Core.Economy.Pack),
            ("Framework", Phobos.Ostranauts.Framework.Items.FrameworkItems.Prepare(), Phobos.Ostranauts.Framework.Items.ItemEconomy.Pack)
        };
        foreach (var (name, d, pack) in packs)
        {
            check(pack.factionKiosks != null && pack.factionKiosks.tiers.Values.All(v => v != nameof(FactionTier.Honored)), name + ": every kiosk item asks less than Honored");
            var tiers = pack.factionKiosks!.tiers;
            var sold = d.Loot.Values.Where(l => offers.Contains(l.strName) && !l.strName.StartsWith("PhobosFaction_", StringComparison.Ordinal))
                .SelectMany(l => l.aCOs.Select(c => c.Substring(0, c.IndexOf('=')))).Where(id => !id.EndsWith("Dmg", StringComparison.Ordinal)).ToHashSet();
            foreach (string kiosk in Kiosks)
            {
                check(d.LootBranches.TryGetValue(kiosk, out var branches) && !d.Loot.ContainsKey(kiosk), name + ": kiosk table linked in place: " + kiosk);
                var here = branches!.Where(b => b.StartsWith("PhobosFaction_", StringComparison.Ordinal)).ToDictionary(b => d.Loot[b].aCOs.Single().Split('=')[0], b => b);
                check(sold.IsSubsetOf(here.Keys), name + ": every item sold elsewhere is also at " + kiosk + ": " + string.Join(", ", sold.Except(here.Keys)));
                foreach (var pair in here)
                {
                    check(d.Loot[pair.Value].aCOs.Single().Contains("=1x"), name + ": kiosk stock is certain, like the game's own: " + pair.Key);
                    string? mark = MarkOf(pair.Value);
                    check(mark == null || mark.StartsWith(FactionKiosks.MarkPrefix, StringComparison.Ordinal), name + ": offer stamps a tier mark: " + pair.Key);
                }
            }
        }
        // Spot anchors from the tier rule.
        string? Offered(NativeDefinitions d, string item) => d.LootBranches[Kiosks[0]].Where(b => b.StartsWith("PhobosFaction_", StringComparison.Ordinal))
            .Where(b => d.Loot[b].aCOs.Single().StartsWith(item + "=", StringComparison.Ordinal)).Select(MarkOf).FirstOrDefault();
        check(Offered(packs[1].Definitions, "PhobosFurnaceLoose") == "IsPhobosFactionTierTrusted", "F6 asks Trusted");
        check(!packs[1].Definitions.LootBranches[Kiosks[0]].Any(b => b.StartsWith("PhobosFaction_", StringComparison.Ordinal) &&
            packs[1].Definitions.Loot[b].aCOs.Single().StartsWith("PhobosFurnaceSection=", StringComparison.Ordinal)), "Retired F6 sections are not at the kiosks");
        check(Offered(packs[1].Definitions, "PhobosSteelIngot") == null, "Ingots are Neutral");
        check(Offered(packs[2].Definitions, "PhobosNavModFireControl") == "IsPhobosFactionTierFriendly", "N3 asks Friendly");
        check(Offered(packs[3].Definitions, "PhobosVolatilesRefinery" + "Loose") == "IsPhobosFactionTierTrusted", "V4 asks Trusted");
    }
}
