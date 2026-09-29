using System;
using System.Collections.Generic;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Which Shipbreaker service, if any, owns a powered object.</summary>
internal enum PowerKind { None, Processor, Reclaimer, Grabber, Collector, Furnace, Thaw }

/// <summary>One classification per definition id for the power hooks, which the game calls for every powered
/// object in the world once per game second (once per frame at fast-forward). The answer is remembered per
/// definition, so an appliance that is not ours costs one dictionary probe and no state.</summary>
internal static class PowerKinds
{
    private static readonly Dictionary<string, PowerKind> kinds = new(StringComparer.Ordinal);
    internal static PowerKind Classify(string? id)
    {
        if (id == null) return PowerKind.None;
        if (kinds.TryGetValue(id, out var known)) return known;
        var kind = FurnaceRules.Machine(id) ? PowerKind.Furnace : ThawRules.IsFamily(id) ? PowerKind.Thaw :
            id == IntakeRules.Grabber + "Installed" ? PowerKind.Grabber : CollectorRules.IsFamily(id) ? PowerKind.Collector :
            ReclaimerRules.IsFamily(id) ? PowerKind.Reclaimer : Content.IsMachine(id) ? PowerKind.Processor : PowerKind.None;
        if (kinds.Count < 65536) kinds[id] = kind;
        return kind;
    }
    internal static void Reset() => kinds.Clear();
}
