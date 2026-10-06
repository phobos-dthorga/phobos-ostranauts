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
    public const string Version = "0.123.0";
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
        Data.DataPacks.UserRoot = Paths.ConfigPath;
        Data.DataPacks.Log = message => Logger.LogWarning(message);
        // Add-ons players publish (0.90.0): every enabled game mod folder, in the game's own order, may hold one.
        Data.AddOns.Log = message => Logger.LogWarning(message);
        Translations.AddOnDirectories = Data.AddOns.TranslationFolders;
        Data.AddOns.EnabledModDirectories = () => DataHandler.dictModInfos?.Values.Where(m => m != null && !m.GetIsDisabled()).Select(m => m.GetDirectory()).Where(d => !string.IsNullOrEmpty(d)).ToArray() ?? Array.Empty<string>();
        FrameworkConsole.AddOns = () => Text.Get("FrameworkConsole.addons") + "\n" + Data.AddOns.Describe() + "\n" + Text.Get("FrameworkConsole.data_packs") + "\n" + Data.DataPacks.Describe();
        FrameworkConsole.ExtraStatus = () => Text.Get("FrameworkConsole.data_packs") + "\n" + Data.DataPacks.Describe() + "\n" + Text.Get("FrameworkConsole.addons") + "\n" + Data.AddOns.Describe();
        FrameworkConsole.Loot = Registration.LootCarveRegistry.Describe;
        FrameworkConsole.Story = Story.StoryContent.Command;
        FrameworkConsole.Upkeep = Crew.Upkeep.Command;
        // The Phobos operations articles and the Time-skip estimate (0.117.0), beside the panel buttons that use them.
        FrameworkConsole.Help = Controls.Help.Command;
        FrameworkConsole.Skip = words => CrewSim.GetSelectedCrew()?.ship is Ship ship ? Crew.CrewSkip.Preview(ship, words.Length >= 3 && int.TryParse(words[2], out int hours) ? hours : 1) : Text.Get("Help.no_game");
        // Crew upkeep (0.111.0): four player settings; the rest of its figures are the upkeep data pack.
        Crew.Upkeep.Settings = new Crew.UpkeepSettings
        {
            InspectionMinutes = Config.Bind("Upkeep", "InspectionMinutes", Crew.UpkeepSettings.DefaultInspectionMinutes, new BepInEx.Configuration.ConfigDescription(Text.Get("Upkeep.setting_inspection_minutes"),
                new BepInEx.Configuration.AcceptableValueRange<double>(Crew.UpkeepSettings.MinInspectionMinutes, Crew.UpkeepSettings.MaxInspectionMinutes))).Value,
            TuningMinutes = Config.Bind("Upkeep", "TuningMinutes", Crew.UpkeepSettings.DefaultTuningMinutes, new BepInEx.Configuration.ConfigDescription(Text.Get("Upkeep.setting_tuning_minutes"),
                new BepInEx.Configuration.AcceptableValueRange<double>(Crew.UpkeepSettings.MinTuningMinutes, Crew.UpkeepSettings.MaxTuningMinutes))).Value,
            MaxTuningGain = Config.Bind("Upkeep", "MaxTuningGain", Crew.UpkeepSettings.DefaultMaxTuningGain, new BepInEx.Configuration.ConfigDescription(Text.Get("Upkeep.setting_max_gain"),
                new BepInEx.Configuration.AcceptableValueRange<double>(Crew.UpkeepSettings.MinMaxTuningGain, Crew.UpkeepSettings.MaxMaxTuningGain))).Value,
            TuneFadeHours = Config.Bind("Upkeep", "TuneFadeHours", Crew.UpkeepSettings.DefaultTuneFadeHours, new BepInEx.Configuration.ConfigDescription(Text.Get("Upkeep.setting_fade_hours"),
                new BepInEx.Configuration.AcceptableValueRange<double>(Crew.UpkeepSettings.MinTuneFadeHours, Crew.UpkeepSettings.MaxTuneFadeHours))).Value
        }.Clamped();
        language = Config.Bind("Localization", "Language", "auto",
            Text.Get("Plugin.language_tag_such_as_en_fr_or"));
        RefreshLanguage();
        // Sounds are loose files beside the plugin (0.120.0); a same-named file in the config folder replaces one.
        Audio.SoundFiles.ShippedDirectory = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(FrameworkPlugin).Assembly.Location) ?? "", Audio.SoundFiles.Folder);
        Audio.SoundFiles.OverrideDirectory = System.IO.Path.Combine(Paths.ConfigPath, "PhobosFramework", Audio.SoundFiles.Folder);
        Audio.SoundFiles.Log = message => Logger.LogInfo(message);
        // Game-made faces (0.121.0) and the social text helper report a fault once, in the log.
        Social.Portraits.Log = message => Logger.LogWarning(message);
        Social.Grammar.Log = message => Logger.LogWarning(message);
        try { System.IO.Directory.CreateDirectory(Audio.SoundFiles.OverrideDirectory); }
        catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException) { Logger.LogWarning("Cannot create the sound replacement folder " + Audio.SoundFiles.OverrideDirectory + ": " + ex.Message); }
        try { Audio.CompletionCues.Player = new Audio.CompletionAudio(gameObject, Config, message => Logger.LogWarning(message)); }
        catch (Exception ex) { Logger.LogWarning("Optional completion audio unavailable: " + ex.Message); }
        // Machine work sounds (0.119.0): content mods register their machines; Framework plays them.
        try { Audio.MachineSounds.Player = new Audio.MachineAudio(Config, message => Logger.LogInfo(message)); }
        catch (Exception ex) { Logger.LogWarning("Optional machine sounds unavailable: " + ex.Message); }
        // Work carries on after a reload (0.95.0; owner decision, 5 October 2026): on by default.
        Persistence.ResumeAfterLoad.Enabled = Config.Bind("Persistence", "ResumeAfterLoad", true, Text.Get("ResumeAfterLoad.setting")).Value;
        // Time-skip step (0.112.0; owner report, 6 October 2026): the player trades the skip's freeze against its detail.
        Crew.CrewSkip.StepSeconds = Crew.CrewBalance.ClampSkipStep(Config.Bind("TimeSkip", "StepSeconds", Crew.CrewBalance.DefaultSkipStepSeconds,
            new BepInEx.Configuration.ConfigDescription(Text.Get("CrewSkip.setting_step"),
                new BepInEx.Configuration.AcceptableValueRange<double>(Crew.CrewBalance.MinSkipStepSeconds, Crew.CrewBalance.MaxSkipStepSeconds))).Value);
        // Machine heat (0.94.0; owner decision, 5 October 2026): one share for every Phobos machine, a quarter by default.
        Processing.RoomHeat.MachineHeatScale = Config.Bind("Heat", "MachineHeatScale", Processing.RoomHeat.DefaultMachineHeatScale,
            new BepInEx.Configuration.ConfigDescription(Text.Get("RoomHeat.setting"),
                new BepInEx.Configuration.AcceptableValueRange<double>(Processing.RoomHeat.MinMachineHeatScale, Processing.RoomHeat.MaxMachineHeatScale))).Value;
        // Fair gig deadlines (0.123.0; owner direction, 6 October 2026): a far gig allows what the player's torch ships need.
        Trading.GigDeadlines.Enabled = Config.Bind("Gigs", "FairDeadlines", true, Text.Get("Gigs.setting_fair")).Value;
        Trading.GigDeadlines.Margin = Config.Bind("Gigs", "TripMargin", Trading.GigTimeRules.DefaultMargin, new BepInEx.Configuration.ConfigDescription(Text.Get("Gigs.setting_margin"),
            new BepInEx.Configuration.AcceptableValueRange<double>(Trading.GigTimeRules.MinMargin, Trading.GigTimeRules.MaxMargin))).Value;
        Trading.GigDeadlines.DockingHours = Config.Bind("Gigs", "DockingHours", Trading.GigTimeRules.DefaultDockingHours, new BepInEx.Configuration.ConfigDescription(Text.Get("Gigs.setting_docking"),
            new BepInEx.Configuration.AcceptableValueRange<double>(Trading.GigTimeRules.MinDockingHours, Trading.GigTimeRules.MaxDockingHours))).Value;
        Trading.MarketStock.AvailabilityMultiplier = Config.Bind("Economy", "StockAvailabilityMultiplier", 1d,
            new BepInEx.Configuration.ConfigDescription(Text.Get("Plugin.chance_multiplier_for_registered_equipment_offers_to"),
                new BepInEx.Configuration.AcceptableValueRange<double>(.25, 4))).Value;
        FrameworkLifecycle.Log = message => Logger.LogInfo(message);
        FrameworkLifecycle.LogDebug = message => Logger.LogDebug(message);
        Sensors.SensorLeases.Log = message => Logger.LogWarning(message);
        Registration.LootCarveRegistry.Log = message => Logger.LogWarning(message);
        Diagnostics.NativePerformance.Initialize(message => Logger.LogWarning(message));
        harmony = new Harmony(FrameworkInfo.PluginId);
        harmony.PatchAll(typeof(FrameworkPlugin).Assembly);
        // Weightless care (0.84.0): applied by hand so a changed game method never stops the rest of Framework.
        Health.WoundGravity.Apply(harmony, message => Logger.LogWarning(message));
        FrameworkLifecycle.ContentLoaded += Crew.CrewSpecialities.Definitions;
        FrameworkLifecycle.ContentLoaded += Trading.FactionKiosks.Definitions;
        // Story packs (0.107.0), once every mod's items exist, so the names they use can be checked.
        FrameworkLifecycle.ContentLoaded += Story.StoryContent.Load;
        // Framework's own water tanks (0.58.0): their panel, console group, station water and the crew reserve setting,
        // which carries over the value a player set under Shipbreaker's Silo section the first time it is read here.
        Items.WaterTankService.Log = message => Logger.LogWarning(message);
        Items.WaterTankService.CrewReserveKg = Config.Bind("WaterTanks", "CrewWaterReserveKg", Items.WaterTankSettings.InitialReserve(Config.ConfigFilePath, Paths.ConfigPath),
            new BepInEx.Configuration.ConfigDescription(Text.Get("WaterTanks.setting_reserve"), new BepInEx.Configuration.AcceptableValueRange<double>(0, 100000))).Value;
        Items.WaterTankProvider.Register(message => Logger.LogWarning(message));
        // The item seen riding a conveyor belt (0.62.0): presentation only, on by default.
        Inventory.BeltCarriers.Enabled = Config.Bind("Belts", "ShowMovingItems", true, Text.Get("BeltCarriers.setting")).Value;
        Trading.BulkSupplies.Register(Items.WaterTankService.Supplies);
        Trading.BulkSupplies.RegisterBuyback(Items.WaterTankService.Buyback);
        Logger.LogInfo(Text.Get("Plugin.phobos_framework_construction_registration_physical_transfers_filters", FrameworkInfo.Version));
    }
    private void Update() { Diagnostics.NativePerformance.Poll(); Discovery.WorldFamilies.Poll(); Audio.CompletionCues.Player?.Poll(); Audio.MachineSounds.Player?.Poll(); Crew.CrewWork.Poll(); Liquids.BufferedDrains.Poll(); Liquids.LineContents.Poll(); Inventory.BeltCarriers.Poll(UnityEngine.Time.deltaTime); Persistence.LegacyItemConversions.Poll(); Persistence.ContainerFit.Poll(); Liquids.VesselContentsDisplay.Poll(); Data.DataFileNotice.Poll(); Story.StoryArcs.Poll(); }
    private void OnApplicationQuit() => Diagnostics.NativePerformance.Shutdown();
    private void OnDestroy() { FrameworkLifecycle.ContentLoaded -= Crew.CrewSpecialities.Definitions; FrameworkLifecycle.ContentLoaded -= Trading.FactionKiosks.Definitions; FrameworkLifecycle.ContentLoaded -= Story.StoryContent.Load; Audio.CompletionCues.Player?.Dispose(); Audio.CompletionCues.Player = null; Audio.MachineSounds.Player?.Dispose(); Audio.MachineSounds.Player = null; Diagnostics.NativePerformance.Shutdown(); harmony?.UnpatchSelf(); }
}
