using Phobos.Ostranauts.Framework.Liquids;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>How a gas store reaches a machine or another store: within one tile, or along an intact Fennmark gas
/// line from the store's line port to the machine's (a bounded native route). Shared by the P1 manifold, the L2
/// filling station and store-to-store transfers.</summary>
internal static class GasLine
{
    /// <summary>"adjacent", "line", or null when the store does not reach <paramref name="machinePoint"/>.</summary>
    internal static string? Connection(CondOwner machine, string machinePoint, CondOwner store)
    {
        if (ProcessorService.Adjacent(machine, store)) return "adjacent";
        var path = NativeFluidRoute.Find(store, ManifoldRules.StoreOutlet, machine, machinePoint, c => PropellantLineRules.IsSegment(c.strCODef));
        return path != null && path.Length <= ManifoldRules.RouteTileLimit ? "line" : null;
    }
}
