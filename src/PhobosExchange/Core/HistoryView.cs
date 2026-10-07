using System;

namespace PhobosExchange.Core;

/// <summary>The points a price chart draws, oldest first (0.2.1): game-free so the panel's fill is tested offline.</summary>
public static class HistoryView
{
    /// <summary>The most points one chart can hold: the largest series of closes and the live price.</summary>
    public static readonly int MaxPoints = Math.Max(ExchangeRules.HourlyCloses, Math.Max(ExchangeRules.DailyCloses, ExchangeRules.WeeklyCloses)) + 1;

    /// <summary>Fills <paramref name="x"/> (time before now, in buckets of <paramref name="bucketSeconds"/>) and
    /// <paramref name="y"/> (prices) from <paramref name="closes"/>, then the live price at 0. A close belongs to the end
    /// of its bucket, so the newest sits less than one bucket before now. A short buffer keeps the newest closes.
    /// Returns the number of points.</summary>
    public static int Fill(CloseSeries closes, double bucketSeconds, long clock, double nowLn, double[] x, double[] y)
    {
        int room = Math.Min(x.Length, y.Length) - 1;
        if (room < 0) return 0;
        double nowBuckets = clock * ExchangeRules.StepSeconds / bucketSeconds;
        int n = 0;
        for (int i = Math.Max(0, closes.Count - room); i < closes.Count; i++)
        {
            x[n] = closes.FirstBucket + i + 1 - nowBuckets;
            y[n] = ExchangeRules.Price(closes[i]);
            n++;
        }
        x[n] = 0; y[n] = ExchangeRules.Price(nowLn);
        return n + 1;
    }
}
