using Phobos.Ostranauts.Framework.Liquids;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>How a gas store reaches a machine or another store: within one tile, or along an intact Fennmark gas
/// line from the store's line port to the machine's (a bounded native route). Shared by the P1 manifold, the L2
/// filling station, the A2 regulator and store-to-store transfers. The test is Framework's shared
/// <see cref="LineReach"/> (Framework 0.56.0); routes come from the shared topology snapshot (reread every two real
/// seconds or when a part changes) and endpoints are checked fresh on every call.</summary>
internal static class GasLine
{
    internal static readonly FluidSegmentFamily Family = new("PhobosManufacturing.GasLine", c => PropellantLineRules.IsSegment(c.strCODef));
    /// <summary>"adjacent", "line", or null when the store does not reach <paramref name="machinePoint"/>.</summary>
    internal static string? Connection(CondOwner machine, string machinePoint, CondOwner store) =>
        LineReach.Of(machine, machinePoint, store, ManifoldRules.StoreOutlet, Family, ManifoldRules.RouteTileLimit) switch
        {
            LineReachKind.Adjacent => "adjacent",
            LineReachKind.Line => "line",
            _ => null
        };
}
