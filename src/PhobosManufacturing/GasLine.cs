using Phobos.Ostranauts.Framework.Liquids;

namespace PhobosManufacturing;

/// <summary>How a gas store reaches a machine or another store: touching, or on one gas-line network (Framework's
/// shared gas line since Manufacturing 0.23.0, with the saved Fennmark line identities unchanged). Shared by the P1
/// manifold, the L2 filling station, the A2 regulator and store-to-store transfers. A network joins every machine and
/// store whose gas port a line reaches, and touching equipment chains as if piped; the topology is Framework's shared
/// snapshot (reread every two real seconds or when a part changes) and both ends are checked fresh on every call.</summary>
internal static class GasLine
{
    internal static FluidSegmentFamily Family => LineFamilies.Gas;
    /// <summary>"adjacent", "line", or null when the store does not reach the machine.</summary>
    internal static string? Connection(CondOwner machine, CondOwner store) =>
        LineReach.Of(machine, store, Family) switch
        {
            LineReachKind.Adjacent => "adjacent",
            LineReachKind.Line => "line",
            _ => null
        };
}
