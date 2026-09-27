using System;
using System.Linq;
using System.Reflection;
using Phobos.Ostranauts.Framework.Controls;

internal static class PerformanceNativeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        var refreshes = typeof(NativeInstruments).GetMethods(flags).Where(m => m.Name == "Refresh");
        foreach (var method in refreshes)
        {
            var calls = PlaceholderLoadChecks.Calls(method);
            check(!calls.Any(m => m.Name == "Find" || m.Name == "GetComponentsInChildren"),
                "Native widget refresh does not rediscover its hierarchy: " + method.GetParameters()[0].ParameterType.Name);
        }
        var discovery = PlaceholderLoadChecks.Calls(typeof(ShipEquipment).GetMethod("Read")!);
        check(discovery.Any(m => m.DeclaringType == typeof(Ship) && m.Name == "GetCOs"), "Shared discovery uses the installed native ship query");
        var nativeQuery = typeof(Ship).GetMethods().Single(m => m.Name == "GetCOs" && m.GetParameters().Length == 4);
        check(nativeQuery.GetParameters().Select(p => p.Name).SequenceEqual(new[] { "ct", "bSubObjects", "bAllowDocked", "bAllowLocked" }) ||
            nativeQuery.GetParameters().Skip(1).Select(p => p.Name).SequenceEqual(new[] { "bSubObjects", "bAllowDocked", "bAllowLocked" }),
            "Native discovery scope switches retain their audited meaning");
    }
}
