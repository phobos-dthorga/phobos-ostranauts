using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Regional availability across the vanilla solar system, from the economy data pack's region factors
/// and regional section. Native supply/demand, condition, merchant margins and negotiation still set prices.</summary>
internal static class RegionalEconomy
{
    internal static (string Region, double Factor)[] Profiles => ShipbreakerEconomy.Pack.regions.Select(p => (p.Key, p.Value)).ToArray();

    internal static readonly string[] TerminalRemainders = { ReclaimerRules.Reject, FurnaceRules.Remainder, FurnaceRecipes.SteelRemainder };

    internal static void Apply(NativeDefinitions d)
    {
        EconomyStock.ApplyRegional(d, ShipbreakerEconomy.Pack, ShipbreakerEconomy.OwnerTag, EquipmentEconomy.Sales);
        // Packaged working fluid is an industrial consumable, never potable water.
        MaintenanceDefinitions.SetStat(d.Objects[FurnaceService.CoolantStock], "IsCategoryIndustrialProducts", 1);
        MaintenanceDefinitions.SetStat(d.Objects[FurnaceService.CoolantWaste], "IsCategoryTrash", 1);
        // Terminal rejects and melt remainders are waste, never stock.
        foreach (string reject in FeedFamilies.RejectKg.Keys.Concat(TerminalRemainders)) MaintenanceDefinitions.SetStat(d.Objects[reject], "IsCategoryTrash", 1);
    }
}
