using System;
using PhobosAutoNav;
using PhobosAutoNav.Core;
int checks=0;
foreach(double cruise in new[]{100d,1500d})
foreach(bool moving in new[]{false,true})
foreach(bool combined in new[]{false,true})
foreach(double distance in new[]{1000d,8000d})
foreach(double dt in new[]{.02,.1,.5,1d})
foreach(double mirror in new[]{-1d,1d})
{
    NavigationService.TestCruise=cruise;AutoNavCore.ResetStatics();StarSystem.fEpoch=0;CrewSim.system=new();CrewSim.AttachCalls=0;
    var own=new Ship{strRegID="own",DeltaVRemainingRCS=10000*AutoNavCore.M_TO_AU};own.Ports.Add("own");own.Comms.Clearance=new();
    var target=new Ship{strRegID="target"};target.Ports.Add("assigned");target.Pairs.Add(("assigned","own"));
    target.objSS.vPosx=mirror*distance*.3*AutoNavCore.M_TO_AU;target.objSS.vPosy=distance*AutoNavCore.M_TO_AU;
    own.objSS.fRot=(float)(mirror*.7);own.objSS.vVelX=mirror*2*AutoNavCore.M_TO_AU;
    if(moving) { target.objSS.vVelX=20000*AutoNavCore.M_TO_AU;own.objSS.vVelX+=target.objSS.vVelX;target.objSS.vVelY=own.objSS.vVelY=-15000*AutoNavCore.M_TO_AU; }
    CrewSim.system.Ships.Add("own",own);CrewSim.system.Ships.Add("target",target);
    CrewSim.coPlayer=new(){strID="player",ship=own};var co=new CondOwner{strID="console",ship=own};own.Items.Add(co);
    co.Items.Add(new(){strID="module",Kind=NavigationService.ModuleId,ship=own});
    GUIDockSys.instance=new(){COSelf=co};GUIOrbitDraw.CrossHairTarget=new(){Ship=target};
    var service=new NavigationService();
    if(combined)service.ApproachDock(co);else service.Dock(co);
    for(double t=0;t<3600 && AutoNavCore.Engaged;t+=dt)
    {
        service.BeforeNavigationPhysics(CrewSim.system,dt);own.objSS.Integrate(dt);target.objSS.Integrate(dt);StarSystem.fEpoch+=dt;
        service.AfterNavigationPhysics(CrewSim.system,dt);
    }
    if(CrewSim.AttachCalls!=1)throw new Exception($"combined={combined} distance={distance} dt={dt} mirror={mirror}: {service.Diagnostic} result={AutoNavCore.LastResult} range=({(target.objSS.vPosx-own.objSS.vPosx)/AutoNavCore.M_TO_AU},{(target.objSS.vPosy-own.objSS.vPosy)/AutoNavCore.M_TO_AU})");
    checks++;
}
Console.WriteLine($"{checks} coupled production-guidance/docking/avoidance scenarios passed against native boundary doubles. Not Unity validation.");

foreach(double mirror in new[]{-1d,1d})
{
    AutoNavCore.ResetStatics(); StarSystem.fEpoch=0; CrewSim.system=new();
    var own=new Ship{strRegID="own",Attached=true,Towed=true};
    var load=new Ship{strRegID="load",Attached=true,Towed=true};
    var target=new Ship{strRegID="target"};target.objSS.vPosy=8000*AutoNavCore.M_TO_AU;
    own.Attachments["own"]=load;load.Attachments["peer"]=own;
    CrewSim.system.Ships["own"]=own;CrewSim.system.Ships["load"]=load;CrewSim.system.Ships["target"]=target;
    CrewSim.coPlayer=new(){strID="player",ship=own};var co=new CondOwner{strID="console",ship=own};own.Items.Add(co);
    co.Items.Add(new(){strID="module",Kind=NavigationService.ModuleId,ship=own});
    var service=new NavigationService();service.BeginAvoidanceFlight(co,SavedFlightMode.Active);
    for(int step=0;step<18000 && AutoNavCore.Engaged;step++)
    {
        load.objSS.vPosx=own.objSS.vPosx+mirror*250*AutoNavCore.M_TO_AU;
        load.objSS.vPosy=own.objSS.vPosy;load.objSS.vVelX=own.objSS.vVelX;load.objSS.vVelY=own.objSS.vVelY;
        service.BeforeNavigationPhysics(CrewSim.system,.1);
        if(service.avoidanceActive)throw new Exception("Attached load incorrectly became a collision obstacle");
        own.objSS.Integrate(.1);target.objSS.Integrate(.1);StarSystem.fEpoch+=.1;service.AfterNavigationPhysics(CrewSim.system,.1);
    }
    if(AutoNavCore.LastResult!="ARRIVED")throw new Exception("Secured tow failed approach: "+AutoNavCore.LastResult);
}
Console.WriteLine("2 mirrored secured-tow production update sequences passed; native group physics remains owner-tested.");
