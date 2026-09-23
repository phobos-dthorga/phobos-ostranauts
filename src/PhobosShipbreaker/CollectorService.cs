using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework.Inventory;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal sealed partial class CollectorService
{
    private sealed class Session
    {
        internal CondOwner? Source, Item;
        internal CollectorRoute? Route;
        internal TransferClock? Clock;
        internal bool Armed;
        internal double Last;
        internal string PairId = "";
        internal string Status = "Paused. Link endpoints if needed, then press Collect residue.";
    }
    private ConditionalWeakTable<CondOwner, Session> sessions = new ConditionalWeakTable<CondOwner, Session>();
    private readonly Action<string> log;
    private readonly Settings options;
    internal CollectorService(Action<string> log, Settings options) { this.log = log; this.options = options; }
    internal static CondOwner[] Find() => CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading ? Array.Empty<CondOwner>() :
        CrewSim.GetSelectedCrew()?.ship?.GetCOs(null, false, false, true)
        .Where(c => c != null && !c.bDestroyed && CollectorRules.IsFamily(c.strCODef) && c.HasCond("IsInstalled"))
        .OrderBy(c => c.strID, StringComparer.Ordinal).ToArray() ?? Array.Empty<CondOwner>();
    internal static bool ValidPayload(CondOwner? item) => item != null && !item.bDestroyed &&
        CollectorRules.Accepts(item.strCODef, item.GetTotalMass(), !item.HasCond("IsInstalled"),
            item.GetCOsSafe(true).Count == 0 && item.Crew == null, item.coStackHead == null && item.aStack.Count == 0);
    internal static bool CanAccept(CondOwner collector, CondOwner item) => ValidPayload(item) && collector.objContainer != null &&
        (collector.objContainer.Contains(item) || collector.objContainer.ContainedCOs.Count < CollectorRules.Capacity);
    internal static string? AccessProblem(CondOwner port)
    {
        var crew = CrewSim.GetSelectedCrew();
        if (crew == null || crew.bDestroyed || crew.HasCond("IsDead") || crew.HasCond("Unconscious")) return "Select an awake crew member.";
        if (port == null || port.bDestroyed || port.ship != crew.ship || TileUtils.TileRange(crew.GetPos(), port.GetPos("use")) > 2)
            return "Move selected crew beside the collector's inboard controls.";
        return port.HasCond("IsLocked") ? "Unlock the collector." : null;
    }
    private static string? MachineProblem(CondOwner port) => !Content.Ready ? Content.Status :
        port.bDestroyed || port.strCODef != CollectorRules.Installed || !port.HasCond("IsInstalled") || port.HasCond("IsDamaged") ? "Install and repair the collector." :
        port.objCOParent != null || port.ship == null || (int)port.ship.LoadState < 2 ? "Collector ship is not loaded." :
        port.objContainer == null || port.objContainer.Locked || port.HasCond("IsLocked") ? "Collector inventory missing or locked." :
        port.HasCond("IsOverrideOff") || port.HasCond("IsSignalOff") ? "Collector is switched off." : null;
    private static string? SourceProblem(CondOwner port, CondOwner? source) => source == null || source.bDestroyed ||
        source.strCODef != Content.Installed || !source.HasCond("IsInstalled") || source.HasCond("IsDamaged") || source.objCOParent != null ? "Choose an installed, undamaged processor." :
        source.ship != port.ship ? "Processor must be on this ship." :
        source.objContainer == null || source.objContainer.Locked || source.HasCond("IsLocked") ? "Unlock the processor product tray." : null;
    internal bool Bind(CondOwner port, CondOwner source)
    {
        var s = sessions.GetValue(port, _ => new Session());
        string? problem = AccessProblem(port) == null || ProcessingService.AccessProblem(source) == null ? null :
            "Stand beside either endpoint to link this pair.";
        if (problem != null) { s.Status = problem; return false; }
        problem = MachineProblem(port) ?? SourceProblem(port, source) ?? CollectorRoute.MountProblem(port);
        if (problem != null) { s.Status = problem; return false; }
        var route = CollectorRoute.Find(port, source);
        if (route == null) { s.Status = "No structural-floor route to this processor (search limit 4096 tiles)."; return false; }
        if (PortPairing.Matches(Sender(source), Receiver(port)))
        { s.Status = "These endpoints are already paired; collection settings retained."; return true; }
        if (!PortPairing.TryLink(Sender(source), Receiver(port), out string linkProblem)) { s.Status = linkProblem; return false; }
        Disarm(port, s); s.Source = source; s.PairId = PortPairing.Read(Receiver(port)).PairId;
        s.Route = route; s.Item = null; s.Clock = null;
        s.Status = "Pair saved; press Collect residue. Filter: mixed panel residue only.";
        return true;
    }
    internal bool Start(CondOwner port)
    {
        var s = sessions.GetValue(port, _ => new Session());
        string? problem = AccessProblem(port) ?? MachineProblem(port);
        if (problem != null) { s.Status = problem; return false; }
        problem = PairProblem(port, out var source, out var link) ?? SourceProblem(port, source) ?? CollectorRoute.MountProblem(port);
        if (problem != null) { s.Status = problem; return false; }
        if (s.PairId != link.PairId || !ReferenceEquals(s.Source, source)) { s.Clock = null; s.Item = null; }
        s.Source = source; s.PairId = link.PairId;
        s.Route = CollectorRoute.Find(port, s.Source!);
        if (s.Route == null) { s.Status = "No structural-floor route. Restore flooring or choose another processor."; Disarm(port, s); return false; }
        s.Armed = true; s.Last = StarSystem.fEpoch; s.Status = "Collection enabled; waiting for residue.";
        return true;
    }
    internal bool Pause(CondOwner port)
    {
        var s = sessions.GetValue(port, _ => new Session());
        string? problem = AccessProblem(port);
        if (problem != null) { s.Status = problem; return false; }
        Disarm(port, s); s.Status = "Collection paused; material retained."; return true;
    }
    private static void Disarm(CondOwner port, Session s) { s.Armed = false; if (!port.bDestroyed) port.ZeroCondAmount(CollectorRules.Working); }
    internal bool BeforePower(CondOwner port)
    {
        port.ZeroCondAmount(CollectorRules.Working);
        if (!sessions.TryGetValue(port, out var s) || !s.Armed) return false;
        string? problem = PairProblem(port, out var linkedSource, out var link);
        if (problem == null && (s.PairId != link.PairId || !ReferenceEquals(s.Source, linkedSource)))
            problem = "Paired equipment changed; resume to check the route again.";
        problem = problem ?? MachineProblem(port) ?? SourceProblem(port, s.Source) ?? CollectorRoute.MountProblem(port);
        if (problem != null || s.Route == null || !s.Route.Valid(port, s.Source!))
        { s.Status = problem ?? "Floor route changed; restore it and resume collection."; Disarm(port, s); return false; }
        var source = s.Source!.objContainer;
        if (s.Item != null && (!source.Contains(s.Item) || !ValidPayload(s.Item))) { s.Item = null; s.Clock = null; }
        if (s.Item == null)
        {
            s.Item = source.ContainedCOs.OrderBy(i => i.strID, StringComparer.Ordinal).FirstOrDefault(ValidPayload);
            s.Last = StarSystem.fEpoch;
            if (s.Item == null) { s.Status = "Waiting for mixed panel residue; other products stay in the processor."; return false; }
            s.Clock = new TransferClock(s.Item.strID, options.CollectorSeconds);
        }
        if (!CanAccept(port, s.Item) || !port.objContainer.AllowedCO(s.Item) || !port.objContainer.CanAddSimple(s.Item, out _))
        { s.Status = "Collector full or blocked; residue remains in the processor."; s.Last = StarSystem.fEpoch; return false; }
        port.SetCondAmount(CollectorRules.Working, 1); return true;
    }
    internal void AfterPower(CondOwner port, bool requested)
    {
        if (!sessions.TryGetValue(port, out var s) || !s.Armed) return;
        try
        {
            double now = StarSystem.fEpoch, elapsed = now - s.Last; s.Last = now;
            var clock = s.Clock;
            if (!BeforePower(port) || s.Clock == null || s.Item == null) return;
            if (!ReferenceEquals(clock, s.Clock)) elapsed = 0;
            bool powered = requested && port.HasCond("IsPowered");
            if (!s.Clock.Advance(s.Item.strID, elapsed, powered))
            { s.Status = "Time gap; collection paused, material retained. Resume when ready."; Disarm(port, s); return; }
            s.Status = powered ? "Collecting residue: " + s.Clock.Progress.ToString("F0") + "/" + s.Clock.Duration.ToString("F0") + " s." : "Waiting for collector conduit power.";
            if (!powered || !s.Clock.Complete) return;
            var move = new NativeItemTransfer(s.Source!.objContainer, port.objContainer, s.Item);
            if (!PhysicalTransfer.Commit(move)) { s.Status = "Transfer blocked; residue retained."; port.ZeroCondAmount(CollectorRules.Working); return; }
            s.Item = null; s.Clock = null; port.ZeroCondAmount(CollectorRules.Working);
            s.Status = "Residue collected; material remains aboard."; move.Redraw();
            if (!options.CollectorContinue) Disarm(port, s);
        }
        catch (Exception ex) { Fault(port, ex); }
    }
    internal void Fault(CondOwner port, Exception ex)
    {
        var s = sessions.GetValue(port, _ => new Session()); Disarm(port, s);
        s.Status = "Collection fault; paused. Inspect the log before resuming."; log(ex.ToString());
    }
    internal string Describe(CondOwner port)
    {
        var s = sessions.GetValue(port, _ => new Session());
        return s.Status + "\n" + DescribeLink(port) +
            " | Floor route: " + (s.Route?.Length ?? 0) + " tiles.\nStored: " + (port.objContainer?.ContainedCOs.Count ?? 0) +
            "/4 packets (52 kg maximum payload). Filter: mixed panel residue only.\n" +
            (port.HasCond("IsPowered") ? "Powered." : "No power.") + " Collection is attached storage, not ejection.";
    }
    internal void Reset() => sessions = new ConditionalWeakTable<CondOwner, Session>();
    internal bool OpenInventory(CondOwner port)
    {
        var s = sessions.GetValue(port, _ => new Session());
        string? problem = AccessProblem(port);
        if (problem != null) { s.Status = problem; return false; }
        if (port.objContainer == null || CrewSim.inventoryGUI == null) { s.Status = "Collector inventory unavailable."; return false; }
        CrewSim.inventoryGUI.SpawnInventoryWindow(port, Ostranauts.Inventory.InventoryWindowType.Container, null);
        return true;
    }
}
