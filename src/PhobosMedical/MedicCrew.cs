using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Health;
using Phobos.Ostranauts.Framework.Inventory;
using PhobosMedical.Core;

namespace PhobosMedical;

/// <summary>The crew order "Keep patient treated" on an installed Ward-3 (Medical 0.4.0). While someone lies in the bed,
/// a crew member with the Operate duty works through the care pack's treatments in order: dress a bleeding wound, splint
/// a fracture, change a dirty dressing. Each treatment uses up one real item from the bed's drawer and puts it onto the
/// wound slot the game's own way (Framework <c>WoundCare</c>), so the healing is the game's. When the drawer lacks an
/// item, crew with the Haul duty bring one from anywhere aboard (the deck, unlocked containers and other machines'
/// trays, never from someone's hands, a locked container or a wound). Crew with a treatment's skill are asked first and
/// work faster. The patient never treats themselves.</summary>
internal sealed class MedicCrewProvider : ICrewWorkProvider, ICrewOrderPresentation
{
    public string Id => Plugin.Id;
    public bool Supports(CondOwner equipment) => equipment != null && equipment.strCODef == MedicalRules.BedInstalled;
    public bool RoutineResume(CondOwner equipment) => true;
    public IReadOnlyList<string> Recipes(CondOwner equipment) => new[] { MedicalRules.TreatRecipe };
    public string RecipeLabel(string recipe) => Text.Get("Medic.recipe");

    /// <summary>The bed's patient and every treatment due on them now, in the order of care.</summary>
    internal static IReadOnlyList<TreatmentStep> Due(CondOwner bed, out CondOwner? patient)
    {
        patient = BedService.Patient(bed);
        var facts = PatientFacts.Read(patient);
        if (patient == null || facts == null || facts.Dead) return Array.Empty<TreatmentStep>();
        var cases = new List<WoundCase>();
        foreach (var w in facts.Wounds)
        {
            string? slot = WoundCare.ItemSlot(w.Part);
            if (slot == null || patient.compSlots?.GetSlot(slot) == null) continue;
            var on = WoundCare.Occupant(patient, slot);
            cases.Add(new WoundCase(w.Part, slot, w.Bleeding, w.Fractured, w.Splinted, w.Vital, w.BleedRate, w.Worst, on != null, on != null && on.HasCond("IsDirty")));
        }
        return TreatmentRules.Due(Care.Treatments, cases);
    }
    private static bool Usable(CondOwner c, string item) => c != null && !c.bDestroyed && c.strCODef == item && !c.HasCond("IsDamaged");
    private static CondOwner? Stock(CondOwner bed, string item) => StackUnits.All(bed).FirstOrDefault(u => Usable(u, item));
    private static string ItemName(string item) => DataHandler.dictCOs != null && DataHandler.dictCOs.TryGetValue(item, out var def) && !string.IsNullOrEmpty(def.strNameFriendly) ? def.strNameFriendly : item;
    private static string Blocked(CondOwner bed) => !BedService.Intact(bed) || BedService.Protected(bed) || bed.HasCond("IsLocked") ? Text.Get("Medic.blocked") : "";

    public CrewWorkOffer? Next(CondOwner bed, StandingOrder order, out string reason)
    {
        reason = Blocked(bed);
        if (reason.Length > 0) return null;
        if (order.Recipe != MedicalRules.TreatRecipe) { reason = Text.Get("Medic.select_recipe"); return null; }
        var steps = Due(bed, out var patient);
        if (patient == null) { reason = Text.Get("Medic.no_patient"); return null; }
        if (steps.Count == 0) { reason = Text.Get("Medic.nothing_due", patient.strNameFriendly); return null; }
        string? missing = null;
        foreach (var step in steps)
        {
            string item = step.Entry.item;
            CrewWorkOffer? offer;
            if (Stock(bed, item) != null)
                offer = new CrewWorkOffer(step.Action, Text.Get("Medic.treat_" + step.Entry.test, ItemName(item), patient.strNameFriendly), CrewRole.Medical, bed,
                    step.Entry.medicSeconds, step.Entry.skill ?? "");
            else
                offer = CrewLogistics.Supply(bed, order, bed, c => Usable(c, item), CrewRole.Medical);
            if (offer != null) { offer.ExcludedActor = patient.strID; return offer; }
            missing ??= item;
        }
        reason = Text.Get("Medic.no_stock", ItemName(missing!));
        return null;
    }

    public bool Complete(CrewWorkContext context, CrewWorkOffer offer, out string reason)
    {
        var bed = context.Equipment;
        reason = Blocked(bed);
        if (reason.Length > 0) return false;
        var step = Due(bed, out var patient).FirstOrDefault(s => s.Action == offer.Action);
        if (patient == null || step == null || context.Actor == patient) { reason = Text.Get("Medic.no_longer_due"); return false; }
        bool done = Treat(bed, patient, step, out reason);
        if (done) Plugin.Log(Text.Get("Medic.log", context.Actor?.strNameFriendly ?? "", step.Id, step.Wound.Part, patient.strNameFriendly));
        return done;
    }

    /// <summary>Performs one treatment: checks every condition before anything moves, takes one unit from the drawer, takes
    /// a spent item off onto the deck when the treatment replaces it, and slots the unit. A unit the game refuses after
    /// it left the drawer is set down on the deck beside the bed, never destroyed.</summary>
    internal static bool Treat(CondOwner bed, CondOwner patient, TreatmentStep step, out string reason)
    {
        string slot = step.Wound.Slot, item = step.Entry.item;
        var unit = Stock(bed, item);
        if (unit == null) { reason = Text.Get("Medic.no_stock", ItemName(item)); return false; }
        if (!WoundCare.Fits(item, slot)) { reason = Text.Get("Medic.does_not_fit", ItemName(item)); return false; }
        if (step.Replace)
        {
            if (WoundCare.RemoveToDeck(patient, slot, bed) == null) { reason = Text.Get("Medic.no_longer_due"); return false; }
        }
        else if (WoundCare.Occupant(patient, slot) != null) { reason = Text.Get("Medic.no_longer_due"); return false; }
        StackUnits.Detach(unit);
        if (WoundCare.Apply(patient, slot, unit, out var failure))
        {
            reason = Text.Get("Medic.done_" + step.Entry.test, ItemName(item), patient.strNameFriendly);
            return true;
        }
        WoundCare.SetDown(unit, bed);
        reason = Text.Get("Medic.refused", ItemName(item), failure.ToString());
        return false;
    }

    /// <summary>Stopping the order leaves every dressing where it is.</summary>
    public void Suspend(CondOwner equipment) { }

    // --- The Crew panel ------------------------------------------------------------------------------------------
    public OrderFields Fields(CondOwner equipment) => OrderFields.Source | OrderFields.Routine;
    public IEnumerable<Ship> Targets(CondOwner equipment) => Array.Empty<Ship>();
    public OrderState Activity(CondOwner bed, StandingOrder order) =>
        Blocked(bed).Length > 0 ? OrderState.Blocked : Due(bed, out _).Count > 0 ? OrderState.Running : OrderState.Waiting;
    public bool Validate(CondOwner equipment, StandingOrder draft, out string reason) { reason = ""; return true; }
    public bool RelevantStore(CondOwner equipment, StandingOrder draft, CondOwner store, bool output) =>
        !output && CrewLogistics.Contents(store).Any(c => Care.Treatments.Any(t => Usable(c, t.Value.item)));

    /// <summary>The right-click toggle, like the game's own Toggle Power: on enables the order with the ship-wide
    /// source (a store already chosen in the Crew panel is kept); off is a manual stop.</summary>
    internal static bool Toggle(CondOwner bed, out string message)
    {
        if (bed == null || bed.bDestroyed || bed.strCODef != MedicalRules.BedInstalled || !bed.HasCond("IsInstalled")) { message = Text.Get("Medic.blocked"); return false; }
        if (!CrewWork.CanManage(bed)) { message = Text.Get("Medic.not_owned"); return false; }
        var order = CrewWork.Order(bed);
        if (order.Protected) { message = CrewWork.Message("protected"); return false; }
        if (order.Permission == WorkPermission.Enabled && order.Recipe == MedicalRules.TreatRecipe)
        {
            CrewWork.SetPermission(bed, WorkPermission.Stopped);
            message = Text.Get("Medic.off", bed.strNameFriendly); return true;
        }
        bool configured = CrewWork.Configure(bed, o => { o.Recipe = MedicalRules.TreatRecipe; if (o.Source == "none") o.Source = StandingOrder.ShipWide; o.ResumeRoutine = true; });
        if (configured) CrewWork.SetPermission(bed, WorkPermission.Enabled);
        if (!configured || CrewWork.Order(bed).Permission != WorkPermission.Enabled) { message = CrewWork.Message("protected"); return false; }
        message = Text.Get("Medic.on", bed.strNameFriendly) + "\n" + CrewWork.Status(bed); return true;
    }
    internal static bool Active(CondOwner bed) => CrewWork.Order(bed) is { Permission: WorkPermission.Enabled } o && o.Recipe == MedicalRules.TreatRecipe;
}
