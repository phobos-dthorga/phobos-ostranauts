using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Inventory;

namespace PhobosAgriculture;

internal static class PanelConfiguration
{
    internal static string Stamp(CondOwner co)=>ConfigurationStamp.For(co,"PhobosMaterialPort.","PhobosState.AgricultureWaterMode","PhobosState.crew-order")+
        (Definitions.Machine(co)?Service.Get(co).DoseId+"|"+Service.Get(co).Solution.Profile+"|"+Service.Get(co).Routed:"");
    internal static bool Apply(CondOwner co,string expected,string action,out string reason)
    {
        reason=ConsoleText.Get("stale");if(co.bDestroyed||Stamp(co)!=expected)return false;
        bool saved=RecyclerCapture.IsRecycler(co)?RecyclerCapture.Command(co,action,out reason):Service.Command(co,null,action,out reason);
        if(saved)ConfigurationStamp.SuspendChangedOrder(co);return saved;
    }
    internal static string[] WaterPeers(CondOwner co)=>co.mapGUIPropMaps.Where(p=>p.Key.StartsWith("PhobosMaterialPort.PhobosAgriculture.Water",StringComparison.Ordinal))
        .Select(p=>PortPairing.Read(new MaterialPort(co.strID,p.Key.Substring("PhobosMaterialPort.".Length),co.mapGUIPropMaps)))
        .Where(p=>p.State==PortLinkState.Linked).Select(p=>p.PeerObjectId).ToArray();
    internal static string Collector(CondOwner co)=>PortPairing.Read(new MaterialPort(co.strID,"PhobosAgriculture.RecyclerRejectOut",co.mapGUIPropMaps)).PeerObjectId;
}
