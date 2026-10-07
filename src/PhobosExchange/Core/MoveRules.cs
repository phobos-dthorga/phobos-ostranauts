using System;
using System.Collections.Generic;

namespace PhobosExchange.Core;

/// <summary>A move or a turn worth telling the player about, with the part of the price that did most of it.</summary>
public struct MoveReport
{
    public int Company;
    /// <summary>True for a phase turning (a rise becoming a fall or the other way); false for a big move over a day.</summary>
    public bool Turn;
    /// <summary>For a move, the change over the day as a share of the price; for a turn, the new phase's speed per week.</summary>
    public double Change;
    public Cause Cause;
    /// <summary>For <see cref="Cause.Driver"/>, which driver.</summary>
    public int Driver;
    public long Hour;
}

/// <summary>Why prices moved (owner direction, 7 October 2026: every big move and every turn of a phase is explained).
/// Keeps each company's parts at every hourly close for a day, in memory only (after a load, reports start once a day
/// has passed). A day's move larger than the pack's <c>moveShare</c> is reported with its largest part in the same
/// direction; a phase moving faster than <c>turnShare</c> a week that has changed direction is reported with the phase
/// that turned it. One report per company per <c>reportCooldownDays</c>. Allocates only when built.</summary>
public sealed class MoveWatch
{
    private const int Hours = 25, Window = 24;
    private static readonly Cause[] Phases = { Cause.Market, Cause.Sector, Cause.Company };
    private readonly int count;
    private readonly double[][][] parts;
    private readonly double[][] prices;
    private readonly long[] lastReport;
    private readonly int[] phase;
    private int filled;
    private long lastHour = long.MinValue;

    public MoveWatch(MarketModel model)
    {
        count = model.Count;
        parts = new double[count][][];
        prices = new double[count][];
        lastReport = new long[count];
        phase = new int[count];
        for (int i = 0; i < count; i++)
        {
            parts[i] = new double[Hours][];
            for (int h = 0; h < Hours; h++) parts[i][h] = new double[(int)Cause.Driver + model.DriverCount(i)];
            prices[i] = new double[Hours];
            lastReport[i] = long.MinValue / 2;
        }
    }

    /// <summary>Takes the hour's snapshot and adds any reports to <paramref name="output"/>.</summary>
    public void HourClosed(MarketModel model, long hour, List<MoveReport> output)
    {
        if (model.Count != count) return;
        if (hour != lastHour + 1) filled = 0;
        lastHour = hour;
        int slot = (int)(((hour % Hours) + Hours) % Hours);
        for (int i = 0; i < count; i++)
        {
            var snap = parts[i][slot];
            for (int k = 0; k < (int)Cause.Driver; k++) snap[k] = model.Part(i, (Cause)k);
            for (int j = (int)Cause.Driver; j < snap.Length; j++) snap[j] = model.DriverPart(i, j - (int)Cause.Driver);
            prices[i][slot] = model.LnPrice[i];
        }
        if (filled < Hours) filled++;
        var market = model.Pack.market;
        double cooldownHours = market.reportCooldownDays * ExchangeRules.DaySeconds / ExchangeRules.HourSeconds;
        double moveLog = Math.Log(1 + market.moveShare), turnLog = Math.Log(1 + market.turnShare);
        int then = (int)((((hour - Window) % Hours) + Hours) % Hours);
        for (int i = 0; i < count; i++)
        {
            double speed = model.TotalSpeed(i);
            int direction = Math.Abs(speed) >= turnLog ? Math.Sign(speed) : 0;
            bool cooled = hour - lastReport[i] >= cooldownHours;
            if (filled > Window && cooled)
            {
                double change = prices[i][slot] - prices[i][then];
                if (Math.Abs(change) >= moveLog)
                {
                    Largest(parts[i][slot], parts[i][then], Math.Sign(change), out var cause, out int driver);
                    output.Add(new MoveReport { Company = i, Turn = false, Change = Math.Exp(change) - 1, Cause = cause, Driver = driver, Hour = hour });
                    lastReport[i] = hour;
                    cooled = false;
                }
            }
            if (direction != 0)
            {
                if (phase[i] != 0 && direction != phase[i] && cooled)
                {
                    var cause = Cause.Market;
                    double best = double.NegativeInfinity;
                    foreach (var k in Phases)
                    {
                        double share = direction * model.Speed(i, k);
                        if (share > best) { best = share; cause = k; }
                    }
                    output.Add(new MoveReport { Company = i, Turn = true, Change = Math.Exp(speed) - 1, Cause = cause, Driver = -1, Hour = hour });
                    lastReport[i] = hour;
                }
                phase[i] = direction;
            }
        }
    }

    /// <summary>Holds back the day's move report for a company whose news the wire has just carried (0.2.0), so the same
    /// move is not reported twice.</summary>
    public void Reported(int company, long hour)
    {
        if (company >= 0 && company < count) lastReport[company] = hour;
    }

    /// <summary>The part that moved most in the move's direction.</summary>
    public static void Largest(double[] now, double[] then, int direction, out Cause cause, out int driver)
    {
        cause = Cause.Noise; driver = -1;
        double best = double.NegativeInfinity;
        for (int k = 0; k < now.Length; k++)
        {
            double moved = direction * (now[k] - then[k]);
            if (moved > best) { best = moved; cause = k < (int)Cause.Driver ? (Cause)k : Cause.Driver; driver = k < (int)Cause.Driver ? -1 : k - (int)Cause.Driver; }
        }
    }

    /// <summary>The companies that moved most between two sets of log prices, largest first (for the away summary).</summary>
    public static int[] Biggest(double[] before, double[] after, int most)
    {
        int n = Math.Min(before.Length, after.Length);
        var order = new int[n];
        for (int i = 0; i < n; i++) order[i] = i;
        Array.Sort(order, (a, b) => Math.Abs(after[b] - before[b]).CompareTo(Math.Abs(after[a] - before[a])) is int c && c != 0 ? c : a.CompareTo(b));
        if (most < n) Array.Resize(ref order, Math.Max(0, most));
        return order;
    }
}
