using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PhobosExchange.Core;

/// <summary>A fixed number of closing log prices, one per consecutive bucket (an hour, a game day or a game week), newest
/// last. The close of a bucket is the price as the next one begins. A gap of several buckets (a long skip) fills with the
/// newest close, never more than the capacity, so a time jump of any size costs at most one pass.</summary>
public sealed class CloseSeries
{
    public readonly int Capacity;
    private readonly double[] values;
    private int start, count;

    public CloseSeries(int capacity)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity; values = new double[capacity];
    }

    public int Count => count;
    /// <summary>The newest close's bucket; meaningless while empty.</summary>
    public long LastBucket { get; private set; }
    public long FirstBucket => LastBucket - count + 1;
    /// <summary>The close at position <paramref name="i"/>, 0 being the oldest kept.</summary>
    public double this[int i] => i < 0 || i >= count ? throw new ArgumentOutOfRangeException(nameof(i)) : values[(start + i) % Capacity];

    public void Push(long bucket, double value)
    {
        if (count > 0 && bucket <= LastBucket)
        {
            if (bucket == LastBucket) values[(start + count - 1) % Capacity] = value;
            return;
        }
        long gap = count == 0 ? 1 : bucket - LastBucket;
        int fills = (int)Math.Min(gap, Capacity);
        for (int k = 0; k < fills; k++) Append(value);
        LastBucket = bucket;
    }

    private void Append(double v)
    {
        if (count < Capacity) { values[(start + count) % Capacity] = v; count++; }
        else { values[start] = v; start = (start + 1) % Capacity; }
    }

    /// <summary>Moves every close by the same amount (a new listing's prices are set once its backfill is done).</summary>
    public void Shift(double offset)
    {
        for (int i = 0; i < count; i++) values[(start + i) % Capacity] += offset;
    }

    public void Clear() { start = 0; count = 0; }

    /// <summary>Copies the closes, oldest first, into a buffer (no allocation for a reused buffer).</summary>
    public int CopyTo(double[] buffer)
    {
        int n = Math.Min(count, buffer.Length);
        for (int i = 0; i < n; i++) buffer[i] = values[(start + count - n + i) % Capacity];
        return n;
    }

    // ---- Saved form ---------------------------------------------------------------------------------------------
    // A series is saved in parts of at most PartSize closes: "1|<first bucket>|<base, 16 hex>|v;v;v", each v the close
    // less the base, in units of 1/10,000 of a log, in signed base 36. Rounding is to 0.005% and never accumulates,
    // because each close is relative to its part's base. Parts stay well under the store's 512-character values.
    public const int PartSize = 60;
    private const double Scale = 10000;

    public int PartCount => (count + PartSize - 1) / PartSize;

    public string EncodePart(int part)
    {
        int from = part * PartSize, to = Math.Min(count, from + PartSize);
        if (from >= to) throw new ArgumentOutOfRangeException(nameof(part));
        double baseline = this[from];
        var text = new StringBuilder(8 + 16 + 5 * (to - from));
        text.Append(Record.FormatTag).Append('|').Append((FirstBucket + from).ToString(CultureInfo.InvariantCulture)).Append('|').Append(Record.Hex(baseline)).Append('|');
        for (int i = from; i < to; i++)
        {
            if (i > from) text.Append(';');
            text.Append(Base36(Math.Max(-2000000000L, Math.Min(2000000000L, (long)Math.Round((this[i] - baseline) * Scale)))));
        }
        return text.ToString();
    }

    /// <summary>Rebuilds a series from its saved parts (part number to value). False, with an empty series, when any part
    /// is unreadable or the parts do not join up; history only feeds the charts, so it then starts afresh.</summary>
    public static bool TryDecode(IReadOnlyDictionary<int, string> parts, int capacity, out CloseSeries series)
    {
        series = new CloseSeries(capacity);
        if (parts.Count == 0) return true;
        long expected = 0;
        for (int part = 0; part < parts.Count; part++)
        {
            if (!parts.TryGetValue(part, out var value)) { series.Clear(); return false; }
            var p = value.Split('|');
            if (p.Length != 4 || p[0] != Record.FormatTag || !long.TryParse(p[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long first) ||
                !Record.TryHex(p[2], out double baseline)) { series.Clear(); return false; }
            if (part > 0 && first != expected) { series.Clear(); return false; }
            var items = p[3].Split(';');
            if (items.Length == 0 || items.Length > PartSize || part < parts.Count - 1 && items.Length != PartSize) { series.Clear(); return false; }
            for (int i = 0; i < items.Length; i++)
            {
                if (!TryBase36(items[i], out long v)) { series.Clear(); return false; }
                series.Push(first + i, baseline + v / Scale);
            }
            expected = first + items.Length;
        }
        return true;
    }

    private const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";

    public static string Base36(long v)
    {
        if (v == 0) return "0";
        bool negative = v < 0;
        ulong u = negative ? (ulong)(-(v + 1)) + 1 : (ulong)v;
        var chars = new char[14];
        int i = chars.Length;
        while (u > 0) { chars[--i] = Digits[(int)(u % 36)]; u /= 36; }
        if (negative) chars[--i] = '-';
        return new string(chars, i, chars.Length - i);
    }

    public static bool TryBase36(string s, out long v)
    {
        v = 0;
        if (string.IsNullOrEmpty(s)) return false;
        bool negative = s[0] == '-';
        int from = negative ? 1 : 0;
        // Twelve base-36 digits always fit a long.
        if (from >= s.Length || s.Length - from > 12) return false;
        long acc = 0;
        for (int i = from; i < s.Length; i++)
        {
            int d = Digits.IndexOf(s[i]);
            if (d < 0) return false;
            acc = acc * 36 + d;
        }
        v = negative ? -acc : acc;
        return true;
    }
}

/// <summary>The save's own price history over its whole life (Phobos Exchange 0.3.0): the log price at the start of each
/// calendar month, at most <see cref="ExchangeRules.LifetimeCapacity"/> points. Point 0 is the anchor, where the save's
/// own history begins; it is never replaced or dropped, and the past generated before it joins it. When full, the points
/// are thinned to every second, fourth, eighth month (counted on the calendar), always keeping the anchor and the newest,
/// so a save played for decades keeps a bounded record of all of it.</summary>
public sealed class LifetimeSeries
{
    public readonly int Capacity;
    private readonly long[] months;
    private readonly double[] values;

    public LifetimeSeries(int capacity = ExchangeRules.LifetimeCapacity)
    {
        if (capacity < 3) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity; months = new long[capacity]; values = new double[capacity];
    }

    public int Count { get; private set; }
    /// <summary>Months per point on the grid: a power of two.</summary>
    public int Resolution { get; private set; } = 1;
    /// <summary>Set when the saved series could not be read (a newer version's, or damaged): it is then kept as saved and
    /// neither sampled nor written over.</summary>
    public bool Foreign { get; internal set; }
    public long MonthAt(int i) => i >= 0 && i < Count ? months[i] : throw new ArgumentOutOfRangeException(nameof(i));
    public double this[int i] => i >= 0 && i < Count ? values[i] : throw new ArgumentOutOfRangeException(nameof(i));
    public long LastMonth => Count > 0 ? months[Count - 1] : long.MinValue;

    /// <summary>Starts the series at its anchor; only while empty.</summary>
    public void Open(long month, double ln)
    {
        if (Foreign || Count > 0) return;
        months[0] = month; values[0] = ln; Count = 1;
    }

    /// <summary>The price at the start of a month. A month already held, or older, is ignored; an off-grid newest point
    /// gives way to a newer one; a full series is thinned first.</summary>
    public void Sample(long month, double ln)
    {
        if (Foreign || Count == 0 || month <= months[Count - 1]) return;
        if (Count == Capacity) Thin();
        // Thinned or not, an off-grid newest point only ever holds the place of the next one.
        if (Count >= 2 && months[Count - 1] % Resolution != 0) { months[Count - 1] = month; values[Count - 1] = ln; return; }
        months[Count] = month; values[Count] = ln; Count++;
    }

    private void Thin()
    {
        do
        {
            Resolution *= 2;
            int kept = 0;
            for (int i = 0; i < Count; i++)
                if (i == 0 || i == Count - 1 || months[i] % Resolution == 0) { months[kept] = months[i]; values[kept] = values[i]; kept++; }
            Count = kept;
        } while (Count >= Capacity);
    }

    /// <summary>Moves every point by the same amount (a new market's prices are set once its backfill is done).</summary>
    public void Shift(double offset) { for (int i = 0; i < Count; i++) values[i] += offset; }

    // ---- Saved form ---------------------------------------------------------------------------------------------
    // Parts of at most PartChars characters: "1|<resolution>|<first month>|<base, 16 hex>|<month gaps>|<values>", the gaps
    // from the previous point (the first 0) and the values less the base in 1/10,000 of a log, both in base 36 joined by
    // ';'. Parts are written oldest first and read back in order.
    public const int PartChars = 480, MaxParts = 16;

    public List<string> EncodeParts()
    {
        var parts = new List<string>();
        int i = 0;
        while (i < Count)
        {
            double baseline = values[i];
            var gaps = new StringBuilder(); var items = new StringBuilder();
            int start = i;
            string head = Record.FormatTag + "|" + Resolution.ToString(CultureInfo.InvariantCulture) + "|" + months[i].ToString(CultureInfo.InvariantCulture) + "|" + Record.Hex(baseline) + "|";
            while (i < Count)
            {
                string gap = CloseSeries.Base36(i == start ? 0 : months[i] - months[i - 1]);
                string value = CloseSeries.Base36(Math.Max(-2000000000L, Math.Min(2000000000L, (long)Math.Round((values[i] - baseline) * 10000))));
                if (i > start && head.Length + gaps.Length + items.Length + gap.Length + value.Length + 3 > PartChars) break;
                if (i > start) { gaps.Append(';'); items.Append(';'); }
                gaps.Append(gap); items.Append(value);
                i++;
            }
            parts.Add(head + gaps + "|" + items);
        }
        return parts;
    }

    /// <summary>Rebuilds a series from its saved parts (part number to value). False when a part is missing, unreadable,
    /// from a newer version, or the points do not run forward; the caller then keeps the parts as they were.</summary>
    public static bool TryDecode(IReadOnlyDictionary<int, string> parts, out LifetimeSeries series)
    {
        series = new LifetimeSeries();
        if (parts.Count == 0) return true;
        if (parts.Count > MaxParts) return false;
        int resolution = 0;
        for (int part = 0; part < parts.Count; part++)
        {
            if (!parts.TryGetValue(part, out var value)) return false;
            var p = value.Split('|');
            if (p.Length != 6 || p[0] != Record.FormatTag || !int.TryParse(p[1], NumberStyles.None, CultureInfo.InvariantCulture, out int res) || res < 1 || (res & (res - 1)) != 0 ||
                !Record.TryLong(p[2], out long first) || !Record.TryHex(p[3], out double baseline)) return false;
            if (part > 0 && res != resolution) return false;
            resolution = res;
            var gaps = p[4].Split(';'); var items = p[5].Split(';');
            if (gaps.Length != items.Length || gaps.Length == 0) return false;
            long month = first;
            for (int i = 0; i < gaps.Length; i++)
            {
                if (!CloseSeries.TryBase36(gaps[i], out long gap) || !CloseSeries.TryBase36(items[i], out long v) || (i == 0 ? gap != 0 : gap <= 0)) return false;
                month += gap;
                if (series.Count >= series.Capacity || series.Count > 0 && month <= series.months[series.Count - 1]) return false;
                series.months[series.Count] = month; series.values[series.Count] = baseline + v / 10000.0; series.Count++;
            }
        }
        series.Resolution = resolution;
        return true;
    }
}

/// <summary>A company's chart history: hourly, daily and weekly closes, and its lifetime points (0.3.0).</summary>
public sealed class PriceHistory
{
    public readonly CloseSeries Hourly = new(ExchangeRules.HourlyCloses);
    public readonly CloseSeries Daily = new(ExchangeRules.DailyCloses);
    public readonly CloseSeries Weekly = new(ExchangeRules.WeeklyCloses);
    public LifetimeSeries Lifetime { get; internal set; } = new();

    public CloseSeries Of(char resolution) => resolution switch { 'h' => Hourly, 'd' => Daily, 'w' => Weekly, _ => throw new ArgumentOutOfRangeException(nameof(resolution)) };
    public static readonly char[] Resolutions = { 'h', 'd', 'w' };
    /// <summary>The saved key letter of the lifetime points; older versions keep its keys untouched.</summary>
    public const char LifetimeResolution = 'm';

    public void Shift(double offset) { Hourly.Shift(offset); Daily.Shift(offset); Weekly.Shift(offset); Lifetime.Shift(offset); }
}
