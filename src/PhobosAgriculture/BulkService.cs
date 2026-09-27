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
    internal static ObjectStateStore Store(CondOwner co)=>new(co.mapGUIPropMaps,"AgricultureBulk",Plugin.Id,1);
    internal static ObjectStateStore Journal(CondOwner co)=>new(co.mapGUIPropMaps,"AgricultureBulkWork",Plugin.Id,1);
    internal static LiquidTransferGuard Guard(CondOwner co)=>new(co.mapGUIPropMaps,"AgricultureBulkTransfer",Plugin.Id);
    internal static StoredCommodity Read(CondOwner co)
    {
        var status=Store(co).Read(out var fields);var s=status==SavedStateStatus.Ready?StoredCommodity.Read(fields,"water",BulkDefinitions.CapacityKg):status==SavedStateStatus.Missing?new StoredCommodity("water",BulkDefinitions.CapacityKg):throw new InvalidOperationException("Protected bulk state");
        if(Math.Abs(co.GetCondAmount("StatMass")-BulkDefinitions.DryKg-s.TotalKg-Cargo(co))>1e-5)throw new InvalidOperationException("Bulk physical mass mismatch");return s;
    }
    private static double Cargo(CondOwner co)=>co.objContainer?.ContainedCOs.Sum(x=>x.GetTotalMass())??0;
    internal static bool Protected(CondOwner co)
    {
        try{Read(co);var status=Journal(co).Read(out var d);return Guard(co).Protected||status!=SavedStateStatus.Missing&&(status!=SavedStateStatus.Ready||d.Count!=1||d["state"]!="clear");}catch{return true;}
    }
    internal static void Save(CondOwner co,StoredCommodity state)
    {
        if(!Store(co).TryWrite(state.Save()))throw new InvalidOperationException("Protected bulk save");
        co.AddMass(BulkDefinitions.DryKg+state.TotalKg+Cargo(co)-co.GetCondAmount("StatMass"),true);
    }
    internal static MaterialPort Port(CondOwner co,bool source)=>new(co.strID,source?Out:In,co.mapGUIPropMaps);
    internal static string Peer(CondOwner co)=>PortPairing.Read(Port(co,BulkDefinitions.IsTank(co))).PeerObjectId;
    internal static bool HasLink(CondOwner co)=>PortPairing.Read(Port(co,BulkDefinitions.IsTank(co))).State!=PortLinkState.Unlinked;
    internal static bool HasSelection(CondOwner w2)=>PortPairing.Read(Port(w2,false)).State!=PortLinkState.Unlinked;
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
    internal static bool Geometry(CondOwner tank,CondOwner w2)
    {
        if(tank.ship==null||w2.ship!=tank.ship||!NativeFluidRoute.EndpointReady(tank)||!NativeFluidRoute.EndpointReady(w2))return false;
        float rotation=tank.Item.TF.eulerAngles.z;if(Math.Abs(Mathf.DeltaAngle(rotation,w2.Item.TF.eulerAngles.z))>.1||Math.Abs(Mathf.DeltaAngle(rotation,Mathf.Round(rotation/90)*90))>.1)return false;
        var delta=Quaternion.Euler(0,0,-rotation)*(w2.GetPos()-tank.GetPos());
        return Math.Abs(delta.x-2.5)<.05&&Math.Abs(Math.Abs(delta.y)-.5)<.05&&ServiceClear(tank,3)&&ServiceClear(w2,2);
    }
    private static bool ServiceClear(CondOwner co,int width)
    {
        for(int i=0;i<width;i++)
        {
            var offset=Quaternion.Euler(0,0,co.Item.TF.eulerAngles.z)*new Vector3(i-(width-1)/2f,-(width+1)/2f,0);
            var tile=co.ship.GetTileByIndex(co.ship.GetTileIndexAtWorldCoords1(co.GetPos()+(Vector2)offset));
            if(tile?.bPassable!=true||tile.coProps==null||!tile.coProps.HasCond("IsFloor")||tile.coProps.HasCond("IsDamaged")||tile.coProps.HasCond("IsEVATile"))return false;
        }
        return true;
    }
    internal static IEnumerable<CondOwner> Candidates(CondOwner co)=>co.ship?.GetCOs(null,false,false,true).Where(c=>BulkDefinitions.IsTank(co)?IrrigationDefinitions.IsSupply(c)&&Geometry(co,c):BulkDefinitions.IsTank(c)&&Geometry(c,co)).OrderBy(c=>c.strID,StringComparer.Ordinal)??Enumerable.Empty<CondOwner>();
    internal static bool Link(CondOwner co,string id,ConsoleBinding? binding,out string reason)
    {
        reason=Text.Get("protected");if(!Definitions.Ready)return false;
        reason=Service.Access(co,binding)??"";if(reason.Length>0)return false;
        var candidate=id=="none"?Service.Resolve(Peer(co)):Candidates(co).FirstOrDefault(c=>c.strID==id);
        if(id!="none"&&candidate==null){reason=Text.Get("bulk_pair");return false;}
        var tank=BulkDefinitions.IsTank(co)?co:candidate;var w2=BulkDefinitions.IsTank(co)?candidate:co;
        if(w2!=null&&(Service.Get(w2).State.Running||Service.Get(w2).State.Receiving)){reason=Text.Get("water_pause");return false;}
        if((tank!=null&&Protected(tank))||(w2!=null&&Service.Get(w2).Protected)){reason=Text.Get("protected");return false;}
        if(id=="none")PortPairing.Unlink(Port(co,BulkDefinitions.IsTank(co)),candidate==null?null:Port(candidate,BulkDefinitions.IsTank(candidate)));
        else if(!PortPairing.TryLink(Port(tank!,true),Port(w2!,false),out reason))return false;
        if(tank!=null)ConfigurationStamp.SuspendChangedOrder(tank);
        if(w2!=null)ConfigurationStamp.SuspendChangedOrder(w2);
        reason=Text.Get("done");return true;
    }
    internal sealed class Endpoint:ILiquidReservoir
    {
        private readonly CondOwner co;internal Endpoint(CondOwner co){this.co=co;}
        public string Identity=>co.strID;public string ShipId=>co.ship.strRegID;public string Commodity=>"water";
        public double QuantityKg=>Read(co).ServiceKg;public double CapacityKg=>BulkDefinitions.CapacityKg-Read(co).CatchKg;
        public void SetQuantity(double kg){var s=Read(co);s.SetService(kg);Save(co,s);}
    }
    internal static double Intake(CondOwner w2,ILiquidReservoir destination,double requested,bool commit)
    {
        var tank=Service.Resolve(Peer(w2));if(tank==null||!BulkDefinitions.IsTank(tank)||!Geometry(tank,w2)||!PortPairing.Matches(Port(tank,true),Port(w2,false))||Protected(tank)||CommodityReservations.Held(tank.strID))return 0;
        var s=Read(tank);if(s.CatchKg>1e-8)return 0;
        if(!TryTarget(w2,out double target))return 0;
        double kg=Math.Min(requested,Math.Min(s.AvailableKg,Math.Max(0,Math.Min(target,destination.CapacityKg)-destination.QuantityKg)));
        return kg<=1e-8?0:commit?LiquidTransferGuard.Commit(new Endpoint(tank),destination,kg,Guard(tank),Service.WaterGuard(w2)).ReceivedKg:kg;
    }
    internal static string Describe(CondOwner co)
    {
        if(Protected(co))return Text.Get("protected");var s=Read(co);
        return Text.Get("bulk_status",s.ServiceKg,s.CatchKg,BulkDefinitions.CapacityKg,s.ReserveKg,ObjectPresentation.Name(Peer(co)))+(s.CatchKg>0?"\n"+Text.Get("bulk_catch_wait"):"");
    }
    internal static bool Command(CondOwner co,ConsoleBinding? binding,string action,out string reason)
    {
        reason=Text.Get("protected");if(!Definitions.Ready)return false;
        reason=Service.Access(co,binding)??"";if(reason.Length>0)return false;
        if(action.StartsWith("bulk-link:",StringComparison.Ordinal))return Link(co,action.Substring(10),binding,out reason);
        if(action!="pause"&&Protected(co)){reason=Text.Get("protected");return false;}
        try
        {
            if(action=="pause") {var w2=Service.Resolve(Peer(co));if(w2!=null&&IrrigationDefinitions.IsSupply(w2)){Service.CrewSuspend(w2);CrewWork.ManualStop(w2);}CrewWork.ManualStop(co);reason=Text.Get("paused");return true;}
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
        var w2=Service.Resolve(Peer(co));if(w2==null||!IrrigationDefinitions.IsSupply(w2))return;
        Service.CrewSuspend(w2);ConfigurationStamp.SuspendChangedOrder(w2);
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
                if(input==null||s.CatchKg>0||s.TotalKg+Definitions.IrrigationKg>BulkDefinitions.CapacityKg)return false;
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
    private static void Pending(CondOwner co,string item,double kg)
    {if(!Journal(co).TryWrite(new Dictionary<string,string>{["state"]="pending",["item"]=item,["before"]=kg.ToString("R",CultureInfo.InvariantCulture)}))throw new InvalidOperationException("Protected bulk conversion");}
    private static void Clear(CondOwner co){if(!Journal(co).TryWrite(new Dictionary<string,string>{["state"]="clear"}))throw new InvalidOperationException("Bulk conversion incomplete");}
}
