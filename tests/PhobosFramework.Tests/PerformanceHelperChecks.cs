using System;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Registration;

internal static class PerformanceHelperChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var gate = new PresentationRefresh(.1);
        int draws = 0;
        for (int i = 0; i < 10000; i++) if (gate.Due(i / 10000d)) draws++;
        check(draws == 10, "Routine presentation draws at most ten times in one second");
        check(gate.Due(5) && !gate.Due(5) && !gate.Due(5.01), "A stall produces one draw without catch-up bursts");
        gate.Invalidate(); check(gate.Due(5.01), "Explicit action refreshes immediately");
        var co = new object(); var actor = new object(); var target = new object();
        gate.Bind(co, actor, target, "en"); check(gate.Due(5.01), "Opening a binding refreshes immediately");
        gate.Bind(co, actor, target, "en"); check(!gate.Due(5.01), "Unchanged binding preserves the rate limit");
        gate.Bind(co, new object(), target, "en"); check(gate.Due(5.01), "Operator change invalidates");
        gate.Bind(co, actor, new object(), "en"); check(gate.Due(5.01), "Target change invalidates");
        gate.Bind(new object(), actor, target, "en"); check(gate.Due(5.01), "Console replacement invalidates");
        gate.Bind(co, actor, target, "fr"); check(gate.Due(5.01), "Language change invalidates");
        check(gate.Due(0) && !gate.Due(double.NaN) && !gate.Due(double.PositiveInfinity), "Clock reset recovers and nonfinite times do not draw");
        foreach (double interval in new[] { 0, -1, double.NaN, double.PositiveInfinity })
        { bool rejected = false; try { _ = new PresentationRefresh(interval); } catch (ArgumentOutOfRangeException) { rejected = true; } check(rejected, "Invalid cadence rejected"); }

        string prefix = "PhobosTest";
        foreach (string suffix in new[] { "Installed", "InstalledDmg", "Loose", "LooseDmg" })
        {
            check(EquipmentIdentity.IsFamily(prefix + suffix, prefix), "Exact supported native form matches");
            check(!EquipmentIdentity.IsFamily(prefix + suffix + "Extra", prefix), "Extended identity is not a family member");
            check(!EquipmentIdentity.IsFamily(prefix + suffix.ToLowerInvariant(), prefix), "Identity matching remains ordinal");
        }
        check(!EquipmentIdentity.IsFamily(null, prefix) && !EquipmentIdentity.IsFamily(prefix, prefix) &&
            !EquipmentIdentity.IsFamily("Installed", ""), "Missing IDs and empty prefixes do not match");
        // Allocation API here is .NET's supported test runtime, not a Unity measurement.
        for (int i = 0; i < 1000; i++) EquipmentIdentity.IsFamily("OtherInstalled", prefix);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 42000; i++) EquipmentIdentity.IsFamily("OtherInstalled", prefix);
        check(GC.GetAllocatedBytesForCurrentThread() == before, "Nonmatching equipment scan allocates no candidate strings on the test runtime");
    }
}
