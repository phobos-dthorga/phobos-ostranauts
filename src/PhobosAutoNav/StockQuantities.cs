using Phobos.Ostranauts.Framework.Data;

namespace PhobosAutoNav;

/// <summary>The board lot and availability floor, from the economy data pack through the shared Framework resolver.</summary>
internal static class StockQuantities
{
    internal static double EquipmentChance => AutoNavEconomy.Pack.Floor(EconomySchema.Equipment, EconomyStock.DefaultFloor);
    internal static double Chance(string item, double original) => EconomyStock.Chance(AutoNavEconomy.Pack, item, original);
    // Each successful merchant offer, in every region; rare derelict salvage stays separate.
    internal static int Boards => AutoNavEconomy.Pack.Lot("boards", EconomyStock.DefaultLot);
}
