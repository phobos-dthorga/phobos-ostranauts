using System.Linq;
using PhobosShipbreaker.Core;
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
    private Rect bounds = new Rect(180, 150, 660, 570);
    private Vector2 scroll;
    internal CollectorPanel(CollectorService service) => this.service = service;
    internal bool Show(CondOwner port)
    {
        if (!RoutingRules.IsReceiver(port.strCODef) || CollectorService.EndpointAccess(port) != null) return false;
        target = port; sourceMode = false; message = ""; return true;
    }
    internal bool ShowSource(CondOwner source)
    {
        if (!RoutingRules.IsSender(source.strCODef) || CollectorService.EndpointAccess(source) != null) return false;
        target = source; sourceMode = true; message = ""; return true;
    }
    internal void Reset() => target = null;
    internal void Draw()
    {
        if (target == null || target.bDestroyed || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return;
        bounds = GUI.Window(WindowId, bounds, Window, Text.Get("CollectorPanel.phobos_material_routing"));
    }
    private void Window(int id)
    {
        var port = target!;
        GUILayout.Label(CollectorService.Label(port));
        GUILayout.BeginHorizontal();
        if (RoutingRules.IsReceiver(port.strCODef) && GUILayout.Button(Text.Get("Routing.input_port"))) Show(port);
        if (RoutingRules.IsSender(port.strCODef) && GUILayout.Button(Text.Get("Routing.output_port"))) ShowSource(port);
        GUILayout.EndHorizontal();
        GUILayout.Label(sourceMode ? CollectorService.DescribeLink(port, true) : service.Describe(port));
        if (!string.IsNullOrEmpty(message)) GUILayout.Label(message);
        GUILayout.BeginHorizontal();
        if (!sourceMode)
        {
            if (GUILayout.Button(Text.Get("Routing.start"))) { message = ""; service.Start(port); }
            if (GUILayout.Button(Text.Get("CollectorPanel.pause"))) { message = ""; service.Pause(port); }
            if (GUILayout.Button(Text.Get("CollectorPanel.inventory"))) { message = ""; service.OpenInventory(port); }
        }
        if (GUILayout.Button(Text.Get("CollectorPanel.unlink"))) service.Unlink(port, out message, sourceMode);
        GUILayout.EndHorizontal();
        if (!sourceMode)
        {
            GUILayout.BeginHorizontal();
            foreach (var choice in ProcessingService.IsReclaimer(port) ? new[] { "feed" } : new[] { "all", "feed", "rejects", "legacy" })
                if (GUILayout.Button(FilterTitle(choice))) service.SetFilter(port, choice, out message);
            GUILayout.EndHorizontal();
        }
        GUILayout.Label(sourceMode ? Text.Get("Routing.choose_receiver") : Text.Get("Routing.choose_sender"));
        scroll = GUILayout.BeginScrollView(scroll);
        var candidates = sourceMode ? CollectorService.Receivers().Where(c => c != port && RoutingRules.CanConnect(port.strCODef, c.strCODef)) :
            CollectorService.Sources().Where(c => c != port && RoutingRules.CanConnect(c.strCODef, port.strCODef));
        foreach (var candidate in candidates)
        {
            if (GUILayout.Button(new GUIContent(Text.Get("CollectorPanel.link", CollectorService.Label(candidate)), candidate.strID)))
            {
                var receiver = sourceMode ? candidate : port;
                service.Bind(receiver, sourceMode ? port : candidate);
                message = service.Describe(receiver);
            }
            GUILayout.Label(CollectorService.DescribeLink(candidate, !sourceMode));
        }
        GUILayout.EndScrollView();
        GUILayout.Label(Text.Get("CollectorPanel.one_sender_per_receiver_unlink_before_changing"));
        if (GUILayout.Button(Text.Get("CollectorPanel.close"))) target = null;
        GUI.DragWindow(new Rect(0, 0, bounds.width, TitleBarHeight));
    }
    private static string FilterTitle(string choice) => choice switch
    {
        "all" => Text.Get("Routing.filter_all"), "feed" => Text.Get("Routing.filter_feed"),
        "rejects" => Text.Get("Routing.filter_rejects"), _ => Text.Get("Routing.filter_legacy")
    };
}
