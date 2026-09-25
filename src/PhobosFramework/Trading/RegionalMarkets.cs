using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Trading;

/// <summary>Verified vanilla retail endpoints, not invented planetary markets or price overrides.</summary>
public static class RegionalMarkets
{
    private static readonly IReadOnlyDictionary<string, string> SupplyTables =
        new Dictionary<string, string>(StringComparer.Ordinal) {
            ["BCER"] = "ItmSupplyKioskBCERInv", ["BCRS"] = "ItmSupplyKioskBCRSInv",
            ["EJDR"] = "ItmSupplyKioskEJDRInv", ["HQCH"] = "ItmSupplyKioskHQCHInv",
            ["JATL"] = "ItmSupplyKioskJATLInv", ["JFTS"] = "ItmSupplyKioskJFTSInv",
            ["JPTN"] = "ItmSupplyKioskJPTNInv", ["MHNG"] = "ItmSupplyKioskMHNGInv",
            ["MLAB"] = "ItmSupplyKioskMLABInv", ["MSUZ"] = "ItmSupplyKioskMSUZInv",
            ["MTRS"] = "ItmSupplyKioskMTRSInv", ["MVOL"] = "ItmSupplyKioskMVOLInv",
            ["OFLT"] = "ItmFlotillaScrapKioskInv", ["OKLG"] = "ItmOKLGSupplyKioskInv",
            ["SVIR"] = "ItmSupplyKioskSVIRInv", ["VCBR"] = "ItmSupplyKioskVCBRInv",
            ["VENC"] = "ItmSupplyKioskVENCInv", ["VNCA"] = "ItmSupplyKioskVNCAInv",
            ["VORB"] = "ItmVORBScrapKioskInv"
        };

    public static string SupplyTable(string region) => SupplyTables.TryGetValue(region, out var table)
        ? table : throw new ArgumentException("Unknown vanilla retail region: " + region, nameof(region));

    /// <summary>One bounded physical lot per native roll. Content owns probability, quantity and condition.</summary>
    public static void Add(NativeDefinitions definitions, string region, string item,
        double probability, StockCondition condition) => Add(definitions, region, item, probability, condition, 1);

    public static void Add(NativeDefinitions definitions, string region, string item,
        double probability, StockCondition condition, int quantity)
    {
        string table = SupplyTable(region);
        // A removed endpoint in a different game/mod combination must not disable the whole mod.
        // Do not create a fake trader or silently redirect its stock to another settlement.
        if (!DataHandler.dictLoot.TryGetValue(table, out var native) || native.strType != "item")
        {
            FrameworkLifecycle.Log("Regional stock skipped: missing native item table " + table);
            return;
        }
        MarketStock.Add(definitions, table, "PhobosRegional_" + region + "_" + item, item, probability, condition, quantity);
    }
}
