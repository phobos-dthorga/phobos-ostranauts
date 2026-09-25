using System;
using System.Collections.Generic;
using System.Linq;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Registration;

internal static class DependencyChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        check(DependencyContract.FrameworkProblem(null) != null, "Missing loaded framework is reported");
        check(DependencyContract.FrameworkProblem(new Version(0, 1, 9)) != null, "Too-old API baseline is blocked");
        check(DependencyContract.FrameworkProblem(new Version(0, 1, 0)) != null, "An old Phobos provider cannot satisfy new construction APIs");
        check(DependencyContract.FrameworkProblem(new Version(0, 2, 1)) != null, "Previous framework lacks the physical transfer API");
        check(DependencyContract.FrameworkProblem(new Version(0, 3, 0)) != null, "Collector needs shared filter, clock and route helpers");
        check(DependencyContract.FrameworkProblem(new Version(0, 4, 0)) != null, "Saved pairing needs the shared port API");
        check(DependencyContract.FrameworkProblem(new Version(0, 6, 0)) != null, "Translation-aware equipment requires the shared localization API");
        check(DependencyContract.FrameworkProblem(new Version(0, 8, 0)) != null, "Saved routing filters require Framework 0.9");
        check(DependencyContract.FrameworkProblem(new Version(0, 9, 0)) != null, "Industrial controls require Framework 0.10");
        check(DependencyContract.FrameworkProblem(new Version(0, 11, 0)) != null, "Branded equipment needs Framework 0.12 naming patterns");
        check(DependencyContract.FrameworkProblem(new Version(0, 12, 0)) != null, "Console observations require Framework 0.13");
        check(DependencyContract.FrameworkProblem(new Version(0, 14, 0)) != null, "Performance handles require Framework 0.15");
        check(DependencyContract.FrameworkProblem(new Version(0, 15, 0)) != null, "Furnace receipts and native instruments require Framework 0.16");
        check(DependencyContract.FrameworkProblem(new Version(0, 16, 0)) != null, "Guarded control and digit adapters require Framework 0.17");
        check(DependencyContract.FrameworkProblem(new Version(0, 18, 0)) != null, "Thermal route endpoint support requires Framework 0.19");
        check(DependencyContract.FrameworkProblem(new Version(0, 19, 0)) != null, "Combined fluid and material release requires Framework 0.20");
        check(DependencyContract.FrameworkProblem(new Version(0, 20, 0)) != null, "Shared completion cues require Framework 0.21");
        check(DependencyContract.FrameworkProblem(new Version(0, 21, 0)) != null, "Saved-grid mitigation requires Framework 0.21.1");
        check(DependencyContract.FrameworkProblem(new Version(0, 21, 1)) != null, "Optional collector admission and reject reservations require Framework 0.22");
        check(DependencyContract.FrameworkProblem(new Version(0, 22, 0)) != null, "Regional retail endpoints require Framework 0.23");
        foreach (var version in new[] { new Version(DependencyContract.MinimumFramework), new Version(0, 23, 1), new Version(1, 0, 0) })
            check(DependencyContract.FrameworkProblem(version) == null, "No invented upper version or age cutoff: " + version);

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

        check(DependencyContract.MissingRecipes(_ => false).Count == 18, "All eighteen construction, finishing and recovery recipes must register");
        check(DependencyContract.MissingRecipes(id => id == DependencyContract.Recipes[0]).Count == 17, "A partially registered construction chain is blocked");
        check(DependencyContract.MissingRecipes(id => DependencyContract.Recipes.Contains(id)).Count == 0, "All construction stages are available");

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
