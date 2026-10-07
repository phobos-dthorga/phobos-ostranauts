using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework;

namespace PhobosExchange.Core;

/// <summary>What a service watches while the market steps: alerts, reports, summaries. Called without allocation.</summary>
public interface IMarketObserver
{
    /// <summary>After every step, fine (one minute) or coarse (a catch-up over a long gap).</summary>
    void Stepped(MarketModel model, long step, bool fine);
    /// <summary>After an hour closes during fine steps.</summary>
    void HourClosed(MarketModel model, long hour);
}

/// <summary>The parts a log price is made of, for explaining a move by its largest cause.</summary>
public enum Cause { Market = 0, Sector = 1, Company = 2, Noise = 3, Trading = 4, Drift = 5, News = 6, Driver = 7 }

/// <summary>The market (Phobos Exchange 0.1.0; design record <c>docs/development/share-market-and-charts-design.md</c>).
/// <para>log price = base + drift·years + market phase·followsMarket + sector phase·followsSector + company phase
/// + drivers + noise + the player's fading impact.</para>
/// Every random term steps exactly on a fixed grid of 60 game seconds with stable noise, so within the last three days a
/// price at a moment is the same whether the player watched or skipped, and a reload never rerolls it. Any gap is caught
/// up within a fixed number of steps: month-aligned steps (at most 240, 0.3.0) for anything older than two years, weekly
/// steps to 120 days, daily steps to three days, then minute steps (owner requirement, 7 October 2026). The step loop
/// allocates nothing and touches no game state; services act on what it reports through <see cref="IMarketObserver"/>.
/// The history before the save's own is drawn separately (<see cref="PastPrices"/>) and never moves a live price.</summary>
public sealed class MarketModel
{
    private const int StreamSlow = 0, StreamGap = 1, StreamVol = 2, StreamNoise = 3, StreamJumpU = 4, StreamJumpZ = 5;
    private const int Coarse = 16, Init = 32;

    public readonly ExchangePack Pack;
    public readonly ExchangeRecord Record;
    public readonly int Count;
    public readonly string[] Ids;
    public readonly CompanyEntry[] Entries;
    public readonly CompanyState[] States;
    /// <summary>Each company's log price at the last step.</summary>
    public readonly double[] LnPrice;
    private readonly PriceHistory[] histories;
    private readonly NewsState[] news;
    private readonly ulong[] keys;
    private readonly int[] sectorOf;
    private readonly string[] sectorIds;
    private readonly TrendState[] sectorStates;
    private readonly TrendKernel[] sectorKernels;
    private readonly ulong[] sectorKeys;
    private readonly TrendKernel marketKernel;
    private readonly ulong marketKey;
    private readonly TrendKernel[] trendKernels;
    private readonly OuKernel[] volKernels, noiseKernels;
    private readonly OuKernel impactKernel, driverKernel;
    private readonly double[] sigma, eta, volNoise, jumpRate, driftPerSecond, newsFade;
    private readonly double[][] targets;
    private TrendState market = new();
    /// <summary>The first step of the month after the last one sampled into the lifetime points.</summary>
    private long monthBoundary = long.MinValue;
    private PastPrices? pastPrices;
    private readonly PastSeries?[] pasts;

    public MarketModel(ExchangePack pack, ExchangeRecord record)
    {
        Pack = pack ?? throw new ArgumentNullException(nameof(pack));
        Record = record ?? throw new ArgumentNullException(nameof(record));
        Ids = pack.companies.Keys.Where(id => !record.Unreadable.Contains(id)).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        Count = Ids.Length;
        Entries = Ids.Select(id => pack.companies[id]).ToArray();
        sectorIds = pack.sectors.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        sectorKernels = sectorIds.Select(id => ExchangeSchema.Kernel(pack.sectors[id].trend)).ToArray();
        sectorKeys = sectorIds.Select(id => StableNoise.Fnv64("sector:" + id)).ToArray();
        sectorStates = new TrendState[sectorIds.Length];
        marketKernel = ExchangeSchema.Kernel(pack.market.trend);
        marketKey = StableNoise.Fnv64("market");
        States = new CompanyState[Count];
        LnPrice = new double[Count];
        histories = new PriceHistory[Count];
        news = new NewsState[Count];
        keys = new ulong[Count];
        sectorOf = new int[Count];
        trendKernels = new TrendKernel[Count];
        volKernels = new OuKernel[Count];
        noiseKernels = new OuKernel[Count];
        sigma = new double[Count]; eta = new double[Count]; volNoise = new double[Count]; jumpRate = new double[Count]; driftPerSecond = new double[Count]; newsFade = new double[Count];
        targets = new double[Count][];
        pasts = new PastSeries?[Count];
        impactKernel = new OuKernel(pack.market.impactHalfLifeDays * ExchangeRules.DaySeconds);
        driverKernel = new OuKernel(ExchangeRules.DriverHalfLifeDays * ExchangeRules.DaySeconds);
        for (int i = 0; i < Count; i++)
        {
            var c = Entries[i];
            keys[i] = StableNoise.Fnv64("company:" + Ids[i]);
            sectorOf[i] = Array.IndexOf(sectorIds, c.sector);
            trendKernels[i] = ExchangeSchema.Kernel(c.trend);
            volKernels[i] = new OuKernel(ExchangeRules.VolHalfLifeDays * ExchangeRules.DaySeconds);
            noiseKernels[i] = new OuKernel(c.noiseHalfLifeYears * ExchangeRules.YearSeconds);
            sigma[i] = c.volatility / Math.Sqrt(ExchangeRules.DaySeconds);
            eta[i] = c.volOfVol;
            // The log-volatility's noise, scaled so its long-run spread is volOfVol.
            volNoise[i] = c.volOfVol * Math.Sqrt(2 * volKernels[i].K);
            jumpRate[i] = c.jumpsPerYear / ExchangeRules.YearSeconds;
            driftPerSecond[i] = c.drift / ExchangeRules.YearSeconds;
            newsFade[i] = Math.Log(2) / (c.newsFadeDays * ExchangeRules.DaySeconds);
            histories[i] = record.History(Ids[i]);
            news[i] = record.NewsFor(Ids[i]);
        }
    }

    public long Clock => Record.Clock ?? 0;
    public PriceHistory History(int i) => histories[i];
    public int IndexOf(string id) => Array.IndexOf(Ids, id);
    public int IndexOfTicker(string ticker)
    {
        for (int i = 0; i < Count; i++) if (string.Equals(Entries[i].ticker, ticker, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }
    public double Price(int i) => ExchangeRules.Price(LnPrice[i]);
    /// <summary>Whether <see cref="Start"/> has run; driver readings before then are ignored.</summary>
    public bool Started { get; private set; }

    /// <summary>Opens a save's market (0.2.1): starts it, then reads every driver through <paramref name="factor"/> (the
    /// factor the station's demand gives, or null for no reading). A new market's drivers start at their readings with the
    /// base moved to match, so it opens at the pack's prices and does not drift over its first day while the drivers settle.
    /// A save whose drivers followed station stock (before 0.5.2) is settled the same way once, so the change of reading
    /// moves no price.</summary>
    public static MarketModel Open(ExchangePack pack, ExchangeRecord record, long now, ulong seed, Func<DriverEntry, double?> factor, IMarketObserver? observer = null)
    {
        bool fresh = record.Clock == null;
        var model = new MarketModel(pack, record);
        model.Start(now, seed, observer);
        model.ReadFactors(factor);
        if (fresh || record.DriverBasis < DriverReading.DemandBasis) model.SettleDrivers();
        record.DriverBasis = Math.Max(record.DriverBasis, DriverReading.DemandBasis);
        return model;
    }

    /// <summary>Reads every driver's goal from the game's price factors.</summary>
    public void ReadFactors(Func<DriverEntry, double?> factor)
    {
        for (int i = 0; i < Count; i++)
        {
            var drivers = Entries[i].drivers;
            for (int j = 0; j < drivers.Count; j++) SetDriverFactor(i, j, factor(drivers[j]));
        }
    }

    /// <summary>Moves every driver straight to its goal and the base the other way, so no price changes.</summary>
    private void SettleDrivers()
    {
        for (int i = 0; i < Count; i++)
        {
            var drivers = States[i].Drivers;
            for (int j = 0; j < drivers.Length; j++)
            {
                States[i].LnBase -= targets[i][j] - drivers[j];
                drivers[j] = targets[i][j];
            }
        }
    }

    /// <summary>Brings the record in line with the pack and the moment: a new record is backfilled two years so the charts
    /// are not empty; companies new to the pack are listed now at the pack's price; a company whose figures changed in the
    /// pack keeps its price (its base moves instead). Then steps the market to <paramref name="now"/>.</summary>
    public int Start(long now, ulong seed, IMarketObserver? observer = null)
    {
        if (!Record.HasSeed) { Record.Seed = seed; Record.HasSeed = true; }
        if (Record.Clock == null) { int filled = Backfill(now); Started = true; return filled; }
        long clock = Record.Clock.Value;
        if (Record.Market == null) { Record.Market = new TrendState(); Stationary(marketKernel, Record.Market, marketKey, clock); }
        market = Record.Market;
        for (int s = 0; s < sectorIds.Length; s++)
        {
            if (!Record.Sectors.TryGetValue(sectorIds[s], out var state))
            {
                Record.Sectors[sectorIds[s]] = state = new TrendState();
                Stationary(sectorKernels[s], state, sectorKeys[s], clock);
            }
            sectorStates[s] = state;
        }
        for (int i = 0; i < Count; i++)
        {
            int drivers = Entries[i].drivers.Count;
            if (!Record.Companies.TryGetValue(Ids[i], out var state))
            {
                // Listed now, in an existing save: its own phases from the stationary state, priced at the pack's price.
                Record.Companies[Ids[i]] = state = NewCompany(i, clock);
                States[i] = state;
                targets[i] = (double[])state.Drivers.Clone();
                LnPrice[i] = Compute(i, clock);
                state.LnBase += Math.Log(Entries[i].price) - LnPrice[i];
                LnPrice[i] = Compute(i, clock);
                state.LastLn = LnPrice[i];
                SeedHistory(i, clock);
                continue;
            }
            States[i] = state;
            if (state.Drivers.Length != drivers) state.Drivers = new double[drivers];
            targets[i] = (double[])state.Drivers.Clone();
            LnPrice[i] = Compute(i, clock);
            // The pack's figures changed since the save: keep the price where it was.
            if (Math.Abs(LnPrice[i] - state.LastLn) > 1e-12)
            {
                state.LnBase += state.LastLn - LnPrice[i];
                LnPrice[i] = Compute(i, clock);
            }
            state.LastLn = LnPrice[i];
            if (histories[i].Hourly.Count == 0) SeedHistory(i, clock);
            SeedLifetime(i, clock);
        }
        Started = true;
        return Advance(now, observer);
    }

    /// <summary>A new market: two years of history drawn from the same model, ending at the pack's prices now.</summary>
    private int Backfill(long now)
    {
        long start = Math.Max(1, now - (long)Math.Ceiling(ExchangeRules.WeeklyWeeks * ExchangeRules.WeekSeconds / ExchangeRules.StepSeconds));
        Record.Clock = start;
        Record.Market = market = new TrendState();
        Stationary(marketKernel, market, marketKey, start);
        for (int s = 0; s < sectorIds.Length; s++)
        {
            Record.Sectors[sectorIds[s]] = sectorStates[s] = new TrendState();
            Stationary(sectorKernels[s], sectorStates[s], sectorKeys[s], start);
        }
        for (int i = 0; i < Count; i++)
        {
            Record.Companies[Ids[i]] = States[i] = NewCompany(i, start);
            targets[i] = (double[])States[i].Drivers.Clone();
            histories[i].Hourly.Clear(); histories[i].Daily.Clear(); histories[i].Weekly.Clear();
            LnPrice[i] = Compute(i, start);
            // The save's own history begins here: the anchor its generated past joins (0.3.0).
            histories[i].Lifetime = new LifetimeSeries();
            histories[i].Lifetime.Open(ExchangeRules.MonthOf(start), LnPrice[i]);
        }
        int steps = Advance(now, null);
        for (int i = 0; i < Count; i++)
        {
            double offset = Math.Log(Entries[i].price) - LnPrice[i];
            States[i].LnBase += offset;
            LnPrice[i] += offset;
            States[i].LastLn = LnPrice[i];
            histories[i].Shift(offset);
        }
        return steps;
    }

    private CompanyState NewCompany(int i, long step)
    {
        var state = new CompanyState { Listed = step, Drivers = new double[Entries[i].drivers.Count] };
        Stationary(trendKernels[i], state.Trend, keys[i], step);
        state.LogVol = eta[i] * StableNoise.Normal(Record.Seed, keys[i], step, Init + StreamVol);
        // The noise starts at its long-run spread, so a new listing is not artificially calm.
        state.Noise = sigma[i] * noiseKernels[i].StationarySd * StableNoise.Normal(Record.Seed, keys[i], step, Init + StreamNoise);
        return state;
    }

    private void Stationary(TrendKernel kernel, TrendState state, ulong key, long step)
    {
        kernel.Stationary(out state.Slow, out state.Gap, StableNoise.Normal(Record.Seed, key, step, Init + StreamSlow), StableNoise.Normal(Record.Seed, key, step, Init + StreamGap));
    }

    private void SeedHistory(int i, long step)
    {
        var h = histories[i];
        h.Hourly.Push(ExchangeRules.HourOf(step), LnPrice[i]);
        h.Daily.Push(ExchangeRules.DayOf(step), LnPrice[i]);
        h.Weekly.Push(ExchangeRules.WeekOf(step), LnPrice[i]);
        h.Lifetime.Open(ExchangeRules.MonthOf(step), LnPrice[i]);
    }

    /// <summary>A record from before 0.3.0 has no lifetime points: they begin at its oldest weekly close. A record played
    /// under an older version since catches up from the weekly closes newer than its last point. Idempotent.</summary>
    private void SeedLifetime(int i, long clock)
    {
        var h = histories[i];
        var life = h.Lifetime;
        if (life.Foreign) return;
        if (life.Count == 0)
        {
            if (h.Weekly.Count > 0) life.Open(GameClock.MonthIndex((h.Weekly.FirstBucket + 1) * ExchangeRules.WeekSeconds), h.Weekly[0]);
            else life.Open(ExchangeRules.MonthOf(clock), LnPrice[i]);
        }
        for (int k = 0; k < h.Weekly.Count; k++)
            life.Sample(GameClock.MonthIndex((h.Weekly.FirstBucket + k + 1) * ExchangeRules.WeekSeconds), h.Weekly[k]);
    }

    /// <summary>The generated past before company <paramref name="i"/>'s own history (0.3.0): drawn on first use from the
    /// pack and the save's seed, kept until the model is replaced, never saved. Empty without a listing year or lifetime
    /// points.</summary>
    public PastSeries Past(int i)
    {
        if (pasts[i] is PastSeries cached) return cached;
        var life = histories[i].Lifetime;
        if (life.Foreign || life.Count == 0 || ExchangeSchema.ListingMonth(Entries[i]) is not long listing || listing >= life.MonthAt(0)) return pasts[i] = PastSeries.None;
        if (pastPrices == null || pastPrices.Horizon < life.MonthAt(0))
        {
            long horizon = 0;
            for (int k = 0; k < Count; k++) if (histories[k].Lifetime.Count > 0) horizon = Math.Max(horizon, histories[k].Lifetime.MonthAt(0));
            pastPrices = new PastPrices(Pack, Record.Seed, horizon);
            Array.Clear(pasts, 0, Count);
        }
        return pasts[i] = pastPrices.For(Ids[i], life.MonthAt(0), life[0]);
    }

    /// <summary>Whether <see cref="Past"/> has been drawn for this company already.</summary>
    public bool HasPast(int i) => pasts[i] != null;

    /// <summary>The generated past's points held in memory, for the footprint.</summary>
    public int PastPoints { get { int n = 0; foreach (var p in pasts) n += p?.Count ?? 0; return n; } }

    /// <summary>Steps the market to <paramref name="target"/>. Returns the number of steps taken, never more than
    /// <see cref="MaxStepsPerAdvance"/> whatever the gap. The clock never goes backwards.</summary>
    public int Advance(long target, IMarketObserver? observer)
    {
        long last = Clock;
        if (target <= last) return 0;
        int steps = 0;
        long fineStart = target - ExchangeRules.FineSteps;
        if (last < fineStart)
        {
            long dailyStart = target - (long)Math.Ceiling(ExchangeRules.DailyDays * ExchangeRules.DaySeconds / ExchangeRules.StepSeconds);
            long weeklyStart = target - (long)Math.Ceiling(ExchangeRules.WeeklyWeeks * ExchangeRules.WeekSeconds / ExchangeRules.StepSeconds);
            if (last < weeklyStart)
            {
                // Older than two years (0.3.0): month-aligned steps every 1, 2, 4 … months, at most LongSteps of them,
                // feeding only the lifetime points; the weekly, daily and minute steps that follow refill the rest.
                long fromMonth = ExchangeRules.MonthOf(last), toMonth = ExchangeRules.MonthOf(weeklyStart);
                long stride = 1;
                while ((toMonth - fromMonth) / stride > ExchangeRules.LongSteps - 1) stride *= 2;
                for (long m = (fromMonth / stride + 1) * stride; m <= toMonth; m += stride)
                {
                    long at = ExchangeRules.StepAtOrAfter(GameClock.MonthStart(m));
                    if (at >= weeklyStart) break;
                    if (at <= last) continue;
                    CoarseStep(last, at, observer, true); last = at; steps++;
                }
                CoarseStep(last, weeklyStart, observer, true); last = weeklyStart; steps++;
            }
            while (last < dailyStart)
            {
                long next = Math.Min(dailyStart, ExchangeRules.StepAtOrAfter((ExchangeRules.WeekOf(last) + 1) * ExchangeRules.WeekSeconds));
                if (next <= last) next = last + 1;
                CoarseStep(last, next, observer); last = next; steps++;
            }
            while (last < fineStart)
            {
                long next = Math.Min(fineStart, ExchangeRules.StepAtOrAfter((ExchangeRules.DayOf(last) + 1) * ExchangeRules.DaySeconds));
                if (next <= last) next = last + 1;
                CoarseStep(last, next, observer); last = next; steps++;
            }
        }
        while (last < target) { FineStep(last, last + 1, observer); last++; steps++; }
        return steps;
    }

    /// <summary>The most steps one <see cref="Advance"/> can take, for any gap.</summary>
    public static int MaxStepsPerAdvance =>
        ExchangeRules.LongSteps + 1 + ExchangeRules.WeeklyWeeks + (int)Math.Ceiling(ExchangeRules.DailyDays) + 2 + (int)ExchangeRules.FineSteps;

    private void FineStep(long from, long step, IMarketObserver? observer)
    {
        const double dt = ExchangeRules.StepSeconds;
        ulong seed = Record.Seed;
        marketKernel.Step(ref market.Slow, ref market.Gap, dt, StableNoise.Normal(seed, marketKey, step, StreamSlow), StableNoise.Normal(seed, marketKey, step, StreamGap));
        for (int s = 0; s < sectorStates.Length; s++)
            sectorKernels[s].Step(ref sectorStates[s].Slow, ref sectorStates[s].Gap, dt, StableNoise.Normal(seed, sectorKeys[s], step, StreamSlow), StableNoise.Normal(seed, sectorKeys[s], step, StreamGap));
        driverKernel.Factors(dt, out double ae, out _);
        impactKernel.Factors(dt, out double ai, out _);
        for (int i = 0; i < Count; i++)
        {
            var st = States[i];
            ulong key = keys[i];
            trendKernels[i].Step(ref st.Trend.Slow, ref st.Trend.Gap, dt, StableNoise.Normal(seed, key, step, StreamSlow), StableNoise.Normal(seed, key, step, StreamGap));
            volKernels[i].Factors(dt, out double ah, out double ch);
            st.LogVol = ah * st.LogVol + volNoise[i] * ch * StableNoise.Normal(seed, key, step, StreamVol);
            double vol = sigma[i] * Math.Exp(st.LogVol - eta[i] * eta[i]);
            noiseKernels[i].Factors(dt, out double aw, out double cw);
            st.Noise = aw * st.Noise + vol * cw * StableNoise.Normal(seed, key, step, StreamNoise);
            if (jumpRate[i] > 0 && StableNoise.Uniform(seed, key, step, StreamJumpU) < jumpRate[i] * dt)
                st.Noise += Entries[i].jumpSize * StableNoise.Normal(seed, key, step, StreamJumpZ);
            Relax(i, ae);
            st.Impact *= ai;
            LnPrice[i] = Compute(i, step);
        }
        Closed(from, step, true, observer);
    }

    private void CoarseStep(long from, long to, IMarketObserver? observer, bool longSegment = false)
    {
        double dt = (to - from) * ExchangeRules.StepSeconds;
        ulong seed = Record.Seed;
        marketKernel.Step(ref market.Slow, ref market.Gap, dt, StableNoise.Normal(seed, marketKey, to, Coarse + StreamSlow), StableNoise.Normal(seed, marketKey, to, Coarse + StreamGap));
        for (int s = 0; s < sectorStates.Length; s++)
            sectorKernels[s].Step(ref sectorStates[s].Slow, ref sectorStates[s].Gap, dt, StableNoise.Normal(seed, sectorKeys[s], to, Coarse + StreamSlow), StableNoise.Normal(seed, sectorKeys[s], to, Coarse + StreamGap));
        driverKernel.Factors(dt, out double ae, out _);
        impactKernel.Factors(dt, out double ai, out _);
        for (int i = 0; i < Count; i++)
        {
            var st = States[i];
            ulong key = keys[i];
            trendKernels[i].Step(ref st.Trend.Slow, ref st.Trend.Gap, dt, StableNoise.Normal(seed, key, to, Coarse + StreamSlow), StableNoise.Normal(seed, key, to, Coarse + StreamGap));
            volKernels[i].Factors(dt, out double ah, out double ch);
            st.LogVol = ah * st.LogVol + volNoise[i] * ch * StableNoise.Normal(seed, key, to, Coarse + StreamVol);
            // Over a long step the noise uses its mean volatility (a labelled approximation) and the jumps their variance.
            st.Noise = noiseKernels[i].LongStep(st.Noise, dt, sigma[i], Entries[i].jumpSize, jumpRate[i],
                StableNoise.Normal(seed, key, to, Coarse + StreamNoise), StableNoise.Normal(seed, key, to, Coarse + StreamJumpZ));
            Relax(i, ae);
            st.Impact *= ai;
            LnPrice[i] = Compute(i, to);
        }
        Closed(from, to, false, observer, longSegment);
    }

    private void Relax(int i, double keep)
    {
        var drivers = States[i].Drivers;
        var goal = targets[i];
        for (int j = 0; j < drivers.Length; j++) drivers[j] = goal[j] + (drivers[j] - goal[j]) * keep;
    }

    /// <summary>History for the buckets a step crossed, then the observer. A step of a long jump feeds only the lifetime
    /// points: the steps after it refill the hourly, daily and weekly closes.</summary>
    private void Closed(long from, long to, bool fine, IMarketObserver? observer, bool longSegment = false)
    {
        Record.Clock = to;
        long hourFrom = ExchangeRules.HourOf(from), hourTo = ExchangeRules.HourOf(to);
        long dayFrom = ExchangeRules.DayOf(from), dayTo = ExchangeRules.DayOf(to);
        long weekFrom = ExchangeRules.WeekOf(from), weekTo = ExchangeRules.WeekOf(to);
        // A new month (0.3.0): its first price joins the lifetime points.
        long month = long.MinValue;
        if (to >= monthBoundary)
        {
            month = ExchangeRules.MonthOf(to);
            monthBoundary = ExchangeRules.StepAtOrAfter(GameClock.MonthStart(month + 1));
        }
        for (int i = 0; i < Count; i++)
        {
            States[i].LastLn = LnPrice[i];
            var h = histories[i];
            if (!longSegment)
            {
                if (hourTo != hourFrom) h.Hourly.Push(hourTo - 1, LnPrice[i]);
                if (dayTo != dayFrom) h.Daily.Push(dayTo - 1, LnPrice[i]);
                if (weekTo != weekFrom) h.Weekly.Push(weekTo - 1, LnPrice[i]);
            }
            if (month != long.MinValue) h.Lifetime.Sample(month, LnPrice[i]);
        }
        if (observer == null) return;
        observer.Stepped(this, to, fine);
        if (fine && hourTo != hourFrom) observer.HourClosed(this, hourTo - 1);
    }

    /// <summary>The log price from the parts.</summary>
    private double Compute(int i, long step)
    {
        double sum = 0;
        for (int k = 0; k < (int)Cause.Driver; k++) sum += Part(i, (Cause)k, step);
        var drivers = States[i].Drivers;
        for (int j = 0; j < drivers.Length; j++) sum += drivers[j];
        return States[i].LnBase + sum;
    }

    /// <summary>One part of company <paramref name="i"/>'s log price now (drivers: <see cref="DriverPart"/>).</summary>
    public double Part(int i, Cause cause) => Part(i, cause, Clock);

    private double Part(int i, Cause cause, long step)
    {
        var st = States[i];
        var c = Entries[i];
        return cause switch
        {
            Cause.Market => c.followsMarket * marketKernel.Level(market.Gap),
            Cause.Sector => sectorOf[i] < 0 ? 0 : c.followsSector * sectorKernels[sectorOf[i]].Level(sectorStates[sectorOf[i]].Gap),
            Cause.Company => trendKernels[i].Level(st.Trend.Gap),
            Cause.Noise => st.Noise,
            Cause.Trading => st.Impact,
            Cause.Drift => driftPerSecond[i] * (step - st.Listed) * ExchangeRules.StepSeconds,
            Cause.News => NewsPart(i, step),
            _ => 0
        };
    }

    public int DriverCount(int i) => States[i].Drivers.Length;
    public double DriverPart(int i, int j) => States[i].Drivers[j];

    /// <summary>How fast a phase is carrying the price, in log units per game week: the market's, the sector's or the
    /// company's own share.</summary>
    public double Speed(int i, Cause cause)
    {
        var c = Entries[i];
        double perSecond = cause switch
        {
            Cause.Market => c.followsMarket * marketKernel.Speed(market.Slow, market.Gap),
            Cause.Sector => sectorOf[i] < 0 ? 0 : c.followsSector * sectorKernels[sectorOf[i]].Speed(sectorStates[sectorOf[i]].Slow, sectorStates[sectorOf[i]].Gap),
            Cause.Company => trendKernels[i].Speed(States[i].Trend.Slow, States[i].Trend.Gap),
            _ => 0
        };
        return perSecond * ExchangeRules.WeekSeconds;
    }

    public double TotalSpeed(int i) => Speed(i, Cause.Market) + Speed(i, Cause.Sector) + Speed(i, Cause.Company);

    /// <summary>The goal a driver relaxes towards, from the game's price factor; null (no reading) holds it where it is.</summary>
    public void SetDriverFactor(int i, int j, double? factor)
    {
        // Before Start there is no state to steer (0.1.0 read the drivers first and failed on every attach).
        if (!Started) return;
        var d = Entries[i].drivers[j];
        if (factor is not double f || double.IsNaN(f) || double.IsInfinity(f)) { targets[i][j] = States[i].Drivers[j]; return; }
        double goal = d.weight * Math.Log(Math.Min(ExchangeRules.MaxFactor, Math.Max(ExchangeRules.MinFactor, f)));
        targets[i][j] = Math.Max(-d.limit, Math.Min(d.limit, goal));
    }

    public double DriverGoal(int i, int j) => targets[i][j];

    /// <summary>The push of a player's order: added now, fading with the impact half-life.</summary>
    public void Push(int i, double logDelta)
    {
        States[i].Impact += logDelta;
        LnPrice[i] += logDelta;
        States[i].LastLn = LnPrice[i];
    }

    /// <summary>The news part of a company's log price at a step (0.4.0): what news moved for good, and the unwinding part
    /// of every jump so far, fading at the company's half-life. Exact for any step, so a skip lands where watching would.</summary>
    private double NewsPart(int i, long step)
    {
        var n = news[i];
        if (n.Transient == 0) return n.Move;
        long since = Math.Max(0, step - n.TransientStep);
        return n.Move + n.Transient * Math.Exp(-newsFade[i] * since * ExchangeRules.StepSeconds);
    }

    /// <summary>A company's news state (for the service's watermark on story flags).</summary>
    public NewsState News(int i) => news[i];

    /// <summary>A piece of story news breaks (Phobos Exchange 0.4.0), as often as its story sets the flag again. The price
    /// jumps by the entry's move at once. The first time in a save its <c>keeps</c> share stays for good; the rest, and the
    /// whole jump every later time, unwinds over the company's <c>newsFadeDays</c>. Its <c>carry</c> pushes the company's
    /// own trend phase the same way, so the move usually runs on for a few weeks. Returns whether this was the first time.</summary>
    public bool BreakNews(int i, NewsEntry entry)
    {
        var n = news[i];
        bool first = n.Applied.Add(entry.flag);
        double move = Math.Log(1 + entry.move), keep = first ? entry.keeps : 0;
        long now = Clock;
        // The unwinding part as it stands now, then this jump's share of it, from now on.
        n.Transient = NewsPart(i, now) - n.Move + (1 - keep) * move;
        n.TransientStep = now;
        n.Move += keep * move;
        if (entry.carry != 0) States[i].Trend.Slow += trendKernels[i].KickFor(Math.Log(1 + entry.carry));
        LnPrice[i] += move;
        States[i].LastLn = LnPrice[i];
        return first;
    }

    /// <summary>A sudden jump (test command only): added to the noise, which fades it over years.</summary>
    public void Jump(int i, double logDelta)
    {
        States[i].Noise += logDelta;
        LnPrice[i] += logDelta;
        States[i].LastLn = LnPrice[i];
    }
}
