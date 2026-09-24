using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosAutoNav.Core;

internal static class PersistenceChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            foreach (var mode in Enum.GetValues<SavedFlightMode>())
            foreach (bool coast in new[] { true, false })
            foreach (bool torch in new[] { true, false })
            {
                var original = new FlightSnapshot
                {
                    ConsoleId = "console-1", ModuleId = "module-2", ShipId = "ship-3", PlayerId = "player-4", TargetId = "ship-5",
                    CruiseMS = 123.5, ArrivalMS = 0.25, ArrivalKM = 0.1, ElapsedSeconds = 8765.4321,
                    Coast = new CoastSettings(3.5, 12.5, .75, 2.5), Coasting = coast, Mode = mode, PreferTorch = torch
                };
                var maps = new Dictionary<string, Dictionary<string, string>>();
                var store = new ObjectStateStore(maps, FlightSnapshot.StoreName, original.ConsoleId, FlightSnapshot.Schema);
                check(original.Valid && store.TryWrite(original.Encode()), "Persist each supported flight state");
                // Detached reload: neither snapshots nor stores survive this boundary.
                var reloaded = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(JsonSerializer.Serialize(maps))!;
                store = new ObjectStateStore(reloaded, FlightSnapshot.StoreName, "console-1", FlightSnapshot.Schema);
                check(store.Read(out var fields) == SavedStateStatus.Ready && FlightSnapshot.TryDecode(fields, out _), "Round trip uses invariant units under non-English culture");
                FlightSnapshot.TryDecode(fields, out var restored);
                check(restored.TargetId == original.TargetId && restored.CruiseMS == original.CruiseMS &&
                    restored.ArrivalMS == original.ArrivalMS && restored.ArrivalKM == original.ArrivalKM &&
                    restored.ElapsedSeconds == original.ElapsedSeconds && restored.Coasting == coast && restored.Mode == mode && restored.PreferTorch == torch,
                    "Reload retains intent, completion state, timeout budget and hysteresis");
                check(restored.Coast.MinimumToleranceMS == 3.5 && restored.Coast.SpeedTolerancePercent == 12.5 &&
                    restored.Coast.EnterFraction == .75 && restored.Coast.BurnHeadingToleranceDegrees == 2.5, "Coast profile remains captured per flight");
                check(restored.Matches("console-1", "module-2", "ship-3", "player-4") &&
                    !restored.Matches("different", "module-2", "ship-3", "player-4") &&
                    !restored.Matches("console-1", "replacement", "ship-3", "player-4") &&
                    !restored.Matches("console-1", "module-2", "docked-neighbour", "player-4") &&
                    !restored.Matches("console-1", "module-2", "ship-3", "other-player"), "Identity binding rejects moved/replaced hardware and different ships/players");
                foreach (string key in original.Encode().Keys)
                {
                    var missing = original.Encode(); missing.Remove(key);
                    if (key == "preferTorch")
                    {
                        check(FlightSnapshot.TryDecode(missing, out var legacy) && !legacy.PreferTorch, "Schema-1 RCS flights do not acquire new torch permission");
                        continue;
                    }
                    check(!FlightSnapshot.TryDecode(missing, out _), "Missing fields cannot silently default: " + key);
                }
                foreach (string value in new[] { "NaN", "Infinity", "-1", "1,5" })
                {
                    var invalid = original.Encode(); invalid["elapsedSeconds"] = value;
                    check(!FlightSnapshot.TryDecode(invalid, out _), "Invalid elapsed time cannot reset or evade timeout");
                }
                var selfTarget = original.Encode(); selfTarget["target"] = original.ShipId;
                check(!FlightSnapshot.TryDecode(selfTarget, out _), "Cannot reload a self-target");
                var invalidMode = original.Encode(); invalidMode["mode"] = "999";
                check(!FlightSnapshot.TryDecode(invalidMode, out _), "Unknown lifecycle states are not resumed");
                invalidMode = original.Encode(); invalidMode["preferTorch"] = "maybe";
                check(!FlightSnapshot.TryDecode(invalidMode, out _), "Invalid torch permission is not accepted");
            }
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }
}
