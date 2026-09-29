using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Discovery;
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

        WorldIndexChecks(check, Reject);
        NameFilterChecks(check);
    }

    /// <summary>The trigger-name pre-filter: false only for names that are certainly not registered.</summary>
    private static void NameFilterChecks(Action<bool, string> check)
    {
        var registered = new[] { "PhobosCraftSelect_PhobosShipbreakerD4_0", "PhobosCraftStation_PhobosFurnace", "PhobosShipbreakerF6SectionMaterial", "\u00e9t\u00e9" };
        var filter = new Phobos.Ostranauts.Framework.Construction.NameFilter();
        foreach (var name in registered) filter.Add(name);
        check(registered.All(filter.MayContain), "Every registered name passes the filter");
        var random = new Random(7); int rejected = 0, tried = 0;
        foreach (var vanilla in new[] { "TIsHuman", "TIsInstalled", "TIsAirPumpInstalledNotDamaged", "Blank", "TCanStackItem", "TIsPoweredConsole" })
        { tried++; if (!filter.MayContain(vanilla)) rejected++; }
        bool exact = true;
        for (int i = 0; i < 20000; i++)
        {
            var chars = new char[random.Next(1, 80)];
            for (int c = 0; c < chars.Length; c++) chars[c] = (char)random.Next(32, 300);
            var name = new string(chars);
            exact &= filter.MayContain(name) || !registered.Contains(name);
            // Near misses: a registered name with its last character changed keeps length and first character.
            var near = registered[i % registered.Length]; near = near.Substring(0, near.Length - 1) + (char)(near[near.Length - 1] + 1);
            exact &= filter.MayContain(near) && !registered.Contains(near);
        }
        check(exact, "A name the filter rejects is never registered, and near misses still reach the table");
        check(rejected == tried, "The game's usual trigger names are rejected without a table lookup");
        check(!filter.MayContain("") && !filter.MayContain("P"), "Empty and unregistered short names are rejected");
        filter.Clear();
        check(!filter.MayContain(registered[0]), "A cleared filter rejects everything");
    }

    private sealed class Thing { internal string? Definition; internal bool Alive = true; internal Thing(string? d) { Definition = d; } }

    /// <summary>Stage 8: one spread sweep finds every family's objects; reads drop anything that left the world.</summary>
    private static void WorldIndexChecks(Action<bool, string> check, Action<Action, string> reject)
    {
        int asked = 0;
        var world = new WorldIndex<Thing>(t => t.Definition, t => t.Alive);
        int racks = world.Register("racks", d => { asked++; return d.StartsWith("Rack", StringComparison.Ordinal); });
        int stores = world.Register("stores", d => d == "Store" || d == "RackStore");
        var things = new List<Thing>();
        for (int i = 0; i < 1000; i++) things.Add(new Thing(i % 100 == 0 ? "RackInstalled" : i % 250 == 1 ? "Store" : "ItmWall"));
        var shared = new Thing("RackStore"); things.Add(shared); things.Add(null!); things.Add(new Thing(null));
        var found = new List<Thing>();
        check(!world.Primed, "A new index has not swept yet");
        world.Begin(things);
        check(world.Sweeping && world.Pending == 1003, "A sweep snapshots the whole world in one copy; missing entries are skipped as it goes");
        int examined = 0, frames = 0;
        while (world.Sweeping) { examined += world.Advance(64); frames++; }
        check(world.Primed && examined == 1003 && frames == 16, "The sweep is spread in slices and completes the index");
        world.Members(racks, found);
        check(found.Count == 11 && found.All(t => t.Definition!.StartsWith("Rack")) && found.Contains(shared), "Every family object is found, in the order met");
        world.Members(stores, found);
        check(found.Count == 5 && found.Contains(shared), "An object can belong to several families");
        check(asked == 4, "Each of the four definitions is classified once, whatever the number of objects carrying it");
        check(world.Belongs(racks, "RackLoose") && !world.Belongs(racks, "Store") && !world.Belongs(stores, null), "Definition membership is answered directly");

        world.Begin(things); world.Advance(int.MaxValue);
        world.Members(racks, found);
        check(found.Count == 11, "A repeat sweep adds nothing twice");
        things[0].Alive = false;
        world.Members(racks, found);
        check(found.Count == 10 && !found.Contains(things[0]), "An object that left the world drops out on the next read");
        things[0].Alive = true;
        world.Members(racks, found);
        check(found.Count == 10, "A dropped object returns only when a sweep or an offer finds it again");
        world.Offer(things[0]);
        world.Members(racks, found);
        check(found.Count == 11, "An offered object joins at once");

        int fresh = world.Register("fresh", d => d == "Store");
        world.Members(racks, found);
        check(!world.Primed && found.Count == 11, "A late registration keeps the other families' members and asks for a new sweep");
        world.Members(fresh, found);
        check(found.Count == 0, "A late family is empty until the sweep reaches its objects");
        world.Begin(things); world.Advance(int.MaxValue);
        world.Members(fresh, found);
        check(world.Primed && found.Count == 4, "The next sweep completes the late family");
        world.Unregister("fresh");
        world.Members(fresh, found);
        check(found.Count == 0 && !world.Belongs(fresh, "Store"), "An unregistered family matches nothing");
        world.Reset();
        world.Members(racks, found);
        check(!world.Primed && found.Count == 0, "A reset forgets every member");
        world.Begin(Array.Empty<Thing>());
        check(world.Primed && !world.Sweeping, "An empty world completes at once");
        reject(() => world.Register("", d => true), "A family needs a key");
        var many = new WorldIndex<Thing>(t => t.Definition, t => t.Alive);
        for (int i = 0; i < WorldIndex<Thing>.MaximumFamilies; i++) many.Register("f" + i, d => false);
        reject(() => many.Register("one too many", d => false), "The family bound is enforced");

        world.Begin(things); world.Advance(int.MaxValue);
        for (int i = 0; i < 20; i++) world.Members(racks, found);
        check(Allocates(() => { for (int i = 0; i < 1000; i++) world.Members(racks, found); }) == false, "Reading a family allocates nothing on the test runtime");
    }

    private static bool Allocates(Action action)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            if (GC.GetAllocatedBytesForCurrentThread() == before) return false;
            System.Threading.Thread.Sleep(200);
        }
        return true;
    }
}
