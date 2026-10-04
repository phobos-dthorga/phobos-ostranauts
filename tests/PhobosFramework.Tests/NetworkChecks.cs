using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;

/// <summary>Framework 0.56.0 line networks and shared stores: participants join the segment at their ports and,
/// where the family allows, every participant they touch, chaining across the ship (the owner's link rule of 30
/// September 2026); receiver banks let several machines share one store, with slot zero keeping the old port.</summary>
internal static class NetworkChecks
{
    internal static void Run(Action<bool, string> check)
    {
        // A 6 x 3 grid: a pipe along row 1 from column 2 to 4 (cells 8, 9, 10).
        IReadOnlyList<IReadOnlyList<int>> ports = new IReadOnlyList<int>[] { new[] { 7 }, new[] { 8 }, new[] { 10 }, new[] { 16 } };
        var piped = FluidTopology.Build(6, 3, 3, new[] { 8, 9, 10 }, 4096, ports);
        check(piped.ParticipantCount == 4 && piped.ParticipantsConnected(1, 2), "Two participants with ports on one run of pipe share a network");
        check(piped.Hops(1, 2) == 4 && piped.Hops(2, 1) == 4, "Network steps count the pipe cells between the ports");
        check(!piped.ParticipantsConnected(0, 1) && !piped.ParticipantsConnected(3, 1), "A participant whose join cells miss the pipe joins nothing");
        check(piped.Hops(0, 1) == -1, "No network, no steps");

        // Framework 0.69.0 (owner decision, 1 October 2026): any pipe under or beside equipment joins it. The adapter
        // gives each participant its own cells and the cells north, south, east and west of them.
        check(FluidTopology.OnOrBeside(6, 3, new[] { 7 }).SequenceEqual(new[] { 7, 6, 8, 1, 13 }), "A tile's join cells are itself and its four neighbours, never the corners");
        check(FluidTopology.OnOrBeside(6, 3, new[] { 5 }).SequenceEqual(new[] { 5, 4, 11 }) && FluidTopology.OnOrBeside(6, 3, new[] { 6 }).SequenceEqual(new[] { 6, 7, 0, 12 }),
            "Join cells stop at the grid edge and never wrap from one row to the next");
        check(FluidTopology.OnOrBeside(6, 3, new[] { 0, 1 }).SequenceEqual(new[] { 0, 1, 6, 2, 7 }), "Join cells of a wider footprint list each cell once");
        check(FluidTopology.OnOrBeside(6, 3, new[] { -1, 18 }).Length == 0 && FluidTopology.OnOrBeside(0, 3, new[] { 1 }).Length == 0, "Cells off the grid, or no grid, give no join cells");
        IReadOnlyList<IReadOnlyList<int>> beside = new IReadOnlyList<int>[] { FluidTopology.OnOrBeside(6, 3, new[] { 7 }), new[] { 10 } };
        var touching = FluidTopology.Build(6, 3, 3, new[] { 8, 9, 10 }, 4096, beside);
        check(touching.ParticipantsConnected(0, 1), "A one-tile participant beside the end of a pipe joins it");

        // The owner's layout of 1 October 2026 on a 12 x 10 grid (column = x + 12, row = y - 18): a 3 x 3 silo over
        // x -10..-8, y 23..25, a 2 x 2 machine over x -11..-10, y 19..20, and water pipe at (-9, 22), (-9, 23), (-9, 24),
        // which runs into the middle of the silo and stops short of the machine.
        int Cell(int x, int y) => (y - 18) * 12 + x + 12;
        int[] Tiles(int x0, int x1, int y0, int y1) => Enumerable.Range(x0, x1 - x0 + 1).SelectMany(x => Enumerable.Range(y0, y1 - y0 + 1).Select(y => Cell(x, y))).ToArray();
        int[] silo = Tiles(-10, -8, 23, 25), machine = Tiles(-11, -10, 19, 20);
        int[] laid = { Cell(-9, 22), Cell(-9, 23), Cell(-9, 24) };
        IReadOnlyList<IReadOnlyList<int>> oldPorts = new IReadOnlyList<int>[] { new[] { Cell(-11, 24) }, new[] { Cell(-9, 19) } };
        var before = FluidTopology.Build(12, 10, laid.Length, laid, 4096, oldPorts);
        check(!before.ParticipantsConnected(0, 1) && !before.ParticipantsOn(laid[0]).Any(), "Under the single-port rule the owner's pipe joined neither the silo nor the machine");
        IReadOnlyList<IReadOnlyList<int>> joinCells = new IReadOnlyList<int>[] { FluidTopology.OnOrBeside(12, 10, silo), FluidTopology.OnOrBeside(12, 10, machine) };
        var after = FluidTopology.Build(12, 10, laid.Length, laid, 4096, joinCells);
        check(after.ParticipantsOn(laid[0]).SequenceEqual(new[] { 0 }) && !after.ParticipantsConnected(0, 1), "A pipe laid under the silo joins it; a machine two tiles from the pipe's end is not joined");
        int[] diagonal = laid.Concat(new[] { Cell(-9, 21) }).ToArray();
        check(!FluidTopology.Build(12, 10, diagonal.Length, diagonal, 4096, joinCells).ParticipantsConnected(0, 1), "A pipe ending on the machine's corner tile does not join it");
        int[] reaching = diagonal.Concat(new[] { Cell(-9, 20) }).ToArray();
        var linked = FluidTopology.Build(12, 10, reaching.Length, reaching, 4096, joinCells);
        check(linked.ParticipantsConnected(0, 1) && linked.Hops(1, 0) == 4, "A pipe that ends beside the machine joins it to the silo");
        check(joinCells[0].Contains(Cell(-11, 24)) && joinCells[1].Contains(Cell(-9, 19)), "The old port tile is one of the join cells, so every layout that joined before still does");

        // Why a picker does not offer a store: the store's own state first, then the pipe at each end, then the run between.
        var ok = new ReachFacts { MachineReady = true, Installed = true, Ready = true, Network = true, StoreOpenPipe = true, MachineOpenPipe = true };
        ReachProblem Why(Func<ReachFacts, ReachFacts> change) => LinkDiagnosis.Classify(change(ok));
        check(Why(f => { f.Reached = true; return f; }) == ReachProblem.None && Why(f => f) == ReachProblem.SeparateRuns, "A reached store has no problem; two piped ends that do not meet are separate runs");
        check(Why(f => { f.MachineReady = false; return f; }) == ReachProblem.MachineNotReady && Why(f => { f.Installed = false; f.Ready = false; f.Damaged = true; return f; }) == ReachProblem.NotInstalled,
            "The machine's own state comes first, then a loose store before any other fault");
        check(Why(f => { f.Damaged = true; f.Ready = false; f.Reached = true; return f; }) == ReachProblem.Damaged && Why(f => { f.Locked = true; f.Ready = false; return f; }) == ReachProblem.Locked &&
            Why(f => { f.Ready = false; return f; }) == ReachProblem.NotReady, "A damaged or locked store is named even when it touches the machine");
        check(Why(f => { f.Network = false; return f; }) == ReachProblem.TouchOnly && Why(f => { f.Overflow = true; return f; }) == ReachProblem.LayoutTooLarge, "Cargo no line carries must touch; an overflowing layout says so");
        check(Why(f => { f.StoreOpenPipe = false; return f; }) == ReachProblem.NoPipeAtStore && Why(f => { f.StoreOpenPipe = false; f.StoreClosedPipe = true; return f; }) == ReachProblem.DrainedAtStore,
            "No pipe at the store, or only a drained one");
        check(Why(f => { f.MachineOpenPipe = false; return f; }) == ReachProblem.NoPipeAtMachine && Why(f => { f.MachineOpenPipe = false; f.MachineClosedPipe = true; return f; }) == ReachProblem.DrainedAtMachine &&
            Why(f => { f.StoreOpenPipe = false; f.MachineOpenPipe = false; return f; }) == ReachProblem.NoPipeAtStore, "The machine's end is checked after the store's");
        // The wrong kind of pipe at an end (Framework 0.81.0): an irrigation conduit beside a process-water intake is named,
        // once per family; the family asked about, and pipe off the join cells, are not.
        var ring = new[] { 4, 5, 6 };
        var lines = new (string, IReadOnlyDictionary<int, string>)[]
        {
            ("water", new Dictionary<int, string> { [5] = "own" }),
            ("irrigation", new Dictionary<int, string> { [5] = "conduit", [6] = "conduit-2" }),
            ("acid", new Dictionary<int, string> { [9] = "far" })
        };
        var foreign = LinkDiagnosis.ForeignSegments(ring, "water", lines);
        check(foreign.Count == 1 && foreign[0] == "conduit", "Another line under or beside the end is named once; its own line and pipe further off are not");
        check(LinkDiagnosis.ForeignSegments(ring, "irrigation", lines).SequenceEqual(new[] { "own" }), "Asked from the other side, the process-water line is the foreign one");

        var joined = FluidTopology.Build(6, 3, 3, new[] { 8, 9, 10 }, 4096, ports, new[] { (0, 1) });
        check(joined.ParticipantsConnected(0, 2) && joined.Hops(0, 2) == 5, "Touching joins as if piped, and joins chain through the network");
        check(!joined.ParticipantsConnected(3, 0), "An untouched participant stays apart");
        var chain = FluidTopology.Build(6, 3, 0, Array.Empty<int>(), 4096, ports, new[] { (0, 3), (3, 2) });
        check(chain.ParticipantsConnected(0, 2) && chain.Hops(0, 2) == 2, "A chain of touching participants is a network without any pipe");
        var junk = FluidTopology.Build(6, 3, 0, Array.Empty<int>(), 4096, ports, new[] { (0, 0), (-1, 2), (1, 9) });
        check(Enumerable.Range(0, 4).All(k => Enumerable.Range(0, 4).All(j => k == j || !junk.ParticipantsConnected(k, j))), "Self, negative and unknown joins are ignored");

        var plain = FluidTopology.Build(6, 3, 3, new[] { 8, 9, 10 });
        check(plain.ParticipantCount == 0 && plain.Connected(8, 10) && plain.Path(8, 10)!.Length == 3 && plain.ComponentOf(9) == plain.ComponentOf(8),
            "A family without participants keeps its cell components and routes");
        var overflow = FluidTopology.Build(6, 3, 5000, new[] { 8, 9, 10 }, 4096, ports, new[] { (0, 1) });
        check(!overflow.ParticipantsConnected(1, 2) && !overflow.ParticipantsConnected(0, 1), "An overflowing layout trusts no network, as before");
        // What a segment joins (Framework 0.60.0): every participant on its run, including one that only touches.
        check(piped.ParticipantsOn(9).SequenceEqual(new[] { 1, 2 }) && joined.ParticipantsOn(8).SequenceEqual(new[] { 0, 1, 2 }),
            "A segment's network lists the participants at its ports and those touching them");
        check(!piped.ParticipantsOn(7).Any() && !overflow.ParticipantsOn(9).Any(), "A cell without pipe, or an overflowing layout, joins no one");
        // Conveyor belts (Framework 0.61.0): a run joins two pieces of equipment when it lies on or beside the cells of each.
        check(plain.JoinsNear(new[] { 7 }, new[] { 11 }) && plain.JoinsNear(new[] { 7 }, new[] { 16 }) && plain.JoinsNear(new[] { 9 }, new[] { 9 }),
            "A belt run beside, or under, both ends joins them");
        check(!plain.JoinsNear(new[] { 0 }, new[] { 11 }) && !plain.JoinsNear(new[] { 7 }, new[] { 5 }) && !plain.JoinsNear(Array.Empty<int>(), new[] { 11 }),
            "An end with no belt on or beside it is not joined");
        var wrap = FluidTopology.Build(6, 3, 1, new[] { 5 });
        check(!wrap.JoinsNear(new[] { 6 }, new[] { 4 }) && wrap.JoinsNear(new[] { 4 }, new[] { 11 }), "A belt at the end of one row is not beside the start of the next");
        var twoRuns = FluidTopology.Build(6, 3, 2, new[] { 0, 5 });
        check(!twoRuns.JoinsNear(new[] { 1 }, new[] { 4 }) && !overflow.JoinsNear(new[] { 7 }, new[] { 11 }), "Two separate runs, or an overflowing layout, join nothing");
        // A bund holds a spill: service moves into the catch chamber, total and capacity unchanged.
        var acid = new StoredCommodity("sulfuric acid", 100); acid.SetService(40);
        double moved = acid.Contain(1.5);
        check(moved == 1.5 && acid.ServiceKg == 38.5 && acid.CatchKg == 1.5 && acid.TotalKg == 40, "Containment moves the spill from service into the catch chamber");
        check(acid.Contain(100) == 38.5 && acid.ServiceKg == 0 && acid.CatchKg == 40 && acid.Contain(1) == 0, "Containment never moves more than is in service");

        // Receiver banks: one store, eight machines; the ninth is refused; slot zero is the old port.
        var store = new Dictionary<string, Dictionary<string, string>>();
        var bank = new PortBank("store", "example.In", store, 8, PortRole.Receiver);
        var machines = Enumerable.Range(0, 8).Select(n => new MaterialPort("machine" + n, "example.Out", new())).ToList();
        foreach (var m in machines) check(bank.TryLinkPeer(m, null, out _), "A receiver bank accepts eight senders: " + m.ObjectId);
        check(bank.Ports[0].PortId == "example.In" && bank.Ports[7].PortId == "example.In.7", "Slot zero keeps the original port id");
        check(PortPairing.Matches(machines[0], bank.Ports[0]) && bank.For(machines[5]) != null, "Each machine sends to its own slot");
        check(!bank.TryLinkPeer(new MaterialPort("ninth", "example.Out", new()), null, out _), "A ninth machine is refused while every slot is live");
        check(bank.TryLinkPeer(new MaterialPort("ninth", "example.Out", new()), link => link.PeerObjectId == "machine3", out _) && bank.For(machines[3]) == null,
            "A slot whose machine no longer points back is reclaimed for a new one");
        check(bank.TryLinkPeer(machines[1], null, out _) && bank.Ports.Count(p => PortPairing.Read(p).PeerObjectId == "machine1") == 1, "Relinking a linked machine keeps its one slot");
        var legacy = new Dictionary<string, Dictionary<string, string>>();
        var oldMachine = new MaterialPort("old", "example.Out", new());
        check(PortPairing.TryLink(oldMachine, new MaterialPort("store2", "example.In", legacy), out _), "A link saved before banks existed");
        check(new PortBank("store2", "example.In", legacy, 8, PortRole.Receiver).For(oldMachine)?.PortId == "example.In", "reads as slot zero of the bank with no rewrite");
        foreach (var (id, expected) in new[] { ("example.In", true), ("example.In.1", true), ("example.In.7", true), ("example.In.8", false), ("example.In.0", false),
                     ("example.In.07", false), ("example.In.x", false), ("example.In.", false), ("other.In.2", false), (null, false) })
            check(PortBank.IsSlot(id, "example.In", 8) == expected, "Slot id rule: " + (id ?? "null"));
        // The sender default still fans out, as the W2 racks rely on.
        var fan = new PortBank("w2", "example.Water", new(), 2);
        check(fan.Role == PortRole.Sender && fan.TryLink(new MaterialPort("rack", "example.Water", new()), out _), "Sender banks keep their behaviour");

        // Framework 0.57.0 port rule: water on the -X side, gas on the +X side, both in the middle row; acid one row lower.
        check(LinePorts.Gas(2) == (24, 8, 1) && LinePorts.Gas(3) == (32, 0, 5) && LinePorts.Gas(4) == (40, 8, 7) && LinePorts.Gas(5) == (48, 0, 14),
            "The gas port is the neighbouring tile on the +X side in the middle row (the stores' original outlet)");
        check(LinePorts.Water(2) == (-24, 8, 0) && LinePorts.Water(3) == (-32, 0, 3) && LinePorts.Water(4) == (-40, 8, 4) && LinePorts.Water(5) == (-48, 0, 10),
            "The water port mirrors it on the -X side");
        check(LinePorts.Acid(2) == (24, -8, 3) && LinePorts.Acid(3) == (32, -16, 8) && LinePorts.Acid(4) == (40, -8, 11), "The acid port sits one row below the gas port");
        check(LinePorts.Ethanol(2) == (-24, -8, 2) && LinePorts.Ethanol(3) == (-32, -16, 6) && LinePorts.Ethanol(4) == (-40, -8, 8), "The ethanol port sits on the -X side one row below the water port");
        bool ethanolRefused = false; try { LinePorts.Ethanol(1); } catch (ArgumentOutOfRangeException) { ethanolRefused = true; }
        check(ethanolRefused, "A one-tile machine has no ethanol port");
        bool acidRefused = false; try { LinePorts.Acid(1); } catch (ArgumentOutOfRangeException) { acidRefused = true; }
        check(acidRefused, "A one-tile footprint has no row below the middle for an acid port");
        LinePorts.Register("example.family", "ExampleInstalled", "ExamplePoint");
        LinePorts.Register("example.family", "ExampleInstalled", "ExamplePoint");
        check(LinePorts.Points("example.family", "ExampleInstalled").Count == 1 && LinePorts.Points("example.family", "Other").Count == 0 && LinePorts.Points("none", null).Count == 0,
            "Port registration is idempotent and lookups never fail");
        check(LineCommodities.For(LineCommodities.Water) == LineFamilies.ProcessWaterId && LineCommodities.For("unknown commodity") == null && LineCommodities.For(null) == null,
            "Water rides the process-water line; an unassigned commodity links by touching only");
        LineCommodities.Assign("example gas", LineFamilies.GasId); LineCommodities.Assign("example gas", LineFamilies.GasId);
        bool conflict = false; try { LineCommodities.Assign("example gas", LineFamilies.ProcessWaterId); } catch (ArgumentException) { conflict = true; }
        check(LineCommodities.For("example gas") == LineFamilies.GasId && conflict, "A commodity is carried by one line; the same assignment twice is harmless, a different one is refused");
        GasNetworkSafety.Classify("example oxidiser", GasHazardClass.Oxidiser);
        check(GasNetworkSafety.ClassOf("example oxidiser") == GasHazardClass.Oxidiser && GasNetworkSafety.ClassOf("unclassed") == GasHazardClass.None, "Gas classes are content-declared");
    }
}
