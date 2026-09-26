using BepInEx;
using HarmonyLib;
using System;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using Phobos.Ostranauts.Framework.Localization;

namespace Phobos.Ostranauts.Framework;

/// <summary>Dependency identity for Ostranauts mods using this shared assembly.</summary>
public static class FrameworkInfo
{
    public const string PluginId = "phobosgekko.ostranauts.framework";
    public const string Version = "0.24.2";
}

[BepInPlugin(FrameworkInfo.PluginId, "Phobos Framework", FrameworkInfo.Version)]
[BepInProcess("Ostranauts.exe")]
public sealed class FrameworkPlugin : BaseUnityPlugin
{
    private Harmony? harmony;
    private static ConfigEntry<string>? language;
    internal static void RefreshLanguage()
    {
        if (language == null) return;
        string selected = language.Value.Trim();
        if (string.Equals(selected, "auto", StringComparison.OrdinalIgnoreCase))
        {
            string native = Localisation.Get();
            selected = CultureInfo.GetCultures(CultureTypes.AllCultures).FirstOrDefault(c =>
                string.Equals(c.EnglishName, native, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.NativeName, native, StringComparison.OrdinalIgnoreCase))?.Name ?? native;
        }
        Translations.Select(selected);
    }
    private void Awake()
    {
        Translations.Log = message => Logger.LogWarning(message);
        Translations.UserDirectory = System.IO.Path.Combine(Paths.ConfigPath, "PhobosTranslations");
        language = Config.Bind("Localization", "Language", "auto",
            Text.Get("Plugin.language_tag_such_as_en_fr_or"));
        RefreshLanguage();
        try { Audio.CompletionCues.Player = new Audio.CompletionAudio(gameObject, Config, message => Logger.LogWarning(message)); }
        catch (Exception ex) { Logger.LogWarning("Optional completion audio unavailable: " + ex.Message); }
        Trading.MarketStock.AvailabilityMultiplier = Config.Bind("Economy", "StockAvailabilityMultiplier", 1d,
            new BepInEx.Configuration.ConfigDescription(Text.Get("Plugin.chance_multiplier_for_registered_equipment_offers_to"),
                new BepInEx.Configuration.AcceptableValueRange<double>(.25, 4))).Value;
        FrameworkLifecycle.Log = message => Logger.LogInfo(message);
        Diagnostics.NativePerformance.Initialize(message => Logger.LogWarning(message));
        harmony = new Harmony(FrameworkInfo.PluginId);
        harmony.PatchAll(typeof(FrameworkPlugin).Assembly);
        Logger.LogInfo(Text.Get("Plugin.phobos_framework_construction_registration_physical_transfers_filters", FrameworkInfo.Version));
    }
    private void Update() { Diagnostics.NativePerformance.Poll(); Audio.CompletionCues.Player?.Poll(); }
    private void OnApplicationQuit() => Diagnostics.NativePerformance.Shutdown();
    private void OnDestroy() { Audio.CompletionCues.Player?.Dispose(); Audio.CompletionCues.Player = null; Diagnostics.NativePerformance.Shutdown(); harmony?.UnpatchSelf(); }
}
