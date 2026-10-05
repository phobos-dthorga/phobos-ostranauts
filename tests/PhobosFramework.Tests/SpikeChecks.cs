using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Liquids;

/// <summary>Framework 0.106.0: the two spikes the owner's 5 October captures found, the pipe layout rescan and the crew
/// discovery scan, now run in full only when something changed or as a 30-second safety net.</summary>
internal static class SpikeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var watched = new List<(string Item, bool Ready)> { ("rack", true), ("w2", true), ("locked-tank", false) };
        var now = new Dictionary<string, bool?> { ["rack"] = true, ["w2"] = true, ["locked-tank"] = false };
        bool Due(bool built, double since) => RouteRecheck.RebuildDue(built, since, watched, id => now[id]);
        check(Due(false, 0), "A ship never read is read in full");
        check(!Due(true, 2), "Nothing changed two seconds on: no rescan");
        check(Due(true, RouteRecheck.FullSeconds), "The safety-net rescan runs every 30 seconds");
        check(Due(true, double.NaN), "A clock that cannot be read rescans rather than trusting the layout");
        now["locked-tank"] = true;
        check(Due(true, 2), "A tank unlocked with nothing physical changing is noticed within two seconds");
        now["locked-tank"] = false; now["w2"] = false;
        check(Due(true, 2), "A W2 that stops being ready (locked, sold, unloaded) is noticed within two seconds");
        now["w2"] = null;
        check(Due(true, 2), "A watched object that is gone or left the ship forces a rescan");
        check(!RouteRecheck.RebuildDue(true, 5, new List<(string, bool)>(), _ => null), "A ship with no pipes or participants is not rescanned every two seconds");

        var orders = new Dictionary<string, StandingOrder>
        {
            ["rack-1"] = new StandingOrder { Permission = WorkPermission.Enabled },
            ["rack-2"] = new StandingOrder { Permission = WorkPermission.Disabled },
            ["furnace"] = new StandingOrder { Permission = WorkPermission.Suspended },
            ["broken"] = new StandingOrder { Permission = WorkPermission.Enabled, Protected = true },
            ["w2"] = new StandingOrder { Permission = WorkPermission.Enabled }
        };
        var quick = DiscoveryPlan.Quick(orders, new[] { "repaired-press", "w2", null! });
        check(quick.SequenceEqual(new[] { "rack-1", "w2", "repaired-press" }),
            "A quick pass visits switched-on readable orders and just-replaced machines, each once");
        check(DiscoveryPlan.Quick(new Dictionary<string, StandingOrder>(), Array.Empty<string>()).Count == 0, "No orders and no replacements: nothing to visit");
        check(DiscoveryPlan.FullScanDue(0, 0) && DiscoveryPlan.FullScanDue(31, 30) && !DiscoveryPlan.FullScanDue(29, 30),
            "The full scan runs after a load (next time zero) and every 30 seconds");
        check(DiscoveryPlan.FullScanDue(double.NaN, 30), "A clock that cannot be read runs the full scan");
    }
}
