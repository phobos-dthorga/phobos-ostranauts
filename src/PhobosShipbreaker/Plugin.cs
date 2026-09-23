using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace PhobosShipbreaker;

[BepInPlugin(Id, "Phobos Shipbreaker", Version)]
[BepInProcess("Ostranauts.exe")]
[BepInDependency(FrameworkId, Core.DependencyContract.MinimumFramework)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "phobosgekko.ostranauts.shipbreaker";
    public const string Version = "0.1.2";
    public const string FrameworkId = "community.ostranauts.craftingframework";
    internal static ProcessingService Service { get; private set; } = null!;
    internal static Action<string> Log { get; private set; } = null!;
    internal static Settings Options { get; private set; } = null!;
    private Harmony? harmony;
    private FixturePanel panel = null!;

    private void Awake()
    {
        Log = text => Logger.LogInfo(text);
        Options = new Settings(Config);
        Service = new ProcessingService(Log, Options);
        panel = new FixturePanel(Service, Options);
        harmony = new Harmony(Id);
        harmony.PatchAll(typeof(Plugin).Assembly);
        Log("Shipbreaker loaded. Requires enabled Phobos Shipbreaker and Salvage Workshop data. " + Options.ControlsKey + ": fixture controls.");
    }
    private void Update() => panel.Update();
    private void OnGUI() => panel.Draw();
    private void OnDestroy() { Service?.Reset(); harmony?.UnpatchSelf(); }
}

[HarmonyPatch(typeof(DataHandler), "PostModLoadMainThread")]
[HarmonyBefore(Plugin.FrameworkId)]
internal static class ContentPatch
{
    private static void Prefix() { Plugin.Service.Reset(); Content.Register(Plugin.Log); }
    // OCF registers recipes in its prefix; the native method then finishes loading.
    private static void Postfix() => Content.ConfirmRecipes(Plugin.Log);
}

[HarmonyPatch(typeof(Powered), "UsePower", new[] { typeof(CondOwner), typeof(double) })]
internal static class PowerPatch
{
    private static void Prefix(CondOwner __0, out bool __state) => __state =
        __0 != null && Content.IsMachine(__0.strCODef) && __0.HasCond(Core.ProcessRules.Working);
    private static void Postfix(CondOwner __0, bool __state) => Plugin.Service.AfterPower(__0, __state);
}

// Clear stale work demand before the native path selects its active/idle coefficient.
[HarmonyPatch(typeof(Powered), "Run")]
internal static class PowerDemandPatch
{
    private static void Prefix(Powered __instance)
    {
        var machine = __instance.CO;
        if (machine == null || !Content.IsMachine(machine.strCODef)) return;
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
    }
}

[HarmonyPatch]
internal static class ReloadPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
        typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame));
    private static void Prefix() => Plugin.Service.Reset();
}
