using System;
using System.Collections.Generic;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;
using PhobosAutoNav;
using Phobos.Ostranauts.Framework.Persistence;
int checks=0;
void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
CondOwner Setup(int walls=1)
{
    ReclamationService.Reset();Plugin.Service=new();CrewSim.system=new();StarSystem.fEpoch=100;
    IndustrialNavigation.Permission=null;IndustrialNavigation.Visible=IndustrialNavigation.Ready=true;IndustrialNavigation.Moves.Clear();
    World.InterruptUninstall=World.Relocate=false;World.CaptureFit=true;World.UninstallCalls=World.ReleaseCalls=World.CaptureCalls=0;
    var own=new Ship {strRegID="ship"};var target=new Ship {strRegID="target"};own.objSS.vPosx=2000;
    CrewSim.system.Ships[own.strRegID]=own;CrewSim.system.Ships[target.strRegID]=target;
    var g=new CondOwner {strID="g4",ship=own};World.Grabber=g;
    var r=new CaptureRecord();foreach(var key in new[]{"owner","ship","target","console","module","g4","chute","processor","permission","mount","wall","ownPort","targetPort","floor"})r[key]=key;
    r["support"]="retained-support";r.Phase=CapturePhase.Captured;World.Capture=r;
    for(int i=0;i<walls;i++)target.Items.Add(new CondOwner {strID="wall"+i,strCODef="ItmWall1x1",ship=target,Reach=i==0});
    return g;
}
ReclamationRecord Read(CondOwner g)
{
    var state=new ObjectStateStore(g.mapGUIPropMaps,ReclamationRecord.StoreName,g.strID,1).Read(out var fields);
    if(state!=SavedStateStatus.Ready||!ReclamationRecord.Read(fields,out var r))throw new Exception("Invalid mission record");return r;
}
void Update(){StarSystem.fEpoch+=2;ReclamationService.Update();}
void Pay(CondOwner g)
{
    ReclamationService.Reset();var r=Read(g);r.Credit(r.Number("kw")*(r.Number("seconds")-r.Number("progress"))/3600);
    Check(new ObjectStateStore(g.mapGUIPropMaps,ReclamationRecord.StoreName,g.strID,1).TryWrite(r.Fields),"Save paid receipt");
    ReclamationService.Command(null,g,"reclaim-resume",out _);
    Check(Read(g).Paid,"Explicit resume binds retained paid work");
}
{
    var g=Setup(2);Check(ReclamationService.Command(null,g,"reclaim-start",out _),"Start authorizes only the bound intake");
    Check(ReclamationService.PreparePower(g),"Supported captured wall requests exclusive cutter power");
    Pay(g);Update();Update();
    Check(World.UninstallCalls==1&&g.objContainer.ContainedCOs.Count==1&&Read(g).Number("completed")==1,"Native replacement is transferred once and completion journal settles");
    Update();Check(World.UninstallCalls==1,"Capacity wait never starts another uninstall");
    var cargo=g.objContainer.ContainedCOs[0];g.objContainer.ContainedCOs.Clear();cargo.objCOParent=new CondOwner();
    Update();Check(World.ReleaseCalls==1&&IndustrialNavigation.Moves.Contains(IndustrialMove.Egress),"Exhausted window releases once and retreats");
    Update();Check(IndustrialNavigation.Moves.Contains(IndustrialMove.Transit),"Retreat precedes automatic traversal");
    Update();Update();Check(World.CaptureCalls==1&&Read(g).Phase==ReclamationPhase.Cutting,"Traversal establishes native recapture before the next cut");
    ReclamationService.ManualTakeover(g.ship);Update();Check(!Plugin.Service.Armed&&IndustrialNavigation.Permission==null&&!ReclamationService.PreparePower(g),"Manual movement cancels automatic mission authority");
}
{
    var g=Setup();g.objContainer.Full=true;Check(ReclamationService.Command(null,g,"reclaim-start",out _),"Full capacity waits under current mission authority");
    Check(!ReclamationService.PreparePower(g)&&World.UninstallCalls==0,"No cut before space reservation");g.objContainer.Full=false;Update();
    Check(ReclamationService.PreparePower(g),"Capacity recovery automatically continues the authorized mission");
    ReclamationService.Command(null,g,"reclaim-stop",out _);Update();Check(!ReclamationService.PreparePower(g)&&g.ship.Attached,"Stop keeps capture and cancels future work");
}
{
    var g=Setup();ReclamationService.Command(null,g,"reclaim-start",out _);Pay(g);Update();Update();
    var cargo=g.objContainer.ContainedCOs[0];g.objContainer.ContainedCOs.Clear();cargo.objCOParent=new CondOwner();
    Plugin.Service.PendingFeed=true;Update();Check(Plugin.Service.Armed&&Read(g).Phase==ReclamationPhase.Seeking,"Last D4 input finishes before terminal mission status");
    Plugin.Service.PendingFeed=false;Update();Check(!Plugin.Service.Armed&&Read(g).Phase==ReclamationPhase.Exhausted,"Drained D4 completes supported-wall mission without discarding a paid job");
}
foreach(bool relocate in new[]{false,true})
{
    var g=Setup();ReclamationService.Command(null,g,"reclaim-start",out _);World.Relocate=relocate;World.InterruptUninstall=!relocate;Pay(g);Update();
    Check(World.UninstallCalls==1,"Paid native uninstall attempted once");ReclamationService.Reset();World.InterruptUninstall=false;
    ReclamationService.Command(null,g,"reclaim-resume",out _);for(int i=0;i<20;i++)Update();
    Check(World.UninstallCalls==1&&g.objContainer.ContainedCOs.Count==0,"Interrupted or relocated native object remains retained, with no replay or remote pull");
}
{
    var g=Setup();ReclamationService.Command(null,g,"reclaim-start",out _);g.objContainer.FailAfterPlacement=true;Pay(g);Update();
    Check(g.objContainer.ContainedCOs.Count==1&&Read(g).Phase==ReclamationPhase.TransferPending,"Post-placement failure retains one object and pending transfer evidence");
    ReclamationService.Reset();g.objContainer.FailAfterPlacement=false;ReclamationService.Command(null,g,"reclaim-resume",out _);
    Check(g.objContainer.ContainedCOs.Count==1&&Read(g).Number("completed")==1&&World.UninstallCalls==1,"Reload settles exact physical ownership without repeating output");
}
{
    var g=Setup();ReclamationService.Command(null,g,"reclaim-start",out _);IndustrialNavigation.Visible=false;Update();
    Check(!Plugin.Service.Armed&&!ReclamationService.PreparePower(g),"Tracking loss suspends acquisition");
    IndustrialNavigation.Visible=true;Update();Check(!Plugin.Service.Armed,"Reacquisition requires explicit Resume");
}
Console.WriteLine($"{checks} reclamation service/physical-transfer boundary assertions passed. No in-game tests.");
