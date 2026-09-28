using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Moves D4 ordinary products and R4 steel, one physical item at a time, into one chosen
/// native store. The selection is saved on the sending machine; permission to unload is not saved
/// and pauses on reload. The store keeps its own native capacity; nothing is created or discarded.</summary>
internal sealed class StorageService
{
    private sealed class Session
    {
        internal bool Armed, NeedsAttention;
        internal CondOwner? Item, Store;
        internal TransferClock? Clock;
        internal CollectorRoute? Route;
        internal double Last;
        internal string Status = Text.Get("Storage.paused");
    }
    private ConditionalWeakTable<CondOwner, Session> sessions = new();
    private readonly Action<string> log;
    private readonly Settings options;
    internal StorageService(Action<string> log, Settings options) { this.log = log; this.options = options; }
    internal void Reset() => sessions = new ConditionalWeakTable<CondOwner, Session>();
    private Session Get(CondOwner machine) => sessions.GetValue(machine, _ => new Session());

    internal static bool Supported(CondOwner? co) => co != null && StorageRules.Port(co.strCODef) != null;
    private static ObjectStateStore Saved(CondOwner co) => new(co.mapGUIPropMaps, StorageSelection.StoreName, Text.Owner, StorageSelection.Schema);
    internal static SavedStateStatus Read(CondOwner co, out StorageSelection? selection)
    {
        selection = null;
        var status = Saved(co).Read(out var fields);
        if (status != SavedStateStatus.Ready) return status;
        if (!StorageSelection.TryLoad(fields, StorageRules.Port(co.strCODef), out var loaded)) return SavedStateStatus.Invalid;
        selection = loaded; return status;
    }
    /// <summary>The saved store ID, for locating or clearing a selection even when it no longer resolves.</summary>
    internal static string Selection(CondOwner co) => Read(co, out var selection) == SavedStateStatus.Ready ? selection!.StoreId : "none";
    internal static bool Eligible(CondOwner machine, CondOwner? store) => store != null && store != machine && machine.ship != null &&
        store.ship == machine.ship && store.Item != null && CrewWork.IsStore(store);
    internal static IEnumerable<CondOwner> Candidates(CondOwner machine) =>
        machine.ship == null ? Array.Empty<CondOwner>() : CrewWork.Stores(machine.ship).Where(c => Eligible(machine, c));
    private static bool Owned(CondOwner co) => CrewSim.coPlayer != null && co.ship != null && CrewSim.system?.GetShipOwner(co.ship.strRegID) == CrewSim.coPlayer.strID;
    private static string? MachineProblem(CondOwner machine) => !Supported(machine) ? Text.Get("Storage.unsupported") :
        ProcessingService.MachineProblem(machine) ?? (Owned(machine) ? null : Text.Get("Storage.owned_ship"));
    private static string? SelectionProblem(CondOwner machine, out CondOwner? store)
    {
        store = null;
        var status = Read(machine, out var selection);
        if (status == SavedStateStatus.Missing) return Text.Get("Storage.none");
        if (status != SavedStateStatus.Ready) return Text.Get("Storage.protected");
        store = CollectorService.Resolve(selection!.StoreId);
        if (store == null) return Text.Get("Storage.missing");
        return selection.ShipId == machine.ship?.strRegID && Eligible(machine, store) ? null : Text.Get("Storage.ineligible");
    }
    private static bool Payload(CondOwner machine, CondOwner item) => !item.bDestroyed && item.Crew == null &&
        StorageRules.Carries(machine.strCODef, item.strCODef, item.GetTotalMass(), !item.HasCond("IsInstalled"),
            item.GetCOsSafe(true).Count == 0 && item.GetLotCOs(true).Count == 0, item.coStackHead == null && item.aStack.Count == 0);
    private static void SetUnloading(CondOwner machine, bool on) { if (!machine.bDestroyed) machine.SetCondAmount(StorageRules.Unloading, on ? 1 : 0); }
    private static void Disarm(CondOwner machine, Session s) { s.Armed = false; SetUnloading(machine, false); }
    private static void Clear(CondOwner machine, Session s, string status)
    { Disarm(machine, s); s.Item = null; s.Clock = null; s.Store = null; s.Route = null; s.Status = status; }

    internal bool Link(CondOwner machine, string? storeId, ConsoleBinding? console, out string message)
    {
        var s = Get(machine);
        message = ProcessingService.AccessProblem(machine, console) ?? MachineProblem(machine) ?? "";
        if (message.Length > 0) return false;
        var store = storeId == null ? null : CollectorService.Resolve(storeId);
        if (!Eligible(machine, store)) { message = Text.Get("Storage.ineligible"); return false; }
        if (CollectorRoute.Find(store!, machine) == null) { message = Text.Get("Storage.no_route"); return false; }
        var status = Read(machine, out _);
        // An unreadable or foreign record is explicitly cleared with Unlink storage, never overwritten.
        if (status != SavedStateStatus.Missing && status != SavedStateStatus.Ready) { message = Text.Get("Storage.protected"); return false; }
        var selection = new StorageSelection { Port = StorageRules.Port(machine.strCODef)!, StoreId = store!.strID, ShipId = machine.ship.strRegID };
        if (!Saved(machine).TryWrite(selection.Save())) { message = Text.Get("Storage.protected"); return false; }
        Clear(machine, s, Text.Get("Storage.linked", CollectorService.Label(store)));
        message = s.Status; return true;
    }
    internal bool Unlink(CondOwner machine, ConsoleBinding? console, out string message)
    {
        message = !Supported(machine) ? Text.Get("Storage.unsupported") : ProcessingService.AccessProblem(machine, console) ?? "";
        if (message.Length > 0) return false;
        // Explicit forget, including an unreadable record. Products stay wherever they physically are.
        Saved(machine).Clear();
        Clear(machine, Get(machine), Text.Get("Storage.unlinked"));
        message = Get(machine).Status; return true;
    }
    internal bool Start(CondOwner machine, ConsoleBinding? console, out string message)
    {
        var s = Get(machine);
        CondOwner? store = null;
        string? problem = ProcessingService.AccessProblem(machine, console) ?? MachineProblem(machine);
        if (problem == null) problem = SelectionProblem(machine, out store);
        var route = problem == null ? CollectorRoute.Find(store!, machine) : null;
        if (problem == null && route == null) problem = Text.Get("Storage.no_route");
        if (problem != null) { s.Status = problem; message = problem; return false; }
        if (!ReferenceEquals(s.Store, store)) { s.Item = null; s.Clock = null; }
        s.Store = store; s.Route = route; s.Armed = true; s.NeedsAttention = false; s.Last = StarSystem.fEpoch;
        s.Status = Text.Get("Storage.enabled");
        message = Describe(machine); return true;
    }
    internal bool Pause(CondOwner machine, ConsoleBinding? console, out string message)
    {
        var s = Get(machine);
        message = ProcessingService.AccessProblem(machine, console) ?? "";
        if (message.Length > 0) return false;
        Disarm(machine, s); s.NeedsAttention = false; s.Status = Text.Get("Storage.paused_manual");
        message = s.Status; return true;
    }
    internal bool Unloading(CondOwner machine) => sessions.TryGetValue(machine, out var s) && s.Armed;

    /// <summary>Native power selection. Only a checked, admitted item requests feeder power.</summary>
    internal bool BeforePower(CondOwner machine)
    {
        SetUnloading(machine, false);
        return sessions.TryGetValue(machine, out var s) && s.Armed && Admit(machine, s);
    }
    private bool Admit(CondOwner machine, Session s)
    {
        SetUnloading(machine, false);
        CondOwner? store = null;
        string? problem = MachineProblem(machine);
        if (problem == null) problem = SelectionProblem(machine, out store);
        if (problem == null && (!ReferenceEquals(store, s.Store) || s.Route == null || !s.Route.Valid(store!, machine)))
            problem = Text.Get("Storage.route_changed");
        if (problem != null) { Disarm(machine, s); s.NeedsAttention = true; s.Status = problem; return false; }
        double now = StarSystem.fEpoch;
        if (s.Clock != null && !TransferClock.ValidStep(now - s.Last)) { Disarm(machine, s); s.Status = Text.Get("Storage.time_gap"); return false; }
        var tray = machine.objContainer;
        if (s.Item != null && (!tray.Contains(s.Item) || !Payload(machine, s.Item))) { s.Item = null; s.Clock = null; }
        if (s.Item == null)
        {
            s.Item = tray.ContainedCOs.OrderBy(i => i.strID, StringComparer.Ordinal).FirstOrDefault(i => Payload(machine, i));
            s.Last = now;
            if (s.Item == null) { s.Status = Text.Get("Storage.waiting"); return false; }
            s.Clock = new TransferClock(s.Item.strID, options.FeederSeconds);
        }
        var destination = store!.objContainer;
        // A full store keeps products in the tray and retries; nothing is dropped or discarded.
        if (destination == null || destination.Locked || !destination.AllowedCO(s.Item) || !destination.CanAddSimple(s.Item, out _))
        { s.Status = Text.Get("Storage.full"); s.Last = now; return false; }
        SetUnloading(machine, true); return true;
    }
    /// <summary>Settles one native power interval: powered seconds advance the item clock, then the
    /// same physical item is revalidated and moved in one checked transfer.</summary>
    internal void AfterPower(CondOwner machine, bool requested, double? poweredSeconds = null)
    {
        if (!sessions.TryGetValue(machine, out var s) || !s.Armed) return;
        try
        {
            double now = StarSystem.fEpoch, elapsed = now - s.Last; s.Last = now;
            if (!TransferClock.ValidStep(elapsed)) { Disarm(machine, s); s.Status = Text.Get("Storage.time_gap"); return; }
            var clock = s.Clock;
            if (!Admit(machine, s) || s.Clock == null || s.Item == null) return;
            if (!ReferenceEquals(clock, s.Clock)) elapsed = 0;
            bool powered = requested && machine.HasCond("IsPowered");
            if (!s.Clock.Advance(s.Item.strID, poweredSeconds.HasValue ? Math.Min(elapsed, poweredSeconds.Value) : elapsed, powered))
            { Disarm(machine, s); s.Status = Text.Get("Storage.time_gap"); return; }
            s.Status = powered ? Text.Get("Storage.moving", s.Clock.Progress.ToString("F0"), s.Clock.Duration.ToString("F0")) : Text.Get("Routing.no_power");
            if (!powered || !s.Clock.Complete) return;
            var move = new NativeItemTransfer(machine.objContainer, s.Store!.objContainer, s.Item);
            if (!PhysicalTransfer.Commit(move)) { s.Status = Text.Get("Storage.blocked"); SetUnloading(machine, false); return; }
            s.Item = null; s.Clock = null; SetUnloading(machine, false);
            s.Status = Text.Get("Storage.delivered"); move.Redraw();
            if (!options.FeederContinue) Disarm(machine, s);
        }
        catch (Exception ex) { Fault(machine, ex); }
    }
    internal void Fault(CondOwner machine, Exception ex)
    {
        var s = Get(machine); Disarm(machine, s); s.NeedsAttention = true;
        s.Status = Text.Get("Storage.fault"); log(ex.ToString());
    }
    internal bool NeedsAttention(CondOwner machine) => sessions.TryGetValue(machine, out var s) && s.NeedsAttention;
    /// <summary>Presentation only; reads the saved selection and session status.</summary>
    internal string Describe(CondOwner machine)
    {
        if (!Supported(machine)) return "";
        var s = Get(machine);
        var status = Read(machine, out var selection);
        var store = selection == null ? null : CollectorService.Resolve(selection.StoreId);
        string target = status == SavedStateStatus.Missing ? Text.Get("Storage.none_short") :
            status != SavedStateStatus.Ready ? Text.Get("Storage.protected_short") :
            store == null ? Text.Get("CollectorLinks.unavailable", selection!.StoreId) : CollectorService.Label(store);
        string products = string.Join(", ", StorageRules.Carried(machine.strCODef).Select(p => DataHandler.GetCondOwnerDef(p.Id)?.strNameFriendly ?? p.Id));
        return Text.Get("Storage.status", target, Text.Get(s.Armed ? "Storage.state_on" : "Storage.state_off"), s.Status, products);
    }
}
