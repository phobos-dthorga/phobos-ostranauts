using Phobos.Ostranauts.Framework.Diagnostics;
using PhobosExchange.Core;

namespace PhobosExchange;

internal static class PerformanceMetrics
{
    // 7 October 2026 (Exchange 0.1.0, L97): stepping the market to the game's clock, once a real second (about 16 steps at
    // speed 16), and the bounded catch-up after a skip or a time jump.
    internal static PerformanceMetric? CatchUp, Drivers;
    internal static void Initialize()
    {
        CatchUp = Performance.RegisterOperation("exchange.catch_up", "processing");
        // Once a game hour: one dictionary lookup per driver in the game's market.
        Drivers = Performance.RegisterOperation("exchange.read_drivers", "processing");
        // The chart history held in memory: a fixed number of closes per listed company.
        Performance.RegisterFootprint("exchange.history_closes", "memory",
            () => Market.Model == null ? 0 : Market.Model.Count * (ExchangeRules.HourlyCloses + ExchangeRules.DailyCloses + ExchangeRules.WeeklyCloses));
    }
}
