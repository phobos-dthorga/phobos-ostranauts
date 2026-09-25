using System;
using PhobosShipbreaker.Core;
internal static class CoolantChargeChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var c=new CoolantCharge{Enabled=true,CleanKg=6};c.Advance(32,true,true,64);
        check(c.Flow(64)>0&&c.Filled(64),"Full charge primes the longest supported route");
        c.Advance(600,false,false,64);
        check(Math.Abs(c.TotalKg-6)<1e-9&&Math.Abs(c.CapturedKg-.6)<1e-9,"Broken-route leakage remains in secondary containment");
        check(!c.Filled(64)&&c.Flow(64)==0,"Loss of working charge prevents circulation");
        var copy=CoolantCharge.Read(c.Save());check(copy.TotalKg==c.TotalKg&&copy.PrimeSeconds==0,"Reload preserves leakage and loses no fluid");
        c.Advance(3600,false,false,64);c.Advance(3600,false,false,64);check(c.CleanKg==0&&Math.Abs(c.CapturedKg-6)<1e-9,"Leak is bounded by actual inventory");
        var b=new FurnaceBatch{HotKJ=100000};double before=b.TotalKJ;b.Circulate(10,10,0);
        check(Math.Abs(b.TotalKJ-before-10)<1e-7,"Blocked coolant retains all consumed pump electricity as heat");
    }
}
