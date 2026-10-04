using Phobos.Ostranauts.Framework.Registration;

namespace PhobosMedical.Core;

/// <summary>Identities and fixed geometry of Phobos Medical. Ids, record names and native names live here; the figures
/// players may tune live in the <c>care</c> data pack (<see cref="Care"/>) and the economy pack.</summary>
public static class MedicalRules
{
    public const string Owner = "phobosgekko.ostranauts.medical";
    public const string ModFolder = "PhobosMedical";
    /// <summary>The Halewright Ward-3 bed family.</summary>
    public const string BedPrefix = "PhobosMedicalBed", BedInstalled = BedPrefix + "Installed", BedMachine = BedPrefix + "Machine";
    /// <summary>The bed's saved record (patient, route, settings).</summary>
    public const string Record = "MedicalBed";
    public const string ContentMarker = "PhobosMedicalContent";
    /// <summary>On the bed while it gives care: the game then draws the working power.</summary>
    public const string InUse = "PhobosMedicalBedInUse";
    /// <summary>On a person resting awake in a Ward-3.</summary>
    public const string Resting = "PhobosMedicalResting";
    /// <summary>Set by the bed service on a rester whose injuries have eased below the discharge share.</summary>
    public const string Rested = "PhobosMedicalRested";
    /// <summary>A rester's care: the game's own Recuperating figures (<c>CONDSleepingMedicalPer</c>) on an awake patient.</summary>
    public const string Recovering = "PhobosMedicalRecovering";
    /// <summary>Hidden mark: a Ward-3 granted this person care, so care is withdrawn if no bed still claims them.</summary>
    public const string CareMark = "PhobosMedicalCare";
    /// <summary>The game's own medical sleep condition and its effect loot.</summary>
    public const string SleepingMedical = "SleepingMedical", RecuperatingEffect = "CONDSleepingMedicalPer";

    public const string Controls = "PhobosMedicalControls";
    public const string Rest = "PhobosMedicalRest", RestLoop = "PhobosMedicalRestLoop", RestSleep = "PhobosMedicalRestSleep",
        RestEnd = "PhobosMedicalRestEnd", RestCancel = "PhobosMedicalRestCancel", Lay = "PhobosMedicalLay";
    /// <summary>The right-click toggle for Send injured crew here (Medical 0.2.0).</summary>
    public const string Send = "PhobosMedicalSend";
    public const string RestStartLoot = "PhobosMedicalRestStartUs", RestStopLoot = "PhobosMedicalRestStopUs";
    public const string BedFreeTrigger = "PhobosMedicalBedFree", CanRestTrigger = "PhobosMedicalCanRest",
        DrawerTrigger = "PhobosMedicalDrawer", DrawerClothTrigger = "PhobosMedicalDrawerCloth";
    /// <summary>The game's sleep opener, which the Ward-3 offers unchanged.</summary>
    public const string Sleep = "SeekSleepSimple";

    /// <summary>3 tiles across, 5 front to back, like the Infirmaway.</summary>
    public const int Width = 3, Depth = 5, DrawerWidth = 3, DrawerHeight = 2;
    public const double MachineKg = 92;
    public const string SleepPoint = "sleep", UsePoint = "use";
    /// <summary>The Infirmaway's own walk-to and lie-down point: the middle of the mattress.</summary>
    public const string PatientPointOffset = "0,-1";
    /// <summary>How far from the sleep point, in world units (one tile each), a patient may lie and still count.</summary>
    public const double PatientReach = 1.0;
    public const double TickSeconds = 2;
    /// <summary>How low the bed draws: the game's own beds use 0.05, so a patient lying on it draws on top.</summary>
    public const float BedZScale = 0.05f;
    public static readonly string[] Forms = { "Installed", "Loose", "InstalledDmg", "LooseDmg" };
    public static bool IsBed(string? id) => EquipmentIdentity.IsFamily(id, BedPrefix);
    /// <summary>The interactions a Ward-3's patient record guards: someone else may not take an occupied bed.</summary>
    public static readonly string[] Guarded = { Sleep, Rest, Lay };
}
