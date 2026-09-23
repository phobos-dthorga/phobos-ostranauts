using System.Linq;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>Presentation only; collection and access checks belong to the service.</summary>
internal sealed class CollectorPanel
{
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
        bounds = GUI.Window(847293, bounds, Window, sourceMode ? "Phobos Material Routing" : "Phobos Residue Collector");
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
            if (GUILayout.Button("Collect residue")) { message = ""; service.Start(port); }
            if (GUILayout.Button("Pause")) { message = ""; service.Pause(port); }
            if (GUILayout.Button("Inventory")) { message = ""; service.OpenInventory(port); }
        }
        if (GUILayout.Button("Unlink")) service.Unlink(port, out message);
        GUILayout.EndHorizontal();
        GUILayout.Label(sourceMode ? "Link this sender to a receiving collector:" : "Link this receiver to a sending processor:");
        scroll = GUILayout.BeginScrollView(scroll);
        var candidates = sourceMode ? CollectorService.Find() : ProcessingService.FindMachines().Where(p => p.strCODef == Content.Installed);
        foreach (var candidate in candidates)
        {
            if (GUILayout.Button(new GUIContent("Link: " + CollectorService.Label(candidate), candidate.strID)))
            {
                var receiver = sourceMode ? candidate : port;
                service.Bind(receiver, sourceMode ? port : candidate);
                message = service.Describe(receiver);
            }
            GUILayout.Label(CollectorService.DescribeLink(candidate));
        }
        GUILayout.EndScrollView();
        GUILayout.Label("One sender per receiver. Unlink before changing partners. Saved pairs survive reload; press Collect at the receiver to resume. Residue only; useful products stay aboard.");
        if (GUILayout.Button("Close")) target = null;
        GUI.DragWindow(new Rect(0, 0, bounds.width, 24));
    }
}
