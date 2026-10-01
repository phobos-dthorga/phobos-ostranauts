using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Hazards;
using Phobos.Ostranauts.Framework.Observations;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>What the ML-2 can see and reach. Reach exists only on the one ship the game has moored to ours: a moored
/// pair shares one deck space, so the arc is measured in deck tiles from the emitter on the head's outer edge. Rock
/// is whatever the game's own mining action accepts; a hull's wall is the ordinary panel the G4 also frees. A
/// candidate must be the first solid thing along its line, and never behind our own hull.</summary>
internal static class LaserGeometry
{
    /// <summary>The game's own test for its Mine action: mineable, destructible and not an opened ore deposit.</summary>
    internal const string MineRule = "TIsMineableDestructableNotDeposit";
    internal const string MooringPort = "MooringPort";
    /// <summary>The game's gangue items. Rock or ice whose breaking can leave nothing else (bare rock floor, ice
    /// floor, a core already reduced to rubble) is not worth the power and is left for a crew member who wants it.</summary>
    internal static readonly HashSet<string> Gangue = new(StringComparer.Ordinal) { "ItmMiningTrash", "ItmIceTrash01" };
    private static readonly Dictionary<string, bool> gangueOnly = new(StringComparer.Ordinal);
    internal static void Reset() => gangueOnly.Clear();
    private static IEnumerable<string> Names(Loot loot) => (loot.aCOs ?? Array.Empty<string>()).Concat(loot.aLoots ?? Array.Empty<string>())
        .SelectMany(entry => entry.Split('|')).Select(choice => choice.Split('=')[0]).Where(name => name.Length > 0);
    /// <summary>Follows a damage threshold's loot to the action it queues and the items that action turns the object
    /// into, in the game's own tables: true only when every possible result is gangue.</summary>
    internal static bool GangueOnly(string damageLoot)
    {
        if (DataHandler.dictLoot == null || DataHandler.dictInteractions == null || !DataHandler.dictLoot.TryGetValue(damageLoot, out var trigger) || trigger.strType != "interaction") return false;
        var results = new List<string>();
        foreach (string action in Names(trigger))
        {
            if (!DataHandler.dictInteractions.TryGetValue(action, out var interaction) || string.IsNullOrEmpty(interaction.objLootModeSwitch) ||
                !DataHandler.dictLoot.TryGetValue(interaction.objLootModeSwitch, out var output)) return false;
            results.AddRange(Names(output));
        }
        return results.Count > 0 && results.All(Gangue.Contains);
    }
    internal static bool GangueOnly(CondOwner co)
    {
        string? loot = co.GetComponent<Destructable>()?.GetDmgLoot(NativeDamage.Stat);
        if (string.IsNullOrEmpty(loot)) return false;
        if (!gangueOnly.TryGetValue(loot!, out bool known)) gangueOnly[loot!] = known = GangueOnly(loot!);
        return known;
    }

    internal sealed class Candidate
    {
        internal CondOwner Object = null!;
        internal string Kind = "";
        internal double Bearing, Distance, Points;
    }

    internal static Vector3 Emitter(CondOwner laser) => laser.GetPos("emit");
    /// <summary>The head's outward direction in degrees, read from where its emitter actually sits.</summary>
    internal static bool Facing(CondOwner laser, out double degrees)
    {
        var centre = laser.GetPos(); var emitter = Emitter(laser);
        double dx = emitter.x - centre.x, dy = emitter.y - centre.y;
        degrees = BeamGeometry.Facing(dx, dy);
        return dx * dx + dy * dy > 0.01;
    }
    internal static string Mount(CondOwner laser)
    {
        var p = laser.GetPos();
        return LaserRules.Mount(p.x - laser.ship.vShipPos.x, p.y - laser.ship.vShipPos.y, laser.Item.TF.eulerAngles.z);
    }
    internal static bool IsAsteroid(Ship target) => target.Classification == Ship.TypeClassification.Asteroid;

    /// <summary>The one moored ship, when the laser may work on it: any tethered asteroid; a hull only on the G4's
    /// terms (ours, nobody aboard, not a station) and with no air left in it.</summary>
    internal static string? TargetProblem(CondOwner laser, out Ship? target, out string port)
    {
        target = null; port = "";
        var own = laser.ship;
        if (own == null || !AttachedTiles.Moored(own, out target, out port) || target == null) { target = null; return Text.Get("Laser.no_attachment"); }
        if (target.LoadState < Ship.Loaded.Edit || target.aRooms == null) return Text.Get("Laser.geometry");
        if (IsAsteroid(target)) return null;
        string? problem = CaptureGeometry.TargetProblem(own, target);
        if (problem != null) return problem;
        if (target.bCheckRooms) return null; // The caller waits for the game to rebuild the rooms.
        foreach (var room in target.aRooms.Where(r => !r.Void))
        {
            double pressure = room.CO?.GetCondAmount("StatGasPressure") ?? double.NaN;
            if (!ReclamationRules.Finite(pressure) || pressure < 0 || pressure > ReclamationRules.MaximumTargetPressureKPa) return Text.Get("Reclamation.pressure");
        }
        return null;
    }

    internal static CondTrigger? MineTrigger() => NativeDefinitions.Trigger(MineRule);
    internal static bool Mineable(CondOwner co, CondTrigger? rule = null) => (rule ?? MineTrigger())?.Triggered(co) == true;

    /// <summary>An ordinary installed wall panel the game's own uninstall will free: the G4's rule without its jaw
    /// reach, exposure and floor requirements, which the laser's line of sight replaces.</summary>
    internal static bool Wall(Ship target, CondOwner wall, bool started = false)
    {
        var rule = NativeDefinitions.Trigger(ReclamationGeometry.VanillaWallRule);
        if (rule == null || !ReclamationGeometry.NativeWallContract() || wall.ship != target || wall.bDestroyed || wall.objCOParent != null || wall.Item == null ||
            !WallIdentity.IsInstalledOrdinary(wall.strCODef) || !wall.HasCond("IsInstalled") || !rule.Triggered(wall)) return false;
        if (!ProcessRules.AcceptedWallKg(wall.GetTotalMass()) || wall.GetCOsSafe(true).Count != 0 || wall.coStackHead != null || wall.aStack.Count != 0) return false;
        if (!started && wall.GetCondAmount("StatUninstallProgress") != 0) return false;
        var onTile = new List<CondOwner>(); target.GetCOsAtWorldCoords1(wall.GetPos(), null, false, true, onTile);
        if (onTile.Any(c => c != wall && !c.bDestroyed && !c.HasCond("IsFloor"))) return false;
        return wall.GetCondAmount("StatUninstallProgressMax") > 0 &&
            wall.GetComponent<Destructable>()?.GetDmgLoot("StatUninstallProgress") == "MSWall1x1Uninstall";
    }

    /// <summary>What the laser never touches: the wall, floor and port a G4 on our ship holds this hull by, and
    /// anything beside a mooring anchor, so the attachment it depends on is never cut away.</summary>
    internal sealed class Anchors
    {
        internal readonly HashSet<string> Ids = new(StringComparer.Ordinal);
        internal readonly List<Vector3> Ports = new();
        internal bool Protects(CondOwner co)
        {
            if (Ids.Contains(co.strID)) return true;
            var p = co.GetPos();
            foreach (var port in Ports)
                if (Math.Max(Math.Abs(p.x - port.x), Math.Abs(p.y - port.y)) <= LaserRules.AnchorClearanceTiles) return true;
            return false;
        }
    }
    internal static Anchors AnchorsOf(Ship own, Ship target, IEnumerable<CondOwner> targetObjects)
    {
        var anchors = new Anchors();
        foreach (var grabber in ShipEquipment.Read(own, ProcessingService.IsGrabber))
            if (CaptureService.Read(grabber, out var capture) && capture["target"] == target.strRegID)
                foreach (string key in new[] { "support", "floor", "targetPort" }) if (capture[key].Length > 0) anchors.Ids.Add(capture[key]);
        foreach (var co in targetObjects) if (co != null && !co.bDestroyed && co.strCODef == MooringPort) anchors.Ports.Add(co.GetPos());
        foreach (var co in own.GetCOs(null, false, false, true)) if (co != null && !co.bDestroyed && co.strCODef == MooringPort) anchors.Ports.Add(co.GetPos());
        return anchors;
    }

    /// <summary>Whether the object is still a job of this kind, in the arc, clear of anchors and first on its line.</summary>
    internal static bool Admit(CondOwner laser, Ship target, CondOwner co, string kind, Anchors anchors, bool started, out Candidate? candidate, out bool ownHull)
    {
        candidate = null; ownHull = false;
        if (co == null || co.bDestroyed || co.ship != target || co.objCOParent != null || co.Item == null || !Facing(laser, out double facing)) return false;
        var emitter = Emitter(laser); var p = co.GetPos();
        if (!BeamGeometry.Sector(emitter.x, emitter.y, facing, p.x, p.y, LaserRules.ArcDegrees, LaserRules.RangeTiles, out double bearing, out double distance)) return false;
        double points = 0;
        if (kind == LaserRules.Rock)
        {
            if (!Mineable(co) || GangueOnly(co)) return false;
            points = NativeDamage.Left(co);
            if (!LaserRules.AdmitRock(points)) return false;
        }
        else if (kind != LaserRules.Wall || !Wall(target, co, started)) return false;
        if (anchors.Protects(co)) return false;
        var hit = AttachedTiles.FirstSolid(laser.ship, target, emitter, p, LaserRules.StepTiles, out var at);
        if (hit == BeamHit.Own) { ownHull = true; return false; }
        if (hit == BeamHit.Other && !Covers(co, at)) return false;
        candidate = new Candidate { Object = co, Kind = kind, Bearing = bearing, Distance = distance, Points = points };
        return true;
    }
    private static bool Covers(CondOwner co, Vector2 at)
    {
        var p = co.GetPos();
        double half = Math.Max(co.Item.nWidthInTiles, co.Item.nHeightInTiles) / 2.0 + IntakeRules.PositionToleranceTiles;
        return Math.Abs(at.x - p.x) <= half && Math.Abs(at.y - p.y) <= half;
    }

    /// <summary>Everything in the arc the filter asks for, in sweep order from the cursor.</summary>
    internal static List<Candidate> Candidates(CondOwner laser, Ship target, LaserFilter filter, double cursor, out bool ownHull, out Anchors anchors)
    {
        ownHull = false;
        bool asteroid = IsAsteroid(target), rock = LaserRules.Takes(filter, asteroid, LaserRules.Rock), walls = LaserRules.Takes(filter, asteroid, LaserRules.Wall);
        var objects = target.GetCOs(null, false, false, true);
        anchors = AnchorsOf(laser.ship, target, objects);
        var mine = rock ? MineTrigger() : null;
        var found = new List<Candidate>();
        foreach (var co in objects)
        {
            if (co == null || co.bDestroyed || co.objCOParent != null || co.Item == null) continue;
            string? kind = mine != null && Mineable(co, mine) ? LaserRules.Rock : walls && WallIdentity.IsInstalledOrdinary(co.strCODef) ? LaserRules.Wall : null;
            if (kind == null) continue;
            if (Admit(laser, target, co, kind, anchors, false, out var candidate, out bool blocked)) found.Add(candidate!);
            ownHull |= blocked;
        }
        return BeamGeometry.SweepOrder(found, c => c.Bearing, c => c.Distance, c => c.Object.strID, cursor).ToList();
    }

    /// <summary>Someone standing within a tile of the beam's path, on either ship.</summary>
    internal static CondOwner? PersonNearBeam(CondOwner laser, Vector3 target)
    {
        var emitter = Emitter(laser);
        foreach (var person in AttachedTiles.People(laser.ship))
        {
            var p = person.GetPos();
            if (BeamGeometry.DistanceToSegment(p.x, p.y, emitter.x, emitter.y, target.x, target.y) <= LaserRules.CrewClearanceTiles) return person;
        }
        return null;
    }
}
