using System;
using System.Linq;
using System.Reflection;

/// <summary>Round 3 of the vanilla-precedence audit (28 September 2026): the native members Auto Nav 0.25.0
/// now leans on instead of its own re-implementations. Signature checks against the game assembly; not a
/// game session.</summary>
internal static class AutoNavPrecedenceNativeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        // The docking console's own clamp button and its admission.
        check(typeof(GUIDockSys).GetMethod("CanDock", flags)?.ReturnType == typeof(bool) && typeof(GUIDockSys).GetMethod("CanDock", flags)!.GetParameters().Length == 0,
            "The docking console's CanDock admission exists for Auto Nav to ask before clamping");
        var clamp = typeof(GUIDockSys).GetMethod("ClampEngage", flags);
        check(clamp != null && clamp.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(bool) }),
            "The docking console's ClampEngage sequence takes the wasTurnedOn flag Auto Nav presses it with");
        check(typeof(GUIDockSys).GetProperty("ClearedShipRegID", flags)?.PropertyType == typeof(string),
            "The docking console names the ship its clearance is for");
        check(typeof(GUIDockSys).GetMethod("ScheduleUnDock", flags) != null && typeof(GUIDockSys).GetMethod("ScheduleDock", flags) != null,
            "Docking and undocking run as the console's own scheduled sequences");
        check(PlaceholderLoadChecks.Calls(clamp!).Any(m => m.DeclaringType == typeof(GUIDockSys) && m.Name == "ScheduleUnDock") &&
            PlaceholderLoadChecks.Calls(clamp!).Any(m => m.DeclaringType == typeof(GUIDockSys) && m.Name == "CanDock"),
            "ClampEngage still releases through ScheduleUnDock and admits through CanDock");
        check(!PlaceholderLoadChecks.Calls(typeof(GUIDockSys).GetMethod("CanDock", flags)!).Any(m => m.Name.Contains("fW") || m.Name.Contains("Spin")),
            "The native docking admission has no target-spin rule");
        // The pilot's throttle mapping and the manoeuvre the game ignores.
        var expMap = typeof(MathUtils).GetMethod("ExpMap", flags);
        check(expMap != null && expMap.GetParameters().Length == 2 && expMap.GetParameters()[0].ParameterType == typeof(float) &&
            Math.Abs((float)expMap.Invoke(null, new object[] { .5f, 10f })! - (float)((Math.Pow(10, .5) - 1) / 9)) < 1e-6,
            "The game's ExpMap throttle mapping is what Auto Nav applies to the slider");
        var maneuver = typeof(Ship).GetMethod("Maneuver", flags);
        check(maneuver != null && maneuver.GetParameters().Length >= 5 && maneuver.GetParameters()[4].ParameterType == typeof(float),
            "Ship.Maneuver takes the step duration Auto Nav must keep positive");
        // What the nav station shows without a signal test.
        check(Enum.GetNames(typeof(Ship.TypeClassification)).Contains("SignalBeacon") && typeof(GUIOrbitDraw).GetMethod("VisibleFromNavStation", flags) != null,
            "Signal beacons and the nav station's own visibility rule exist");
        var visible = typeof(GUIOrbitDraw).GetMethod("VisibleFromNavStation", flags)!;
        check(PlaceholderLoadChecks.Calls(visible).Any(m => m.Name == "IsDockedWith"), "The nav station shows docked partners without a signal test");
        // The epoch advances inside the system update, after any prefix on it.
        var update = typeof(StarSystem).GetMethod("Update", new[] { typeof(double) });
        check(update != null && typeof(StarSystem).GetField("fEpoch", flags)?.IsStatic == true, "StarSystem.Update and the static epoch the torch guard keys on exist");
        bool Has(Type type, string name) => type.GetMethods(flags).Any(m => m.Name == name);
        check(Has(typeof(ShipSitu), "LockToOrbit") && Has(typeof(ShipSitu), "LockToBO") && Has(typeof(Ship), "LockToOrbit"),
            "Native orbit locks Auto Nav now yields to exist");
    }
}
