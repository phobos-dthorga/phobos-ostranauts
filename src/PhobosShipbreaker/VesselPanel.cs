using Phobos.Ostranauts.Framework.Controls;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>The T2 thaw unit and the ML-2 mining laser on Framework's shared Control Panel (Shipbreaker 0.63.0; owner
/// decision, 1 October 2026), the panel Manufacturing's machines and the water silos already use. Both are presented
/// entirely by <see cref="VesselProvider"/>, so nothing but the host changes: the same choices, commands and access
/// rule as the industrial panel gave them. Each machine has its own page names. The seated C1 console still lists
/// both through the industrial panel.</summary>
internal static class VesselPanel
{
    internal const string ThawKey = "PhobosShipbreakerThawPanel", LaserKey = "PhobosShipbreakerLaserPanel";

    internal static void Register(VesselProvider provider)
    {
        ProviderPanel.Register(Spec(ThawKey, provider, "Thaw", ThawRules.IsFamily));
        ProviderPanel.Register(Spec(LaserKey, provider, "Laser", LaserRules.IsFamily));
    }
    private static ProviderPanelSpec Spec(string key, VesselProvider provider, string texts, System.Func<string?, bool> family) => new()
    {
        Key = key, Provider = provider,
        Handles = co => co != null && !co.bDestroyed && co.HasCond("IsInstalled") && family(co.strCODef),
        Access = co => CollectorService.EndpointAccess(co), Resolve = CollectorService.Resolve,
        Text = name => Text.Get(texts + ".panel_" + name), Log = text => Plugin.Log(text), StopAction = "pause"
    };
    /// <summary>Opens the shared panel for a T2 or an ML-2; false when it is neither or the panel cannot open now.</summary>
    internal static bool Show(CondOwner co) =>
        ThawRules.IsFamily(co.strCODef) ? ProviderPanel.Show(ThawKey, co) : LaserRules.IsFamily(co.strCODef) && ProviderPanel.Show(LaserKey, co);
}
