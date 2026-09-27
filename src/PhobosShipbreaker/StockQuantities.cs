using System;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

// Authored wholesale quantities per successful merchant offer, including regional stock.
internal static class StockQuantities
{
    internal const double EquipmentChance = .85, SupplyChance = .95;
    internal static double Chance(string item, double original) => Math.Min(1, Math.Max(
        item == FurnaceService.CoolantStock || item.StartsWith(FurnaceCooling.Conduit, StringComparison.Ordinal)
            ? SupplyChance : EquipmentChance, original));

    internal const int Machines = 8, Sections = 24, Pipes = 128, Coolant = 64;
    internal static int For(string item)
    {
        if (item.StartsWith(FurnaceCooling.Conduit, StringComparison.Ordinal)) return Pipes;
        if (item == FurnaceService.CoolantStock) return Coolant;
        if (item == ProcessRules.AssemblySection || item == ReclaimerRules.Section || item == FurnaceRules.Section) return Sections;
        return Machines;
    }
}
