using Phobos.Ostranauts.Framework.Audio;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Which washer/pump loop each Shipbreaker machine plays while it works (0.84.0; owner request, 6 October
/// 2026: one hard-coded loop per machine, chosen semi-randomly). Big machines run lower, small ones higher. The D4, R4,
/// T2 and C2 sound while their working condition is set and they are powered; the F6 while it heats a batch with
/// power arriving. The G4 grabber and ML-2 laser work outside the hull, where no sound carries; bins, chutes, the
/// console and the furnace's radiator and ports are passive.</summary>
internal static class MachineSoundTable
{
    internal const string FurnaceInstalled = FurnaceRules.Prefix + "Installed";
    internal static void Register()
    {
        MachineSounds.Register(Content.Installed, MachineLoop.D, .8f);
        MachineSounds.Register(ReclaimerRules.Installed, MachineLoop.A, .9f);
        MachineSounds.Register(ThawRules.Installed, MachineLoop.E, 1.06f);
        MachineSounds.Register(CollectorRules.Installed, MachineLoop.G, 1.18f);
        MachineSounds.Register(FurnaceInstalled, MachineLoop.F, .78f, FurnaceService.HeatingNow);
    }
}
