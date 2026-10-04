using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Health;

namespace PhobosMedical.Core;

/// <summary>How a patient came to be in the bed.</summary>
public enum BedRoute { None, Resting, Asleep, Laid }

/// <summary>Why the bed is or is not giving care.</summary>
public enum CareReason { NoPatient, Caring, Damaged, NoPower, NoAir }

/// <summary>The Ward-3's decisions, kept apart from the game objects so they can be checked offline.</summary>
public static class BedRules
{
    /// <summary>Whether the bed gives care, and why not.</summary>
    public static CareReason Decide(bool patient, bool intact, bool powered, bool pressurised)
    {
        if (!patient) return CareReason.NoPatient;
        if (!intact) return CareReason.Damaged;
        if (!powered) return CareReason.NoPower;
        if (!pressurised) return CareReason.NoAir;
        return CareReason.Caring;
    }

    /// <summary>The condition that carries the game's Recuperating figures for this route: a resting patient is awake,
    /// so it is the bed's own <see cref="MedicalRules.Recovering"/>; a sleeping or unconscious patient gets the game's
    /// own medical sleep, which its time skip and its wake already handle.</summary>
    public static string CareCondition(BedRoute route) => route == BedRoute.Resting ? MedicalRules.Recovering : MedicalRules.SleepingMedical;

    /// <summary>The route a person in the bed is on: resting (our mark), asleep in this bed (the game's sleep chain
    /// targets it), or laid there unconscious (a knocked-out person's loop targets the ship). Anyone else is not a patient.</summary>
    public static BedRoute Route(bool resting, bool unconscious, bool sleepingInThisBed)
    {
        if (resting && !unconscious) return BedRoute.Resting;
        if (unconscious) return sleepingInThisBed ? BedRoute.Asleep : BedRoute.Laid;
        return BedRoute.None;
    }

    /// <summary>Injured: any figure at its threshold, a wound bleeding, or a fracture without a splint.</summary>
    public static bool Injured(PatientFacts f, AdmissionEntry a) =>
        f.Bleeding > 0 || f.UnsplintedFractures > 0 || f.BloodLost >= a.bloodLost || f.Infection >= a.infection || f.Pain >= a.pain || f.WorstWound >= a.wound;

    /// <summary>Recovered enough to get up: nothing bleeding or unsplinted, every figure under its threshold times the
    /// discharge share. The gap between this and <see cref="Injured"/> keeps a patient from bouncing in and out.</summary>
    public static bool Recovered(PatientFacts f, AdmissionEntry a)
    {
        double k = a.dischargeShare;
        return f.Bleeding == 0 && f.UnsplintedFractures == 0 && f.BloodLost < a.bloodLost * k && f.Infection < a.infection * k && f.Pain < a.pain * k && f.WorstWound < a.wound * k;
    }

    /// <summary>How badly off someone is, for choosing whom to send to a free bed first: each figure as a share of the
    /// game's fatal or knock-out level, plus the worst wound, and half a point for each bleeding wound.</summary>
    public static double Severity(PatientFacts f) =>
        f.BloodLost / CareSchema.FatalBloodLost + f.Infection / CareSchema.FatalInfection + f.Pain / CareSchema.KnockoutPain + f.WorstWound + 0.5 * f.Bleeding + 0.25 * f.UnsplintedFractures;
}

/// <summary>The Ward-3's saved record: who is in it and how, since when, and its one setting.</summary>
public sealed class BedState
{
    public string Patient { get; set; } = "";
    public BedRoute Route { get; set; }
    public double Since { get; set; }
    /// <summary>Only injured crew may use the bed (off by default: any tired crew member may sleep in a free Ward-3).</summary>
    public bool Reserved { get; set; }
    /// <summary>A power or air outage has been announced; cleared when care resumes, so each outage is told once.</summary>
    public bool OutageNoticed { get; set; }
    /// <summary>Send injured crew here (Medical 0.2.0): while the bed is free and working, the worst-off injured crew
    /// member not under the player's direct control is given Rest and recover.</summary>
    public bool SendInjured { get; set; }
    /// <summary>Fields of a record saved by Medical 0.1.0 (no send setting), and the current count.</summary>
    public const int LegacyFields = 5, Fields = 6;

    /// <summary>The saved word for an empty bed: the state store refuses empty values.</summary>
    public const string Nobody = "none";

    public Dictionary<string, string> Save() => new()
    {
        ["patient"] = Patient.Length == 0 ? Nobody : Patient, ["route"] = Route.ToString(), ["since"] = Since.ToString("R", CultureInfo.InvariantCulture),
        ["reserved"] = Reserved ? "1" : "0", ["outage"] = OutageNoticed ? "1" : "0", ["send"] = SendInjured ? "1" : "0"
    };

    /// <summary>Exactly the five fields of 0.1.0 or the six since 0.2.0, each valid; anything else is not ours to guess at.</summary>
    public static BedState Read(IReadOnlyDictionary<string, string> f)
    {
        bool current = f.Count == Fields && f.ContainsKey("send");
        if (f.Count != LegacyFields && !current) throw new FormatException("Invalid medical bed record.");
        if (!f.TryGetValue("patient", out var saved) || !SafeId(saved)) throw new FormatException("Invalid medical bed patient.");
        string patient = saved == Nobody ? "" : saved;
        if (!f.TryGetValue("route", out var route) || !Enum.TryParse(route, false, out BedRoute r) || !Enum.IsDefined(typeof(BedRoute), r)) throw new FormatException("Invalid medical bed route.");
        if (!f.TryGetValue("since", out var since) || !double.TryParse(since, NumberStyles.Float, CultureInfo.InvariantCulture, out double s) || !CareSchema.Finite(s) || s < 0)
            throw new FormatException("Invalid medical bed time.");
        if ((patient.Length == 0) != (r == BedRoute.None)) throw new FormatException("Medical bed patient and route disagree.");
        return new BedState { Patient = patient, Route = r, Since = s, Reserved = Flag(f, "reserved"), OutageNoticed = Flag(f, "outage"), SendInjured = current && Flag(f, "send") };
    }
    private static bool Flag(IReadOnlyDictionary<string, string> f, string k) =>
        f.TryGetValue(k, out var v) && (v == "1" || (v == "0" ? false : throw new FormatException("Invalid medical bed flag.")));
    /// <summary>A value the shared state store accepts (an object id, or <see cref="Nobody"/>).</summary>
    public static bool SafeId(string? id) => Phobos.Ostranauts.Framework.Persistence.ObjectStateStore.SafeValue(id) && id!.IndexOf(';') < 0;
}
