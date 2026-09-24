using System;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Move a single unstacked item between finite, same-ship native containers.</summary>
public sealed class NativeItemTransfer : IPhysicalTransfer
{
    private readonly Container source, destination;
    private readonly CondOwner item;
    private PairXY original, target;
    private double mass;
    public NativeItemTransfer(Container source, Container destination, CondOwner item)
    { this.source = source; this.destination = destination; this.item = item; }
    public bool AtSource => item != null && !item.bDestroyed && item.objCOParent == source.CO && item.ship == source.CO.ship && source.Contains(item) && !destination.Contains(item);
    public bool AtDestination => item != null && !item.bDestroyed && item.objCOParent == destination.CO && item.ship == destination.CO.ship && destination.Contains(item) && !source.Contains(item);
    public bool Detached => item != null && !item.bDestroyed && item.objCOParent == null && item.ship == null && !source.Contains(item) && !destination.Contains(item);
    public bool Prepare()
    {
        if (!AtSource || source == destination || source.Locked || destination.Locked ||
            source.CO.ship == null || (int)source.CO.ship.LoadState < 2 || source.CO.ship != destination.CO.ship ||
            item.coStackHead != null || item.aStack.Count != 0 ||
            source.CO.HasCond("IsInfiniteContainer") || destination.CO.HasCond("IsInfiniteContainer") ||
            !destination.AllowedCO(item) || !destination.CanAddSimple(item, out target)) return false;
        original = item.pairInventoryXY;
        mass = item.GetTotalMass();
        return original.IsValid() && mass > 0 && !double.IsInfinity(mass);
    }
    public void Detach() => source.RemoveCO(item);
    public void Place()
    {
        if (!destination.AllowedCO(item) || !destination.CanAddSimple(item, out target))
            throw new InvalidOperationException(Text.Get("NativeItemTransfer.destination_changed_during_transfer"));
        destination.AddCOSimple(item, target);
        if (item.GetTotalMass() != mass) throw new InvalidOperationException(Text.Get("NativeItemTransfer.item_mass_changed_during_transfer"));
    }
    public void Restore()
    {
        // Synchronous main-thread commit: no other gameplay order can take these
        // cells between removal and recovery. Native methods retain the same object.
        source.AddCOSimple(item, original);
    }
    public void Redraw() { source.Redraw(); destination.Redraw(); }
}
