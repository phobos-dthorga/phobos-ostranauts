using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Trading;

internal static class BulkStorageChecks
{
    internal static void Run(Action<bool,string> check)
    {
        void Reject(Action action,string label){bool failed=false;try{action();}catch{failed=true;}check(failed,label);}
        var water=new StoredCommodity("water",120);water.SetService(120);water.SetReserve(10);
        check(water.AvailableKg==110,"Reserve protects measured source quantity");
        water.Isolate();water.Isolate();check(water.CatchKg==120&&water.ServiceKg==0&&water.TotalKg==120,"Repeated damage conserves isolated mass");
        Reject(()=>water.SetService(.1),"Containment shares total capacity");
        var copy=StoredCommodity.Read(water.Save(),"water",120);copy.Recover();copy.Recover();
        check(copy.TotalKg==120&&copy.ServiceKg==120&&copy.CatchKg==0&&copy.ReserveKg==10,"Save/load/repeated recovery preserves contents and reserve");
        Reject(()=>StoredCommodity.Read(water.Save(),"acid",120),"No unapproved commodity conversion");
        Reject(()=>copy.SetService(double.NaN),"Invalid physical quantity rejected");
        Reject(()=>StoredCommodity.Read(water.Save(),"water",100),"Shrinking capacity never discards saved contents");
        check(CommodityReservations.TryAcquire("first","tank","wallet"),"Exact endpoints reserved");
        check(!CommodityReservations.TryAcquire("other","free","tank")&&!CommodityReservations.Held("free"),"Competing reservation is all-or-nothing");
        CommodityReservations.Release("other");check(CommodityReservations.Held("tank"),"Cancellation releases only its own claims");CommodityReservations.Release("first");

        BulkPurchaseQuote Quote()=>new("crew","station","ship","tank","water","revision",10,2);
        ObjectStateStore Store()=>new(new(),"Purchase","test",1);
        var target=new Target();var journal=Store();var quote=Quote();
        var result=BulkSettlement.Buy(quote,target,journal);
        check(result.Success&&result.Delivered==10&&result.Paid==20&&target.Balance==80&&target.Cargo==10&&target.Ledger==20,"Measured purchase charges once and creates paid receipt");
        check(!BulkSettlement.Buy(quote,target,journal).Success&&target.Balance==80&&target.Cargo==10,"Replayed quote cannot debit or deliver again");
        target=new Target{Limit=4};result=BulkSettlement.Buy(Quote(),target,Store());
        check(result.Success&&result.Delivered==4&&result.Paid==8&&target.Balance==92&&target.Ledger==8,"Known partial delivery refunds only missing quantity");
        target=new Target{Limit=0};result=BulkSettlement.Buy(Quote(),target,Store());
        check(result.Success&&target.Balance==100&&target.Ledger==0,"Zero measured receipt gives full refund without an unpaid ledger entry");
        target=new Target{Balance=1};result=BulkSettlement.Buy(Quote(),target,Store());check(!result.Success&&target.Cargo==0&&target.Balance==1,"Insufficient funds change neither cargo nor wallet");
        target=new Target{Valid=false};result=BulkSettlement.Buy(Quote(),target,Store());check(!result.Success&&target.Balance==100&&target.Cargo==0,"Stale capacity/authority/quote rejected before spending");
        foreach(string fault in new[]{"debit-before","debit-after","delivery-before","delivery-after","refund-before","refund-after","ledger-before","ledger-after","unknown-receipt"})
        {
            target=new Target{Fault=fault,Limit=4};journal=Store();quote=Quote();result=BulkSettlement.Buy(quote,target,journal);
            check(result.Protected&&BulkSettlement.Protected(journal),"Interrupted settlement retains evidence: "+fault);
            double cash=target.Balance,cargo=target.Cargo,ledger=target.Ledger;
            check(!BulkSettlement.Buy(Quote(),target,journal).Success&&target.Balance==cash&&target.Cargo==cargo&&target.Ledger==ledger,"Reload/retry cannot invent a second settlement: "+fault);
            check(!CommodityReservations.Held("tank")&&!CommodityReservations.Held("crew"),"Failed operation releases only transient capacity reservations: "+fault);
        }
        var maps=new Dictionary<string,Dictionary<string,string>>();new ObjectStateStore(maps,"Purchase","future",2).TryWrite(new Dictionary<string,string>{{"state","pending"}});
        journal=new(maps,"Purchase","test",1);target=new();result=BulkSettlement.Buy(Quote(),target,journal);
        check(result.Protected&&target.Balance==100&&target.Cargo==0,"Unknown/newer purchase journals remain untouched");
        CommodityReservations.TryAcquire("competing","tank");target=new();result=BulkSettlement.Buy(Quote(),target,Store());
        check(!result.Success&&target.Balance==100&&target.Cargo==0&&CommodityReservations.Held("tank"),"Purchase does not steal another worker's reservation");CommodityReservations.Release("competing");
        foreach(string phase in new[]{"validated","debit-after","delivery-after","refund-after","ledger-after"})
        {
            var changingMaps=new Dictionary<string,Dictionary<string,string>>();journal=new(changingMaps,"Purchase","test",1);
            int validations=0;
            target=new Target{Limit=4,After=at=>{
                if(at=="validated"&&++validations<2)return;
                if(at!=phase)return;
                changingMaps["PhobosState.Purchase"]=new Dictionary<string,string>{{"schema","9"},{"owner","future"}};
            }};
            result=BulkSettlement.Buy(Quote(),target,journal);
            check(result.Protected&&BulkSettlement.Protected(journal),"Journal write refusal stops at exact phase: "+phase);
            double cash=target.Balance,cargo=target.Cargo;
            check(!BulkSettlement.Buy(Quote(),target,journal).Success&&target.Balance==cash&&target.Cargo==cargo,"Uncertain journal writes cannot repeat payment/delivery: "+phase);
        }
    }
    private sealed class Target:IBulkSettlementTarget
    {
        public double Balance{get;set;}=100;
        public double Cargo,Ledger,Limit=10;
        public bool Valid=true;
        public string Fault="";
        public Action<string>? After;
        void Fail(string phase){After?.Invoke(phase);if(Fault==phase)throw new InvalidOperationException(phase);}
        public bool Validate(BulkPurchaseQuote q,out string reason){reason="stale";After?.Invoke("validated");return Valid;}
        public void Debit(double amount){Fail("debit-before");Balance-=amount;Fail("debit-after");}
        public void Refund(double amount){Fail("refund-before");Balance+=amount;Fail("refund-after");}
        public double Deliver(BulkPurchaseQuote q){Fail("delivery-before");double amount=Math.Min(Limit,q.Quantity);Cargo+=amount;Fail("delivery-after");return Fault=="unknown-receipt"?double.NaN:amount;}
        public void RecordPaid(BulkPurchaseQuote q,double amount,double paid){Fail("ledger-before");Ledger+=paid;Fail("ledger-after");}
    }
}
