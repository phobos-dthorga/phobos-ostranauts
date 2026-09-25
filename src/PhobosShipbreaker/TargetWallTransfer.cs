using System;
using UnityEngine;
using Phobos.Ostranauts.Framework.Inventory;

namespace PhobosShipbreaker;

// Sole cross-ship adapter: exact mission target, exact capture, one real detached wall.
// Framework's ordinary NativeItemTransfer remains strictly same-ship.
internal sealed class TargetWallTransfer : IPhysicalTransfer
{
    private readonly Ship source;
    private readonly CondOwner grabber,item;
    private readonly Func<bool> permitted;
    private PairXY cell;
    private Vector3 original;
    internal TargetWallTransfer(Ship source,CondOwner grabber,CondOwner item,Func<bool> permitted)
    { this.source=source;this.grabber=grabber;this.item=item;this.permitted=permitted; }
    public bool AtSource => !item.bDestroyed&&item.ship==source&&item.objCOParent==null&&ReclamationGeometry.Resolve(source,item.strID)==item;
    public bool AtDestination => !item.bDestroyed&&item.ship==grabber.ship&&item.objCOParent==grabber&&grabber.objContainer.Contains(item);
    public bool Detached => !item.bDestroyed&&item.ship==null&&item.objCOParent==null&&!grabber.objContainer.Contains(item);
    public bool Prepare()
    {
        if(!AtSource||!permitted()||!ProcessingService.ValidPanel(item)||!ReclamationGeometry.Reach(grabber,item)||
            grabber.objContainer.Locked||!grabber.objContainer.AllowedCO(item)||!grabber.objContainer.CanAddSimple(item,out cell)) return false;
        original=item.tf.position; return true;
    }
    public void Detach() => item.RemoveFromCurrentHome();
    public void Place()
    {
        if(!permitted()||!grabber.objContainer.AllowedCO(item)||!grabber.objContainer.CanAddSimple(item,out cell)) throw new InvalidOperationException("Wall destination changed.");
        grabber.objContainer.AddCOSimple(item,cell);
        if(!ProcessingService.ValidPanel(item)) throw new InvalidOperationException("Wall identity or mass changed.");
    }
    public void Restore() { item.tf.position=original; source.AddCO(item,bTiles:true); }
}
