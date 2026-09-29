using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

namespace PhobosAgriculture;

// Authored availability factors informed by native production and retail roles.
// Native supply/demand, condition, merchant margins and negotiation still set prices.
internal static class RegionalEconomy
{
    internal static readonly (string Region, double Factor)[] Profiles = {
        ("BCER", 0.75),
        ("BCRS", 0.75),
        ("EJDR", 0.75),
        ("HQCH", 0.5),
        ("JATL", 0.75),
        ("JFTS", 0.75),
        ("MHNG", 1.25),
        ("MSUZ", 1.25),
        ("MTRS", 1.5),
        ("MVOL", 0.75),
        ("OFLT", 0.5),
        ("SVIR", 0.75),
        ("VCBR", 0.75),
        ("VENC", 1),
        ("VNCA", 1)
    };

    internal static void Apply(NativeDefinitions d)
    {
        foreach (string merchant in new[] { "ItmOKLGSupplyKioskInv", "ItmOKLGFixer", "ItmTraderSanDiegoHalvorsonInv", "ItmVORBScrapKioskInv" })
        foreach (string item in new[] { Definitions.Rack + "Loose", Definitions.Cooker + "Loose", IrrigationDefinitions.Supply + "Loose", WorkupDefinitions.Bench + "Loose", BulkDefinitions.Tank + "Loose", Definitions.PotatoSeed, Definitions.LettuceSeed, Definitions.Nutrient, BulkDefinitions.Nutrients, Definitions.Irrigation, Service.RecoveryCartridge, WorkupDefinitions.Makeup, IrrigationDefinitions.Pipe + "Loose", Definitions.Raw, Definitions.Leaves, Definitions.Meal })
            MarketStock.AddMissing(d, merchant, "PhobosExpanded_Agriculture_" + merchant + "_" + item,
                item, StockQuantities.Chance(item, 0), StockCondition.Pristine, StockQuantities.For(item));
        foreach (string merchant in new[] { "ItmOKLGFoodCart01KioskInv", "ItmOKLGFoodCart02KioskInv", "ItmTraderSanDiegoFutureFoodsInv" })
            MarketStock.Add(d, merchant, "PhobosHearthFood_" + merchant, Definitions.Meal,
                StockQuantities.SupplyChance, StockCondition.Pristine, StockQuantities.Supplies);
        foreach (var profile in Profiles)
        {
            var condition = profile.Region == "OFLT" ? StockCondition.Refurbished : StockCondition.Pristine;
            foreach (string machine in Definitions.MachineFamilies)
                RegionalMarkets.Add(d, profile.Region, machine + "Loose", StockQuantities.Chance(machine + "Loose", .25 * profile.Factor), condition, StockQuantities.For(machine+"Loose"));
            foreach (string item in new[] { Definitions.PotatoSeed, Definitions.LettuceSeed, Definitions.Nutrient,
                BulkDefinitions.Nutrients, Definitions.Irrigation, Service.RecoveryCartridge, WorkupDefinitions.Makeup, IrrigationDefinitions.Pipe + "Loose", Definitions.Raw, Definitions.Leaves, Definitions.Meal })
                RegionalMarkets.Add(d, profile.Region, item, StockQuantities.Chance(item, .65 * profile.Factor), StockCondition.Pristine, StockQuantities.For(item));
        }
        MaintenanceDefinitions.SetStat(d.Objects[WorkupDefinitions.Makeup], "IsCategoryIndustrialProducts", 1);
        foreach (string waste in new[] { WorkupDefinitions.Spent, Service.RecoveryReject, RecyclerCapture.Wet })
            MaintenanceDefinitions.SetStat(d.Objects[waste], "IsCategoryTrash", 1);
    }
}
