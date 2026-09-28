using System;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Explain hidden maintenance without granting work permission or changing equipment.</summary>
public static class MaintenanceInformation
{
    public static void Register(NativeDefinitions definitions, string action, Func<CondOwner, string>? extra = null)
    {
        var ids = definitions.Installables.Values.Where(j => j.strJobType != "install")
            .Select(j => j.strActionCO).Where(definitions.Objects.ContainsKey).Distinct().ToArray();
        Controls.ItemInformation.Register(definitions, action, Text.Get("MaintenanceInfo.title"), ids, co => {
            string? bin = MaintenanceSafety.Actions.TryGetValue("ACT" + co.strCODef + "Dismantle", out var internalBin) ? internalBin : null;
            string message = MaintenanceSafety.Reason(co, bin) ?? Text.Get("MaintenanceInfo.empty");
            return Text.Get("MaintenanceInfo.basics") + "\n\n" + message +
                (extra == null ? "" : "\n\n" + extra(co));
        });
    }
}
