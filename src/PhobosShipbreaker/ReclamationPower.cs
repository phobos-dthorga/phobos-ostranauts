using System;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;
namespace PhobosShipbreaker;
internal static partial class ReclamationService
{
    internal sealed class PowerTransfer
    {
        internal GasContainer Gas=null!;internal double Mols;internal EnergyReceipt Receipt=null!;
        internal string Grabber="";internal bool Finished;
    }
    internal static bool BeginPower(Powered power,CondOwner g,ref double amount,out PowerTransfer? transfer,out bool owned)
    {
        transfer=null;owned=sessions.TryGetValue(g.strID,out var s)&&s.Authorized&&s.Demand;
        if(!owned) return true;
        var r=s!.Record;var processor=CollectorService.Resolve(r["processor"]);
        var room=processor?.ship.GetRoomAtWorldCoords1(processor.GetPos("use"),false)?.CO;var gas=room?.GasContainer;
        amount=Math.Min(amount*r.Number("kw")/IntakeRules.WorkingKW,(r.Number("seconds")-r.Number("progress"))*r.Number("kw")/3600);
        double mols=0;
        if(gas==null||!gas.mapGasMols1.TryGetValue("StatGasMolTotal",out mols)||
            !ReclaimerRules.CoolingBudget(mols,room!.GetCondAmount("StatGasTemp"),gas.fDGasTemp,room.GetCondAmount("StatGasPressure"),r.Number("kw"),amount*3600/r.Number("kw"),out _))
        { s.Notice=Text.Get("Reclamation.cooling");g.ZeroCondAmount(IntakeRules.Working);return false; }
        transfer=new PowerTransfer { Grabber=g.strID,Gas=gas,Mols=mols,Receipt=NativeEnergyReceipts.Begin(power,g,amount) };return true;
    }
    internal static void FinishPower(Powered power,CondOwner g,PowerTransfer? transfer)
    {
        if(transfer==null||transfer.Finished) return;
        transfer.Finished=true;
        double supplied=NativeEnergyReceipts.Complete(power,g,transfer.Receipt);
        if(!ReclamationRules.Finite(supplied)||supplied<0) throw new InvalidOperationException("Invalid cutter energy receipt.");
        transfer.Gas.fDGasTemp+=supplied*3600*ReclaimerRules.JoulesPerKilojoule/(transfer.Mols*ReclaimerRules.GasHeatCapacity);
        if(!sessions.TryGetValue(transfer.Grabber,out var s)) throw new InvalidOperationException("Cutter session lost during receipt.");
        s.Record.Credit(supplied);s.Notice=Text.Get(supplied>0?"Reclamation.cutting":"Reclamation.power");
        if(!Save(s)) Suspend(s,Text.Get("Capture.save"));
    }
}
