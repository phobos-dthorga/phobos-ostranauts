using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Carved loot shares on native expressions alone: a carve takes probability from its donor and
/// sits right after it, so every other unit keeps its band and the total never changes.</summary>
internal static class LootCarveChecks
{
    private static Dictionary<string, LootCarve> Carves(params (string Choice, string Donor, double Share)[] c) =>
        c.ToDictionary(x => x.Choice, x => new LootCarve(x.Donor, x.Share), StringComparer.Ordinal);

    // The cumulative band [start, end) of every unit, in expression order, as the game rolls them.
    private static List<(string Name, double Start, double End)> Bands(string expression)
    {
        var bands = new List<(string, double, double)>(); double at = 0;
        foreach (string unit in expression.Split('|'))
        {
            var bits = unit.Split('='); double chance = double.Parse(bits[1].Split('x')[0], CultureInfo.InvariantCulture);
            bands.Add((bits[0], at, at + chance)); at += chance;
        }
        return bands;
    }

    internal static void Run(Action<bool, string> check)
    {
        // The game's C-class deposit table, verbatim.
        var cClass = new[] { "ItmMineral03=0.37x1|ItmMineral04=0.4x1|ItmMineral11=0.1x1|ItmIce01=0.1x1|ItmMineral79=0.03x1" };
        var one = LootCarveMath.Render(cClass, Carves(("PhobosClayHydrates", "ItmMineral04", 0.1)), out var refused);
        check(refused.Count == 0 && one[0] == "ItmMineral03=0.37x1|ItmMineral04=0.3x1|PhobosClayHydrates=0.1x1|ItmMineral11=0.1x1|ItmIce01=0.1x1|ItmMineral79=0.03x1",
            "A carve reduces its donor and sits immediately after it");
        var before = Bands(cClass[0]); var after = Bands(one[0]);
        foreach (var b in before.Where(b => b.Name != "ItmMineral04"))
        {
            var a = after.Single(x => x.Name == b.Name);
            check(Math.Abs(a.Start - b.Start) < 1e-9 && Math.Abs(a.End - b.End) < 1e-9, "Every other unit keeps its band: " + b.Name);
        }
        check(Math.Abs(after.Last().End - before.Last().End) < 1e-9, "The table's total probability is unchanged");

        // Two sets carve the same donor; the native ice unit is a separate, untouched unit of the same name.
        var both = LootCarveMath.Render(cClass, Carves(("PhobosClayHydrates", "ItmMineral04", 0.1), ("ItmIce01", "ItmMineral04", 0.05)), out refused);
        var reversed = LootCarveMath.Render(cClass, Carves(("ItmIce01", "ItmMineral04", 0.05), ("PhobosClayHydrates", "ItmMineral04", 0.1)), out _);
        check(refused.Count == 0 && both.SequenceEqual(reversed), "Carves on one donor compose the same way whatever order they were registered in");
        check(both[0] == "ItmMineral03=0.37x1|ItmMineral04=0.25x1|ItmIce01=0.05x1|PhobosClayHydrates=0.1x1|ItmMineral11=0.1x1|ItmIce01=0.1x1|ItmMineral79=0.03x1",
            "Composed carves follow the donor in ordinal order and leave the native ice unit alone");
        double waterIce = Bands(both[0]).Where(b => b.Name == "ItmIce01").Sum(b => b.End - b.Start);
        check(Math.Abs(waterIce - 0.15) < 1e-9, "A carved choice adds to a native unit of the same name");

        // Idempotent and restorable: rendering always starts from the native original.
        check(LootCarveMath.Render(cClass, Carves(("PhobosClayHydrates", "ItmMineral04", 0.1)), out _).SequenceEqual(one), "Rendering twice gives the same table");
        check(LootCarveMath.Render(cClass, Carves(("PhobosClayHydrates", "ItmMineral04", 0)), out refused).SequenceEqual(cClass) && refused.Count == 0, "A zero share restores the native table exactly");

        // Refusals: overdraw, missing and ambiguous donors leave the table's other odds intact.
        var over = LootCarveMath.Render(cClass, Carves(("A", "ItmMineral79", 0.02), ("B", "ItmMineral79", 0.02)), out refused);
        check(refused.Count == 1 && refused[0].Choice == "B" && refused[0].Reason == "insufficient" && over[0].Contains("ItmMineral79=0.01x1|A=0.02x1"),
            "A donor never goes below zero; the carve it cannot cover is refused");
        LootCarveMath.Render(cClass, Carves(("A", "ItmNothing", 0.1)), out refused);
        check(refused.Single().Reason == "missing", "A missing donor is refused");
        var twice = new[] { "A=0.5x1|B=0.5x1", "A=0.2x1" };
        check(LootCarveMath.Render(twice, Carves(("C", "A", 0.1)), out refused).SequenceEqual(twice) && refused.Single().Reason == "ambiguous", "A donor in two expressions is ambiguous");
        var negative = new[] { "-A=1x1", "B=0.5x1" };
        check(LootCarveMath.Render(negative, Carves(("C", "A", 0.1)), out refused).SequenceEqual(negative) && refused.Single().Reason == "ambiguous", "A donor inside a negative expression is never carved");

        // Ship tables: the asteroid-field picker keeps its count text and every other cluster's odds.
        var fields = new[] { "ClusterC01=0.2x1|ClusterC02=0.2x1|ClusterC03=0.2x1|ClusterC04=0.2x1|ClusterS01=0.15x1|ClusterM01=0.05x1" };
        var ice = LootCarveMath.Render(fields, Carves(("ClusterI01", "ClusterC02", 0.05)), out refused);
        check(refused.Count == 0 && ice[0] == "ClusterC01=0.2x1|ClusterC02=0.15x1|ClusterI01=0.05x1|ClusterC03=0.2x1|ClusterC04=0.2x1|ClusterS01=0.15x1|ClusterM01=0.05x1",
            "An asteroid cluster carves its share from one named cluster");
        var ranged = LootCarveMath.Render(new[] { "ItmMineralStone01=0.8x1-2|ItmMiningTrash=0.2x1" }, Carves(("X", "ItmMineralStone01", 0.3)), out _);
        check(ranged[0] == "ItmMineralStone01=0.5x1-2|X=0.3x1|ItmMiningTrash=0.2x1", "The donor keeps its own quantity range");

        // Invariant formatting under a comma-decimal culture.
        var culture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            check(LootCarveMath.Render(cClass, Carves(("PhobosClayHydrates", "ItmMineral04", 0.1)), out _).SequenceEqual(one), "Shares are written in the game's invariant format under any culture");
        }
        finally { Thread.CurrentThread.CurrentCulture = culture; }
    }
}
