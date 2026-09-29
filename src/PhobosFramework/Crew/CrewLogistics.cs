using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>Exact physical units, including native stacks. No virtual carried inventory.</summary>
public static class CrewLogistics
{
    private static CondOwner? preflight;
    /// <summary>Read-only admission of one unit; content still validates the detached unit at settlement.</summary>
    public static bool IsUnitPreflight(CondOwner item) => preflight == item;
    private static bool Fits(CondOwner destination,CondOwner item,out PairXY cell)
    {
        cell=default;
        if(destination.objContainer?.Locked!=false)return false;
        var previous=preflight; preflight=item;
        try { return destination.objContainer.AllowedCO(item)&&destination.objContainer.CanAddSimple(item,out cell); }
        finally { preflight=previous; }
    }
    public static bool Loose(CondOwner co) => co != null && !co.bDestroyed && !co.HasCond("IsInstalled") &&
        !co.HasCond("IsHuman") && co.GetLotCOs(true).Count == 0 &&
        co.GetCOsSafe(true).All(c => co.StackAsList.Contains(c));
    public static IEnumerable<CondOwner> Contents(CondOwner? co) => co?.objContainer?.ContainedCOs
        .SelectMany(c => c.StackAsList).Distinct().Where(Loose).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray() ?? Array.Empty<CondOwner>();
    private static IEnumerable<CondOwner> Heads(CondOwner? co) => Contents(co).Where(c => c.coStackHead == null);

    /// <summary>Stack heads anywhere aboard the equipment's ship that an order with the ship-wide source
    /// may take, nearest to the equipment first: on the deck, in unlocked containers and in other
    /// machines' trays, the way the game's own Reload job searches (same ship, nothing locked). Never
    /// from inside the equipment itself, a hidden system bin or someone's hands.</summary>
    public static IEnumerable<CondOwner> Aboard(CondOwner equipment, CondOwner destination)
    {
        var ship = equipment.ship;
        if (ship == null) return Array.Empty<CondOwner>();
        var origin = equipment.GetPos("use");
        return LooseHeads(ship)
            .Where(c => !c.bDestroyed && Available(c, equipment, destination))
            .OrderBy(c => TileUtils.TileRange(c.GetPos(), origin)).ThenBy(c => c.strID, StringComparer.Ordinal).ToArray();
    }
    // One discovery pass asks for every starved machine on a ship in the same step; the walk over every nested
    // object aboard is shared for that step, and each machine sorts its own view of the loose stack heads.
    private static readonly Phobos.Ostranauts.Framework.Processing.StepMemo<Ship, CondOwner[]> looseHeads = new();
    private static CondOwner[] LooseHeads(Ship ship) => looseHeads.GetOrAdd(Phobos.Ostranauts.Framework.Processing.NativeSteps.Frame, ship, s =>
        s.GetCOs(null, true, false, false).Where(c => c != null && !c.bDestroyed && c.ship == s && c.coStackHead == null && Loose(c)).ToArray());
    private static bool Available(CondOwner unit, CondOwner equipment, CondOwner destination)
    {
        var parent = unit.objCOParent;
        if (parent == null) return unit.slotNow == null; // lying on the deck
        if (parent.HasCond("IsHuman") || parent.HasCond("IsSystem") || parent.HasCond("IsLocked") || parent.objContainer == null || parent.objContainer.Locked) return false;
        var root = unit.RootParent();
        return parent != equipment && parent != destination && root != equipment && root != destination && !root.HasCond("IsHuman");
    }
    public static CrewWorkOffer? Supply(CondOwner equipment, StandingOrder order, CondOwner destination,
        Func<CondOwner,bool> accepts, CrewRole role, int retain = 0)
    {
        if (order.Source == StandingOrder.ShipWide)
        {
            if (!CrewWork.CanManage(equipment)) return null;
            var heads = Aboard(equipment, destination).ToArray();
            if (heads.SelectMany(c => c.StackAsList).Distinct().Where(Loose).Count(accepts) <= retain) return null;
            return heads.Where(accepts).Select(c => Move(c, destination, role)).FirstOrDefault(o => o != null);
        }
        var source = CrewWork.Resolve(order.Source);
        if (source == null || source == destination || source.ship != equipment.ship ||
            source.objContainer?.Locked != false || !CrewWork.CanManage(source)) return null;
        if (Contents(source).Count(accepts) <= retain) return null;
        return Heads(source).Where(accepts).Select(c => Move(c, destination, role)).FirstOrDefault(o => o != null);
    }
    public static CrewWorkOffer? Output(CondOwner equipment, StandingOrder order, CondOwner source,
        Func<CondOwner,bool> accepts, CrewRole role, int retain = 0)
    {
        var destination = CrewWork.Resolve(order.Destination);
        if (destination == null || destination == source || destination.ship != equipment.ship ||
            destination.objContainer?.Locked != false || !CrewWork.CanManage(destination) || Contents(source).Count(accepts) <= retain) return null;
        return Heads(source).Where(accepts).Select(c => Move(c, destination, role)).FirstOrDefault(o => o != null);
    }
    public static CrewWorkOffer? Move(CondOwner cargo, CondOwner destination, CrewRole role)
    {
        if (!Loose(cargo) || !Fits(destination,cargo,out _)) return null;
        return new CrewWorkOffer("haul", CrewWork.Message("haul", cargo.strNameFriendly, destination.strNameFriendly), role,
            destination, CrewBalance.HandlingSeconds, duty:"Haul", cargo:cargo, destination:destination);
    }
    /// <summary>The item still lies where the offer found it: in its origin container, or on the deck of
    /// the actor's ship when the offer names no origin.</summary>
    private static bool AtOrigin(CondOwner actor, CondOwner item, CrewWorkOffer offer) =>
        item.ship == actor.ship && item.objCOParent == offer.Origin &&
        (offer.Origin == null ? item.slotNow == null : offer.Origin.objContainer?.Locked == false);
    internal static bool Prepare(CondOwner actor, CrewWorkOffer offer)
    {
        var item = offer.Cargo;
        if (item == null) return true;
        return Loose(item) && AtOrigin(actor, item, offer) && offer.Destination?.objContainer?.Locked == false &&
            Fits(offer.Destination,item,out _) &&
            actor.CanTakeItemsSimulated(new List<CondOwner> { item }) && CrewWork.Path(actor, item);
    }
    internal static bool Deliver(CrewWorkContext context, CrewWorkOffer offer, out string reason)
    {
        reason = CrewWork.Message("cargo_blocked");
        var item = offer.Cargo; var destination = offer.Destination;
        if (item == null || destination == null || !Loose(item) || item.ship != context.Actor.ship ||
            destination.ship != item.ship || !CrewWork.CanManage(destination)) return false;
        if (context.Skipping)
        {
            // The skip charges the walk to the item (or its container) and to the destination, then moves the unit.
            if (!AtOrigin(context.Actor, item, offer) || !CrewWork.Path(context.Actor, offer.Origin ?? item) || !CrewWork.Path(context.Actor, destination)) return false;
        }
        else if (item.RootParent() != context.Actor || !CrewWork.LocalAccess(context.Actor, destination, 2)) return false;
        var transfer=new UnitTransfer(item,destination);
        bool done = PhysicalTransfer.Commit(transfer);
        if(done)transfer.Redraw();
        if (done) reason = CrewWork.Message("done");
        return done;
    }
    private sealed class UnitTransfer : IPhysicalTransfer
    {
        private readonly CondOwner item, destination;
        private readonly CondOwner? original;
        private readonly Ship? deck;
        private readonly Slot? slot;
        private PairXY position, originalPosition;
        private Vector3 deckPosition;
        private CondOwner? remainder;
        private double mass;
        internal UnitTransfer(CondOwner item, CondOwner destination)
        { this.item = item; this.destination = destination; original = item.objCOParent; slot = item.slotNow; deck = original == null && slot == null ? item.ship : null; }
        public bool AtSource => !item.bDestroyed && item.objCOParent == original && (original != null || deck != null && item.ship == deck);
        public bool AtDestination => !item.bDestroyed && item.objCOParent == destination && item.coStackHead == null && item.aStack.Count == 0;
        public bool Detached => !item.bDestroyed && item.objCOParent == null && item.ship == null;
        public bool Prepare()
        {
            if (!AtSource || original == destination || original?.objContainer?.Locked == true ||
                !Fits(destination,item,out position)) return false;
            originalPosition = item.pairInventoryXY; deckPosition = item.tf.position;
            mass = item.GetCondAmount("StatMass");
            return CrewBalance.Finite(mass) && mass > 0;
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
                throw new InvalidOperationException("Crew delivery destination changed.");
            destination.objContainer.AddCOSimple(item, position);
            if (!AtDestination || Math.Abs(item.GetTotalMass() - mass) > 1e-7)
                throw new InvalidOperationException("Crew delivery changed item identity or mass.");
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
                if (original.AddCO(item, bEquip:false, bOverflow:false, bIgnoreLocks:false) != null)
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
}
