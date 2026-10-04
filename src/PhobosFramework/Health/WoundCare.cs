using System;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Health;

/// <summary>Why a dressing could not go on.</summary>
public enum WoundCareFailure { None, NoPatient, NoWound, Occupied, DoesNotFit, NotSingle, Failed }

/// <summary>Puts one real item onto a wound, the game's own way (Framework 0.89.0, first used
/// by Phobos Medical's medic order). The game treats a wound when an item is dropped onto the wound's item slot in the
/// paper doll: <c>Slots.SlotItem(slot, item, bAuto: false)</c> then runs the item's own slot interaction (a clean cloth
/// staunches, a splint splints, a dirty cloth staunches but adds infection). This calls the same slotting from code,
/// so every effect stays the game's. Each wound part <c>Wound&lt;Place&gt;</c> has the item slot
/// <c>WoundItem&lt;Place&gt;</c>, which the patient's own slots find through their sub-objects.</summary>
public static class WoundCare
{
    public const string WoundPrefix = "Wound", ItemSlotPrefix = "WoundItem";

    /// <summary>The item slot of a wound part, or null when the name is not a wound part's.</summary>
    public static string? ItemSlot(string? woundPart) =>
        woundPart != null && woundPart.StartsWith(WoundPrefix, StringComparison.Ordinal) && !woundPart.StartsWith(ItemSlotPrefix, StringComparison.Ordinal) && woundPart.Length > WoundPrefix.Length
            ? ItemSlotPrefix + woundPart.Substring(WoundPrefix.Length) : null;

    /// <summary>The item on a wound's item slot, or null.</summary>
    public static CondOwner? Occupant(CondOwner? patient, string slot)
    {
        var s = patient?.compSlots?.GetSlot(slot);
        return s?.aCOs?.FirstOrDefault(c => c != null && !c.bDestroyed);
    }

    /// <summary>Whether the item's definition names this wound slot among its slot effects (the game's own fit rule
    /// for a paper-doll drop).</summary>
    public static bool Fits(string? itemDefinition, string slot)
    {
        if (itemDefinition == null || DataHandler.dictCOs == null || !DataHandler.dictCOs.TryGetValue(itemDefinition, out var def)) return false;
        var effects = def.mapSlotEffects ?? Array.Empty<string>();
        for (int i = 0; i + 1 < effects.Length; i += 2) if (effects[i] == slot) return true;
        return false;
    }

    /// <summary>Slots one single, loose unit onto the patient's wound slot. A dressing or splint stays there; an item the
    /// game uses up on a wound (water, spirits) is gone afterwards. The unit must already be detached from any
    /// stack (see <c>StackUnits.Detach</c>); it is removed from wherever it lies first. Returns false, leaving the unit
    /// where it was put back by the caller, when the slot is taken or the game refuses it.</summary>
    public static bool Apply(CondOwner patient, string slot, CondOwner unit, out WoundCareFailure failure)
    {
        failure = WoundCareFailure.NoPatient;
        if (patient == null || patient.bDestroyed || patient.compSlots == null) return false;
        failure = WoundCareFailure.NoWound;
        if (patient.compSlots.GetSlot(slot) == null) return false;
        failure = WoundCareFailure.Occupied;
        if (Occupant(patient, slot) != null) return false;
        failure = WoundCareFailure.DoesNotFit;
        if (unit == null || unit.bDestroyed || !Fits(unit.strCODef, slot)) return false;
        failure = WoundCareFailure.NotSingle;
        if (unit.coStackHead != null || (unit.aStack != null && unit.aStack.Count > 0)) return false;
        unit.RemoveFromCurrentHome(true);
        failure = WoundCareFailure.Failed;
        bool slotted;
        try { slotted = patient.compSlots.SlotItem(slot, unit, bAuto: false); }
        catch (Exception) { slotted = false; }
        // An item the game uses up on the wound (water, spirits) is removed by its own slot effect: that is success too.
        if (!slotted || !unit.bDestroyed && Occupant(patient, slot) != unit) return false;
        failure = WoundCareFailure.None;
        return true;
    }

    /// <summary>Takes the item off a wound slot and returns it, unattached (the caller puts it somewhere), or null.</summary>
    public static CondOwner? Remove(CondOwner patient, string slot)
    {
        if (patient?.compSlots == null || Occupant(patient, slot) == null) return null;
        try { return patient.compSlots.UnSlotItem(slot); }
        catch (Exception) { return null; }
    }

    /// <summary>Takes the item off a wound slot and sets it down on the deck beside <paramref name="near"/> (see
    /// <see cref="SetDown"/>). The game's unslot interaction runs, so a dressing taken off stops counting as one. Returns
    /// the item, or null when nothing was there or it could not be taken off.</summary>
    public static CondOwner? RemoveToDeck(CondOwner patient, string slot, CondOwner near)
    {
        if (patient == null || near == null || (near.ship ?? patient.ship) == null) return null;
        var item = Remove(patient, slot);
        if (item != null) SetDown(item, near);
        return item;
    }

    /// <summary>Sets a loose item down on the deck beside <paramref name="near"/> through the game's own deck drop
    /// (nearby free tiles, stacking where it can; whatever does not fit is added where it stands). Never destroys it.</summary>
    public static void SetDown(CondOwner item, CondOwner near)
    {
        var ship = near?.ship;
        if (item == null || item.bDestroyed || ship == null) return;
        Persistence.LegacyItemConversions.Drop(ship, item, new UnityEngine.Vector2(near!.tf.position.x, near.tf.position.y));
    }
}
