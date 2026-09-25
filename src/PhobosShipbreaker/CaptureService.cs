using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ostranauts.Core.Models;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosAutoNav;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

// Selected-G4 capture milestone. No acquisition, processing permission or crew impersonation.
internal static class CaptureService
{
    private sealed class Session
    {
        internal CondOwner Grabber = null!;
        internal CaptureRecord Record = null!;
        internal string Notice = "";
    }
    private static readonly Dictionary<string, Session> sessions = new(StringComparer.Ordinal);
    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, CaptureRecord.StoreName, co.strID, 1);
    private static bool Save(Session s) => s.Record.Valid && Store(s.Grabber).TryWrite(s.Record.Fields);
    internal static bool Read(CondOwner co, out CaptureRecord r)
    {
        r = null!;
        return Store(co).Read(out var fields) == SavedStateStatus.Ready && CaptureRecord.Read(fields, out r) && r["g4"] == co.strID;
    }
    internal static CondOwner[] Consoles(Ship ship) => ship.GetCOs(null, false, false, true)
        .Where(c => c.ship == ship && c.HasCond("IsNavStation") && c.HasCond("IsInstalled") && IndustrialNavigation.ModuleId(c) != null)
        .OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    internal static string Label(CondOwner co) => CaptureRules.Label(co.strID,
        IndustryService.Discover(co.ship).Where(ProcessingService.IsGrabber).Select(c => c.strID));
    internal static string Describe(CondOwner co)
    {
        var pos = co.GetPos();
        string text = Text.Get("Capture.mount", Label(co), pos.x, pos.y, co.Item.TF.eulerAngles.z);
        if (!Read(co, out var r)) return text + "\n" + Text.Get("Capture.unbound");
        string phase = sessions.TryGetValue(co.strID, out var s) ? Text.Get("Capture.phase_" + r.Phase) : Text.Get("Capture.resume_required");
        return text + "\n" + Text.Get("Capture.binding", r["target"], r["console"], r["module"], r["processor"], phase) +
            "\n" + (s?.Notice ?? "") + "\n" + Text.Get("Capture.scope");
    }
    internal static string? BindingProblem(CondOwner g, CaptureRecord r, bool attached = false)
    {
        if (g.bDestroyed || g.ship?.strRegID != r["ship"] || CrewSim.coPlayer?.strID != r["owner"] ||
            CrewSim.coPlayer.ship != g.ship || CrewSim.system.GetShipOwner(r["ship"]) != r["owner"])
            return Text.Get("Capture.changed");
        var target = CrewSim.system.GetShipByRegID(r["target"]);
        var problem = CaptureGeometry.TargetProblem(g.ship, target);
        if (problem != null) return problem;
        if (!ProcessingService.CaptureIntake(g, out var chute, out var processor, out var intake)) return intake;
        if (chute!.strID != r["chute"] || processor!.strID != r["processor"]) return Text.Get("Capture.changed");
        if (!g.HasCond("IsPowered")) return Text.Get("Capture.changed");
        var nav = CollectorService.Resolve(r["console"]);
        if (nav == null || nav.ship != g.ship || !nav.HasCond("IsInstalled") || nav.HasCond("IsDamaged") ||
            !nav.HasCond("IsPowered") || !nav.GetCOsSafe(true).Any(c => c.strID == r["module"] && !c.bDestroyed && !c.HasCond("IsDamaged") &&
                (c.strCODef == "PhobosNavModAutoNav" || c.strCODef == "PhobosNavModPursuit"))) return Text.Get("Capture.changed");
        if (!attached && (g.ship.IsDocked() || g.ship.IsMoored() || target!.IsDocked() || target.IsMoored())) return Text.Get("Capture.attached");
        // Exact mounting geometry is captured, not silently replaced by another aligned assembly.
        if (Mount(g) != r["mount"]) return Text.Get("Capture.changed");
        return null;
    }
    private static string Mount(CondOwner g)
    {
        var p = g.GetPos();
        return string.Join("/", new[] { (double)(p.x - g.ship.vShipPos.x), p.y - g.ship.vShipPos.y, g.Item.TF.eulerAngles.z }
            .Select(v => v.ToString("F3", CultureInfo.InvariantCulture)));
    }
    internal static bool Command(ConsoleBinding? binding, CondOwner g, string action, string? value, out string message)
    {
        message = ProcessingService.AccessProblem(g, binding) ?? "";
        if (message.Length != 0) return false;
        try
        {
            if (action == "capture-bind") return Bind(g, value, out message);
            if (action == "capture-status") { message = Describe(g); return true; }
            if (!Read(g, out var record)) { message = Text.Get("Capture.unbound"); return false; }
            var s = sessions.TryGetValue(g.strID, out var previous) ? previous : new Session { Grabber = g, Record = record };
            switch (action)
            {
                case "capture-stop":
                    IndustrialNavigation.Release(record["permission"]);
                    // Preserve pending mutations and attachment IDs for reconciliation, never repeat them.
                    if (record.Phase == CapturePhase.Approaching) record.Phase = CapturePhase.Suspended;
                    s.Record = record; s.Notice = message = Text.Get("Capture.stopped"); sessions[g.strID] = s;
                    return Save(s);
                case "capture-release": return Release(s, out message);
                case "capture-start": return Start(s, out message);
                default: message = Text.Get("Industry.unsupported_action"); return false;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log(ex.ToString());
            if (Read(g, out var failed))
            {
                IndustrialNavigation.Release(failed["permission"]);
                if (failed.Phase == CapturePhase.Approaching)
                { failed.Phase = CapturePhase.Suspended; Store(g).TryWrite(failed.Fields); }
            }
            message = Text.Get("Capture.uncertain"); return false;
        }
    }
    private static bool Bind(CondOwner g, string? navId, out string message)
    {
        message = Text.Get("Capture.stop_rebind");
        var saved = Store(g).Read(out _);
        if (saved != SavedStateStatus.Missing && (saved != SavedStateStatus.Ready || !Read(g, out _)))
        { message = Text.Get("Capture.save"); return false; }
        if (Read(g, out var old) && old.Phase != CapturePhase.Bound && old.Phase != CapturePhase.Released && old.Phase != CapturePhase.Suspended) return false;
        if (g.ship.IsDocked() || g.ship.IsMoored()) return false;
        var nav = navId == null ? null : CollectorService.Resolve(navId);
        var target = GUIOrbitDraw.CrossHairTarget?.Ship;
        if (nav == null || !Consoles(g.ship).Contains(nav) || target == null) { message = Text.Get("Capture.select"); return false; }
        string? problem = CaptureGeometry.TargetProblem(g.ship, target);
        if (problem != null) { message = problem; return false; }
        if (!ProcessingService.CaptureIntake(g, out var chute, out var processor, out message)) return false;
        var r = new CaptureRecord();
        r["g4"] = g.strID; r["chute"] = chute!.strID; r["processor"] = processor!.strID;
        r["console"] = nav.strID; r["module"] = IndustrialNavigation.ModuleId(nav)!;
        r["target"] = target.strRegID; r["ship"] = g.ship.strRegID; r["owner"] = CrewSim.coPlayer.strID;
        r["permission"] = Guid.NewGuid().ToString("N"); r["mount"] = Mount(g); r.Phase = CapturePhase.Bound;
        var s = new Session { Grabber = g, Record = r };
        problem = BindingProblem(g, r);
        if (problem != null) { message = problem; return false; }
        if (!Save(s)) { message = Text.Get("Capture.save"); return false; }
        sessions[g.strID] = s; message = Text.Get("Capture.bound"); return true;
    }
    private static bool Start(Session s, out string message)
    {
        var r = s.Record; var g = s.Grabber;
        message = Text.Get("Capture.uncertain");
        if (r.Phase == CapturePhase.CapturePending || r.Phase == CapturePhase.ReleasePending)
            return Reconcile(s, out message);
        if (r.Phase == CapturePhase.Approaching && sessions.ContainsKey(g.strID)) return false;
        if (r.Phase == CapturePhase.Captured)
        {
            var target = CrewSim.system.GetShipByRegID(r["target"]);
            string? problem = BindingProblem(g, r, true);
            if (problem != null) { message = problem; return false; }
            if (!CaptureGeometry.ExactAttachment(g.ship, target, r) || !(r.Fields.ContainsKey("support")?ReclamationGeometry.Support(target,r):CaptureGeometry.Contact(g, target, r["wall"]))) return false;
            sessions[g.strID] = s; s.Notice = message = Text.Get("Capture.contact"); return true;
        }
        string? fault = BindingProblem(g, r);
        if (fault != null) { message = fault; return false; }
        if (!Plan(g, r, out _))
        { message = Text.Get("Capture.fit"); return false; }
        r.Phase = CapturePhase.Approaching;
        if (!Save(s)) { message = Text.Get("Capture.save"); return false; }
        var nav = CollectorService.Resolve(r["console"]);
        if (!IndustrialNavigation.Request(r["permission"], nav!, r["module"], r["target"],
            g.Item.TF.eulerAngles.z + g.ship.nGridRotation, () => BindingProblem(g, r), out message))
        { r.Phase = CapturePhase.Suspended; Save(s); return false; }
        sessions[g.strID] = s; s.Notice = message; return true;
    }
    internal static void Update()
    {
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || CrewSim.Paused) return;
        foreach (var s in sessions.Values.ToArray())
        {
            if (s.Record.Phase != CapturePhase.Approaching) continue;
            try
            {
                if (!IndustrialNavigation.Observe(s.Record["permission"], out bool ready, out var message))
                { s.Record.Phase = CapturePhase.Suspended; s.Notice = message; Save(s); continue; }
                s.Notice = message;
                if (ready) Capture(s);
            }
            catch (Exception ex)
            {
                Plugin.Log(ex.ToString()); IndustrialNavigation.Release(s.Record["permission"]);
                if (s.Record.Phase == CapturePhase.Approaching) s.Record.Phase = CapturePhase.Suspended;
                s.Notice = Text.Get("Capture.uncertain"); Save(s);
            }
        }
    }
    private static void Capture(Session s)
    {
        var r = s.Record; var own = s.Grabber.ship; var target = CrewSim.system.GetShipByRegID(r["target"]);
        if (!Plan(s.Grabber, r, out var plan))
        { IndustrialNavigation.Release(r["permission"]); r.Phase = CapturePhase.Suspended; s.Notice = Text.Get("Capture.fit"); Save(s); return; }
        // The write-ahead record precedes every native mutation, including anchor creation.
        r["wall"] = plan!.Wall;
        if (plan.Support.Length > 0) { r["support"] = plan.Support; r["floor"] = plan.Floor; } r["ownPort"] = "MP|" + DataHandler.GetNextID(); r["targetPort"] = "MP|I|" + DataHandler.GetNextID();
        r.Phase = CapturePhase.CapturePending;
        if (!Save(s)) { IndustrialNavigation.Release(r["permission"]); s.Notice = Text.Get("Capture.save"); return; }
        IndustrialNavigation.Release(r["permission"]);
        plan.OwnPort.strID = r["ownPort"]; plan.TargetPort.strID = r["targetPort"];
        CaptureGeometry.AddPort(own, plan.OwnPort); CaptureGeometry.AddPort(target, plan.TargetPort);
        CaptureGeometry.PreservingCrimeFlags = true;
        try { CrewSim.MoorShip(new DockingPortDTO(own, r["ownPort"]), new DockingPortDTO(target, r["targetPort"]), false); }
        finally { CaptureGeometry.PreservingCrimeFlags = false; }
        target = CrewSim.system.GetShipByRegID(r["target"]);
        if (!CaptureGeometry.ExactAttachment(own, target, r)) { s.Notice = Text.Get("Capture.uncertain"); return; }
        r.Phase = CapturePhase.Captured;
        s.Notice = Text.Get(CaptureGeometry.Contact(s.Grabber, target, r["wall"]) ? "Capture.contact" : "Capture.no_contact");
        if (!Save(s)) s.Notice = Text.Get("Capture.save");
    }
    private static bool Release(Session s, out string message)
    {
        var r = s.Record; var own = s.Grabber.ship; var target = CrewSim.system.GetShipByRegID(r["target"]);
        message = Text.Get("Capture.uncertain");
        if (target == null || !CaptureGeometry.ExactAttachment(own, target, r) || CrewSim.coPlayer?.ship != own ||
            CrewSim.coPlayer.strID != r["owner"] ||
            CrewSim.system.GetShipOwner(own.strRegID) != r["owner"] || target.People.Any() ||
            own.GetDockedShipsAndPortIDs().Count != 1 || target.GetDockedShipsAndPortIDs().Count != 1) return false;
        IndustrialNavigation.Release(r["permission"]);
        r.Phase = CapturePhase.ReleasePending;
        if (!Save(s)) { message = Text.Get("Capture.save"); return false; }
        CrewSim.UnMoorShip(own, target);
        if (own.IsDockedWith(target)) return false;
        r.Phase = CapturePhase.Released; r["mount"] = Mount(s.Grabber);
        s.Notice = message = Text.Get("Capture.released"); sessions[s.Grabber.strID] = s;
        return Save(s);
    }
    private static bool Reconcile(Session s, out string message)
    {
        var r = s.Record; var own = s.Grabber.ship; var target = CrewSim.system.GetShipByRegID(r["target"]);
        message = Text.Get("Capture.uncertain");
        if (own?.strRegID != r["ship"] || CrewSim.coPlayer?.strID != r["owner"] || CrewSim.coPlayer.ship != own ||
            CrewSim.system.GetShipOwner(own.strRegID) != r["owner"] || CaptureGeometry.TargetProblem(own, target) != null) return false;
        if (CaptureGeometry.ExactAttachment(own, target, r))
        {
            if (BindingProblem(s.Grabber, r, true) != null || !(r.Fields.ContainsKey("support")?ReclamationGeometry.Support(target,r):CaptureGeometry.Contact(s.Grabber, target, r["wall"]))) return false;
            r.Phase = CapturePhase.Captured; message = Text.Get("Capture.contact");
        }
        else
        {
            bool HasPort(Ship ship, string id) => ship.LoadState >= Ship.Loaded.Edit ?
                ship.GetMappedCos().Any(pair => pair.Key == id) : ship.json.aItems.Any(item => item != null && item.strID == id);
            if (own.IsDocked() || own.IsMoored() || target.IsDocked() || target.IsMoored() ||
                HasPort(own, r["ownPort"]) || HasPort(target, r["targetPort"])) return false;
            // A completed native release, or a save before either anchor was created, is observable.
            // Do not replay either mutation or erase a surviving orphan anchor.
            r.Phase = CapturePhase.Released; r["mount"] = Mount(s.Grabber); message = Text.Get("Capture.reconciled");
        }
        s.Notice = message; sessions[s.Grabber.strID] = s;
        return Save(s);
    }
    private static bool Plan(CondOwner g, CaptureRecord r, out CapturePlan? plan) => CaptureGeometry.TryPlan(g,
        CrewSim.system.GetShipByRegID(r["target"]),out plan,r.Fields.ContainsKey("workWall")?r["workWall"]:null,
        int.TryParse(r["outward"],out var angle)?angle:(int?)null,r.Fields.ContainsKey("workWall"));
    internal static bool SelectWork(CondOwner g,string wall,int outward,out string message)
    {
        message=Text.Get("Capture.uncertain");
        if(!Read(g,out var r) || g.ship.IsDocked() || g.ship.IsMoored()) return false;
        r["workWall"]=wall; r["outward"]=outward.ToString(CultureInfo.InvariantCulture);
        if(!Plan(g,r,out _)) { message=Text.Get("Capture.fit"); return false; }
        var s=new Session { Grabber=g,Record=r }; return Save(s) && Start(s,out message);
    }
    internal static bool MissionRelease(CondOwner g,out string message)
    {
        message=Text.Get("Capture.uncertain");
        return Read(g,out var r) && Release(new Session { Grabber=g,Record=r },out message);
    }
    internal static bool MissionReconcile(CondOwner g,out string message)
    {
        message=Text.Get("Capture.uncertain");
        return Read(g,out var r) && Reconcile(new Session { Grabber=g,Record=r },out message);
    }
    internal static void Reset() => sessions.Clear(); // Loading drops permissions; it never detaches ships.
    internal static void Shutdown()
    {
        foreach (var session in sessions.Values) IndustrialNavigation.Release(session.Record["permission"]);
        Reset();
    }
}
