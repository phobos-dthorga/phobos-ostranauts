using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>Presentation only. All gameplay changes are delegated to the service.</summary>
internal sealed class FixturePanel
{
    private const int WindowId = 847292;
    private const float TitleBarHeight = 24;
    private readonly ProcessingService service;
    private readonly Settings options;
    private bool visible;
    private Rect bounds = new Rect(40, 100, 600, 580);
    private Vector2 scroll;
    private CondOwner[] machines = Array.Empty<CondOwner>();
    private readonly Dictionary<string, string> intakeDescriptions = new Dictionary<string, string>();
    private const float RefreshSeconds = 0.5f;
    private float refresh;
    internal FixturePanel(ProcessingService service, Settings options) { this.service = service; this.options = options; }
    internal void Update()
    {
        if (!CrewSim.Typing && Input.GetKeyDown(options.ControlsKey)) visible = !visible;
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading)
        { machines = Array.Empty<CondOwner>(); return; }
        if (!visible || Time.unscaledTime < refresh) return;
        refresh = Time.unscaledTime + RefreshSeconds;
        machines = ProcessingService.FindMachines();
        intakeDescriptions.Clear();
        foreach (var machine in machines) intakeDescriptions[machine.strID] = service.DescribeIntake(machine);
    }
    internal void Draw()
    {
        if (visible && CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading)
            bounds = GUI.Window(WindowId, bounds, Window, "Phobos Shipbreaker");
    }
    private void Window(int id)
    {
        GUILayout.Label(Text.Get("FixturePanel.x_fixture_panel_feed_x_output_tray", options.CycleSeconds, options.WorkingKW, Core.ProcessRules.Footprint, Core.ProcessRules.FeedCapacity, Core.ProcessRules.OutputSize));
        GUILayout.Label(Text.Get("FixturePanel.load_detached_ordinary_walls_at_the_exterior"));
        if (!Content.Ready) GUILayout.Label(Content.Status);
        else if (machines.Length == 0) GUILayout.Label(Text.Get("FixturePanel.no_fixture_on_this_ship_build_one"));
        scroll = GUILayout.BeginScrollView(scroll);
        foreach (var machine in machines)
        {
            if (machine == null || machine.bDestroyed) continue;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(machine.strNameFriendly + " (" + machine.strID + ")");
            GUILayout.Label(service.Describe(machine));
            if (intakeDescriptions.TryGetValue(machine.strID, out string intakeDescription)) GUILayout.Label(intakeDescription);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Text.Get("FixturePanel.start_resume_pipeline"))) service.Start(machine);
            if (GUILayout.Button(Text.Get("FixturePanel.pause"))) service.Pause(machine, false);
            if (GUILayout.Button(Text.Get("FixturePanel.cancel_work"))) service.Pause(machine, true);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Text.Get("FixturePanel.collect_products"))) service.OpenInventory(machine, false);
            if (GUILayout.Button(Text.Get("FixturePanel.manual_feed_fallback"))) service.OpenInventory(machine, true);
            GUILayout.EndHorizontal();
            GUILayout.Label(CollectorService.DescribeLink(machine));
            if (GUILayout.Button(Text.Get("FixturePanel.residue_destination_unlink"))) Plugin.CollectorControls.ShowSource(machine);
            GUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
        GUILayout.Label(Text.Get("FixturePanel.per_panel_mechanical_parts_aluminium_carbon_fibre", ProcessingService.NewPanelProducts(), Core.ProcessRules.InputKg));
        GUILayout.Label(Text.Get("FixturePanel.queue_continuation", (options.ContinueQueue ? Text.Get("FixturePanel.automatic") : Text.Get("FixturePanel.one_panel_per_start"))));
        if (GUILayout.Button(Text.Get("FixturePanel.close"))) visible = false;
        GUI.DragWindow(new Rect(0, 0, bounds.width, TitleBarHeight));
    }
}
