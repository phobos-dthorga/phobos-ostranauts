using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAgriculture;

internal sealed class RecyclerCapture : IRecyclerRejectSink
{
    internal static readonly RecyclerCapture Instance=new();
    internal const string Wet="PhobosVerdemorrowRecyclerWetRejects",Controls="PhobosAgricultureRecyclerCapture";
    private const string Collector="PhobosResidueCollectorInstalled";
    private const double PacketKg=13,CapacityKg=52;
    private static readonly HashSet<string> armed=new(StringComparer.Ordinal);
    internal static bool Available;
    internal static void Reset()=>armed.Clear();
    internal static bool IsRecycler(CondOwner? co)=>co!=null && !co.bDestroyed && DataHandler.GetCondTrigger("TIsWaterRecyclerInstalled",true)?.Triggered(co)==true;
    private static MaterialPort Sender(CondOwner co)=>new(co.strID,"PhobosAgriculture.RecyclerRejectOut",co.mapGUIPropMaps);
    private static MaterialPort Receiver(CondOwner co)=>new(co.strID,"PhobosShipbreaker.ResidueIn",co.mapGUIPropMaps);
    private static ObjectStateStore Journal(CondOwner co)=>new(co.mapGUIPropMaps,"AgricultureRecyclerCapture",Plugin.Id,1);
    private static ObjectStateStore Packet(CondOwner co)=>new(co.mapGUIPropMaps,"AgricultureWetRejects",Plugin.Id,1);
    private static bool Clear(CondOwner co)
    {
        var status=Journal(co).Read(out var d);
        return status==SavedStateStatus.Missing || status==SavedStateStatus.Ready && d.Count==1 && d.TryGetValue("state",out var value) && value=="clear";
    }
    private static void Mark(CondOwner co,string state)
    {if(!Journal(co).TryWrite(new Dictionary<string,string>{["state"]=state}))throw new InvalidOperationException("Protected recycler journal.");}
    internal static bool Cargo(CondOwner co)
    {
        if(co.strCODef!=Wet||co.bDestroyed||co.HasCond("IsInstalled")||co.coStackHead!=null||co.aStack.Count>0||co.GetCOsSafe(true).Count>0||co.GetLotCOs(true).Count>0)return false;
        var status=Packet(co).Read(out var d);
        return status==SavedStateStatus.Ready && d.Count==1 && d.TryGetValue("kg",out var raw) && double.TryParse(raw,NumberStyles.Float,CultureInfo.InvariantCulture,out var kg) &&
            Core.CropState.Finite(kg) && kg>=0 && kg<=PacketKg && Math.Abs(co.GetTotalMass()-kg)<1e-8;
    }
    private static void WritePacket(CondOwner co,double kg)
    {
        if(!Core.CropState.Finite(kg)||kg<0||kg>PacketKg+1e-6)throw new ArgumentException("Invalid wet reject payload.");
        if(!Packet(co).TryWrite(new Dictionary<string,string>{["kg"]=kg.ToString("R",CultureInfo.InvariantCulture)}))throw new InvalidOperationException("Protected wet reject packet.");
        co.AddMass(kg-co.GetTotalMass(),true);co.SetCondAmount("StatBasePrice",.01*kg/PacketKg);
    }
    private static bool Endpoint(CondOwner co)=>!co.bDestroyed && co.objCOParent==null && co.ship!=null && (int)co.ship.LoadState>=2 && co.HasCond("IsInstalled") &&
        !co.HasCond("IsDamaged") && !co.HasCond("IsLocked") && !co.HasCond("IsOverrideOff") && !co.HasCond("IsSignalOff") && co.objContainer!=null && !co.objContainer.Locked;
    private static bool Attached(CondOwner recycler, CondOwner collector)=>recycler.ship==collector.ship && recycler.Item!=null && collector.Item!=null &&
        Core.RecyclerAttachment.Aligned(recycler.GetPos().x,recycler.GetPos().y,recycler.Item.TF.eulerAngles.z,
            collector.GetPos().x,collector.GetPos().y,collector.Item.TF.eulerAngles.z);
    internal static IEnumerable<CondOwner> Candidates(CondOwner recycler)=>recycler.ship.GetCOs(null,false,false,true).Where(c=>c.strCODef==Collector && CollectorCargo.EndpointReady(c) && Endpoint(c) && Attached(recycler,c));
    private static CondOwner? Peer(CondOwner recycler)=>Service.Resolve(PortPairing.Read(Sender(recycler)).PeerObjectId);
    public bool Handles(CondOwner recycler)=>PortPairing.Read(Sender(recycler)).State!=PortLinkState.Unlinked;
    public RecyclerRejectReservation? Reserve(CondOwner recycler)
    {
        var collector=Peer(recycler);
        if(!Available||!Definitions.Ready||!armed.Contains(recycler.strID)||!Endpoint(recycler)||collector==null||collector.strCODef!=Collector||!CollectorCargo.EndpointReady(collector)||!Endpoint(collector)||!collector.HasCond("IsPowered")||
            !Attached(recycler,collector)||!PortPairing.Matches(Sender(recycler),Receiver(collector))||!Clear(recycler)||!Clear(collector))return null;
        var container=collector.objContainer!;
        double headroom=Math.Max(0,CapacityKg-container.ContainedCOs.Sum(c=>c.GetTotalMass()));
        var packet=container.ContainedCOs.FirstOrDefault(c=>Cargo(c)&&c.GetTotalMass()<PacketKg-1e-6);
        bool created=false;
        if(packet==null)
        {
            if(container.ContainedCOs.Count>=4 || headroom<1e-6)return null;
            packet=DataHandler.GetCondOwner(Wet);WritePacket(packet,0);created=true;
            if(!container.AllowedCO(packet)||!container.CanAddSimple(packet,out var slot)){packet.Destroy();return null;}
            Mark(recycler,"pending");Mark(collector,"pending");
            container.AddCOSimple(packet,slot);
            if(packet.objCOParent!=collector)throw new InvalidOperationException("Reject collector reservation failed.");
        }
        else {Mark(recycler,"pending");Mark(collector,"pending");}
        double before=packet.GetTotalMass(),capacity=Math.Min(headroom,PacketKg-before);bool settled=false;
        return new RecyclerRejectReservation(capacity,(kg,uncertain)=>
        {
            if(settled){if(uncertain){Mark(recycler,"pending");Mark(collector,"pending");armed.Remove(recycler.strID);}return;}
            settled=true;
            if(packet.objCOParent!=collector || !Cargo(packet) || Math.Abs(packet.GetTotalMass()-before)>1e-8)throw new InvalidOperationException("Reserved rejects moved during native processing.");
            WritePacket(packet,before+kg);
            if(created && kg==0){packet.RemoveFromCurrentHome(true);packet.Destroy();}
            if(uncertain){armed.Remove(recycler.strID);return;}
            Mark(recycler,"clear");Mark(collector,"clear");container.Redraw();
        });
    }
    internal static bool Command(CondOwner recycler,string action,out string message)
    {
        message=Service.Access(recycler)??"";if(message.Length>0)return false;
        if(!Available||!IsRecycler(recycler)){message=Text.Get("capture_unavailable");return false;}
        if(!Clear(recycler)){message=Text.Get("protected");return false;}
        var peer=Peer(recycler);
        if(action.StartsWith("capture-link:",StringComparison.Ordinal))
        {
            var chosen=Service.Resolve(action.Substring("capture-link:".Length));
            if(chosen==null||!Candidates(recycler).Contains(chosen)||!Clear(chosen)||Service.Access(chosen)!=null){message=Text.Get("capture_access");return false;}
            if(!PortPairing.TryLink(Sender(recycler),Receiver(chosen),out message))return false;
            armed.Remove(recycler.strID);
        }
        else if(action=="capture-start")armed.Add(recycler.strID);
        else if(action=="capture-pause")armed.Remove(recycler.strID);
        else if(action=="capture-unlink")
        {
            armed.Remove(recycler.strID);
            PortPairing.Unlink(Sender(recycler),peer!=null&&peer.ship==recycler.ship?Receiver(peer):null);
        }
        else return false;
        message=Describe(recycler);return true;
    }
    internal static string Describe(CondOwner recycler)
    {
        var link=PortPairing.Read(Sender(recycler));var peer=Peer(recycler);
        return Text.Get("capture_status",link.State==PortLinkState.Unlinked?Text.Get("water_unlinked"):link.PeerObjectId,
            !Clear(recycler)||peer!=null&&!Clear(peer)?Text.Get("protected"):armed.Contains(recycler.strID)?Text.Get("capture_enabled"):Text.Get("capture_paused"));
    }
}
