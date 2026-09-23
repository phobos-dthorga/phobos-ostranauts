using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Phobos.Ostranauts.Framework;

namespace PhobosShipbreaker;

[BepInPlugin(Id, "Phobos Shipbreaker", Version)]
[BepInProcess("Ostranauts.exe")]
[BepInDependency(FrameworkInfo.PluginId, Core.DependencyContract.MinimumFramework)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "phobosgekko.ostranauts.shipbreaker";
    public const string Version = "0.6.0";
    internal static ProcessingService Service { get; private set; } = null!;
    internal static Action<string> Log { get; private set; } = null!;
    internal static Settings Options { get; private set; } = null!;
    internal static CollectorService Collectors { get; private set; } = null!;
    internal static CollectorPanel CollectorControls { get; private set; } = null!;
    private Harmony? harmony;
    private FixturePanel panel = null!;

    private void Awake()
    {
        Log = text => Logger.LogInfo(text);
        Options = new Settings(Config);
        Service = new ProcessingService(Log, Options);
        Collectors = new CollectorService(Log, Options);
        CollectorControls = new CollectorPanel(Collectors);
        panel = new FixturePanel(Service, Options);
        harmony = new Harmony(Id);
        harmony.PatchAll(typeof(Plugin).Assembly);
        FrameworkLifecycle.ContentLoading += LoadContent;
        FrameworkLifecycle.ContentLoaded += ConfirmContent;
        Log("Shipbreaker loaded with independent Phobos Framework construction and machinery. " + Options.ControlsKey + ": fixture controls.");
    }
    private void Update() => panel.Update();
    private void OnGUI() { panel.Draw(); CollectorControls.Draw(); }
    internal static void ResetServices() { Service.Reset(); Collectors.Reset(); CollectorControls.Reset(); }
    private static void LoadContent() { ResetServices(); Content.Register(Log); }
    private static void ConfirmContent() => Content.ConfirmRecipes(Log);
    private void OnDestroy()
    {
        FrameworkLifecycle.ContentLoading -= LoadContent; FrameworkLifecycle.ContentLoaded -= ConfirmContent;
        Service?.Reset(); Collectors?.Reset(); harmony?.UnpatchSelf();
    }
}

[HarmonyPatch(typeof(Powered), "UsePower", new[] { typeof(CondOwner), typeof(double) })]
internal static class PowerPatch
{
    private static void Prefix(CondOwner __0, out bool __state) => __state =
        __0 != null && (Content.IsMachine(__0.strCODef) && __0.HasCond(Core.ProcessRules.Working) ||
            ProcessingService.IsGrabber(__0) && __0.HasCond(Core.IntakeRules.Working) ||
            Core.CollectorRules.IsFamily(__0.strCODef) && __0.HasCond(Core.CollectorRules.Working));
    private static void Postfix(CondOwner __0, bool __state)
    {
        if (__0 == null) return;
        if (Core.CollectorRules.IsFamily(__0.strCODef)) Plugin.Collectors.AfterPower(__0, __state);
        else if (ProcessingService.IsGrabber(__0)) Plugin.Service.AfterIntakePower(__0, __state);
        else Plugin.Service.AfterPower(__0, __state);
    }
}

// Clear stale work demand before the native path selects its active/idle coefficient.
[HarmonyPatch(typeof(Powered), "Run")]
internal static class PowerDemandPatch
{
    private static void Prefix(Powered __instance)
    {
        var machine = __instance.CO;
        if (machine == null) return;
        if (Core.CollectorRules.IsFamily(machine.strCODef))
        {
            try { Plugin.Collectors.BeforePower(machine); }
            catch (Exception ex) { Plugin.Collectors.Fault(machine, ex); }
            return;
        }
        if (ProcessingService.IsGrabber(machine))
        {
            try { Plugin.Service.BeforeIntakePower(machine); }
            catch (Exception ex) { Plugin.Service.IntakeFault(machine, ex); }
            return;
        }
        if (!Content.IsMachine(machine.strCODef)) return;
        try { Plugin.Service.BeforePower(machine); }
        catch (Exception ex) { Plugin.Service.Fault(machine, ex); }
    }
}

[HarmonyPatch(typeof(Container), nameof(Container.AllowedCO))]
internal static class FeedPatch
{
    private static void Postfix(Container __instance, CondOwner coIn, ref bool __result)
    {
        if (__result && __instance.CO != null && __instance.CO.strCODef == Content.InputBin)
            __result = ProcessingService.CanFeed(__instance.CO, coIn);
        if (__result && __instance.CO != null && Core.CollectorRules.IsFamily(__instance.CO.strCODef))
            __result = CollectorService.CanAccept(__instance.CO, coIn);
    }
}

[HarmonyPatch]
internal static class ReloadPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
        typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame));
    private static void Prefix() => Plugin.ResetServices();
}
