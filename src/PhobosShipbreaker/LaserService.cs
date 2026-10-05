using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Hazards;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>The ML-2 mining laser. After an explicit Start it sweeps its arc over the one ship moored to ours, one
/// paid job at a time: electricity is measured and credited as work, and when a job is paid the service writes the
/// pending phase and then makes exactly one native change (the game's own damage on rock, the game's own uninstall
/// on a hull panel). It then only watches for the game to finish. Permission to fire is transient: a reload, a
/// moved head, a changed mooring or a fault suspends it, and only Start resumes. What falls stays where it falls.</summary>
internal static partial class LaserService
{
    private sealed class Session
    {
        internal CondOwner Laser = null!;
        internal LaserRecord Record = null!;
        internal bool Authorized, Demand, CrewHold, Presenting;
        internal double NextUpdate, PendingSince;
        internal int Remaining = -1;
        internal Vector3? Aim;
        internal LaserGeometry.Anchors? Anchors;
        // The paired cooling assembly as last checked by the one-second step; power steps recheck only its room.
        internal CondOwner? Radiator;
        internal string Notice = "";
        // Crew jobs painted since Start (not saved: the tasks themselves are the game's).
        internal int HaulJobs, DepositJobs;
        internal bool ZoneWarned;
    }
    internal const string FilterStoreName = "Shipbreaker.LaserFilter";
    private static readonly Dictionary<string, Session> sessions = new(StringComparer.Ordinal);
    internal static int SessionCount => sessions.Count;
    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, LaserRecord.StoreName, co.strID, 1);
    private static ObjectStateStore FilterStore(CondOwner co) => new(co.mapGUIPropMaps, FilterStoreName, co.strID, 1);
    private static bool Save(Session s) => s.Record.Valid && Store(s.Laser).TryWriteIfChanged(s.Record.Fields);
    private static bool Read(CondOwner co, out LaserRecord r)
    { r = null!; return Store(co).Read(out var fields) == SavedStateStatus.Ready && LaserRecord.Read(fields, out r) && r["laser"] == co.strID; }

    /// <summary>What this head is set to cut; rock until the player chooses otherwise.</summary>
    internal static LaserFilter Filter(CondOwner co) =>
        FilterStore(co).Read(out var fields) == SavedStateStatus.Ready && fields.TryGetValue("filter", out var id) && LaserRules.ParseFilter(id, out var filter) ? filter : LaserFilter.Rock;
    internal static bool SetFilter(CondOwner co, ConsoleBinding? binding, string id, out string message)
    {
        message = ProcessingService.AccessProblem(co, binding) ?? "";
        if (message.Length != 0) return false;
        if (!LaserRules.ParseFilter(id, out var filter)) { message = Text.Get("Industry.unsupported_action"); return false; }
        if (sessions.TryGetValue(co.strID, out var s) && s.Authorized && s.Record.HasJob) { message = Text.Get("Laser.filter_busy"); return false; }
        if (!WriteChoice(co, "filter", LaserRules.FilterId(filter))) { message = Text.Get("Laser.save"); return false; }
        message = Text.Get("Laser.filter_set", Text.Get("Laser.filter_" + LaserRules.FilterId(filter)));
        return true;
    }

    internal static string? MachineProblem(CondOwner co)
    {
        if (!Content.Ready) return Content.Status;
        if (co == null || co.bDestroyed) return Text.Get("Laser.install_first");
        if (co.HasCond("IsDamaged")) return Text.Get("Laser.repair_first");
        if (co.strCODef != LaserRules.Installed || !co.HasCond("IsInstalled")) return Text.Get("Laser.install_first");
        if (co.ship == null || (int)co.ship.LoadState < 2 || co.Item == null) return Text.Get("ProcessingService.ship_is_not_loaded");
        if (co.HasCond("IsOverrideOff") || co.HasCond("IsSignalOff")) return Text.Get("Laser.switched_off");
        return null;
    }

    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Text.Get("Laser.fault");
        if (!Content.Ready) { message = Content.Status; return false; }
        if (action.StartsWith("filter:", StringComparison.Ordinal)) return SetFilter(co, binding, action.Substring(7), out message);
        if (action.StartsWith("cooling:", StringComparison.Ordinal)) return SetCooling(co, binding, action.Substring(8), out message);
        if (action.StartsWith("power:", StringComparison.Ordinal)) return SetPower(co, binding, action.Substring(6), out message);
        if (action.StartsWith("haul:", StringComparison.Ordinal)) return SetJobs(co, binding, LaserRules.HaulJobsKey, action.Substring(5), out message);
        if (action.StartsWith("deposits:", StringComparison.Ordinal)) return SetJobs(co, binding, LaserRules.DepositJobsKey, action.Substring(9), out message);
        if (action == "status") { message = Describe(co); return true; }
        message = ProcessingService.AccessProblem(co, binding) ?? "";
        if (message.Length != 0) return false;
        try
        {
            switch (action)
            {
                case "start": return Start(co, out message);
                case "pause": Pause(co, false); message = Describe(co); return true;
                case "stop": Pause(co, true); message = Describe(co); return true;
                default: message = Text.Get("Industry.unsupported_action"); return false;
            }
        }
        catch (Exception ex) { Fault(co, ex); message = Text.Get("Laser.fault"); return false; }
    }

    private static bool Start(CondOwner co, out string message)
    {
        string? problem = MachineProblem(co);
        if (problem != null) { message = problem; return false; }
        if (sessions.TryGetValue(co.strID, out var running) && running.Authorized) { message = Describe(co); return true; }
        problem = LaserGeometry.TargetProblem(co, out var target, out string port);
        if (problem != null || target == null) { message = problem ?? Text.Get("Laser.no_attachment"); return false; }
        if (!LaserGeometry.Facing(co, out _)) { message = Text.Get("Laser.geometry"); return false; }
        var r = new LaserRecord();
        var status = Store(co).Read(out var fields);
        // A record this version cannot read is left exactly as it is.
        if (status != SavedStateStatus.Missing && (status != SavedStateStatus.Ready || !LaserRecord.Read(fields, out r) || r["laser"] != co.strID))
        { message = Text.Get("Laser.save"); return false; }
        string mount = LaserGeometry.Mount(co);
        bool fresh = status == SavedStateStatus.Missing || r["ship"] != co.ship.strRegID || r["mount"] != mount || r["target"] != target.strRegID ||
            r["port"] != port || r.Phase == LaserPhase.Exhausted;
        if (fresh) r.Bind(co.ship.strRegID, co.strID, mount, target.strRegID, port);
        else if (r.HasJob) Reconcile(co, target, r);
        var s = new Session { Laser = co, Record = r, Authorized = true, Notice = Text.Get("Laser.seeking") };
        sessions[co.strID] = s;
        if (!Save(s)) { sessions.Remove(co.strID); message = Text.Get("Laser.save"); return false; }
        Step(s);
        message = Describe(co);
        // The saved mark of a running laser (Shipbreaker 0.77.0): a reload carries its work on.
        Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Mark(co, s.Authorized);
        return s.Authorized;
    }
    /// <summary>A saved job is settled by what can be seen, never by repeating its change: a pending change that
    /// happened is counted, one that did not is given up and paid for again; unpaid work on an object that is still
    /// there carries on.</summary>
    private static void Reconcile(CondOwner co, Ship target, LaserRecord r)
    {
        var found = ReclamationGeometry.Resolve(target, r["object"]);
        bool changed = found == null || found.strCODef != r["stage"];
        if (r.Phase == LaserPhase.Working) { if (changed) r.Drop(); return; }
        if (!changed) { r.Drop(); return; }
        bool finished = Finished(r, found);
        if (finished) QueueJobs(co, null, target, r, found, null);
        r.Settle(finished);
    }
    private static bool Finished(LaserRecord r, CondOwner? found) => r["kind"] == LaserRules.Wall || found == null || !LaserGeometry.Mineable(found);

    private static void Pause(CondOwner co, bool stop)
    {
        if (sessions.TryGetValue(co.strID, out var s))
        {
            if (stop && s.Record.Phase == LaserPhase.Working) s.Record.Drop();
            Suspend(s, Text.Get(stop ? "Laser.stopped" : "Laser.paused"));
            return;
        }
        if (!stop || !Read(co, out var saved) || saved.Phase != LaserPhase.Working) return;
        saved.Drop();
        Store(co).TryWrite(saved.Fields);
    }

    private static string? Problem(Session s, out Ship? target)
    {
        target = null;
        var co = s.Laser; var r = s.Record;
        string? problem = MachineProblem(co);
        if (problem != null) return problem;
        if (r["ship"] != co.ship.strRegID || r["mount"] != LaserGeometry.Mount(co)) return Text.Get("Laser.moved");
        problem = LaserGeometry.TargetProblem(co, out target, out string port);
        if (problem != null || target == null) return problem ?? Text.Get("Laser.no_attachment");
        return target.strRegID != r["target"] || port != r["port"] ? Text.Get("Laser.attachment_changed") : null;
    }

    internal static void Update()
    {
        if (sessions.Count == 0 || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || CrewSim.Paused) return;
        // Nothing is copied on a frame with no laser at work and none to forget.
        bool due = false;
        foreach (var session in sessions.Values) if (session.Authorized || session.Laser == null || session.Laser.bDestroyed) { due = true; break; }
        if (!due) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Laser);
        foreach (var pair in sessions.ToArray())
        {
            var s = pair.Value;
            if (s.Laser == null || s.Laser.bDestroyed) { sessions.Remove(pair.Key); continue; }
            if (!s.Authorized || StarSystem.fEpoch < s.NextUpdate) continue;
            s.NextUpdate = StarSystem.fEpoch + 1;
            try { Step(s); } catch (Exception ex) { Fault(s.Laser, ex); }
        }
    }

    private static void Step(Session s)
    {
        string? problem = Problem(s, out var target);
        if (problem != null || target == null) { Suspend(s, problem ?? Text.Get("Laser.no_attachment")); return; }
        if (target.bCheckRooms) { s.Notice = Text.Get("Laser.geometry"); return; }
        s.Radiator = Radiator(s.Laser, out _);
        var filter = Filter(s.Laser);
        switch (s.Record.Phase)
        {
            case LaserPhase.Exhausted: Suspend(s, Text.Get("Laser.exhausted")); return;
            case LaserPhase.DamagePending:
            case LaserPhase.UninstallPending: Settle(s, target); return;
            case LaserPhase.Working: Work(s, target, filter); return;
            default: Seek(s, target, filter); return;
        }
    }

    private static void Seek(Session s, Ship target, LaserFilter filter)
    {
        var co = s.Laser; var r = s.Record;
        var candidates = LaserGeometry.Candidates(co, target, filter, r.Number("cursor"), out bool ownHull, out var anchors);
        s.Anchors = anchors; s.Remaining = candidates.Count; s.Aim = null;
        if (candidates.Count == 0)
        {
            r.Phase = LaserPhase.Exhausted;
            PlayerNotices.Post(co.ship, "shipbreaker.laser.done." + co.strID, NoticeLevel.Info, Text.Get("Laser.exhausted_log", co.strNameFriendly));
            Suspend(s, Text.Get(ownHull ? "Laser.blocked_by_own_hull" : "Laser.exhausted"));
            return;
        }
        var next = candidates[0];
        // The job captures its draw now: the high setting only while a paired cooling assembly is ready.
        r.Begin(next.Object.strID, next.Kind, next.Object.strCODef, next.Points, next.Bearing, LaserRules.JobKW(HighPower(co), s.Radiator != null));
        s.Aim = next.Object.GetPos(); s.Notice = Text.Get("Laser.cutting");
        if (!Save(s)) Suspend(s, Text.Get("Laser.save"));
    }

    private static void Work(Session s, Ship target, LaserFilter filter)
    {
        var co = s.Laser; var r = s.Record;
        var found = ReclamationGeometry.Resolve(target, r["object"]);
        s.Anchors ??= LaserGeometry.AnchorsOf(co.ship, target, target.GetCOs(null, false, false, true));
        if (found == null || found.strCODef != r["stage"] || !LaserRules.Takes(filter, LaserGeometry.IsAsteroid(target), r["kind"]) ||
            !LaserGeometry.Admit(co, target, found, r["kind"], s.Anchors, true, out _, out _))
        {
            // Not a stop: the sweep carries on with whatever is next. Work paid on the lost object is gone.
            r.Drop(); s.Aim = null; s.Notice = Text.Get("Laser.target_changed");
            if (!Save(s)) Suspend(s, Text.Get("Laser.save"));
            return;
        }
        var aim = found.GetPos(); s.Aim = aim;
        var person = LaserGeometry.PersonNearBeam(co, aim);
        if (person != null)
        {
            if (!s.CrewHold)
                PlayerNotices.Post(co.ship, "shipbreaker.laser.crew." + co.strID, NoticeLevel.Caution,
                    Text.Get("Laser.crew_in_beam_log", person.strNameFriendly), Text.Get("Laser.crew_in_beam_banner"));
            s.CrewHold = true; s.Notice = Text.Get("Laser.crew_in_beam");
            return;
        }
        s.CrewHold = false;
        if (!r.Paid) return;
        // Write the pending phase first, then make the one native change this job paid for.
        r.Commit(); s.PendingSince = StarSystem.fEpoch;
        if (!Save(s)) { Suspend(s, Text.Get("Laser.save")); return; }
        if (r["kind"] == LaserRules.Rock) NativeDamage.Apply(found, NativeDamage.Left(found));
        else
        {
            found.SetCondAmount("StatUninstallProgress", found.GetCondAmount("StatUninstallProgressMax"));
            found.GetComponent<Destructable>().ScheduleDamageCheck();
        }
        if (Plugin.Options.LaserEffects) NativeDamage.Trail(LaserGeometry.Emitter(co), aim);
        s.Notice = Text.Get("Laser.native_wait");
    }

    private static void Settle(Session s, Ship target)
    {
        var r = s.Record;
        var found = ReclamationGeometry.Resolve(target, r["object"]);
        if (found == null || found.strCODef != r["stage"])
        {
            bool wall = r["kind"] == LaserRules.Wall, finished = Finished(r, found);
            if (finished) QueueJobs(s.Laser, s, target, r, found, s.Aim);
            r.Settle(finished); s.Aim = null; s.PendingSince = 0;
            s.Notice = Text.Get(wall ? "Laser.panel_freed" : finished ? "Laser.rock_broken" : "Laser.rock_cracked");
            if (!Save(s)) Suspend(s, Text.Get("Laser.save"));
            return;
        }
        if (s.PendingSince == 0) s.PendingSince = StarSystem.fEpoch;
        if (StarSystem.fEpoch - s.PendingSince > LaserRules.PendingTimeoutSeconds) Suspend(s, Text.Get("Laser.uncertain"));
        else s.Notice = Text.Get("Laser.native_wait");
    }

    private static void Present(Session s, bool firing)
    {
        if (!firing && !s.Presenting) return;
        s.Presenting = firing;
        LaserPresentation.Refresh(s.Laser, firing, firing ? s.Aim : null);
    }
    private static void Suspend(Session s, string reason, bool leavingWorld = false)
    {
        s.Authorized = s.Demand = s.CrewHold = false; s.Notice = reason; s.Aim = null;
        try
        {
            // A stop in play ends the player's Start; the world being put away for a load does not.
            if (!leavingWorld) Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Mark(s.Laser, false);
            if (s.Laser != null && !s.Laser.bDestroyed) { s.Laser.ZeroCondAmount(LaserRules.Working); Present(s, false); }
            Save(s);
        }
        catch (Exception ex) { Plugin.Log(ex.Message); }
    }
    internal static void Fault(CondOwner co, Exception ex)
    {
        Plugin.Log(ex.ToString());
        if (co != null && sessions.TryGetValue(co.strID, out var s)) Suspend(s, Text.Get("Laser.fault"));
    }
    internal static void Reset()
    {
        foreach (var s in sessions.Values.ToArray()) if (s.Authorized) Suspend(s, Text.Get("Laser.paused"), leavingWorld: true);
        sessions.Clear();
        LaserGeometry.Reset();
        LaserPresentation.Reset();
    }

    /// <summary>Before the native power step: the working draw is asked for only while a paid-for job is still
    /// unpaid, its target is in view and nobody stands by the beam.</summary>
    internal static void PreparePower(CondOwner co)
    {
        // A laser that was cutting when the game was saved starts again once after the load (Shipbreaker 0.77.0; owner
        // decision, 5 October 2026), through Start's own checks: mount, attachment and facing. A saved cut is settled
        // by what can be seen, as any Start settles it.
        if ((!sessions.TryGetValue(co.strID, out var s) || !s.Authorized) && Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Due(co))
        {
            if (!Start(co, out string resumed)) { Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Mark(co, false); Plugin.Log(Text.Get("Content.resume_failed", co.strNameFriendly, resumed)); }
            sessions.TryGetValue(co.strID, out s);
        }
        if (s == null) { if (co.HasCond(LaserRules.Working)) co.ZeroCondAmount(LaserRules.Working); return; }
        s.Demand = s.Authorized && s.Record.Phase == LaserPhase.Working && !s.Record.Paid && !s.CrewHold && s.Aim.HasValue;
        if (s.Demand) co.SetCondAmount(LaserRules.Working, 1);
        else { co.ZeroCondAmount(LaserRules.Working); Present(s, false); }
    }

    internal static EquipmentState State(CondOwner co)
    {
        if (co.HasCond("IsDamaged")) return EquipmentState.Blocked;
        if (!sessions.TryGetValue(co.strID, out var s) || !s.Authorized) return EquipmentState.Paused;
        return co.HasCond(LaserRules.Working) && co.HasCond("IsPowered") ? EquipmentState.Running : EquipmentState.Waiting;
    }

    private static string JobsLine(CondOwner co, Session? s) =>
        Text.Get("Laser.jobs_line", SwitchLabel(HaulJobs(co)), SwitchLabel(DepositJobs(co)), s?.HaulJobs ?? 0, s?.DepositJobs ?? 0);
    internal static string Describe(CondOwner co)
    {
        string filter = Text.Get("Laser.filter_" + LaserRules.FilterId(Filter(co)));
        bool cooled = Radiator(co, out _) != null, high = HighPower(co);
        double kw = LaserRules.JobKW(high, cooled);
        string demand = Text.Get(cooled ? "Laser.demand_radiator" : "Laser.demand", kw, Phobos.Ostranauts.Framework.Processing.RoomHeat.Machine(LaserRules.HeatKW(kw)), LaserRules.ArcDegrees, LaserRules.RangeTiles) +
            "\n" + Text.Get("Laser.power_line", PowerLabel(high)) + "\n" + CoolingStatus(co);
        sessions.TryGetValue(co.strID, out var s);
        if (!Read(co, out var r))
            return (s != null && s.Notice.Length > 0 ? s.Notice + "\n" : "") + Text.Get("Laser.help", filter) + "\n" + JobsLine(co, s) + "\n" + demand;
        string remaining = s != null && s.Remaining >= 0 ? s.Remaining.ToString(CultureInfo.InvariantCulture) : Text.Get("Laser.unknown");
        return Text.Get("Laser.status", s != null ? s.Notice : Text.Get("Laser.resume_required"), Text.Get("Laser.phase_" + r.Phase), filter,
                r.Number("rock"), r.Number("walls"), remaining) +
            (r.HasJob ? "\n" + Text.Get("Laser.work", Text.Get("Laser.kind_" + r["kind"]), r.Number("progress"), r.Number("seconds"), r.Number("kw")) : "") +
            "\n" + JobsLine(co, s) + "\n" + demand;
    }
}
