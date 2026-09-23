using BepInEx;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework;

/// <summary>Dependency identity for Ostranauts mods using this shared assembly.</summary>
public static class FrameworkInfo
{
    public const string PluginId = "phobosgekko.ostranauts.framework";
    public const string Version = "0.2.1";
}

[BepInPlugin(FrameworkInfo.PluginId, "Phobos Framework", FrameworkInfo.Version)]
[BepInProcess("Ostranauts.exe")]
public sealed class FrameworkPlugin : BaseUnityPlugin
{
    private Harmony? harmony;
    private void Awake()
    {
        FrameworkLifecycle.Log = message => Logger.LogInfo(message);
        harmony = new Harmony(FrameworkInfo.PluginId);
        harmony.PatchAll(typeof(FrameworkPlugin).Assembly);
        Logger.LogInfo("Phobos Framework " + FrameworkInfo.Version + ": construction, registration and inventory services loaded. Conveyor transport is not yet implemented.");
    }
    private void OnDestroy() => harmony?.UnpatchSelf();
}
