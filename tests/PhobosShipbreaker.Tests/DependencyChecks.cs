using System;
using System.Collections.Generic;
using System.Linq;
using PhobosShipbreaker.Core;

internal static class DependencyChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        check(DependencyContract.FrameworkProblem(null, true) != null, "Missing loaded framework is reported");
        check(DependencyContract.FrameworkProblem(new Version(0, 8, 70), true) != null, "Too-old API baseline is blocked");
        check(DependencyContract.FrameworkProblem(new Version(0, 8, 71), false) != null, "Plugin presence does not substitute for enabled native data");
        foreach (var version in new[] { new Version(0, 8, 71), new Version(0, 8, 72), new Version(1, 0, 0) })
            check(DependencyContract.FrameworkProblem(version, true) == null, "No invented upper version or age cutoff: " + version);

        var tables = DependencyContract.Required.ToDictionary(g => g.Table, g => g.Names.ToHashSet());
        bool Contains(string table, string id) => tables.TryGetValue(table, out var entries) && entries.Contains(id);
        check(DependencyContract.MissingDefinitions(Contains).Count == 0, "Complete template contract passes");
        foreach (var group in DependencyContract.Required)
        foreach (string id in group.Names)
        {
            tables[group.Table].Remove(id);
            var errors = DependencyContract.MissingDefinitions(Contains);
            check(errors.Count == 1 && errors[0].Contains(group.Table + ": " + id), "Missing definition has an actionable identity: " + id);
            tables[group.Table].Add(id);
        }

        const string installed = "SWB_SorterInstalled";
        foreach (string variant in DependencyContract.Variants)
        {
            string id = "SWB_Sorter" + variant;
            bool powered = id == installed;
            check(DependencyContract.MachineProblems(id, id, "SWB_SorterCompartments", new[] { "SWB_SorterInput" },
                powered ? "SWB_SorterPower" : null, powered ? new[] { "Power" } : null, true).Count == 0,
                "Baseline machine variant passes: " + id);
        }
        check(DependencyContract.MachineProblems(installed, "ChangedItem", "ChangedCompartments", new[] { "SWB_SorterInput", "ExtraSlot" },
            "ChangedPower", new[] { "Power", "NewAutomation" }, false).Count == 6,
            "Changed item, storage, power, tickers and container relationships are all reported");
        check(DependencyContract.MachineProblems(installed, installed, "SWB_SorterCompartments", null, null, null, true).Count == 3,
            "Missing input, power and ticker are rejected before cloning");
        check(DependencyContract.MissingRecipes(_ => false).Count == 2, "Neither registered construction recipe is accepted as ready");
        check(DependencyContract.MissingRecipes(id => id == DependencyContract.Recipes[0]).Count == 1, "A partially registered construction chain is blocked");
        check(DependencyContract.MissingRecipes(id => DependencyContract.Recipes.Contains(id)).Count == 0, "Both construction stages are required");

        var first = new Dictionary<string, int> { ["existing"] = 10, ["foreign"] = 20 };
        var second = new FaultingDictionary { ["existing"] = 30 };
        var third = new Dictionary<string, string> { ["existing"] = "original" };
        var batch = new DefinitionTransaction();
        batch.Stage(first, new Dictionary<string, int> { ["existing"] = 11, ["new"] = 12 });
        batch.Stage(third, new Dictionary<string, string> { ["existing"] = "changed", ["new"] = "new value" });
        batch.Stage((IDictionary<string, int>)second, new Dictionary<string, int> { ["existing"] = 31 });
        check(first.Count == 2 && first["existing"] == 10 && third["existing"] == "original", "Preparation publishes nothing");
        second.FailOnKey = "existing";
        throws(batch.Commit, "A failure after assignment is surfaced");
        check(first.Count == 2 && first["existing"] == 10 && first["foreign"] == 20 && second.Count == 1 && second["existing"] == 30
            && third.Count == 1 && third["existing"] == "original", "Rollback restores old entries, removes new entries and preserves foreign definitions across tables");
        throws(batch.Commit, "A failed transaction cannot accidentally be replayed");

        // A fresh successful registration can replace our entries repeatedly without duplication.
        for (int pass = 0; pass < 2; pass++)
        {
            var success = new DefinitionTransaction();
            success.Stage(first, new Dictionary<string, int> { ["existing"] = 11, ["new"] = 12 });
            success.Stage((IDictionary<string, int>)second, new Dictionary<string, int> { ["existing"] = 31 });
            success.Commit();
            check(first.Count == 3 && first["existing"] == 11 && first["new"] == 12 && first["foreign"] == 20 && second["existing"] == 31,
                "Successful registration supports a repeat data load");
            throws(() => success.Stage(first, first), "Committed batch cannot accept more writes");
        }
        check(Command.Parse("PHOBOSSHIPBREAKER dependencies").Action == CommandAction.Dependencies, "Dependency diagnostics use the shared console parser");
        check(Command.Parse("phobosshipbreaker dependencies fixture-id").Action == CommandAction.Invalid, "Dependency diagnostics do not accept a mutation target");
    }

    private sealed class FaultingDictionary : Dictionary<string, int>, IDictionary<string, int>
    {
        internal string? FailOnKey;
        int IDictionary<string, int>.this[string key]
        {
            get => base[key];
            set
            {
                base[key] = value;
                if (FailOnKey != key) return;
                FailOnKey = null;
                throw new InvalidOperationException("Injected failure after writing a definition");
            }
        }
    }
}
