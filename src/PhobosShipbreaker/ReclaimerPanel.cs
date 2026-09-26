using System;
using System.Linq;
using HarmonyLib;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

// Presentation only. All inventory, work and routing changes use existing services.
internal sealed class ReclaimerPanel
{
    private const int WindowId = 847294;
    private CondOwner? target;
    private Rect bounds = new Rect(160, 100, 640, 470);
    private Vector2 scroll;
    internal void Reset() => target = null;
    internal bool Show(CondOwner machine)
    {
        if (!ReclaimerRules.IsFamily(machine.strCODef) || ProcessingService.AccessProblem(machine) != null) return false;
        target = machine; return true;
    }
    internal void Draw()
    {
        if (target == null || target.bDestroyed || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return;
        bounds = GUI.Window(WindowId, bounds, Window, Text.Get("Reclaimer.name"));
    }
    private void Window(int id)
    {
        var machine = target!; var service = Plugin.Service;
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label(CollectorService.Label(machine));
        GUILayout.Label(Text.Get("Reclaimer.panel_budget", Plugin.Options.ReclaimerSeconds, Plugin.Options.ReclaimerKW,
            ReclaimerRules.InputKg, ReclaimerRules.RejectKg));
        GUILayout.Label(service.Describe(machine));
        if (GUILayout.Button(Phobos.Ostranauts.Framework.Crew.CrewWork.Message("open")) && Phobos.Ostranauts.Framework.Crew.CrewPanel.Show(machine)) target=null;
        GUILayout.Label(Text.Get("Reclaimer.cooling_hint", ReclaimerRules.MaxRoomKelvin - Phobos.Ostranauts.Framework.Units.CelsiusToKelvin, ReclaimerRules.MinPressureKPa));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(Text.Get("Reclaimer.start"))) service.Start(machine);
        if (GUILayout.Button(Text.Get("FixturePanel.pause"))) service.Pause(machine, false);
        if (GUILayout.Button(Text.Get("FixturePanel.cancel_work"))) service.Pause(machine, true);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(Text.Get("Reclaimer.feed"))) service.OpenInventory(machine, true);
        if (GUILayout.Button(Text.Get("FixturePanel.collect_products"))) service.OpenInventory(machine, false);
        GUILayout.EndHorizontal();
        GUILayout.Label(CollectorService.DescribeLink(machine, false));
        GUILayout.Label(CollectorService.DescribeLink(machine, true));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(Text.Get("Routing.input_port"))) Plugin.CollectorControls.Show(machine);
        if (GUILayout.Button(Text.Get("Routing.output_port"))) Plugin.CollectorControls.ShowSource(machine);
        GUILayout.EndHorizontal();
        GUILayout.Label(Text.Get("Routing.queue_hint"));
        if (GUILayout.Button(Text.Get("Industry.action_watch"))) service.WatchCompletion(machine, true);
        if (GUILayout.Button(Text.Get("Industry.action_unwatch"))) service.WatchCompletion(machine, false);
        if (GUILayout.Button(Phobos.Ostranauts.Framework.Audio.CompletionCues.VolumeLabel)) Phobos.Ostranauts.Framework.Audio.CompletionCues.CycleVolume();
        GUILayout.EndScrollView();
        if (GUILayout.Button(Text.Get("FixturePanel.close"))) target = null;
        GUI.DragWindow(new Rect(0, 0, bounds.width, 24));
    }
    internal static bool Command(string input, out bool result, out string response)
    {
        result = false; response = "";
        var words = input.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !words[0].Equals("phobosreclaimer", StringComparison.OrdinalIgnoreCase)) return false;
        var action = words.Length > 1 ? words[1].ToLowerInvariant() : "help";
        if (action == "help" && words.Length <= 2) { result = true; response = Text.Get("Reclaimer.help"); return true; }
        if (words.Length > 3 || !new[] { "status", "start", "pause", "cancel", "feed", "products", "controls" }.Contains(action))
        { response = Text.Get("Reclaimer.help"); return true; }
        var machines = ProcessingService.FindMachines(true).Where(m => ReclaimerRules.IsFamily(m.strCODef) &&
            (words.Length < 3 || m.strID.Equals(words[2], StringComparison.OrdinalIgnoreCase))).ToArray();
        if (action == "status")
        {
            response = machines.Length == 0 ? Text.Get("Reclaimer.none") : string.Join("\n\n", machines.Select(m => m.strID + "\n" + Plugin.Service.Describe(m)));
            result = true; return true;
        }
        if (machines.Length != 1) { response = Text.Get("Reclaimer.select_one"); return true; }
        var machine = machines[0];
        switch (action)
        {
            case "start": result = Plugin.Service.Start(machine); break;
            case "pause": result = Plugin.Service.Pause(machine, false); break;
            case "cancel": result = Plugin.Service.Pause(machine, true); break;
            case "feed": result = Plugin.Service.OpenInventory(machine, true); break;
            case "products": result = Plugin.Service.OpenInventory(machine, false); break;
            case "controls": result = Plugin.ReclaimerControls.Show(machine); break;
        }
        response = Plugin.Service.Describe(machine); return true;
    }
}

[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class ReclaimerControlsPatch
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (!isCancelIa && __instance.strName == ReclaimerRules.Controls && __instance.objUs == CrewSim.GetSelectedCrew() && __instance.objThem != null)
            Plugin.ReclaimerControls.Show(__instance.objThem);
    }
}
