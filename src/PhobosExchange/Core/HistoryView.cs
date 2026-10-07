using System;
using Phobos.Ostranauts.Framework;

namespace PhobosExchange.Core;

/// <summary>A chart's span: three days, 120 days, two years, or everything since the listing (0.3.0).</summary>
public enum ChartRange { Hours, Days, Weeks, All }

/// <summary>The points a price chart draws, oldest first (0.2.1, sources merged in 0.3.0): game-free so the panel's fill
/// is tested offline.</summary>
public static class HistoryView
{
    /// <summary>The most points one chart can hold: every source and the live price.</summary>
    public static readonly int MaxPoints = ExchangeRules.HourlyCloses + ExchangeRules.DailyCloses + ExchangeRules.WeeklyCloses +
                                           ExchangeRules.LifetimeCapacity + ExchangeRules.MaxPastSteps + 2;

    /// <summary>The x axis's unit: hours, days or weeks before now, or calendar years for <see cref="ChartRange.All"/>.</summary>
    public static double UnitSeconds(ChartRange range) => range switch
    {
        ChartRange.Hours => ExchangeRules.HourSeconds,
        ChartRange.Days => ExchangeRules.DaySeconds,
        ChartRange.Weeks => ExchangeRules.WeekSeconds,
        _ => ExchangeRules.YearSeconds
    };

    /// <summary>How far back a range reaches, in game seconds.</summary>
    public static double WindowSeconds(ChartRange range) => range switch
    {
        ChartRange.Hours => 3 * ExchangeRules.DaySeconds,
        ChartRange.Days => ExchangeRules.DailyDays * ExchangeRules.DaySeconds,
        ChartRange.Weeks => ExchangeRules.WeeklyWeeks * ExchangeRules.WeekSeconds,
        _ => double.PositiveInfinity
    };

    /// <summary>An x position for a moment: units before now, or the calendar year (with its fraction) for All.</summary>
    public static double X(ChartRange range, double seconds, double nowSeconds) =>
        range == ChartRange.All ? seconds / ExchangeRules.YearSeconds : (seconds - nowSeconds) / UnitSeconds(range);

    private enum Kind { Closes, Lifetime, Past }
    private readonly struct Source
    {
        public readonly Kind Kind;
        public readonly CloseSeries? Closes;
        public readonly double Bucket;
        public readonly LifetimeSeries? Life;
        public readonly PastSeries? Past;
        public Source(CloseSeries closes, double bucket) { Kind = Kind.Closes; Closes = closes; Bucket = bucket; Life = null; Past = null; }
        public Source(LifetimeSeries life) { Kind = Kind.Lifetime; Closes = null; Bucket = 0; Life = life; Past = null; }
        public Source(PastSeries past) { Kind = Kind.Past; Closes = null; Bucket = 0; Life = null; Past = past; }
        public int Count => Kind == Kind.Closes ? Closes!.Count : Kind == Kind.Lifetime ? (Life!.Foreign ? 0 : Life.Count) : Past!.Count;
        /// <summary>A close belongs to the end of its bucket; a lifetime or past point to the start of its month.</summary>
        public double Time(int i) => Kind == Kind.Closes ? (Closes!.FirstBucket + i + 1) * Bucket
            : GameClock.MonthStart(Kind == Kind.Lifetime ? Life!.MonthAt(i) : Past!.Months[i]);
        public double Ln(int i) => Kind == Kind.Closes ? Closes![i] : Kind == Kind.Lifetime ? Life![i] : Past!.Ln[i];
    }

    /// <summary>Fills <paramref name="x"/> and <paramref name="y"/> with a range's points, then the live price last. The
    /// finest history wins: a coarser source (weekly closes, the lifetime points, then the generated past) fills in only
    /// before the finer ones begin, so a company new to a save still shows its past. Points before the company's listing
    /// month are left out. All shows prices as they were, however small; the other ranges as the panel shows prices. A
    /// short buffer keeps the newest points. Returns the number of points.</summary>
    public static int Fill(ChartRange range, PriceHistory history, PastSeries? past, long? listingMonth, long clock, double nowLn, double[] x, double[] y)
    {
        int room = Math.Min(x.Length, y.Length) - 1;
        if (room < 0) return 0;
        double now = clock * ExchangeRules.StepSeconds;
        double lower = Math.Max(now - WindowSeconds(range), listingMonth is long listing ? GameClock.MonthStart(listing) : double.NegativeInfinity);
        var sources = new Source[5];
        int count = 0;
        if (range == ChartRange.Hours) sources[count++] = new Source(history.Hourly, ExchangeRules.HourSeconds);
        if (range <= ChartRange.Days) sources[count++] = new Source(history.Daily, ExchangeRules.DaySeconds);
        if (range <= ChartRange.Weeks) sources[count++] = new Source(history.Weekly, ExchangeRules.WeekSeconds);
        sources[count++] = new Source(history.Lifetime);
        if (past != null) sources[count++] = new Source(past);

        // Each source, finest first, covers what lies before the finer ones' first point.
        var cutoff = new double[count];
        double before = double.PositiveInfinity;
        int total = 0;
        for (int s = 0; s < count; s++)
        {
            cutoff[s] = Math.Min(before, now);
            var src = sources[s];
            for (int i = 0; i < src.Count; i++)
            {
                double t = src.Time(i);
                if (t < lower || t >= cutoff[s]) continue;
                if (t < before) before = t;
                total++;
            }
        }
        int skip = Math.Max(0, total - room), n = 0, seen = 0;
        for (int s = count - 1; s >= 0; s--)
        {
            var src = sources[s];
            for (int i = 0; i < src.Count; i++)
            {
                double t = src.Time(i);
                if (t < lower || t >= cutoff[s]) continue;
                if (seen++ < skip) continue;
                x[n] = X(range, t, now);
                y[n] = Value(range, src.Ln(i));
                n++;
            }
        }
        x[n] = X(range, now, now); y[n] = Value(range, nowLn);
        return n + 1;
    }

    private static double Value(ChartRange range, double ln) => range == ChartRange.All ? Math.Exp(Math.Min(Math.Log(ExchangeRules.MaxPrice), ln)) : ExchangeRules.Price(ln);
}
