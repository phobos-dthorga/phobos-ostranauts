using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Feed taken from a machine's own inventory (Framework 0.83.0; owner report, 4 October 2026). The game shows
/// one inventory for an object, so a machine's internal feed compartment cannot be loaded by hand: what a player puts in
/// the machine lands in its ordinary inventory, beside its products. A machine that is looking for feed therefore takes
/// what it can use from that inventory into its feed compartment, one checked unit at a time, and gives unworked feed
/// back when a batch is cancelled. The unit is the same physical item throughout; a unit of a stack leaves the rest of
/// its stack where it was. Content decides what counts as feed and how much to take.</summary>
public static class OwnInventoryFeed
{
    /// <summary>The units in the machine's own inventory that <paramref name="accept"/> admits as single units (asked
    /// with <see cref="UnitItemTransfer.IsUnitPreflight"/> true, so a stacked unit is judged at its own mass), in a
    /// stable order.</summary>
    public static List<CondOwner> Units(CondOwner? machine, Func<CondOwner, bool> accept) =>
        StackUnits.All(machine).Where(u => u != null && !u.bDestroyed && UnitItemTransfer.AsUnit(u, () => accept(u)))
            .OrderBy(u => u.strID, StringComparer.Ordinal).ToList();

    /// <summary>Moves one unit from the machine's inventory into its feed compartment, when the compartment's own
    /// admission takes it and has room. False, with nothing moved, otherwise.</summary>
    public static bool Take(CondOwner unit, CondOwner feed)
    {
        if (unit == null || feed?.objContainer == null) return false;
        var move = new UnitItemTransfer(unit, feed);
        if (!PhysicalTransfer.Commit(move)) return false;
        move.Redraw();
        return true;
    }

    /// <summary>With an empty feed compartment, takes the first unit in the machine's inventory that the compartment
    /// admits. True when the compartment holds something afterwards.</summary>
    public static bool TopUp(CondOwner? machine, CondOwner? feed)
    {
        var contents = feed?.objContainer?.ContainedCOs;
        if (machine?.objContainer == null || contents == null) return false;
        if (contents.Count > 0) return true;
        if (machine.objContainer.ContainedCOs.Count == 0) return false;
        foreach (var unit in Units(machine, u => UnitItemTransfer.Fits(feed!, u, out _)))
            if (Take(unit, feed!)) return true;
        return false;
    }

    /// <summary>Puts what the feed compartment holds back into the machine's inventory, as far as it fits, so a
    /// cancelled batch's feed is in reach again. Returns how many units stayed behind for want of room.</summary>
    public static int Return(CondOwner? feed, CondOwner? machine)
    {
        var contents = feed?.objContainer?.ContainedCOs;
        if (contents == null || machine?.objContainer == null) return 0;
        int left = 0;
        foreach (var unit in contents.ToArray())
        {
            var move = new UnitItemTransfer(unit, machine);
            if (PhysicalTransfer.Commit(move)) move.Redraw(); else left++;
        }
        return left;
    }
}
