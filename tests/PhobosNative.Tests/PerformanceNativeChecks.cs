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

        // 29 September 2026 pass (FF3): the power hooks classify every powered object by one probe per definition and
        // leave no state for appliances that are not ours.
        PhobosShipbreaker.PowerKinds.Reset();
        foreach (var (id, kind) in new[] {
            (PhobosShipbreaker.Content.Installed, PhobosShipbreaker.PowerKind.Processor), (PhobosShipbreaker.Content.Installed + "Dmg", PhobosShipbreaker.PowerKind.Processor),
            (PhobosShipbreaker.Core.ReclaimerRules.Installed, PhobosShipbreaker.PowerKind.Reclaimer),
            (PhobosShipbreaker.Core.IntakeRules.Grabber + "Installed", PhobosShipbreaker.PowerKind.Grabber),
            (PhobosShipbreaker.Core.CollectorRules.Installed, PhobosShipbreaker.PowerKind.Collector),
            (PhobosShipbreaker.Core.FurnaceRules.Prefix + "Installed", PhobosShipbreaker.PowerKind.Furnace), (PhobosShipbreaker.Core.FurnaceRules.Prefix + "InstalledDmg", PhobosShipbreaker.PowerKind.Furnace),
            (PhobosShipbreaker.Core.ThawRules.Installed, PhobosShipbreaker.PowerKind.Thaw),
            (PhobosShipbreaker.Core.FurnaceRules.Radiator + "Installed", PhobosShipbreaker.PowerKind.None),
            ("ItmWall1x1", PhobosShipbreaker.PowerKind.None), ("ItmAirPumpInstalled", PhobosShipbreaker.PowerKind.None), (null!, PhobosShipbreaker.PowerKind.None) })
            check(PhobosShipbreaker.PowerKinds.Classify(id) == kind, "Power hook classification: " + (id ?? "null") + " -> " + kind);
        check(PhobosShipbreaker.FurnaceService.IsEquipmentDefinition(PhobosShipbreaker.Core.FurnaceRules.Radiator + "Installed") &&
            PhobosShipbreaker.FurnaceService.IsEquipmentDefinition(PhobosShipbreaker.Core.FurnaceRules.ThermalPort + "LooseDmg") &&
            !PhobosShipbreaker.FurnaceService.IsEquipmentDefinition("ItmWall1x1") && !PhobosShipbreaker.FurnaceService.IsEquipmentDefinition(null), "Furnace-family discovery classifies by definition");
        for (int i = 0; i < 1000; i++) { PhobosShipbreaker.PowerKinds.Classify("ItmAirPumpInstalled"); PhobosShipbreaker.FurnaceService.IsEquipmentDefinition("ItmAirPumpInstalled"); }
        check(AllocatesNothing(() => { for (int i = 0; i < 42000; i++) { PhobosShipbreaker.PowerKinds.Classify("ItmAirPumpInstalled"); PhobosShipbreaker.FurnaceService.IsEquipmentDefinition("ItmAirPumpInstalled"); } }),
            "Classifying a foreign appliance allocates nothing on the test runtime");

        // Manufacturing's hooks classify the same way.
        PhobosManufacturing.MachineKinds.Reset();
        foreach (var (id, kind) in new[] {
            (PhobosManufacturing.Core.RefineryRules.Installed, PhobosManufacturing.MachineKind.Refinery), (PhobosManufacturing.Core.ProcessorRules.Installed + "Dmg", PhobosManufacturing.MachineKind.Processor),
            (PhobosManufacturing.Core.SabatierRules.Installed, PhobosManufacturing.MachineKind.Sabatier), (PhobosManufacturing.Core.FillerRules.Installed, PhobosManufacturing.MachineKind.Filler),
            (PhobosManufacturing.Core.CrackerRules.Installed + "Dmg", PhobosManufacturing.MachineKind.Cracker),
            (PhobosManufacturing.Core.ManifoldRules.Installed, PhobosManufacturing.MachineKind.None), (PhobosManufacturing.Core.GasStores.Hydrogen.Installed, PhobosManufacturing.MachineKind.None),
            ("ItmAirPumpInstalled", PhobosManufacturing.MachineKind.None), (null!, PhobosManufacturing.MachineKind.None) })
            check(PhobosManufacturing.MachineKinds.Classify(id) == kind, "Manufacturing power hook classification: " + (id ?? "null") + " -> " + kind);
        check(PhobosManufacturing.MachineKinds.IsOurs(PhobosManufacturing.Core.ManifoldRules.Installed) && PhobosManufacturing.MachineKinds.IsOurs(PhobosManufacturing.Core.GasStores.Hydrogen.Installed + "Dmg") &&
            PhobosManufacturing.MachineKinds.IsOurs(PhobosManufacturing.Core.RegulatorRules.Installed) && !PhobosManufacturing.MachineKinds.IsOurs("ItmAirPumpInstalled") && !PhobosManufacturing.MachineKinds.IsOurs(null),
            "Every Manufacturing family is ours by definition; foreign ids are not");
        for (int i = 0; i < 1000; i++) { PhobosManufacturing.MachineKinds.IsOurs("ItmAirPumpInstalled"); PhobosManufacturing.MachineKinds.Classify("ItmAirPumpInstalled"); }
        check(AllocatesNothing(() => { for (int i = 0; i < 42000; i++) { PhobosManufacturing.MachineKinds.Classify("ItmAirPumpInstalled"); PhobosManufacturing.MachineKinds.IsOurs("ItmAirPumpInstalled"); } }),
            "Manufacturing classification of a foreign appliance allocates nothing on the test runtime");

        // Stage 8: capture probes resolve their game methods by exact signature; the shared world families classify ours only.
        foreach (var (name, target) in Phobos.Ostranauts.Framework.Diagnostics.CaptureProbes.Targets())
            check(target != null && target.DeclaringType?.Assembly == typeof(CrewSim).Assembly, "Capture probe target resolves in the game: " + name);
        check(PhobosAgriculture.Definitions.MachineDefinition(PhobosAgriculture.Definitions.Rack + "Installed") && PhobosAgriculture.Definitions.MachineDefinition(PhobosAgriculture.Definitions.Cooker + "LooseDmg") &&
            !PhobosAgriculture.Definitions.MachineDefinition("ItmAirPumpInstalled") && !PhobosAgriculture.Definitions.MachineDefinition(null), "Agriculture's world family is its machines by definition");
    }

    /// <summary>True when the action allocates nothing on the current thread in at least one of three attempts. The counter also
    /// sees the test runtime's own tiered-compilation work landing inside the window, which is not the code under test; code
    /// that really allocates per call does so on every attempt and still fails.</summary>
    internal static bool AllocatesNothing(Action action)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            if (GC.GetAllocatedBytesForCurrentThread() == before) return true;
            System.Threading.Thread.Sleep(200);
        }
        return false;
    }
}
