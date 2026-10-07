using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework;

namespace PhobosExchange.Core;

/// <summary>A company's generated past: log prices at the start of each month (or every few months), oldest first, up to
/// the month before its own history begins.</summary>
public sealed class PastSeries
{
    public static readonly PastSeries None = new(Array.Empty<long>(), Array.Empty<double>());
    public readonly long[] Months;
    public readonly double[] Ln;
    public PastSeries(long[] months, double[] ln) { Months = months; Ln = ln; }
    public int Count => Months.Length;
}

/// <summary>The history before a save's own (Phobos Exchange 0.3.0; owner decisions, 7 October 2026): from each
/// company's listing to the anchor where the save's own history begins, drawn from the pack and the save's seed. It is
/// never saved: it is drawn afresh after every load, so later lore from writers or add-ons reaches existing saves, and it
/// only ever feeds the charts and the company page; no live price depends on it.
/// <para>The market's and each sector's phases run monthly from a fixed origin (January of
/// <see cref="ExchangeRules.EarliestYear"/>) with stationary starts, so they are the same whichever companies are present.
/// Each company's own phase and noise start at its listing. The price at month t is the anchor's, moved by the phases
/// and noise since t, by the moves of the history entries between t and the anchor, and by the company's drift run
/// backwards; with an authored listing price a straight-line correction also meets that price at the listing. Every path
/// takes at most <see cref="ExchangeRules.MaxPastSteps"/> steps: a longer span steps every 2, 4 … months.</para></summary>
public sealed class PastPrices
{
    private const int StreamSlow = 0, StreamGap = 1, StreamNoise = 3, StreamJumpZ = 5;
    /// <summary>Stream offsets of their own, so the past never shares a draw with the live market.</summary>
    private const int Past = 48, PastInit = 56;

    public readonly ExchangePack Pack;
    public readonly ulong Seed;
    /// <summary>The fixed origin and the latest month any path reaches, as calendar months.</summary>
    public readonly long Origin, Horizon;
    /// <summary>Months per step: a power of two.</summary>
    public readonly int Stride;
    private readonly long first;
    private double[]? market;
    private readonly Dictionary<string, double[]> sectors = new(StringComparer.Ordinal);
    /// <summary>Steps taken so far, for the checks on the bound.</summary>
    public int Steps { get; private set; }

    public PastPrices(ExchangePack pack, ulong seed, long horizonMonth)
    {
        Pack = pack ?? throw new ArgumentNullException(nameof(pack));
        Seed = seed;
        Origin = ExchangeRules.MonthIndex(ExchangeRules.EarliestYear, 1);
        Horizon = Math.Max(Origin, horizonMonth);
        int stride = 1;
        while ((Horizon - Origin) / stride > ExchangeRules.MaxPastSteps) stride *= 2;
        Stride = stride;
        first = (Origin + stride - 1) / stride * stride;
    }

    private double N(ulong key, long month, int stream) => StableNoise.Normal(Seed, key, month, stream);

    /// <summary>A trend phase's level at every point of the grid, from a stationary start at the origin.</summary>
    private double[] Path(TrendKernel kernel, ulong key)
    {
        int n = (int)((Horizon - first) / Stride) + 1;
        var levels = new double[n];
        kernel.Stationary(out double slow, out double gap, N(key, first, PastInit + StreamSlow), N(key, first, PastInit + StreamGap));
        levels[0] = kernel.Level(gap);
        double dt = Stride * GameClock.MonthSeconds;
        for (int k = 1; k < n; k++)
        {
            long month = first + (long)k * Stride;
            kernel.Step(ref slow, ref gap, dt, N(key, month, Past + StreamSlow), N(key, month, Past + StreamGap));
            levels[k] = kernel.Level(gap);
            Steps++;
        }
        return levels;
    }

    /// <summary>A company's past from its listing up to the month before <paramref name="anchorMonth"/>, joined to the
    /// anchor's log price. Empty for a company with no listing year, or one listed at or after its anchor.</summary>
    public PastSeries For(string id, long anchorMonth, double anchorLn)
    {
        if (!Pack.companies.TryGetValue(id, out var c) || ExchangeSchema.ListingMonth(c) is not long listing) return PastSeries.None;
        if (anchorMonth > Horizon) throw new ArgumentOutOfRangeException(nameof(anchorMonth), "beyond the generator's horizon");
        long start = Math.Max(listing, first);
        long g0 = (start + Stride - 1) / Stride * Stride;
        if (g0 >= anchorMonth) return PastSeries.None;
        int k0 = (int)((g0 - first) / Stride);
        int n = (int)((anchorMonth - 1 - g0) / Stride) + 1;

        market ??= Path(ExchangeSchema.Kernel(Pack.market.trend), StableNoise.Fnv64("market"));
        double[]? sector = null;
        if (Pack.sectors.TryGetValue(c.sector, out var s) && !sectors.TryGetValue(c.sector, out sector))
            sectors[c.sector] = sector = Path(ExchangeSchema.Kernel(s.trend), StableNoise.Fnv64("sector:" + c.sector));

        ulong key = StableNoise.Fnv64("company:" + id);
        var trend = ExchangeSchema.Kernel(c.trend);
        var noiseKernel = new OuKernel(c.noiseHalfLifeYears * ExchangeRules.YearSeconds);
        double sigma = c.volatility / Math.Sqrt(ExchangeRules.DaySeconds), jumpRate = c.jumpsPerYear / ExchangeRules.YearSeconds;
        double dt = Stride * GameClock.MonthSeconds;
        trend.Stationary(out double slow, out double gap, N(key, g0, PastInit + StreamSlow), N(key, g0, PastInit + StreamGap));
        double noise = sigma * noiseKernel.StationarySd * N(key, g0, PastInit + StreamNoise);
        var months = new long[n];
        var x = new double[n];
        for (int k = 0; k < n; k++)
        {
            long month = g0 + (long)k * Stride;
            if (k > 0)
            {
                trend.Step(ref slow, ref gap, dt, N(key, month, Past + StreamSlow), N(key, month, Past + StreamGap));
                noise = noiseKernel.LongStep(noise, dt, sigma, c.jumpSize, jumpRate, N(key, month, Past + StreamNoise), N(key, month, Past + StreamJumpZ));
                Steps++;
            }
            months[k] = month;
            x[k] = c.followsMarket * market[k0 + k] + (sector == null ? 0 : c.followsSector * sector[k0 + k]) + trend.Level(gap) + noise;
        }

        // A history entry's move shows from the month after it; lore and entries outside the span move nothing.
        var moves = ExchangeSchema.Milestones(Pack, id).Where(m => m.Entry.move != null && m.Month >= listing && m.Month < anchorMonth).ToArray();
        double MovesBefore(long month) { double sum = 0; foreach (var m in moves) if (m.Month < month) sum += m.LogMove; return sum; }
        double total = MovesBefore(anchorMonth), driftPerMonth = c.drift / 12;
        var ln = new double[n];
        for (int k = 0; k < n; k++)
            ln[k] = anchorLn + (x[k] - x[n - 1]) + (MovesBefore(months[k]) - total) + driftPerMonth * (months[k] - anchorMonth);
        if (c.listingPrice is double price)
        {
            double miss = Math.Log(price) - ln[0];
            for (int k = 0; k < n; k++) ln[k] += miss * (anchorMonth - months[k]) / (double)(anchorMonth - months[0]);
        }
        return new PastSeries(months, ln);
    }
}
