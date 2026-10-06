using Phobos.Ostranauts.Framework.Audio;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>Which washer/pump loop each Manufacturing machine plays while it works (0.57.0; owner request, 6 October
/// 2026: one hard-coded loop per machine, chosen semi-randomly). Big machines run a little lower, small ones a little
/// higher. Each sounds while its working condition is set and it is powered (its power override); the A2 only while gas
/// actually flows. Passive stores, tanks, lines and the P1 manifold make no sound.</summary>
internal static class MachineSoundTable
{
    internal static void Register()
    {
        MachineSounds.Register(ChargeMachines.Refinery.Spec.Installed, MachineLoop.A, .85f);
        MachineSounds.Register(ChargeMachines.Leach.Spec.Installed, MachineLoop.E, .95f);
        MachineSounds.Register(ChargeMachines.AcidPlant.Spec.Installed, MachineLoop.C, .92f);
        MachineSounds.Register(ChargeMachines.Fermenter.Spec.Installed, MachineLoop.H, 1f);
        MachineSounds.Register(ChargeMachines.ElectrolysisCell.Spec.Installed, MachineLoop.D, .82f);
        MachineSounds.Register(ChargeMachines.Carbothermal.Spec.Installed, MachineLoop.G, .88f);
        MachineSounds.Register(ProcessorRules.Installed, MachineLoop.B, 1.08f);
        MachineSounds.Register(SabatierRules.Installed, MachineLoop.F, 1.05f);
        MachineSounds.Register(CrackerRules.Installed, MachineLoop.C, 1.1f);
        MachineSounds.Register(FillerRules.Installed, MachineLoop.G, 1.12f);
        MachineSounds.Register(RegulatorRules.Installed, MachineLoop.E, 1.15f, RegulatorService.Feeding);
        MachineSounds.Register(BottlerRules.Installed, MachineLoop.H, 1.1f);
        MachineSounds.Register(FeederRules.Installed, MachineLoop.B, 1.2f);
    }
}
