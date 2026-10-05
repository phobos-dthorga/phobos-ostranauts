using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using HarmonyLib;
using UnityEngine;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;

namespace PhobosAgriculture;
internal static class BulkService
{
    internal const string Out="PhobosAgriculture.BulkOut",In="PhobosAgriculture.BulkIn";
    // Custody lives in Framework BulkVessel since Agriculture 0.18.0 (Framework 0.39.0); the R3 declaration keeps
    // the record, journal and guard names every saved R3 already carries.
    internal static LiquidTransferGuard Guard(CondOwner co)=>BulkVessel.Guard(co);
    internal static StoredCommodity Read(CondOwner co)=>BulkVessel.Read(co);
    internal static bool Protected(CondOwner co)=>BulkVessel.Protected(co);
    /// <summary>Owner-confirmed recovery of a protected tank: the readable record is trusted, interrupted
    /// transfer and conversion journals are closed and the item's mass is set back to dry mass plus record
    /// plus cargo. An unreadable record cannot be accepted.</summary>
    internal static bool Accept(CondOwner co,out string reason)
    {
        bool accepted=BulkVessel.Accept(co,Plugin.Log);
        reason=Text.Get(accepted?"accept_done":"accept_unavailable");return accepted;
    }
    internal static void Save(CondOwner co,StoredCommodity state)=>BulkVessel.Save(co,state);
    /// <summary>The W2's intake (Agriculture 0.30.0): a water vessel feeds it while the two touch or share a
    /// process-water line, and the vessel's outlet is a sending bank, so one tank or silo may feed several W2s. A pair
    /// saved before reads as the bank's first slot.</summary>
    internal static readonly VesselLink WaterLink=new(In,Out,LineFamilies.Water,machineSends:false);
    private static CondOwner? Find(string id)=>Service.Resolve(id);
    /// <summary>The vessel side of a link is any water vessel (a reservoir of any size or a Shipbreaker silo); the W2 is the other side.</summary>
    private static bool VesselSide(CondOwner co)=>!IrrigationDefinitions.IsSupply(co);
    internal static bool IsWaterVessel(CondOwner? co)=>co!=null&&BulkVessels.Of(co)?.Commodity=="water";
    private static PortBank Bank(CondOwner tank)=>SharedPorts.Bank(tank,Out,PortRole.Sender);
    /// <summary>The W2s a vessel feeds, from its outlet bank's saved slots, when each W2 still points back.</summary>
    internal static IEnumerable<CondOwner> Supplies(CondOwner tank)=>Bank(tank).Ports.Select(PortPairing.Read).Where(l=>l.State==PortLinkState.Linked)
        .Select(l=>Find(l.PeerObjectId)).Where(c=>c!=null&&IrrigationDefinitions.IsSupply(c)&&WaterLink.PeerId(c)==tank.strID).Select(c=>c!).ToArray();
    /// <summary>A W2's vessel, or a vessel's first W2 (its panel lists them all).</summary>
    internal static string Peer(CondOwner co)=>VesselSide(co)?Supplies(co).FirstOrDefault()?.strID??"":WaterLink.PeerId(co);
    internal static bool HasLink(CondOwner co)=>VesselSide(co)?Bank(co).Occupied:HasSelection(co);
    internal static bool HasSelection(CondOwner w2)=>PortPairing.Read(WaterLink.Ours(w2)).State!=PortLinkState.Unlinked;
    private static ObjectStateStore Settings(CondOwner w2)=>new(w2.mapGUIPropMaps,"AgricultureBulkSettings",Plugin.Id,1);
    internal static bool TryTarget(CondOwner w2,out double value)
    {
        value=19.5;var status=Settings(w2).Read(out var d);if(status==SavedStateStatus.Missing)return true;
        return status==SavedStateStatus.Ready&&d.Count==1&&d.TryGetValue("target",out var raw)&&double.TryParse(raw,NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&!double.IsNaN(value)&&value>=0&&value<=19.5;
    }
    internal static bool SetTarget(CondOwner w2,ConsoleBinding? binding,string raw,out string reason)
    {
        reason=Text.Get("protected");if(!Definitions.Ready)return false;
        reason=Service.Access(w2,binding)??"";if(reason.Length>0)return false;
        if(!TryTarget(w2,out _)){reason=Text.Get("protected");return false;}
        var s=Service.Get(w2);if(s.Protected||s.State.Running||s.State.Receiving){reason=Text.Get("water_pause");return false;}
        if(!double.TryParse(raw,NumberStyles.Float,CultureInfo.InvariantCulture,out double target)||double.IsNaN(target)||target<0||target>19.5||!Settings(w2).TryWrite(new Dictionary<string,string>{["target"]=target.ToString("R",CultureInfo.InvariantCulture)})){reason=Text.Get("protected");return false;}
        reason=Text.Get("done");return true;
    }
    /// <summary>A water vessel reaches a W2 when both are ready on the same ship and they touch (within one tile) or
    /// share a process-water line (Agriculture 0.30.0). The R3's original edge-to-edge placement still qualifies.</summary>
    internal static bool Geometry(CondOwner tank,CondOwner w2)=>tank.ship!=null&&w2.ship==tank.ship&&IsWaterVessel(tank)&&IrrigationDefinitions.IsSupply(w2)&&
        NativeFluidRoute.EndpointReady(tank)&&NativeFluidRoute.EndpointReady(w2)&&WaterLink.Reach(w2,tank)!=LineReachKind.None;
    internal static IEnumerable<CondOwner> Candidates(CondOwner co)=>co.ship?.GetCOs(null,false,false,true).Where(c=>VesselSide(co)?IrrigationDefinitions.IsSupply(c)&&Geometry(co,c):IsWaterVessel(c)&&Geometry(c,co)).OrderBy(c=>c.strID,StringComparer.Ordinal)??Enumerable.Empty<CondOwner>();
    /// <summary>Why a water tank aboard is not offered to a W2 (Agriculture 0.35.0): loose, damaged, or no water line
    /// touching it; from the tank's side, why a W2 aboard is not offered (Agriculture 0.53.0). Empty when everything is offered.</summary>
    internal static string LinkNote(CondOwner co)=>co.ship==null?"":VesselSide(co)
        ?Phobos.Ostranauts.Framework.Controls.LinkChoices.Note(co,LineFamilies.ProcessWater,co.ship.GetCOs(null,false,false,true).Where(c=>c!=co&&c.ship==co.ship&&IrrigationDefinitions.IsSupply(c)),Candidates(co))
        :Phobos.Ostranauts.Framework.Controls.LinkChoices.Note(co,LineFamilies.ProcessWater,BulkVessels.AboardAnyState(co.ship,LineFamilies.Water),Candidates(co));
    internal static bool Link(CondOwner co,string id,ConsoleBinding? binding,out string reason)
    {
        reason=Text.Get("protected");if(!Definitions.Ready)return false;
        reason=Service.Access(co,binding)??"";if(reason.Length>0)return false;
        bool vessel=VesselSide(co);
        // Clearing a vessel's selection releases every W2 it feeds; clearing a W2's releases its one vessel.
        var candidates=id=="none"?(vessel?Supplies(co).ToArray():new[]{Service.Resolve(Peer(co))}.Where(c=>c!=null).Select(c=>c!).ToArray()):Candidates(co).Where(c=>c.strID==id).Take(1).ToArray();
        if(id!="none"&&candidates.Length==0){reason=Text.Get("bulk_pair");return false;}
        var pairs=candidates.Select(c=>(Tank:vessel?co:c,W2:vessel?c:co)).ToArray();
        if(pairs.Length==0&&!vessel)pairs=new[]{((CondOwner)null!,co)};
        foreach(var (tank,w2) in pairs)
        {
            if(w2!=null&&(Service.Get(w2).State.Running||Service.Get(w2).State.Receiving)){reason=Text.Get("water_pause");return false;}
            if((tank!=null&&Protected(tank))||(w2!=null&&Service.Get(w2).Protected)){reason=Text.Get("protected");return false;}
        }
        foreach(var (tank,w2) in pairs)
        {
            if(id=="none"){if(w2!=null)WaterLink.Unlink(w2,Find);}
            else if(!WaterLink.Link(w2!,tank!,Find,out reason))return false;
            if(tank!=null)ConfigurationStamp.SuspendChangedOrder(tank);
            if(w2!=null)ConfigurationStamp.SuspendChangedOrder(w2);
        }
        reason=Text.Get("done");return true;
    }
    internal static ILiquidReservoir Endpoint(CondOwner co)=>new BulkVessel.Endpoint(co);
    internal static double Intake(CondOwner w2,ILiquidReservoir destination,double requested,bool commit)
    {
        var tank=Service.Resolve(Peer(w2));if(tank==null||!IsWaterVessel(tank)||!Geometry(tank,w2)||!WaterLink.Paired(w2,tank)||Protected(tank)||CommodityReservations.Held(tank.strID))return 0;
        var s=Read(tank);if(s.CatchKg>1e-8)return 0;
        if(!TryTarget(w2,out double target))return 0;
        double kg=Math.Min(requested,Math.Min(s.AvailableKg,Math.Max(0,Math.Min(target,destination.CapacityKg)-destination.QuantityKg)));
        return kg<=1e-8?0:commit?LiquidTransferGuard.Commit(Endpoint(tank),destination,kg,Guard(tank),Service.WaterGuard(w2)).ReceivedKg:kg;
    }
    internal static string Describe(CondOwner co)
    {
        if(Protected(co))return Text.Get("protected");var s=Read(co);
        // Every machine linked to the vessel, from any mod (Agriculture 0.30.0: tanks are shared).
        var linked=LinkChoices.LinkedNames(co);
        return Text.Get("bulk_status",s.ServiceKg,s.CatchKg,BulkDefinitions.CapacityOf(co),s.ReserveKg,linked.Count==0?ObjectPresentation.Name(""):string.Join(", ",linked))+(s.CatchKg>0?"\n"+Text.Get("bulk_catch_wait"):"");
    }
    internal static bool Command(CondOwner co,ConsoleBinding? binding,string action,out string reason)
    {
        reason=Text.Get("protected");if(!Definitions.Ready)return false;
        reason=Service.Access(co,binding)??"";if(reason.Length>0)return false;
        if(action.StartsWith("bulk-link:",StringComparison.Ordinal))return Link(co,action.Substring(10),binding,out reason);
        if(action=="bulk-accept")return Accept(co,out reason);
        if(action!="pause"&&Protected(co)){reason=Text.Get("protected");return false;}
        try
        {
            if(action=="pause") {foreach(var w2 in Supplies(co)){Service.CrewSuspend(w2);CrewWork.ManualStop(w2);}CrewWork.ManualStop(co);reason=Text.Get("paused");return true;}
            if(action.StartsWith("bulk-reserve:",StringComparison.Ordinal))
            {var s=Read(co);s.SetReserve(double.Parse(action.Substring(13),CultureInfo.InvariantCulture));Save(co,s);PauseSupply(co);reason=Text.Get("done");return true;}
            if(BulkDefinitions.Work.Contains(action)&&binding==null)
            {CrewSim.GetSelectedCrew().QueueInteraction(co,DataHandler.GetInteraction(BulkDefinitions.WorkId(action)));reason=Text.Get("queued");return true;}
            reason=Describe(co);return action=="status";
        }
        catch(Exception e){Plugin.Log(e.ToString());reason=Text.Get("protected");return false;}
    }
    internal static void PauseSupply(CondOwner co)
    {
        foreach(var w2 in Supplies(co)){Service.CrewSuspend(w2);ConfigurationStamp.SuspendChangedOrder(w2);}
    }
    internal static bool Work(CondOwner co,CondOwner actor,string action)
    {
        if(!Definitions.Ready||Service.Access(co,null,actor)!=null||Protected(co)||!NativeFluidRoute.EndpointReady(co)||CommodityReservations.Held(co.strID))return false;
        var s=Read(co);CondOwner? product=null;bool published=false;
        try
        {
            if(action=="bulk-recover"){if(s.CatchKg<=0)return false;s.Recover();Save(co,s);return true;}
            if(action=="bulk-load")
            {
                var input=Service.Input(co,Definitions.Irrigation,Definitions.IrrigationKg);
                if(input==null||s.CatchKg>0||s.TotalKg+Definitions.IrrigationKg>BulkDefinitions.CapacityOf(co))return false;
                Pending(co,input.strID,s.TotalKg);input.RemoveFromCurrentHome(true);
                if(input.objCOParent!=null||input.ship!=null)throw new InvalidOperationException("Bulk input did not detach");
                s.SetService(s.ServiceKg+Definitions.IrrigationKg);Save(co,s);input.Destroy();Clear(co);co.objContainer.Redraw();return true;
            }
            if(action!="bulk-drain"||s.ServiceKg<=0||co.objContainer==null||co.objContainer.Locked)return false;
            double kg=Math.Min(20,s.ServiceKg);product=DataHandler.GetCondOwner(Service.CharacterizedDrainage);product.SetCondAmount("StatMass",kg);
            if(!new ObjectStateStore(product.mapGUIPropMaps,"AgricultureDrainage",Plugin.Id,1).TryWrite(Core.DrainageRecovery.Save(new LiquidMixture(kg,0))))throw new InvalidOperationException("Protected drainage record");
            if(!co.objContainer.AllowedCO(product)||!co.objContainer.CanAddSimple(product,out var placement))return false;
            Pending(co,product.strID,s.TotalKg);co.objContainer.AddCOSimple(product,placement);
            if(product.objCOParent!=co)throw new InvalidOperationException("Bulk drain placement failed");published=true;
            s.SetService(s.ServiceKg-kg);Save(co,s);Clear(co);co.objContainer.Redraw();return true;
        }
        catch(Exception e){Plugin.Log(e.ToString());return false;}
        finally{if(product!=null&&!published&&product.objCOParent==null)product.Destroy();}
    }
    private static void Pending(CondOwner co,string item,double kg)=>BulkVessel.BeginConversion(co,item,kg);
    private static void Clear(CondOwner co)=>BulkVessel.EndConversion(co);
}
