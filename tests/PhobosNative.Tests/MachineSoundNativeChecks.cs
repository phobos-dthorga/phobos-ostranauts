using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Audio;

/// <summary>Framework 0.119.0 machine work sounds: every machine a mod registers exists as an installed definition, and
/// one without its own working test has a power override that shows its work, so none is silent by mistake. The
/// machines left silent on purpose (outside the hull, no moving parts) stay unregistered.</summary>
internal static class MachineSoundNativeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        PhobosManufacturing.MachineSoundTable.Register();
        PhobosShipbreaker.MachineSoundTable.Register();
        PhobosAgriculture.MachineSoundTable.Register();
        MachineSounds.ForgetDefinitions();
        check(MachineSounds.Entries.Count == 22, "Twenty-two machines sound while they work: " + MachineSounds.Entries.Count);
        foreach (var entry in MachineSounds.Entries.Values)
        {
            check(DataHandler.dictCOs.TryGetValue(entry.Definition, out var definition) && definition.aStartingConds.Any(c => c.StartsWith("IsInstalled=", StringComparison.Ordinal)),
                "A machine sound belongs to an installed machine: " + entry.Definition);
            check(entry.Pitch >= MachineSoundRules.MinPitch && entry.Pitch <= MachineSoundRules.MaxPitch, "A machine's pitch is in range: " + entry.Definition);
            if (entry.Working == null)
                check(!string.IsNullOrEmpty(MachineSounds.OverrideCondition(entry.Definition)), "A machine with no test of its own shows its work through its power override: " + entry.Definition);
        }
        check(Enum.GetValues(typeof(MachineLoop)).Cast<MachineLoop>().All(loop => MachineSounds.Entries.Values.Any(e => e.Loop == loop)), "All eight loops are in use");
        foreach (string silent in new[] { "PhobosExteriorGrabberInstalled", "PhobosMiningLaserInstalled", "PhobosMedicalBedInstalled", "PhobosMedicalMonitorInstalled", "PhobosPropellantManifoldInstalled" })
            check(!MachineSounds.Has(silent), "Left silent on purpose: " + silent);

        MachineSounds.Register("TestMachineInstalled", MachineLoop.C, 1.1f, _ => true);
        check(MachineSounds.Has("TestMachineInstalled") && MachineSounds.Entries["TestMachineInstalled"].Loop == MachineLoop.C, "A mod registers a machine's loop");
        MachineSounds.Register("TestMachineInstalled", MachineLoop.D, .9f);
        check(MachineSounds.Entries["TestMachineInstalled"].Loop == MachineLoop.D, "Registering again replaces the entry");
        bool threw = false;
        try { MachineSounds.Register("TestMachineInstalled", MachineLoop.A, 2f); } catch (ArgumentOutOfRangeException) { threw = true; }
        check(threw, "A pitch outside the allowed range is refused");
        MachineSounds.Unregister("TestMachineInstalled");
        check(!MachineSounds.Has("TestMachineInstalled"), "A machine can be unregistered");
    }
}
