using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Framework 0.116.0: every mod's right-click maintenance entry keeps its interaction id and is now titled
/// Maintenance; installed machines carry it beside their Control Panel.</summary>
internal static class MaintenanceSheetNativeChecks
{
    private static readonly string[] Ids =
    {
        "PhobosFrameworkMaintenanceInformation", "PhobosShipbreakerMaintenanceInformation", "PhobosAgricultureMaintenanceInformation",
        "PhobosManufacturingMaintenanceInformation", "PhobosMedicalMaintenanceInformation"
    };
    internal static void Run(IEnumerable<NativeDefinitions> definitions, Action<bool, string> check)
    {
        var sets = definitions.ToArray();
        foreach (string id in Ids)
        {
            var interaction = sets.Select(d => d.Interactions.TryGetValue(id, out var found) ? found : null).FirstOrDefault(i => i != null);
            check(interaction != null && interaction.strTitle == "Maintenance", "The maintenance entry keeps its id and is called Maintenance: " + id);
        }
        check(sets.SelectMany(d => d.Objects.Values).First(o => o.strName == "PhobosVolatilesRefineryInstalled").aInteractions.Contains("PhobosManufacturingMaintenanceInformation"),
            "The V4 carries the Maintenance entry");
    }
}
