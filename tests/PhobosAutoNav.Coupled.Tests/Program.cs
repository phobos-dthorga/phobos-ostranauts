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
