using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Localization;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>The shared primitives of the 29 September 2026 performance pass, on their own: the definition index,
/// per-step memo, real-time cadence, deferred text and fluid topology.</summary>
internal static class PrimitiveChecks
{
    internal static void Run(Action<bool, string> check)
    {
        void Reject(Action action, string label) { bool failed = false; try { action(); } catch { failed = true; } check(failed, label); }

        // Definition index: one probe per definition, families by prefix, cleared with its registrations.
        var index = new DefinitionIndex<string>();
        index.Add("PhobosSiloA", "a"); index.Add("PhobosSiloB", "b");
        check(index.Get("PhobosSiloAInstalled") == "a" && index.Get("PhobosSiloBLooseDmg") == "b" && index.Get("PhobosSiloAInstalledExtra") == null && index.Get(null) == null,
            "The index answers by the four native forms of each family and nothing else");
        index.Add("PhobosSiloA", "a2");
        check(index.Get("PhobosSiloAInstalled") == "a2", "A re-registered family replaces its value and forgets remembered answers");
        for (int i = 0; i < 1000; i++) index.Get("ItmOtherInstalled");
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 42000; i++) index.Get("ItmOtherInstalled");
        check(GC.GetAllocatedBytesForCurrentThread() == before, "A remembered miss allocates nothing on the test runtime");
        check(index.Remove(v => v == "b") == 1 && index.Get("PhobosSiloBInstalled") == null && index.Count == 1, "Removing an owner's families forgets their answers");
        index.Clear(); check(index.Count == 0 && index.Get("PhobosSiloAInstalled") == null, "Clear empties the index");
        Reject(() => index.Add("", "x"), "An empty prefix is refused");

        // Step memo: values live for one step and vanish with it or on request.
        var memo = new StepMemo<string, int>();
        memo.Set(10, "a", 1);
        check(memo.TryGet(10, "a", out int a) && a == 1 && memo.Count == 1, "A value set in a step is found in that step");
        check(!memo.TryGet(11, "a", out _) && memo.Count == 0 && memo.Step == 11, "The next step forgets every value");
        int computed = 0;
        check(memo.GetOrAdd(11, "b", _ => ++computed) == 1 && memo.GetOrAdd(11, "b", _ => ++computed) == 1 && computed == 1, "GetOrAdd computes once per step");
        memo.Invalidate(); check(!memo.TryGet(11, "b", out _), "Invalidate forgets within the step");

        // Cadence: first pass at once, one pass per interval, no catch-up bursts, invalidation.
        var cadence = new Cadence(2);
        check(cadence.Due(100) && !cadence.Due(101) && !cadence.Due(101.9) && cadence.Due(102) && !cadence.Due(103), "One pass per interval");
        check(cadence.Due(200) && !cadence.Due(201), "A stall produces one pass without catch-up");
        cadence.Invalidate(); check(cadence.Due(201), "Invalidate makes the next call due");
        check(!cadence.Due(double.NaN) && !cadence.Due(double.PositiveInfinity), "Nonfinite times are never due");
        foreach (double bad in new[] { 0, -1, double.NaN, double.PositiveInfinity }) Reject(() => new Cadence(bad), "Invalid cadence rejected");

        // Deferred text: formatted on read, once per change, unchanged sets cost nothing.
        int formats = 0;
        var message = new DeferredMessage((key, args) => { formats++; return key + ":" + string.Join(",", args); }, "start");
        check(message.Resolve() == "start:" && message.Resolve() == "start:" && formats == 1, "Resolved once until it changes");
        check(!message.Set("start") && formats == 1, "Setting the same key leaves it alone");
        check(message.Set("level", 3.5, "tank") && message.Same("level", 3.5, "tank") && !message.Same("level", 3.6, "tank") && message.ToString() == "level:3.5,tank" && formats == 2,
            "A changed key or argument is formatted again, equal arguments are not");
        check(!message.Set("level", 3.5, "tank") && formats == 2, "Equal arguments do not re-format");
        Reject(() => new DeferredMessage(null!, "x"), "A message needs a formatter");

        // Fluid topology: components equal reachability; paths equal the bounded grid search.
        // 4 x 4 grid: cells 0,1,2 on row 0; 5 below 1; 10,11 on row 2 apart from the first run.
        var topology = FluidTopology.Build(4, 4, 6, new[] { 0, 1, 2, 5, 10, 11 });
        check(topology.AllowedCount == 6 && topology.ComponentCount == 2 && topology.Connected(0, 5) && topology.Connected(2, 5) && !topology.Connected(0, 10) && topology.ComponentOf(3) == -1,
            "Adjacent segment cells share a component; separated runs do not");
        var path = topology.Path(0, 5);
        var reference = GridRoute.Find(4, 4, new[] { 0 }, new HashSet<int> { 5 }, topology.Allowed);
        check(path != null && reference != null && path.SequenceEqual(reference) && path.SequenceEqual(new[] { 0, 1, 5 }), "The topology path is the grid search's path");
        check(topology.Path(0, 10) == null && topology.Path(0, 3) == null && topology.Path(7, 0) == null, "No path across components or from a cell that carries no fluid");
        check(topology.Path(0, 5, 2) == null, "A visit limit still bounds the search exactly as before");
        var overflow = FluidTopology.Build(4, 4, 5000, new[] { 0, 1 }, 4096);
        check(overflow.Overflow && !overflow.Connected(0, 1) && overflow.Path(0, 1) == null, "More segments than the limit trusts no route, as the old search did");
        check(FluidTopology.Build(4, 4, 0, new[] { -1, 99 }).AllowedCount == 0, "Cells off the grid are ignored");
        Reject(() => FluidTopology.Build(0, 4, 0, Array.Empty<int>()), "An empty grid is refused");
    }
}
