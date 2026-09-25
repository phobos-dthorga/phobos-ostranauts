using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    private sealed class Departure
    {
        internal CondOwner Console = null!;
        internal Dictionary<string,string> Fields = new(StringComparer.Ordinal);
        internal string this[string key] { get => Fields.TryGetValue(key,out var value) ? value : ""; set => Fields[key]=value; }
    }
    private Departure? departure;
    private int departureMode;
    private static readonly string[] DepartureModes = { "fly", "rendezvous", "follow", "dock", "approachdock" };
    private static ObjectStateStore DepartureStore(CondOwner co) => new(co.mapGUIPropMaps,"AutoNav.Departure",co.strID,1);
    private static bool SaveDeparture(Departure d) => DepartureStore(d.Console).TryWrite(d.Fields);
    internal string DepartureDescription(CondOwner? co)
    {
        string mode=Text.Get("Departure.mode_"+DepartureModes[departureMode]);
        if (co == null) return mode;
        var state=DepartureStore(co).Read(out var fields);
        return mode+"\n"+(state == SavedStateStatus.Missing ? Text.Get("Departure.help") :
            state == SavedStateStatus.Ready && fields.TryGetValue("phase",out var phase) ? Text.Get("Departure.phase",Text.Get("Departure.phase_"+phase))+"\n"+status : Text.Get("Persistence.invalid_state"));
    }
    internal void DepartureAction(CondOwner? co,string action)
    {
        bool handled=false; ExtendedCommand(co,action,ref handled);
    }
    partial void ExtendedCommand(CondOwner? co,string action,ref bool handled)
    {
        if (!action.StartsWith("depart",StringComparison.Ordinal)) return;
        handled=true;
        try
        {
            if (!IsLocalConsole(co) || !Plugin.Enabled.Value || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading)
            { status=Text.Get("Persistence.open_console"); return; }
            if(action=="depart-mode") { if(departure==null) departureMode=(departureMode+1)%DepartureModes.Length; return; }
            if(action=="depart-stop")
            {
                Disengage(Text.Get("Departure.stopped"));
                if(DepartureStore(co!).Read(out var stopped)==SavedStateStatus.Ready && DepartureRules.Valid(stopped) && stopped["phase"]!="DetachPending")
                { var cancelled=new Departure { Console=co!,Fields=stopped.ToDictionary(p=>p.Key,p=>p.Value) };cancelled["phase"]="Cancelled";SaveDeparture(cancelled); }
                return;
            }
            if(action=="depart-resume") { ResumeDeparture(co!); return; }
            if(action!="depart" && action!="depart-continue") return;
            if(AutoNavCore.Engaged || industrial!=null || departure!=null || OtherControllerBusyExceptIndustrial() || DisplaySnapshot(co)!=null)
            { status=Text.Get("Industrial.busy"); return; }
            var existing=DepartureStore(co!).Read(out var old);
            if(existing!=SavedStateStatus.Missing && (existing!=SavedStateStatus.Ready || !old.TryGetValue("phase",out var phase) || phase!="Complete" && phase!="Cancelled"))
            { status=Text.Get("Departure.resume"); return; }
            var attachments=co!.ship.GetDockedShipsAndPortIDs();
            if(attachments.Count!=1) { status=Text.Get("Departure.attachment"); return; }
            var link=attachments.Single(); var peer=link.Value;
            var d=new Departure { Console=co };
            d["owner"]=CrewSim.coPlayer.strID; d["ship"]=co.ship.strRegID; d["console"]=co.strID;
            d["module"]=IndustrialNavigation.ModuleId(co) ?? ""; d["peer"]=peer.strRegID;
            d["ownPort"]=link.Key; d["peerPort"]=peer.GetPortIdForDockedShip(co.ship.strRegID);
            d["permission"]=Guid.NewGuid().ToString("N"); d["phase"]="Prepared";
            d["kind"]=co.ship.IsMooredWith(peer)?"Moor":"Dock";
            d["operation"]=action=="depart" ? "none" : DepartureModes[departureMode];
            if(action=="depart-continue")
            {
                var target=GUIOrbitDraw.CrossHairTarget?.Ship;
                if(target==null || target==co.ship || target==peer || !NativeContactReader.Read(co.ship,target.strRegID).Usable)
                { status=Text.Get("Docking.select"); return; }
                if((d["operation"]=="follow" || d["operation"]=="rendezvous") && !HasPursuit(co))
                { status=Text.Get("Pursuit.module_required"); return; }
                if(!ReadPreferences(co,out var prefs)) { status=Text.Get("Preferences.invalid"); return; }
                var mode=d["operation"]=="follow"?SavedFlightMode.Following:d["operation"]=="rendezvous"?SavedFlightMode.Rendezvous:SavedFlightMode.Active;
                var f=CaptureFlight(co,TargetRef.FromShipId(target.strRegID)!,prefs.CruiseMS,
                    d["operation"]=="fly"?prefs.ArrivalMS:0,prefs.ArrivalKM,mode);
                f.Mode=f.SuspendedMode;f.Coast=Plugin.ReadCoastSettings();f.PreferTorch=Plugin.PreferTorch.Value;
                foreach(var field in f.Encode()) d["flight."+field.Key]=field.Value;
                d["target"]=target.strRegID;
            }
            string? problem=DepartureProblem(d,true);
            if(problem!=null) { status=problem; return; }
            if(!SaveDeparture(d)) { status=Text.Get("Persistence.write_failed"); return; }
            departure=d; CommitDeparture(d);
        }
        catch(Exception ex) { log(ex.ToString()); StopExtended(Text.Get("Departure.uncertain")); }
    }
    private static string? DepartureProblem(Departure d,bool attached)
    {
        var co=d.Console; var own=co.ship; var peer=CrewSim.system?.GetShipByRegID(d["peer"]);
        if(CrewSim.system==null||CrewSim.coPlayer==null||own==null||peer==null) return Text.Get("Departure.hardware");
        if(!Plugin.Enabled.Value || co.strID!=d["console"] || CrewSim.coPlayer?.strID!=d["owner"] || own.strRegID!=d["ship"] ||
            !IsLocalConsole(co) || !HasIndustrialModule(co,d["module"]) || peer==null || peer.bDestroyed || peer.IsGroundStation() ||
            co.HasCond("IsDamaged") || co.HasCond("IsDamagedSoftware") || !co.HasCond("IsPowered") || co.HasCond("IsOff") ||
            CrewSim.system!.IsInAtmo(own) || ReadThrottle(co)<=0 || OtherControllerBusyExceptIndustrial()) return Text.Get("Departure.hardware");
        if(own.RCSCount<=0||own.GetRCSRemain()<=0||own.objSS==null||!ArrivalBrake.Finite(own.RCSAccelMax)||own.RCSAccelMax<=0) return Text.Get("Departure.reserve");
        var nativeProblem=NativeControlProblem(co);if(nativeProblem!=null)return nativeProblem;
        if(CrewSim.aCrew==null || CrewSim.aCrew.Any(c=>c!=null && !c.bDestroyed && c.ship!=own)) return Text.Get("Departure.crew");
        if(own.GetCOs(null,false,false,true).Any(c=>c.HasCond("IsInstalled") && c.HasCond("IsDockSys") && (c.HasCond("IsOpen") || c.HasCond("IsDamaged")))) return Text.Get("Departure.seal");
        // Native attached delta-v includes the station's mass; departure uses this ship's own propulsion and mass.
        double reserve=own.DeltaVRemainingRCS*(attached?own.RCSAccelMaxUndocked/own.RCSAccelMax:1)/AutoNavCore.M_TO_AU;
        if(!NativeContactReader.Read(own,peer.strRegID).Usable || !ArrivalBrake.Finite(reserve) ||
            reserve < (attached?DepartureRules.MinimumReserveMS:0)) return Text.Get("Departure.reserve");
        if(attached)
        {
            if(own.GetDockedShipsAndPortIDs().Count!=1 || own.GetPortIdForDockedShip(peer.strRegID)!=d["ownPort"] ||
                peer.GetPortIdForDockedShip(own.strRegID)!=d["peerPort"] || !own.IsDockedWith(peer) || own.TowBraceSecured(peer.strRegID)) return Text.Get("Departure.attachment");
            var port=peer.GetCOs(null,false,false,true).FirstOrDefault(c=>c.strID==d["peerPort"]);
            if(d["kind"]=="Dock" && (port==null || port.HasCond("IsOpen") || port.HasCond("IsDamaged"))) return Text.Get("Departure.seal");
            var clearance=own.Comms?.Clearance;
            if((peer.IsStation()||CrewSim.system.GetShipOwner(peer.strRegID)!=d["owner"]) && (clearance==null || clearance.TargetRegId!=peer.strRegID || clearance.ClearanceType!="PUSHBACK & TAXI")) return Text.Get("Departure.clearance");
            var delta=new NavVector(own.objSS.vPosx-peer.objSS.vPosx,own.objSS.vPosy-peer.objSS.vPosy);
            if(delta.Length<=0) return Text.Get("Departure.exit");
            double distance=CollisionManager.GetCollisionDistanceAU(own,peer)+1000*AutoNavCore.M_TO_AU;
            var end=new NavVector(peer.objSS.vPosx,peer.objSS.vPosy)+delta.Unit*distance;
            foreach(var body in CrewSim.system.aBOs.Values)
            {
                if(body==null||body.IsAsteroidField||body.nDrawFlagsBody==ContactRules.PlaceholderBodyDrawFlag) continue;
                double radius=body.fRadius+own.objSS.GetRadiusAU()+1000*AutoNavCore.M_TO_AU;
                if(ObstacleRoute.Distance(new NavVector(own.objSS.vPosx,own.objSS.vPosy),end,new NavVector(body.dXReal,body.dYReal))<=radius) return Text.Get("Departure.exit");
            }
            foreach(var other in CrewSim.system.dictShips.Values)
            {
                if(other==null || other==own || other==peer || !NativeContactReader.Read(own,other.strRegID).Usable) continue;
                double speed=new NavVector(other.objSS.vVelX-peer.objSS.vVelX,other.objSS.vVelY-peer.objSS.vVelY).Length;
                double radius=CollisionManager.GetCollisionDistanceAU(own,other)+ObstacleRoute.MinimumMarginM*AutoNavCore.M_TO_AU+speed*60;
                if(ObstacleRoute.Distance(new NavVector(own.objSS.vPosx,own.objSS.vPosy),end,new NavVector(other.objSS.vPosx,other.objSS.vPosy))<=radius) return Text.Get("Departure.exit");
            }
        }
        else if(own.IsDocked()||own.IsMoored()) return Text.Get("Departure.attachment");
        return null;
    }
    private void CommitDeparture(Departure d)
    {
        var problem=DepartureProblem(d,true); if(problem!=null) { status=problem; departure=null; return; }
        d["phase"]="DetachPending"; if(!SaveDeparture(d)) { status=Text.Get("Persistence.write_failed"); departure=null; return; }
        CeaseFire(); Torch.Release();
        var own=d.Console.ship; var peer=CrewSim.system.GetShipByRegID(d["peer"])!;
        if(d["kind"]=="Moor") CrewSim.UnMoorShip(own,peer); else CrewSim.UndockShip(own,peer,true);
        if(own.IsDockedWith(peer)) { StopExtended(Text.Get("Departure.uncertain")); return; }
        d["phase"]="Detached"; if(!SaveDeparture(d)) { StopExtended(Text.Get("Persistence.write_failed")); return; }
        BeginEgress(d);
    }
    private void BeginEgress(Departure d)
    {
        var problem=DepartureProblem(d,false); if(problem!=null) { StopExtended(problem); return; }
        var own=d.Console.ship; var peer=CrewSim.system.GetShipByRegID(d["peer"])!;
        double bearing=Math.Atan2(own.objSS.vPosy-peer.objSS.vPosy,own.objSS.vPosx-peer.objSS.vPosx)*180/Math.PI;
        if(!RequestIndustrialMove(d["permission"],d.Console,d["module"],d["peer"],IndustrialMove.Egress,bearing,1001,0,
            ()=>DepartureProblem(d,false),out status)) { StopExtended(status); return; }
        d["phase"]="Departing"; if(!SaveDeparture(d)) { StopExtended(Text.Get("Persistence.write_failed")); return; }
        departure=d;
    }
    private void ResumeDeparture(CondOwner co)
    {
        if(departure!=null || industrial!=null || AutoNavCore.Engaged) { status=Text.Get("Industrial.busy"); return; }
        if(DepartureStore(co).Read(out var fields)!=SavedStateStatus.Ready || !DepartureRules.Valid(fields))
        { status=Text.Get("Persistence.invalid_state");return; }
        var d=new Departure { Console=co,Fields=fields.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal) };
        var peer=CrewSim.system.GetShipByRegID(d["peer"]);
        if(peer==null||co.strID!=d["console"]||co.ship.strRegID!=d["ship"]||CrewSim.coPlayer.strID!=d["owner"])
        { status=Text.Get("Persistence.binding_changed");return; }
        bool attached=co.ship.IsDockedWith(peer);
        bool HasPort(Ship ship,string id)=>ship.LoadState>=Ship.Loaded.Edit?ship.GetMappedCos().Any(p=>p.Key==id):ship.json.aItems.Any(i=>i.strID==id);
        bool orphan=!attached&&d["kind"]=="Moor"&&(HasPort(co.ship,d["ownPort"])||HasPort(peer,d["peerPort"]));
        switch(DepartureRules.Reconcile(d["phase"],attached,co.ship.GetPortIdForDockedShip(peer.strRegID)==d["ownPort"]&&peer.GetPortIdForDockedShip(co.ship.strRegID)==d["peerPort"],
            co.ship.GetDockedShipsAndPortIDs().Count>(attached?1:0),orphan))
        {
            case DepartureRecovery.Finished: status=Text.Get("Departure.complete");return;
            case DepartureRecovery.Detach: departure=d;CommitDeparture(d);return;
            case DepartureRecovery.Egress: departure=d;BeginEgress(d);return;
            case DepartureRecovery.Continue: departure=d;ContinueDeparture(d);return;
            default: status=Text.Get("Departure.uncertain");return;
        }
    }
    internal void TickDeparture(double dt)
    {
        if(departure==null || CrewSim.Paused || dt<=0) return;
        var d=departure;
        if(d["phase"]!="Departing") return;
        if(!ObserveIndustrial(d["permission"],out bool ready,out var message)) { StopExtended(message); return; }
        if(!ready) return;
        ReleaseIndustrial(d["permission"]); d["phase"]="Continue";
        if(!SaveDeparture(d)) { StopExtended(Text.Get("Persistence.write_failed")); return; }
        ContinueDeparture(d);
    }
    private void ContinueDeparture(Departure d)
    {
        departure=null;
        if(DepartureProblem(d,false)!=null) { status=DepartureProblem(d,false)!; return; }
        if(d["operation"]=="none") { d["phase"]="Complete"; status=SaveDeparture(d)?Text.Get("Departure.complete"):Text.Get("Persistence.write_failed"); return; }
        if(d["operation"]=="dock") Dock(d.Console,d["target"]);
        else if(d["operation"]=="approachdock") ApproachDock(d.Console,d["target"]);
        else
        {
            var fields=d.Fields.Where(p=>p.Key.StartsWith("flight.",StringComparison.Ordinal)).ToDictionary(p=>p.Key.Substring(7),p=>p.Value);
            if(!FlightSnapshot.TryDecode(fields,out var snapshot) || snapshot.TargetId!=d["target"] || !Store(d.Console).TryWrite(fields))
            { status=Text.Get("Persistence.invalid_state"); return; }
            ResumeSaved(d.Console);
        }
        if(AutoNavCore.Engaged) { d["phase"]="Complete"; if(!SaveDeparture(d)) Disengage(Text.Get("Persistence.write_failed")); }
    }
    partial void StopExtended(string reason)
    {
        var d=departure; departure=null;
        previousThreats.Clear();obstacleRoute.Reset();routeTarget="";avoidanceActive=avoidanceBlocked=false;
        if(d!=null) { ReleaseIndustrial(d["permission"]); SaveDeparture(d); }
        status=reason;
    }
    partial void ResetExtended()
    { departure=null; obstacleRoute.Reset(); previousThreats.Clear(); bodyMotion.Clear(); nextRoute=0; routeTarget=""; }
}
