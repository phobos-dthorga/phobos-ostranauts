using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

namespace PhobosAutoNav;

// Authored availability factors informed by native production and retail roles.
// Native supply/demand, condition, merchant margins and negotiation still set prices.
internal static class RegionalEconomy
{
    internal static readonly (string Region, double Factor)[] Profiles = {
        ("BCER", 1),
        ("BCRS", 0.75),
        ("EJDR", 0.75),
        ("HQCH", 0.5),
        ("JATL", 0.75),
        ("JFTS", 0.75),
        ("MHNG", 1),
        ("MSUZ", 1.25),
        ("MTRS", 1.25),
        ("MVOL", 1.5),
        ("OFLT", 0.5),
        ("SVIR", 0.5),
        ("VCBR", 0.75),
        ("VENC", 0.75),
        ("VNCA", 0.75)
    };

    internal static void Apply(NativeDefinitions d)
    {
        foreach (string merchant in new[] { "ItmOKLGSupplyKioskInv", "ItmOKLGFixer", "ItmTraderSanDiegoHalvorsonInv", "ItmVORBScrapKioskInv", "ItmTraderSanDiegoPolarisInv" })
        foreach (string item in new[] { NavigationService.ModuleId, NavigationService.PursuitId, NavigationService.FireControlId })
            MarketStock.AddMissing(d, merchant, "PhobosExpanded_AutoNav_" + merchant + "_" + item,
                item, StockQuantities.Chance(item, 0), StockCondition.Pristine, StockQuantities.Boards);
        foreach (var profile in Profiles)
        {
            var condition = profile.Region == "OFLT" ? StockCondition.Refurbished : StockCondition.Pristine;
            RegionalMarkets.Add(d, profile.Region, NavigationService.ModuleId, StockQuantities.Chance(NavigationService.ModuleId, .30 * profile.Factor), condition, StockQuantities.Boards);
            RegionalMarkets.Add(d, profile.Region, NavigationService.PursuitId, StockQuantities.Chance(NavigationService.PursuitId, .15 * profile.Factor), condition, StockQuantities.Boards);
            RegionalMarkets.Add(d, profile.Region, NavigationService.FireControlId, StockQuantities.Chance(NavigationService.FireControlId, .10 * profile.Factor), condition, StockQuantities.Boards);
        }
    }
}
