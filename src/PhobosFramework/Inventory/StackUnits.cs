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

    /// <summary>Takes one unit out of wherever it is, leaving the rest of its stack in place (Framework 0.71.0, lifted
    /// from the unit transfer): a member leaves its head, a head with members hands the stack to the next unit (the
    /// game's own pop, which re-adds the rest at the same cell), a single item simply leaves. Returns the stack that
    /// stays behind, or null.</summary>
    public static CondOwner? Detach(CondOwner unit)
    {
        if (unit.coStackHead != null) { var head = unit.coStackHead; head.RemoveCO(unit); return head; }
        if (unit.aStack.Count > 0) return unit.PopHeadFromStack();
        unit.RemoveFromCurrentHome(true);
        return null;
    }

    /// <summary>The kind a unit stacks as (Framework 0.71.0), or null when it must keep its own cells. Units of one
    /// kind are interchangeable, so putting them in one stack loses no information: the game stacks them (a stack
    /// limit above one), they hold no contents or lot and are undamaged, and they have the same definition, the same
    /// mass and the same Phobos records, byte for byte. A plain unit (its definition's own mass, no record) has its
    /// definition id as its kind, which is also how a batch is judged before its products exist; a unit that carries a
    /// record, such as a bagged charge, has a longer kind that only an identical unit shares.</summary>
    public static string? Kind(CondOwner? unit)
    {
        if (unit == null || unit.bDestroyed || unit.nStackLimit <= 1 || unit.HasCond("IsDamaged") || !Empty(unit)) return null;
        double own = UnitMass(unit);
        if (double.IsNaN(own) || double.IsInfinity(own)) return null;
        string records = Records(unit);
        double? mass = DefinitionMass(unit.strCODef);
        if (records.Length == 0 && mass != null && System.Math.Abs(own - mass.Value) <= 1e-7) return unit.strCODef;
        return unit.strCODef + "#" + own.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "#" + records;
    }
    /// <summary>A unit that carries nothing of its own: its kind is just its definition.</summary>
    public static bool Plain(CondOwner? unit) => unit != null && Kind(unit) == unit.strCODef;
    /// <summary>How many more units of a kind a stack in a container takes: zero unless its head and every member are
    /// of that kind.</summary>
    public static int Room(CondOwner? head, string kind)
    {
        if (head == null || head.coStackHead != null || Kind(head) != kind) return 0;
        foreach (var member in head.aStack) if (Kind(member) != kind) return 0;
        return System.Math.Max(0, head.nStackLimit - head.StackCount);
    }
    // The unit's Phobos records in a fixed order, or empty when it has none.
    private static string Records(CondOwner unit)
    {
        if (unit.mapGUIPropMaps == null) return "";
        var text = new System.Text.StringBuilder();
        foreach (var map in unit.mapGUIPropMaps.Where(m => m.Key.StartsWith("Phobos", System.StringComparison.Ordinal)).OrderBy(m => m.Key, System.StringComparer.Ordinal))
        {
            text.Append(map.Key).Append('{');
            if (map.Value != null)
                foreach (var field in map.Value.OrderBy(f => f.Key, System.StringComparer.Ordinal)) text.Append(field.Key).Append('=').Append(field.Value).Append(';');
            text.Append('}');
        }
        return text.ToString();
    }
    private static readonly Dictionary<string, double?> masses = new(System.StringComparer.Ordinal);
    internal static void Reset() => masses.Clear();
    private static double? DefinitionMass(string? definition)
    {
        if (definition == null) return null;
        if (masses.TryGetValue(definition, out var known)) return known;
        var def = DataHandler.GetCondOwnerDef(definition);
        double? mass = def?.aStartingConds == null ? null : (double?)Registration.EquipmentSaveUpgrade.Amount(def.aStartingConds, "StatMass");
        if (mass is double m && (double.IsNaN(m) || m <= 0)) mass = null;
        masses[definition] = mass;
        return mass;
    }
}
