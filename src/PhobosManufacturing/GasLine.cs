using Phobos.Ostranauts.Framework.Liquids;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>How a gas store reaches a machine or another store: within one tile, or along an intact Fennmark gas
/// line from the store's line port to the machine's (a bounded native route). Shared by the P1 manifold, the L2
/// filling station, the A2 regulator and store-to-store transfers. Routes come from Framework's shared topology
/// snapshot (reread every two real seconds or when a part changes); endpoints are checked fresh on every call.</summary>
internal static class GasLine
{
    internal static readonly FluidSegmentFamily Family = new("PhobosManufacturing.GasLine", c => PropellantLineRules.IsSegment(c.strCODef));
    /// <summary>"adjacent", "line", or null when the store does not reach <paramref name="machinePoint"/>.</summary>
    internal static string? Connection(CondOwner machine, string machinePoint, CondOwner store)
    {
        if (ProcessorService.Adjacent(machine, store)) return "adjacent";
        var path = FluidRouteCache.Find(store, ManifoldRules.StoreOutlet, machine, machinePoint, Family);
        return path != null && path.Length <= ManifoldRules.RouteTileLimit ? "line" : null;
    }
}
