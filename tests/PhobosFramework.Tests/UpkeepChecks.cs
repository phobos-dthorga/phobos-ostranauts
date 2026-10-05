using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Data;

/// <summary>Crew upkeep (Framework 0.111.0): tuning, fading, inspection, planning, records and the data pack.</summary>
internal static class UpkeepChecks
{
    internal static void Run(Action<bool, string> check)
    {
        void Near(double a, double b, string m) => check(Math.Abs(a - b) < 1e-9, m + $": {a} vs {b}");
        var pack = new UpkeepPack();
        check(pack.tuneStep == 0.2 && pack.tuneStepSkilled == 0.3 && pack.inspectionValidHours == 24 && pack.inspectedFadeShare == 0.5 && pack.practiceMinutes == 10, "Shipped upkeep figures");

        // The rate: exactly 1 untuned, the full gain fully tuned, the family's share respected.
        check(UpkeepRules.Rate(0, 0.10, 1) == 1, "An untuned machine works exactly as before");
        Near(UpkeepRules.Rate(1, 0.10, 1), 1.10, "A full tune is the whole gain");
        Near(UpkeepRules.Rate(0.5, 0.10, 1), 1.05, "Half a tune is half the gain");
        Near(UpkeepRules.Rate(1, 0.10, 0.5), 1.05, "A family's share scales its gain");
        check(UpkeepRules.Rate(1, 0, 1) == 1 && UpkeepRules.Rate(1, 0.10, 0) == 1, "A zero gain or a zero share turns the benefit off");
        check(UpkeepRules.Rate(7, 0.10, 3) <= 1.10 + 1e-12 && UpkeepRules.Rate(-1, 0.10, 1) == 1, "Out-of-range levels and shares are held to their limits");

        // Fading: with work only, evenly over the fade hours, at the inspected share while inspected.
        Near(UpkeepRules.Fade(1, 18 * 3600, 36, false, 0.5), 0.5, "Half the fade hours of work costs half a tune");
        Near(UpkeepRules.Fade(1, 18 * 3600, 36, true, 0.5), 0.75, "An inspected machine fades at half the rate");
        check(UpkeepRules.Fade(0.6, 0, 36, false, 0.5) == 0.6, "A machine that does not work keeps its tune");
        check(UpkeepRules.Fade(0.1, 36 * 3600, 36, false, 0.5) == 0, "A tune never fades below none");
        check(UpkeepRules.InspectionGood(1000, 1000 + 23 * 3600, 24) && !UpkeepRules.InspectionGood(1000, 1000 + 24 * 3600, 24) && !UpkeepRules.InspectionGood(0, 5000, 24),
            "An inspection is good for its hours and never before one was made");

        // Sessions.
        Near(UpkeepRules.Tuned(0, false, pack), 0.2, "An unskilled session adds a step");
        Near(UpkeepRules.Tuned(0, true, pack), 0.3, "A skilled session adds more");
        check(UpkeepRules.Tuned(0.9, true, pack) == 1, "A tune is never more than full");
        check(UpkeepRules.TuneDue(0.8, pack) && !UpkeepRules.TuneDue(0.81, pack), "Tuning is offered while a whole step still fits");
        check(!UpkeepRules.WorthSaving(0.504, 0.5) && UpkeepRules.WorthSaving(0.51, 0.5) && UpkeepRules.WorthSaving(0, 0.004), "A record is written on a whole percent, and when the tune runs out");

        // Planning: tuning before inspection, least tuned first, stable order.
        var machines = new[]
        {
            ("b-press", new UpkeepState { Level = 0.4 }, true),
            ("a-still", new UpkeepState { Level = 0.1 }, true),
            ("c-bed", new UpkeepState { Level = 0 }, false),
            ("d-full", new UpkeepState { Level = 1, Inspected = 100000 }, true),
            ("e-protected", new UpkeepState { Protected = true }, true),
        };
        double now = 100000 + 3600;
        var plan = UpkeepRules.Plan(machines, true, true, now, pack);
        check(plan.Select(c => c.Id).SequenceEqual(new[] { "a-still", "b-press", "c-bed" }), "Least tuned first, then inspection; a full, inspected machine and a protected one are left out: " + string.Join(",", plan.Select(c => c.Id)));
        check(plan[0].Kind == UpkeepKind.Tune && plan[2].Kind == UpkeepKind.Inspect, "A machine that is never tuned is only inspected");
        check(UpkeepRules.Plan(machines, false, true, now, pack).Select(c => c.Id).SequenceEqual(new[] { "a-still", "b-press", "c-bed" }) &&
              UpkeepRules.Plan(machines, false, true, now, pack).All(c => c.Kind == UpkeepKind.Inspect), "With tuning off every uninspected machine is inspected");
        check(UpkeepRules.Plan(machines, true, false, now, pack).Count == 2 && UpkeepRules.Plan(machines, false, false, now, pack).Count == 0, "Each switch plans only its own work");
        check(UpkeepRules.Plan(machines, true, true, 100000 + 30 * 3600, pack).Any(c => c.Id == "d-full" && c.Kind == UpkeepKind.Inspect), "An inspection that has run out is due again");

        // The record: a round trip, and an old machine without one.
        var state = new UpkeepState { Level = 0.37, Inspected = 123456.5 };
        var back = UpkeepRules.Decode(UpkeepRules.Encode(state));
        check(back.Level == 0.37 && back.Inspected == 123456.5 && back.SavedLevel == 0.37, "A machine's upkeep record round-trips");
        check(UpkeepRules.Encode(state).All(f => Phobos.Ostranauts.Framework.Persistence.ObjectStateStore.SafeValue(f.Value)), "Its fields fit a Phobos record");
        var empty = UpkeepRules.Decode(new Dictionary<string, string>());
        check(empty.Level == 0 && empty.Inspected == 0 && !empty.Protected, "A machine saved before upkeep existed reads as untuned and uninspected");
        check(UpkeepRules.Decode(new Dictionary<string, string> { ["level"] = "9", ["inspected"] = "NaN" }).Level == 1, "A record out of range is held to a full tune and no inspection");

        // Time-skips: banked crew time buys whole sessions, walking included.
        double bank = 700;
        check(UpkeepRules.Sessions(ref bank, 600, 40) == 1 && Math.Abs(bank - 60) < 1e-9, "A banked session and its walk are spent together");
        check(UpkeepRules.Sessions(ref bank, 600, 40) == 0 && Math.Abs(bank - 60) < 1e-9, "Less than a session buys nothing and is kept");

        // Framework 0.113.0: four switches; a record from 0.111.0 reads with practice and housekeeping off.
        var old = UpkeepSwitches.Decode(new Dictionary<string, string> { ["tune"] = "1", ["inspect"] = "0" });
        check(old.Tune && !old.Inspect && !old.Practice && !old.Housekeeping && old.Any, "An upkeep switch record from 0.111.0 keeps its switches and reads the new ones as off");
        check(!UpkeepSwitches.Decode(new Dictionary<string, string>()).Any, "A player without a switch record has every switch off");
        var all = new UpkeepSwitches().With(UpkeepKind.Practice, true).With(UpkeepKind.Housekeeping, true);
        var round = UpkeepSwitches.Decode(all.Encode());
        check(round.Practice && round.Housekeeping && !round.Tune && !round.Inspect && round[UpkeepKind.Housekeeping], "The four switches round-trip");
        check(all.Encode().All(f => Phobos.Ostranauts.Framework.Persistence.ObjectStateStore.SafeValue(f.Value)), "The switch record fits a Phobos record");
        check(UpkeepRules.Priority.SequenceEqual(new[] { UpkeepKind.Tune, UpkeepKind.Inspect, UpkeepKind.Housekeeping, UpkeepKind.Practice }), "Tuning and inspection come first, then housekeeping, then practice");
        check((int)UpkeepKind.Tune == 0 && (int)UpkeepKind.Inspect == 1, "Existing upkeep kinds keep their numbers");

        // Housekeeping: where an item goes.
        var stores = new[]
        {
            new TidyStoreChoice("locker-near", 2, false, false, true),
            new TidyStoreChoice("locker-same", 9, true, false, true),
            new TidyStoreChoice("bin-far", 12, false, true, true),
            new TidyStoreChoice("bin-same", 20, true, true, true),
            new TidyStoreChoice("locker-full", 1, true, false, false),
        };
        check(UpkeepRules.TidyDestination(true, stores) == "locker-same" || UpkeepRules.TidyDestination(true, stores) == "bin-same", "A Phobos supply goes to a store already holding its kind");
        check(UpkeepRules.TidyDestination(true, stores) == "locker-same", "The nearer of two stores holding its kind wins: " + UpkeepRules.TidyDestination(true, stores));
        check(UpkeepRules.TidyDestination(true, stores.Where(s => !s.HoldsSame)) == "bin-far", "With no store holding its kind, a Phobos supply goes to a tidy store, never an empty locker");
        check(UpkeepRules.TidyDestination(false, stores) == "bin-same", "Any other item goes only to a tidy store, the one holding its kind first");
        check(UpkeepRules.TidyDestination(false, stores.Where(s => !s.Tidy)) == null, "The game's own clutter is never sorted into lockers");
        check(UpkeepRules.TidyDestination(true, new[] { new TidyStoreChoice("full", 1, true, true, false) }) == null, "An item with nowhere it fits stays where it lies");
        check(UpkeepRules.TidyDestination(true, new[] { new TidyStoreChoice("b", 3, true, false, true), new TidyStoreChoice("a", 3, true, false, true) }) == "a", "Ties go by id, so the choice is stable");

        // Settings and the data pack.
        var clamped = new UpkeepSettings { InspectionMinutes = 500, TuningMinutes = 0, MaxTuningGain = double.NaN, TuneFadeHours = 1 }.Clamped();
        check(clamped.InspectionMinutes == 30 && clamped.TuningMinutes == 2 && clamped.MaxTuningGain == 0.10 && clamped.TuneFadeHours == 6, "Settings are held to their ranges");
        check(new UpkeepSettings().InspectionMinutes == 5, "An inspection takes five game minutes by default (owner choice)");
        const string shipped = @"{ ""schemaVersion"": 1, ""schema"": ""upkeep"", ""tuneStep"": 0.2, ""tuneStepSkilled"": 0.3, ""inspectionValidHours"": 24, ""inspectedFadeShare"": 0.5, ""families"": { ""manufacturing.refinery"": { ""gainShare"": 0.5 } } }";
        var loaded = DataPacks.LoadText<UpkeepPack>(shipped, "", "test", UpkeepSchema.Name, UpkeepSchema.Validate);
        check(loaded.families["manufacturing.refinery"].gainShare == 0.5, "The upkeep pack loads with a family's share");
        foreach (var (find, replace, why) in new[]
        {
            ("\"tuneStep\": 0.2", "\"tuneStep\": 0", "A session must add something"),
            ("\"tuneStepSkilled\": 0.3", "\"tuneStepSkilled\": 0.1", "Skill never makes a session worth less"),
            ("\"inspectedFadeShare\": 0.5", "\"inspectedFadeShare\": 1.5", "The inspected share is at most the whole fade"),
            ("\"gainShare\": 0.5", "\"gainShare\": 2", "A family's share is at most the whole gain"),
            ("\"inspectionValidHours\": 24", "\"inspectionValidHours\": 24, \"colour\": 1", "Unknown fields are refused"),
            ("\"inspectionValidHours\": 24", "\"inspectionValidHours\": 24, \"practiceMinutes\": 1", "A practice session lasts at least two minutes"),
        })
        {
            bool refused = false;
            try { DataPacks.LoadText<UpkeepPack>(shipped.Replace(find, replace), "", "test", UpkeepSchema.Name, UpkeepSchema.Validate); }
            catch (Exception ex) when (ex is ArgumentException || ex is FormatException) { refused = true; }
            check(shipped.Contains(find) && refused, why);
        }
    }
}
