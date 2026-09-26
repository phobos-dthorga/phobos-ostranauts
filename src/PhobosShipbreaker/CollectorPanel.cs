using System.Linq;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Controls;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>Presentation only; collection and access checks belong to the service.</summary>
internal sealed class CollectorPanel
{
    private const int WindowId = 847293;
    private const float TitleBarHeight = 24;
    private readonly CollectorService service;
    private CondOwner? target;
    private bool sourceMode, metalsMode;
    private string message = "";
    private Rect bounds = new Rect(180, 150, 660, 570);
    private Vector2 scroll;
    internal CollectorPanel(CollectorService service) => this.service = service;
    internal bool Show(CondOwner port)
    {
        if (!RoutingRules.IsReceiver(port.strCODef) || CollectorService.EndpointAccess(port) != null) return false;
        target = port; sourceMode = false; metalsMode = false; message = ""; return true;
    }
    internal bool ShowSource(CondOwner source, bool metals = false)
    {
        if (metals && !ProcessingService.IsReclaimer(source) || !RoutingRules.IsSender(source.strCODef) || CollectorService.EndpointAccess(source) != null) return false;
        target = source; sourceMode = true; metalsMode = metals; message = ""; return true;
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
        if (ProcessingService.IsReclaimer(port) && GUILayout.Button(Text.Get("Routing.metals_port"))) ShowSource(port, true);
        GUILayout.EndHorizontal();
        GUILayout.Label(sourceMode ? CollectorService.DescribeLink(port, true, metalsMode) : service.Describe(port));
        if (!string.IsNullOrEmpty(message)) GUILayout.Label(message);
        GUILayout.BeginHorizontal();
        if (!sourceMode)
        {
            if (GUILayout.Button(Text.Get("Routing.start"))) { message = ""; service.Start(port); }
            if (GUILayout.Button(Text.Get("CollectorPanel.pause"))) { message = ""; service.Pause(port); }
            if (GUILayout.Button(Text.Get("CollectorPanel.inventory"))) { message = ""; service.OpenInventory(port); }
        }
        if (GUILayout.Button(Text.Get("CollectorPanel.unlink"))) Feedback(port, service.Unlink(port, out message, sourceMode, metals: metalsMode));
        GUILayout.EndHorizontal();
        if (!sourceMode)
        {
            GUILayout.BeginHorizontal();
            foreach (var choice in RoutingRules.Choices(port.strCODef))
                if (GUILayout.Button(FilterTitle(choice))) Feedback(port, service.SetFilter(port, choice, out message));
            GUILayout.EndHorizontal();
        }
        GUILayout.Label(sourceMode ? Text.Get("Routing.choose_receiver") : Text.Get("Routing.choose_sender"));
        scroll = GUILayout.BeginScrollView(scroll);
        var candidates = sourceMode ? CollectorService.Receivers().Where(c => c != port && RoutingRules.CanConnect(port.strCODef, RoutingRules.OutputPort(port.strCODef, metalsMode), c.strCODef)) :
            CollectorService.Sources().Where(c => c != port && RoutingRules.CanConnect(c.strCODef, port.strCODef));
        foreach (var candidate in candidates)
        {
            if (GUILayout.Button(new GUIContent(Text.Get("CollectorPanel.link", CollectorService.Label(candidate)), candidate.strID)))
            {
                var receiver = sourceMode ? candidate : port;
                bool success = service.Bind(receiver, sourceMode ? port : candidate);
                message = service.Describe(receiver);
                Feedback(port, success);
            }
            GUILayout.Label(CollectorService.DescribeLink(candidate, !sourceMode, !sourceMode && FurnaceRules.Machine(port.strCODef)));
        }
        GUILayout.EndScrollView();
        GUILayout.Label(Text.Get("CollectorPanel.one_sender_per_receiver_unlink_before_changing"));
        if (GUILayout.Button(Text.Get("CollectorPanel.close"))) target = null;
        GUI.DragWindow(new Rect(0, 0, bounds.width, TitleBarHeight));
    }
    private void Feedback(CondOwner port, bool success) => message = PanelFeedback.Additional(success, message,
        sourceMode ? CollectorService.DescribeLink(port, true, metalsMode) : service.Describe(port));
    private static string FilterTitle(string choice) => choice switch
    {
        "all" => Text.Get("Routing.filter_all"), "feed" => Text.Get("Routing.filter_feed"),
        "aluminium" => Text.Get("Industry.filter_aluminium"), "furnace-products" => Text.Get("Industry.filter_furnace-products"),
        "rejects" => Text.Get("Routing.filter_rejects"), _ => Text.Get("Routing.filter_legacy")
    };
}
