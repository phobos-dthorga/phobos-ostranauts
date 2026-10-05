using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>Which equipment a crew discovery pass looks at (Framework 0.106.0; owner performance review, 5 October
/// 2026). Until 0.106.0 every pass, every two seconds, scanned every object on every crew ship to find equipment with a
/// provider: about 4,800 objects and up to 27 ms on the owner's ship. The scan is what loads each machine's saved order,
/// and once loaded an order lives in memory, so most passes need only the machines whose order is switched on and any
/// machine a mode switch has just replaced (damage, repair, installation). The full scan still runs after a load and
/// every <see cref="FullSeconds"/>, to load orders for equipment that comes aboard or comes into reach. Pure.</summary>
internal static class DiscoveryPlan
{
    internal const double FullSeconds = 30;

    internal static bool FullScanDue(double now, double nextFull) => !(now < nextFull);

    /// <summary>The equipment ids a quick pass visits: every loaded order that is switched on and readable, then the
    /// replaced objects queued since the last pass. Each id once, in that order.</summary>
    internal static List<string> Quick(IEnumerable<KeyValuePair<string, StandingOrder>> orders, IEnumerable<string> replaced)
    {
        var ids = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in orders)
            if (pair.Value.Permission == WorkPermission.Enabled && !pair.Value.Protected && seen.Add(pair.Key)) ids.Add(pair.Key);
        foreach (var id in replaced) if (id != null && seen.Add(id)) ids.Add(id);
        return ids;
    }
}
