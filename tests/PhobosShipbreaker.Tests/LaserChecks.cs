using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;

/// <summary>The ML-2 mining laser's rules and saved sweep: paid work, the write-ahead pending phase, what a reload may
/// and may not do, admission by damage points, the filter, and the room-heat budget at its heat share.</summary>
internal static class LaserChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        LaserRecord Bound()
        {
            var bound = new LaserRecord();
            bound.Bind("ship-reg", "laser-id", LaserRules.Mount(3.5, -2, 90), "rock-reg", "MP|port-id");
            return bound;
        }
        void Pay(LaserRecord record) => record.Credit(record.Number("kw") * (record.Number("seconds") - record.Number("progress")) / 3600);

        var r = Bound();
        check(r.Valid && r.Phase == LaserPhase.Seeking && !r.HasJob && r.Number("cursor") == -LaserRules.ArcDegrees / 2 && r.Number("rock") == 0 && r.Number("walls") == 0,
            "A fresh sweep is bound to one moored ship and starts at the arc's near edge with nothing counted");

        // One paid job on one rock stage.
        r.Begin("rock-1", LaserRules.Rock, "ItmWallRock011x1", 15, -12.5);
        check(r.Valid && r.Phase == LaserPhase.Working && r.Number("seconds") == 15 && r.Number("kw") == LaserRules.WorkingKW,
            "A rock job captures its duration from the damage points left and the working draw");
        r.Credit(LaserRules.WorkingKW * 6 / 3600 * .5);
        check(Math.Abs(r.Number("progress") - 3) < 1e-9 && !r.Paid, "Half the power for six seconds earns three seconds of work");
        check(LaserRecord.Read(r.Fields, out var restored) && Math.Abs(restored.Number("progress") - 3) < 1e-9 && restored.Phase == LaserPhase.Working,
            "A reload keeps paid work and the job, without any permission to fire");
        throws(() => r.Commit(), "An unpaid job cannot announce its change");
        throws(() => r.Begin("rock-2", LaserRules.Rock, "ItmWallRock011x1", 15, 0), "One job at a time");
        r.Credit(LaserRules.WorkingKW * 60 / 3600.0);
        check(r.Paid && r.Number("progress") == 15, "Work is capped at the job, never banked for the next one");
        r.Commit();
        check(r.Valid && r.Phase == LaserPhase.DamagePending, "A paid rock job writes its pending phase before the damage is applied");
        throws(() => r.Credit(.1), "A pending change cannot earn or repeat work");
        throws(() => r.Commit(), "A pending change cannot be announced twice");
        r.Settle(false);
        check(r.Valid && r.Phase == LaserPhase.Seeking && !r.HasJob && r.Number("rock") == 0 && r.Number("cursor") == -12.5,
            "A rock that only reached its damaged form is not counted, and the sweep resumes from its bearing");
        throws(() => r.Settle(true), "A settled change cannot be settled again");
        r.Begin("rock-1", LaserRules.Rock, "ItmWallRock011x1Dmg", 30, -12.5); Pay(r); r.Commit(); r.Settle(true);
        check(r.Valid && r.Number("rock") == 1 && r.Number("walls") == 0, "A rock that is gone counts once");

        // A hull panel uses the uninstall phase and its own duration.
        r.Begin("wall-1", LaserRules.Wall, "ItmWall1x1", 0, 4);
        check(r.Number("seconds") == LaserRules.WallSeconds && Math.Abs(LaserRules.WallSeconds * LaserRules.WorkingKW / 3600 - .4) < 1e-9,
            "A wall panel costs its authored time at the working draw: 0.4 kWh");
        Pay(r); r.Commit();
        check(r.Valid && r.Phase == LaserPhase.UninstallPending, "A paid panel job announces the uninstall, not damage");
        r.Settle(true);
        check(r.Number("walls") == 1 && r.Number("rock") == 1 && r.Number("cursor") == 4, "A freed panel counts once and moves the sweep on");

        // Giving a job up loses its work and nothing else; a new target keeps the counts and restarts the sweep.
        r.Begin("rock-3", LaserRules.Rock, "ItmWallRock021x1", 15, 20); r.Credit(LaserRules.WorkingKW * 5 / 3600.0); r.Drop();
        check(r.Valid && !r.HasJob && r.Phase == LaserPhase.Seeking && r.Number("rock") == 1 && r.Number("walls") == 1 && r.Number("cursor") == 4, "A dropped job is not counted and does not move the sweep");
        r.Bind("ship-reg", "laser-id", LaserRules.Mount(3.5, -2, 90), "hull-reg", "MP|other");
        check(r.Valid && r.Number("cursor") == -30 && r.Number("rock") == 1 && r.Number("walls") == 1 && r["target"] == "hull-reg", "A new mooring starts a fresh sweep and keeps the totals");

        // Records that do not add up are refused whole.
        LaserRecord Broken(Action<LaserRecord> change) { var broken = Bound(); change(broken); return broken; }
        check(!Broken(b => b.Fields.Remove("port")).Valid && !Broken(b => b["target"] = "ship-reg").Valid && !Broken(b => b["phase"] = "Working").Valid &&
            !Broken(b => b.Number("rock", 1.5)).Valid && !Broken(b => b.Number("cursor", double.NaN)).Valid && !Broken(b => b["phase"] = "4").Valid,
            "A missing binding, a ship moored to itself, a phase without its job, a fractional count and an unknown cursor are invalid");
        check(!Broken(b => { b.Begin("x", LaserRules.Rock, "stage", 15, 0); b["kind"] = "ore"; }).Valid &&
            !Broken(b => { b.Begin("x", LaserRules.Rock, "stage", 15, 0); b["phase"] = LaserPhase.DamagePending.ToString(); }).Valid &&
            !Broken(b => { b.Begin("x", LaserRules.Wall, "stage", 0, 0); b.Number("progress", LaserRules.WallSeconds); b["phase"] = LaserPhase.DamagePending.ToString(); }).Valid &&
            !Broken(b => { b.Begin("x", LaserRules.Rock, "stage", 15, 0); b.Number("progress", 16); }).Valid &&
            !Broken(b => { b["object"] = "x"; }).Valid,
            "An unknown kind, an unpaid pending change, the wrong pending phase for the kind, overcredited work and a job left on a seeking sweep are invalid");

        // Admission and choices.
        check(LaserRules.AdmitRock(15) && LaserRules.AdmitRock(115) && LaserRules.AdmitRock(LaserRules.MaximumPoints) && !LaserRules.AdmitRock(LaserRules.MaximumPoints + .1) &&
            !LaserRules.AdmitRock(296) && !LaserRules.AdmitRock(0) && !LaserRules.AdmitRock(double.NaN), "Rock walls and intact cores are taken; rubble, spent and unknown stages are left");
        check(LaserRules.RockSeconds(45) == 45 && Math.Abs(45 * LaserRules.WorkingKW / 3600 - .3) < 1e-9, "A whole rock wall, both stages, is 45 s and 0.3 kWh at the working draw");
        check(LaserRules.ParseFilter("rock", out var filter) && filter == LaserFilter.Rock && LaserRules.ParseFilter("walls", out filter) && filter == LaserFilter.Walls &&
            LaserRules.ParseFilter("both", out filter) && filter == LaserFilter.Both && !LaserRules.ParseFilter("Rock", out _) && !LaserRules.ParseFilter(null, out _) &&
            new[] { LaserFilter.Rock, LaserFilter.Walls, LaserFilter.Both }.All(f => LaserRules.ParseFilter(LaserRules.FilterId(f), out var back) && back == f),
            "Filter ids are the three exact lower-case names and round trip");
        check(new[] { LaserFilter.Rock, LaserFilter.Walls, LaserFilter.Both }.All(f => LaserRules.Takes(f, true, LaserRules.Rock) && !LaserRules.Takes(f, true, LaserRules.Wall)),
            "An asteroid is rock whatever the filter says");
        check(LaserRules.Takes(LaserFilter.Rock, false, LaserRules.Rock) && !LaserRules.Takes(LaserFilter.Rock, false, LaserRules.Wall) &&
            !LaserRules.Takes(LaserFilter.Walls, false, LaserRules.Rock) && LaserRules.Takes(LaserFilter.Walls, false, LaserRules.Wall) &&
            LaserRules.Takes(LaserFilter.Both, false, LaserRules.Rock) && LaserRules.Takes(LaserFilter.Both, false, LaserRules.Wall) && !LaserRules.Takes(LaserFilter.Both, false, "ore"),
            "On a hull the filter decides, and nothing else is ever a job");
        check(LaserRules.Mount(1.23456, -2, 270) == "1.235/-2.000/270.000" && LaserRules.Mount(1.2351, -2, 270) != LaserRules.Mount(1.2351, -2, 180), "The mount fingerprint fixes position and turn");

        // Heat: the declared share goes to the room behind the mount under the shared air-cooled rule.
        double heat = LaserRules.HeatKW(LaserRules.WorkingKW);
        check(Math.Abs(heat - 14.4) < 1e-9, "Sixty percent of the 24 kW working draw warms the room");
        check(!RoomHeat.Budget(1000, 295, 0, 0, heat, 1, out _) && !RoomHeat.Budget(1000, 295, 0, 9.9, heat, 1, out _), "Vacuum or thin air behind the mount is not cooling");
        check(!RoomHeat.Budget(1000, 313.14, 0, 100, heat, 1, out _), "A room at the ceiling takes no more laser heat");
        check(RoomHeat.Budget(800, 293.15, 0, 100, heat, 1, out double rise) && Math.Abs(rise - 14.4 * 1000 / (800 * RoomHeat.GasHeatCapacityJPerMolK)) < 1e-9 && rise > .8 && rise < .9,
            "About 800 mol of room air warms a little under 0.9 K for each second of cutting");

        // Presentation constants the art and the game's frame animation depend on.
        check(LaserRules.SheetFrames <= LaserRules.SheetColumns * LaserRules.SheetRows && int.TryParse(LaserRules.SheetFrameRate, out int fps) && fps > 0 && !LaserRules.SheetFrameRate.Contains("-"),
            "The firing sheet has a cell for every frame and one fixed rate");
        check(LaserRules.EmitterPixelsX == 0 && LaserRules.EmitterPixelsY == LaserRules.Footprint * LaserRules.PixelsPerTile / 2.0, "The beam leaves the middle of the head's outer edge");

        // Wiring: the console group, the economy list and the assembled name.
        check(IndustrialRules.Group(LaserRules.Installed) == "laser" && IndustrialRules.Group(LaserRules.Installed + "Dmg") == "laser" && IndustrialRules.Equipment(LaserRules.Installed),
            "The laser is listed as equipment under its own console group");
        check(ShipbreakerEconomy.Machines.Any(m => m.Prefix == LaserRules.Prefix && m.MassKg == LaserRules.MachineKg) && ShipbreakerEconomy.MassOf(LaserRules.Prefix) == LaserRules.MachineKg &&
            ShipbreakerEconomy.Price(LaserRules.Prefix) > ShipbreakerEconomy.Price(IntakeRules.Grabber), "The economy pack prices the laser, above the G4, with salvage weighed against its mass");
        check(PhobosShipbreaker.Text.Get("Laser.name") == "Phobos’ Ablatine ML-2 Mining Laser" || PhobosShipbreaker.Text.Get("Laser.name") == "Phobos' Ablatine ML-2 Mining Laser",
            "The full name carries the Phobos prefix, the Ablatine brand and the ML-2 model");
    }
}
