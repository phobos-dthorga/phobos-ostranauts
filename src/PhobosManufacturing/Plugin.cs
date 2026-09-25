using BepInEx;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Localization;

namespace PhobosManufacturing;

[BepInPlugin(Id, "Phobos Manufacturing", Version)]
[BepInDependency(FrameworkInfo.PluginId, MinimumFrameworkVersion)]
[BepInProcess("Ostranauts.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "phobosgekko.ostranauts.manufacturing";
    public const string Version = "0.0.1";
    public const string MinimumFrameworkVersion = "0.17.0";

    private void Awake()
    {
        var text = Translations.Register(Id, typeof(Plugin).Assembly, "PhobosManufacturing.en.json");
        Logger.LogInfo(text.Get("Scaffold.loaded", Version));
    }
}
