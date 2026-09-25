using System;
using System.Collections.Generic;
using System.Globalization;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Lumped line contents retained by an endpoint custodian. No implicit cargo disposal.</summary>
public sealed class FluidLine
{
    public string Route="", Profile="";
    public LiquidMixture Quantity;
    public double CapacityKg, TransitSeconds, RemainingSeconds;
    public double TotalKg=>Quantity.TotalKg;
    public bool Empty=>TotalKg<=MixtureTransfer.ToleranceKg;
    public bool Ready=>RemainingSeconds<=0;
    public void Configure(string route,string profile,double capacity,double transit)
    {
        if(route=="-"||profile=="-")throw new ArgumentException("Reserved empty line marker.");
        if(!Empty && (Route!=route || Profile!=profile || CapacityKg!=capacity || TransitSeconds!=transit)) throw new InvalidOperationException("Drain retained line contents before changing its route.");
        Route=route; Profile=profile; CapacityKg=capacity; TransitSeconds=transit; Validate();
    }
    public void SetQuantity(LiquidMixture value)
    {
        bool wasEmpty=Empty; Quantity=value;
        if(wasEmpty && !Empty) RemainingSeconds=TransitSeconds;
        if(Empty) RemainingSeconds=0;
        Validate();
    }
    public void Advance(double seconds)
    {
        if(!Valid(seconds) || seconds>3600) throw new ArgumentOutOfRangeException(nameof(seconds));
        RemainingSeconds=Math.Max(0,RemainingSeconds-seconds);
    }
    private static bool Valid(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v)&&v>=0;
    public void Validate()
    {
        if(!Valid(Quantity.CarrierKg)||!Valid(Quantity.SoluteKg)||!Valid(CapacityKg)||!Valid(TransitSeconds)||!Valid(RemainingSeconds)||TotalKg>CapacityKg+1e-9||RemainingSeconds>TransitSeconds||CapacityKg>1000||TransitSeconds>3600||(!Empty&&(Route.Length==0||Profile.Length==0))) throw new ArgumentException("Invalid retained fluid line.");
    }
    public Dictionary<string,string> Save()
    {
        Validate(); string N(double v)=>v.ToString("R",CultureInfo.InvariantCulture);
        return new(){["route"]=Route.Length==0?"-":Route,["profile"]=Profile.Length==0?"-":Profile,["water"]=N(Quantity.CarrierKg),["solute"]=N(Quantity.SoluteKg),["capacity"]=N(CapacityKg),["transit"]=N(TransitSeconds),["remaining"]=N(RemainingSeconds)};
    }
    public static FluidLine Read(IReadOnlyDictionary<string,string> d)
    {
        if(d.Count!=7) throw new ArgumentException("Unknown line record.");
        double N(string k)=>double.Parse(d[k],CultureInfo.InvariantCulture);
        var r=new FluidLine{Route=d["route"]=="-"?"":d["route"],Profile=d["profile"]=="-"?"":d["profile"],Quantity=new(N("water"),N("solute")),CapacityKg=N("capacity"),TransitSeconds=N("transit"),RemainingSeconds=N("remaining")}; r.Validate(); return r;
    }
    public FluidLine Copy()=>Read(Save());
}
