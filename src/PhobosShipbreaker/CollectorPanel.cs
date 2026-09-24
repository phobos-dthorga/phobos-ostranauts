using System.Linq;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>Presentation only; collection and access checks belong to the service.</summary>
internal sealed class CollectorPanel
{
    private const int WindowId = 847293;
    private const float TitleBarHeight = 24;
    private readonly CollectorService service;
    private CondOwner? target;
    private bool sourceMode;
    private string message = "";
    private Rect bounds = new Rect(180, 150, 620, 460);
    private Vector2 scroll;
    internal CollectorPanel(CollectorService service) => this.service = service;
    internal bool Show(CondOwner port)
    {
        if (CollectorService.AccessProblem(port) != null) return false;
        target = port; sourceMode = false; message = ""; return true;
    }
    internal bool ShowSource(CondOwner source)
    {
        if (ProcessingService.AccessProblem(source) != null) return false;
        target = source; sourceMode = true; message = ""; return true;
    }
    internal void Reset() => target = null;
    internal void Draw()
    {
        if (target == null || target.bDestroyed || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return;
        bounds = GUI.Window(WindowId, bounds, Window, sourceMode ? Text.Get("CollectorPanel.phobos_material_routing") : Text.Get("CollectorPanel.phobos_residue_collector"));
    }
    private void Window(int id)
    {
        var port = target!;
        GUILayout.Label(CollectorService.Label(port));
        GUILayout.Label(sourceMode ? CollectorService.DescribeLink(port) : service.Describe(port));
        if (!string.IsNullOrEmpty(message)) GUILayout.Label(message);
        GUILayout.BeginHorizontal();
        if (!sourceMode)
        {
            if (GUILayout.Button(Text.Get("CollectorPanel.collect_residue"))) { message = ""; service.Start(port); }
            if (GUILayout.Button(Text.Get("CollectorPanel.pause"))) { message = ""; service.Pause(port); }
            if (GUILayout.Button(Text.Get("CollectorPanel.inventory"))) { message = ""; service.OpenInventory(port); }
        }
        if (GUILayout.Button(Text.Get("CollectorPanel.unlink"))) service.Unlink(port, out message);
        GUILayout.EndHorizontal();
        GUILayout.Label(sourceMode ? Text.Get("CollectorPanel.link_this_sender_to_a_receiving_collector") : Text.Get("CollectorPanel.link_this_receiver_to_a_sending_processor"));
        scroll = GUILayout.BeginScrollView(scroll);
        var candidates = sourceMode ? CollectorService.Find() : ProcessingService.FindMachines(true).Where(ProcessingService.IsInstalledProcessor);
        foreach (var candidate in candidates)
        {
            if (GUILayout.Button(new GUIContent(Text.Get("CollectorPanel.link", CollectorService.Label(candidate)), candidate.strID)))
            {
                var receiver = sourceMode ? candidate : port;
                service.Bind(receiver, sourceMode ? port : candidate);
                message = service.Describe(receiver);
            }
            GUILayout.Label(CollectorService.DescribeLink(candidate));
        }
        GUILayout.EndScrollView();
        GUILayout.Label(Text.Get("CollectorPanel.one_sender_per_receiver_unlink_before_changing"));
        if (GUILayout.Button(Text.Get("CollectorPanel.close"))) target = null;
        GUI.DragWindow(new Rect(0, 0, bounds.width, TitleBarHeight));
    }
}
