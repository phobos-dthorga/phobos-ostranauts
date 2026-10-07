using Ostranauts.Trading;

namespace PhobosExchange;

/// <summary>Reads the game's own cargo market (Ostranauts 1.0.1.5: <c>MarketManager</c>, <c>ShipMarket</c>) for the
/// drivers. Read-only: every station's market is kept by the game for every station in the system, loaded or not, and
/// its price factor per category of goods rises when that station runs short and falls when it has plenty. Nothing
/// here changes the game's market.</summary>
internal static class NativeMarket
{
    /// <summary>The game's price factor for a category at a station, or null when the station has no market or does not
    /// price that category (the driver then holds still).</summary>
    public static double? Factor(string station, string category)
    {
        var market = MarketManager.GetShipMarket(station);
        if (market?.PriceModifiers == null || !market.PriceModifiers.TryGetValue(category, out float factor)) return null;
        return factor;
    }

    /// <summary>Why a driver cannot read anything, or null when the game knows the station's market and the category.</summary>
    public static string? Problem(string station, string category)
    {
        if (DataHandler.GetDataCoCollection(category) == null) return Text.Get("Market.no_category");
        if (CrewSim.system != null && CrewSim.system.GetShipByRegID(station) == null) return Text.Get("Market.no_station");
        return null;
    }

    /// <summary>The station's name as the game shows it, or its registration.</summary>
    public static string StationName(string station)
    {
        var ship = CrewSim.system?.GetShipByRegID(station);
        return ship != null && !string.IsNullOrWhiteSpace(ship.publicName) ? ship.publicName : station;
    }

    /// <summary>The category's name as the game shows it (Consumer Goods, Ores), or its id.</summary>
    public static string CategoryName(string category) => DataHandler.GetDataCoCollection(category)?.FriendlyName ?? category;

    /// <summary>The game's own description of a station's market: what it makes and how full its stores are.</summary>
    public static string Status(string station) => MarketManager.GetStatusForShip(station);
}
