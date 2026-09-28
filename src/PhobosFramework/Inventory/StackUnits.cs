using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>The game stacks matching items dropped into a container (StackOrAddToContainer): a stack
/// head carries its members in aStack and the container lists only heads. A machine that consumes one
/// unit at a time must see members as units and take a member first, leaving the head in place; the
/// native removal path detaches a member from its stack by itself.</summary>
public static class StackUnits
{
    /// <summary>Every unit in a container: each stack's members first, then its head.</summary>
    public static IEnumerable<CondOwner> All(CondOwner? container)
    {
        var heads = container?.objContainer?.ContainedCOs;
        if (heads == null) yield break;
        foreach (var head in heads.ToArray())
        {
            if (head == null) continue;
            if (head.aStack != null) foreach (var member in head.aStack.ToArray()) if (member != null) yield return member;
            yield return head;
        }
    }

    /// <summary>True when the unit sits in the container directly or as a member of a stack there.</summary>
    public static bool Inside(CondOwner? unit, CondOwner? container) => unit != null && container != null &&
        (unit.objCOParent == container || unit.coStackHead != null && unit.coStackHead.objCOParent == container);

    /// <summary>This unit's own mass, without the stack members a head may carry.</summary>
    public static double UnitMass(CondOwner unit) => unit.GetCondAmount("StatMass");

    /// <summary>No contents or lot of its own; stack members are separate units, not contents.</summary>
    public static bool Empty(CondOwner unit)
    {
        var members = unit.aStack == null ? new HashSet<CondOwner>() : new HashSet<CondOwner>(unit.aStack);
        return unit.GetCOsSafe(true).All(members.Contains) && unit.GetLotCOs(true).Count == 0;
    }

    /// <summary>Neither a stack member nor a head with members.</summary>
    public static bool Single(CondOwner unit) => unit.coStackHead == null && (unit.aStack == null || unit.aStack.Count == 0);
}
