using Phobos.Ostranauts.Framework.Controls;

namespace PhobosMedical;

/// <summary>The Ward-3's Control Panel: Framework's shared <see cref="ProviderPanel"/> over our provider.</summary>
internal static class Panel
{
    internal const string Key = "PhobosMedicalPanel";
    private static readonly Provider provider = new();
    internal static void Register() => ProviderPanel.Register(new ProviderPanelSpec
    {
        Key = Key, Provider = provider, Handles = co => Content.Machine(co), Access = co => Content.Access(co),
        Resolve = id => Content.Resolve(id), Text = key => Text.Get("Panel." + key), Log = Plugin.Log, StopAction = "status"
    });
    internal static bool Show(CondOwner co) => ProviderPanel.Show(Key, co);
}
