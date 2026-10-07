using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhobosExchange.Core;

/// <summary>Encoding helpers shared by the record and the history.</summary>
internal static class Record
{
    /// <summary>The format tag every value starts with; a value with another tag is a newer version's, kept untouched.</summary>
    public const string FormatTag = "1";

    /// <summary>A double as its 16 hexadecimal bits: exact, and the same on every runtime.</summary>
    public static string Hex(double v) => BitConverter.DoubleToInt64Bits(v).ToString("X16", CultureInfo.InvariantCulture);

    public static bool TryHex(string s, out double v)
    {
        v = 0;
        if (s == null || s.Length != 16 || !long.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out long bits)) return false;
        v = BitConverter.Int64BitsToDouble(bits);
        return !double.IsNaN(v) && !double.IsInfinity(v);
    }

    public static string Long(long v) => v.ToString(CultureInfo.InvariantCulture);
    public static bool TryLong(string s, out long v) => long.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out v);
}

/// <summary>A trend phase's state: its slow process and the gap to its fast one.</summary>
public sealed class TrendState
{
    public double Slow, Gap;
}

/// <summary>One company's model state at the last step.</summary>
public sealed class CompanyState
{
    /// <summary>The log price's fixed base, set when the company was listed and moved only to keep the price continuous
    /// when the pack's figures for it change.</summary>
    public double LnBase;
    /// <summary>The step the company was listed at: its drift counts from here.</summary>
    public long Listed;
    public readonly TrendState Trend = new();
    /// <summary>The log-volatility's deviation, the accumulated noise and jumps, and the fading push of the player's own
    /// orders, all in log units.</summary>
    public double LogVol, Noise, Impact;
    /// <summary>Each driver's smoothed contribution, in log units, in the pack's order.</summary>
    public double[] Drivers = Array.Empty<double>();
    /// <summary>The log price at the last step, saved so a change to the pack never makes the price jump.</summary>
    public double LastLn;
}

/// <summary>The story news that has moved one company's price (Phobos Exchange 0.2.0): the total move, in log units,
/// and the flags already applied, so each piece of news moves the price once per save. Saved under its own key, so a
/// 0.1.0 record reads unchanged and an older version keeps this one untouched.</summary>
public sealed class NewsState
{
    /// <summary>What news has moved the price for good, in log units, and the news that has broken at least once (whose
    /// lasting share is spent).</summary>
    public double Move;
    public readonly HashSet<string> Applied = new(StringComparer.Ordinal);
    public bool Empty => Move == 0 && Applied.Count == 0;
    /// <summary>Recurring news (Phobos Exchange 0.4.0), saved under <c>newsfx.&lt;company&gt;</c>: the unwinding part of
    /// every news jump so far, as it stood at <see cref="TransientStep"/>, fading at the company's half-life from there.</summary>
    public double Transient;
    public long TransientStep;
    /// <summary>Story flags set at or before this game time have been answered; null until the exchange first looks
    /// (then it is set to the latest news flag already set, so nothing old breaks again).</summary>
    public double? Watermark;
    public bool EffectsEmpty => Transient == 0 && Watermark == null;
}

/// <summary>Shares the player holds in one company.</summary>
public sealed class Holding
{
    public long Shares;
    /// <summary>What the shares held cost in all, commission included (the cost basis).</summary>
    public double Cost;
    /// <summary>Gains and losses already taken by selling.</summary>
    public double Realised;
}

/// <summary>A price alert on one company: tell the player when the price reaches a level. Each fires once.</summary>
public sealed class Alert
{
    public double? Above, Below;
    public bool Empty => Above == null && Below == null;
}

/// <summary>The player's exchange record, saved on the player character as one Phobos record
/// (<c>PhobosState.PhobosExchange</c>). Fields this version does not understand, and values with a newer format tag, are
/// kept exactly as they were; a company whose state cannot be read is left read-only rather than reset.</summary>
public sealed class ExchangeRecord
{
    public const string Name = "PhobosExchange";
    public const int Version = 1;
    private const string SeedKey = "seed", ClockKey = "clock", MarketKey = "trend.market", SectorPrefix = "trend.sector.", CompanyPrefix = "co.",
        HoldingPrefix = "hold.", AlertPrefix = "alert.", HistoryPrefix = "hist.", TestKey = "test", NewsPrefix = "news.", LifetimePrefix = "hist.m.", EffectsPrefix = "newsfx.";

    public ulong Seed;
    public bool HasSeed;
    /// <summary>The last step the market reached; null for a new record (the market is then backfilled).</summary>
    public long? Clock;
    public TrendState? Market;
    public Dictionary<string, TrendState> Sectors { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, CompanyState> Companies { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, Holding> Holdings { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, Alert> Alerts { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, PriceHistory> Histories { get; } = new(StringComparer.Ordinal);
    /// <summary>Story news applied per company (0.2.0).</summary>
    public Dictionary<string, NewsState> News { get; } = new(StringComparer.Ordinal);
    /// <summary>Companies whose saved state or holding this version cannot read; they are not traded or stepped.</summary>
    public HashSet<string> Unreadable { get; } = new(StringComparer.Ordinal);
    /// <summary>How many test commands changed this save, and the last one (owner rule: a test-changed save says so).</summary>
    public int TestChanges;
    public long LastTestStep;
    public string LastTest = "";
    private readonly Dictionary<string, string> kept = new(StringComparer.Ordinal);

    public NewsState NewsFor(string id)
    {
        if (!News.TryGetValue(id, out var n)) News[id] = n = new NewsState();
        return n;
    }

    public PriceHistory History(string id)
    {
        if (!Histories.TryGetValue(id, out var h)) Histories[id] = h = new PriceHistory();
        return h;
    }

    public static ExchangeRecord Decode(IReadOnlyDictionary<string, string>? fields)
    {
        var record = new ExchangeRecord();
        var histories = new Dictionary<(string Id, char Res), Dictionary<int, string>>();
        var lifetimes = new Dictionary<string, Dictionary<int, (string Key, string Value)>>(StringComparer.Ordinal);
        foreach (var pair in fields ?? new Dictionary<string, string>())
        {
            string key = pair.Key, value = pair.Value ?? "";
            var p = value.Split('|');
            bool ours = p.Length > 0 && p[0] == Record.FormatTag;
            if (key == SeedKey && ours && p.Length == 2 && p[1].Length == 16 && ulong.TryParse(p[1], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong seed))
            { record.Seed = seed; record.HasSeed = true; }
            else if (key == ClockKey && ours && p.Length == 2 && Record.TryLong(p[1], out long clock) && clock >= 0) record.Clock = clock;
            else if (key == MarketKey && ours && TryTrend(p, out var market)) record.Market = market;
            else if (key.StartsWith(SectorPrefix, StringComparison.Ordinal) && ours && TryTrend(p, out var sector)) record.Sectors[key.Substring(SectorPrefix.Length)] = sector;
            else if (key.StartsWith(CompanyPrefix, StringComparison.Ordinal))
            {
                string id = key.Substring(CompanyPrefix.Length);
                if (ours && TryCompany(p, out var company)) record.Companies[id] = company;
                else { record.kept[key] = value; record.Unreadable.Add(id); }
            }
            else if (key.StartsWith(HoldingPrefix, StringComparison.Ordinal))
            {
                string id = key.Substring(HoldingPrefix.Length);
                if (ours && TryHolding(p, out var holding)) record.Holdings[id] = holding;
                else { record.kept[key] = value; record.Unreadable.Add(id); }
            }
            else if (key.StartsWith(AlertPrefix, StringComparison.Ordinal) && ours && TryAlert(p, out var alert)) record.Alerts[key.Substring(AlertPrefix.Length)] = alert;
            else if (key.StartsWith(NewsPrefix, StringComparison.Ordinal) && ours && TryNews(p, out var news))
            { var n = record.NewsFor(key.Substring(NewsPrefix.Length)); n.Move = news.Move; n.Applied.UnionWith(news.Applied); }
            // Recurring news (0.4.0) under a key of its own, so 0.2 and 0.3 keep it untouched and still read news.<id>.
            else if (key.StartsWith(EffectsPrefix, StringComparison.Ordinal) && ours && TryEffects(p, record.NewsFor(key.Substring(EffectsPrefix.Length)))) { }
            else if (key.StartsWith(HistoryPrefix, StringComparison.Ordinal) && TryHistoryKey(key, out string hid, out char res, out int part))
            {
                if (!histories.TryGetValue((hid, res), out var parts)) histories[(hid, res)] = parts = new Dictionary<int, string>();
                parts[part] = value;
            }
            else if (key.StartsWith(LifetimePrefix, StringComparison.Ordinal) && TryLifetimeKey(key, out string lid, out int lpart))
            {
                if (!lifetimes.TryGetValue(lid, out var parts)) lifetimes[lid] = parts = new Dictionary<int, (string, string)>();
                parts[lpart] = (key, value);
            }
            else if (key == TestKey && ours && p.Length == 4 && int.TryParse(p[1], NumberStyles.None, CultureInfo.InvariantCulture, out int tests) && Record.TryLong(p[2], out long at))
            { record.TestChanges = tests; record.LastTestStep = at; record.LastTest = p[3]; }
            else record.kept[key] = value;
        }
        // History only feeds the charts: an unreadable series starts afresh rather than blocking anything.
        foreach (var pair in histories)
            if (CloseSeries.TryDecode(pair.Value, record.History(pair.Key.Id).Of(pair.Key.Res).Capacity, out var series))
                Copy(series, record.History(pair.Key.Id).Of(pair.Key.Res));
        // The lifetime points (0.3.0) are the save's only record of its own past beyond two years, so a series this
        // version cannot read is kept exactly as saved and left alone, never started afresh.
        foreach (var pair in lifetimes)
        {
            if (LifetimeSeries.TryDecode(pair.Value.ToDictionary(p => p.Key, p => p.Value.Value), out var life)) { record.History(pair.Key).Lifetime = life; continue; }
            foreach (var part in pair.Value.Values) record.kept[part.Key] = part.Value;
            record.History(pair.Key).Lifetime.Foreign = true;
        }
        return record;
    }

    public Dictionary<string, string> Encode()
    {
        var fields = new Dictionary<string, string>(kept, StringComparer.Ordinal);
        if (HasSeed) fields[SeedKey] = Record.FormatTag + "|" + Seed.ToString("X16", CultureInfo.InvariantCulture);
        if (Clock is long clock) fields[ClockKey] = Record.FormatTag + "|" + Record.Long(clock);
        if (Market != null) fields[MarketKey] = Trend(Market);
        foreach (var pair in Sectors) fields[SectorPrefix + pair.Key] = Trend(pair.Value);
        foreach (var pair in Companies)
        {
            var c = pair.Value;
            fields[CompanyPrefix + pair.Key] = string.Join("|", Record.FormatTag, Record.Hex(c.LnBase), Record.Long(c.Listed), Record.Hex(c.Trend.Slow), Record.Hex(c.Trend.Gap),
                Record.Hex(c.LogVol), Record.Hex(c.Noise), Record.Hex(c.Impact), Record.Hex(c.LastLn), string.Join(";", c.Drivers.Select(Record.Hex)));
        }
        foreach (var pair in Holdings)
            if (pair.Value.Shares > 0 || pair.Value.Realised != 0)
                fields[HoldingPrefix + pair.Key] = string.Join("|", Record.FormatTag, Record.Long(pair.Value.Shares), Record.Hex(pair.Value.Cost), Record.Hex(pair.Value.Realised));
        foreach (var pair in Alerts)
            if (!pair.Value.Empty)
                fields[AlertPrefix + pair.Key] = string.Join("|", Record.FormatTag, pair.Value.Above is double a ? Record.Hex(a) : "", pair.Value.Below is double b ? Record.Hex(b) : "");
        foreach (var pair in News)
            if (!pair.Value.Empty)
                fields[NewsPrefix + pair.Key] = string.Join("|", Record.FormatTag, Record.Hex(pair.Value.Move), string.Join(";", pair.Value.Applied.OrderBy(f => f, StringComparer.Ordinal)));
        foreach (var pair in News)
            if (!pair.Value.EffectsEmpty)
                fields[EffectsPrefix + pair.Key] = string.Join("|", Record.FormatTag, pair.Value.Watermark is double w ? Record.Hex(w) : "", Record.Hex(pair.Value.Transient), Record.Long(pair.Value.TransientStep));
        foreach (var pair in Histories)
        {
            foreach (char res in PriceHistory.Resolutions)
            {
                var series = pair.Value.Of(res);
                for (int part = 0; part < series.PartCount; part++)
                    fields[HistoryPrefix + res + "." + pair.Key + "." + part.ToString(CultureInfo.InvariantCulture)] = series.EncodePart(part);
            }
            var life = pair.Value.Lifetime;
            if (life.Foreign || life.Count == 0) continue;
            var parts = life.EncodeParts();
            for (int part = 0; part < parts.Count; part++) fields[LifetimePrefix + pair.Key + "." + part.ToString(CultureInfo.InvariantCulture)] = parts[part];
        }
        if (TestChanges > 0)
            fields[TestKey] = string.Join("|", Record.FormatTag, TestChanges.ToString(CultureInfo.InvariantCulture), Record.Long(LastTestStep), ExchangeRules.Clean(LastTest).Replace("|", "/"));
        return fields;
    }

    private static string Trend(TrendState t) => string.Join("|", Record.FormatTag, Record.Hex(t.Slow), Record.Hex(t.Gap));

    private static bool TryTrend(string[] p, out TrendState trend)
    {
        trend = new TrendState();
        return p.Length == 3 && Record.TryHex(p[1], out trend.Slow) && Record.TryHex(p[2], out trend.Gap);
    }

    private static bool TryCompany(string[] p, out CompanyState c)
    {
        c = new CompanyState();
        if (p.Length != 10) return false;
        if (!Record.TryHex(p[1], out c.LnBase) || !Record.TryLong(p[2], out c.Listed) || !Record.TryHex(p[3], out c.Trend.Slow) || !Record.TryHex(p[4], out c.Trend.Gap) ||
            !Record.TryHex(p[5], out c.LogVol) || !Record.TryHex(p[6], out c.Noise) || !Record.TryHex(p[7], out c.Impact) || !Record.TryHex(p[8], out c.LastLn)) return false;
        if (p[9].Length == 0) { c.Drivers = Array.Empty<double>(); return true; }
        var items = p[9].Split(';');
        var drivers = new double[items.Length];
        for (int i = 0; i < items.Length; i++) if (!Record.TryHex(items[i], out drivers[i])) return false;
        c.Drivers = drivers;
        return true;
    }

    private static bool TryNews(string[] p, out NewsState n)
    {
        n = new NewsState();
        if (p.Length != 3 || !Record.TryHex(p[1], out n.Move)) return false;
        if (p[2].Length > 0) foreach (var flag in p[2].Split(';')) { if (flag.Length == 0) return false; n.Applied.Add(flag); }
        return true;
    }

    private static bool TryEffects(string[] p, NewsState n)
    {
        if (p.Length != 4 || !Record.TryHex(p[2], out double transient) || !Record.TryLong(p[3], out long step)) return false;
        if (p[1].Length > 0) { if (!Record.TryHex(p[1], out double watermark)) return false; n.Watermark = watermark; }
        n.Transient = transient; n.TransientStep = step;
        return true;
    }

    private static bool TryHolding(string[] p, out Holding h)
    {
        h = new Holding();
        return p.Length == 4 && Record.TryLong(p[1], out h.Shares) && h.Shares >= 0 && Record.TryHex(p[2], out h.Cost) && Record.TryHex(p[3], out h.Realised);
    }

    private static bool TryAlert(string[] p, out Alert a)
    {
        a = new Alert();
        if (p.Length != 3) return false;
        if (p[1].Length > 0) { if (!Record.TryHex(p[1], out double above) || !(above > 0)) return false; a.Above = above; }
        if (p[2].Length > 0) { if (!Record.TryHex(p[2], out double below) || !(below > 0)) return false; a.Below = below; }
        return true;
    }

    private static bool TryHistoryKey(string key, out string id, out char res, out int part)
    {
        id = ""; res = 'h'; part = 0;
        // hist.<h|d|w>.<id>.<part>
        var p = key.Split('.');
        if (p.Length != 4 || p[1].Length != 1 || Array.IndexOf(PriceHistory.Resolutions, p[1][0]) < 0 || p[2].Length == 0 ||
            !int.TryParse(p[3], NumberStyles.None, CultureInfo.InvariantCulture, out part) || part > 8) return false;
        id = p[2]; res = p[1][0];
        return true;
    }

    /// <summary>hist.m.&lt;id&gt;.&lt;part&gt; (0.3.0); older versions keep these keys untouched.</summary>
    private static bool TryLifetimeKey(string key, out string id, out int part)
    {
        id = ""; part = 0;
        var p = key.Split('.');
        if (p.Length != 4 || p[2].Length == 0 || !int.TryParse(p[3], NumberStyles.None, CultureInfo.InvariantCulture, out part) || part >= LifetimeSeries.MaxParts) return false;
        id = p[2];
        return true;
    }

    private static void Copy(CloseSeries from, CloseSeries to)
    {
        to.Clear();
        for (int i = 0; i < from.Count; i++) to.Push(from.FirstBucket + i, from[i]);
    }
}
