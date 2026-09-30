using Phobos.Ostranauts.Framework.Controls;

namespace PhobosManufacturing;

/// <summary>Manufacturing's Control Panel: Framework's shared <see cref="ProviderPanel"/> host (Framework 0.56.0)
/// over our provider, with our access rule, object resolution and page texts. The native GUI key is unchanged, so a
/// panel that was open when the game saved still reopens.</summary>
internal static class Panel
{
    internal const string Key = "PhobosManufacturingPanel";
    private static readonly Provider provider = new();
    internal static void Register() => ProviderPanel.Register(new ProviderPanelSpec
    {
        Key = Key, Provider = provider, Handles = co => Content.Machine(co), Access = co => Content.Access(co),
        Resolve = id => Content.Resolve(id), Text = key => Text.Get("Panel." + key), Log = Plugin.Log
    });
    internal static bool Show(CondOwner co) => ProviderPanel.Show(Key, co);
}
