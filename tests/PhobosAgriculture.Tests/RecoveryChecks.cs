using System;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;
internal static class RecoveryChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var store=new Phobos.Ostranauts.Framework.Persistence.ObjectStateStore(new(),"Treatment","test",1);
        check(store.TryWrite(RecoveryWork.Save("","",0)),"Idle treatment fields use native-safe empty markers");
        check(store.Read(out var empty)==Phobos.Ostranauts.Framework.Persistence.SavedStateStatus.Ready&&RecoveryWork.Read(empty).Input=="","Idle treatment reload remains idle");
        check(store.TryWrite(RecoveryWork.Save("full-input-id","full-filter-id",.03)),"Bound treatment saves exact IDs and measured work");
        store.Read(out var saved);var job=RecoveryWork.Read(saved);check(job.Input=="full-input-id"&&job.Filter=="full-filter-id"&&job.Energy==.03,"Treatment reload preserves exact job meaning");
        foreach(var input in new[]{new LiquidMixture(5,.04),new LiquidMixture(0,.5),new LiquidMixture(20,0)})
        {
            var q=DrainageRecovery.Read(DrainageRecovery.Save(input),input.TotalKg);var recovered=DrainageRecovery.Recover(q);
            check(Math.Abs(recovered.TotalKg+DrainageRecovery.RejectKg(q)-q.TotalKg-DrainageRecovery.CartridgeKg)<1e-9,"Treatment retains all input and cartridge mass");
            check(recovered.CarrierKg<=q.CarrierKg&&recovered.SoluteKg<=q.SoluteKg,"Neither recovered component exceeds measured stock");
            var second=DrainageRecovery.Recover(recovered);check(second.TotalKg<recovered.TotalKg,"Repeated eligible use cannot increase recovered matter");
        }
        bool rejected=false;try{DrainageRecovery.Read(new System.Collections.Generic.Dictionary<string,string>(),1);}catch{rejected=true;}check(rejected,"Historic unrecorded drainage is never assigned an assay");
        rejected=false;try{DrainageRecovery.Read(DrainageRecovery.Save(new(1,.01)),5);}catch{rejected=true;}check(rejected,"Physical mass mismatch rejects treatment");
    }
}
