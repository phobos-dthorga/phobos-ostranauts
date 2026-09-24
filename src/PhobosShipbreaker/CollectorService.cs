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
        internal string Status = Text.Get("CollectorService.paused_link_endpoints_if_needed_then_press");
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
        (collector.objContainer.Contains(item) || collector.objContainer.ContainedCOs.Count < CollectorRules.Capacity && collector.objContainer.ContainedCOs.Sum(c => c.GetTotalMass()) + item.GetTotalMass() <= CollectorRules.MaxPayloadKg + ProcessRules.MassTolerance);
    internal static string? AccessProblem(CondOwner port)
    {
        var crew = CrewSim.GetSelectedCrew();
        if (crew == null || crew.bDestroyed || crew.HasCond("IsDead") || crew.HasCond("Unconscious")) return Text.Get("CollectorService.select_an_awake_crew_member");
        if (port == null || port.bDestroyed || port.ship != crew.ship || TileUtils.TileRange(crew.GetPos(), port.GetPos("use")) > CollectorRules.AccessRangeTiles)
            return Text.Get("CollectorService.move_selected_crew_beside_the_collector_s");
        return port.HasCond("IsLocked") ? Text.Get("CollectorService.unlock_the_collector") : null;
    }
    private static string? MachineProblem(CondOwner port) => !Content.Ready ? Content.Status :
        port.bDestroyed || port.strCODef != CollectorRules.Installed || !port.HasCond("IsInstalled") || port.HasCond("IsDamaged") ? Text.Get("CollectorService.install_and_repair_the_collector") :
        port.objCOParent != null || port.ship == null || (int)port.ship.LoadState < 2 ? Text.Get("CollectorService.collector_ship_is_not_loaded") :
        port.objContainer == null || port.objContainer.Locked || port.HasCond("IsLocked") ? Text.Get("CollectorService.collector_inventory_missing_or_locked") :
        port.HasCond("IsOverrideOff") || port.HasCond("IsSignalOff") ? Text.Get("CollectorService.collector_is_switched_off") : null;
    private static string? SourceProblem(CondOwner port, CondOwner? source) => source == null || source.bDestroyed ||
        !ProcessingService.IsInstalledProcessor(source) || !source.HasCond("IsInstalled") || source.HasCond("IsDamaged") || source.objCOParent != null ? Text.Get("CollectorService.choose_an_installed_undamaged_processor") :
        source.ship != port.ship ? Text.Get("CollectorService.processor_must_be_on_this_ship") :
        source.objContainer == null || source.objContainer.Locked || source.HasCond("IsLocked") ? Text.Get("CollectorService.unlock_the_processor_product_tray") : null;
    internal bool Bind(CondOwner port, CondOwner source)
    {
        var s = sessions.GetValue(port, _ => new Session());
        string? problem = AccessProblem(port) == null || ProcessingService.AccessProblem(source) == null ? null :
            Text.Get("CollectorService.stand_beside_either_endpoint_to_link_this");
        if (problem != null) { s.Status = problem; return false; }
        problem = MachineProblem(port) ?? SourceProblem(port, source) ?? CollectorRoute.MountProblem(port);
        if (problem != null) { s.Status = problem; return false; }
        var route = CollectorRoute.Find(port, source);
        if (route == null) { s.Status = Text.Get("CollectorService.no_structural_floor_route_to_this_processor", GridRoute.DefaultVisitLimit); return false; }
        if (PortPairing.Matches(Sender(source), Receiver(port)))
        { s.Status = Text.Get("CollectorService.these_endpoints_are_already_paired_collection_settings"); return true; }
        if (!PortPairing.TryLink(Sender(source), Receiver(port), out string linkProblem)) { s.Status = linkProblem; return false; }
        Disarm(port, s); s.Source = source; s.PairId = PortPairing.Read(Receiver(port)).PairId;
        s.Route = route; s.Item = null; s.Clock = null;
        s.Status = Text.Get("CollectorService.pair_saved_press_collect_residue_filter_mixed");
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
        if (s.Route == null) { s.Status = Text.Get("CollectorService.no_structural_floor_route_restore_flooring_or"); Disarm(port, s); return false; }
        s.Armed = true; s.Last = StarSystem.fEpoch; s.Status = Text.Get("CollectorService.collection_enabled_waiting_for_residue");
        return true;
    }
    internal bool Pause(CondOwner port)
    {
        var s = sessions.GetValue(port, _ => new Session());
        string? problem = AccessProblem(port);
        if (problem != null) { s.Status = problem; return false; }
        Disarm(port, s); s.Status = Text.Get("CollectorService.collection_paused_material_retained"); return true;
    }
    private static void Disarm(CondOwner port, Session s) { s.Armed = false; if (!port.bDestroyed) port.ZeroCondAmount(CollectorRules.Working); }
    internal bool BeforePower(CondOwner port)
    {
        port.ZeroCondAmount(CollectorRules.Working);
        if (!sessions.TryGetValue(port, out var s) || !s.Armed) return false;
        string? problem = PairProblem(port, out var linkedSource, out var link);
        if (problem == null && (s.PairId != link.PairId || !ReferenceEquals(s.Source, linkedSource)))
            problem = Text.Get("CollectorService.paired_equipment_changed_resume_to_check_the");
        problem = problem ?? MachineProblem(port) ?? SourceProblem(port, s.Source) ?? CollectorRoute.MountProblem(port);
        if (problem != null || s.Route == null || !s.Route.Valid(port, s.Source!))
        { s.Status = problem ?? Text.Get("CollectorService.floor_route_changed_restore_it_and_resume"); Disarm(port, s); return false; }
        var source = s.Source!.objContainer;
        if (s.Item != null && (!source.Contains(s.Item) || !ValidPayload(s.Item))) { s.Item = null; s.Clock = null; }
        if (s.Item == null)
        {
            s.Item = source.ContainedCOs.OrderBy(i => i.strID, StringComparer.Ordinal).FirstOrDefault(ValidPayload);
            s.Last = StarSystem.fEpoch;
            if (s.Item == null) { s.Status = Text.Get("CollectorService.waiting_for_mixed_panel_residue_other_products"); return false; }
            s.Clock = new TransferClock(s.Item.strID, options.CollectorSeconds);
        }
        if (!CanAccept(port, s.Item) || !port.objContainer.AllowedCO(s.Item) || !port.objContainer.CanAddSimple(s.Item, out _))
        { s.Status = Text.Get("CollectorService.collector_full_or_blocked_residue_remains_in"); s.Last = StarSystem.fEpoch; return false; }
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
            { s.Status = Text.Get("CollectorService.time_gap_collection_paused_material_retained_resume"); Disarm(port, s); return; }
            s.Status = powered ? Text.Get("CollectorService.collecting_residue_s", s.Clock.Progress.ToString("F0"), s.Clock.Duration.ToString("F0")) : Text.Get("CollectorService.waiting_for_collector_conduit_power");
            if (!powered || !s.Clock.Complete) return;
            var move = new NativeItemTransfer(s.Source!.objContainer, port.objContainer, s.Item);
            if (!PhysicalTransfer.Commit(move)) { s.Status = Text.Get("CollectorService.transfer_blocked_residue_retained"); port.ZeroCondAmount(CollectorRules.Working); return; }
            s.Item = null; s.Clock = null; port.ZeroCondAmount(CollectorRules.Working);
            s.Status = Text.Get("CollectorService.residue_collected_material_remains_aboard"); move.Redraw();
            if (!options.CollectorContinue) Disarm(port, s);
        }
        catch (Exception ex) { Fault(port, ex); }
    }
    internal void Fault(CondOwner port, Exception ex)
    {
        var s = sessions.GetValue(port, _ => new Session()); Disarm(port, s);
        s.Status = Text.Get("CollectorService.collection_fault_paused_inspect_the_log_before"); log(ex.ToString());
    }
    internal string Describe(CondOwner port)
    {
        var s = sessions.GetValue(port, _ => new Session());
        return Text.Get("CollectorService.floor_route_tiles_stored_packets_kg_maximum", s.Status, DescribeLink(port), (s.Route?.Length ?? 0), (port.objContainer?.ContainedCOs.Count ?? 0), (port.HasCond("IsPowered") ? Text.Get("CollectorService.powered") : Text.Get("CollectorService.no_power")), CollectorRules.Capacity, CollectorRules.MaxPayloadKg);
    }
    internal void Reset() => sessions = new ConditionalWeakTable<CondOwner, Session>();
    internal bool OpenInventory(CondOwner port)
    {
        var s = sessions.GetValue(port, _ => new Session());
        string? problem = AccessProblem(port);
        if (problem != null) { s.Status = problem; return false; }
        if (port.objContainer == null || CrewSim.inventoryGUI == null) { s.Status = Text.Get("CollectorService.collector_inventory_unavailable"); return false; }
        CrewSim.inventoryGUI.SpawnInventoryWindow(port, Ostranauts.Inventory.InventoryWindowType.Container, null);
        return true;
    }
}
