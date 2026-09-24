using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace PhobosApproachAssist;

[BepInPlugin(Id, "Phobos Approach Assist (prototype)", Version)]
[BepInProcess("Ostranauts.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "phobosgekko.ostranauts.approachassist";
    public const string Version = "0.1.2";
    internal static ApproachService Service { get; private set; } = null!;
    private Harmony? harmony;
    private bool showTestTools;
    private Rect toolsRect = new Rect(30, 100, 460, 260);

    private void Awake()
    {
        Service = new ApproachService(message => Logger.LogInfo(message));
        harmony = new Harmony(Id);
        harmony.PatchAll(typeof(Plugin).Assembly);
        Logger.LogInfo("Prototype loaded. F3: phobosapproach help. F8 opens test tools. Burns and item creation require a PhobosApproachAssistTest save.");
    }

    private void Update()
    {
        if (!CrewSim.Typing && Input.GetKeyDown(KeyCode.F8)) showTestTools = !showTestTools;
    }

    private void OnGUI()
    {
        if (showTestTools) toolsRect = GUI.Window(847291, toolsRect, DrawTestTools, "Phobos Approach Assist — test tools");
    }

    private void DrawTestTools(int id)
    {
        GUILayout.Label("Integration prototype: a two-second RCS pulse. No automatic approach or braking yet.");
        GUILayout.Label("Use a separate test world saved as PhobosApproachAssistTest. Open its nav console.");
        GUILayout.Label("F3 console: phobosapproach help / status / spawn / pulse / stop");
        GUILayout.Label(Service.Status);
        if (GUILayout.Button("Add test module to this console")) Service.AddTestModule();
        if (GUILayout.Button("Add damaged test module to this console")) Service.AddTestModule(damaged: true);
        if (GUILayout.Button("Disengage")) Service.Disengage("Disengaged by pilot");
        GUI.DragWindow(new Rect(0, 0, 460, 24));
    }

    private void OnDestroy()
    {
        Service?.Disengage("Plugin unloaded");
        harmony?.UnpatchSelf();
    }
}

[HarmonyPatch(typeof(GUIOrbitDraw), "LoadModules")]
internal static class ModulePatch
{
    private static void Prefix(GUIOrbitDraw __instance) => ApproachPanel.Ensure(__instance);
}

[HarmonyPatch(typeof(StarSystem), nameof(StarSystem.Update))]
internal static class SimulationPatch
{
    private static void Prefix(StarSystem __instance, double fTimeDelta) => Plugin.Service.BeforePhysics(__instance, fTimeDelta);
    private static void Postfix() => Plugin.Service.AfterPhysics();
    private static Exception? Finalizer(Exception? __exception)
    {
        if (__exception != null) Plugin.Service.Disengage("Simulation interrupted");
        return __exception;
    }
}

[HarmonyPatch(typeof(Ship), nameof(Ship.Maneuver))]
internal static class ManualInputPatch
{
    private static void Prefix(Ship __instance, float fX, float fY, float fR) =>
        Plugin.Service.ObserveExternalThrust(__instance, fX, fY, fR);
}

[HarmonyPatch]
internal static class LoadPatch
{
    private static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods() =>
        typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame));
    private static void Prefix() => Plugin.Service.Disengage("Load started; prototype disarmed");
}
