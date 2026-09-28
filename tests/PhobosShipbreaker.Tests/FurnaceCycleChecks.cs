using System;
using System.Collections.Generic;
using System.Linq;
using PhobosShipbreaker.Core;

internal static class FurnaceCycleChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var phases = (FurnacePhase[])Enum.GetValues(typeof(FurnacePhase));
        var flags = new[] { false, true };

        // The shared selector must reproduce the crew provider's previous hot-step choices exactly.
        foreach (var phase in phases)
        foreach (bool armed in flags)
        foreach (bool safe in flags)
        {
            string expected = phase == FurnacePhase.Idle ? (safe ? "seal" : "none") :
                phase == FurnacePhase.Equalize && safe ? "equalize" :
                phase == FurnacePhase.Ready && safe ? "release" :
                phase > FurnacePhase.Idle && phase < FurnacePhase.Equalize && !armed ? "auto-run" : "none";
            string actual = FurnaceCycle.Next(phase, armed, safe, true) switch
            {
                FurnaceStep.Seal => "seal", FurnaceStep.Equalize => "equalize", FurnaceStep.Release => "release",
                FurnaceStep.Resume => "auto-run", _ => "none"
            };
            check(actual == expected, $"Crew hot step unchanged for {phase}, armed {armed}, safe {safe}");
        }
        check(FurnaceCycle.Next(FurnacePhase.Idle, false, true, false) == FurnaceStep.Wait, "A partial charge is never sealed");
        check(FurnaceCycle.Next(FurnacePhase.Idle, false, false, true) == FurnaceStep.Wait, "A hot empty lining is not resealed");

        // The machine run starts only the batch it has just sealed.
        foreach (var phase in phases)
        foreach (bool armed in flags)
        foreach (bool qualified in flags)
        foreach (bool safe in flags)
        foreach (bool full in flags)
        {
            var action = FurnaceCycle.Repeat(phase, armed, qualified, safe, full);
            if (action == FurnaceRepeatAction.Start) check(phase == FurnacePhase.Sealed && !armed, $"Repeat never re-arms {phase}");
            if (action == FurnaceRepeatAction.Seal) check(phase == FurnacePhase.Idle && safe && full, "Repeat seals only a full, cool, idle charge");
            if (action == FurnaceRepeatAction.Release) check(phase == FurnacePhase.Ready && safe, "Repeat releases only a cool, equalized batch");
            if (action == FurnaceRepeatAction.Equalize) check(phase == FurnacePhase.Equalize && safe, "Repeat equalizes only below the release temperature");
            if (armed && phase != FurnacePhase.Delivering) check(action != FurnaceRepeatAction.Suspend, $"An armed {phase} batch keeps running");
        }
        foreach (var heating in new[] { FurnacePhase.Evacuating, FurnacePhase.Preheat, FurnacePhase.Melt, FurnacePhase.Hold })
            check(FurnaceCycle.Repeat(heating, false, false, false, true) == FurnaceRepeatAction.Suspend, $"An interrupted {heating} batch suspends the run");
        check(FurnaceCycle.Repeat(FurnacePhase.Solidify, false, true, false, true) == FurnaceRepeatAction.Wait &&
            FurnaceCycle.Repeat(FurnacePhase.Cool, false, true, false, true) == FurnaceRepeatAction.Wait, "A finished melt cools without heating permission");
        check(FurnaceCycle.Repeat(FurnacePhase.Solidify, false, false, false, true) == FurnaceRepeatAction.Suspend, "A stopped, unfinished melt suspends the run");
        check(FurnaceCycle.Repeat(FurnacePhase.Delivering, false, true, true, true) == FurnaceRepeatAction.Suspend, "An uncertain product commit suspends the run");
        check(FurnaceCycle.Repeat(FurnacePhase.Idle, false, false, true, false) == FurnaceRepeatAction.Wait, "The final partial charge stays unprocessed");

        // Saved intent round-trips; damaged, future or unsafe records fail closed.
        var record = new FurnaceRepeatRecord { ShipId = "SHIP-1", RoomId = "room-7", CoolingId = "radiator-3", Completed = 4 };
        var fields = record.Save();
        check(FurnaceRepeatRecord.TryLoad(fields, out var loaded) && loaded.ShipId == "SHIP-1" && loaded.RoomId == "room-7" &&
            loaded.CoolingId == "radiator-3" && loaded.Completed == 4 && loaded.Revision == FurnaceRules.RecipeRevision, "Repeat record round-trips");
        check(!fields.ContainsKey("armed") && !fields.ContainsKey("authorized") && !fields.Keys.Any(k => k.Contains("permission")), "Heating permission is never saved");
        foreach (var damage in new (string Key, string? Value)[] { ("ship", null), ("room", ""), ("cooling", "a=b"), ("completed", "-1"), ("completed", "x"),
            ("revision", (FurnaceRules.RecipeRevision + 1).ToString()), ("revision", "0"), ("room", "line\nbreak") })
        {
            var copy = new Dictionary<string, string>(fields);
            if (damage.Value == null) copy.Remove(damage.Key); else copy[damage.Key] = damage.Value;
            check(!FurnaceRepeatRecord.TryLoad(copy, out _), $"Damaged repeat field {damage.Key} is protected");
        }
    }
}
