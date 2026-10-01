using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Liquids;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>How a gas store reaches a machine or another store: touching, or on one gas-line network (Framework's
/// shared gas line since Manufacturing 0.23.0, with the saved Fennmark line identities unchanged). Shared by the P1
/// manifold, the L2 filling station, the A2 regulator and store-to-store transfers. A network joins every machine and
/// store a gas line runs under or right beside (Framework 0.69.0), and touching equipment chains as if piped; the
/// topology is Framework's shared snapshot (reread every two real seconds or when a part changes) and both ends are
/// checked fresh on every call.</summary>
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
    /// <summary>The gas stores a machine may link (Manufacturing 0.28.0, one rule for the P1, L2 and A2): of a kind it
    /// accepts, installed, undamaged and unlocked on its own ship, and touching it or on its gas line. A damaged or
    /// locked store that merely touches is no longer offered; a link saved to one stays saved.</summary>
    internal static IEnumerable<CondOwner> Stores(CondOwner machine, Func<CondOwner, bool> accepts) => Standing(machine, accepts)
        .Where(c => NativeFluidRoute.EndpointReady(c) && Connection(machine, c) != null).ToArray();
    /// <summary>Why a gas store of an accepted kind aboard is not offered: loose, damaged, locked, or no gas line touching it.</summary>
    internal static string Note(CondOwner machine, Func<CondOwner, bool> accepts) => LinkChoices.Note(machine, Family, Standing(machine, accepts), Stores(machine, accepts));
    // Every store of an accepted kind standing on the machine's ship, in any state.
    private static IEnumerable<CondOwner> Standing(CondOwner machine, Func<CondOwner, bool> accepts) => (machine.ship?.GetCOs(null, false, false, true) ?? Enumerable.Empty<CondOwner>())
        .Where(c => c != null && !c.bDestroyed && c != machine && c.ship == machine.ship && c.objCOParent == null && GasStores.IsFamily(c.strCODef) && accepts(c))
        .OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
}
