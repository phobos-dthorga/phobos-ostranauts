using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>One concrete opt-in sink for the inspected ShipsWater 0.16.1 recycler. No nutrient assay.</summary>
public interface IRecyclerRejectSink
{
    bool Handles(CondOwner recycler);
    RecyclerRejectReservation? Reserve(CondOwner recycler);
}
public sealed class RecyclerRejectReservation
{
    public double CapacityKg { get; }
    public Action<double,bool> Settle { get; }
    public RecyclerRejectReservation(double capacityKg, Action<double,bool> settle)
    { CapacityKg=capacityKg;Settle=settle??throw new ArgumentNullException(nameof(settle)); }
}
public static class RecyclerRejectBudget
{
    // Authored 1 kg/L water-remainder convention, not dry solute or a wastewater assay.
    public static double Seconds(double requested,double rate,double recovery,double capacity)
    {
        if(new[]{requested,rate,recovery,capacity}.Any(v=>!FiniteLiquidTransfer.Finite(v)||v<0)||recovery>1)throw new ArgumentException("Invalid recycler budget.");
        return recovery==1 || rate==0 ? requested : Math.Min(requested,capacity/(rate*(1-recovery)));
    }
    public static double Rejected(double debit,double credit,double capacity)
    {
        if(new[]{debit,credit,capacity}.Any(v=>!FiniteLiquidTransfer.Finite(v)||v<0) || credit>debit+1e-6 || debit-credit>capacity+1e-6)throw new ArgumentException("Recycler receipt exceeds reservation.");
        return Math.Max(0,debit-credit);
    }
}
public static class ShipsWaterRejects
{
    private static IRecyclerRejectSink? sink;
    private sealed class Capture
    {
        internal RecyclerRejectReservation Reservation=null!;
        internal List<CondOwner> Waste=null!,Potable=null!;
        internal double BeforeWaste,BeforePotable;
    }
    public static bool Install(Harmony harmony,IRecyclerRejectSink receiver)
    {
        var provider=Chainloader.PluginInfos.Values.FirstOrDefault(p=>p.Instance!=null && p.Instance.GetType().FullName=="ShipsWater.Plugin" && p.Metadata.Version==new Version(0,16,1));
        var type=provider?.Instance.GetType().Assembly.GetType("ShipsWater.Recycler");
        var method=type==null?null:AccessTools.Method(type,"ProcessOne",new[]{typeof(CondOwner),typeof(double),typeof(float),typeof(float),typeof(float),typeof(List<CondOwner>),typeof(List<CondOwner>),typeof(CondTrigger)});
        if(method==null || sink!=null)return false;
        harmony.Patch(method,prefix:new HarmonyMethod(typeof(ShipsWaterRejects),nameof(Before)),finalizer:new HarmonyMethod(typeof(ShipsWaterRejects),nameof(After)));
        sink=receiver;return true;
    }
    public static void Forget(IRecyclerRejectSink receiver) {if(ReferenceEquals(sink,receiver))sink=null;}
    private static bool Before(CondOwner __0,ref float __2,float __3,float __4,ref List<CondOwner> __5,ref List<CondOwner> __6,out Capture? __state)
    {
        __state=null;
        if(sink==null || !sink.Handles(__0))return true;
        RecyclerRejectReservation? reservation=null;
        __0.ZeroCondAmount("IsWaterRecyclerRunning");
        try
        {
            if(__0.ship==null || (int)__0.ship.LoadState<2)return false;
            reservation=sink.Reserve(__0);if(reservation==null)return false;
            // Clone lists: never alter the provider's list shared with its other recyclers.
            bool Local(CondOwner co)=>co!=null && !co.bDestroyed && co.ship==__0.ship && co.objCOParent==null && co.HasCond("IsInstalled") && !co.HasCond("IsDamaged") && !co.HasCond("IsLocked");
            __5=__5.Where(Local).ToList();__6=__6.Where(Local).ToList();
            __2=(float)(RecyclerRejectBudget.Seconds(__2,__3,__4,reservation.CapacityKg)*.999999); // reserve float-rounding headroom
            __state=new Capture{Reservation=reservation,Waste=__5,Potable=__6,BeforeWaste=Total(__5,"StatLiqH2OWaste"),BeforePotable=Total(__6,"StatLiqH2O")};
            return true;
        }
        catch { reservation?.Settle(0,true);return false; }
    }
    private static double Total(IEnumerable<CondOwner> tanks,string stat)=>tanks.Sum(t=>t.GetCondAmount(stat));
    private static void After(Exception? __exception,Capture? __state)
    {
        if(__state==null)return;
        var s=__state;
        try
        {
            double reject=RecyclerRejectBudget.Rejected(s.BeforeWaste-Total(s.Waste,"StatLiqH2OWaste"),Total(s.Potable,"StatLiqH2O")-s.BeforePotable,s.Reservation.CapacityKg);
            s.Reservation.Settle(reject,__exception!=null);
        }
        catch { s.Reservation.Settle(0,true); }
    }
}
