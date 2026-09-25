using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Liquids;

namespace PhobosAgriculture.Core;
public static class DrainageRecovery
{
    // Authored treatment budgets. Measured residual feed only, never unknown legacy waste.
    public const double WaterYield=.9, NutrientYield=.8, CartridgeKg=.05, KWhPerKg=.01, PowerKW=.5;
    public static LiquidMixture Recover(LiquidMixture input)=>new(input.CarrierKg*WaterYield,input.SoluteKg*NutrientYield);
    public static double RejectKg(LiquidMixture input)=>input.TotalKg+CartridgeKg-Recover(input).TotalKg;
    public static Dictionary<string,string> Save(LiquidMixture q)
    {
        Validate(q,q.TotalKg); return new(){["water"]=q.CarrierKg.ToString("R",CultureInfo.InvariantCulture),["nutrients"]=q.SoluteKg.ToString("R",CultureInfo.InvariantCulture)};
    }
    public static LiquidMixture Read(IReadOnlyDictionary<string,string> d,double mass)
    {
        if(d.Count!=2) throw new ArgumentException("Unknown drainage composition.");
        var q=new LiquidMixture(double.Parse(d["water"],CultureInfo.InvariantCulture),double.Parse(d["nutrients"],CultureInfo.InvariantCulture));Validate(q,mass);return q;
    }
    private static void Validate(LiquidMixture q,double mass)
    {
        if(!CropState.Finite(q.CarrierKg)||!CropState.Finite(q.SoluteKg)||!CropState.Finite(mass)||q.CarrierKg<0||q.SoluteKg<0||mass<=0||Math.Abs(q.TotalKg-mass)>1e-7) throw new ArgumentException("Uncharacterized drainage.");
    }
}
