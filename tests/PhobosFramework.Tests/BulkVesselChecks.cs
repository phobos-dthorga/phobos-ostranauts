using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;

/// <summary>The shared bulk vessel contract without a game object: declarations, the registry, the native-mass
/// invariant and the saved records a vessel keeps. Custody of a live CondOwner is exercised by the native suite.</summary>
internal static class BulkVesselChecks
{
    internal static void Run(Action<bool, string> check)
    {
        void Reject(Action action, string label) { bool failed = false; try { action(); } catch { failed = true; } check(failed, label); }
        var silo = new BulkVesselSpec("PhobosTestSilo", "water", 1000, 240, "test.owner", "TestSilo", "TestSiloWork", "TestSiloTransfer");
        check(silo.CapacityKg == 1000 && silo.DryKg == 240 && silo.Commodity == "water", "A declaration carries capacity, dry mass and one commodity");
        Reject(() => new BulkVesselSpec("PhobosTestSilo", "water", 0, 240, "o", "a", "b", "c"), "Zero capacity is refused");
        Reject(() => new BulkVesselSpec("PhobosTestSilo", "water", 1000, -1, "o", "a", "b", "c"), "Negative dry mass is refused");
        Reject(() => new BulkVesselSpec("PhobosTestSilo", "water", double.NaN, 240, "o", "a", "b", "c"), "Invalid capacity is refused");
        Reject(() => new BulkVesselSpec("PhobosTestSilo", "water", 1000, 240, "o", "same", "same", "c"), "Record, journal and guard names must differ");
        Reject(() => new BulkVesselSpec("", "water", 1000, 240, "o", "a", "b", "c"), "A family prefix is required");
        Reject(() => new BulkVesselSpec("PhobosTestSilo", " ", 1000, 240, "o", "a", "b", "c"), "A commodity is required");
        // Capacity from volume needs a declared density: Framework never assumes one.
        check(BulkVesselSpec.CapacityFromVolume(1, 1000) == 1000 && Math.Abs(BulkVesselSpec.CapacityFromVolume(40.4, 1107) - 44722.8) < 1e-9,
            "Capacity from volume multiplies by the declared density (water 1,000; the game's heavy water 1,107)");
        Reject(() => BulkVesselSpec.CapacityFromVolume(1, 0), "A zero density cannot give a capacity");
        Reject(() => BulkVesselSpec.CapacityFromVolume(-1, 1000), "A negative volume cannot give a capacity");

        // Framework 0.101.0 (owner report, 5 October 2026): an object destroyed while the game unloads a ship has already
        // been taken off that ship, so a ship's unload under way counts as unloading too. Nothing held is announced as lost.
        check(Phobos.Ostranauts.Framework.FrameworkLifecycle.IsUnloading(true, 0) && Phobos.Ostranauts.Framework.FrameworkLifecycle.IsUnloading(false, 1) &&
              Phobos.Ostranauts.Framework.FrameworkLifecycle.IsUnloading(false, 2) && !Phobos.Ostranauts.Framework.FrameworkLifecycle.IsUnloading(false, 0),
            "A store goes with its ship's unload when its ship is marked destroyed or any ship's unload is under way, and only then");
        BulkVessels.Unregister("test.owner"); BulkVessels.Unregister("other.owner");
        BulkVessels.Register(silo);
        check(BulkVessels.SpecFor("PhobosTestSiloInstalled") == silo && BulkVessels.SpecFor("PhobosTestSiloLooseDmg") == silo, "The four native forms of a family resolve to its declaration");
        check(BulkVessels.SpecFor("PhobosTestSilo") == null && BulkVessels.SpecFor("PhobosTestSiloSection") == null && BulkVessels.SpecFor(null) == null, "Other identities are not vessels");
        var again = new BulkVesselSpec("PhobosTestSilo", "water", 1200, 240, "test.owner", "TestSilo", "TestSiloWork", "TestSiloTransfer");
        BulkVessels.Register(again);
        check(BulkVessels.SpecFor("PhobosTestSiloInstalled") == again && BulkVessels.All.Count(s => s.Family == "PhobosTestSilo") == 1, "The same owner re-registering a family replaces its declaration");
        Reject(() => BulkVessels.Register(new BulkVesselSpec("PhobosTestSilo", "water", 1000, 240, "other.owner", "OtherSilo", "OtherSiloWork", "OtherSiloTransfer")), "Another owner cannot claim a registered family");
        Reject(() => BulkVessels.Register(new BulkVesselSpec("PhobosTestOther", "water", 1000, 240, "test.owner", "TestSilo", "X", "Y")), "One owner cannot reuse a record name for a second family");
        BulkVessels.Register(new BulkVesselSpec("PhobosTestTank", "coolant", 100, 20, "other.owner", "TestTank", "TestTankWork", "TestTankTransfer"));
        check(BulkVessels.All.Count == 2, "Different families from different owners coexist");
        BulkVessels.Unregister("other.owner");
        check(BulkVessels.All.Count == 1 && BulkVessels.SpecFor("PhobosTestTankInstalled") == null, "Unregistering removes only that owner's families");

        // The native-mass invariant: dry housing plus contents plus physical cargo.
        var state = new StoredCommodity("water", 1200); state.SetService(300);
        check(BulkVessel.ExpectedMassKg(again, state, 0) == 540 && BulkVessel.ExpectedMassKg(again, state, 12.5) == 552.5, "Expected native mass is dry housing plus contents plus cargo");
        state.Isolate();
        check(BulkVessel.ExpectedMassKg(again, state, 0) == 540, "Contents trapped in the catch still weigh what they weigh");
        check(BulkVessel.MassMatches(540, 540 + 1e-6) && !BulkVessel.MassMatches(540, 540.01) && !BulkVessel.MassMatches(double.NaN, 540), "The mass check tolerates rounding and nothing else");

        // The records a vessel keeps: same envelope as the R3 reservoir, so nothing about a saved R3 changes.
        var maps = new Dictionary<string, Dictionary<string, string>>();
        var store = new ObjectStateStore(maps, again.Record, again.Owner, 1);
        check(store.Read(out _) == SavedStateStatus.Missing, "A new vessel has no record and reads as empty");
        check(store.TryWrite(state.Save()) && maps.ContainsKey("PhobosState.TestSilo") && maps["PhobosState.TestSilo"]["owner"] == "test.owner", "The record is saved under the declared name and owner");
        var read = StoredCommodity.Read(maps["PhobosState.TestSilo"].Where(p => p.Key.StartsWith("data.")).ToDictionary(p => p.Key.Substring(5), p => p.Value), "water", 1200);
        check(read.CatchKg == 300 && read.ServiceKg == 0 && read.CapacityKg == 1200, "The saved record round-trips service, catch and reserve");
        Reject(() => StoredCommodity.Read(maps["PhobosState.TestSilo"].Where(p => p.Key.StartsWith("data.")).ToDictionary(p => p.Key.Substring(5), p => p.Value), "water", 200),
            "A smaller declared capacity never discards saved contents");
        var journal = new ObjectStateStore(maps, again.Journal, again.Owner, 1);
        check(journal.TryWrite(new Dictionary<string, string> { ["state"] = "pending", ["item"] = "ice-1", ["before"] = "0" }), "A conversion journal opens before the first mutation");
        check(journal.Read(out var pending) == SavedStateStatus.Ready && pending["state"] == "pending", "An open journal is evidence of an interrupted conversion");
        check(journal.TryWrite(new Dictionary<string, string> { ["state"] = "clear" }) && journal.Read(out var clear) == SavedStateStatus.Ready && clear.Count == 1, "Ending the conversion clears the journal");
        var snapshot = new BulkVesselSnapshot("silo-1", "ship-1", "water", 300, 100, 50, 1200, 3, false);
        check(snapshot.AvailableKg == 250 && snapshot.HeadroomKg == 800, "A snapshot reports available (above reserve) and headroom (capacity less service and catch)");
        check(new BulkVesselSnapshot("silo-1", "ship-1", "water", 300, 100, 50, 1200, 3, true).HeadroomKg == 0, "A protected vessel offers no headroom");
        BulkVessels.Unregister("test.owner");

        // Damage policy: silos isolate into the catch chamber; a pressurised store declares a leak instead.
        check(silo.DamagePolicy == VesselDamagePolicy.Isolate && silo.LeakKgPerHour == 0, "The eight-argument declaration isolates, as every existing vessel does");
        var tank = new BulkVesselSpec("PhobosTestStore", "hydrogen", 24, 160, "test.owner", "TestStore", "TestStoreWork", "TestStoreTransfer", VesselDamagePolicy.Leak, 2);
        check(tank.DamagePolicy == VesselDamagePolicy.Leak && tank.LeakKgPerHour == 2, "A leaking vessel declares its rate");
        check(tank.LeakKg(0.5, 24) == 1 && tank.LeakKg(20, 5) == 5 && tank.LeakKg(0, 24) == 0 && silo.LeakKg(10, 300) == 0,
            "The leak is rate times hours, never more than the contents, and an isolating vessel never leaks");
        Reject(() => new BulkVesselSpec("PhobosTestStore", "hydrogen", 24, 160, "o", "a", "b", "c", VesselDamagePolicy.Leak, 0), "A leaking vessel needs a positive rate");
        Reject(() => new BulkVesselSpec("PhobosTestStore", "hydrogen", 24, 160, "o", "a", "b", "c", VesselDamagePolicy.Isolate, 2), "An isolating vessel declares no rate");
        Reject(() => new BulkVesselSpec("PhobosTestStore", "hydrogen", 24, 160, "o", "a", "b", "c", VesselDamagePolicy.Leak, double.NaN), "An invalid rate is refused");
        Reject(() => tank.LeakKg(-1, 24), "A negative interval is refused");

        // Framework 0.79.0: the right-click card shows the record, never the other way round.
        check(VesselContentsDisplay.ConditionName("water") == "StatPhobosVesselWater" && VesselContentsDisplay.ConditionName("carbon dioxide") == "StatPhobosVesselCarbonDioxide" &&
              VesselContentsDisplay.ConditionName("sulfuric acid") == "StatPhobosVesselSulfuricAcid" && VesselContentsDisplay.ConditionName("crop nutrients") == "StatPhobosVesselCropNutrients",
            "Each commodity's row is Phobos' own condition, named in title case");
        Reject(() => VesselContentsDisplay.ConditionName(" "), "A row needs a commodity");
        Reject(() => VesselContentsDisplay.ConditionName("--"), "A row needs a commodity with a letter or digit");
        var record = new StoredCommodity("water", 1000);
        check(VesselContentsDisplay.Rows(record) == (0, 0), "An empty vessel shows no row: the game removes a condition at zero");
        record.SetService(640);
        check(VesselContentsDisplay.Rows(record) == (640, 0), "A vessel in service shows its contents and no trapped row");
        record.Contain(140);
        check(VesselContentsDisplay.Rows(record) == (500, 140), "A spill held in the catch chamber shows on the trapped row");
        record.Isolate();
        check(VesselContentsDisplay.Rows(record) == (0, 640), "A damaged isolating vessel shows everything as trapped until it is recovered");
        check(VesselContentsDisplay.Rows(null) == (0, 0), "An unreadable record shows nothing; the vessel's panel says why");
    }
}
