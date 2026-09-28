using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Sensors;

internal static class SensorLeaseChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var maps = new Dictionary<string, Dictionary<string, string>> { ["Panel A"] = new() { ["knob"] = "1" } };
        var store = SensorLeaseRecord.Store(maps, "sensor-unit-1", "IsSensorIR");
        check(SensorLeaseRecord.Read(store) == null, "A unit without a note has no lease");
        var lease = new SensorLeaseRecord("PhobosAutoNav", "console-7", SensorLeaseState.Leased);
        check(store.TryWrite(lease.Encode()), "A lease note fits the saved-state envelope");
        var read = SensorLeaseRecord.Read(store);
        check(read != null && read.BelongsTo("PhobosAutoNav", "console-7") && read.State == SensorLeaseState.Leased, "The note round-trips");
        check(!read!.BelongsTo("PhobosAutoNav", "console-8") && !read.BelongsTo("OtherMod", "console-7"), "Notes belong to one automation and holder");
        check(SensorLeaseRecord.Read(SensorLeaseRecord.Store(maps, "sensor-unit-1", "IsSensorRadar")) == null, "Each sensor type keeps its own note");
        check(SensorLeaseRecord.Read(SensorLeaseRecord.Store(maps, "copied-unit", "IsSensorIR")) == null, "A copied property map is not adopted by another unit");
        check(store.TryWrite(read.With(SensorLeaseState.Declined).Encode()) && SensorLeaseRecord.Read(store)!.State == SensorLeaseState.Declined,
            "A player switch-off turns the lease into a decline for the same holder");
        check(maps["Panel A"]["knob"] == "1", "Native property maps are otherwise untouched");

        maps["PhobosState.SensorLease.IsSensorIR"]["data.state"] = "Borrowed";
        check(SensorLeaseRecord.Read(store) == null, "An unknown state is not treated as a lease");
        maps["PhobosState.SensorLease.IsSensorIR"]["data.state"] = "Leased";
        maps["PhobosState.SensorLease.IsSensorIR"]["data.extra"] = "x";
        check(SensorLeaseRecord.Read(store) == null, "Unexpected fields are not treated as a lease");
        maps["PhobosState.SensorLease.IsSensorIR"]["schema"] = "2";
        check(store.Read(out _) == SavedStateStatus.UnsupportedVersion && !store.TryWrite(lease.Encode()), "A future note schema is left intact");
        bool rejected = false;
        try { _ = new SensorLeaseRecord("PhobosAutoNav", "bad,holder", SensorLeaseState.Leased); } catch (ArgumentException) { rejected = true; }
        check(rejected, "Unsafe holder identities are refused");

        var cooldown = new NoticeCooldown(20);
        check(cooldown.Allow("sensors", 100) && !cooldown.Allow("sensors", 110) && cooldown.Allow("other", 110), "Banner cooldown is per notice kind");
        check(cooldown.Allow("sensors", 120.5) && !cooldown.Allow("sensors", 121), "A quiet notice kind may show its banner again");
        check(cooldown.Allow("sensors", 3), "A restarted clock never suppresses notices indefinitely");
        check(!cooldown.Allow("nan", double.NaN), "An unreadable clock shows no banner");
    }
}
