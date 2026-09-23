using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>Presentation only. All gameplay changes are delegated to the service.</summary>
internal sealed class FixturePanel
{
    private readonly ProcessingService service;
    private readonly Settings options;
    private bool visible;
    private Rect bounds = new Rect(40, 100, 600, 580);
    private Vector2 scroll;
    private CondOwner[] machines = Array.Empty<CondOwner>();
    private readonly Dictionary<string, string> intakeDescriptions = new Dictionary<string, string>();
    private float refresh;
    internal FixturePanel(ProcessingService service, Settings options) { this.service = service; this.options = options; }
    internal void Update()
    {
        if (!CrewSim.Typing && Input.GetKeyDown(options.ControlsKey)) visible = !visible;
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading)
        { machines = Array.Empty<CondOwner>(); return; }
        if (!visible || Time.unscaledTime < refresh) return;
        refresh = Time.unscaledTime + 0.5f;
        machines = ProcessingService.FindMachines();
        intakeDescriptions.Clear();
        foreach (var machine in machines) intakeDescriptions[machine.strID] = service.DescribeIntake(machine);
    }
    internal void Draw()
    {
        if (visible && CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading)
            bounds = GUI.Window(847292, bounds, Window, "Phobos Shipbreaker");
    }
    private void Window(int id)
    {
        GUILayout.Label("4 x 4 fixture | 4-panel feed | 8 x 8 output tray\nNew panel: " + options.CycleSeconds +
            " seconds, " + options.WorkingKW + " kW while working.");
        GUILayout.Label("Load detached ordinary walls at the exterior grabber using Inventory. Start here to move them through the wall chute and process them. Collect products from this fixture's Inventory. Stand beside the fixture for controls.");
        if (!Content.Ready) GUILayout.Label(Content.Status);
        else if (machines.Length == 0) GUILayout.Label("No fixture on this ship. Build one at an installed Bar Table or Dining Table, then install it. A Salvage Workshop workbench also works when available.");
        scroll = GUILayout.BeginScrollView(scroll);
        foreach (var machine in machines)
        {
            if (machine == null || machine.bDestroyed) continue;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(machine.strNameFriendly + " (" + machine.strID + ")");
            GUILayout.Label(service.Describe(machine));
            if (intakeDescriptions.TryGetValue(machine.strID, out string intakeDescription)) GUILayout.Label(intakeDescription);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Start / resume pipeline")) service.Start(machine);
            if (GUILayout.Button("Pause")) service.Pause(machine, false);
            if (GUILayout.Button("Cancel work")) service.Pause(machine, true);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Collect products")) service.OpenInventory(machine, false);
            if (GUILayout.Button("Manual feed (fallback)")) service.OpenInventory(machine, true);
            GUILayout.EndHorizontal();
            GUILayout.Label(CollectorService.DescribeLink(machine));
            if (GUILayout.Button("Residue destination / unlink")) Plugin.CollectorControls.ShowSource(machine);
            GUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
        GUILayout.Label("Per panel: 2 mechanical parts, 2 aluminium, 2 carbon-fibre, 6 steel, and one 13 kg residue item. Total: 24 kg.");
        GUILayout.Label("Queue continuation: " + (options.ContinueQueue ? "automatic" : "one panel per Start"));
        if (GUILayout.Button("Close")) visible = false;
        GUI.DragWindow(new Rect(0, 0, bounds.width, 24));
    }
}
