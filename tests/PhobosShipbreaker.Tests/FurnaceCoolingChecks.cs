using System;
using System.Collections.Generic;
using PhobosShipbreaker.Core;

internal static class FurnaceCoolingChecks
{
    internal static void Run(Action<bool, string> check)
    {
        foreach (double angle in new[] { 0, 90, 180, 270 })
        foreach (bool port in new[] { false, true })
        foreach (int side in port ? new[] { -1, 1 } : new[] { 0 })
        {
            var p = IntakeRules.Rotate(port ? side * 3.5 : 0, port ? .5 : 6, angle);
            check(FurnaceCooling.Aligned(port, 10, -20, angle, 10 + p.X, -20 + p.Y, angle), "Both cooling layouts rotate around their exact sockets");
            check(!FurnaceCooling.Aligned(port, 10, -20, angle, 10 + p.X, -20 + p.Y, angle + 90), "Wrong equipment orientation blocks cooling");
            check(!FurnaceCooling.Aligned(port, 10, -20, angle, 11 + p.X, -20 + p.Y, angle), "Nearby hardware is not an implicit cooling connection");
        }
        check(!FurnaceCooling.Aligned(true, 0, 0, 0, 0, -3.5, 0), "Port cannot occupy front operator access");
        check(FurnaceCooling.FloorSupport(true, true, false, false, true), "Intact sealed native floor supports underside assembly");
        foreach (var flags in new[] { (false,true,false,false,true), (true,false,false,false,true), (true,true,true,false,true), (true,true,false,true,true), (true,true,false,false,false) })
            check(!FurnaceCooling.FloorSupport(flags.Item1, flags.Item2, flags.Item3, flags.Item4, flags.Item5), "Missing, unsealed, wall, EVA or damaged support is rejected");
        check(FurnaceCooling.CanChange(false, true, FurnaceRules.ReleaseK, 0, 0), "Cool idle empty connection can change");
        check(!FurnaceCooling.CanChange(false, true, FurnaceRules.ReleaseK + .01, 0, 0) &&
            !FurnaceCooling.CanChange(false, false, 300, 0, 0) && !FurnaceCooling.CanChange(false, true, 300, 1, 0) &&
            !FurnaceCooling.CanChange(false, true, 300, 0, 1) && !FurnaceCooling.CanChange(true, true, 300, 0, 0),
            "Hot, sealed, physically loaded or protected equipment cannot be switched or removed");
        foreach (bool damaged in new[] { false, true })
        {
            double outside = 12000, underside = outside;
            double loss = FurnaceCooling.Radiate(ref outside, 60, true, damaged);
            double alternateLoss = FurnaceCooling.Radiate(ref underside, 60, true, damaged);
            check(outside == underside && loss == alternateLoss && Math.Abs(outside + loss - 12000) < 1e-8, "Both installations use one conserved finite radiation model, including damage");
            double paused = underside;
            check(FurnaceCooling.Radiate(ref underside, 60, false, damaged) == 0 && underside == paused, "Lost port support retains all stored heat");
            var batch = new FurnaceBatch { Phase = FurnacePhase.Cool, HotKJ = FurnaceRules.Enthalpy(600), SinkKJ = 1000, Armed = true };
            double before = batch.TotalKJ;
            batch.Passive(1, false, true, 300, 0, false);
            check(!batch.Armed && batch.SinkKJ > 1000 && Math.Abs(before - batch.TotalKJ) < 1e-8, "Probe and power loss do not disable available passive heat transfer");
        }
        var legacy = new Dictionary<string,string> { ["sink"] = "12345.6789" };
        check(FurnaceCooling.TryRead(legacy, out double retained) && retained == 12345.6789, "Legacy radiator record loads without conversion");
        check(FurnaceCooling.TryRead(FurnaceCooling.Save(retained), out double portHeat) && portHeat == retained, "Port uses the same precise saved thermal store");
        legacy["sink"] = "NaN"; check(!FurnaceCooling.TryRead(legacy, out _), "Unavailable heat cannot become zero");
        check(!FurnaceCooling.TryRead(new Dictionary<string,string> { ["sink"]="100", ["future"]="1" }, out _), "Future cooling records remain protected");
    }
}
