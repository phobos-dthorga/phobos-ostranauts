using System;
using System.Collections.Generic;
using System.Linq;

namespace PhobosMedical.Core;

/// <summary>One wound as the medic order sees it: the game's wound facts plus what lies on its item slot.</summary>
public readonly struct WoundCase
{
    public string Part { get; }
    public string Slot { get; }
    public bool Bleeding { get; }
    public bool Fractured { get; }
    public bool Splinted { get; }
    public bool Vital { get; }
    public double BleedRate { get; }
    public double Worst { get; }
    /// <summary>Something already lies on the wound's item slot.</summary>
    public bool Covered { get; }
    /// <summary>What lies there is dirty (the game turns a clean dressing dirty as it wears).</summary>
    public bool Spent { get; }
    public WoundCase(string part, string slot, bool bleeding, bool fractured, bool splinted, bool vital, double bleedRate, double worst, bool covered, bool spent)
    {
        Part = part; Slot = slot; Bleeding = bleeding; Fractured = fractured; Splinted = splinted; Vital = vital; BleedRate = bleedRate; Worst = worst; Covered = covered; Spent = spent;
    }
}

/// <summary>A treatment due on one wound.</summary>
public sealed class TreatmentStep
{
    public string Id { get; }
    public TreatmentEntry Entry { get; }
    public WoundCase Wound { get; }
    /// <summary>The spent item must come off before the new one goes on.</summary>
    public bool Replace => Entry.test == TreatmentRules.SpentDressing;
    /// <summary>The crew action name: treatment and wound, so a finished step is checked against the same need.</summary>
    public string Action => TreatmentRules.ActionPrefix + Id + ":" + Wound.Part;
    public TreatmentStep(string id, TreatmentEntry entry, WoundCase wound) { Id = id; Entry = entry; Wound = wound; }
}

/// <summary>The medic order's rules (Medical 0.4.0). Code owns a fixed vocabulary of tests and effects; the care pack
/// says which item each treatment uses and how long it takes. A treatment never writes wound figures: the item goes onto
/// the wound slot and the game's own slot effect does the rest (a clean cloth staunches, a splint splints).</summary>
public static class TreatmentRules
{
    public const string Bleeding = "bleeding", Fracture = "fracture", SpentDressing = "spent-dressing";
    public static readonly IReadOnlyList<string> Tests = new[] { Bleeding, Fracture, SpentDressing };
    public const string SlotItem = "slot-item";
    public static readonly IReadOnlyList<string> Effects = new[] { SlotItem };
    public const double MinSeconds = 5, MaxSeconds = 1800;
    public const int MaxOrder = 1000, MaxName = 48;
    public const string ActionPrefix = "treat:";
    /// <summary>The game names fracture wounds and their item slots with this word (<c>WoundItemArmFractureL</c>).</summary>
    public const string FractureWord = "Fracture";

    /// <summary>A check against the game's definitions, set while the game is loaded (offline checks leave it unset):
    /// given a test and an item, a problem text or null.</summary>
    public static Func<string, string, string?>? NativeCheck { get; set; }

    public static void Validate(string id, TreatmentEntry entry)
    {
        if (!Name(id, allowDash: true)) throw new ArgumentException(Text.Get("care_treatment_name", id ?? "", MaxName));
        if (entry == null) throw new ArgumentException(Text.Get("care_treatment_name", id, MaxName));
        if (!Tests.Contains(entry.test)) throw new ArgumentException(Text.Get("care_treatment_test", id, entry.test ?? "", string.Join(", ", Tests)));
        if (!Effects.Contains(entry.effect)) throw new ArgumentException(Text.Get("care_treatment_effect", id, entry.effect ?? "", string.Join(", ", Effects)));
        if (!Name(entry.item, allowDash: false)) throw new ArgumentException(Text.Get("care_treatment_item", id));
        if (!CareSchema.Finite(entry.medicSeconds) || entry.medicSeconds < MinSeconds || entry.medicSeconds > MaxSeconds)
            throw new ArgumentException(Text.Get("care_treatment_seconds", id, MinSeconds, MaxSeconds));
        if (!string.IsNullOrEmpty(entry.skill) && !Name(entry.skill, allowDash: false)) throw new ArgumentException(Text.Get("care_treatment_skill", id));
        if (entry.order < 0 || entry.order > MaxOrder) throw new ArgumentException(Text.Get("care_treatment_order", id, MaxOrder));
        var problem = NativeCheck?.Invoke(entry.test, entry.item);
        if (problem != null) throw new ArgumentException(Text.Get("care_treatment_native", id, problem));
    }
    private static bool Name(string? s, bool allowDash) => !string.IsNullOrEmpty(s) && s!.Length <= MaxName &&
        s.All(c => c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_' || allowDash && c == '-');

    /// <summary>Whether an item that fits these wound item slots can serve the test: a fracture test needs a fracture
    /// slot, the others an ordinary wound slot.</summary>
    public static bool SlotsSuit(string test, IEnumerable<string> slots)
    {
        bool fracture = test == Fracture;
        return slots.Any(s => s.StartsWith("WoundItem", StringComparison.Ordinal) && s.Contains(FractureWord) == fracture);
    }

    /// <summary>Whether the test applies to the wound now.</summary>
    public static bool Applies(string test, WoundCase w) => test switch
    {
        Bleeding => w.Bleeding && !w.Covered && !IsFractureSlot(w.Slot),
        Fracture => w.Fractured && !w.Splinted && !w.Covered && IsFractureSlot(w.Slot),
        SpentDressing => w.Covered && w.Spent && !IsFractureSlot(w.Slot),
        _ => false,
    };
    public static bool IsFractureSlot(string slot) => slot.Contains(FractureWord);

    /// <summary>Every treatment due, in the order of care: treatments by their order, and within one treatment the
    /// worst wound first (vital parts, then the fastest bleed, then the deepest wound, then by name). A wound may appear
    /// under more than one treatment, so a later one (a dirty cloth when no clean one can be had) serves as a fallback;
    /// the medic order takes the first step it can carry out. A treatment stays due while its test holds, so its item
    /// should change the wound (a dressing covers it, a splint splints it).</summary>
    public static IReadOnlyList<TreatmentStep> Due(IEnumerable<KeyValuePair<string, TreatmentEntry>> treatments, IEnumerable<WoundCase> wounds)
    {
        var cases = wounds.OrderByDescending(w => w.Vital).ThenByDescending(w => w.BleedRate).ThenByDescending(w => w.Worst).ThenBy(w => w.Part, StringComparer.Ordinal).ToList();
        var due = new List<TreatmentStep>();
        foreach (var t in treatments.OrderBy(p => p.Value.order).ThenBy(p => p.Key, StringComparer.Ordinal))
            foreach (var w in cases)
                if (Applies(t.Value.test, w)) due.Add(new TreatmentStep(t.Key, t.Value, w));
        return due;
    }
}
