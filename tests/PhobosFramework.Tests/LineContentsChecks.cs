using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;

/// <summary>Framework 0.63.0 lines that hold their contents: realistic hold-up per tile, any mix of gases in a gas line,
/// top-up from the stores' available contents and draining nearest first (owner decisions, 1 October 2026).</summary>
internal static class LineContentsChecks
{
    internal static void Run(Action<bool, string> check)
    {
        bool Near(double a, double b, double tolerance) => Math.Abs(a - b) <= tolerance;
        check(Near(LineGeometry.VolumeM3(25) * 1000, 0.4909, 1e-4), "A metre of 25 mm bore holds 0.49 litres");
        var water = LineCommodity.Liquid("water", 998.2);
        var acid = LineCommodity.Liquid("sulfuric acid", 1836, mistSpecies: "H2SO4", mistFraction: 1e-4);
        check(Near(water.KgPerTile, 0.490, 1e-3) && Near(acid.KgPerTile, 0.901, 1e-3), "Water and 98% acid hold-ups per tile follow their densities");
        check(Near(water.CanisterKg, 19.964, 1e-9) && Near(acid.CanisterKg, 36.72, 1e-9), "A 20 litre canister holds its liquid by density");
        check(Near(acid.MistKg(0.9), 0.9e-4, 1e-15) && water.MistKg(0.9) == 0, "Only a liquid with a declared mist releases one");
        var oxygen = LineCommodity.GasOf("oxygen", "O2");
        var hydrogen = LineCommodity.GasOf("hydrogen", null, 0.0020159);
        check(Near(oxygen.KgPerTile * 1000, 6.45, 0.01) && Near(hydrogen.KgPerTile * 1000, 0.406, 0.001),
            "Gas hold-up is grams a tile at the 10 bar line pressure (ideal gas, the game's constant and molar masses)");
        check(oxygen.CanisterKg == 0 && oxygen.RoomSpecies == "O2" && hydrogen.RoomSpecies == null, "Gases vent: native species to the room, hydrogen overboard");
        bool refused = false;
        try { LineCommodity.GasOf("helium", "He2"); } catch (ArgumentException) { refused = true; }
        check(refused, "A gas the game gives no room condition must name its molar mass and vents overboard");

        // Records.
        var mixed = new LineMixture { Closed = true };
        mixed.Add("carbon dioxide", 0.004); mixed.Add("oxygen", 0.002);
        var back = LineMixture.Read(mixed.Save());
        check(back.Closed && back.Of("carbon dioxide") == 0.004 && back.Of("oxygen") == 0.002 && back.Kilograms.Count == 2, "A mixed record, closed, reads back exactly");
        check(LineMixture.Read(new LineMixture().Save()) is { Closed: false, Empty: true }, "An empty open record reads back empty and open");
        foreach (var bad in new[] { new Dictionary<string, string>(), new Dictionary<string, string> { ["state"] = "ajar" },
                     new Dictionary<string, string> { ["state"] = "open", ["c0"] = "water" }, new Dictionary<string, string> { ["state"] = "open", ["c0"] = "water", ["kg0"] = "-1" } })
        {
            bool failed = false;
            try { LineMixture.Read(bad); } catch (ArgumentException) { failed = true; }
            check(failed, "An unknown, partial or negative line record is refused, never guessed");
        }

        // Filling a water run from 1 kg available: nearest segments first, never more than was available.
        LineCommodity? Water(string name) => name == "water" ? water : null;
        var run = Enumerable.Range(0, 3).Select(_ => new LineMixture()).ToArray();
        var available = new Dictionary<string, double> { ["water"] = 1.0 };
        var drawn = LinePlanner.Fill(run, available, Water);
        check(Near(drawn["water"], 1.0, 1e-12) && Near(available["water"], 0, 1e-12), "A run takes no more than the stores have available");
        check(Near(run[0].Of("water"), water.KgPerTile, 1e-12) && Near(run[1].Of("water"), water.KgPerTile, 1e-12) && Near(run[2].Of("water"), 1.0 - 2 * water.KgPerTile, 1e-12),
            "Segments fill in order and the last takes the remainder");
        check(LinePlanner.Fill(run.Take(2).ToArray(), new Dictionary<string, double> { ["water"] = 5 }, Water).Count == 0, "A full segment takes nothing");

        // A gas line holds any mix: an even share of the volume from each gas on offer, then any gas that still has some.
        LineCommodity? Gas(string name) => name == "oxygen" ? oxygen : name == "hydrogen" ? hydrogen : null;
        var gas = new[] { new LineMixture() };
        LinePlanner.Fill(gas, new Dictionary<string, double> { ["oxygen"] = 1, ["hydrogen"] = 1 }, Gas);
        check(Near(gas[0].Fraction(Gas), 1, 1e-9) && Near(gas[0].Of("oxygen") / oxygen.KgPerTile, 0.5, 1e-9), "Two gases share a segment's volume evenly");
        var scarce = new[] { new LineMixture() };
        LinePlanner.Fill(scarce, new Dictionary<string, double> { ["oxygen"] = 1, ["hydrogen"] = hydrogen.KgPerTile * 0.1 }, Gas);
        check(Near(scarce[0].Fraction(Gas), 1, 1e-9) && Near(scarce[0].Of("hydrogen"), hydrogen.KgPerTile * 0.1, 1e-15), "A scarce gas gives what it has and another gas fills the rest");

        // Draining nearest first into the room a canister has left.
        var full = Enumerable.Range(0, 3).Select(_ => { var m = new LineMixture(); m.Add("water", 0.49); return m; }).ToArray();
        var taken = LinePlanner.Drain(full, "water", 1.0);
        check(Near(taken[0], 0.49, 1e-12) && Near(taken[1], 0.49, 1e-12) && Near(taken[2], 0.02, 1e-12) && Near(full[2].Of("water"), 0.47, 1e-12),
            "A drain takes from the nearest segments first and stops at the canister's room");
        check(Near(taken.Sum() + full.Sum(m => m.TotalKg), 1.47, 1e-12), "Draining moves mass; it never creates or loses any");
    }
}
