using System;

namespace PhobosAgriculture;

// Authored wholesale quantities per successful merchant offer, including regional stock.
internal static class StockQuantities
{
    internal const int Machines = 8, Pipes = 128, Supplies = 64;
    internal static int For(string item)
    {
        if (item.StartsWith(IrrigationDefinitions.Pipe, StringComparison.Ordinal)) return Pipes;
        if (item.EndsWith("Loose", StringComparison.Ordinal) || item.EndsWith("LooseDmg", StringComparison.Ordinal)) return Machines;
        return Supplies;
    }
}
