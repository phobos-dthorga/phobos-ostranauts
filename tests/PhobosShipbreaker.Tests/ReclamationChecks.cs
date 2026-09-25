using System;
using System.Linq;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Persistence;
internal static class ReclamationChecks
{
    internal static void Run(Action<bool,string> check,Action<Action,string> throws)
    {
        var r=new ReclamationRecord();
        foreach(var key in new[]{"owner","ship","target","console","module","g4","chute","processor","permission"}) r[key]=key+"-exact-id";
        r.Phase=ReclamationPhase.Seeking;r.Number("completed",0);check(r.Valid,"Complete mission binding is required");
        foreach(var key in new[]{"ownPort","targetPort","support","floor"}) r[key]=key+"-id";
        r.Begin("wall-identity");double duration=r.Number("seconds"),kw=r.Number("kw");
        r.Credit(kw*30/3600*.4);check(Math.Abs(r.Number("progress")-12)<1e-8,"Partial delivered power earns proportional work");
        check(ReclamationRecord.Read(r.Fields,out var restored)&&Math.Abs(restored.Number("progress")-12)<1e-8,"Reload preserves paid work without runtime authorization");
        foreach(var phase in new[]{ReclamationPhase.UninstallPending,ReclamationPhase.TransferPending})
        { restored.Phase=phase;throws(()=>restored.Credit(.1),"Mutation journal phases cannot earn or repeat work"); }
        r.Credit(kw*(duration-12)/3600);check(r.Paid,"Paid wall becomes eligible for native uninstall");
        r.Phase=ReclamationPhase.TransferPending;r.CompleteTransfer();
        check(r.Number("completed")==1&&r.Phase==ReclamationPhase.Feeding,"One journal settlement records one real transfer");
        throws(()=>r.CompleteTransfer(),"Repeat settlement cannot repeat completion count");
        check(!ReclamationRules.ValidWork(double.NaN,120,12)&&!ReclamationRules.ValidWork(121,120,12),"Corrupt or overcredited work is protected");
        for(int failure=0;failure<9;failure++)
        {
            bool[] states={true,false,true,false,true,true,false};
            double mass=24;
            if(failure==0) mass=23;
            else if(failure<=7) states[failure-1]=!states[failure-1];
            bool valid=ReclamationRules.CanCut(mass,states[0],states[1],states[2],states[3],states[4],states[5],states[6]);
            check(valid==(failure==8),"Mass, installation, damage, contents, stacks, exposure, floors and anchors gate cutting");
        }
        foreach(int angle in new[]{0,90,180,270})
        foreach(double x in new[]{-1.5,-.5,.5,1.5})
        {
            var delta=IntakeRules.Rotate(x,CaptureRules.WallCentreY,angle);
            check(CaptureRules.InContact(12,20,angle,12+delta.X,20+delta.Y),"All G4 orientations retain exact deck reach");
            check(!CaptureRules.InContact(12,20,angle,12+delta.X*4,20+delta.Y*4),"Relocated native replacement cannot be pulled from outside cutter reach");
        }
        check(!ReclaimerRules.CoolingBudget(1000,295,0,0,12,1,out _),"Vacuum supplies no free cutter cooling");
        check(!ReclaimerRules.CoolingBudget(1000,313.14,0,10,12,1,out _),"Near-ceiling room pauses cutter energy before debit");
        check(ReclaimerRules.CoolingBudget(100000,295,0,10,12,1,out var rise)&&rise>0,"Admitted cutter electricity adds positive native gas heat");
    }
}
