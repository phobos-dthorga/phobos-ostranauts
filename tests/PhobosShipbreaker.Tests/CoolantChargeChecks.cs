using System;
using PhobosShipbreaker.Core;
internal static class CoolantChargeChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var c=new CoolantCharge{Enabled=true,CleanKg=6};c.Advance(32,true,true,64,true);
        check(c.Flow(64,true)>0&&c.Filled(true),"A full circuit and the base charge prime the longest supported route");
        check(!c.Filled(false)&&c.Flow(64,false)==0&&c.PressureKPa(false)==0,"The loop does not circulate until its conduit is full (Shipbreaker 0.58.0)");
        c.Advance(1200,false,false,64,true);
        check(Math.Abs(c.TotalKg-6)<1e-9&&Math.Abs(c.CapturedKg-1.2)<1e-9,"Broken-route leakage remains in secondary containment");
        check(!c.Filled(true)&&c.Flow(64,true)==0,"Loss of working charge below the base prevents circulation");
        var copy=CoolantCharge.Read(c.Save());check(copy.TotalKg==c.TotalKg&&copy.PrimeSeconds==0,"Reload preserves leakage and loses no fluid");
        c.Advance(3600,false,false,64,true);c.Advance(3600,false,false,64,true);check(c.CleanKg==0&&Math.Abs(c.CapturedKg-6)<1e-9,"Leak is bounded by actual inventory");

        // Priming the conduit from the reservoir: only the surplus above the base charge, at the pump's rate, never past the room.
        var r=new CoolantCharge{Enabled=true,CleanKg=5.2};
        check(Math.Abs(r.SurplusKg-.2)<1e-12&&Math.Abs(r.PrimeKg(60,10)-.2)<1e-12,"An old 0.01 kg-a-tile pipe allowance above the base charge primes the conduit");
        check(Math.Abs(new CoolantCharge{Enabled=true,CleanKg=6}.PrimeKg(3,10)-.3)<1e-12&&new CoolantCharge{Enabled=true,CleanKg=6}.PrimeKg(60,.25)==.25,"Priming follows the pump rate and stops when the circuit is full");
        check(new CoolantCharge{Enabled=true,CleanKg=4}.PrimeKg(60,10)==0,"The base charge never leaves the furnace");
        var held=CoolantCharge.HeldCoolant();
        check(Math.Abs(held.KgPerTile-Math.PI*.01*.01*1050)<1e-12&&Math.Abs(held.KgPerTile-.33)<.001,"A conduit tile holds 20 mm bore of service fluid, about 0.33 kg");

        var b=new FurnaceBatch{HotKJ=100000};double before=b.TotalKJ;b.Circulate(10,10,0);
        check(Math.Abs(b.TotalKJ-before-10)<1e-7,"Blocked coolant retains all consumed pump electricity as heat");
    }
}
