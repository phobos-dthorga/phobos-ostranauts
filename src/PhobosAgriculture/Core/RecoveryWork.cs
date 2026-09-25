using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Persistence;
namespace PhobosAgriculture.Core;
public static class RecoveryWork
{
    public static Dictionary<string,string> Save(string input,string filter,double energy)
    {
        if((input.Length==0)!=(filter.Length==0)||!CropState.Finite(energy)||energy<0||energy>100||input.Length==0&&energy!=0||input=="none"||filter=="none")throw new ArgumentException("Invalid treatment binding.");
        var d=new Dictionary<string,string>{["input"]=input.Length==0?"none":input,["filter"]=filter.Length==0?"none":filter,["energy"]=energy.ToString("R",CultureInfo.InvariantCulture)};
        foreach(var value in d.Values)if(!ObjectStateStore.SafeValue(value))throw new ArgumentException("Unsafe treatment identity.");
        return d;
    }
    public static (string Input,string Filter,double Energy) Read(IReadOnlyDictionary<string,string>d)
    {
        if(d.Count!=3)throw new ArgumentException("Unknown treatment fields.");
        string input=d["input"]=="none"?"":d["input"],filter=d["filter"]=="none"?"":d["filter"];
        double energy=double.Parse(d["energy"],CultureInfo.InvariantCulture);_=Save(input,filter,energy);return(input,filter,energy);
    }
}
