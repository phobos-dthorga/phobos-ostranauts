using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

/// <summary>The outcomes data pack (Framework 0.88.0): the pick is stable and fair, and the rules every file passes.</summary>
internal static class OutcomeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        void Throws(Action action, string message) { bool failed = false; try { action(); } catch (ArgumentException) { failed = true; } check(failed, message); }
        var weights = new Dictionary<string, int> { ["wash"] = 50, ["wash-steel"] = 30, ["wash-aluminium"] = 15, ["wash-nickel"] = 5 };
        var lumps = new[] { "c3", "a1", "d4", "b2" };
        string first = Outcomes.Pick(weights, lumps);
        check(first == Outcomes.Pick(weights, lumps.Reverse()) && first == Outcomes.Pick(weights, new[] { "a1", "b2", "c3", "d4" }), "The same units give the same outcome in any order");
        check(Outcomes.Hash(new[] { "a1", "b2" }) != Outcomes.Hash(new[] { "a1b", "2" }) && Outcomes.Hash(new[] { "x" }) == Outcomes.Hash(new[] { "x" }), "The hash is stable and does not confuse joined ids");
        // Over many unit sets shaped like the game's ids, the shares land near the table.
        var counts = weights.Keys.ToDictionary(k => k, _ => 0);
        const int draws = 20000;
        for (int i = 0; i < draws; i++)
            counts[Outcomes.Pick(weights, Enumerable.Range(0, 4).Select(n => new Guid(i, (short)n, 7, 1, 2, 3, 4, 5, 6, 7, 8).ToString()))]++;
        check(weights.All(w => Math.Abs(counts[w.Key] / (double)draws - w.Value / 100.0) < 0.02), "Twenty thousand washes land within two points of 50, 30, 15 and 5 in a hundred: " +
            string.Join(", ", counts.OrderBy(c => c.Key).Select(c => c.Key + " " + c.Value)));
        var off = new Dictionary<string, int> { ["wash"] = 0, ["wash-steel"] = 7 };
        check(Enumerable.Range(0, 200).All(i => Outcomes.Pick(off, new[] { "u" + i }) == "wash-steel"), "An outcome with a weight of 0 is never picked");
        Throws(() => Outcomes.Pick(new Dictionary<string, int> { ["wash"] = 0 }, lumps), "A table with no weight cannot be picked from");

        var recipes = new Dictionary<string, OutcomeRecipe>
        {
            ["wash"] = new("leach", "sig"), ["wash-steel"] = new("leach", "sig"), ["other-inputs"] = new("leach", "other"), ["other-machine"] = new("refinery", "sig"), ["second"] = new("leach", "sig")
        };
        OutcomeRecipe? Find(string id) => recipes.TryGetValue(id, out var r) ? r : null;
        OutcomePack Pack(params (string Base, (string Id, int Weight)[] Outcomes)[] tables)
        {
            var pack = new OutcomePack();
            foreach (var t in tables) pack.tables[t.Base] = new OutcomeTable { outcomes = t.Outcomes.ToDictionary(o => o.Id, o => o.Weight) };
            return pack;
        }
        OutcomeSchema.Validate(Pack(("wash", new[] { ("wash", 50), ("wash-steel", 30) })), Find);
        OutcomeSchema.Validate(Pack(("wash", new[] { ("wash", 0), ("wash-steel", 1) })), Find);
        Throws(() => OutcomeSchema.Validate(Pack(("wash", new[] { ("wash-steel", 30) })), Find), "A table must list its own base recipe");
        Throws(() => OutcomeSchema.Validate(Pack(("missing", new[] { ("missing", 1) })), Find), "A table's base must be a recipe");
        Throws(() => OutcomeSchema.Validate(Pack(("wash", new[] { ("wash", 1), ("nowhere", 1) })), Find), "An outcome must be a recipe");
        Throws(() => OutcomeSchema.Validate(Pack(("wash", new[] { ("wash", 1), ("other-inputs", 1) })), Find), "An outcome with a different charge is refused");
        Throws(() => OutcomeSchema.Validate(Pack(("wash", new[] { ("wash", 1), ("other-machine", 1) })), Find), "An outcome on another machine is refused");
        Throws(() => OutcomeSchema.Validate(Pack(("wash", new[] { ("wash", 0), ("wash-steel", 0) })), Find), "A table with no weight at all is refused");
        Throws(() => OutcomeSchema.Validate(Pack(("wash", new[] { ("wash", 1), ("wash-steel", -1) })), Find), "A negative weight is refused");
        Throws(() => OutcomeSchema.Validate(Pack(("wash", new[] { ("wash", 1), ("wash-steel", OutcomeSchema.MaxWeight + 1) })), Find), "A weight above the limit is refused");
        Throws(() => OutcomeSchema.Validate(Pack(("wash", new[] { ("wash", 1), ("wash-steel", 1) }), ("second", new[] { ("second", 1), ("wash-steel", 1) })), Find), "A recipe sits in at most one table");

        // Player files merge by key: retune one weight, add an outcome; an unknown field or a bad table is refused and the shipped table stands.
        string shipped = "{\"schemaVersion\":1,\"schema\":\"outcomes\",\"tables\":{\"wash\":{\"outcomes\":{\"wash\":50,\"wash-steel\":30}}}}";
        string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "phobos-outcomes-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(folder);
        try
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "a-tune.json"), "{\"tables\":{\"wash\":{\"outcomes\":{\"wash-steel\":70,\"second\":5}}}}");
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "b-bad.json"), "{\"tables\":{\"wash\":{\"outcomes\":{\"other-inputs\":5}}}}");
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "c-field.json"), "{\"tables\":{\"wash\":{\"odds\":{}}}}");
            int before = DataPacks.Problems.Count;
            var merged = DataPacks.LoadText<OutcomePack>(shipped, folder, "test", OutcomeSchema.Name, p => OutcomeSchema.Validate(p, Find));
            var table = merged.tables["wash"].outcomes;
            check(table["wash"] == 50 && table["wash-steel"] == 70 && table["second"] == 5 && !table.ContainsKey("other-inputs"), "A player file retunes one weight and adds an outcome; the rest of the shipped table stands");
            check(DataPacks.Problems.Count == before + 2, "A player file with a mismatched outcome, or an unknown field, is refused with its reason");
        }
        finally { System.IO.Directory.Delete(folder, true); }
    }
}
