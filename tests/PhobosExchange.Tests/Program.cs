using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosExchange.Core;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
bool Near(double a, double b, double tolerance) => Math.Abs(a - b) <= tolerance * Math.Max(1, Math.Abs(b));

string repo = AppContext.BaseDirectory;
while (!File.Exists(Path.Combine(repo, "AGENTS.md"))) repo = Path.GetDirectoryName(repo.TrimEnd(Path.DirectorySeparatorChar))!;

// ---- Stable noise ------------------------------------------------------------------------------------------
Check(StableNoise.Hash(1, 2, 3, 4) == StableNoise.Hash(1, 2, 3, 4), "the same inputs always give the same hash");
Check(StableNoise.Hash(1, 2, 3, 4) != StableNoise.Hash(1, 2, 3, 5) && StableNoise.Hash(1, 2, 3, 4) != StableNoise.Hash(1, 2, 4, 4) &&
      StableNoise.Hash(1, 2, 3, 4) != StableNoise.Hash(1, 3, 3, 4) && StableNoise.Hash(1, 2, 3, 4) != StableNoise.Hash(2, 2, 3, 4), "every input changes the hash");
Check(StableNoise.Fnv64("market") == StableNoise.Fnv64("market") && StableNoise.Fnv64("a") != StableNoise.Fnv64("b"), "FNV keys are stable");
Check(Near(StableNoise.InverseNormal(0.975), 1.959963985, 1e-8) && Near(StableNoise.InverseNormal(0.5), 0, 1e-12) &&
      Near(StableNoise.InverseNormal(0.001), -3.090232306, 1e-8) && Near(StableNoise.InverseNormal(0.999), 3.090232306, 1e-8), "the inverse normal matches known quantiles");
{
    const int n = 400000;
    double sum = 0, sq = 0; int tails = 0, outside = 0;
    for (int i = 0; i < n; i++)
    {
        double u = StableNoise.Uniform(7, 11, i, 3);
        if (!(u > 0 && u < 1)) outside++;
        double z = StableNoise.Normal(7, 11, i, 0);
        sum += z; sq += z * z; if (Math.Abs(z) > 3) tails++;
    }
    Check(outside == 0, "uniforms stay strictly inside (0, 1)");
    double mean = sum / n, variance = sq / n - mean * mean;
    Check(Math.Abs(mean) < 0.01 && Math.Abs(variance - 1) < 0.01, "normal draws have mean 0 and variance 1 (" + mean.ToString("0.0000") + ", " + variance.ToString("0.0000") + ")");
    Check(Math.Abs(tails / (double)n - 0.0027) < 0.0006, "normal draws have the right tails beyond 3 sd (" + (tails / (double)n).ToString("0.0000") + ")");
}

// ---- Exact process maths -----------------------------------------------------------------------------------
foreach (double x in new[] { 1e-9, 1e-6, 1e-3, 0.0999, 0.1, 0.5, 3, 40 })
    Check(Near(ExactMath.Phi(x), x > 1e-4 ? (1 - Math.Exp(-x)) / x : 1 - x / 2 + x * x / 6, 1e-9), "φ is accurate at " + x);
foreach (var (a, b) in new[] { (0.0499, 0.0499 * 0.25), (0.04, 0.01), (0.1, 0.03), (2.0, 0.5) })
{
    double series = ExactMath.PhiCombo(a, b), direct = ExactMath.Phi(2 * a) + ExactMath.Phi(2 * b) - 2 * ExactMath.Phi(a + b);
    Check(Near(series, direct, 1e-6), "the combined φ matches its direct form at " + a + ", " + b);
}
Check(Near(ExactMath.PhiCombo(1e-5, 4e-6), 1e-5 * (1e-5 - 4e-6) * (1e-5 - 4e-6) / 3 / 1e-5, 1e-3), "the combined φ starts at (a−b)²/3, without cancellation");
{
    var kernel = TrendKernel.FromWeeks(6, 1.5, 0.15);
    double slow = 0, gap = 0;
    kernel.Step(ref slow, ref gap, 60, 1, 1);
    Check(!double.IsNaN(slow) && !double.IsNaN(gap), "a one-minute step has a real covariance (no cancellation to a negative variance)");
    const int n = 60000;
    double sq = 0, change = 0, changeSq = 0, year = ExchangeRules.YearSeconds;
    for (int i = 0; i < n; i++)
    {
        kernel.Stationary(out double s, out double g, StableNoise.Normal(1, 9, i, 0), StableNoise.Normal(1, 9, i, 1));
        double before = kernel.Level(g);
        sq += before * before;
        kernel.Step(ref s, ref g, year, StableNoise.Normal(1, 9, i, 2), StableNoise.Normal(1, 9, i, 3));
        double d = kernel.Level(g) - before;
        change += d; changeSq += d * d;
    }
    Check(Near(Math.Sqrt(sq / n), 0.15, 0.02), "a trend phase's stationary spread is its sd");
    Check(Near(changeSq / n, kernel.VarianceOfChange(year), 0.03), "a year's exact step has the variance the guard uses");
    Check(Near(kernel.VarianceOfChange(1e12), 2 * 0.15 * 0.15, 1e-6) && kernel.VarianceOfChange(0) < 1e-12, "the change variance runs from 0 to twice the stationary variance");
    // Momentum: over a few days the phase keeps its direction far more often than chance.
    int kept = 0, total = 0;
    for (int i = 0; i < 2000; i++)
    {
        kernel.Stationary(out double s, out double g, StableNoise.Normal(2, 9, i, 0), StableNoise.Normal(2, 9, i, 1));
        double v0 = kernel.Speed(s, g);
        kernel.Step(ref s, ref g, 3 * ExchangeRules.DaySeconds, StableNoise.Normal(2, 9, i, 2), StableNoise.Normal(2, 9, i, 3));
        if (Math.Abs(v0) > 0.01 / ExchangeRules.WeekSeconds) { total++; if (Math.Sign(kernel.Speed(s, g)) == Math.Sign(v0)) kept++; }
    }
    Check(kept > 0.75 * total, "a trend phase keeps its direction over three days (" + kept + " of " + total + ")");
}
{
    var ou = new OuKernel(ExchangeRules.DaySeconds);
    ou.Factors(60, out double a, out double c);
    Check(Near(a, Math.Exp(-Math.Log(2) / ExchangeRules.DaySeconds * 60), 1e-12) && Near(c * c, 60 * ExactMath.Phi(2 * ou.K * 60), 1e-12), "an OU step's factors are exact");
}

// ---- The exchange pack -------------------------------------------------------------------------------------
const string Fixture = """
{
  "schemaVersion": 1, "schema": "exchange",
  "market": {
    "name": "Test Exchange", "commission": 0.004, "minCommission": 20, "impact": 0.8, "impactHalfLifeDays": 1,
    "maxOrderShare": 0.25, "maxHolding": 250000, "trend": { "slowWeeks": 10, "fastWeeks": 2.5, "sd": 0.08 },
    "moveShare": 0.05, "turnShare": 0.02, "reportCooldownDays": 1
  },
  "sectors": {
    "ships": { "name": "Shipbuilding", "trend": { "slowWeeks": 8, "fastWeeks": 2, "sd": 0.08 } },
    "mining": { "name": "Mining", "trend": { "slowWeeks": 8, "fastWeeks": 2, "sd": 0.09 } }
  },
  "companies": {
    "keel": { "ticker": "KEEL", "name": "Keel Yards", "profile": "Builds hulls for the inner system.", "sector": "ships", "price": 42,
      "dailyVolume": 20000, "volatility": 0.008, "volOfVol": 0.3, "noiseHalfLifeYears": 6, "drift": 0.04, "jumpsPerYear": 3, "jumpSize": 0.06,
      "spread": 0.004, "trend": { "slowWeeks": 6, "fastWeeks": 1.5, "sd": 0.14 },
      "drivers": [ { "station": "MTRS", "category": "AnyHull", "weight": 0.5 } ] },
    "lode": { "ticker": "LODE", "name": "Lode Mining", "profile": "Mines the belt.", "sector": "mining", "price": 12.5,
      "dailyVolume": 60000, "volatility": 0.01, "volOfVol": 0.35, "noiseHalfLifeYears": 6, "drift": 0.04, "jumpsPerYear": 4, "jumpSize": 0.08,
      "spread": 0.006, "followsMarket": 0.8, "trend": { "slowWeeks": 6, "fastWeeks": 1.5, "sd": 0.15 },
      "drivers": [ { "station": "BCRS", "category": "AnyOres", "weight": 0.6 }, { "station": "MTRS", "category": "AnyMetal", "weight": -0.3, "limit": 0.08 } ] },
    "glow": { "ticker": "GLOW", "name": "Glow Works", "profile": "Lights for stations.", "sector": "ships", "price": 230,
      "dailyVolume": 4000, "volatility": 0.006, "volOfVol": 0.25, "noiseHalfLifeYears": 8, "drift": 0.035, "jumpsPerYear": 2, "jumpSize": 0.05,
      "spread": 0.003, "followsSector": 0.6, "trend": { "slowWeeks": 6, "fastWeeks": 1.5, "sd": 0.12 } }
  }
}
""";
ExchangePack LoadPack(string json) => DataPacks.LoadText<ExchangePack>(json, "", ExchangeRules.Owner, ExchangeSchema.Name, ExchangeSchema.Validate);
bool Refused(string json) { try { LoadPack(json); return false; } catch (Exception e) when (e is ArgumentException || e is FormatException || e is InvalidOperationException || e is Newtonsoft.Json.JsonException) { return true; } }
string With(string find, string replace) { Check(Fixture.Contains(find), "fixture has " + find); return Fixture.Replace(find, replace); }

var pack = LoadPack(Fixture);
Check(pack.companies.Count == 3 && pack.sectors.Count == 2 && pack.companies["lode"].drivers.Count == 2 && pack.companies["lode"].drivers[1].limit == 0.08, "the fixture loads");
Check(pack.companies["keel"].drivers[0].limit == 0.12 && pack.companies["keel"].followsMarket == 1, "defaults fill unset fields");
foreach (var pair in pack.companies)
{
    double er = ExchangeSchema.ExpectedReturn(pair.Value, pack.sectors[pair.Value.sector].trend, pack.market.trend);
    Check(er > 0 && er <= ExchangeRules.MaxExpectedReturn, "expected return within the guard: " + pair.Key + " " + er.ToString("0.000"));
}
Check(Refused(With("\"ticker\": \"LODE\"", "\"ticker\": \"KEEL\"")), "a ticker used twice is refused");
Check(Refused(With("\"ticker\": \"LODE\"", "\"ticker\": \"lode\"")), "a lowercase ticker is refused");
Check(Refused(With("\"drift\": 0.04, \"jumpsPerYear\": 3", "\"drift\": 0.2, \"jumpsPerYear\": 3")), "a drift above the cap is refused");
Check(Refused(With("\"trend\": { \"slowWeeks\": 6, \"fastWeeks\": 1.5, \"sd\": 0.14 }", "\"trend\": { \"slowWeeks\": 6, \"fastWeeks\": 1.5, \"sd\": 0.5 }")), "phases strong enough to break the return guard are refused");
Check(Refused(With("\"trend\": { \"slowWeeks\": 6, \"fastWeeks\": 1.5, \"sd\": 0.14 }", "\"trend\": { \"slowWeeks\": 2, \"fastWeeks\": 1.5, \"sd\": 0.14 }")), "a slow half-life too close to the fast one is refused");
Check(Refused(With("\"sector\": \"mining\"", "\"sector\": \"farming\"")), "an unknown sector is refused");
Check(Refused(With("\"category\": \"AnyOres\"", "\"category\": \"Ores\"")), "a category not in the game's form is refused");
Check(Refused(With("\"weight\": 0.6", "\"weight\": 0")), "a driver of no weight is refused");
Check(Refused(With("\"name\": \"Lode Mining\"", "\"name\": \"Lode, Mining\"")), "a name the ledger cannot hold is refused");
Check(Refused(With("\"spread\": 0.006", "\"spread\": 0.006, \"colour\": \"red\"")), "an unknown field is refused");
Check(Refused(With("\"station\": \"BCRS\"", "\"station\": \"bcrs\"")), "a station registration is capitals and digits");

// ---- The model: determinism, step size, reloads ------------------------------------------------------------
const ulong Seed = 0xC0FFEE;
long now = ExchangeRules.StepOf(2.2e10);
var factors = new Dictionary<(string, int), double> { [("keel", 0)] = 1.3, [("lode", 0)] = 0.8, [("lode", 1)] = 1.5 };
MarketModel NewMarket(ExchangePack p, ExchangeRecord r, long start)
{
    var m = new MarketModel(p, r);
    m.Start(start, Seed);
    SetFactors(m);
    return m;
}
void SetFactors(MarketModel m)
{
    for (int i = 0; i < m.Count; i++)
        for (int j = 0; j < m.DriverCount(i); j++)
            m.SetDriverFactor(i, j, factors.TryGetValue((m.Ids[i], j), out double f) ? f : (double?)null);
}
string Fingerprint(MarketModel m) => string.Join(";", Enumerable.Range(0, m.Count).Select(i =>
    BitConverter.DoubleToInt64Bits(m.LnPrice[i]) + ":" + BitConverter.DoubleToInt64Bits(m.States[i].Noise) + ":" + BitConverter.DoubleToInt64Bits(m.States[i].Trend.Gap))) + "@" + m.Clock;

var one = NewMarket(pack, new ExchangeRecord(), now);
Check(one.Clock == now, "a new market is stepped to now");
for (int i = 0; i < one.Count; i++)
    Check(Near(one.Price(i), pack.companies[one.Ids[i]].price, 1e-9), "a new market opens at the pack's prices: " + one.Ids[i]);
Check(one.Ids.SequenceEqual(new[] { "glow", "keel", "lode" }), "companies are stepped in a stable order");
var two = NewMarket(pack, new ExchangeRecord(), now);
Check(Fingerprint(one) == Fingerprint(two), "the same seed backfills the same market");
for (int i = 0; i < one.Count; i++)
{
    var h = one.History(i);
    Check(h.Hourly.Count == ExchangeRules.HourlyCloses && h.Daily.Count == ExchangeRules.DailyCloses && h.Weekly.Count >= 100, "backfill fills the charts: " + one.Ids[i]);
    Check(h.Daily.Count == two.History(i).Daily.Count && Enumerable.Range(0, h.Daily.Count).All(k => h.Daily[k] == two.History(i).Daily[k]), "backfilled history is the same for the same seed");
}
var otherSeed = new MarketModel(pack, new ExchangeRecord());
otherSeed.Start(now, Seed + 1);
Check(otherSeed.History(0).Daily[0] != one.History(0).Daily[0], "another save's market differs");

// 1,440 single steps, one catch-up and random chunks give the same bits.
for (int k = 0; k < 1440; k++) one.Advance(one.Clock + 1, null);
two.Advance(two.Clock + 1440, null);
Check(Fingerprint(one) == Fingerprint(two), "minute by minute and one catch-up of a day give identical prices");
var three = NewMarket(pack, new ExchangeRecord(), now);
var chunks = new Random(5);
while (three.Clock < now + 1440) three.Advance(Math.Min(now + 1440, three.Clock + chunks.Next(1, 200)), null);
Check(Fingerprint(three) == Fingerprint(one), "any chunking gives identical prices");
// Save and load halfway through.
var four = NewMarket(pack, new ExchangeRecord(), now);
four.Advance(now + 700, null);
var reloadedRecord = ExchangeRecord.Decode(four.Record.Encode());
var reloaded = new MarketModel(pack, reloadedRecord);
reloaded.Start(now + 700, Seed);
SetFactors(reloaded);
Check(Enumerable.Range(0, reloaded.Count).All(i => reloaded.LnPrice[i] == four.LnPrice[i]), "a reload resumes at exactly the saved prices");
reloaded.Advance(now + 1440, null);
Check(Fingerprint(reloaded) == Fingerprint(one), "a reload halfway changes nothing that follows");
Check(one.Advance(one.Clock - 10, null) == 0 && Fingerprint(one) == Fingerprint(two), "the clock never steps backwards");

// ---- Opening a save's market (0.2.1: 0.1.0 read the drivers before starting and failed on every attach) ------
double? Factor(DriverEntry d)
{
    foreach (var pair in pack.companies)
        for (int j = 0; j < pair.Value.drivers.Count; j++)
            if (ReferenceEquals(pair.Value.drivers[j], d)) return factors.TryGetValue((pair.Key, j), out double f) ? f : null;
    return null;
}
{
    var early = new MarketModel(pack, new ExchangeRecord());
    early.SetDriverFactor(early.IndexOf("keel"), 0, 1.3);
    early.ReadFactors(Factor);
    Check(!early.Started, "a driver reading before the market starts is ignored, not a failure");
    var opened = MarketModel.Open(pack, new ExchangeRecord(), now, Seed, Factor);
    Check(opened.Started && opened.Clock == now, "opening a new save's market starts it and steps it to now");
    for (int i = 0; i < opened.Count; i++)
        Check(Near(opened.Price(i), pack.companies[opened.Ids[i]].price, 1e-9), "a new market opens at the pack's prices with its drivers read: " + opened.Ids[i]);
    int keel = opened.IndexOf("keel"), lode = opened.IndexOf("lode");
    Check(opened.DriverGoal(keel, 0) != 0 && opened.DriverPart(keel, 0) == opened.DriverGoal(keel, 0) && opened.DriverPart(lode, 1) == opened.DriverGoal(lode, 1),
        "a new market's drivers start at their readings");
    opened.Advance(now + 1440, null);
    Check(Near(opened.DriverPart(keel, 0), opened.DriverGoal(keel, 0), 1e-12), "settled drivers do not drift over the first day");
    var resumed = MarketModel.Open(pack, ExchangeRecord.Decode(opened.Record.Encode()), opened.Clock, Seed, Factor);
    Check(Enumerable.Range(0, resumed.Count).All(i => resumed.LnPrice[i] == opened.LnPrice[i]) && resumed.DriverPart(keel, 0) == opened.DriverPart(keel, 0),
        "opening a saved market resumes at its saved prices and drivers");
}
// The chart's points (0.2.1: the 120-day chart lost its newest 15 closes).
{
    var h = one.History(0);
    var x = new double[HistoryView.MaxPoints]; var y = new double[HistoryView.MaxPoints];
    int n = HistoryView.Fill(ChartRange.Days, h, null, null, one.Clock, one.LnPrice[0], x, y);
    Check(h.Daily.Count == ExchangeRules.DailyCloses && n >= ExchangeRules.DailyCloses + 1 &&
          Enumerable.Range(0, ExchangeRules.DailyCloses).All(k => y[n - 1 - ExchangeRules.DailyCloses + k] == ExchangeRules.Price(h.Daily[k])), "the 120-day chart draws every daily close, then now (" + n + ")");
    Check(y[n - 2] == ExchangeRules.Price(h.Daily[h.Daily.Count - 1]) && x[n - 2] <= 0 && x[n - 2] > -1 && x[n - 1] == 0 && y[n - 1] == one.Price(0),
        "the newest close sits within a day before now, and now is last");
    Check(Enumerable.Range(1, n - 1).All(k => x[k] > x[k - 1]) && x[0] >= -ExchangeRules.DailyDays, "chart points run oldest first, within the range");
    var shortX = new double[11]; var shortY = new double[11];
    int m = HistoryView.Fill(ChartRange.Days, h, null, null, one.Clock, one.LnPrice[0], shortX, shortY);
    Check(m == 11 && shortY[9] == ExchangeRules.Price(h.Daily[h.Daily.Count - 1]), "a short buffer keeps the newest closes");
}

// A change to the pack keeps every price where it was.
{
    var record = ExchangeRecord.Decode(one.Record.Encode());
    var changed = LoadPack(Fixture.Replace("\"sd\": 0.14", "\"sd\": 0.1").Replace("\"followsSector\": 0.6", "\"followsSector\": 0.9")
        .Replace("\"drivers\": [ { \"station\": \"MTRS\", \"category\": \"AnyHull\", \"weight\": 0.5 } ]",
                 "\"drivers\": [ { \"station\": \"MTRS\", \"category\": \"AnyHull\", \"weight\": 0.5 }, { \"station\": \"SVIR\", \"category\": \"AnyHull\", \"weight\": 0.3 } ]"));
    var model = new MarketModel(changed, record);
    model.Start(one.Clock, Seed);
    Check(Enumerable.Range(0, model.Count).All(i => Near(model.LnPrice[i], one.LnPrice[i], 1e-12)), "changed figures in the pack never make a price jump");
    Check(model.DriverCount(model.IndexOf("keel")) == 2, "a company's new driver joins in");
}
// A company added to the pack is listed now at its price; one removed keeps its saved state.
{
    var record = ExchangeRecord.Decode(one.Record.Encode());
    var added = LoadPack(Fixture.Replace("\"companies\": {", """
        "companies": {
            "nova": { "ticker": "NOVA", "name": "Nova Lamps", "profile": "Lamps.", "sector": "ships", "price": 9, "dailyVolume": 9000, "volatility": 0.009,
              "volOfVol": 0.3, "noiseHalfLifeYears": 6, "drift": 0.03, "jumpsPerYear": 2, "jumpSize": 0.05, "spread": 0.005, "trend": { "slowWeeks": 6, "fastWeeks": 1.5, "sd": 0.12 } },
        """));
    var model = new MarketModel(added, record);
    model.Start(one.Clock, Seed);
    int nova = model.IndexOf("nova");
    Check(Near(model.Price(nova), 9, 1e-9) && model.States[nova].Listed == one.Clock && model.History(nova).Hourly.Count == 1, "a newly listed company opens at its price with one point of history");
    var removed = LoadPack(Fixture.Replace("\"glow\": {", "\"zzz-unused\": {").Replace("\"GLOW\"", "\"ZZZ\""));
    var kept = new MarketModel(removed, ExchangeRecord.Decode(one.Record.Encode()));
    kept.Start(one.Clock, Seed);
    Check(kept.Record.Encode().ContainsKey("co.glow") && kept.Record.Encode().ContainsKey("hist.d.glow.0"), "a company the pack no longer lists keeps its saved state and history");
}

// ---- Time jumps of any size (owner requirement) ------------------------------------------------------------
foreach (var (label, seconds) in new[] { ("a minute", 60.0), ("an hour", 3600.0), ("a day", ExchangeRules.DaySeconds), ("three days", 3 * ExchangeRules.DaySeconds),
             ("a month", 30 * ExchangeRules.DaySeconds), ("a year", ExchangeRules.YearSeconds), ("two and a half years", 2.5 * ExchangeRules.YearSeconds),
             ("ten years", 10 * ExchangeRules.YearSeconds), ("fifty years", 50 * ExchangeRules.YearSeconds), ("a thousand years", 1000 * ExchangeRules.YearSeconds) })
{
    var a = NewMarket(pack, new ExchangeRecord(), now);
    var b = NewMarket(pack, new ExchangeRecord(), now);
    long target = now + (long)Math.Ceiling(seconds / ExchangeRules.StepSeconds);
    var watch = Stopwatch.StartNew();
    int steps = a.Advance(target, null);
    watch.Stop();
    b.Advance(target, null);
    Check(steps <= MarketModel.MaxStepsPerAdvance, "a jump of " + label + " takes at most " + MarketModel.MaxStepsPerAdvance + " steps (" + steps + ")");
    Check(a.Clock == target && Fingerprint(a) == Fingerprint(b), "a jump of " + label + " lands at the target, the same each time");
    Check(Enumerable.Range(0, a.Count).All(i => !double.IsNaN(a.LnPrice[i]) && !double.IsInfinity(a.LnPrice[i]) && a.Price(i) >= ExchangeRules.MinPrice && a.Price(i) <= ExchangeRules.MaxPrice),
        "prices stay finite after " + label);
    for (int i = 0; i < a.Count; i++)
    {
        var h = a.History(i);
        Check(h.Hourly.Count <= ExchangeRules.HourlyCloses && h.Daily.Count <= ExchangeRules.DailyCloses && h.Weekly.Count <= ExchangeRules.WeeklyCloses, "history stays within its caps after " + label);
        // 0.3.0: the lifetime points keep the whole span, bounded, starting at the anchor and running forward.
        var life = h.Lifetime;
        Check(life.Count <= ExchangeRules.LifetimeCapacity && life.MonthAt(0) == b.History(i).Lifetime.MonthAt(0) && Enumerable.Range(1, life.Count - 1).All(k => life.MonthAt(k) > life.MonthAt(k - 1)) &&
              life.LastMonth == ExchangeRules.MonthOf(target) && (seconds < 2 * ExchangeRules.YearSeconds || life.Count >= 30), "the lifetime points cover a jump of " + label + " (" + life.Count + ", every " + life.Resolution + " months)");
    }
    Console.WriteLine($"  jump of {label}: {steps} steps, {watch.Elapsed.TotalMilliseconds:0.0} ms");
}

// ---- The saved record --------------------------------------------------------------------------------------
{
    var market = NewMarket(pack, new ExchangeRecord(), now);
    market.Record.Holdings["keel"] = new Holding { Shares = 120, Cost = 5100.5, Realised = -40.25 };
    market.Record.Alerts["lode"] = new Alert { Above = 15, Below = 10 };
    market.Record.TestChanges = 2; market.Record.LastTestStep = now; market.Record.LastTest = "test shock KEEL 10";
    for (int day = 0; day < 400; day++) market.Advance(market.Clock + (long)Math.Ceiling(ExchangeRules.DaySeconds / ExchangeRules.StepSeconds), null);
    var fields = market.Record.Encode();
    var maps = new Dictionary<string, Dictionary<string, string>>();
    Check(new ObjectStateStore(maps, ExchangeRecord.Name, ExchangeRules.Owner, ExchangeRecord.Version).TryWrite(fields), "every key and value passes the save store's rules");
    int size = fields.Sum(p => p.Key.Length + p.Value.Length);
    Console.WriteLine($"  record after 400 game days: {fields.Count} fields, {size} characters");
    Check(size < 16000, "the record stays under 16,000 characters after 400 game days (" + size + ")");
    Check(fields.Values.All(v => v.Length <= ObjectStateStore.MaxValueLength), "no value is longer than the store allows");
    var back = ExchangeRecord.Decode(fields);
    Check(back.Encode().OrderBy(p => p.Key).SequenceEqual(fields.OrderBy(p => p.Key)), "the record round-trips exactly");
    Check(back.Holdings["keel"].Shares == 120 && back.Holdings["keel"].Cost == 5100.5 && back.Holdings["keel"].Realised == -40.25, "holdings round-trip");
    Check(back.Alerts["lode"].Above == 15 && back.Alerts["lode"].Below == 10 && back.TestChanges == 2 && back.LastTest == "test shock KEEL 10", "alerts and the test marker round-trip");
    for (int i = 0; i < market.Count; i++)
        foreach (char res in PriceHistory.Resolutions)
        {
            var saved = market.History(i).Of(res); var loaded = back.History(market.Ids[i]).Of(res);
            Check(saved.Count == loaded.Count && saved.LastBucket == loaded.LastBucket && Enumerable.Range(0, saved.Count).All(k => Math.Abs(saved[k] - loaded[k]) <= 0.00005 + 1e-12),
                "history round-trips to 0.005%: " + market.Ids[i] + " " + res);
        }
    // Fields from a newer version, or that this version cannot read, are kept exactly and block nothing else.
    fields["future.thing"] = "9|later";
    fields["co.glow"] = "2|from a newer version";
    fields["hold.lode"] = "2|newer holding";
    var mixed = ExchangeRecord.Decode(fields);
    Check(mixed.Unreadable.SetEquals(new[] { "glow", "lode" }), "a company whose state or holding cannot be read is set aside");
    var guarded = new MarketModel(pack, mixed);
    guarded.Start(market.Clock, Seed);
    Check(guarded.Count == 1 && guarded.Ids[0] == "keel", "the market steps only what it can read");
    var again = guarded.Record.Encode();
    Check(again["future.thing"] == "9|later" && again["co.glow"] == "2|from a newer version" && again["hold.lode"] == "2|newer holding", "unreadable fields are written back untouched");
}
{
    var series = new CloseSeries(5);
    series.Push(10, 1); series.Push(11, 2); series.Push(11, 2.5); series.Push(9, 7);
    Check(series.Count == 2 && series[1] == 2.5 && series.LastBucket == 11, "a close for the same bucket replaces it; an older bucket is ignored");
    series.Push(14, 3);
    Check(series.Count == 5 && series[2] == 3 && series[4] == 3 && series.FirstBucket == 10, "a gap fills with the new close");
    series.Push(1000000, 4);
    Check(series.Count == 5 && Enumerable.Range(0, 5).All(k => series[k] == 4) && series.LastBucket == 1000000, "a huge gap costs at most the capacity");
    foreach (long v in new long[] { 0, 1, -1, 35, 36, -36, 123456789, -2000000000, 2000000000 })
        Check(CloseSeries.TryBase36(CloseSeries.Base36(v), out long w) && w == v, "base 36 round-trips " + v);
    Check(!CloseSeries.TryBase36("", out _) && !CloseSeries.TryBase36("-", out _) && !CloseSeries.TryBase36("x!", out _) && !CloseSeries.TryBase36("zzzzzzzzzzzzz", out _), "bad base 36 is refused");
    var parts = new Dictionary<int, string> { [0] = "1|5|3FF0000000000000|0;1;2", [2] = "1|8|3FF0000000000000|0" };
    Check(!CloseSeries.TryDecode(parts, 10, out var broken) && broken.Count == 0, "history whose parts do not join starts afresh");
}

// ---- Trading -----------------------------------------------------------------------------------------------
{
    var market = NewMarket(pack, new ExchangeRecord(), now);
    int k = market.IndexOf("keel");
    var c = market.Entries[k]; var m = pack.market;
    Check(Near(TradeRules.ImpactOf(c, m, 4000), 2 * TradeRules.ImpactOf(c, m, 1000), 1e-12), "impact grows with the square root of the order");
    var holding = new Holding();
    double cash = 100000;
    var buy = TradeRules.Buy(c, m, market.LnPrice[k], 1000, holding, cash);
    Check(buy.Ok && buy.PerShare > TradeRules.QuoteOf(market.LnPrice[k], c.spread).Ask && Near(buy.Total, buy.Gross + buy.Commission, 1e-9), "a buy pays the asking price and some of its own push, plus commission");
    TradeRules.Apply(holding, buy); market.Push(k, buy.Impact); cash -= buy.Total;
    var sell = TradeRules.Sell(c, m, market.LnPrice[k], 1000, holding);
    Check(sell.Ok && sell.Total < buy.Total, "selling straight back always loses (" + sell.Total.ToString("0.00") + " for " + buy.Total.ToString("0.00") + ")");
    TradeRules.Apply(holding, sell);
    Check(holding.Shares == 0 && holding.Cost == 0 && Near(holding.Realised, sell.Total - buy.Total, 1e-9), "the loss is booked as realised");
    Check(TradeRules.Buy(c, m, market.LnPrice[k], 0, null, cash).Refusal == Refusal.NoShares, "no shares, no order");
    Check(TradeRules.Buy(c, m, market.LnPrice[k], TradeRules.MaxOrder(c, m) + 1, null, 1e12).Refusal == Refusal.OrderTooLarge, "an order above a quarter of the day's volume is refused");
    Check(TradeRules.Buy(c, m, market.LnPrice[k], 4000, null, 10).Refusal == Refusal.NotEnoughCash, "an order the cash cannot cover is refused");
    Check(TradeRules.Buy(c, m, market.LnPrice[k], 10, new Holding { Shares = 1000000 }, 1e12).Refusal == Refusal.HoldingCap, "an order past the holding cap is refused");
    Check(TradeRules.Sell(c, m, market.LnPrice[k], 5, new Holding { Shares = 4 }).Refusal == Refusal.NotHeld, "selling more than is held is refused");
    var tiny = LoadPack(Fixture.Replace("\"minCommission\": 20", "\"minCommission\": 500"));
    Check(TradeRules.Sell(tiny.companies["lode"], tiny.market, Math.Log(12.5), 1, new Holding { Shares = 1 }).Refusal == Refusal.TooSmall, "a sale worth less than its commission is refused");
    long most = TradeRules.MaxBuy(c, m, market.LnPrice[k], null, 30000);
    Check(most > 0 && TradeRules.Buy(c, m, market.LnPrice[k], most, null, 30000).Ok && !TradeRules.Buy(c, m, market.LnPrice[k], most + 1, null, 30000).Ok, "the most the player can buy is exactly affordable");
    var part = new Holding { Shares = 100, Cost = 1000 };
    TradeRules.Apply(part, new Fill { Buy = false, Shares = 25, Total = 400 });
    Check(part.Shares == 75 && Near(part.Cost, 750, 1e-12) && Near(part.Realised, 150, 1e-12), "a partial sale takes the average cost off the basis");
}

// ---- Alerts, also inside a catch-up ------------------------------------------------------------------------
{
    var alert = new Alert { Above = 10, Below = 5 };
    Check(AlertRules.Check(alert, 7) == 0 && AlertRules.Check(alert, 10) == 1 && alert.Above == null && AlertRules.Check(alert, 11) == 0, "the upper alert fires once and clears");
    Check(AlertRules.Check(alert, 4.9) == -1 && alert.Empty, "the lower alert fires once and clears");
    Check(AlertRules.Valid(12, 10, true) && !AlertRules.Valid(9, 10, true) && AlertRules.Valid(9, 10, false) && !AlertRules.Valid(0, 10, false), "an alert must be on the far side of the price");
    var probe = NewMarket(pack, new ExchangeRecord(), now);
    int k = probe.IndexOf("lode");
    double start = probe.Price(k);
    var peak = new PeakObserver(k, start);
    probe.Advance(now + 4000, peak);
    bool up = peak.High - start >= start - peak.Low;
    Check(up ? peak.High > start : peak.Low < start, "the probe found the price moving within the window");
    var market = NewMarket(pack, new ExchangeRecord(), now);
    var level = up ? new Alert { Above = (start + peak.High) / 2 } : new Alert { Below = (start + peak.Low) / 2 };
    var watcher = new AlertObserver(k, level);
    market.Advance(now + 4000, watcher);
    Check(watcher.Fired == 1 && watcher.Alert.Empty, "an alert crossed inside a catch-up fires exactly once");
}

// ---- Reports: big moves and turns, with their causes -------------------------------------------------------
{
    var market = NewMarket(pack, new ExchangeRecord(), now);
    var reports = new List<MoveReport>();
    var watch = new MoveWatch(market);
    var observer = new ReportObserver(watch, reports);
    market.Advance(market.Clock + 26 * 60, observer);
    reports.Clear();
    int k = market.IndexOf("glow");
    market.Jump(k, Math.Log(1.25));
    market.Advance(market.Clock + 60, observer);
    Check(reports.Any(r => r.Company == k && !r.Turn && r.Change > 0.2 && r.Cause == Cause.Noise), "a sudden 25% jump is reported as a move with its cause");
    market.Advance(market.Clock + (long)(90 * ExchangeRules.DaySeconds / 60), observer);
    var turns = reports.Where(r => r.Turn).ToList();
    Check(turns.Count > 0 && turns.All(r => r.Cause == Cause.Market || r.Cause == Cause.Sector || r.Cause == Cause.Company), "phases turn within 90 days, each named by the phase that turned it (" + turns.Count + ")");
    var byCompany = reports.GroupBy(r => r.Company).Select(g => g.OrderBy(r => r.Hour).ToList()).ToList();
    double cooldown = pack.market.reportCooldownDays * ExchangeRules.DaySeconds / 3600;
    Check(byCompany.All(list => list.Zip(list.Skip(1), (a, b) => b.Hour - a.Hour).All(gap => gap >= cooldown)), "one report per company per cooldown");
}

// ---- Story news and story events (Phobos Exchange 0.2.0) --------------------------------------------------
{
    string withNews = Fixture.Replace("\"drivers\": [ { \"station\": \"MTRS\", \"category\": \"AnyHull\", \"weight\": 0.5 } ] },",
        "\"drivers\": [ { \"station\": \"MTRS\", \"category\": \"AnyHull\", \"weight\": 0.5 } ], \"news\": [ { \"flag\": \"keel-titan-contract\", \"move\": 0.08, \"carry\": 0.04, \"keeps\": 0.5, \"wire\": \"Keel Yards wins the Titan hull contract.\" }, { \"flag\": \"keel-yard-fire\", \"move\": -0.12 } ] },");
    Check(withNews != Fixture, "the fixture takes news entries");
    var newsPack = LoadPack(withNews);
    Check(newsPack.companies["keel"].news.Count == 2 && newsPack.companies["keel"].news[1].wire == null && newsPack.companies["glow"].news.Count == 0, "news loads, with no wire line where none is given");
    string BadNews(string entry) => Fixture.Replace("\"drivers\": [ { \"station\": \"MTRS\", \"category\": \"AnyHull\", \"weight\": 0.5 } ] },",
        "\"drivers\": [ { \"station\": \"MTRS\", \"category\": \"AnyHull\", \"weight\": 0.5 } ], \"news\": [ " + entry + " ] },");
    Check(Refused(BadNews("{ \"flag\": \"Keel Contract\", \"move\": 0.08 }")), "a news flag must be a story id");
    Check(Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.5 }")) && Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": -0.5 }")), "news may move a price at most 30% either way");
    Check(Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.001 }")), "news too small to see is refused");
    Check(Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.05 }, { \"flag\": \"keel-a\", \"move\": 0.06 }")), "one flag names one piece of news for a company");
    Check(Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.05, \"wire\": \"[player] did it\" }")), "a wire line has no placeholders");
    // Variant wire lines (0.5.0, Framework 0.132.0): one of up to eight, each under the same rules.
    var varied = LoadPack(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.05, \"wire\": [ \"Keel Yards wins a hull contract.\", \"Keel Yards signs for new hulls.\" ] }"));
    Check(varied.companies["keel"].news[0].wire!.Count == 2 && varied.companies["keel"].news[0].wire![1] == "Keel Yards signs for new hulls.", "a wire line may be a list of variants");
    Check(Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.05, \"wire\": [ \"Keel Yards wins.\", \"[player] did it\" ] }")), "no variant of a wire line has a placeholder");
    Check(Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.05, \"wire\": [ \"Keel Yards wins.\", \"Keel Yards wins.\" ] }")), "two identical wire variants are refused");
    Check(Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.05, \"wire\": [ ] }")), "an empty list of wire variants is refused");
    Check(Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.05, \"wire\": [ \"Keel Yards wins.\", \"" + new string('x', 301) + "\" ] }")), "every wire variant keeps to 300 characters");
    Check(Phobos.Ostranauts.Framework.Story.TextVariants.Key("News.keel.keel-a", 0) == "News.keel.keel-a" && Phobos.Ostranauts.Framework.Story.TextVariants.Key("News.keel.keel-a", 1) == "News.keel.keel-a.2",
        "the first wire variant keeps its translation key, the second takes .2");

    // Recurring news (0.4.0): a jump at once, a share kept the first time only, the rest unwinding at the company's
    // half-life, and a carry through the company's own trend phase.
    Check(Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.05, \"carry\": -0.02 }")) && Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.05, \"carry\": 0.2 }")),
        "a carry runs the same way as the move, and is bounded");
    Check(Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.05, \"keeps\": 1.5 }")), "a lasting share is 0 to 1");
    Check(Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.3 }")) && !Refused(BadNews("{ \"flag\": \"keel-a\", \"move\": 0.08 }")),
        "a jump whose unwind a player could count on is held to a few percent a week");
    Check(Refused(With("\"spread\": 0.004,", "\"spread\": 0.004, \"newsFadeDays\": 3,")), "a fade shorter than a week is refused");
    var titan = newsPack.companies["keel"].news[0];
    var fire = newsPack.companies["keel"].news[1];
    var market = NewMarket(newsPack, new ExchangeRecord(), now);
    int k = market.IndexOf("keel");
    double before = market.LnPrice[k], m = Math.Log(1.08);
    Check(market.BreakNews(k, titan), "the first breaking is the first");
    Check(Near(Math.Exp(market.LnPrice[k] - before), 1.08, 1e-12) && Near(market.Part(k, Cause.News), m, 1e-12), "news moves the price by its share at once, as its own part of the price");
    Check(Near(market.News(k).Move, 0.5 * m, 1e-12), "the first time, its lasting share stays for good");
    long fade = (long)Math.Round(30 * ExchangeRules.DaySeconds / ExchangeRules.StepSeconds);
    var twin = NewMarket(LoadPack(withNews.Replace("\"carry\": 0.04, ", "")), new ExchangeRecord(), now);
    twin.BreakNews(k, twin.Entries[k].news[0]);
    market.Advance(market.Clock + fade, null);
    twin.Advance(twin.Clock + fade, null);
    Check(Near(market.Part(k, Cause.News), 0.5 * m + 0.25 * m, 1e-6), "the rest unwinds at the company's half-life (" + market.Part(k, Cause.News).ToString("0.00000") + ")");
    var kernel = ExchangeSchema.Kernel(newsPack.companies["keel"].trend);
    double carried = market.Part(k, Cause.Company) - twin.Part(k, Cause.Company);
    Check(carried > 0 && carried < Math.Log(1.04) + 1e-9, "the carry pushes the company's phase the same way (" + carried.ToString("0.0000") + " after a month)");
    var peakA = NewMarket(newsPack, new ExchangeRecord(), now); var peakB = NewMarket(LoadPack(withNews.Replace("\"carry\": 0.04, ", "")), new ExchangeRecord(), now);
    peakA.BreakNews(k, titan); peakB.BreakNews(k, peakB.Entries[k].news[0]);
    long peak = (long)Math.Round(kernel.PeakSeconds / ExchangeRules.StepSeconds);
    peakA.Advance(peakA.Clock + peak, null); peakB.Advance(peakB.Clock + peak, null);
    Check(Near(peakA.Part(k, Cause.Company) - peakB.Part(k, Cause.Company), Math.Log(1.04), 1e-4), "on average the carry reaches its height when the phase says");
    double beforeAgain = market.LnPrice[k], newsBefore = market.Part(k, Cause.News);
    Check(!market.BreakNews(k, titan) && Near(market.LnPrice[k] - beforeAgain, m, 1e-12) && Near(market.News(k).Move, 0.5 * m, 1e-12),
        "the same news breaks again, jumping as far, but keeps nothing more for good");
    Check(Near(market.Part(k, Cause.News), newsBefore + m, 1e-12), "a second breaking adds its whole jump to what is still unwinding");
    market.Advance(market.Clock + 40 * fade, null);
    Check(Near(market.Part(k, Cause.News), 0.5 * m, 1e-9), "in the long run only the first breaking's lasting share remains, so repeated news never compounds");
    // Exact through skips and saves.
    var stepA = NewMarket(newsPack, new ExchangeRecord(), now); var stepB = NewMarket(newsPack, new ExchangeRecord(), now);
    stepA.BreakNews(k, fire); stepB.BreakNews(k, fire);
    for (int s = 0; s < 600; s++) stepA.Advance(stepA.Clock + 1, null);
    stepB.Advance(stepB.Clock + 600, null);
    Check(stepA.Part(k, Cause.News) == stepB.Part(k, Cause.News), "news unwinds to the same bits minute by minute or in one go");
    market.News(k).Watermark = 123456.5;
    var fields = market.Record.Encode();
    Check(fields.TryGetValue("news.keel", out var saved) && saved.StartsWith("1|") && saved.EndsWith("|keel-titan-contract") && fields.ContainsKey("newsfx.keel"),
        "news that has broken is saved under its own key, what is unwinding under another");
    var back = new MarketModel(newsPack, ExchangeRecord.Decode(fields));
    back.Start(market.Clock, Seed);
    Check(back.LnPrice[k] == market.LnPrice[k] && back.Part(k, Cause.News) == market.Part(k, Cause.News) && back.News(k).Watermark == 123456.5 && back.News(k).Applied.Contains("keel-titan-contract"),
        "news survives a save and load exactly");
    var oldRecord = market.Record.Encode();
    oldRecord.Remove("newsfx.keel");
    var fromOld = ExchangeRecord.Decode(oldRecord);
    Check(fromOld.NewsFor("keel").Watermark == null && fromOld.NewsFor("keel").Applied.Contains("keel-titan-contract"), "a 0.2 or 0.3 record reads with no watermark yet, so the service starts one");
    oldRecord.Remove("news.keel");
    Check(ExchangeRecord.Decode(oldRecord).News.Values.All(n => n.Empty && n.EffectsEmpty), "a 0.1.0 record, without news, reads as no news");
    var future = market.Record.Encode();
    future["news.keel"] = "2|from a newer version"; future["newsfx.keel"] = "2|newer";
    var kept = ExchangeRecord.Decode(future).Encode();
    Check(kept["news.keel"] == "2|from a newer version" && kept["newsfx.keel"] == "2|newer", "news from a newer version is kept untouched");
    // The wire carries the news once: the day's move report does not repeat it.
    var reports = new List<MoveReport>();
    var watch = new MoveWatch(market);
    var observer = new ReportObserver(watch, reports);
    market.Advance(market.Clock + 26 * 60, observer);
    reports.Clear();
    market.BreakNews(k, fire);
    watch.Reported(k, ExchangeRules.HourOf(market.Clock));
    market.Advance(market.Clock + 30 * 60, observer);
    Check(!reports.Any(r => r.Company == k && !r.Turn), "news the wire has carried is not reported again as the day's move");
    var plain = new MoveWatch(market);
    var plainReports = new List<MoveReport>();
    var plainObserver = new ReportObserver(plain, plainReports);
    market.Advance(market.Clock + 26 * 60, plainObserver);
    market.BreakNews(k, new NewsEntry { flag = "keel-extra", move = 0.2 });
    market.Advance(market.Clock + 60, plainObserver);
    Check(plainReports.Any(r => r.Company == k && !r.Turn && r.Cause == Cause.News), "a move made by news is put down to the news");
}
{
    // Story ids the exchange sets and starts are valid story ids for every shipped company (at most 48 characters).
    string shippedExchange = Path.Combine(repo, "mods", "PhobosExchange", "framework", "exchange.json");
    var companies = File.Exists(shippedExchange) ? LoadPack(File.ReadAllText(shippedExchange)).companies.Keys.ToList() : new List<string>();
    foreach (var id in companies)
        foreach (var what in ExchangeRules.Events)
            Check(Phobos.Ostranauts.Framework.Story.StorySchema.IsId(ExchangeRules.StoryId(id, what)), "event id is a story id: " + ExchangeRules.StoryId(id, what));
    Check(Phobos.Ostranauts.Framework.Story.StorySchema.IsId(ExchangeRules.StoryId(new string('a', ExchangeSchema.MaxIdLength), ExchangeRules.MajorHolder)), "the longest company id still makes valid event ids");
    string shippedStory = Path.Combine(repo, "mods", "PhobosExchange", "framework", "story.json");
    if (File.Exists(shippedStory))
    {
        var story = DataPacks.LoadText<Phobos.Ostranauts.Framework.Story.StoryPack>(File.ReadAllText(shippedStory), "", ExchangeRules.Owner, Phobos.Ostranauts.Framework.Story.StorySchema.Name,
            s => Phobos.Ostranauts.Framework.Story.StorySchema.Validate(s, false));
        Check(story.threads.ContainsKey("exchange-lodestar") && companies.All(id => story.threads.ContainsKey("exchange-" + id)), "the story pack has a thread for the exchange and every company");
        foreach (var arc in story.arcs.Keys.Where(a => a.StartsWith("exchange-", StringComparison.Ordinal)))
            Check(companies.Any(id => ExchangeRules.Events.Any(what => arc == ExchangeRules.StoryId(id, what))), "every exchange arc answers a known company's event: " + arc);
    }
}

// ---- Company histories before the game (Exchange 0.3.0) --------------------------------------------------------
{
    long newGame = ExchangeRules.StepOf(65627498854);
    Check(ExchangeRules.MonthOf(newGame) / 12 == ExchangeRules.FirstSaveYear, "a new game's epoch falls in the first save year");
    string H(string find, string replace) { Check(Fixture.Contains(find), "fixture has " + find); return replace; }
    string history = Fixture
        .Replace("\"reportCooldownDays\": 1", H("\"reportCooldownDays\": 1", "\"reportCooldownDays\": 1, \"opened\": 2030, \"history\": { \"ceres-panic\": { \"year\": 2061, \"month\": 4, \"move\": -0.3, \"line\": \"Panic selling after the Ceres yard fire.\" } }"))
        .Replace("\"name\": \"Shipbuilding\", \"trend\": { \"slowWeeks\": 8, \"fastWeeks\": 2, \"sd\": 0.08 } }", H("\"name\": \"Shipbuilding\", \"trend\": { \"slowWeeks\": 8, \"fastWeeks\": 2, \"sd\": 0.08 } }",
                 "\"name\": \"Shipbuilding\", \"trend\": { \"slowWeeks\": 8, \"fastWeeks\": 2, \"sd\": 0.08 }, \"history\": { \"hull-boom\": { \"year\": 2050, \"move\": 0.25, \"line\": \"Hull orders double across the yards.\" } } }"))
        .Replace("\"price\": 42,", H("\"price\": 42,", "\"price\": 42, \"founded\": 2031, \"listed\": 2040, \"listingPrice\": 8, \"history\": { \"first-yard\": { \"year\": 2033, \"line\": \"Opens its first yard.\" }, " +
                 "\"titan-order\": { \"year\": 2066, \"month\": 9, \"move\": 0.4, \"line\": \"Wins a Titan fleet order.\" } },"))
        .Replace("\"price\": 12.5,", H("\"price\": 12.5,", "\"price\": 12.5, \"founded\": 2045,"));
    var hp = LoadPack(history);
    var keelMilestones = ExchangeSchema.Milestones(hp, "keel");
    Check(keelMilestones.Select(m => m.Id).SequenceEqual(new[] { "first-yard", "hull-boom", "ceres-panic", "titan-order" }), "a company's history is the exchange's, its sector's and its own, oldest first");
    Check(keelMilestones[0].LogMove == 0 && Near(keelMilestones[2].LogMove, Math.Log(0.7), 1e-12) && keelMilestones[2].Key == "Market.history.ceres-panic" &&
          keelMilestones[1].Key == "Sectors.ships.history.hull-boom" && keelMilestones[3].Key == "Companies.keel.history.titan-order", "lore moves nothing; each entry has its catalogue key");
    var lodePanic = ExchangeSchema.Milestones(hp, "lode").Single(m => m.Id == "ceres-panic");
    Check(Near(lodePanic.LogMove, 0.8 * Math.Log(0.7), 1e-12), "the exchange's history moves a company by its followsMarket");
    long hashed = keelMilestones[1].Month;
    Check(hashed / 12 == 2050 && hashed == ExchangeSchema.MonthOf("sector:ships", "hull-boom", hp.sectors["ships"].history["hull-boom"]), "a month left out is picked by a stable hash, within its year");
    Check(Near(ExchangeSchema.ListingGrowth(hp, "keel"), (Math.Log(42 / 8.0) - keelMilestones.Sum(m => m.LogMove)) / (2079 - 2040), 1e-12), "an authored listing price's yearly growth is worked net of the history's moves");

    string Bad(string find, string replace) { Check(history.Contains(find), "history fixture has " + find); return history.Replace(find, replace); }
    Check(Refused(Bad("\"opened\": 2030", "\"opened\": 1878")), "a year before the 200 years allowed is refused");
    Check(Refused(Bad("\"founded\": 2031", "\"founded\": 2079")), "a year from the game's start on is refused");
    Check(Refused(Bad("\"listed\": 2040", "\"listed\": 2030")), "a listing before the founding is refused");
    Check(Refused(Bad("\"founded\": 2031, \"listed\": 2040", "\"founded\": 2025, \"listed\": 2028")), "a listing before the exchange opened is refused");
    Check(Refused(Bad("\"year\": 2066", "\"year\": 2077")), "a move in the two years the market draws is refused");
    Check(Refused(Bad("\"year\": 2066", "\"year\": 2035")), "a move before the listing is refused");
    Check(!Refused(Bad("\"year\": 2033", "\"year\": 2032")) && Refused(Bad("\"year\": 2033", "\"year\": 2030")), "lore may date from the founding, not before");
    Check(Refused(Bad("\"month\": 4", "\"month\": 13")), "a month is 1 to 12");
    Check(Refused(Bad("\"move\": -0.3", "\"move\": -0.7")) && Refused(Bad("\"move\": 0.4", "\"move\": 1.5")) && Refused(Bad("\"move\": 0.4", "\"move\": 0.005")), "a move stays from -0.6 to 1, at least 0.01");
    Check(Refused(Bad("Opens its first yard.", "Opens [its] first yard.")), "a history line has no placeholders");
    Check(Refused(Bad("\"price\": 12.5, \"founded\": 2045,", "\"price\": 12.5, \"founded\": 2070, \"listingPrice\": 0.5,")) &&
          Refused(Bad("\"price\": 12.5, \"founded\": 2045,", "\"price\": 12.5, \"founded\": 2070, \"listingPrice\": 200,")), "a listing price that means a cliff or a collapse is refused");
    Check(Refused(Bad("\"price\": 12.5, \"founded\": 2045,", "\"price\": 12.5, \"founded\": 2077, \"listingPrice\": 12,")), "a listing price for a company listed in the two years the market draws is refused");
    Check(Refused(Bad("\"price\": 230,", "\"price\": 230, \"listingPrice\": 20,")) &&
          Refused(Bad("\"price\": 230,", "\"price\": 230, \"history\": { \"x\": { \"year\": 2050, \"line\": \"Something happened.\" } },")), "a listing price or history needs a founding or listing year");

    // The past: drawn from the pack and the seed, joined to where the save's own history begins.
    var hm = MarketModel.Open(hp, new ExchangeRecord(), newGame, Seed, _ => null);
    int keel = hm.IndexOf("keel"), lode = hm.IndexOf("lode"), glow = hm.IndexOf("glow");
    var life = hm.History(keel).Lifetime;
    Check(life.MonthAt(0) == ExchangeRules.MonthOf(hm.Record.Companies["keel"].Listed) && life.Count >= 20, "a new market's lifetime points start where its backfill starts (" + life.Count + ")");
    var past = hm.Past(keel);
    Check(past.Count == life.MonthAt(0) - 2040 * 12 && past.Months[0] == 2040 * 12 && past.Months[past.Count - 1] == life.MonthAt(0) - 1, "the past runs monthly from the listing to the month before the save's own history");
    Check(Near(past.Ln[0], Math.Log(8), 1e-12), "an authored listing price is met exactly");
    Check(Math.Abs(past.Ln[past.Count - 1] - life[0]) < 0.08, "the past joins the save's own history without a jump (" + (past.Ln[past.Count - 1] - life[0]).ToString("0.0000") + ")");
    // The same past with the Titan order as lore only: the noise is the same, so the difference is the move itself, a
    // step in the month after it, plus the straight line that keeps the listing price.
    var lore = MarketModel.Open(LoadPack(history.Replace("\"month\": 9, \"move\": 0.4,", "\"month\": 9,")), new ExchangeRecord(), newGame, Seed, _ => null).Past(keel);
    int titan = Array.IndexOf(past.Months, ExchangeRules.MonthIndex(2066, 9));
    double D(int k) => past.Ln[k] - lore.Ln[k];
    double kink = (D(titan + 1) - D(titan)) - (D(titan) - D(titan - 1));
    Check(Near(kink, Math.Log(1.4), 1e-9), "a history move shows as a step in the month after it (" + kink.ToString("0.000000") + ")");
    Check(hm.Past(glow).Count == 0, "a company without a founding or listing year has no past");
    var lodePast = hm.Past(lode);
    Check(lodePast.Count > 0 && lodePast.Months[0] == 2045 * 12, "the listing defaults to the founding");
    var again = MarketModel.Open(hp, new ExchangeRecord(), newGame, Seed, _ => null);
    Check(again.Past(keel).Ln.SequenceEqual(past.Ln) && again.Past(keel).Months.SequenceEqual(past.Months), "the same save draws the same past every time");
    var reload = new MarketModel(hp, ExchangeRecord.Decode(hm.Record.Encode()));
    reload.Start(hm.Clock, Seed);
    Check(reload.Past(keel).Ln.Zip(past.Ln, (a, b) => Math.Abs(a - b)).Max() < 1e-3, "a reload draws the same past again from the saved anchor");
    var withNova = LoadPack(history.Replace("\"companies\": {", """
        "companies": {
            "nova": { "ticker": "NOVA", "name": "Nova Lamps", "profile": "Lamps.", "sector": "ships", "price": 9, "dailyVolume": 9000, "volatility": 0.009, "founded": 2050,
              "volOfVol": 0.3, "noiseHalfLifeYears": 6, "drift": 0.03, "jumpsPerYear": 2, "jumpSize": 0.05, "spread": 0.005, "trend": { "slowWeeks": 6, "fastWeeks": 1.5, "sd": 0.12 } },
        """));
    var other = MarketModel.Open(withNova, new ExchangeRecord(), newGame, Seed, _ => null);
    Check(other.Past(other.IndexOf("keel")).Ln.SequenceEqual(past.Ln), "another company in the pack changes nothing in this one's past");
    var gen = new PastPrices(hp, Seed, life.MonthAt(0));
    gen.For("keel", life.MonthAt(0), life[0]);
    Check(gen.Stride == 1 && gen.Steps <= 3 * ExchangeRules.MaxPastSteps, "a past from 1879 steps monthly within the bound (" + gen.Steps + " steps)");
    Check(new PastPrices(hp, Seed, ExchangeRules.MonthIndex(2500, 1)).Stride == 2, "a past reaching centuries ahead steps every few months, within the bound");
    var timer = Stopwatch.StartNew();
    var full = new PastPrices(hp, Seed, life.MonthAt(0));
    foreach (var id in hp.companies.Keys) full.For(id, life.MonthAt(0), 0);
    Console.WriteLine($"  past for every fixture company: {full.Steps} steps, {timer.Elapsed.TotalMilliseconds:0.0} ms");

    // The views: All runs from the listing to now; a company new to a save shows its past on the two-year chart.
    var xs = new double[HistoryView.MaxPoints]; var ys = new double[HistoryView.MaxPoints];
    int count = HistoryView.Fill(ChartRange.All, hm.History(keel), past, ExchangeSchema.ListingMonth(hp.companies["keel"]), hm.Clock, hm.LnPrice[keel], xs, ys);
    Check(Math.Abs(xs[0] - 2040) < 0.01 && Near(ys[0], 8, 1e-9) && Math.Abs(xs[count - 1] - hm.Clock * 60 / ExchangeRules.YearSeconds) < 1e-9 && ys[count - 1] == Math.Exp(hm.LnPrice[keel]),
        "the All chart runs from the listing price to now, in calendar years");
    Check(Enumerable.Range(1, count - 1).All(k => xs[k] > xs[k - 1]) && count == past.Count + life.Count + 1, "the past, then the save's own points, then now, in order (" + count + ")");
    var later = ExchangeRecord.Decode(hm.Record.Encode());
    var mid = new MarketModel(withNova, later);
    mid.Start(hm.Clock + 10, Seed);
    int nova = mid.IndexOf("nova");
    Check(mid.History(nova).Lifetime.Count == 1 && mid.History(nova).Lifetime.MonthAt(0) == ExchangeRules.MonthOf(hm.Clock), "a company new to a save starts its own history now");
    count = HistoryView.Fill(ChartRange.Weeks, mid.History(nova), mid.Past(nova), ExchangeSchema.ListingMonth(withNova.companies["nova"]), mid.Clock, mid.LnPrice[nova], xs, ys);
    Check(count > 20 && xs[0] < -90 && xs[0] >= -ExchangeRules.WeeklyWeeks && Enumerable.Range(1, count - 1).All(k => xs[k] > xs[k - 1]), "its two-year chart shows its generated past (" + count + " points)");
    var young = LoadPack(history.Replace("\"price\": 12.5, \"founded\": 2045,", "\"price\": 12.5, \"founded\": 2078,"));
    var ym = MarketModel.Open(young, new ExchangeRecord(), newGame, Seed, _ => null);
    count = HistoryView.Fill(ChartRange.Weeks, ym.History(ym.IndexOf("lode")), ym.Past(ym.IndexOf("lode")), ExchangeSchema.ListingMonth(young.companies["lode"]), ym.Clock, ym.LnPrice[ym.IndexOf("lode")], xs, ys);
    double listedAt = (Phobos.Ostranauts.Framework.GameClock.MonthStart(2078 * 12) - ym.Clock * 60.0) / ExchangeRules.WeekSeconds;
    Check(xs[0] >= listedAt && xs[0] < listedAt + 2 && ym.Past(ym.IndexOf("lode")).Count == 0, "a company listed in the two years the market draws shows nothing from before its listing");

    // Records from 0.1 and 0.2 have no lifetime points: they begin at the oldest weekly close, and catch up.
    var fields = hm.Record.Encode();
    foreach (var key in fields.Keys.Where(k => k.StartsWith("hist.m.", StringComparison.Ordinal)).ToList()) fields.Remove(key);
    var old = new MarketModel(hp, ExchangeRecord.Decode(fields));
    old.Start(hm.Clock, Seed);
    var weekly = old.History(keel).Weekly;
    Check(old.History(keel).Lifetime.MonthAt(0) == Phobos.Ostranauts.Framework.GameClock.MonthIndex((weekly.FirstBucket + 1) * ExchangeRules.WeekSeconds) && old.History(keel).Lifetime.Count >= 20,
        "an older record's lifetime points begin at its oldest weekly close");
    var twice = new MarketModel(hp, ExchangeRecord.Decode(old.Record.Encode()));
    twice.Start(hm.Clock, Seed);
    Check(twice.Record.Encode().OrderBy(p => p.Key).SequenceEqual(old.Record.Encode().OrderBy(p => p.Key)), "starting twice changes nothing");
    // 0.2's own key rule, frozen here: it never reads the lifetime keys, so it keeps them untouched.
    static bool Read020(string key) { var p = key.Split('.'); return p.Length == 4 && p[1].Length == 1 && "hdw".IndexOf(p[1][0]) >= 0; }
    Check(hm.Record.Encode().Keys.Where(k => k.StartsWith("hist.m.", StringComparison.Ordinal)).All(k => !Read020(k)), "0.2 keeps the lifetime keys untouched");
    var future = hm.Record.Encode();
    foreach (var key in future.Keys.Where(k => k.StartsWith("hist.m.keel.", StringComparison.Ordinal)).ToList()) future.Remove(key);
    future["hist.m.keel.0"] = "2|from a newer version";
    var guarded = new MarketModel(hp, ExchangeRecord.Decode(future));
    guarded.Start(hm.Clock + 50000, Seed);
    var written = guarded.Record.Encode();
    Check(guarded.History(keel).Lifetime.Foreign && written["hist.m.keel.0"] == "2|from a newer version" && !written.ContainsKey("hist.m.keel.1") && guarded.Past(keel).Count == 0,
        "lifetime points this version cannot read are kept as they were and never written over");

    // The lifetime series itself.
    var series = new LifetimeSeries(6);
    series.Open(100, 1); series.Sample(100, 2); series.Sample(99, 3); series.Open(50, 9);
    Check(series.Count == 1 && series[0] == 1 && series.MonthAt(0) == 100, "the anchor is never replaced, and nothing older joins");
    for (long m = 101; m <= 140; m++) series.Sample(m, m);
    Check(series.Count <= 6 && series.MonthAt(0) == 100 && series[0] == 1 && series.LastMonth == 140 &&
          Enumerable.Range(1, series.Count - 2).All(k => series.MonthAt(k) % series.Resolution == 0), "a full series thins to the calendar grid, keeping the anchor and the newest (every " + series.Resolution + ")");
    var wild = new LifetimeSeries();
    wild.Open(22548, 0);
    for (int k = 1; k < 3000; k++) wild.Sample(22548 + k, k % 3 == 0 ? 50 : -50 + k * 0.001);
    var parts = wild.EncodeParts();
    Check(parts.All(p => p.Length <= ObjectStateStore.MaxValueLength && ObjectStateStore.SafeValue(p)) && parts.Count <= LifetimeSeries.MaxParts, "an extreme series fits the save store (" + parts.Count + " parts)");
    Check(LifetimeSeries.TryDecode(parts.Select((p, k) => (p, k)).ToDictionary(t => t.k, t => t.p), out var back) && back.Count == wild.Count && back.Resolution == wild.Resolution &&
          Enumerable.Range(0, back.Count).All(k => back.MonthAt(k) == wild.MonthAt(k) && Math.Abs(back[k] - wild[k]) <= 0.00005 + 1e-9), "lifetime points round-trip to 0.005%");
    Check(!LifetimeSeries.TryDecode(new Dictionary<int, string> { [1] = parts[0] }, out _) &&
          !LifetimeSeries.TryDecode(new Dictionary<int, string> { [0] = "1|1|100|3FF0000000000000|0;0|0;1" }, out _) &&
          !LifetimeSeries.TryDecode(new Dictionary<int, string> { [0] = "1|3|100|3FF0000000000000|0|0" }, out _) &&
          !LifetimeSeries.TryDecode(new Dictionary<int, string> { [0] = "1|1|100|3FF0000000000000|0;!|0;1" }, out _), "lifetime parts missing, not running forward, off the grid's powers of two or unreadable are refused");
}

// ---- The worked example add-on (Exchange 0.2.0, Framework 0.129.0), through Framework's own loader --------------
{
    string example = Path.Combine(repo, "examples", "addons", "PhobosExampleKeelhaulListing");
    var saved = AddOns.EnabledModDirectories;
    try
    {
        AddOns.EnabledModDirectories = () => new[] { example };
        AddOns.Reset();
        int problems = DataPacks.Problems.Count;
        var listed = DataPacks.Load<ExchangePack>(new DataPackSource(ExchangeRules.Owner, ExchangeRules.ModFolder, ExchangeSchema.Name, typeof(Fill).Assembly, "PhobosExchange.exchange.json"), ExchangeSchema.Validate);
        Check(AddOns.Current.Count == 1 && AddOns.Refused.Count == 0 && DataPacks.Problems.Count == problems, "the example add-on is found and its exchange file is accepted: " + string.Join("; ", DataPacks.Problems.Skip(problems).Select(x => x.File + ": " + x.Message)));
        Check(listed.companies.TryGetValue("keelhaul-freight", out var keelhaul) && keelhaul.ticker == "KHF" && keelhaul.news.Single().flag == "keelhaul-titan-contract" && listed.companies.Count == 9,
            "its company joins the eight listed, with its news");
        Check(keelhaul!.founded == 2052 && keelhaul.listed == 2058 && keelhaul.listingPrice == 4 && keelhaul.history.Keys.All(k => k.StartsWith("keelhaul-", StringComparison.Ordinal)) &&
              ExchangeSchema.Milestones(listed, "keelhaul-freight").Count(m => m.Scope == HistoryScope.Company && m.Entry.move != null) == 1 &&
              ExchangeSchema.Milestones(listed, "keelhaul-freight").All(m => m.Scope == HistoryScope.Company || m.Month >= 0),
              "and its history before the game (0.3.0), under its own prefix, beside the shipped exchange's and sector's");
        var story = DataPacks.Load<Phobos.Ostranauts.Framework.Story.StoryPack>(new DataPackSource("phobosgekko.ostranauts.framework", "PhobosFramework", Phobos.Ostranauts.Framework.Story.StorySchema.Name,
            typeof(Phobos.Ostranauts.Framework.Story.StoryPack).Assembly, "PhobosFramework.story.json"), s => Phobos.Ostranauts.Framework.Story.StorySchema.Validate(s, true));
        bool refusedWithoutNamespace = !AddOns.Namespaces.Contains("exchange") && DataPacks.Problems.Count > problems && !story.arcs.ContainsKey("exchange-keelhaul-freight-bought");
        Check(refusedWithoutNamespace || AddOns.Namespaces.Contains("exchange"), "without the exchange namespace the add-on's event arc is refused");
        AddOns.RegisterNamespace("exchange");
        problems = DataPacks.Problems.Count;
        story = DataPacks.Load<Phobos.Ostranauts.Framework.Story.StoryPack>(new DataPackSource("phobosgekko.ostranauts.framework", "PhobosFramework", Phobos.Ostranauts.Framework.Story.StorySchema.Name,
            typeof(Phobos.Ostranauts.Framework.Story.StoryPack).Assembly, "PhobosFramework.story.json"), s => Phobos.Ostranauts.Framework.Story.StorySchema.Validate(s, true));
        Check(DataPacks.Problems.Count == problems && story.arcs.ContainsKey("keelhaul-titan-contract") && story.arcs.ContainsKey("exchange-keelhaul-freight-bought") && story.threads.ContainsKey("keelhaul-freight"),
            "with the exchange namespace registered, its story and its bought letter load: " + string.Join("; ", DataPacks.Problems.Skip(problems).Select(x => x.File + ": " + x.Message)));
        Check(story.arcs["keelhaul-titan-contract"].steps.Last().onComplete!.setFlags.Contains("keelhaul-titan-contract"), "its arc sets the flag its news answers to");
        Check(keelhaul.news.Single().wire!.Count == 3 && story.broadcasts["keelhaul-surge-wire"].text.Count == 3 &&
              story.arcs["exchange-keelhaul-freight-major-holder"].steps[0].delivery!.message!.text.Count == 3,
            "the example shows variant lines (Framework 0.132.0) on its wire line, its news item and its repeating letter");
    }
    finally { AddOns.EnabledModDirectories = saved; AddOns.Reset(); }
}

// ---- The drift guard against Banking's cheapest loan (owner rule) ------------------------------------------
{
    var lenders = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(repo, "mods", "PhobosBank", "framework", "lenders.json"))).RootElement;
    double cheapest = double.MaxValue;
    foreach (string table in new[] { "lenders", "creditLines" })
        foreach (var entry in lenders.GetProperty(table).EnumerateObject())
            cheapest = Math.Min(cheapest, entry.Value.GetProperty("ratePerShift").GetDouble());
    double yearly = cheapest * ExchangeRules.YearSeconds / Phobos.Ostranauts.Framework.GameClock.ShiftSeconds;
    Console.WriteLine($"  cheapest Banking rate {cheapest:0.0000} a shift, {yearly:P1} a game year; guard {ExchangeRules.MaxExpectedReturn:P0}");
    Check(ExchangeRules.MaxExpectedReturn <= yearly / 2, "the expected-return cap is at most half the cheapest Banking loan's yearly cost");
}

// ---- Economy: borrowing to hold never pays; trend-following is reported --------------------------------------
Economy(pack, "fixture");
string shipped = Path.Combine(repo, "mods", "PhobosExchange", "framework", "exchange.json");
if (File.Exists(shipped))
{
    var shippedPack = LoadPack(File.ReadAllText(shipped));
    Check(shippedPack.companies.Count >= 6 && shippedPack.companies.Count <= 10, "the shipped exchange lists six to ten companies");
    foreach (var pair in shippedPack.companies)
    {
        double er = ExchangeSchema.ExpectedReturn(pair.Value, shippedPack.sectors[pair.Value.sector].trend, shippedPack.market.trend);
        Console.WriteLine($"  {pair.Value.ticker}: expected yearly return {er:P1}");
        Check(pair.Value.drivers.Count > 0, "every shipped company follows the game's market: " + pair.Key);
        Check(pair.Value.founded != null && pair.Value.listed != null && pair.Value.listed >= shippedPack.market.opened, "every shipped company has a founding and a listing year: " + pair.Key);
    }
    Check(shippedPack.market.opened != null, "the shipped exchange has an opening year");
    // The record's size with the shipped companies (0.3.0): at first, and after fifty years of lifetime points.
    var sized = MarketModel.Open(shippedPack, new ExchangeRecord(), ExchangeRules.StepOf(65627498854), Seed, _ => null);
    int atFirst = sized.Record.Encode().Sum(p => p.Key.Length + p.Value.Length);
    for (int year = 0; year < 50; year++) sized.Advance(sized.Clock + (long)(ExchangeRules.YearSeconds / ExchangeRules.StepSeconds), null);
    var fifty = sized.Record.Encode();
    int afterFifty = fifty.Sum(p => p.Key.Length + p.Value.Length);
    Console.WriteLine($"  shipped record: {atFirst} characters at first, {afterFifty} after fifty years");
    Check(fifty.Values.All(v => v.Length <= ObjectStateStore.MaxValueLength) && afterFifty < 40000, "the shipped record stays bounded over fifty years (" + afterFifty + ")");
    var timePast = Stopwatch.StartNew();
    for (int i = 0; i < sized.Count; i++) sized.Past(i);
    Console.WriteLine($"  shipped past for every company: {sized.PastPoints} points, {timePast.Elapsed.TotalMilliseconds:0.0} ms");
    Economy(shippedPack, "shipped");
}

void Economy(ExchangePack p, string label)
{
    const double rate = 0.0002;
    double shiftsPerYear = ExchangeRules.YearSeconds / Phobos.Ostranauts.Framework.GameClock.ShiftSeconds;
    double holdProfit = 0, trendProfit = 0, chartProfit = 0; int holds = 0, months = 0;
    long month = (long)Math.Ceiling(30 * ExchangeRules.DaySeconds / ExchangeRules.StepSeconds);
    for (ulong seed = 1; seed <= 10; seed++)
    {
        var start = new MarketModel(p, new ExchangeRecord());
        start.Start(now, seed);
        double[] open = (double[])start.LnPrice.Clone();
        var trend = new MarketModel(p, ExchangeRecord.Decode(start.Record.Encode()));
        trend.Start(now, seed);
        // Buy and hold for three years with borrowed money at the cheapest rate.
        start.Advance(now + 36 * month, null);
        for (int i = 0; i < start.Count; i++)
        {
            double growth = Math.Exp(start.LnPrice[i] - open[i]);
            holdProfit += growth - 1 - rate * shiftsPerYear * 3;
            holds++;
        }
        // Follow the trend month by month, in cash only: hold while the phase rises.
        for (int mth = 0; mth < 36; mth++)
        {
            double[] snapshot = (double[])trend.LnPrice.Clone();
            bool[] rise = Enumerable.Range(0, trend.Count).Select(i => trend.TotalSpeed(i) > 0).ToArray();
            // What a player can see: the price above where it stood a week ago on the daily chart.
            bool[] chart = Enumerable.Range(0, trend.Count).Select(i => { var d = trend.History(i).Daily; return d.Count >= 8 && trend.LnPrice[i] > d[d.Count - 8]; }).ToArray();
            trend.Advance(trend.Clock + month, null);
            for (int i = 0; i < trend.Count; i++)
            {
                double gain = Math.Exp(trend.LnPrice[i] - snapshot[i]) - 1 - 2 * trend.Entries[i].spread;
                if (rise[i]) trendProfit += gain;
                if (chart[i]) chartProfit += gain;
            }
            months++;
        }
    }
    double meanHold = holdProfit / holds;
    Console.WriteLine($"  economy ({label}): borrow-and-hold over 3 years {meanHold:P1} after interest; following the hidden phase {trendProfit / months / Math.Max(1, p.companies.Count):P2}, following the weekly chart {chartProfit / months / Math.Max(1, p.companies.Count):P2} a month per company, before commission");
    Check(meanHold < 0, "borrowing at the cheapest rate to buy and hold loses on average (" + label + ")");
}

Console.WriteLine($"Phobos Exchange checks passed: {checks}.");

sealed class PeakObserver : IMarketObserver
{
    private readonly int company;
    public double High, Low;
    public PeakObserver(int company, double start) { this.company = company; High = Low = start; }
    public void Stepped(MarketModel model, long step, bool fine) { High = Math.Max(High, model.Price(company)); Low = Math.Min(Low, model.Price(company)); }
    public void HourClosed(MarketModel model, long hour) { }
}

sealed class AlertObserver : IMarketObserver
{
    private readonly int company;
    public readonly Alert Alert;
    public int Fired;
    public AlertObserver(int company, Alert alert) { this.company = company; Alert = alert; }
    public void Stepped(MarketModel model, long step, bool fine) { if (AlertRules.Check(Alert, model.Price(company)) != 0) Fired++; }
    public void HourClosed(MarketModel model, long hour) { }
}

sealed class ReportObserver : IMarketObserver
{
    private readonly MoveWatch watch;
    private readonly List<MoveReport> reports;
    public ReportObserver(MoveWatch watch, List<MoveReport> reports) { this.watch = watch; this.reports = reports; }
    public void Stepped(MarketModel model, long step, bool fine) { }
    public void HourClosed(MarketModel model, long hour) => watch.HourClosed(model, hour, reports);
}
