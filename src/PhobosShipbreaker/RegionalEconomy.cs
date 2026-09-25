using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

// Authored availability factors informed by native production and retail roles.
// Native supply/demand, condition, merchant margins and negotiation still set prices.
internal static class RegionalEconomy
{
    internal static readonly (string Region, double Factor)[] Profiles = {
        ("BCER", 1),
        ("BCRS", 1.25),
        ("EJDR", 0.75),
        ("HQCH", 0.75),
        ("JATL", 0.75),
        ("JFTS", 1),
        ("MHNG", 0.5),
        ("MSUZ", 1),
        ("MTRS", 1.5),
        ("MVOL", 1.5),
        ("OFLT", 0.5),
        ("SVIR", 1.25),
        ("VCBR", 1),
        ("VENC", 0.75),
        ("VNCA", 1.25)
    };

    internal static void Apply(NativeDefinitions d)
    {
        foreach (var profile in Profiles)
        {
            var condition = profile.Region == "OFLT" ? StockCondition.Refurbished : StockCondition.Pristine;
            foreach (var machine in EquipmentEconomy.Machines)
            {
                bool small = machine.Prefix == IntakeRules.Chute || machine.Prefix == CollectorRules.Prefix;
                Offer(machine.Prefix + "Loose", small ? .30 : .20);
            }
            foreach (string section in new[] { ProcessRules.AssemblySection, ReclaimerRules.Section, FurnaceRules.Section })
                Offer(section, .30);
            Offer(FurnaceCooling.Conduit + "Loose", .65);
            // Consumable charge quality is independent of refurbished machinery.
            RegionalMarkets.Add(d, profile.Region, FurnaceService.CoolantStock, .65 * profile.Factor, StockCondition.Pristine, StockQuantities.Coolant);
            void Offer(string item, double chance) => RegionalMarkets.Add(d, profile.Region, item, chance * profile.Factor, condition, StockQuantities.For(item));
        }
        // Packaged working fluid is an industrial consumable, never potable water.
        MaintenanceDefinitions.SetStat(d.Objects[FurnaceService.CoolantStock], "IsCategoryIndustrialProducts", 1);
        MaintenanceDefinitions.SetStat(d.Objects[FurnaceService.CoolantWaste], "IsCategoryTrash", 1);
    }
}
