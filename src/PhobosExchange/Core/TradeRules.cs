using System;

namespace PhobosExchange.Core;

/// <summary>Why an order cannot go through. Each is a condition only the player can change.</summary>
public enum Refusal { None, NoShares, OrderTooLarge, HoldingCap, NotEnoughCash, NotHeld, TooSmall }

/// <summary>The prices a company trades at now: the middle, what a buyer pays and what a seller gets, before impact.</summary>
public readonly struct Quote
{
    public readonly double Mid, Bid, Ask;
    public Quote(double mid, double bid, double ask) { Mid = mid; Bid = bid; Ask = ask; }
}

/// <summary>An order worked out before it is made: the price per share with impact, the money each way, or why not.</summary>
public sealed class Fill
{
    public bool Buy;
    public long Shares;
    public double PerShare, Gross, Commission;
    /// <summary>What the player pays (a buy) or receives (a sale), commission included, in credits.</summary>
    public double Total;
    /// <summary>The push on the log price the order leaves behind.</summary>
    public double Impact;
    public Refusal Refusal;
    /// <summary>For a refusal, the figure that limits the order: the largest order, the holding cap, or the shares held.</summary>
    public double Limit;
    public bool Ok => Refusal == Refusal.None;
}

/// <summary>Buying and selling (Phobos Exchange 0.1.0). Whole shares only, paid in cash, never on credit or short. The
/// spread and the commission are the house's take; an order's own push on the price follows the square-root shape real
/// markets show (Tóth et al., Physical Review X 1:021006, 2011), at an authored scale. A buy pays two thirds of its own
/// push and leaves the price pushed; selling straight back pays the spread, two commissions and part of the push again,
/// so a quick round trip always loses.</summary>
public static class TradeRules
{
    public static Quote QuoteOf(double lnPrice, double spread)
    {
        double mid = ExchangeRules.Price(lnPrice);
        return new Quote(mid, mid * (1 - spread / 2), mid * (1 + spread / 2));
    }

    /// <summary>The push an order of <paramref name="shares"/> puts on the log price.</summary>
    public static double ImpactOf(CompanyEntry c, MarketEntry m, long shares) =>
        shares <= 0 ? 0 : m.impact * c.volatility * Math.Sqrt(shares / c.dailyVolume);

    /// <summary>The largest single order, in shares.</summary>
    public static long MaxOrder(CompanyEntry c, MarketEntry m) => Math.Max(1, (long)Math.Floor(m.maxOrderShare * c.dailyVolume));

    public static double CommissionOf(MarketEntry m, double gross) => Math.Round(Math.Max(m.minCommission, m.commission * gross), 2);

    public static Fill Buy(CompanyEntry c, MarketEntry m, double lnPrice, long shares, Holding? held, double cash)
    {
        var fill = new Fill { Buy = true, Shares = shares };
        if (shares <= 0) { fill.Refusal = Refusal.NoShares; return fill; }
        long maxOrder = MaxOrder(c, m);
        if (shares > maxOrder) { fill.Refusal = Refusal.OrderTooLarge; fill.Limit = maxOrder; return fill; }
        fill.Impact = ImpactOf(c, m, shares);
        fill.PerShare = QuoteOf(lnPrice, c.spread).Ask * Math.Exp(fill.Impact * 2 / 3);
        fill.Gross = Math.Round(fill.PerShare * shares, 2);
        fill.Commission = CommissionOf(m, fill.Gross);
        fill.Total = Math.Round(fill.Gross + fill.Commission, 2);
        long after = (held?.Shares ?? 0) + shares;
        if (after * fill.PerShare > m.maxHolding) { fill.Refusal = Refusal.HoldingCap; fill.Limit = m.maxHolding; return fill; }
        if (!(cash >= fill.Total)) { fill.Refusal = Refusal.NotEnoughCash; fill.Limit = cash; return fill; }
        return fill;
    }

    public static Fill Sell(CompanyEntry c, MarketEntry m, double lnPrice, long shares, Holding? held)
    {
        var fill = new Fill { Buy = false, Shares = shares };
        if (shares <= 0) { fill.Refusal = Refusal.NoShares; return fill; }
        long have = held?.Shares ?? 0;
        if (shares > have) { fill.Refusal = Refusal.NotHeld; fill.Limit = have; return fill; }
        long maxOrder = MaxOrder(c, m);
        if (shares > maxOrder) { fill.Refusal = Refusal.OrderTooLarge; fill.Limit = maxOrder; return fill; }
        fill.Impact = -ImpactOf(c, m, shares);
        fill.PerShare = QuoteOf(lnPrice, c.spread).Bid * Math.Exp(fill.Impact * 2 / 3);
        fill.Gross = Math.Round(fill.PerShare * shares, 2);
        fill.Commission = CommissionOf(m, fill.Gross);
        fill.Total = Math.Round(fill.Gross - fill.Commission, 2);
        if (!(fill.Total > 0)) { fill.Refusal = Refusal.TooSmall; fill.Limit = fill.Commission; return fill; }
        return fill;
    }

    /// <summary>The holding after a fill: a buy adds its cost (commission included) to the basis; a sale takes the
    /// average cost of the shares sold off it and books the difference as realised.</summary>
    public static void Apply(Holding holding, Fill fill)
    {
        if (!fill.Ok) throw new InvalidOperationException("A refused order changes nothing.");
        if (fill.Buy) { holding.Shares += fill.Shares; holding.Cost += fill.Total; return; }
        double basis = holding.Shares <= 0 ? 0 : holding.Cost * fill.Shares / holding.Shares;
        holding.Shares -= fill.Shares;
        holding.Cost = holding.Shares == 0 ? 0 : holding.Cost - basis;
        holding.Realised += fill.Total - basis;
    }

    /// <summary>The most shares the player can buy now: the largest order, the holding cap and the cash, all at once.</summary>
    public static long MaxBuy(CompanyEntry c, MarketEntry m, double lnPrice, Holding? held, double cash)
    {
        long lo = 0, hi = MaxOrder(c, m);
        if (Buy(c, m, lnPrice, 1, held, cash).Refusal != Refusal.None) return 0;
        while (lo < hi)
        {
            long mid = lo + (hi - lo + 1) / 2;
            if (Buy(c, m, lnPrice, mid, held, cash).Ok) lo = mid; else hi = mid - 1;
        }
        return lo;
    }

    /// <summary>What the holding would fetch if sold now in orders no larger than the largest allowed, before impact.</summary>
    public static double Value(Holding? held, double lnPrice, double spread) => (held?.Shares ?? 0) * QuoteOf(lnPrice, spread).Bid;
}

/// <summary>Price alerts: each side fires once, when the price reaches it, and is then cleared.</summary>
public static class AlertRules
{
    /// <summary>Checks an alert against a price: +1 when the upper level was reached, −1 the lower, 0 neither. A side
    /// that fires is cleared.</summary>
    public static int Check(Alert alert, double price)
    {
        if (alert.Above is double above && price >= above) { alert.Above = null; return 1; }
        if (alert.Below is double below && price <= below) { alert.Below = null; return -1; }
        return 0;
    }

    /// <summary>Whether a level makes sense now: an upper level above the price, a lower one below it.</summary>
    public static bool Valid(double level, double price, bool above) =>
        level > 0 && !double.IsInfinity(level) && (above ? level > price : level < price);
}
