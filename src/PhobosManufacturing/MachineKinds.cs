using System;
using System.Collections.Generic;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>Which Manufacturing service, if any, owns a powered object.</summary>
internal enum MachineKind { None, Refinery, Processor, Sabatier, Filler, Cracker }

/// <summary>One classification per definition id for the power hooks, which the game calls for every powered
/// object in the world once per game second (once per frame at fast-forward); a foreign appliance costs one
/// dictionary probe and no state. Also the memo behind <see cref="Content.Machine"/>.</summary>
internal static class MachineKinds
{
    private static readonly Dictionary<string, MachineKind> kinds = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, bool> ours = new(StringComparer.Ordinal);
    internal static MachineKind Classify(string? id)
    {
        if (id == null) return MachineKind.None;
        if (kinds.TryGetValue(id, out var known)) return known;
        var kind = RefineryRules.IsFamily(id) ? MachineKind.Refinery : ProcessorRules.IsFamily(id) ? MachineKind.Processor :
            SabatierRules.IsFamily(id) ? MachineKind.Sabatier : FillerRules.IsFamily(id) ? MachineKind.Filler : CrackerRules.IsFamily(id) ? MachineKind.Cracker : MachineKind.None;
        if (kinds.Count < 65536) kinds[id] = kind;
        return kind;
    }
    /// <summary>Any Manufacturing machine, store, manifold, filler or regulator, by definition.</summary>
    internal static bool IsOurs(string? id)
    {
        if (id == null) return false;
        if (ours.TryGetValue(id, out bool known)) return known;
        bool yes = Classify(id) != MachineKind.None || GasStores.IsFamily(id) || ManifoldRules.IsFamily(id) || RegulatorRules.IsFamily(id);
        if (ours.Count < 65536) ours[id] = yes;
        return yes;
    }
    internal static void Reset() { kinds.Clear(); ours.Clear(); }
}
