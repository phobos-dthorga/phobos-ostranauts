using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Controls;
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
        internal bool NeedsAttention;
        internal double Last;
        internal string PairId = "";
        internal string FilterSignature = "";
        // A true admission holds for the rest of its step (native power in between changes no item or route).
        internal long AdmittedStep = long.MinValue;
        // Whether this load already tried to resume a route that was running when the game was saved.
        internal bool ResumeTried;
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
    private static CondOwner[] Furnaces() => IndustryService.Discover(CrewSim.GetSelectedCrew()?.ship).Where(c => FurnaceRules.Machine(c.strCODef)).ToArray();
    internal static CondOwner[] Receivers() => Find().Concat(Furnaces()).Concat(ProcessingService.FindMachines(true).Where(m => m.strCODef == ReclaimerRules.Installed)).ToArray();
    internal static CondOwner[] Sources() => Find().Concat(Furnaces()).Concat(ProcessingService.FindMachines(true).Where(ProcessingService.IsInstalledProcessor)).ToArray();
    internal static string? EndpointAccess(CondOwner port, ConsoleBinding? console = null) => (console != null ? ControlAuthority.Check(port, console) : CollectorRules.IsFamily(port.strCODef) ? AccessProblem(port) : ProcessingService.AccessProblem(port)) ??
        (port.HasCond("IsLocked") ? Text.Get("CollectorLinks.unlock_this_endpoint") : null);
    private static Container? Destination(CondOwner port) => FurnaceRules.Machine(port.strCODef) ? FurnaceService.Feed(port)?.objContainer : ProcessingService.IsReclaimer(port) ? ProcessingService.Feed(port)?.objContainer : port.objContainer;
    private static string WorkingCondition(CondOwner port) => ProcessingService.IsReclaimer(port) ? RoutingRules.Feeding : CollectorRules.Working;
    private double CycleSeconds(CondOwner port) => (ProcessingService.IsReclaimer(port) || FurnaceRules.Machine(port.strCODef)) ? options.FeederSeconds : options.CollectorSeconds;
    internal static bool ValidPayload(CondOwner? item) => item != null && !item.bDestroyed &&
        CollectorRules.Accepts(item.strCODef, item.GetTotalMass(), !item.HasCond("IsInstalled"),
            item.GetCOsSafe(true).Count == 0 && item.Crew == null, item.coStackHead == null && item.aStack.Count == 0) ||
        item != null && !item.bDestroyed && FurnaceMaterialRules.Product(item.strCODef, item.GetTotalMass()) && !item.HasCond("IsInstalled") &&
        item.Crew == null && item.GetCOsSafe(true).Count == 0 && item.GetLotCOs(true).Count == 0 && item.coStackHead == null && item.aStack.Count == 0;
    /// <summary>One unit of a native stack judged at its own mass, as the crew orders judge it (Shipbreaker 0.57.0):
    /// routed receivers take stacked products one unit at a time.</summary>
    internal static bool ValidUnit(CondOwner unit)
    {
        if (!Phobos.Ostranauts.Framework.Crew.CrewLogistics.Loose(unit) || unit.Crew != null) return false;
        double kg = unit.GetCondAmount("StatMass"); bool detached = !unit.HasCond("IsInstalled"), noLot = unit.GetLotCOs(true).Count == 0;
        return CollectorRules.Accepts(unit.strCODef, kg, detached, noLot, true) || FurnaceMaterialRules.Product(unit.strCODef, kg) && detached && noLot;
    }
    private static bool Payload(CondOwner port, CondOwner item) => !UnitItemTransfer.Stacked(item) ?
        (FurnaceRules.Machine(port.strCODef) ? FurnaceService.ValidFeed(port, item) : ValidPayload(item)) :
        FurnaceRules.Machine(port.strCODef) ? FurnaceService.CrewFeed(port, item) : ValidUnit(item);
    internal static bool CanAccept(CondOwner collector, CondOwner item) => (ValidPayload(item) || (UnitItemTransfer.Stacked(item) || Phobos.Ostranauts.Framework.Crew.CrewLogistics.IsUnitPreflight(item)) && ValidUnit(item) || CollectorCargo.Accepts(item)) && collector.objContainer != null &&
        (collector.objContainer.Contains(item) || collector.objContainer.ContainedCOs.Count < CollectorRules.Capacity && collector.objContainer.ContainedCOs.Sum(c => c.GetTotalMass()) + UnitItemTransfer.UnitKg(item) <= CollectorRules.MaxPayloadKg + ProcessRules.MassTolerance);
    /// <summary>The units in a sender's tray, stacks opened, in id order.</summary>
    private static System.Collections.Generic.IReadOnlyList<CondOwner> SourceUnits(CondOwner source) => Phobos.Ostranauts.Framework.Crew.CrewLogistics.Contents(source).ToArray();
    internal static string? AccessProblem(CondOwner port)
    {
        var crew = Phobos.Ostranauts.Framework.Crew.CrewWork.Actor ?? CrewSim.GetSelectedCrew();
        if (crew == null || crew.bDestroyed || crew.HasCond("IsDead") || crew.HasCond("Unconscious")) return Text.Get("CollectorService.select_an_awake_crew_member");
        if (port == null || port.bDestroyed || !Phobos.Ostranauts.Framework.Crew.CrewWork.LocalAccess(crew, port, CollectorRules.AccessRangeTiles))
            return Text.Get("CollectorService.move_selected_crew_beside_the_collector_s");
        return port.HasCond("IsLocked") ? Text.Get("CollectorService.unlock_the_collector") : null;
    }
    private static string? MachineProblem(CondOwner port) => FurnaceRules.Machine(port.strCODef) ? FurnaceService.MaterialProblem(port, true) : ProcessingService.IsReclaimer(port) ? ProcessingService.MachineProblem(port) : !Content.Ready ? Content.Status :
        port.bDestroyed || port.strCODef != CollectorRules.Installed || !port.HasCond("IsInstalled") || port.HasCond("IsDamaged") ? Text.Get("CollectorService.install_and_repair_the_collector") :
        port.objCOParent != null || port.ship == null || (int)port.ship.LoadState < 2 ? Text.Get("CollectorService.collector_ship_is_not_loaded") :
        port.objContainer == null || port.objContainer.Locked || port.HasCond("IsLocked") ? Text.Get("CollectorService.collector_inventory_missing_or_locked") :
        port.HasCond("IsOverrideOff") || port.HasCond("IsSignalOff") ? Text.Get("CollectorService.collector_is_switched_off") : null;
    private static string? SourceProblem(CondOwner port, CondOwner? source) => source == null || source == port || source.bDestroyed ||
        !RoutingRules.CanConnect(source.strCODef, port.strCODef) || !source.HasCond("IsInstalled") || source.HasCond("IsDamaged") || source.objCOParent != null ? Text.Get("Routing.invalid_source") :
        source.ship != port.ship ? Text.Get("Routing.same_ship") :
        source.objContainer == null || source.objContainer.Locked || source.HasCond("IsLocked") ? Text.Get("Routing.source_locked") : FurnaceRules.Machine(source.strCODef) ? FurnaceService.MaterialProblem(source, false) : null;
    internal bool Bind(CondOwner port, CondOwner source, ConsoleBinding? console = null)
    {
        var s = sessions.GetValue(port, _ => new Session());
        string? problem = console != null ? EndpointAccess(port, console) ?? EndpointAccess(source, console) :
            EndpointAccess(port) == null || EndpointAccess(source) == null ? null : Text.Get("CollectorService.stand_beside_either_endpoint_to_link_this");
        if (problem != null) { s.Status = problem; return false; }
        problem = MachineProblem(port) ?? SourceProblem(port, source) ?? FilterProblem(port) ?? CollectorRoute.MountProblem(port);
        if (problem != null) { s.Status = problem; return false; }
        var route = CollectorRoute.Find(port, source);
        if (route == null) { s.Status = Text.Get("Routing.no_belt"); return false; }
        if (PortPairing.Matches(SourcePort(source, port), Receiver(port)))
        { s.Status = Text.Get("CollectorService.these_endpoints_are_already_paired_collection_settings"); return true; }
        if (!PortPairing.TryLink(SourcePort(source, port), Receiver(port), out string linkProblem)) { s.Status = linkProblem; return false; }
        Disarm(port, s); s.Source = source; s.PairId = PortPairing.Read(Receiver(port)).PairId;
        s.Route = route; s.Item = null; s.Clock = null;
        s.Status = Text.Get("Routing.linked");
        return true;
    }
    internal bool Start(CondOwner port, ConsoleBinding? console = null)
    {
        var s = sessions.GetValue(port, _ => new Session());
        string? problem = EndpointAccess(port, console);
        if (problem != null) { s.Status = problem; return false; }
        return Arm(port, s);
    }
    /// <summary>For a furnace repeat run that already holds explicit, revalidated permission.
    /// Every machine, pair, route and filter check still applies; only operator presence is not required.</summary>
    internal bool StartAuthorized(CondOwner port) =>
        FurnaceService.RepeatActive(port) && !port.HasCond("IsLocked") && Arm(port, sessions.GetValue(port, _ => new Session()));
    private bool Arm(CondOwner port, Session s)
    {
        string? problem = MachineProblem(port);
        if (problem != null) { s.Status = problem; return false; }
        problem = PairProblem(port, out var source, out var link) ?? SourceProblem(port, source) ?? FilterProblem(port) ?? CollectorRoute.MountProblem(port);
        if (problem != null) { s.Status = problem; return false; }
        if (s.PairId != link.PairId || !ReferenceEquals(s.Source, source)) { s.Clock = null; s.Item = null; }
        s.Source = source; s.PairId = link.PairId;
        s.Route = CollectorRoute.Find(port, s.Source!);
        if (s.Route == null) { s.Status = Text.Get("Routing.no_belt"); Disarm(port, s); return false; }
        s.FilterSignature = FilterSignature(port);
        if (FurnaceRules.Machine(port.strCODef) && FurnaceMaterialRules.ChargeFull(Destination(port)!.ContainedCOs.Count))
        { s.Status = Text.Get("Routing.charge_full", FurnaceRules.ChargeUnits); return false; }
        s.NeedsAttention = false; s.Armed = true; s.Last = StarSystem.fEpoch; s.Status = Text.Get("CollectorService.collection_enabled_waiting_for_residue");
        SetResume(port, true);
        return true;
    }
    internal bool Pause(CondOwner port, ConsoleBinding? console = null)
    {
        var s = sessions.GetValue(port, _ => new Session());
        string? problem = EndpointAccess(port, console);
        if (problem != null) { s.Status = problem; return false; }
        Phobos.Ostranauts.Framework.Crew.CrewWork.ManualStop(port);
        Disarm(port, s); s.NeedsAttention = false; s.Status = Text.Get("CollectorService.collection_paused_material_retained"); return true;
    }
    private static void SetWorking(CondOwner port, bool working) { if (!FurnaceRules.Machine(port.strCODef) && !port.bDestroyed) port.SetCondAmount(WorkingCondition(port), working ? 1 : 0); }
    private static void Disarm(CondOwner port, Session s) { s.Armed = false; SetWorking(port, false); SetResume(port, false); BeltCarriers.Hide(port.strID); }
    /// <summary>The saved mark of a running route (Shipbreaker 0.56.0): set while armed, so a reload resumes it.</summary>
    private static void SetResume(CondOwner port, bool on) { if (!port.bDestroyed && port.HasCond(RoutingRules.BeltResume) != on) port.SetCondAmount(RoutingRules.BeltResume, on ? 1 : 0); }
    /// <summary>How the route runs, for the status: by belt, or touching.</summary>
    private static string RouteText(CollectorRoute? route) => Text.Get(route == null ? "Routing.route_none" : route.Touching ? "Routing.route_touching" : "Routing.route_belt");
    /// <summary>Owner decision (30 September 2026): belt routes resume after a reload, like the crew's standing orders.
    /// A route that was running when the game was saved arms itself once, through every pair, route and filter check;
    /// if any fails it stays paused with the reason, as it would have before.</summary>
    private void ResumeAfterLoad(CondOwner port)
    {
        if (!port.HasCond(RoutingRules.BeltResume) || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return;
        var s = sessions.GetValue(port, _ => new Session());
        if (s.Armed || s.ResumeTried) return;
        s.ResumeTried = true;
        if (Arm(port, s)) s.Status = Text.Get("Routing.resumed");
        else { SetResume(port, false); s.NeedsAttention = true; }
    }
    internal object? TransferToken(CondOwner port) => sessions.TryGetValue(port, out var s) && s.Armed ? s.Clock : null;
    internal bool ReceivingEnabled(CondOwner port) => sessions.TryGetValue(port, out var s) && s.Armed;
    internal EquipmentActivity? ActiveReceiving(CondOwner port) => sessions.TryGetValue(port, out var s) && (s.Armed || s.NeedsAttention) ? Activity(port) : (EquipmentActivity?)null;
    internal void Interrupt(CondOwner port, string status) { if (sessions.TryGetValue(port, out var s) && s.Armed) ClearTransfer(port, status); }
    internal void CancelPending(CondOwner port, string status) => ClearTransfer(port, status);
    internal bool BeforePower(CondOwner port)
    {
        ResumeAfterLoad(port);
        if (sessions.TryGetValue(port, out var admitted) && admitted.Armed && admitted.AdmittedStep == Phobos.Ostranauts.Framework.Processing.NativeSteps.Frame && admitted.Item != null && admitted.Clock != null) return true;
        bool verdict = BeforePowerNow(port);
        if (verdict && sessions.TryGetValue(port, out var s)) s.AdmittedStep = Phobos.Ostranauts.Framework.Processing.NativeSteps.Frame;
        return verdict;
    }
    private bool BeforePowerNow(CondOwner port)
    {
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.RouteCheck);
        SetWorking(port, false);
        if (!sessions.TryGetValue(port, out var s) || !s.Armed) return false;
        string? problem = PairProblem(port, out var linkedSource, out var link);
        if (problem == null && (s.PairId != link.PairId || !ReferenceEquals(s.Source, linkedSource)))
            problem = Text.Get("CollectorService.paired_equipment_changed_resume_to_check_the");
        problem = problem ?? MachineProblem(port) ?? SourceProblem(port, s.Source) ?? FilterProblem(port) ?? CollectorRoute.MountProblem(port);
        if (problem == null && s.FilterSignature != FilterSignature(port)) problem = Text.Get("Routing.filter_changed");
        if (problem != null || s.Route == null || !s.Route.Valid(port, s.Source!))
        { s.NeedsAttention = true; s.Status = problem ?? Text.Get("Routing.belt_changed"); Disarm(port, s); return false; }
        if (FurnaceRules.Machine(port.strCODef) && FurnaceMaterialRules.ChargeFull(Destination(port)!.ContainedCOs.Count))
        { ClearTransfer(port, Text.Get("Routing.charge_full", FurnaceRules.ChargeUnits)); return false; }
        var units = SourceUnits(s.Source!);
        if (s.Item != null && (!units.Contains(s.Item) || !Payload(port, s.Item) || !FilterAllows(port, s.Item))) { s.Item = null; s.Clock = null; }
        if (s.Item == null)
        {
            Phobos.Ostranauts.Framework.Diagnostics.Performance.Increment(PerformanceMetrics.RouteCandidates, units.Count);
            s.Item = units.FirstOrDefault(i => Payload(port, i) && FilterAllows(port, i));
            s.Last = StarSystem.fEpoch;
            if (s.Item == null) { s.Status = Text.Get("Routing.waiting"); return false; }
            s.Clock = new TransferClock(s.Item.strID, CycleSeconds(port));
        }
        var destination = Destination(port);
        var item = s.Item;
        bool Admits() => FurnaceRules.Machine(port.strCODef) ? FurnaceService.CanFeed(destination!.CO, item) : ProcessingService.IsReclaimer(port) ? ProcessingService.CanFeed(destination!.CO, item) : CanAccept(port, item);
        // A stacked unit is asked about as one unit, as the crew orders ask; a single item as before.
        if (destination == null || destination.Locked || !(UnitItemTransfer.Stacked(item) ? UnitItemTransfer.AsUnit(item, Admits) : Admits()) ||
            !UnitItemTransfer.Fits(destination.CO, item, out _))
        { s.Status = Text.Get("Routing.full"); s.Last = StarSystem.fEpoch; return false; }
        SetWorking(port, true); return true;
    }
    internal void AfterPower(CondOwner port, bool requested, double? poweredSeconds = null, object? admittedToken = null)
    {
        if (!sessions.TryGetValue(port, out var s) || !s.Armed) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.RouteAdvance);
        try
        {
            // A long interval (time-skip, reload gap) is not a fault: the paid power interval bounds the work,
            // and the route catches up like a native machine would.
            double now = StarSystem.fEpoch, elapsed = now - s.Last; s.Last = now;
            var clock = s.Clock;
            if (!BeforePower(port) || s.Clock == null || s.Item == null) return;
            if (!ReferenceEquals(clock, s.Clock) || FurnaceRules.Machine(port.strCODef) && !ReferenceEquals(admittedToken, s.Clock)) elapsed = 0;
            bool powered = requested && port.HasCond("IsPowered");
            if (!s.Clock.Advance(s.Item.strID, poweredSeconds.HasValue ? Math.Min(elapsed, poweredSeconds.Value) : elapsed, powered))
            { s.Item = null; s.Clock = null; s.Status = Text.Get("Routing.waiting"); return; }
            s.Status = powered ? Text.Get("CollectorService.collecting_residue_s", s.Clock.Progress.ToString("F0"), s.Clock.Duration.ToString("F0")) : Text.Get("Routing.no_power");
            // The item seen riding the belt (display only; the item itself stays in the sender until delivery).
            BeltCarriers.Show(port.strID, port.ship, s.Item, s.Route?.BeltPath, s.Clock.Duration > 0 ? s.Clock.Progress / s.Clock.Duration : 0);
            if (!powered || !s.Clock.Complete) return;
            var move = new UnitItemTransfer(s.Item, Destination(port)!.CO);
            if (!PhysicalTransfer.Commit(move)) { s.Status = Text.Get("CollectorService.transfer_blocked_residue_retained"); SetWorking(port, false); return; }
            s.Item = null; s.Clock = null; SetWorking(port, false); BeltCarriers.Hide(port.strID);
            s.Status = Text.Get("Routing.delivered"); move.Redraw();
            if (ProcessingService.IsReclaimer(port)) Plugin.Service.FeedArrived(port);
            if (FurnaceRules.Machine(port.strCODef) && FurnaceMaterialRules.ChargeFull(Destination(port)!.ContainedCOs.Count)) ClearTransfer(port, Text.Get("Routing.charge_full", FurnaceRules.ChargeUnits));
            else if (!((ProcessingService.IsReclaimer(port) || FurnaceRules.Machine(port.strCODef)) ? options.FeederContinue : options.CollectorContinue)) Disarm(port, s);
        }
        catch (Exception ex) { Fault(port, ex); }
    }
    internal void Block(CondOwner port, string status)
    { IndustryObservations.RecordStop(port, status); ClearTransfer(port, status); sessions.GetValue(port, _ => new Session()).NeedsAttention = true; }
    /// <summary>The room cannot take the machine's heat this step: the route keeps its permission and its
    /// pending item, shows why, and continues by itself once the room cools. Not a stop.</summary>
    internal void HeatWait(CondOwner port, string status) { if (sessions.TryGetValue(port, out var s) && s.Armed) s.Status = status; }
    internal void Fault(CondOwner port, Exception ex)
    {
        var s = sessions.GetValue(port, _ => new Session()); Disarm(port, s);
        s.NeedsAttention = true;
        s.Status = Text.Get("CollectorService.collection_fault_paused_inspect_the_log_before"); log(ex.ToString());
        IndustryObservations.RecordStop(port, s.Status);
    }
    internal string Describe(CondOwner port)
    {
        var s = sessions.GetValue(port, _ => new Session());
        if (FurnaceRules.Machine(port.strCODef)) return Text.Get("Routing.furnace_receiving", s.Status, DescribeLink(port, false), RouteText(s.Route), Destination(port)?.ContainedCOs.Count ?? 0, FurnaceRules.ChargeUnits);
        return Text.Get("CollectorService.floor_route_tiles_stored_packets_kg_maximum", s.Status + "\n" + FilterLabel(port), DescribeLink(port, false), RouteText(s.Route), (Destination(port)?.ContainedCOs.Count ?? 0), (port.HasCond("IsPowered") ? Text.Get("CollectorService.powered") : Text.Get("CollectorService.no_power")), CollectorRules.Capacity, CollectorRules.MaxPayloadKg);
    }
    internal void Reset() => sessions = new ConditionalWeakTable<CondOwner, Session>();
    internal bool OpenInventory(CondOwner port)
    {
        var s = sessions.GetValue(port, _ => new Session());
        string? problem = EndpointAccess(port);
        if (problem != null) { s.Status = problem; return false; }
        if (FurnaceRules.Machine(port.strCODef)) return FurnaceService.Command(null, port, "feed", null, out _);
        if (ProcessingService.IsReclaimer(port)) return Plugin.Service.OpenInventory(port, true);
        if (port.objContainer == null || CrewSim.inventoryGUI == null) { s.Status = Text.Get("CollectorService.collector_inventory_unavailable"); return false; }
        CrewSim.inventoryGUI.SpawnInventoryWindow(port, Ostranauts.Inventory.InventoryWindowType.Container, null);
        return true;
    }
}
