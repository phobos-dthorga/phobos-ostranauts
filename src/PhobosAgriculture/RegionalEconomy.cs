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
        foreach (var profile in Profiles)
        {
            var condition = profile.Region == "OFLT" ? StockCondition.Refurbished : StockCondition.Pristine;
            foreach (string machine in new[] { Definitions.Rack, Definitions.Cooker, IrrigationDefinitions.Supply, WorkupDefinitions.Bench })
                RegionalMarkets.Add(d, profile.Region, machine + "Loose", .25 * profile.Factor, condition);
            foreach (string item in new[] { Definitions.PotatoSeed, Definitions.LettuceSeed, Definitions.Nutrient,
                Definitions.Irrigation, Service.RecoveryCartridge, WorkupDefinitions.Makeup, IrrigationDefinitions.Pipe + "Loose" })
                RegionalMarkets.Add(d, profile.Region, item, .65 * profile.Factor, StockCondition.Pristine);
        }
        MaintenanceDefinitions.SetStat(d.Objects[WorkupDefinitions.Makeup], "IsCategoryIndustrialProducts", 1);
        foreach (string waste in new[] { WorkupDefinitions.Spent, Service.RecoveryReject, RecyclerCapture.Wet })
            MaintenanceDefinitions.SetStat(d.Objects[waste], "IsCategoryTrash", 1);
    }
}
