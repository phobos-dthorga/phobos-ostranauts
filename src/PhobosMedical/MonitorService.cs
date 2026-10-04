using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Health;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Observations;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosMedical.Core;

namespace PhobosMedical;

/// <summary>The Vigil-2 patient monitor (Medical 0.3.0). While installed, intact and powered it watches the patient of
/// the Ward-3 it touches (the one chosen on its panel, or the only or first one touching it), reads the game's own
/// health figures, keeps an hour's trend in memory and posts one caution to the crew log when a figure rises to its
/// alert threshold or a wound starts bleeding. It only reads: it never heals, treats or moves the patient. A reading
/// it cannot take is reported as unavailable, never as zero.</summary>
internal static class MonitorService
{
    private sealed class Session
    {
        internal MonitorState State = new();
        internal bool Protected;
        internal string Patient = "";
        internal readonly List<MonitorSample> Samples = new();
        internal readonly HashSet<MonitorAlert> Announced = new();
    }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    internal static void Reset() => sessions = new();
    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, MedicalRules.MonitorRecord, Plugin.Id, 1);
    private static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co, out var found)) return found;
        var s = new Session();
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Ready) { try { s.State = MonitorState.Read(fields); } catch (Exception e) { s.Protected = true; Plugin.Log(e.ToString()); } }
        else if (status != SavedStateStatus.Missing) s.Protected = true;
        sessions.Add(co, s);
        return s;
    }
    private static bool Save(CondOwner co, Session s)
    {
        if (Store(co).TryWriteIfChanged(s.State.Save())) return true;
        s.Protected = true; return false;
    }
    internal static MonitorState StateOf(CondOwner co) => Get(co).State;
    internal static bool Protected(CondOwner co) => Get(co).Protected;
    private static bool Working(CondOwner monitor) => Content.Ready && monitor.strCODef == MedicalRules.MonitorInstalled && monitor.HasCond("IsInstalled") &&
        !monitor.HasCond("IsDamaged") && monitor.HasCond("IsPowered");

    /// <summary>Installed Ward-3 beds touching the monitor, in a stable order.</summary>
    internal static IEnumerable<CondOwner> Beds(CondOwner monitor) =>
        monitor.ship == null ? Enumerable.Empty<CondOwner>() :
        ShipEquipment.Read(monitor.ship, c => c.strCODef == MedicalRules.BedInstalled && Footprints.Touching(monitor, c)).OrderBy(c => c.strID, StringComparer.Ordinal);

    /// <summary>The bed the monitor watches: its saved choice while that still touches it, otherwise the first touching.</summary>
    internal static CondOwner? Bed(CondOwner monitor)
    {
        var beds = Beds(monitor).ToList();
        string chosen = Get(monitor).State.Bed;
        return beds.FirstOrDefault(b => b.strID == chosen) ?? beds.FirstOrDefault();
    }

    internal static void Tick(CondOwner monitor)
    {
        var s = Get(monitor);
        if (s.Protected) return;
        var bed = Working(monitor) ? Bed(monitor) : null;
        var patient = bed != null ? BedService.Patient(bed) : null;
        bool watching = patient != null;
        if (watching && !monitor.HasCond(MedicalRules.Watching)) monitor.AddCondAmount(MedicalRules.Watching, 1);
        else if (!watching && monitor.HasCond(MedicalRules.Watching)) monitor.ZeroCondAmount(MedicalRules.Watching);
        if (patient == null) { s.Patient = ""; s.Samples.Clear(); s.Announced.Clear(); return; }
        if (s.Patient != patient.strID) { s.Patient = patient.strID; s.Samples.Clear(); s.Announced.Clear(); }
        var facts = PatientFacts.Read(patient);
        if (facts == null) return;
        MonitorRules.Record(s.Samples, new MonitorSample(StarSystem.fEpoch, facts));
        var fired = MonitorRules.Fire(facts, Care.Alerts, s.Announced);
        if (fired.Count > 0 && s.State.Alerts)
        {
            string what = string.Join(", ", fired.Select(a => Text.Get("Monitor.alert_" + a)));
            PlayerNotices.Post(monitor.ship, "PhobosMedical.monitor", NoticeLevel.Caution, Text.Get("Monitor.alert", ObjectPresentation.Name(monitor), ObjectPresentation.Name(patient), what),
                Text.Get("Monitor.alert_banner", ObjectPresentation.Name(patient), what));
        }
    }

    internal static EquipmentState State(CondOwner monitor)
    {
        var s = Get(monitor);
        if (s.Protected || monitor.HasCond("IsDamaged")) return EquipmentState.Blocked;
        if (!Working(monitor)) return EquipmentState.Waiting;
        return s.Patient.Length > 0 ? EquipmentState.Running : EquipmentState.Ready;
    }

    internal static string Describe(CondOwner monitor)
    {
        var s = Get(monitor);
        if (s.Protected) return Text.Get("Monitor.protected");
        if (monitor.HasCond("IsDamaged")) return Text.Get("Monitor.repair_first");
        if (!Working(monitor)) return Text.Get("Monitor.no_power");
        var bed = Bed(monitor);
        if (bed == null) return Text.Get("Monitor.no_bed");
        var patient = BedService.Patient(bed);
        var lines = new List<string> { Text.Get("Monitor.watching", ObjectPresentation.Name(bed)) };
        if (patient == null) { lines.Add(Text.Get("Monitor.no_patient")); return string.Join("\n", lines); }
        var facts = PatientFacts.Read(patient);
        if (facts == null) { lines.Add(Text.Get("Monitor.unavailable")); return string.Join("\n", lines); }
        lines.Add(Text.Get("Monitor.patient", ObjectPresentation.Name(patient), Text.Get(facts.Unconscious ? "Monitor.unconscious" : "Monitor.conscious")));
        var trend = MonitorRules.Trend(s.Samples, new MonitorSample(StarSystem.fEpoch, facts));
        string Change(double delta, string unit) => trend == null ? Text.Get("Monitor.trend_wait") : Text.Get("Monitor.change", delta, unit);
        lines.Add(Text.Get("Monitor.blood", facts.BloodLost, Change(trend?.Change.BloodLost ?? 0, "")));
        lines.Add(Text.Get("Monitor.infection", facts.Infection, Change(trend?.Change.Infection ?? 0, "")));
        lines.Add(Text.Get("Monitor.pain", facts.Pain, Change(trend?.Change.Pain ?? 0, "")));
        lines.Add(Text.Get("Monitor.wound", facts.WorstWound * 100, Change((trend?.Change.WorstWound ?? 0) * 100, "%")));
        if (trend != null) lines.Add(Text.Get("Monitor.trend_span", trend.Value.Hours * 60));
        var wounds = facts.Wounds.Where(w => w.Worst >= 0.01).OrderByDescending(w => w.Worst).ToList();
        if (wounds.Count == 0) lines.Add(Text.Get("Monitor.no_wounds"));
        foreach (var w in wounds)
            lines.Add(Text.Get("Monitor.wound_line", Text.Has("Wound." + w.Part) ? Text.Get("Wound." + w.Part) : w.Part, w.Cut * 100, w.Blunt * 100,
                string.Join(", ", new[] { w.Bleeding ? Text.Get("Monitor.tag_bleeding") : null, w.Staunched ? Text.Get("Monitor.tag_dressed") : null,
                    w.Fractured ? Text.Get(w.Splinted ? "Monitor.tag_splinted" : "Monitor.tag_fracture") : null, w.InfectionRate > 0 ? Text.Get("Monitor.tag_infected") : null }
                    .Where(t => t != null))));
        lines.Add(Text.Get(s.State.Alerts ? "Monitor.alerts_on" : "Monitor.alerts_off"));
        return string.Join("\n", lines);
    }

    internal static bool Command(CondOwner monitor, ConsoleBinding? binding, string action, out string message)
    {
        message = Content.Ready ? Content.Access(monitor, binding) ?? "" : Text.Get("Content.loading");
        if (message.Length > 0) return false;
        var s = Get(monitor);
        if (action == "status") { message = Describe(monitor); return true; }
        if (action == "accept")
        {
            var status = Store(monitor).Read(out var fields);
            MonitorState? state = null;
            try { state = status == SavedStateStatus.Ready ? MonitorState.Read(fields) : status == SavedStateStatus.Missing ? new MonitorState() : null; } catch (Exception e) { Plugin.Log(e.ToString()); }
            if (state == null) { Store(monitor).Clear(); state = new MonitorState(); }
            s.State = state; s.Protected = false;
            bool ok = Save(monitor, s); message = Text.Get(ok ? "Monitor.accept_done" : "Monitor.protected"); return ok;
        }
        if (s.Protected) { message = Text.Get("Monitor.protected"); return false; }
        string[] parts = action.Split(new[] { ':' }, 2);
        switch (parts[0])
        {
            case "alerts" when parts.Length == 2 && (parts[1] == "on" || parts[1] == "off"):
                s.State.Alerts = parts[1] == "on"; message = Text.Get(s.State.Alerts ? "Monitor.alerts_on" : "Monitor.alerts_off"); break;
            case "bed" when parts.Length == 2:
                if (parts[1] != "auto" && !Beds(monitor).Any(b => b.strID == parts[1])) { message = Text.Get("Monitor.bed_missing"); return false; }
                s.State.Bed = parts[1] == "auto" ? "" : parts[1]; message = Text.Get("Monitor.bed_set"); break;
            default: message = Text.Get("Content.unsupported_action"); return false;
        }
        if (!Save(monitor, s)) { message = Text.Get("Monitor.protected"); return false; }
        return true;
    }
}
