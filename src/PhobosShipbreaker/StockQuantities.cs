using System;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

// Authored wholesale quantities per successful merchant offer, including regional stock.
internal static class StockQuantities
{
    internal const int Machines = 8, Sections = 24, Pipes = 128, Coolant = 64;
    internal static int For(string item)
    {
        if (item.StartsWith(FurnaceCooling.Conduit, StringComparison.Ordinal)) return Pipes;
        if (item == FurnaceService.CoolantStock) return Coolant;
        if (item == ProcessRules.AssemblySection || item == ReclaimerRules.Section || item == FurnaceRules.Section) return Sections;
        return Machines;
    }
}
