using System;
using System.Linq;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Persistence;

internal static class CaptureChecks
{
    internal static void Run(Action<bool, string> check)
    {
        foreach (int angle in new[] { 0, 90, 180, 270 })
        foreach (double column in new[] { -1.5, -.5, .5, 1.5 })
        {
            var wall = IntakeRules.Rotate(column, CaptureRules.WallCentreY, angle);
            check(CaptureRules.InContact(15, -30, angle, 15 + wall.X, -30 + wall.Y), "Each G4 orientation accepts its actual mouth contact");
            var back = IntakeRules.Rotate(column, -2, angle);
            check(!CaptureRules.InContact(15, -30, angle, 15 + back.X, -30 + back.Y), "G4 never acquires its backing walls");
            var far = IntakeRules.Rotate(column, 3, angle);
            check(!CaptureRules.InContact(15, -30, angle, 15 + far.X, -30 + far.Y), "No invisible forward extension");
        }
        check(!CaptureRules.InContact(0, 0, 45, 0, 2) && !CaptureRules.InContact(0, 0, 0, double.NaN, 2), "Invalid mounting/coordinates cannot establish contact");
        var ids = new[] { "A71C-long-one", "A71C-long-two", "A71C", "B2" };
        check(ids.Select(id => CaptureRules.Label(id, ids)).Distinct().Count() == ids.Length, "G4 labels extend through collisions and prefix IDs");
        var r = new CaptureRecord();
        foreach (string field in new[] { "g4", "chute", "processor", "console", "module", "ship", "owner", "target", "permission" }) r[field] = field + "-full-id";
        r["mount"] = "1.500/2.000/90.000"; r.Phase = CapturePhase.Bound;
        var maps = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>();
        var store = new ObjectStateStore(maps, CaptureRecord.StoreName, r["g4"], 1);
        check(r.Valid && store.TryWrite(r.Fields), "Exact mission bindings can be persisted without shortening addresses");
        store.Read(out var saved);
        check(CaptureRecord.Read(saved, out var read) && read["module"] == r["module"] && read["g4"] == r["g4"], "Reload preserves exact module and G4");
        r.Phase = CapturePhase.CapturePending;
        check(!r.Valid, "A pending attachment cannot be recorded without both anchor IDs and the wall");
        r["ownPort"] = "MP|exact-own"; r["targetPort"] = "MP|I|exact-target"; r["wall"] = "wall-full-id";
        check(r.Valid && store.TryWrite(r.Fields), "Write-ahead attachment identity is persistable");
        store.Read(out saved); CaptureRecord.Read(saved, out read);
        check(read.Phase == CapturePhase.CapturePending, "Reload preserves uncertain native commit, never converts it into a retry");
        r["target"] = r["ship"];
        check(!r.Valid, "A mission cannot target its own carrier");
        r["target"] = "target-full-id"; r["phase"] = "99";
        check(!r.Valid && !CaptureRecord.Read(r.Fields, out _), "Unknown numeric mission phases cannot acquire live permission");
    }
}
