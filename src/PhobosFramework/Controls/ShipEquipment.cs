using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Fresh native root-object discovery, excluding docked neighbours and nested cargo.</summary>
public static class ShipEquipment
{
    public static IReadOnlyList<CondOwner> Read(Ship? ship, Func<CondOwner, bool> include)
    {
        if (ship == null || (int)ship.LoadState < 2) return Array.Empty<CondOwner>();
        var candidates = ship.GetCOs(null, bSubObjects: false, bAllowDocked: false, bAllowLocked: true);
        Diagnostics.Performance.Increment(Diagnostics.Performance.ShipCandidates, candidates.Count);
        var result = new List<CondOwner>();
        foreach (var co in candidates)
            if (co != null && !co.bDestroyed && co.ship == ship && co.objCOParent == null && include(co)) result.Add(co);
        return result;
    }
}
