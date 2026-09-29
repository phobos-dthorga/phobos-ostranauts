using System;
using System.Collections.Generic;
using System.Linq;
using PhobosManufacturing.Core;

/// <summary>The P1 propellant manifold's record and its conversions, on numbers alone.</summary>
internal static class ManifoldChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        // Every store commodity is worth its gas; unknown commodities keep the nitrogen worth.
        check(Math.Abs(ManifoldRules.Ratio("methane") - 1.45) < 0.02 && Math.Abs(ManifoldRules.Ratio("hydrogen") - 3.69) < 0.03 && ManifoldRules.Ratio("water") == 1,
            "Methane and hydrogen are worth their cold-gas figures; anything else is plain mass");
        check(Math.Abs(ManifoldRules.KilogramsFor("methane", ManifoldRules.EquivalentKg("methane", 7.5)) - 7.5) < 1e-12, "Kilograms and nitrogen-equivalent round-trip");
        check(ManifoldRules.KilogramsFor("hydrogen", 10) < 3, "A hydrogen store gives ten kilograms of push for under three of its own");

        var s = new ManifoldState();
        check(!s.On && !s.First && s.Sources.Count == 0, "A new manifold is off, draws after the canisters, and has no stores");
        check(s.Link("PhobosMethaneStoreInstalledA") && !s.Link("PhobosMethaneStoreInstalledA"), "A store links once");
        check(!s.Sources[0].Enabled, "A newly linked store starts switched off");
        check(s.Link("B") && s.Link("C") && s.Link("D") && !s.Link("E"), "At most four stores");
        check(!s.Link("bad;id") && !s.Link("bad|id") && !s.Link(""), "Unsafe ids are refused");
        check(s.Switch("B", true) && s.Sources.Single(x => x.Id == "B").Enabled && !s.Switch("Z", true), "Stores switch individually");
        check(s.Unlink("D") && !s.Unlink("D") && s.Sources.Count == 3, "A store unlinks once");
        s.On = true; s.First = true;
        var round = ManifoldState.Read(s.Save());
        check(round.On && round.First && round.Sources.Select(x => x.Id + x.Enabled).SequenceEqual(s.Sources.Select(x => x.Id + x.Enabled)), "The record round-trips in order");
        check(ManifoldState.Read(new ManifoldState().Save()).Sources.Count == 0, "An empty source list round-trips");
        foreach (var bad in new[] {
            new Dictionary<string, string> { ["on"] = "yes" }, new Dictionary<string, string> { ["sources"] = "A|2" },
            new Dictionary<string, string> { ["sources"] = "A|1;A|0" }, new Dictionary<string, string> { ["sources"] = "A|1;B|1;C|1;D|1;E|1" },
            new Dictionary<string, string> { ["unknown"] = "1" }, new Dictionary<string, string> { ["sources"] = "A" } })
            throws(() => ManifoldState.Read(bad), "A corrupt record is refused: " + string.Join(",", bad.Select(p => p.Key + "=" + p.Value)));
        check(ManifoldRules.IsFamily("PhobosPropellantManifoldInstalledDmg") && !ManifoldRules.IsFamily("PhobosPropellantLineInstalled"), "Manifold family identity");
        check(PropellantLineRules.IsSegment("PhobosPropellantLineInstalled") && !PropellantLineRules.IsSegment("PhobosPropellantLineInstalledDmg") &&
            !PropellantLineRules.IsSegment("PhobosVerdemorrowWaterConduitInstalled") && !PropellantLineRules.IsSegment("PhobosFurnaceCoolantConduitInstalled"),
            "Only intact propellant line carries propellant; coolant and irrigation lines never do");
    }
}
