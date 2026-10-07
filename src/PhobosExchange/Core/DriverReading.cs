using System;

namespace PhobosExchange.Core;

/// <summary>What a driver reads from a station's cargo market (Phobos Exchange 0.5.2; owner decision, 8 October 2026):
/// the station's demand for a category, not how full its stock is. No game types, so the offline checks run it.
/// <para>Observed in the game's <c>ShipMarket</c> (Ostranauts 1.0.1.5): a category's price factor is
/// <c>1 + D × g</c>, where <c>D</c> is the station's net demand from its production, clamped to ±0.8 (up to 1.7 under a
/// blockade), and <c>g</c> is how empty its stock is (<c>1 − fill</c> when <c>D</c> is positive, <c>fill</c> when
/// negative). Every item a player trades there moves the stock, so a player who floods a station with a category's
/// cheapest goods swings the factor across its whole range for a few credits. A driver therefore recovers <c>D</c> from
/// the factor and the fill and follows the factor that demand gives at half stock: cargo trading cannot move it, while
/// blockades, station events and changes in production still do.</para></summary>
public static class DriverReading
{
    /// <summary>The save's driver basis: 0 or absent for the stock-following drivers of 0.1.0 to 0.5.1, this value for
    /// demand. A save on an older basis has its drivers settled once on load so no price jumps.</summary>
    public const int DemandBasis = 2;
    /// <summary>The game's clamp on net demand: its normal lower bound and its blockade upper bound.</summary>
    public const double MinDemand = -0.8, MaxDemand = 1.7;
    /// <summary>The stock level whose factor a driver follows.</summary>
    public const double ReferenceFill = 0.5;
    /// <summary>Below this share of the stock term the demand cannot be told apart from rounding: the reading holds.</summary>
    public const double MinShare = 0.02;
    /// <summary>The game stores its factor as a float.</summary>
    public const double FactorTolerance = 1e-5;

    /// <summary>The station's net demand for the category, or null when it cannot be read (no factor, or the stock at the
    /// end where the factor says nothing about demand).</summary>
    public static double? Demand(double? factor, double stock, double capacity)
    {
        if (factor is not double f || double.IsNaN(f) || double.IsInfinity(f) || double.IsNaN(stock) || double.IsNaN(capacity)) return null;
        // The game counts an empty or missing capacity as an empty stock.
        double fill = capacity > 0 ? Math.Min(1, Math.Max(0, stock / capacity)) : 0;
        double demand;
        if (f > 1 + FactorTolerance)
        {
            if (1 - fill < MinShare) return null;
            demand = (f - 1) / (1 - fill);
        }
        else if (f < 1 - FactorTolerance)
        {
            if (fill < MinShare) return null;
            demand = (f - 1) / fill;
        }
        else
        {
            // A factor of one is no demand only when neither end of the stock could have hidden it.
            if (fill < MinShare || 1 - fill < MinShare) return null;
            demand = 0;
        }
        return Math.Min(MaxDemand, Math.Max(MinDemand, demand));
    }

    /// <summary>The factor the driver follows: what the station's demand would give at half stock, or null.</summary>
    public static double? Factor(double? factor, double stock, double capacity) =>
        Demand(factor, stock, capacity) is double d ? 1 + d * ReferenceFill : null;
}
