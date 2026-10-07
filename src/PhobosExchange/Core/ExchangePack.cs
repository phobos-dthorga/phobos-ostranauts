using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Story;

namespace PhobosExchange.Core;

/// <summary>The <c>exchange</c> schema (Phobos Exchange 0.1.0): the market's terms, its sectors and the listed companies,
/// for players and add-ons to tune or add to. The rules that act on them (the time grid, the exact steps, history, the
/// guard on expected return) stay in code. Agent-authored balance; see the design record for what is authored.</summary>
public sealed class ExchangePack : DataPack
{
    public MarketEntry market = new();
    /// <summary>Sectors by id: companies in one sector share its trend phases.</summary>
    public Dictionary<string, SectorEntry> sectors = new(StringComparer.Ordinal);
    /// <summary>Listed companies by id. The id names the company's saved state, so it never changes once shipped.</summary>
    public Dictionary<string, CompanyEntry> companies = new(StringComparer.Ordinal);
}

/// <summary>A trend phase: how long rises and falls last, and how far they go.</summary>
public sealed class TrendEntry
{
    /// <summary>How long a phase lasts, roughly: the slow half-life, in game weeks.</summary>
    public double slowWeeks;
    /// <summary>How quickly a phase turns: the fast half-life, in game weeks; shorter than <see cref="slowWeeks"/>.</summary>
    public double fastWeeks;
    /// <summary>How far phases carry the price, as a standard deviation of the log price (0.15 is about 15%).</summary>
    public double sd;
}

/// <summary>The exchange's own terms.</summary>
public sealed class MarketEntry
{
    public string? notes;
    /// <summary>The exchange's name in the world.</summary>
    public string name = "";
    /// <summary>The broker's commission, as a share of an order's value.</summary>
    public double commission;
    /// <summary>The least commission on an order, in credits.</summary>
    public double minCommission;
    /// <summary>How hard an order pushes the price: impact = this × daily volatility × √(shares ÷ daily volume).</summary>
    public double impact;
    /// <summary>How quickly the push fades, as a half-life in game days.</summary>
    public double impactHalfLifeDays;
    /// <summary>The largest order, as a share of one company's daily volume.</summary>
    public double maxOrderShare;
    /// <summary>The most one player may hold in one company, valued at the asking price, in credits.</summary>
    public double maxHolding;
    /// <summary>The market-wide trend phase every company follows to its own degree.</summary>
    public TrendEntry trend = new();
    /// <summary>A change over one game day this large (as a share of the price) is reported with its cause.</summary>
    public double moveShare;
    /// <summary>A phase moving this fast (as a share of the price per game week) counts as a trend when it turns.</summary>
    public double turnShare;
    /// <summary>The fewest game days between two reports on one company.</summary>
    public double reportCooldownDays;
}

public sealed class SectorEntry
{
    public string? notes;
    public string name = "";
    public TrendEntry trend = new();
}

/// <summary>One native price signal a company follows: the game's price factor for a category of goods at a station.
/// Shortage there raises the factor; plenty lowers it.</summary>
public sealed class DriverEntry
{
    /// <summary>The station's registration (the game's market id), for example <c>MTRS</c>.</summary>
    public string station = "";
    /// <summary>The game's category of goods, for example <c>AnyWeapons</c>.</summary>
    public string category = "";
    /// <summary>How strongly the company follows it: positive when dear goods there help the company, negative when they
    /// cost it (an input).</summary>
    public double weight;
    /// <summary>The most this driver may move the log price either way.</summary>
    public double limit = 0.12;
}

public sealed class CompanyEntry
{
    public string? notes;
    /// <summary>The short trading symbol: two to five capital letters, unique.</summary>
    public string ticker = "";
    public string name = "";
    /// <summary>What the company does, in a wire service's neutral voice.</summary>
    public string profile = "";
    public string sector = "";
    /// <summary>The price when the company is first listed in a save, in credits per share.</summary>
    public double price;
    /// <summary>Shares that change hands in a game day: the scale of the player's price impact.</summary>
    public double dailyVolume;
    /// <summary>Day-to-day noise: the standard deviation of the log price over one game day (0.008 is 0.8%).</summary>
    public double volatility;
    /// <summary>How much the noise itself wanders between calm and stormy stretches (standard deviation of its log).</summary>
    public double volOfVol;
    /// <summary>How slowly accumulated noise fades back, as a half-life in game years.</summary>
    public double noiseHalfLifeYears;
    /// <summary>The steady upward drift of the log price, per game year (owner choice: real-world drift).</summary>
    public double drift;
    /// <summary>Sudden jumps: how many a game year, and their typical size (standard deviation of the log price).</summary>
    public double jumpsPerYear, jumpSize;
    /// <summary>The gap between buying and selling prices, as a share of the price.</summary>
    public double spread;
    /// <summary>How closely the company follows the market's and its sector's phases (1 is fully).</summary>
    public double followsMarket = 1, followsSector = 1;
    /// <summary>The company's own trend phases.</summary>
    public TrendEntry trend = new();
    /// <summary>The native price signals it follows. No default entries: the file reader adds a file's list to a default one.</summary>
    public List<DriverEntry> drivers = new();
    /// <summary>Story news that moves the price once (Phobos Exchange 0.2.0): when a story flag is set, by a story pack's arc
    /// or by another mod, the price jumps by the entry's share and the wire carries its line. No default entries.</summary>
    public List<NewsEntry> news = new();
}

/// <summary>A piece of story news and what it does to a company's price (Phobos Exchange 0.2.0; owner direction,
/// 7 October 2026: stories are data that writers and players add, and the exchange reacts to them).</summary>
public sealed class NewsEntry
{
    public string? notes;
    /// <summary>The story flag that brings the news: a story id, set by an arc's <c>setFlags</c> or by another mod.</summary>
    public string flag = "";
    /// <summary>How far the price moves when the news breaks, as a share of the price: 0.08 is up 8%, -0.1 down 10%. Once
    /// per save, and it stays: a contract won or a yard lost changes what the company is worth.</summary>
    public double move;
    /// <summary>The wire line the player reads when the news moves the price, in a wire service's neutral voice. Without
    /// one the wire says the company moved on the news.</summary>
    public string? wire;
}

public static class ExchangeSchema
{
    public const string Name = "exchange";
    public const int MaxIdLength = 24, MaxName = 40, MaxProfile = 400, MaxDrivers = 6, MaxCompanies = 40, MaxSectors = 16, MaxNews = 12, MaxWire = 300;
    /// <summary>The largest move one piece of news may make, either way, and the smallest worth reporting.</summary>
    public const double MaxNewsMove = 0.3, MinNewsMove = 0.005;
    private static readonly Regex Ticker = new("^[A-Z]{2,5}$", RegexOptions.CultureInvariant);
    private static readonly Regex Station = new("^[A-Z0-9]{3,8}$", RegexOptions.CultureInvariant);
    private static readonly Regex Category = new("^Any[A-Za-z0-9]{1,37}$", RegexOptions.CultureInvariant);

    /// <summary>The checks every file passes, shipped or player. Whether a driver's station and category exist in the game
    /// is checked by the native checks and, at load, by the game side.</summary>
    public static void Validate(ExchangePack pack)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        var m = pack.market ?? throw new ArgumentException("market: expected the exchange's terms");
        Notes(m.notes, "market");
        CheckName(m.name, "market.name");
        Range(m.commission, 0, 0.05, "market.commission");
        Range(m.minCommission, 0, 10000, "market.minCommission");
        Range(m.impact, 0, 5, "market.impact");
        Range(m.impactHalfLifeDays, 0.05, 30, "market.impactHalfLifeDays");
        Range(m.maxOrderShare, 0.01, 5, "market.maxOrderShare");
        Range(m.maxHolding, 1000, 100000000, "market.maxHolding");
        Trend(m.trend, "market.trend");
        Range(m.moveShare, 0.01, 0.5, "market.moveShare");
        Range(m.turnShare, 0.001, 0.5, "market.turnShare");
        Range(m.reportCooldownDays, 0, 30, "market.reportCooldownDays");

        if (pack.sectors == null) throw new ArgumentException("sectors: expected id to sector");
        if (pack.sectors.Count > MaxSectors) throw new ArgumentException("sectors: at most " + MaxSectors);
        foreach (var pair in pack.sectors)
        {
            string where = "sectors." + pair.Key;
            var s = pair.Value ?? throw new ArgumentException(where + ": empty");
            Id(pair.Key, where);
            Notes(s.notes, where);
            CheckName(s.name, where + ".name");
            Trend(s.trend, where + ".trend");
        }

        if (pack.companies == null) throw new ArgumentException("companies: expected id to company");
        if (pack.companies.Count > MaxCompanies) throw new ArgumentException("companies: at most " + MaxCompanies);
        var tickers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in pack.companies)
        {
            string where = "companies." + pair.Key;
            var c = pair.Value ?? throw new ArgumentException(where + ": empty");
            Id(pair.Key, where);
            Notes(c.notes, where);
            if (c.ticker == null || !Ticker.IsMatch(c.ticker)) throw new ArgumentException(where + ".ticker: two to five capital letters");
            if (!tickers.Add(c.ticker)) throw new ArgumentException(where + ".ticker: " + c.ticker + " is already another company's");
            CheckName(c.name, where + ".name");
            StorySchema.Words(c.profile, MaxProfile, where + ".profile");
            if (c.profile.IndexOf('[') >= 0) throw new ArgumentException(where + ".profile: no placeholders");
            if (c.sector == null || !pack.sectors.ContainsKey(c.sector)) throw new ArgumentException(where + ".sector: one of the sectors");
            Range(c.price, 0.5, 100000, where + ".price");
            Range(c.dailyVolume, 100, 1e9, where + ".dailyVolume");
            Range(c.volatility, 0.001, 0.05, where + ".volatility");
            Range(c.volOfVol, 0, 1, where + ".volOfVol");
            Range(c.noiseHalfLifeYears, 0.25, 50, where + ".noiseHalfLifeYears");
            Range(c.drift, 0, 0.1, where + ".drift");
            Range(c.jumpsPerYear, 0, 12, where + ".jumpsPerYear");
            Range(c.jumpSize, 0, 0.3, where + ".jumpSize");
            Range(c.spread, 0, 0.05, where + ".spread");
            Range(c.followsMarket, 0, 2, where + ".followsMarket");
            Range(c.followsSector, 0, 2, where + ".followsSector");
            Trend(c.trend, where + ".trend");
            if (c.drivers == null || c.drivers.Count > MaxDrivers) throw new ArgumentException(where + ".drivers: at most " + MaxDrivers);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < c.drivers.Count; i++)
            {
                string dw = where + ".drivers[" + i + "]";
                var d = c.drivers[i] ?? throw new ArgumentException(dw + ": empty");
                if (d.station == null || !Station.IsMatch(d.station)) throw new ArgumentException(dw + ".station: a station registration such as MTRS");
                if (d.category == null || !Category.IsMatch(d.category)) throw new ArgumentException(dw + ".category: a game category such as AnyWeapons");
                if (!seen.Add(d.station + "/" + d.category)) throw new ArgumentException(dw + ": the same station and category twice");
                if (!Finite(d.weight) || d.weight == 0 || Math.Abs(d.weight) > 1) throw new ArgumentException(dw + ".weight: from -1 to 1, not 0");
                Range(d.limit, 0.01, 0.3, dw + ".limit");
            }
            if (c.news == null || c.news.Count > MaxNews) throw new ArgumentException(where + ".news: at most " + MaxNews);
            var flags = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < c.news.Count; i++)
            {
                string nw = where + ".news[" + i + "]";
                var n = c.news[i] ?? throw new ArgumentException(nw + ": empty");
                Notes(n.notes, nw);
                if (!StorySchema.IsId(n.flag)) throw new ArgumentException(nw + ".flag: a story flag id, lowercase words joined by dashes");
                if (!flags.Add(n.flag)) throw new ArgumentException(nw + ".flag: " + n.flag + " is used twice for this company");
                if (!Finite(n.move) || Math.Abs(n.move) < MinNewsMove || n.move > MaxNewsMove || n.move < -MaxNewsMove)
                    throw new ArgumentException(nw + ".move: from -" + MaxNewsMove + " to " + MaxNewsMove + ", at least " + MinNewsMove + " either way");
                if (n.wire != null)
                {
                    StorySchema.Words(n.wire, MaxWire, nw + ".wire");
                    if (n.wire.IndexOf('[') >= 0) throw new ArgumentException(nw + ".wire: no placeholders");
                }
            }
            // One-off news is authored and bounded; the guard covers what a holder can expect from the market itself.
            double expected = ExpectedReturn(c, pack.sectors[c.sector].trend, m.trend);
            if (!(expected <= ExchangeRules.MaxExpectedReturn))
                throw new ArgumentException(where + ": expected yearly return " + expected.ToString("0.000") + " is above " + ExchangeRules.MaxExpectedReturn.ToString("0.00") +
                                            "; lower the drift, the noise, the trend phases or the jumps");
        }
    }

    /// <summary>The expected yearly return of buying the company at a random moment and holding it a game year: the drift,
    /// half the year's variance of every term (the convexity of a log price) and the jumps. The guard compares this with
    /// <see cref="ExchangeRules.MaxExpectedReturn"/>. Drivers count by a bound on their spread, since they follow the game.</summary>
    public static double ExpectedReturn(CompanyEntry c, TrendEntry sector, TrendEntry market)
    {
        double year = ExchangeRules.YearSeconds;
        double variance = c.followsMarket * c.followsMarket * Kernel(market).VarianceOfChange(year)
                        + c.followsSector * c.followsSector * Kernel(sector).VarianceOfChange(year)
                        + Kernel(c.trend).VarianceOfChange(year);
        double sigma2 = c.volatility * c.volatility / ExchangeRules.DaySeconds;
        variance += sigma2 * new OuKernel(c.noiseHalfLifeYears * year).VarianceOfChange(year);
        double driverRange = c.drivers?.Sum(d => d.limit) ?? 0;
        variance += driverRange * driverRange / 3;
        double jumps = c.jumpsPerYear * (Math.Exp(c.jumpSize * c.jumpSize / 2) - 1);
        return Math.Exp(c.drift + variance / 2 + jumps) - 1;
    }

    public static TrendKernel Kernel(TrendEntry t) => TrendKernel.FromWeeks(t.slowWeeks, t.fastWeeks, t.sd);

    private static void Trend(TrendEntry? t, string where)
    {
        if (t == null) throw new ArgumentException(where + ": expected slowWeeks, fastWeeks and sd");
        Range(t.fastWeeks, 0.25, 26, where + ".fastWeeks");
        Range(t.slowWeeks, 0.5, 104, where + ".slowWeeks");
        if (!(t.slowWeeks >= t.fastWeeks * 1.5)) throw new ArgumentException(where + ".slowWeeks: at least one and a half times fastWeeks");
        Range(t.sd, 0, 0.5, where + ".sd");
    }

    private static void Id(string id, string where)
    {
        if (!StorySchema.IsId(id, MaxIdLength)) throw new ArgumentException(where + ": an id is lowercase letters and digits joined by dashes, at most " + MaxIdLength);
    }

    private static void CheckName(string? name, string where)
    {
        if (!ExchangeRules.LedgerSafe(name) || name!.Length > MaxName) throw new ArgumentException(where + ": 1 to " + MaxName + " characters, without | , ; = # [ ] < > or line breaks");
    }

    private static void Notes(string? notes, string where)
    {
        if (notes != null && notes.Length > 2000) throw new ArgumentException(where + ".notes: at most 2000 characters");
    }

    private static void Range(double v, double min, double max, string where)
    {
        if (!Finite(v) || v < min || v > max) throw new ArgumentException(where + ": from " + min.ToString(System.Globalization.CultureInfo.InvariantCulture) + " to " + max.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    internal static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
}
