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

/// <summary>Moves D4 ordinary products and R4 steel, one physical unit at a time (taken from a native stack if the
/// tray stacked them, Shipbreaker 0.56.0), into one chosen native store that touches the machine or shares a conveyor
/// belt with it. The selection is saved on the sending machine; running unloading is marked on it and resumes after a
/// reload (owner decision, 30 September 2026). The store keeps its own native capacity; nothing is created or discarded.</summary>
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
        // A true admission holds for the rest of its step (native power in between changes no item or store).
        internal long AdmittedStep = long.MinValue;
        // Whether this load already tried to resume unloading that was running when the game was saved.
        internal bool ResumeTried;
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
    /// <summary>Material bins are crew stores but admit only mined material, never these products (the native
    /// checks prove it), so they are not offered here rather than chosen and then stalling as full.</summary>
    internal static bool Eligible(CondOwner machine, CondOwner? store) => store != null && store != machine && machine.ship != null &&
        store.ship == machine.ship && store.Item != null && CrewWork.IsStore(store) && !BinRules.IsFamily(store.strCODef);
    /// <summary>Every suitable store aboard, in reach or not.</summary>
    internal static IEnumerable<CondOwner> Aboard(CondOwner machine) =>
        machine.ship == null ? Array.Empty<CondOwner>() : CrewWork.Stores(machine.ship).Where(c => Eligible(machine, c));
    /// <summary>The stores offered: those the machine touches or a conveyor belt joins it to (Shipbreaker 0.80.0; the
    /// list used to offer every store aboard and refuse the unreachable ones on Apply).</summary>
    internal static IEnumerable<CondOwner> Candidates(CondOwner machine) => Aboard(machine).Where(c => CollectorRoute.Find(c, machine) != null);
    internal static string Note(CondOwner machine) => LinkNotes.For(Aboard(machine), c => CollectorRoute.Find(c, machine) != null ? null : Text.Get("Links.no_belt"));
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
    /// <summary>One unit the machine may send: a single item, or one unit of a native stack judged at its own mass.</summary>
    private static bool Payload(CondOwner machine, CondOwner unit)
    {
        if (unit.bDestroyed || unit.Crew != null || !CrewLogistics.Loose(unit)) return false;
        bool stacked = unit.coStackHead != null || unit.aStack.Count > 0;
        return StorageRules.Carries(machine.strCODef, unit.strCODef, stacked ? unit.GetCondAmount("StatMass") : unit.GetTotalMass(), !unit.HasCond("IsInstalled"),
            unit.GetLotCOs(true).Count == 0, true);
    }
    /// <summary>The units in the machine's tray, stacks opened, in id order.</summary>
    private static IEnumerable<CondOwner> TrayUnits(CondOwner machine) => CrewLogistics.Contents(machine);
    private static void SetUnloading(CondOwner machine, bool on) { if (!machine.bDestroyed) machine.SetCondAmount(StorageRules.Unloading, on ? 1 : 0); }
    private static void Disarm(CondOwner machine, Session s) { s.Armed = false; SetUnloading(machine, false); SetResume(machine, false); BeltCarriers.Hide(Carrier(machine)); }
    /// <summary>The moving-item display's key for this machine's unloading.</summary>
    private static string Carrier(CondOwner machine) => machine.strID + ".storage";
    private static void SetResume(CondOwner machine, bool on) { if (!machine.bDestroyed && machine.HasCond(StorageRules.Resume) != on) machine.SetCondAmount(StorageRules.Resume, on ? 1 : 0); }
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
        string? access = ProcessingService.AccessProblem(machine, console);
        if (access != null) { Get(machine).Status = access; message = access; return false; }
        return Arm(machine, Get(machine), out message);
    }
    /// <summary>Arms unloading after every machine, selection and route check; no operator presence is needed.</summary>
    private bool Arm(CondOwner machine, Session s, out string message)
    {
        CondOwner? store = null;
        string? problem = MachineProblem(machine);
        if (problem == null) problem = SelectionProblem(machine, out store);
        var route = problem == null ? CollectorRoute.Find(store!, machine) : null;
        if (problem == null && route == null) problem = Text.Get("Storage.no_route");
        if (problem != null) { s.Status = problem; message = problem; return false; }
        if (!ReferenceEquals(s.Store, store)) { s.Item = null; s.Clock = null; }
        s.Store = store; s.Route = route; s.Armed = true; s.NeedsAttention = false; s.Last = StarSystem.fEpoch;
        s.Status = Text.Get("Storage.enabled"); SetResume(machine, true);
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
        ResumeAfterLoad(machine);
        SetUnloading(machine, false);
        return sessions.TryGetValue(machine, out var s) && s.Armed && Admit(machine, s);
    }
    /// <summary>Owner decision (30 September 2026): unloading that was running when the game was saved resumes once
    /// after the reload, through every check; if one fails it stays paused with the reason.</summary>
    private void ResumeAfterLoad(CondOwner machine)
    {
        if (!machine.HasCond(StorageRules.Resume) || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return;
        var s = Get(machine);
        if (s.Armed || s.ResumeTried) return;
        s.ResumeTried = true;
        if (Arm(machine, s, out _)) s.Status = Text.Get("Routing.resumed");
        else { SetResume(machine, false); s.NeedsAttention = true; }
    }
    private bool Admit(CondOwner machine, Session s)
    {
        if (s.AdmittedStep == Phobos.Ostranauts.Framework.Processing.NativeSteps.Frame && s.Armed && s.Item != null && s.Clock != null) return true;
        bool verdict = AdmitNow(machine, s);
        if (verdict) s.AdmittedStep = Phobos.Ostranauts.Framework.Processing.NativeSteps.Frame;
        return verdict;
    }
    private bool AdmitNow(CondOwner machine, Session s)
    {
        SetUnloading(machine, false);
        CondOwner? store = null;
        string? problem = MachineProblem(machine);
        if (problem == null) problem = SelectionProblem(machine, out store);
        if (problem == null && (!ReferenceEquals(store, s.Store) || s.Route == null || !s.Route.Valid(store!, machine)))
            problem = Text.Get("Storage.route_changed");
        if (problem != null) { Disarm(machine, s); s.NeedsAttention = true; s.Status = problem; return false; }
        double now = StarSystem.fEpoch;
        if (s.Item != null && (!TrayUnits(machine).Contains(s.Item) || !Payload(machine, s.Item))) { s.Item = null; s.Clock = null; }
        if (s.Item == null)
        {
            s.Item = TrayUnits(machine).FirstOrDefault(i => Payload(machine, i));
            s.Last = now;
            if (s.Item == null) { s.Status = Text.Get("Storage.waiting"); return false; }
            s.Clock = new TransferClock(s.Item.strID, options.FeederSeconds);
        }
        var destination = store!.objContainer;
        // A full store keeps products in the tray and retries; nothing is dropped or discarded.
        if (destination == null || destination.Locked || !UnitItemTransfer.Fits(store!, s.Item, out _))
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
            // A long interval (time-skip, reload gap) is not a fault: the paid power interval bounds the work.
            double now = StarSystem.fEpoch, elapsed = now - s.Last; s.Last = now;
            var clock = s.Clock;
            if (!Admit(machine, s) || s.Clock == null || s.Item == null) return;
            if (!ReferenceEquals(clock, s.Clock)) elapsed = 0;
            bool powered = requested && machine.HasCond("IsPowered");
            if (!s.Clock.Advance(s.Item.strID, poweredSeconds.HasValue ? Math.Min(elapsed, poweredSeconds.Value) : elapsed, powered))
            { s.Item = null; s.Clock = null; s.Status = Text.Get("Storage.waiting"); return; }
            s.Status = powered ? Text.Get("Storage.moving", s.Clock.Progress.ToString("F0"), s.Clock.Duration.ToString("F0")) : Text.Get("Routing.no_power");
            BeltCarriers.Show(Carrier(machine), machine.ship, s.Item, s.Route?.BeltPath, s.Clock.Duration > 0 ? s.Clock.Progress / s.Clock.Duration : 0);
            if (!powered || !s.Clock.Complete) return;
            var move = new UnitItemTransfer(s.Item, s.Store!);
            if (!PhysicalTransfer.Commit(move)) { s.Status = Text.Get("Storage.blocked"); SetUnloading(machine, false); return; }
            s.Item = null; s.Clock = null; SetUnloading(machine, false); BeltCarriers.Hide(Carrier(machine));
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
