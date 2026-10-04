using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Health;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;
using PhobosMedical.Core;
using UnityEngine;

namespace PhobosMedical;

/// <summary>The Ward-3 bed. Every couple of seconds it finds who lies in it (the saved patient, or anyone asleep,
/// unconscious or resting at its sleep point), and while it is installed, intact, powered and in a pressurised room
/// it keeps the game's Recuperating figures on that patient: the game's own medical sleep for a sleeping or
/// unconscious patient, the bed's own Recovering for one resting awake. When power or air fails the care is withdrawn
/// and the player is told once; it returns when they do. The bed mirrors its power into the game's <c>IsOff</c>, so an
/// unpowered Ward-3 is an ordinary bed to the game's own sleep chain, as the Infirmaway's Off form is.</summary>
internal static class BedService
{
    private sealed class Session
    {
        internal BedState State = new();
        internal bool Protected;
        internal double LastTick = double.NaN;
        internal CareReason Reason = CareReason.NoPatient;
    }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    /// <summary>Patients a bed claimed on its last tick, so the care sweep never withdraws care a bed still gives.</summary>
    private static readonly HashSet<string> claimed = new(StringComparer.Ordinal);
    private static int passes;
    internal static void Reset() { sessions = new(); claimed.Clear(); passes = 0; }
    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, MedicalRules.Record, Plugin.Id, 1);
    private static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co, out var found)) return found;
        var s = new Session();
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Ready) { try { s.State = BedState.Read(fields); } catch (Exception e) { s.Protected = true; Plugin.Log(e.ToString()); } }
        else if (status != SavedStateStatus.Missing) s.Protected = true;
        sessions.Add(co, s);
        return s;
    }
    private static bool Save(CondOwner co, Session s)
    {
        if (Store(co).TryWriteIfChanged(s.State.Save())) return true;
        s.Protected = true; return false;
    }
    internal static BedState StateOf(CondOwner co) => Get(co).State;
    internal static bool Protected(CondOwner co) => Get(co).Protected;

    internal static bool Intact(CondOwner bed) => Content.Ready && bed.strCODef == MedicalRules.BedInstalled && bed.HasCond("IsInstalled") && !bed.HasCond("IsDamaged");

    /// <summary>A living person of the bed's ship lying at its sleep point who is asleep, unconscious or resting.</summary>
    internal static bool Present(CondOwner bed, CondOwner? person)
    {
        if (person == null || person.bDestroyed || bed.ship == null || person.ship != bed.ship || person.HasCond("IsDead")) return false;
        if (!person.HasCond("Unconscious") && !person.HasCond(MedicalRules.Resting)) return false;
        Vector2 at = bed.GetPos(MedicalRules.SleepPoint);
        return Vector2.Distance(at, (Vector2)person.tf.position) <= MedicalRules.PatientReach;
    }

    /// <summary>The patient in the bed: the saved one while still there, otherwise the first person found lying there.</summary>
    internal static CondOwner? Patient(CondOwner bed)
    {
        var s = Get(bed);
        var saved = CrewWork.Resolve(s.State.Patient);
        if (Present(bed, saved)) return saved;
        if (bed.ship == null) return null;
        foreach (var person in bed.ship.GetPeople(false))
            if (Present(bed, person)) return person;
        return null;
    }
    /// <summary>Someone other than <paramref name="actor"/> is lying in the bed.</summary>
    internal static bool OccupiedByOther(CondOwner bed, CondOwner? actor)
    {
        var patient = Patient(bed);
        return patient != null && patient != actor;
    }

    internal static BedRoute RouteOf(CondOwner bed, CondOwner person) =>
        BedRules.Route(person.HasCond(MedicalRules.Resting), person.HasCond("Unconscious"), person.GetInteractionCurrent()?.objThem == bed);

    /// <summary>One bed step, from the plugin's short timer. The first step after a load only reads the bed.</summary>
    internal static void Tick(CondOwner bed)
    {
        var s = Get(bed);
        bool first = double.IsNaN(s.LastTick);
        s.LastTick = StarSystem.fEpoch;
        if (s.Protected) return;
        bool intact = Intact(bed), powered = intact && bed.HasCond("IsPowered");
        if (intact)
        {
            if (powered && bed.HasCond("IsOff")) bed.ZeroCondAmount("IsOff");
            else if (!powered && !bed.HasCond("IsOff")) bed.SetCondAmount("IsOff", 1);
        }
        var patient = intact ? Patient(bed) : null;
        var previous = CrewWork.Resolve(s.State.Patient);
        if (previous != null && previous != patient) Withdraw(previous);
        if (patient == null) { s.State.Patient = ""; s.State.Route = BedRoute.None; }
        else
        {
            if (s.State.Patient != patient.strID) s.State.Since = StarSystem.fEpoch;
            s.State.Patient = patient.strID; s.State.Route = RouteOf(bed, patient);
            claimed.Add(patient.strID);
        }
        if (first) { Save(bed, s); return; }
        var air = RoomHeat.Read(bed, MedicalRules.SleepPoint);
        bool pressurised = air != null && air.PressureKPa >= RoomHeat.MinPressureKPa;
        s.Reason = BedRules.Decide(patient != null, intact, powered, pressurised);
        if (s.Reason == CareReason.Caring)
        {
            Grant(patient!, s.State.Route);
            SetInUse(bed, true);
            s.State.OutageNoticed = false;
        }
        else
        {
            if (patient != null) Withdraw(patient);
            SetInUse(bed, false);
            if (patient != null && (s.Reason == CareReason.NoPower || s.Reason == CareReason.NoAir) && !s.State.OutageNoticed)
            {
                s.State.OutageNoticed = true;
                PlayerNotices.Post(bed.ship, "PhobosMedical.bed", NoticeLevel.Caution,
                    Text.Get(s.Reason == CareReason.NoPower ? "Bed.notice_power" : "Bed.notice_air", ObjectPresentation.Name(patient), ObjectPresentation.Name(bed)));
            }
        }
        if (patient != null && s.State.Route == BedRoute.Resting && PatientFacts.Read(patient) is { } facts && BedRules.Recovered(facts, Care.Admission)
            && !patient.HasCond(MedicalRules.Rested))
            patient.AddCondAmount(MedicalRules.Rested, 1);
        // A laid patient has no loop of their own on the bed; keep it marked occupied, as the game's sleep loop does.
        if (patient != null && s.State.Route == BedRoute.Laid) bed.AddCondAmount("IsOccupied", 1);
        Save(bed, s);
    }

    private static void SetInUse(CondOwner bed, bool on)
    {
        if (on && !bed.HasCond(MedicalRules.InUse)) bed.AddCondAmount(MedicalRules.InUse, 1);
        else if (!on && bed.HasCond(MedicalRules.InUse)) bed.ZeroCondAmount(MedicalRules.InUse);
    }
    /// <summary>Puts the route's care condition on the patient (and takes the other off), marked as the bed's.</summary>
    private static void Grant(CondOwner patient, BedRoute route)
    {
        string want = BedRules.CareCondition(route), other = want == MedicalRules.Recovering ? MedicalRules.SleepingMedical : MedicalRules.Recovering;
        if (!patient.HasCond(want)) patient.AddCondAmount(want, 1);
        if (other == MedicalRules.Recovering && patient.HasCond(other)) patient.ZeroCondAmount(other);
        if (!patient.HasCond(MedicalRules.CareMark)) patient.AddCondAmount(MedicalRules.CareMark, 1);
    }
    /// <summary>Takes the bed's care off a person: its own Recovering, and the game's medical sleep where the bed gave it.</summary>
    internal static void Withdraw(CondOwner person)
    {
        if (person == null || person.bDestroyed) return;
        if (person.HasCond(MedicalRules.Recovering)) person.ZeroCondAmount(MedicalRules.Recovering);
        if (person.HasCond(MedicalRules.CareMark))
        {
            if (person.HasCond(MedicalRules.SleepingMedical)) person.ZeroCondAmount(MedicalRules.SleepingMedical);
            person.ZeroCondAmount(MedicalRules.CareMark);
        }
    }

    /// <summary>After each pass over the beds. Every few passes (never in the first two after a load), care no bed
    /// claimed is withdrawn: a bed destroyed, uninstalled or unloaded under its patient leaves nothing behind.</summary>
    internal static void AfterPass()
    {
        passes++;
        if (passes < 3 || passes % 5 != 0) { claimed.Clear(); return; }
        try
        {
            foreach (var ship in CrewSim.system.GetAllLoadedShips())
                foreach (var person in ship.GetPeople(false))
                    if (person != null && person.HasCond(MedicalRules.CareMark) && !claimed.Contains(person.strID)) Withdraw(person);
        }
        finally { claimed.Clear(); }
    }

    // ---- Offer gate and actions ----------------------------------------------------------------------------------

    /// <summary>Why <paramref name="action"/> is not offered on this bed now, or null when it is.</summary>
    internal static string? Refusal(string action, CondOwner? actor, CondOwner bed)
    {
        if (!Content.Ready) return Text.Get("Content.loading");
        if (Protected(bed)) return Text.Get("Bed.protected");
        if (OccupiedByOther(bed, actor)) return Text.Get("Bed.occupied");
        var facts = PatientFacts.Read(actor);
        bool injured = facts != null && BedRules.Injured(facts, Care.Admission);
        switch (action)
        {
            case MedicalRules.Lay:
                return PatientPlacement.CanPlace(actor) switch
                {
                    PlacementFailure.None => Intact(bed) ? null : Text.Get("Bed.repair_first"),
                    PlacementFailure.Dead => Text.Get("Bed.lay_dead"),
                    PlacementFailure.NotUnconscious => Text.Get("Bed.lay_awake"),
                    _ => Text.Get("Bed.lay_nobody")
                };
            case MedicalRules.Rest:
                return injured ? null : Text.Get("Bed.rest_uninjured");
            case MedicalRules.Sleep:
                return Get(bed).State.Reserved && !injured ? Text.Get("Bed.reserved") : null;
            default:
                return null;
        }
    }

    /// <summary>Lay patient here: the selected crew member puts down the unconscious person they are dragging.</summary>
    internal static bool Lay(CondOwner bed, CondOwner actor, out string message)
    {
        var refusal = Refusal(MedicalRules.Lay, actor, bed);
        if (refusal != null) { message = refusal; return false; }
        var person = PatientPlacement.Place(actor, bed, MedicalRules.SleepPoint, out var failure);
        if (person == null) { message = Text.Get("Bed.lay_failed", failure); return false; }
        var s = Get(bed);
        s.State.Patient = person.strID; s.State.Route = BedRoute.Laid; s.State.Since = StarSystem.fEpoch;
        Save(bed, s);
        message = Text.Get("Bed.laid", ObjectPresentation.Name(person), ObjectPresentation.Name(bed));
        return true;
    }

    // ---- Presentation and commands -----------------------------------------------------------------------------

    internal static EquipmentState State(CondOwner bed)
    {
        var s = Get(bed);
        if (s.Protected || bed.HasCond("IsDamaged")) return EquipmentState.Blocked;
        return s.Reason switch
        {
            CareReason.Caring => EquipmentState.Running,
            CareReason.NoPatient => bed.HasCond("IsPowered") ? EquipmentState.Ready : EquipmentState.Waiting,
            _ => EquipmentState.Waiting
        };
    }

    internal static string Describe(CondOwner bed)
    {
        var s = Get(bed);
        if (s.Protected) return Text.Get("Bed.protected");
        var lines = new List<string>();
        var patient = CrewWork.Resolve(s.State.Patient);
        if (patient == null) lines.Add(Text.Get(Intact(bed) ? "Bed.empty" : "Bed.repair_first"));
        else
        {
            double hours = Math.Max(0, (StarSystem.fEpoch - s.State.Since) / Units.SecondsPerHour);
            lines.Add(Text.Get("Bed.patient", ObjectPresentation.Name(patient), Text.Get("Bed.route_" + s.State.Route), hours));
            lines.Add(Text.Get("Bed.care_" + s.Reason));
            if (PatientFacts.Read(patient) is { } f)
                lines.Add(Text.Get("Bed.facts", f.BloodLost, f.Infection, f.Pain, f.WorstWound * 100, f.Bleeding, f.UnsplintedFractures));
        }
        var air = RoomHeat.Read(bed, MedicalRules.SleepPoint);
        lines.Add(air == null ? Text.Get("Bed.room_vacuum") : Text.Get("Bed.room", air.PressureKPa));
        lines.Add(bed.HasCond("IsPowered") ? Text.Get("Content.powered") : Text.Get("Content.no_power"));
        lines.Add(Text.Get(s.State.Reserved ? "Bed.use_injured" : "Bed.use_anyone"));
        return string.Join("\n", lines);
    }

    internal static string? MaintenanceReason(CondOwner bed)
    {
        if (!MedicalRules.IsBed(bed.strCODef)) return null;
        if (Get(bed).Protected) return Text.Get("Maintenance.protected");
        return Patient(bed) != null ? Text.Get("Maintenance.occupied") : null;
    }

    internal static bool Command(CondOwner bed, ConsoleBinding? binding, string action, out string message)
    {
        message = Content.Ready ? Content.Access(bed, binding) ?? "" : Text.Get("Content.loading");
        if (message.Length > 0) return false;
        var s = Get(bed);
        if (action == "status") { message = Describe(bed); return true; }
        if (action == "accept")
        {
            var status = Store(bed).Read(out var fields);
            BedState? state = null;
            try { state = status == SavedStateStatus.Ready ? BedState.Read(fields) : status == SavedStateStatus.Missing ? new BedState() : null; } catch (Exception e) { Plugin.Log(e.ToString()); }
            if (state == null) { Store(bed).Clear(); state = new BedState(); }
            s.State = state; s.Protected = false;
            bool ok = Save(bed, s); message = Text.Get(ok ? "Bed.accept_done" : "Bed.protected"); return ok;
        }
        if (s.Protected) { message = Text.Get("Bed.protected"); return false; }
        switch (action)
        {
            case "use:injured": s.State.Reserved = true; message = Text.Get("Bed.use_injured"); break;
            case "use:anyone": s.State.Reserved = false; message = Text.Get("Bed.use_anyone"); break;
            default: message = Text.Get("Content.unsupported_action"); return false;
        }
        if (!Save(bed, s)) { message = Text.Get("Bed.protected"); return false; }
        return true;
    }
}
