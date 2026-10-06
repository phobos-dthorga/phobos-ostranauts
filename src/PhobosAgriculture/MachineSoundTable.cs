using Phobos.Ostranauts.Framework.Audio;

namespace PhobosAgriculture;

/// <summary>Which washer/pump loop each Verdemorrow machine plays while it works (0.64.0; owner request, 6 October
/// 2026: one hard-coded loop per machine, chosen semi-randomly). Each sounds while power above standby is reaching it:
/// a rack with its lamps lit, the cooker or bench working, the W2 pumping. Hoppers, reservoirs and conduits are passive.</summary>
internal static class MachineSoundTable
{
    internal static void Register()
    {
        MachineSounds.Register(Definitions.Rack + "Installed", MachineLoop.H, .95f, Service.Working);
        MachineSounds.Register(Definitions.Cooker + "Installed", MachineLoop.C, 1.02f, Service.Working);
        MachineSounds.Register(WorkupDefinitions.Bench + "Installed", MachineLoop.A, 1.1f, Service.Working);
        MachineSounds.Register(IrrigationDefinitions.Supply + "Installed", MachineLoop.B, 1f, Service.Working);
    }
}
