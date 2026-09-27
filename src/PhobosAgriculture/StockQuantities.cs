using System;

namespace PhobosAgriculture;

// Authored wholesale quantities per successful merchant offer, including regional stock.
internal static class StockQuantities
{
    internal const double EquipmentChance = .85, SupplyChance = .95;
    internal static double Chance(string item, double original) => Math.Min(1, Math.Max(
        item.StartsWith(IrrigationDefinitions.Pipe, StringComparison.Ordinal) ||
        !(item.EndsWith("Loose", StringComparison.Ordinal) || item.EndsWith("LooseDmg", StringComparison.Ordinal))
            ? SupplyChance : EquipmentChance, original));

    internal const int Machines = 8, Pipes = 128, Supplies = 64;
    internal static int For(string item)
    {
        if(item.StartsWith(BulkDefinitions.Tank,StringComparison.Ordinal))return BulkDefinitions.TankStock;
        if(item==BulkDefinitions.Nutrients)return BulkDefinitions.NutrientStock;
        if (item.StartsWith(IrrigationDefinitions.Pipe, StringComparison.Ordinal)) return Pipes;
        if (item.EndsWith("Loose", StringComparison.Ordinal) || item.EndsWith("LooseDmg", StringComparison.Ordinal)) return Machines;
        return Supplies;
    }
}
