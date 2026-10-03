using System;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;
namespace PhobosShipbreaker;
internal static partial class ReclamationService
{
    internal sealed class PowerTransfer
    {
        internal RoomHeat.Air Air=null!;internal EnergyReceipt Receipt=null!;
        internal string Grabber="";internal bool Finished;
    }
    internal static bool BeginPower(Powered power,CondOwner g,ref double amount,out PowerTransfer? transfer,out bool owned)
    {
        transfer=null;owned=sessions.TryGetValue(g.strID,out var s)&&s.Authorized&&s.Demand;
        if(!owned) return true;
        var r=s!.Record;var processor=CollectorService.Resolve(r["processor"]);
        amount=Math.Min(amount*r.Number("kw")/IntakeRules.WorkingKW,(r.Number("seconds")-r.Number("progress"))*r.Number("kw")/3600);
        // The cutter's heat goes into the D4 service room's air.
        var air=processor==null?null:RoomHeat.Read(processor,"use");
        var heat=processor==null?default:RoomHeat.Check(air,r.Number("kw"),amount*3600/r.Number("kw"));
        if(processor==null||!heat.Admitted)
        { s.Notice=processor==null?Text.Get("Reclamation.heat_no_processor"):RoomHeat.Describe(heat);g.ZeroCondAmount(IntakeRules.Working);return false; }
        transfer=new PowerTransfer { Grabber=g.strID,Air=air!,Receipt=NativeEnergyReceipts.Begin(power,g,amount) };return true;
    }
    internal static void FinishPower(Powered power,CondOwner g,PowerTransfer? transfer)
    {
        if(transfer==null||transfer.Finished) return;
        transfer.Finished=true;
        double supplied=NativeEnergyReceipts.Complete(power,g,transfer.Receipt);
        if(!ReclamationRules.Finite(supplied)||supplied<0) throw new InvalidOperationException("Invalid cutter energy receipt.");
        RoomHeat.Deposit(transfer.Air,supplied);
        if(!sessions.TryGetValue(transfer.Grabber,out var s)) throw new InvalidOperationException("Cutter session lost during receipt.");
        s.Record.Credit(supplied);s.Notice=Text.Get(supplied>0?"Reclamation.cutting":"Reclamation.power");
        if(!Save(s)) Suspend(s,Text.Get("Capture.save"));
    }
}
