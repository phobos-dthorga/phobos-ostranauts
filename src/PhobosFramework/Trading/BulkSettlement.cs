using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Liquids;

namespace Phobos.Ostranauts.Framework.Trading;

public sealed class BulkPurchaseQuote
{
    public readonly string Operation=Guid.NewGuid().ToString("N"),Actor,Station,Ship,Destination,Offer,Revision;
    public readonly double Quantity,UnitPrice;
    public double Total=>Quantity*UnitPrice;
    public BulkPurchaseQuote(string actor,string station,string ship,string destination,string offer,string revision,double quantity,double unitPrice)
    {Actor=actor;Station=station;Ship=ship;Destination=destination;Offer=offer;Revision=revision;Quantity=quantity;UnitPrice=unitPrice;if(string.IsNullOrWhiteSpace(actor)||string.IsNullOrWhiteSpace(station)||string.IsNullOrWhiteSpace(ship)||string.IsNullOrWhiteSpace(destination)||string.IsNullOrWhiteSpace(offer)||string.IsNullOrWhiteSpace(revision)||!Finite(quantity)||quantity<=0||!Finite(unitPrice)||unitPrice<=0||!Finite(Total)||Total<=0)throw new ArgumentException("Invalid quote");}
    internal static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
}
public interface IBulkSettlementTarget
{
    double Balance { get; }
    bool Validate(BulkPurchaseQuote quote,out string reason);
    void Debit(double amount);
    void Refund(double amount);
    // Return measured delivered units. Throw for unknown custody; never infer from a wallet delta.
    double Deliver(BulkPurchaseQuote quote);
    void RecordPaid(BulkPurchaseQuote quote,double delivered,double paid);
}
public readonly struct BulkPurchaseReceipt
{
    public readonly bool Success,Protected;
    public readonly double Delivered,Paid;
    public readonly string Reason;
    public BulkPurchaseReceipt(bool success,bool protect,double delivered,double paid,string reason){Success=success;Protected=protect;Delivered=delivered;Paid=paid;Reason=reason;}
}
/// <summary>Independent measured purchase. Persistent uncertainty blocks retry; no crash-atomic promise.</summary>
public static class BulkSettlement
{
    public static bool Protected(ObjectStateStore journal)
    {
        var state=journal.Read(out var d);
        return state!=SavedStateStatus.Missing&&(state!=SavedStateStatus.Ready||!d.TryGetValue("state",out var s)||s!="complete");
    }
    public static BulkPurchaseReceipt Buy(BulkPurchaseQuote q,IBulkSettlementTarget target,ObjectStateStore journal)
    {
        if(Protected(journal))return new(false,true,0,0,"protected");
        journal.Read(out var previous);
        if(previous.TryGetValue("operation",out var prior)&&prior==q.Operation)return new(false,false,0,0,"already_settled");
        if(!target.Validate(q,out var reason))return new(false,false,0,0,reason);
        double balance=target.Balance;
        if(!BulkPurchaseQuote.Finite(balance)||balance<q.Total)return new(false,false,0,0,"funds");
        if(!CommodityReservations.TryAcquire(q.Operation,q.Actor,q.Destination))return new(false,false,0,0,"reserved");
        var evidence=new Dictionary<string,string>{["state"]="pending",["operation"]=q.Operation,["actor"]=q.Actor,["station"]=q.Station,["ship"]=q.Ship,["destination"]=q.Destination,["offer"]=q.Offer,["revision"]=q.Revision,["quantity"]=N(q.Quantity),["price"]=N(q.UnitPrice),["balance"]=N(balance)};
        double delivered=0,paid=0;
        try
        {
            if(!target.Validate(q,out reason))return new(false,false,0,0,reason);
            Write("pending");target.Debit(q.Total);paid=q.Total;Write("debited");
            delivered=target.Deliver(q);
            if(!BulkPurchaseQuote.Finite(delivered)||delivered<0||delivered>q.Quantity+1e-8)throw new InvalidOperationException("Unknown purchase receipt");
            evidence["delivered"]=N(delivered);Write("delivered");
            double refund=(q.Quantity-delivered)*q.UnitPrice;
            if(refund>0)target.Refund(refund);
            paid=delivered*q.UnitPrice;evidence["paid"]=N(paid);Write("refunded");
            if(paid>0)target.RecordPaid(q,delivered,paid);
            Write("complete");return new(true,false,delivered,paid,delivered<q.Quantity?"partial":"done");
        }
        catch{return new(false,true,delivered,paid,"protected");}
        finally{CommodityReservations.Release(q.Operation);}
        void Write(string state){evidence["state"]=state;if(!journal.TryWrite(evidence))throw new InvalidOperationException("Protected purchase journal");}
    }
    private static string N(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
}
