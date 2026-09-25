using System;
using System.Linq;
using PhobosAutoNav;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;
internal static class DepartureChecks
{
    private static (NavigationService Service,CondOwner Console,Ship Own,Ship Peer) Setup(bool station=false,bool mooring=false)
    {
        AutoNavCore.ResetStatics();AutoNavCore.Busy=false;NativeContactReader.State=ContactState.Ready;
        CrewSim.system=new();CrewSim.objInstance.FinishedLoading=true;CrewSim.Paused=false;StarSystem.fEpoch=100;
        CrewSim.DetachCalls=0;CrewSim.DuringDetach=null;CrewSim.aCrew.Clear();
        var own=new Ship { strRegID="own",Attached=true,Moored=mooring };
        var peer=new Ship { strRegID="peer",Station=station,Attached=true };
        peer.objSS.vPosx=210*AutoNavCore.M_TO_AU;
        var co=new CondOwner { strID="console",ship=own };
        co.Items.Add(new CondOwner { strID="module",Kind=NavigationService.ModuleId,ship=own });
        own.Items.Add(co);
        string a=mooring?"MP|own":"own-port",b=mooring?"MP|peer":"peer-port";
        var ownPort=new CondOwner { strID=a,ship=own };ownPort.Conditions.Add("IsDockSys");own.Items.Add(ownPort);
        peer.Items.Add(new CondOwner { strID=b,ship=peer });
        own.Attachments[a]=peer;peer.Attachments[b]=own;
        own.Comms.Clearance=new Clearance { TargetRegId="peer",DockID=b,ClearanceType="PUSHBACK & TAXI" };
        CrewSim.coPlayer=new CondOwner { strID="captain",ship=own };CrewSim.aCrew.Add(CrewSim.coPlayer);
        CrewSim.system.Ships[own.strRegID]=own;CrewSim.system.Ships[peer.strRegID]=peer;
        return (new NavigationService(),co,own,peer);
    }
    private static string Phase(CondOwner co)
    { new ObjectStateStore(co.mapGUIPropMaps,"AutoNav.Departure",co.strID,1).Read(out var fields);return fields.TryGetValue("phase",out var value)?value:"missing"; }
    internal static void Run(Action<bool,string> check)
    {
        foreach(var kind in new[]{"station","ship","mooring"})
        {
            var s=Setup(kind=="station",kind=="mooring");
            if(kind=="station") s.Peer.Attachments["another-port"]=new Ship { strRegID="station-neighbour" };
            s.Service.DepartureAction(s.Console,"depart");
            check(CrewSim.DetachCalls==1&&Phase(s.Console)=="Departing",kind+" uses exactly one native departure and then RCS egress: "+s.Service.Diagnostic);
            if(kind=="station") check(s.Peer.Attachments.ContainsKey("another-port"),"Other station attachments remain intact");
            for(int i=0;i<3000&&Phase(s.Console)!="Complete";i++)
            {
                s.Service.TickDeparture(.5);
                if(!s.Service.GuardNavigation(.5)) s.Service.TickIndustrial(.5,false);
                double ax=s.Own.LastX,ay=s.Own.LastY;
                s.Own.objSS.vPosx+=(s.Own.objSS.vVelX*.5+ax*AutoNavCore.M_TO_AU*.125);
                s.Own.objSS.vPosy+=(s.Own.objSS.vVelY*.5+ay*AutoNavCore.M_TO_AU*.125);
                s.Own.objSS.vVelX+=ax*AutoNavCore.M_TO_AU*.5;s.Own.objSS.vVelY+=ay*AutoNavCore.M_TO_AU*.5;
                StarSystem.fEpoch+=.5;
                if(!s.Service.avoidanceActive) s.Service.TickIndustrial(.5,true);
            }
            check(Phase(s.Console)=="Complete",kind+" departure reaches 1 km hull gap and relative stop: "+s.Service.Diagnostic+" x="+s.Own.objSS.vPosx/AutoNavCore.M_TO_AU+" speed="+s.Own.objSS.vVelX/AutoNavCore.M_TO_AU);
        }
        {
            var s=Setup(true);s.Own.RCSAccelMax*=.001;s.Own.DeltaVRemainingRCS*=.001;
            s.Service.DepartureAction(s.Console,"depart");
            check(CrewSim.DetachCalls==1,"Departure reserve excludes the attached station mass using native undocked acceleration");
            s.Service.WorldChanging();
        }
        foreach(var failure in new[]{"clearance","open","crew","tow","extra","ground","fuel","sensor","rcs","native"})
        {
            var s=Setup(true);
            switch(failure)
            {
                case "clearance":s.Own.Comms.Clearance=null;break;
                case "open":s.Own.Items.Last().Conditions.Add("IsOpen");break;
                case "crew":CrewSim.aCrew.Add(new CondOwner { ship=s.Peer });break;
                case "tow":s.Own.Towed=true;break;
                case "extra":s.Own.Attachments["extra"]=new Ship();break;
                case "ground":s.Peer.Ground=true;break;
                case "fuel":s.Own.DeltaVRemainingRCS=1*AutoNavCore.M_TO_AU;break;
                case "sensor":NativeContactReader.State=ContactState.Weak;break;
                case "rcs":s.Own.RCSCount=0;break;
                case "native":s.Console.mapGUIPropMaps["chkEngage"]=new(){{"value","true"}};break;
            }
            s.Service.DepartureAction(s.Console,"depart");
            check(CrewSim.DetachCalls==0&&s.Own.Attached&&s.Own.LastX==0,"Unprepared departure does not detach or thrust: "+failure);
        }
        {
            var s=Setup();s.Console.Items.Add(new(){strID="n2-exact",Kind=NavigationService.PursuitId,ship=s.Own});
            var destination=new Ship {strRegID="pursuit-destination"};destination.objSS.vPosy=5000*AutoNavCore.M_TO_AU;
            CrewSim.system.Ships[destination.strRegID]=destination;GUIOrbitDraw.CrossHairTarget=new(){Ship=destination};
            s.Service.DepartureAction(s.Console,"depart-mode");s.Service.DepartureAction(s.Console,"depart-mode");
            s.Service.DepartureAction(s.Console,"depart-continue");
            new ObjectStateStore(s.Console.mapGUIPropMaps,"AutoNav.Departure",s.Console.strID,1).Read(out var record);
            check(record["flight.module"]=="n2-exact"&&record["operation"]=="follow","Mixed N1/N2 console captures the N2 required for Follow continuation");
            s.Service.WorldChanging();
        }
        foreach(bool detached in new[]{false,true})
        {
            var s=Setup();
            CrewSim.DuringDetach=()=>{if(detached) {s.Own.Attached=false;s.Own.Attachments.Clear();}throw new InvalidOperationException("native interruption");};
            s.Service.DepartureAction(s.Console,"depart");
            check(CrewSim.DetachCalls==1&&Phase(s.Console)=="DetachPending","Write-ahead detachment survives native exception");
            CrewSim.DuringDetach=null;s.Service.WorldChanging();s.Service.DepartureAction(s.Console,"depart-resume");
            check(CrewSim.DetachCalls==1,"Resume never blindly repeats native detachment");
            check(Phase(s.Console)==(detached?"Departing":"DetachPending"),"Recovery uses physical attachment evidence");
        }
        {
            var s=Setup();var destination=new Ship { strRegID="saved-destination" };destination.objSS.vPosx=5000*AutoNavCore.M_TO_AU;
            CrewSim.system.Ships[destination.strRegID]=destination;GUIOrbitDraw.CrossHairTarget=new() {Ship=destination};
            s.Service.DepartureAction(s.Console,"depart-continue");
            GUIOrbitDraw.CrossHairTarget=new() {Ship=s.Peer};
            new ObjectStateStore(s.Console.mapGUIPropMaps,"AutoNav.Departure",s.Console.strID,1).Read(out var record);
            check(record["target"]==destination.strRegID&&record["operation"]=="fly","Continuation captures mode and exact destination before disconnect");
            s.Service.WorldChanging();check(!s.Service.IndustrialOwns(s.Own.objSS),"Reload does not restore live departure authority");
            s.Service.DepartureAction(s.Console,"depart-resume");
            check(Phase(s.Console)=="Departing"&&CrewSim.DetachCalls==1,"Explicit Resume replans only the remaining egress");
        }
        {
            var s=Setup();s.Own.Attached=false;s.Own.Attachments.Clear();s.Peer.Attached=false;
            var crossing=new Ship { strRegID="crossing" };crossing.objSS.vPosx=-400*AutoNavCore.M_TO_AU;crossing.objSS.vPosy=300*AutoNavCore.M_TO_AU;
            crossing.objSS.vVelY=-20*AutoNavCore.M_TO_AU;CrewSim.system.Ships[crossing.strRegID]=crossing;
            check(s.Service.RequestIndustrialMove("window",s.Console,"module","peer",IndustrialMove.Transit,180,1200,0,()=>null,out _),"Industrial transit accepts additive request");
            s.Service.GuardNavigation(.5);NativeContactReader.State=ContactState.Weak;s.Service.GuardNavigation(.5);
            check(!s.Service.IndustrialOwns(s.Own.objSS)&&s.Own.LastX==0&&s.Own.LastY==0,"Contact loss releases owned avoidance thrust");
            NativeContactReader.State=ContactState.Ready;check(!s.Service.ObserveIndustrial("window",out _,out _),"Reacquisition does not restore mission authority");
        }
    }
}
