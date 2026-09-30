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
        check(!piped.ParticipantsConnected(0, 1) && !piped.ParticipantsConnected(3, 1), "A port beside the pipe, or far from it, joins nothing");
        check(piped.Hops(0, 1) == -1, "No network, no steps");

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
