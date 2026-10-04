using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Health;

/// <summary>One wound as the game holds it: a body-part object under the person with its own cut, blunt, bleeding,
/// infection and dressing state.</summary>
public readonly struct WoundFacts
{
    public string Part { get; }
    public double Cut { get; }
    public double Blunt { get; }
    public double BleedRate { get; }
    public double InfectionRate { get; }
    public bool Staunched { get; }
    public bool Fractured { get; }
    public bool Splinted { get; }
    public bool Vital { get; }
    /// <summary>Bleeding into the patient: the game stops adding blood loss once the rate falls under 0.1 or the wound is staunched.</summary>
    public bool Bleeding => BleedRate >= PatientFacts.BleedFloor && !Staunched;
    public double Worst => Math.Max(Cut, Blunt);
    public WoundFacts(string part, double cut, double blunt, double bleedRate, double infectionRate, bool staunched, bool fractured, bool splinted, bool vital)
    {
        Part = part; Cut = cut; Blunt = blunt; BleedRate = bleedRate; InfectionRate = infectionRate; Staunched = staunched; Fractured = fractured; Splinted = splinted; Vital = vital;
    }
}

/// <summary>A read-only snapshot of a person's health as the game holds it (Framework 0.82.0, first used by Phobos
/// Medical): blood lost (<c>StatBlood</c> counts loss, not blood remaining), infection and pain on the person, and
/// each wound on its body part. Reading never changes the person. Missing figures read as zero, as the game's own
/// condition lookup does; a person whose wounds cannot be read has an empty list, not a healthy one by assertion.</summary>
public sealed class PatientFacts
{
    /// <summary>The game zeroes a wound's bleed rate below this (<c>Wound.Run</c>).</summary>
    public const double BleedFloor = 0.1;
    public double BloodLost { get; }
    public double Infection { get; }
    public double Pain { get; }
    public IReadOnlyList<WoundFacts> Wounds { get; }
    public bool Unconscious { get; }
    public bool Dead { get; }
    public double WorstWound { get { double w = 0; foreach (var x in Wounds) w = Math.Max(w, x.Worst); return w; } }
    public int Bleeding { get { int n = 0; foreach (var x in Wounds) if (x.Bleeding) n++; return n; } }
    public int UnsplintedFractures { get { int n = 0; foreach (var x in Wounds) if (x.Fractured && !x.Splinted) n++; return n; } }

    public PatientFacts(double bloodLost, double infection, double pain, IReadOnlyList<WoundFacts> wounds, bool unconscious, bool dead)
    {
        BloodLost = bloodLost; Infection = infection; Pain = pain; Wounds = wounds ?? Array.Empty<WoundFacts>(); Unconscious = unconscious; Dead = dead;
    }

    /// <summary>The person's facts, or null for something that is not a person. Wounds are only the person's own: the
    /// game's wound list also walks a dragged body, so each wound must lead back to this person.</summary>
    public static PatientFacts? Read(CondOwner? person)
    {
        if (person == null || person.bDestroyed || !(person.HasCond("IsHuman") || person.HasCond("IsRobot"))) return null;
        var wounds = new List<WoundFacts>();
        List<Wound>? found = null;
        try { found = person.GetAllWounds(); } catch (Exception) { found = null; }
        foreach (var wound in found ?? new List<Wound>())
        {
            var part = wound?.coUs;
            if (part == null || part.bDestroyed || part.RootParent() != person) continue;
            wounds.Add(new WoundFacts(part.strName, part.GetCondAmount("StatWoundCut"), part.GetCondAmount("StatWoundBlunt"), part.GetCondAmount("StatBloodRate"),
                part.GetCondAmount("StatInfectionRate"), part.HasCond("IsStaunched"), part.HasCond("FracturedBone"), part.HasCond("IsSplinted"), part.HasCond("IsWoundVital")));
        }
        return new PatientFacts(person.GetCondAmount("StatBlood"), person.GetCondAmount("StatInfection"), person.GetCondAmount("StatPain"), wounds,
            person.HasCond("Unconscious"), person.HasCond("IsDead"));
    }
}
