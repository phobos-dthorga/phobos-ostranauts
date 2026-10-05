using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Audio;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The L2 canister filling station. Start arms it; while powered it works one vessel at a time: in Fill mode
/// it tops up suit bottles in its rack and linked canisters beside it to 99% of their rating, from switched-on bulk
/// stores (first) or a linked source canister; in Decant mode it empties them into switched-on bulk stores. Measured
/// electricity limits how much it moves (isothermal compression, see <see cref="FillerRules"/>), and all of it ends
/// as heat in the room. Its links and mode are saved; running permission is not, so a reload waits for Start.</summary>
internal static class FillerService
{
    private sealed class Session
    {
        internal FillerState State = new();
        internal bool Protected, Running, HeatWait, NeedsAttention;
        internal double NextCheck;
        internal string Status = Text.Get("Filler.paused");
        internal string? LastStop;
        internal readonly CompletionWatch Watch = new();
        /// <summary>Gas-line connections are rechecked every few seconds, never every power step.</summary>
        internal readonly Dictionary<string, (double Until, bool Connected)> Routes = new(StringComparer.Ordinal);
        internal string WatchActor = "", WatchShip = "";
    }
    internal sealed class Transfer { internal RoomHeat.Air Air = null!; internal double HeatFraction; internal EnergyReceipt Receipt = null!; }
    /// <summary>One piece of work: a vessel to fill or empty, where the gas comes from or goes, and its species.</summary>
    private sealed class Job
    {
        internal CondOwner Vessel = null!; internal CondOwner Other = null!; internal string Species = ""; internal bool Decant; internal double WantedKg, KWhPerKg;
    }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    private static readonly ConditionalWeakTable<Powered, Transfer> pending = new();
    internal static void Reset() => sessions = new();
    private const double MinimumKg = 1e-4;

    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, FillerRules.Record, Plugin.Id, 1);
    private static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co, out var found)) return found;
        var s = new Session();
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Ready) { try { s.State = FillerState.Read(fields); } catch (Exception e) { s.Protected = true; Plugin.Log(e.ToString()); } }
        else if (status != SavedStateStatus.Missing) s.Protected = true;
        if (s.Protected) s.Status = Text.Get("Filler.protected");
        sessions.Add(co, s);
        return s;
    }
    // A changed setting is looked at again at once; a power step's own save keeps the back-off it has just set
    // (Manufacturing 0.31.0: the reset used to cancel it, so a station that could move nothing searched every step).
    private static bool Save(CondOwner co, Session s, bool recheck = true)
    {
        if (Store(co).TryWriteIfChanged(s.State.Save())) { if (recheck) s.NextCheck = 0; return true; }
        s.Protected = true; s.Status = Text.Get("Filler.protected"); return false;
    }
    internal static bool Protected(CondOwner co) => Get(co).Protected;
    internal static FillerState StateOf(CondOwner co) => Get(co).State;

    // --- What it can reach ---------------------------------------------------------------------------------------
    /// <summary>Suit bottles in its own rack.</summary>
    internal static IEnumerable<CondOwner> Rack(CondOwner co) => co.objContainer?.ContainedCOs.Where(NativeGasVessel.IsBottle).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray()
        ?? Enumerable.Empty<CondOwner>();
    /// <summary>The game's own O2, N2 and CO2 canisters installed within one tile.</summary>
    /// <summary>The game's canisters of a gas the station handles anywhere aboard, in any position: what the canister
    /// field's note explains when one is not offered (Manufacturing 0.54.0). Suit bottles are never filled here.</summary>
    internal static IEnumerable<CondOwner> CanistersAboard(CondOwner co) => (co.ship?.GetCOs(null, false, false, true) ?? Enumerable.Empty<CondOwner>())
        .Where(c => c != null && !c.bDestroyed && c.ship == co.ship && c != co && !NativeGasVessel.IsBottle(c) &&
            NativeGasVessel.TryRead(c, out var r) && FillerRules.Species.Contains(r.Species)).ToArray();
    internal static IEnumerable<CondOwner> CanisterCandidates(CondOwner co) => (co.ship?.GetCOs(null, false, false, true) ?? Enumerable.Empty<CondOwner>())
        .Where(c => c != null && !c.bDestroyed && c.ship == co.ship && c != co && c.HasCond("IsInstalled") && !NativeGasVessel.IsBottle(c) &&
            NativeGasVessel.TryRead(c, out var r) && FillerRules.Species.Contains(r.Species) && ProcessorService.Adjacent(co, c))
        .OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    /// <summary>Oxygen, nitrogen and carbon dioxide stores of any size touching the station or on its gas line.</summary>
    internal static IEnumerable<CondOwner> StoreCandidates(CondOwner co) => GasLine.Stores(co, Fills);
    /// <summary>A store of a gas the station handles.</summary>
    internal static bool Fills(CondOwner c) => GasStores.For(c.strCODef) is GasStore g && FillerRules.Species.Contains(g.Family.Species);
    private static bool CanisterReady(CondOwner co, CondOwner c) => !c.bDestroyed && c.ship == co.ship && c.HasCond("IsInstalled") && ProcessorService.Adjacent(co, c) && NativeGasVessel.TryRead(c, out _);
    private static bool StoreReady(CondOwner co, Session s, CondOwner store)
    {
        if (store.bDestroyed || store.ship != co.ship || !GasStores.IsFamily(store.strCODef) || !NativeFluidRoute.EndpointReady(store) || CommodityReservations.Held(store.strID)) return false;
        var snapshot = BulkVessel.Snapshot(store);
        return !snapshot.Protected && snapshot.CatchKg <= 1e-8 && Connected(co, s, store);
    }
    // Connection rechecks follow real time: a game-time interval would shrink to every frame at fast-forward.
    private static bool Connected(CondOwner co, Session s, CondOwner store)
    {
        if (s.Routes.TryGetValue(store.strID, out var cached) && Cadence.RealTime < cached.Until) return cached.Connected;
        bool connected = GasLine.Connection(co, store) != null;
        s.Routes[store.strID] = (Cadence.RealTime + FillerRules.RecheckSeconds, connected);
        return connected;
    }
    private static IEnumerable<CondOwner> Linked(CondOwner co, Session s, FillerLinkKind kind, bool enabledOnly) =>
        s.State.OfKind(kind).Where(l => !enabledOnly || l.Enabled).Select(l => CrewWork.Resolve(l.Id)).Where(c => c != null).Cast<CondOwner>();

    /// <summary>The next piece of work, or null with the reason nothing can be done.</summary>
    private static Job? NextJob(CondOwner co, Session s, out string reason, out bool finished)
    {
        var vessels = Rack(co).Concat(Linked(co, s, FillerLinkKind.Target, true).Where(c => CanisterReady(co, c))).ToList();
        var stores = Linked(co, s, FillerLinkKind.Store, true).Where(c => StoreReady(co, s, c)).ToList();
        if (s.State.Mode == FillerMode.Decant)
        {
            // Decant empties rack bottles and every linked canister, sources included, into matching stores.
            vessels.AddRange(Linked(co, s, FillerLinkKind.Source, true).Where(c => CanisterReady(co, c)));
            reason = vessels.Count == 0 ? Text.Get("Filler.nothing_linked") : Text.Get("Filler.nothing_to_decant");
            finished = vessels.Count > 0;
            foreach (var vessel in vessels)
            {
                if (!NativeGasVessel.TryRead(vessel, out var r)) continue;
                double heldKg = NativeGasCanister.Kilograms(r.Species, r.Moles);
                if (heldKg <= MinimumKg) continue;
                var store = stores.FirstOrDefault(x => BulkVessels.Of(x)?.Commodity == FillerRules.Commodity(r.Species) && BulkVessel.Snapshot(x).HeadroomKg > MinimumKg);
                if (store == null) { reason = Text.Get("Filler.no_store_room", Gas(r.Species)); finished = false; continue; }
                reason = ""; finished = false;
                return new Job { Vessel = vessel, Other = store, Species = r.Species, Decant = true, WantedKg = Math.Min(heldKg, BulkVessel.Snapshot(store).HeadroomKg),
                    KWhPerKg = FillerRules.KWhPerKg(r.Species, BulkVesselDelivery) };
            }
            return null;
        }
        var sources = Linked(co, s, FillerLinkKind.Source, true).Where(c => CanisterReady(co, c)).ToList();
        reason = vessels.Count == 0 ? Text.Get("Filler.nothing_linked") : Text.Get("Filler.all_full");
        finished = vessels.Count > 0;
        foreach (var vessel in vessels)
        {
            if (vessel.HasCond("IsDamaged") || !NativeGasVessel.TryRead(vessel, out var r)) continue;
            double roomKg = NativeGasCanister.Kilograms(r.Species, r.HeadroomMoles(FillerRules.FillFraction));
            if (roomKg <= MinimumKg) continue;
            string? commodity = FillerRules.Commodity(r.Species);
            CondOwner? from = stores.FirstOrDefault(x => BulkVessels.Of(x)?.Commodity == commodity && BulkVessel.Snapshot(x).AvailableKg > MinimumKg);
            double availableKg = from == null ? 0 : BulkVessel.Snapshot(from).AvailableKg;
            if (from == null)
            {
                from = sources.FirstOrDefault(x => x != vessel && NativeGasVessel.TryRead(x, out var q) && q.Species == r.Species && q.Moles > 1e-9);
                availableKg = from != null && NativeGasVessel.TryRead(from, out var q2) ? NativeGasCanister.Kilograms(r.Species, q2.Moles) : 0;
            }
            if (from == null) { reason = Text.Get("Filler.no_source", Gas(r.Species)); finished = false; continue; }
            reason = ""; finished = false;
            return new Job { Vessel = vessel, Other = from, Species = r.Species, WantedKg = Math.Min(roomKg, availableKg), KWhPerKg = FillerRules.KWhPerKg(r.Species, r.MaxPressureKPa) };
        }
        return null;
    }
    /// <summary>A bulk store takes gas back at the native canister rating (authored: stores run at canister pressure).</summary>
    private const double BulkVesselDelivery = 41400;
    /// <summary>Moves up to <paramref name="kg"/> for a job; returns what arrived.</summary>
    private static double Move(Job job, double kg)
    {
        if (job.Decant) return GasTransfers.VesselToStore(job.Vessel, job.Other, job.Species, kg);
        return BulkVessels.IsVessel(job.Other) ? GasTransfers.StoreToVessel(job.Other, job.Vessel, job.Species, kg, FillerRules.FillFraction)
            : GasTransfers.VesselToVessel(job.Other, job.Vessel, job.Species, kg, FillerRules.FillFraction);
    }
    private static string Gas(string species) => Text.Get("Filler.gas_" + species);

    // --- Running ---------------------------------------------------------------------------------------------------
    internal static string? MachineProblem(CondOwner co)
    {
        if (!Content.Ready) return Content.Status;
        if (co == null || co.bDestroyed || co.strCODef != FillerRules.Installed || !co.HasCond("IsInstalled")) return Text.Get("Filler.install_first");
        if (co.HasCond("IsDamaged")) return Text.Get("Filler.repair_first");
        if (co.ship == null || (int)co.ship.LoadState < 2) return Text.Get("Content.ship_not_loaded");
        if (co.HasCond("IsLocked")) return Text.Get("Filler.unlock");
        if (co.HasCond("IsOverrideOff") || co.HasCond("IsSignalOff")) return Text.Get("Filler.switched_off");
        return null;
    }
    // Written only when it changes: an idle machine reaches this on every power step.
    private static void SetWorking(CondOwner co, bool value) { if (co.HasCond(ManufacturingRules.Filling) != value) co.SetCondAmount(ManufacturingRules.Filling, value ? 1 : 0); }
    /// <summary>afterLoad: the one start a machine that was running gets after a reload, where no crew member need stand by.</summary>
    internal static bool Start(CondOwner co, ConsoleBinding? binding = null, bool afterLoad = false)
    {
        var s = Get(co);
        string? problem = (afterLoad ? null : Content.Access(co, binding)) ?? MachineProblem(co);
        if (problem != null) { s.Status = problem; return false; }
        if (s.Protected) { s.Status = Text.Get("Filler.protected"); return false; }
        s.LastStop = null; s.NeedsAttention = false; s.Running = true; Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Mark(co, true); s.NextCheck = 0;
        s.Status = Text.Get(s.State.Mode == FillerMode.Decant ? "Filler.armed_decant" : "Filler.armed_fill");
        // A quiet cue when this fill run finishes, for the crew member who started it.
        var actor = CrewWork.Actor ?? CrewSim.GetSelectedCrew();
        s.WatchActor = actor?.strID ?? ""; s.WatchShip = co.ship?.strRegID ?? "";
        s.Watch.Arm(s.WatchActor, s.WatchShip);
        return true;
    }
    internal static bool Pause(CondOwner co, ConsoleBinding? binding = null)
    {
        var s = Get(co);
        string? problem = Content.Access(co, binding);
        if (problem != null) { s.Status = problem; return false; }
        CrewWork.ManualStop(co);
        Stop(co, s, Text.Get("Filler.paused"), false);
        return true;
    }
    private static void Stop(CondOwner co, Session s, string message, bool needsAttention = true)
    {
        s.LastStop = message; s.NeedsAttention = needsAttention; s.Running = false; Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Mark(co, false); s.HeatWait = false; s.Status = message; SetWorking(co, false);
        s.Watch.Cancel();
    }
    internal static void Fault(CondOwner co, Exception ex) { var s = Get(co); Stop(co, s, Text.Get("Filler.fault")); Plugin.Log(ex.ToString()); }

    /// <summary>Before the native power step: work only while there is something to move.</summary>
    internal static void BeforePower(CondOwner co)
    {
        if (!sessions.TryGetValue(co, out var s) || !s.Running)
        {
            // Work that was running when the game was saved carries on (Manufacturing 0.47.0), through Start's own checks.
            if (!Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Due(co)) { SetWorking(co, false); return; }
            if (!Start(co, null, afterLoad: true)) { Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Mark(co, false); SetWorking(co, false); return; }
            s = Get(co); s.Status = Text.Get("Content.resumed");
        }
        var problem = MachineProblem(co);
        if (problem != null) { Stop(co, s, problem); return; }
        if (s.Protected) { Stop(co, s, Text.Get("Filler.protected")); return; }
        if (Cadence.RealTime < s.NextCheck && !co.HasCond(ManufacturingRules.Filling)) return;
        try
        {
            var job = NextJob(co, s, out string why, out bool finished);
            if (job == null)
            {
                s.NextCheck = Cadence.RealTime + FillerRules.RecheckSeconds; SetWorking(co, false);
                s.Status = Text.Get("Filler.waiting", why);
                // Everything linked is full (or empty, when decanting): the watched run is complete.
                if (s.Watch.Armed && finished)
                    CompletionCues.Complete(s.Watch, s.WatchActor, s.WatchShip);
                return;
            }
        }
        catch (Exception ex) { Fault(co, ex); return; }
        SetWorking(co, true);
    }
    internal static bool BeginPower(Powered power, CondOwner co, ref double amount, out Transfer? transfer)
    {
        transfer = null;
        pending.Remove(power);
        bool working = co.HasCond(ManufacturingRules.Filling);
        double demand = working ? FillerRules.WorkingKW : FillerRules.IdleKW;
        double seconds = amount * Units.SecondsPerHour / demand, heatKW = demand * FillerRules.RoomHeatFraction;
        var air = RoomHeat.Read(co);
        var heat = RoomHeat.Check(air, heatKW, seconds);
        if (!heat.Admitted)
        {
            if (sessions.TryGetValue(co, out var s) && s.Running) { s.HeatWait = true; s.Status = RoomHeat.Describe(heat); }
            co.ZeroCondAmount("IsPowered");
            return false;
        }
        if (sessions.TryGetValue(co, out var ready)) ready.HeatWait = false;
        transfer = new Transfer { Air = air!, HeatFraction = FillerRules.RoomHeatFraction, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
        pending.Add(power, transfer);
        return true;
    }
    internal static void FinishPower(Powered power, CondOwner co, Transfer? transfer)
    {
        pending.Remove(power);
        if (transfer == null) return;
        double supplied = NativeEnergyReceipts.Complete(power, co, transfer.Receipt);
        if (!ManufacturingRules.Finite(supplied)) throw new InvalidOperationException("Invalid filling station energy receipt.");
        RoomHeat.Deposit(transfer.Air, supplied, transfer.HeatFraction);
        if (!sessions.TryGetValue(co, out var s) || !s.Running || !co.HasCond(ManufacturingRules.Filling) || s.Protected) return;
        // Idle draw is not work: only the share above the idle demand moves gas.
        double work = supplied * Math.Max(0, FillerRules.WorkingKW - FillerRules.IdleKW) / FillerRules.WorkingKW;
        // One job is chosen per step and kept while it still has room and its source still gives; the next
        // vessel is looked for only when this one is done or its source fell short.
        var job = NextJob(co, s, out _, out _);
        Job? last = null;
        for (int i = 0; i < 8 && work > 1e-12 && job != null; i++)
        {
            double kg = FillerRules.KgFor(work, job.KWhPerKg, job.WantedKg);
            double moved = kg <= 0 ? 0 : Move(job, kg);
            // Nothing moved: stand down and look again after the recheck interval, instead of searching every step
            // at working power.
            if (moved <= 1e-9) { s.NextCheck = Cadence.RealTime + FillerRules.RecheckSeconds; SetWorking(co, false); break; }
            work -= moved * job.KWhPerKg;
            if (job.Decant) s.State.DecantedKg += moved; else s.State.FilledKg += moved;
            last = job;
            job.WantedKg -= moved;
            if (job.WantedKg <= 1e-9 || moved < kg - 1e-9) job = NextJob(co, s, out _, out _);
        }
        // The status names the last vessel worked this step; it is written once, not once per move.
        if (last != null) s.Status = Text.Get(last.Decant ? "Filler.decanting" : "Filler.filling", ObjectPresentation.Name(last.Vessel), Gas(last.Species), ObjectPresentation.Name(last.Other));
        Save(co, s, recheck: false);
    }
    internal static void Forget(Powered power) { pending.Remove(power); NativeEnergyReceipts.Forget(power); }

    // --- Presentation and commands -------------------------------------------------------------------------------
    internal static EquipmentState State(CondOwner co)
    {
        if (co.HasCond("IsDamaged") || co.HasCond("IsLocked")) return EquipmentState.Blocked;
        var s = Get(co);
        if (s.NeedsAttention || s.Protected) return EquipmentState.Blocked;
        if (co.HasCond(ManufacturingRules.Filling)) return co.HasCond("IsPowered") ? EquipmentState.Running : EquipmentState.Waiting;
        return s.Running ? EquipmentState.Waiting : EquipmentState.Paused;
    }
    private static string VesselLine(CondOwner c)
    {
        if (!NativeGasVessel.TryRead(c, out var r)) return Text.Get("Filler.vessel_unreadable", ObjectPresentation.Name(c));
        return Text.Get("Filler.vessel_line", ObjectPresentation.Name(c), Gas(r.Species), NativeGasCanister.Kilograms(r.Species, r.Moles), r.FillFraction * 100);
    }
    internal static string Describe(CondOwner co)
    {
        var s = Get(co);
        if (s.Protected) return Text.Get("Filler.protected");
        var lines = new List<string>
        {
            s.Status,
            Text.Get(s.State.Mode == FillerMode.Decant ? "Filler.mode_decant" : "Filler.mode_fill") + " " + Text.Get("Filler.totals", s.State.FilledKg, s.State.DecantedKg),
            co.HasCond("IsPowered") ? Text.Get("Content.powered") : Text.Get("Content.no_power")
        };
        var rack = Rack(co).ToArray();
        lines.Add(rack.Length == 0 ? Text.Get("Filler.rack_empty") : Text.Get("Filler.rack_header", rack.Length, FillerRules.RackCells));
        lines.AddRange(rack.Select(VesselLine));
        foreach (var link in s.State.Links)
        {
            var c = CrewWork.Resolve(link.Id);
            string role = Text.Get(link.Kind == FillerLinkKind.Store ? "Filler.role_store" : link.Kind == FillerLinkKind.Source ? "Filler.role_source" : "Filler.role_target");
            string state = Text.Get(link.Enabled ? "Provider.on" : "Provider.off");
            if (c == null) { lines.Add(Text.Get("Filler.link_missing_line", ObjectPresentation.Name(link.Id), role)); continue; }
            if (link.Kind == FillerLinkKind.Store)
            {
                var snap = BulkVessels.IsVessel(c) ? BulkVessel.Snapshot(c) : null;
                lines.Add(Text.Get("Filler.store_line", ObjectPresentation.Name(c), role, state, snap?.ServiceKg ?? 0, snap?.CapacityKg ?? 0));
            }
            else lines.Add(role + " (" + state + "): " + VesselLine(c));
        }
        lines.Add(Text.Get("Filler.demand", FillerRules.WorkingKW, FillerRules.FillFraction * 100));
        if (s.LastStop != null) lines.Add(Text.Get("Content.last_stop", s.LastStop));
        return string.Join("\n", lines);
    }
    internal static string? MaintenanceReason(CondOwner co) => FillerRules.IsFamily(co.strCODef) && Get(co).Protected ? Text.Get("Maintenance.protected") : null;
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Content.Ready ? Content.Access(co, binding) ?? "" : Content.Status;
        if (message.Length > 0) return false;
        var s = Get(co);
        switch (action)
        {
            case "status": message = Describe(co); return true;
            case "start": { bool ok = Start(co, binding); message = s.Status; return ok; }
            case "pause": case "cancel": { bool ok = Pause(co, binding); message = s.Status; return ok; }
            case "accept":
            {
                var status = Store(co).Read(out var fields);
                FillerState? state = null;
                try { state = status == SavedStateStatus.Ready ? FillerState.Read(fields) : status == SavedStateStatus.Missing ? new FillerState() : null; } catch (Exception e) { Plugin.Log(e.ToString()); }
                if (state == null) { message = Text.Get("Filler.accept_unavailable"); return false; }
                s.State = state; s.Protected = false;
                bool ok = Save(co, s); message = Text.Get(ok ? "Filler.accept_done" : "Filler.accept_unavailable"); if (ok) s.Status = Text.Get("Filler.paused"); return ok;
            }
        }
        if (s.Protected) { message = Text.Get("Filler.protected"); return false; }
        string[] parts = action.Split(new[] { ':' }, 2);
        string verb = parts[0], arg = parts.Length > 1 ? parts[1] : "";
        bool changed;
        switch (verb)
        {
            case "mode":
                if (s.Running) { message = Text.Get("Filler.link_busy"); return false; }
                s.State.Mode = arg == "decant" ? FillerMode.Decant : FillerMode.Fill; changed = true;
                message = Text.Get(s.State.Mode == FillerMode.Decant ? "Filler.mode_decant" : "Filler.mode_fill"); break;
            case "link":
            {
                bool store = StoreCandidates(co).Any(c => c.strID == arg), canister = !store && CanisterCandidates(co).Any(c => c.strID == arg);
                if ((!store && !canister) || !s.State.Link(arg, store ? FillerLinkKind.Store : FillerLinkKind.Target)) { message = Text.Get(store || canister ? "Filler.full" : "Filler.link_missing"); return false; }
                changed = true; message = Text.Get(store ? "Filler.store_linked" : "Filler.canister_linked"); break;
            }
            case "unlink": changed = s.State.Unlink(arg); message = Text.Get(changed ? "Filler.unlinked" : "Filler.link_missing"); break;
            case "source-on": case "source-off":
                changed = s.State.Switch(arg, verb == "source-on"); message = Text.Get(!changed ? "Filler.link_missing" : verb == "source-on" ? "Filler.source_on" : "Filler.source_off"); break;
            case "target": case "draw":
                changed = s.State.Role(arg, verb == "draw" ? FillerLinkKind.Source : FillerLinkKind.Target);
                message = Text.Get(!changed ? "Filler.link_missing" : verb == "draw" ? "Filler.role_source_set" : "Filler.role_target_set"); break;
            default: message = Text.Get("Content.unsupported_action"); return false;
        }
        if (changed && !Save(co, s)) { message = Text.Get("Filler.protected"); return false; }
        return changed;
    }
}
