using Phobos.Ostranauts.Framework.Data;

namespace PhobosAgriculture;

/// <summary>Wholesale lots per successful merchant offer and the availability floors, from the economy data pack's
/// lots and chanceFloors tables through the shared Framework resolver.</summary>
internal static class StockQuantities
{
    internal static double EquipmentChance => AgricultureEconomy.Pack.Floor(EconomySchema.Equipment, EconomyStock.DefaultFloor);
    internal static double SupplyChance => AgricultureEconomy.Pack.Floor(EconomySchema.Supplies, EconomyStock.DefaultFloor);
    internal static double Chance(string item, double original) => EconomyStock.Chance(AgricultureEconomy.Pack, item, original);

    internal static int Machines => AgricultureEconomy.Pack.Lot(EconomySchema.Equipment, EconomyStock.DefaultLot);
    internal static int Pipes => AgricultureEconomy.Pack.Lot("pipes", EconomyStock.DefaultLot);
    internal static int Supplies => AgricultureEconomy.Pack.Lot(EconomySchema.Supplies, EconomyStock.DefaultLot);
    internal static int For(string item) => EconomyStock.Quantity(AgricultureEconomy.Pack, item);
}
