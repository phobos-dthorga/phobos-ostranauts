using BepInEx;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework;

/// <summary>Dependency identity for Ostranauts mods using this shared assembly.</summary>
public static class FrameworkInfo
{
    public const string PluginId = "phobosgekko.ostranauts.framework";
    public const string Version = "0.6.0";
}

[BepInPlugin(FrameworkInfo.PluginId, "Phobos Framework", FrameworkInfo.Version)]
[BepInProcess("Ostranauts.exe")]
public sealed class FrameworkPlugin : BaseUnityPlugin
{
    private Harmony? harmony;
    private void Awake()
    {
        Trading.MarketStock.AvailabilityMultiplier = Config.Bind("Economy", "StockAvailabilityMultiplier", 1d,
            new BepInEx.Configuration.ConfigDescription("Chance multiplier for registered equipment offers, 0.25 to 4. Each offer remains at most one item. Requires restart and normal trader restock.",
                new BepInEx.Configuration.AcceptableValueRange<double>(.25, 4))).Value;
        FrameworkLifecycle.Log = message => Logger.LogInfo(message);
        harmony = new Harmony(FrameworkInfo.PluginId);
        harmony.PatchAll(typeof(FrameworkPlugin).Assembly);
        Logger.LogInfo("Phobos Framework " + FrameworkInfo.Version + ": construction, registration, physical transfers, filters, clocks, grid routing and saved port pairing loaded. Machine rules remain in content mods.");
    }
    private void OnDestroy() => harmony?.UnpatchSelf();
}
