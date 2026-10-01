using Phobos.Ostranauts.Framework.Data;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Wholesale lots per successful merchant offer and the availability floors, from the economy data pack's
/// lots and chanceFloors tables through the shared Framework resolver.</summary>
internal static class StockQuantities
{
    internal static double EquipmentChance => ShipbreakerEconomy.Pack.Floor(EconomySchema.Equipment, EconomyStock.DefaultFloor);
    internal static double SupplyChance => ShipbreakerEconomy.Pack.Floor(EconomySchema.Supplies, EconomyStock.DefaultFloor);
    internal static double Chance(string item, double original) => EconomyStock.Chance(ShipbreakerEconomy.Pack, item, original);

    internal static int Machines => ShipbreakerEconomy.Pack.Lot(EconomySchema.Equipment, EconomyStock.DefaultLot);
    internal static int Pipes => ShipbreakerEconomy.Pack.Lot(EconomySchema.Supplies, EconomyStock.DefaultLot);
    internal static int Coolant => ShipbreakerEconomy.Pack.Lot("coolant", EconomyStock.DefaultLot);
    internal static int Ingots => ShipbreakerEconomy.Pack.Lot("ingots", EconomyStock.DefaultLot);
    internal static int For(string item) => EconomyStock.Quantity(ShipbreakerEconomy.Pack, item);
}
