using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Observations;
using PhobosWarDeclared.Core;

namespace PhobosWarDeclared;

[BepInPlugin(Id, "Phobos' War Has Been Declared", Version)]
[BepInDependency(FrameworkInfo.PluginId, MinimumFrameworkVersion)]
[BepInProcess("Ostranauts.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = WarRules.Owner;
    public const string Version = "0.3.0";
    public const string MinimumFrameworkVersion = "0.104.0";
    internal static Action<string> Log = _ => { };
    private static ConfigEntry<string>? schematic;
    private Harmony? harmony;
    private float nextPoll;

    private void Awake()
    {
        Log = x => Logger.LogInfo(x); Text.EnsureLoaded();
        PerformanceMetrics.Initialize();
        var options = new WarService.Settings
        {
            Enabled = Config.Bind("General", "Enabled", true, Text.Get("Config.enabled")).Value,
            QuietSeconds = 60 * Config.Bind("Battle", "QuietPeriodMinutes", WarRules.DefaultQuietMinutes, new ConfigDescription(Text.Get("Config.quiet"),
                new AcceptableValueRange<double>(WarRules.MinimumQuietMinutes, WarRules.MaximumQuietMinutes))).Value,
            DamageStartsBattle = Config.Bind("Battle", "DamageStartsBattle", true, Text.Get("Config.damage")).Value,
            LayDuringCombat = Config.Bind("Rebuild", "LayDuringCombat", false, Text.Get("Config.lay_during")).Value
        };
        WarService.Options = options;
        schematic = Config.Bind("Rebuild", "Schematic", WarRules.DefaultSchematic, Text.Get("Config.schematic"));
        Schematics.UserDirectory = Path.Combine(Paths.ConfigPath, "PhobosWarDeclared", "schematics");
        ReloadSchematics();
        harmony = new Harmony(Id); harmony.PatchAll(typeof(Plugin).Assembly);
        NativeCombat.EnsureListening();
        FrameworkLifecycle.ContentLoading += Load;
        Log(Text.Get("Plugin.loaded", Version, Schematics.Active.Key));
    }

    internal static string ReloadSchematics()
    {
        var chosen = Schematics.Load(schematic?.Value ?? WarRules.DefaultSchematic);
        foreach (var problem in Schematics.Problems) Log(problem);
        return chosen;
    }

    /// <summary>Remembers the player's choice in the settings file so it survives a restart.</summary>
    internal static bool SelectSchematic(string key, out string message)
    {
        if (!Schematics.Select(key, out message)) return false;
        if (schematic != null) schematic.Value = Schematics.Selected;
        return true;
    }

    private static void Load()
    {
        WarService.Reset();
        Phobos.Ostranauts.Framework.Construction.NativePlaceholders.Reset();
        Content.Register(Log);
    }

    private void Update()
    {
        if (UnityEngine.Time.unscaledTime < nextPoll) return;
        nextPoll = UnityEngine.Time.unscaledTime + WarRules.PollSeconds;
        if (!Content.Ready) return;
        try { WarService.Poll(); }
        catch (Exception ex) { Log(ex.ToString()); }
    }

    private void OnDestroy()
    {
        FrameworkLifecycle.ContentLoading -= Load;
        WarService.Reset(); harmony?.UnpatchSelf();
    }
}

// A damage check that reaches StatDamageMax queues the game's own switch and subtracts the maximum; note
// which objects it did that for, so later switches can be told apart from uninstalling or dismantling.
[HarmonyPatch(typeof(DestCheck), nameof(DestCheck.DamageCheck))]
internal static class DamageCheckPatch
{
    private static void Prefix(DestCheck __instance, CondOwner co, out double __state)
    {
        __state = double.NaN;
        if (co == null || __instance.strDamageCond != "StatDamage" || !Content.Ready) return;
        double amount = co.GetCondAmount("StatDamage");
        if (amount >= co.GetCondAmount(__instance.strDamageCondMax)) __state = amount;
    }
    private static void Postfix(DestCheck __instance, CondOwner co, double __state)
    {
        if (double.IsNaN(__state) || co == null) return;
        try
        {
            if (co.GetCondAmount("StatDamage") < __state - 1e-9)
                WarService.DamageQueued(co, DataHandler.GetLoot(__instance.strLootModeSwitch)?.GetLootNames() ?? new List<string>());
        }
        catch (Exception ex) { Plugin.Log(ex.Message); }
    }
}

// Observe the game's own switch; never alter or block it.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
internal static class ModeSwitchPatch
{
    private static void Prefix(CondOwner __instance, out WarService.Before? __state)
    {
        __state = null;
        try { __state = WarService.BeforeSwitch(__instance); }
        catch (Exception ex) { Plugin.Log(ex.Message); }
    }
    private static void Postfix(CondOwner coNew, WarService.Before? __state)
    {
        try { WarService.AfterSwitch(__state, coNew); }
        catch (Exception ex) { Plugin.Log(ex.Message); }
    }
}

[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.Destroy))]
internal static class DestroyPatch
{
    private static void Prefix(CondOwner __instance)
    {
        try { WarService.Destroying(__instance); }
        catch (Exception ex) { Plugin.Log(ex.Message); }
    }
}

[HarmonyPatch]
internal static class ReloadPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    private static void Prefix() => WarService.Reset();
}

// Orders are offered only when they would do something, and carried out through the service.
[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class OrderOffer
{
    private static void Postfix(Interaction __instance, CondOwner objThem, ref bool __result)
    {
        if (__result && Content.Orders.Contains(__instance.strName) && !Content.Offered(__instance.strName, objThem)) __result = false;
    }
}

[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class OrderFinish
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (isCancelIa || !Content.Orders.Contains(__instance.strName) || __instance.objThem == null || __instance.objUs != CrewSim.GetSelectedCrew()) return;
        bool done = Content.Carry(__instance.strName, __instance.objThem, out string message);
        var actor = __instance.objUs;
        if (actor != null && !actor.bDestroyed && actor.HasCond("IsHuman")) actor.LogMessage(message, done ? "Neutral" : "Bad", "Game");
    }
}

[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsolePatch
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        var parts = strInput.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !parts[0].Equals("phoboswar", StringComparison.OrdinalIgnoreCase)) return true;
        var ship = CrewSim.GetSelectedCrew()?.ship;
        string verb = parts.Length > 1 ? parts[1].ToLowerInvariant() : "status";
        string message;
        switch (verb)
        {
            case "status": message = WarService.Status(ship); __result = true; break;
            case "battle": __result = WarService.Declare(ship, out message); break;
            case "standdown": __result = WarService.StandDown(ship, out message); break;
            case "lay": __result = WarService.LayHeld(ship, out message); break;
            case "schematics": message = Schematics.Describe(); __result = true; break;
            case "schematic":
                if (parts.Length < 3) { message = Schematics.Describe(); __result = true; }
                else __result = Plugin.SelectSchematic(parts[2], out message);
                break;
            case "reload":
                Plugin.ReloadSchematics(); message = Schematics.Describe(); __result = true; break;
            default: message = Text.Get("Console.help"); __result = false; break;
        }
        strInput += "\n" + message;
        return false;
    }
}
