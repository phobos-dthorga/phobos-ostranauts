using System;
using System.Collections.Generic;
using System.Globalization;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Mass custody only. Content owns chemistry, native mass and admission.</summary>
public sealed class StoredCommodity
{
    public string Commodity { get; }
    public double CapacityKg { get; }
    public double ServiceKg { get; private set; }
    public double CatchKg { get; private set; }
    public double ReserveKg { get; private set; }
    public long Revision { get; private set; }
    public double TotalKg => ServiceKg + CatchKg;
    public double AvailableKg => Math.Max(0, ServiceKg - ReserveKg);
    public StoredCommodity(string commodity, double capacity)
    { if(string.IsNullOrWhiteSpace(commodity)||!Valid(capacity)||capacity<=0)throw new ArgumentException("Invalid commodity capacity");Commodity=commodity;CapacityKg=capacity; }
    private static bool Valid(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x)&&x>=0;
    public void SetService(double kg)
    { if(!Valid(kg)||kg+CatchKg>CapacityKg+1e-8)throw new ArgumentOutOfRangeException(nameof(kg));ServiceKg=kg;Revision++; }
    public void SetReserve(double kg)
    { if(!Valid(kg)||kg>CapacityKg)throw new ArgumentOutOfRangeException(nameof(kg));ReserveKg=kg;Revision++; }
    public void Isolate(){if(ServiceKg<=0)return;CatchKg+=ServiceKg;ServiceKg=0;Revision++;}
    public void Recover(){if(CatchKg<=0)return;ServiceKg+=CatchKg;CatchKg=0;Revision++;}
    public Dictionary<string,string> Save()=>new(){["commodity"]=Commodity,["service"]=ServiceKg.ToString("R",CultureInfo.InvariantCulture),["catch"]=CatchKg.ToString("R",CultureInfo.InvariantCulture),["reserve"]=ReserveKg.ToString("R",CultureInfo.InvariantCulture),["revision"]=Revision.ToString(CultureInfo.InvariantCulture)};
    public static StoredCommodity Read(IReadOnlyDictionary<string,string> d,string commodity,double capacity)
    {
        if(d.Count!=5||d["commodity"]!=commodity)throw new ArgumentException("Unknown commodity record");
        double N(string k)=>double.Parse(d[k],CultureInfo.InvariantCulture);
        var s=new StoredCommodity(commodity,capacity){ServiceKg=N("service"),CatchKg=N("catch"),ReserveKg=N("reserve"),Revision=long.Parse(d["revision"],CultureInfo.InvariantCulture)};
        if(!Valid(s.ServiceKg)||!Valid(s.CatchKg)||!Valid(s.ReserveKg)||s.TotalKg>capacity+1e-8||s.ReserveKg>capacity||s.Revision<0)throw new ArgumentException("Invalid stored mass");return s;
    }
}

/// <summary>Main-thread exclusive capacity/input custody, held by one operation only.</summary>
public static class CommodityReservations
{
    private static readonly Dictionary<string,string> owners=new(StringComparer.Ordinal);
    public static bool Held(string endpoint)=>owners.ContainsKey(endpoint);
    public static bool TryAcquire(string operation,params string[] endpoints)
    {
        if(string.IsNullOrWhiteSpace(operation)||endpoints.Length==0)throw new ArgumentException("Missing reservation identity");
        foreach(var id in endpoints)if(string.IsNullOrWhiteSpace(id)||owners.ContainsKey(id))return false;
        foreach(var id in endpoints)owners[id]=operation;return true;
    }
    public static void Release(string operation)
    { var keys=new List<string>();foreach(var p in owners)if(p.Value==operation)keys.Add(p.Key);foreach(var k in keys)owners.Remove(k); }
}
