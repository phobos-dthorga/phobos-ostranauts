using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Liquids;

namespace PhobosShipbreaker.Core;
/// <summary>Optional serviced loop. Leaks enter a sealed catch tank, preserving mass and existing thermal energy.</summary>
public sealed class CoolantCharge
{
    public const double CapacityKg=6, BaseChargeKg=5, PipeKgPerCell=.01, LeakKgPerSecond=.001, RatedKPa=200;
    public bool Enabled;
    public double CleanKg, CapturedKg, PrimeSeconds;
    public double TotalKg=>CleanKg+CapturedKg;
    public static double Required(int cells)=>cells>=1&&cells<=FurnaceCooling.RouteLimit?BaseChargeKg+PipeKgPerCell*cells:throw new ArgumentOutOfRangeException(nameof(cells));
    public bool Filled(int cells)=>CleanKg+1e-9>=Required(cells);
    public double PressureKPa(int cells)=>RatedKPa*Math.Pow(Math.Min(1,CleanKg/Required(cells)),2);
    public double Flow(int cells)=>Filled(cells)&&PrimeSeconds>=cells*.5?HydraulicRoute.FlowFraction(cells,32):0;
    public void Advance(double seconds,bool intact,bool powered,int cells)
    {
        if(!double.IsFinite(seconds)||seconds<0||seconds>3600) throw new ArgumentOutOfRangeException(nameof(seconds));
        if(!Enabled)return;
        if(!intact) { double leak=Math.Min(CleanKg,seconds*LeakKgPerSecond);CleanKg-=leak;CapturedKg+=leak;PrimeSeconds=0; }
        else if(powered&&Filled(cells)) PrimeSeconds=Math.Min(cells*.5,PrimeSeconds+seconds);
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
