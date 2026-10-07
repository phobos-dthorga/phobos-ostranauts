using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Diagnostics;
using Phobos.Ostranauts.Framework.Trading;

/// <summary>Framework 0.128.0: the chart's arithmetic (ticks, ranges, thinning), the test-command gate's rule and the
/// player holdings registry. The chart's drawing and the gate's reading of the game's switch are native.</summary>
internal static class ChartChecks
{
    internal static void Run(Action<bool, string> check)
    {
        check(ChartRules.NiceStep(0.37, 4) == 0.1 && ChartRules.NiceStep(95, 5) == 20 && ChartRules.NiceStep(73, 5) == 20 && ChartRules.NiceStep(7, 4) == 2, "tick steps are 1, 2 or 5 times a power of ten");
        check(ChartRules.NiceStep(0, 4) == 1 && ChartRules.NiceStep(double.NaN, 4) == 1, "a zero or unreadable span still gives a step");
        var ticks = new double[12];
        int n = ChartRules.Ticks(0, 100, 5, ticks);
        check(n == 6 && ticks.Take(n).SequenceEqual(new double[] { 0, 20, 40, 60, 80, 100 }), "ticks fall on the step's multiples, ends included");
        n = ChartRules.Ticks(-73, 0, 5, ticks);
        check(n == 4 && ticks.Take(n).SequenceEqual(new double[] { -60, -40, -20, 0 }), "ticks on a span ending at now");
        check(ChartRules.Ticks(5, 5, 5, ticks) == 0 && ChartRules.Ticks(0, 1, 5, Array.Empty<double>()) == 0, "no span or no room gives no ticks");
        check(ChartRules.Ticks(0, 1e6, 3, new double[2]) == 2, "ticks never overrun the buffer");

        ChartRules.Range(new[] { 2.0, 8.0 }, 2, 0.1, out double lo, out double hi);
        check(Math.Abs(lo - 1.4) < 1e-12 && Math.Abs(hi - 8.6) < 1e-12, "the range is padded by a share of the span");
        ChartRules.Range(new[] { 5.0, 5.0 }, 2, 0.1, out lo, out hi);
        check(lo < 5 && hi > 5, "a flat series gets a span around its value");
        ChartRules.Range(new[] { double.NaN, double.PositiveInfinity }, 2, 0.1, out lo, out hi);
        check(lo == 0 && hi == 1, "no finite value gives 0 to 1");
        ChartRules.Range(new[] { 3.0, double.NaN, 9.0 }, 3, 0, out lo, out hi);
        check(lo == 3 && hi == 9, "unreadable values are skipped");
        check(ChartRules.Map(5, 0, 10, 200) == 100 && ChartRules.Map(5, 5, 5, 200) == 100, "values map across the pixels; a flat range sits in the middle");

        const int count = 10000;
        var xs = Enumerable.Range(0, count).Select(i => (double)i).ToArray();
        var ys = xs.Select(x => Math.Sin(x / 37) * 10 + (x == 4321 ? 50 : 0) + (x == 777 ? -40 : 0)).ToArray();
        var ox = new double[512]; var oy = new double[512];
        int kept = ChartRules.Thin(xs, ys, count, 0, count - 1, 100, ox, oy);
        check(kept <= 201 && kept > 100, "a long series thins to at most two points per column (" + kept + ")");
        check(ox[0] == 0 && ox[kept - 1] == count - 1, "thinning keeps the first and the newest point");
        check(oy.Take(kept).Max() == ys.Max() && oy.Take(kept).Min() == ys.Min(), "thinning keeps the highest and lowest points");
        check(Enumerable.Range(1, kept - 1).All(i => ox[i] >= ox[i - 1]), "thinned points stay in order");
        int few = ChartRules.Thin(new double[] { 0, 1, 2 }, new double[] { 5, 6, 7 }, 3, 0, 2, 100, ox, oy);
        check(few == 3 && oy[2] == 7, "a short series is drawn as it is");
        check(ChartRules.Thin(xs, ys, 0, 0, 1, 10, ox, oy) == 0 && ChartRules.Thin(xs, ys, count, 0, count, 10, Array.Empty<double>(), oy) == 0, "nothing to draw, or nowhere to put it, gives nothing");

        // The test-command gate (owner rule, 7 October 2026).
        check(DebugCommands.Decide(false, false) == DebugCommands.Decision.Locked && DebugCommands.Decide(false, true) == DebugCommands.Decision.Locked, "locked debug commands refuse, confirmed or not");
        check(DebugCommands.Decide(true, false) == DebugCommands.Decision.Warn, "unlocked, the first use only warns");
        check(DebugCommands.Decide(true, true) == DebugCommands.Decision.GoAhead, "unlocked and confirmed goes ahead");
        // Framework 0.128.1: the shared test-change mark on the save.
        string first = DebugCommands.Mark(null, 1234.5, "jump TSTD by +20%");
        check(DebugCommands.Count(first) == 1 && first.EndsWith("|jump TSTD by +20%"), "the first test change counts one and names itself");
        string second = DebugCommands.Mark(first, 2000, "a|b=c,d");
        check(DebugCommands.Count(second) == 2 && !second.Substring(second.LastIndexOf('|') + 1).Contains('=') && !second.Contains(','), "later changes count up, in text the save store can hold");
        check(DebugCommands.Count("garbage") == 0 && DebugCommands.Count(DebugCommands.Mark("garbage", 1, "x")) == 1, "a mark this version cannot read starts its count again");
        check(Phobos.Ostranauts.Framework.Persistence.ObjectStateStore.SafeValue(DebugCommands.Mark(null, 1, new string('x', 900))), "a long description is cut to fit the save store");

        // Player holdings for overviews.
        PlayerHoldings.Register("test.b", () => new HoldingLine { Label = "B", Value = 2 });
        PlayerHoldings.Register("test.a", () => new HoldingLine { Label = "A", Value = 1 });
        PlayerHoldings.Register("test.none", () => null);
        PlayerHoldings.Register("test.broken", () => throw new InvalidOperationException("broken"));
        var lines = PlayerHoldings.Read();
        check(lines.Select(l => l.Owner).SequenceEqual(new[] { "test.a", "test.b" }) && lines[0].Value == 1, "holdings are read in owner order; nothing held and failing providers are left out");
        PlayerHoldings.Register("test.a", () => new HoldingLine { Label = "A2", Value = 3 });
        check(PlayerHoldings.Read().First(l => l.Owner == "test.a").Label == "A2", "registering again replaces a provider");
        foreach (var owner in new[] { "test.a", "test.b", "test.none", "test.broken" }) PlayerHoldings.Unregister(owner);
        check(PlayerHoldings.Read().Count == 0, "unregistered providers are gone");
    }
}
