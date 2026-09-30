using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Liquids;

namespace PhobosShipbreaker.Core;
/// <summary>Optional serviced loop. Leaks enter a sealed catch tank, preserving mass and existing thermal energy.
/// Since Shipbreaker 0.58.0 (owner decision, 1 October 2026: lines hold their contents) the charge is the furnace's
/// reservoir: the conduit segments hold their own coolant through Framework's line contents, primed by the pump from
/// what the reservoir holds above its base charge. The loop circulates only with a full circuit and the base charge.</summary>
public sealed class CoolantCharge
{
    public const double CapacityKg=6, BaseChargeKg=5, LeakKgPerSecond=.001, RatedKPa=200;
    /// <summary>How fast the pump primes the conduit from the reservoir's surplus, authored: 0.1 kg a second.</summary>
    public const double PrimeKgPerSecond=.1;
    /// <summary>The authored service fluid: 1,050 kg per cubic metre (a water-glycol heat-transfer fluid's order of
    /// density), in a 20 mm bore a tile, about 0.33 kg a conduit tile.</summary>
    public const string Commodity="coolant";
    public const double DensityKgPerM3=1050, BoreMm=20;
    public bool Enabled;
    public double CleanKg, CapturedKg, PrimeSeconds;
    public double TotalKg=>CleanKg+CapturedKg;
    /// <summary>What the reservoir holds above its base charge, which the pump may send into the conduit.</summary>
    public double SurplusKg=>Math.Max(0,CleanKg-BaseChargeKg);
    public static LineCommodity HeldCoolant()=>LineCommodity.Liquid(Commodity,DensityKgPerM3,BoreMm);
    public bool Filled(bool circuitFull)=>circuitFull&&CleanKg+1e-9>=BaseChargeKg;
    public double PressureKPa(bool circuitFull)=>circuitFull?RatedKPa*Math.Pow(Math.Min(1,CleanKg/BaseChargeKg),2):0;
    public double Flow(int cells,bool circuitFull)=>cells>=1&&cells<=FurnaceCooling.RouteLimit&&Filled(circuitFull)&&PrimeSeconds>=cells*.5?HydraulicRoute.FlowFraction(cells,32):0;
    /// <summary>The kilograms the pump sends into a circuit with <paramref name="roomKg"/> free over <paramref name="seconds"/>
    /// of pumping: never more than the surplus above the base charge.</summary>
    public double PrimeKg(double seconds,double roomKg)=>!double.IsFinite(seconds)||seconds<=0||!double.IsFinite(roomKg)||roomKg<=0?0:Math.Min(roomKg,Math.Min(SurplusKg,seconds*PrimeKgPerSecond));
    public void Advance(double seconds,bool intact,bool powered,int cells,bool circuitFull)
    {
        if(!double.IsFinite(seconds)||seconds<0||seconds>3600) throw new ArgumentOutOfRangeException(nameof(seconds));
        if(!Enabled)return;
        if(!intact) { double leak=Math.Min(CleanKg,seconds*LeakKgPerSecond);CleanKg-=leak;CapturedKg+=leak;PrimeSeconds=0; }
        else if(powered&&Filled(circuitFull)) PrimeSeconds=Math.Min(cells*.5,PrimeSeconds+seconds);
        Validate();
    }
    public void Validate()
    {
        if(!double.IsFinite(CleanKg)||!double.IsFinite(CapturedKg)||!double.IsFinite(PrimeSeconds)||CleanKg<0||CapturedKg<0||PrimeSeconds<0||PrimeSeconds>32||TotalKg>CapacityKg+1e-9||!Enabled&&TotalKg>0)throw new ArgumentException("Invalid coolant charge.");
    }
    public Dictionary<string,string> Save()
    {Validate();string N(double x)=>x.ToString("R",CultureInfo.InvariantCulture);return new(){["enabled"]=Enabled?"1":"0",["clean"]=N(CleanKg),["captured"]=N(CapturedKg),["prime"]=N(PrimeSeconds)};}
    public static CoolantCharge Read(IReadOnlyDictionary<string,string>d)
    {
        if(d.Count!=4||d["enabled"]!="0"&&d["enabled"]!="1")throw new ArgumentException("Unknown coolant record.");
        var c=new CoolantCharge{Enabled=d["enabled"]=="1",CleanKg=double.Parse(d["clean"],CultureInfo.InvariantCulture),CapturedKg=double.Parse(d["captured"],CultureInfo.InvariantCulture),PrimeSeconds=double.Parse(d["prime"],CultureInfo.InvariantCulture)};c.Validate();return c;
    }
}
