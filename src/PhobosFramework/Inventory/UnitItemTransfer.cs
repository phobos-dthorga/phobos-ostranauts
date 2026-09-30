using System;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>One physical unit into a container, taken from a native stack if it is in one (Framework 0.61.0; moved
/// from the crew hauling orders, which keep using it unchanged). The unit is admitted by the destination's own rules
/// before anything moves, with <see cref="IsUnitPreflight"/> telling content admission that a single unit is being
/// asked about; the rest of the stack stays where it was. Restore puts the unit back beside its stack, in its slot,
/// at its inventory cell or on the deck where it lay. No virtual carried inventory.</summary>
public sealed class UnitItemTransfer : IPhysicalTransfer
{
    private static CondOwner? preflight;
    /// <summary>Read-only admission of one unit; content still validates the detached unit at settlement.</summary>
    public static bool IsUnitPreflight(CondOwner item) => preflight == item;
    /// <summary>Whether the destination admits the unit now, and where it would go.</summary>
    public static bool Fits(CondOwner destination, CondOwner item, out PairXY cell)
    {
        cell = default;
        if (destination?.objContainer == null || destination.objContainer.Locked) return false;
        var previous = preflight; preflight = item;
        try { return destination.objContainer.AllowedCO(item) && destination.objContainer.CanAddSimple(item, out cell); }
        finally { preflight = previous; }
    }

    private readonly CondOwner item, destination;
    private readonly CondOwner? original;
    private readonly Ship? deck;
    private readonly Slot? slot;
    private PairXY position, originalPosition;
    private Vector3 deckPosition;
    private CondOwner? remainder;
    private double mass;
    public UnitItemTransfer(CondOwner item, CondOwner destination)
    { this.item = item; this.destination = destination; original = item.objCOParent; slot = item.slotNow; deck = original == null && slot == null ? item.ship : null; }
    public CondOwner Item => item;
    public bool AtSource => !item.bDestroyed && item.objCOParent == original && (original != null || deck != null && item.ship == deck);
    public bool AtDestination => !item.bDestroyed && item.objCOParent == destination && item.coStackHead == null && item.aStack.Count == 0;
    public bool Detached => !item.bDestroyed && item.objCOParent == null && item.ship == null;
    public bool Prepare()
    {
        if (!AtSource || original == destination || original?.objContainer?.Locked == true || !Fits(destination, item, out position)) return false;
        originalPosition = item.pairInventoryXY; deckPosition = item.tf.position;
        mass = item.GetCondAmount("StatMass");
        return !double.IsNaN(mass) && !double.IsInfinity(mass) && mass > 0;
    }
    public void Detach()
    {
        if (item.coStackHead != null) { remainder = item.coStackHead; remainder.RemoveCO(item); }
        else if (item.aStack.Count > 0) remainder = item.PopHeadFromStack();
        else item.RemoveFromCurrentHome(true);
    }
    public void Place()
    {
        if (!Detached || !destination.objContainer.AllowedCO(item) || !destination.objContainer.CanAddSimple(item, out position))
            throw new InvalidOperationException("Unit delivery destination changed.");
        destination.objContainer.AddCOSimple(item, position);
        if (!AtDestination || Math.Abs(item.GetTotalMass() - mass) > 1e-7)
            throw new InvalidOperationException("Unit delivery changed item identity or mass.");
    }
    public void Restore()
    {
        if (original == null)
        {
            // Back onto the deck where it lay; a unit taken from a deck stack lies beside that stack.
            if (deck == null) throw new InvalidOperationException("Native deck restoration failed; physical cargo retained.");
            item.tf.position = deckPosition; deck.AddCO(item, bTiles: true);
        }
        else if (remainder != null)
        {
            if (original.AddCO(item, bEquip: false, bOverflow: false, bIgnoreLocks: false) != null)
                throw new InvalidOperationException("Native stack restoration failed; physical cargo retained.");
        }
        else if (slot != null)
        {
            if (!slot.compSlots.SlotItem(slot.strName, item))
                throw new InvalidOperationException("Native slot restoration failed; physical cargo retained.");
        }
        else original.objContainer.AddCOSimple(item, originalPosition);
    }
    public void Redraw() { original?.objContainer?.Redraw(); destination.objContainer.Redraw(); }
}
