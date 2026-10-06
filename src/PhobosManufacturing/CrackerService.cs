using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The AX-2 ammonia cracker: Start arms it; while powered and linked, it draws one cycle's ammonia from a
/// linked store into its hold, credits measured electricity to the cycle, and on completion turns the ammonia into
/// a product hold of nitrogen and hydrogen in one saved step. Products then go to the linked nitrogen and hydrogen
/// stores under both transfer guards. A new cycle begins only once the products are delivered, and only when both
/// stores have room for a whole cycle. Running permission is not saved: a reload waits for Start. The K2's pattern,
/// with one gas in and two out.</summary>
internal static class CrackerService
{
    private sealed class Session
    {
        internal CrackerState State = new();
        internal bool Protected, Running, HeatWait, OutputWait, NeedsAttention;
        internal double NextCheck;
        internal string Status = Text.Get("Cracker.paused");
        internal string? LastStop;
    }
    internal sealed class Transfer { internal RoomHeat.Air Air = null!; internal bool Working; internal EnergyReceipt Receipt = null!; }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    private static readonly ConditionalWeakTable<Powered, Transfer> pending = new();
    internal static void Reset() => sessions = new();

    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, CrackerRules.Record, Plugin.Id, 1);
    internal static LiquidTransferGuard Guard(CondOwner co) => new(co.mapGUIPropMaps, CrackerRules.Guard, Plugin.Id);
    private static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co, out var found)) return found;
        var s = new Session();
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Ready) { try { s.State = CrackerState.Read(fields); } catch (Exception e) { s.Protected = true; Plugin.Log(e.ToString()); } }
        else if (status != SavedStateStatus.Missing) s.Protected = true;
        if (!s.Protected && (Guard(co).Protected || !BulkVessel.MassMatches(co.GetCondAmount("StatMass"), CrackerRules.MachineKg + s.State.HeldKg))) s.Protected = true;
        if (s.Protected) s.Status = Text.Get("Cracker.protected");
        sessions.Add(co, s);
        return s;
    }
    private static void Save(CondOwner co, Session s)
    {
        if (!Store(co).TryWrite(s.State.Save())) { s.Protected = true; s.Status = Text.Get("Cracker.protected"); return; }
        co.AddMass(CrackerRules.MachineKg + s.State.HeldKg - co.GetCondAmount("StatMass"), true);
    }
    internal static bool Protected(CondOwner co) => Get(co).Protected;
    internal static bool Accept(CondOwner co)
    {
        var s = Get(co);
        var status = Store(co).Read(out var fields);
        CrackerState? state = null;
        try { state = status == SavedStateStatus.Ready ? CrackerState.Read(fields) : status == SavedStateStatus.Missing ? new CrackerState() : null; } catch (Exception e) { Plugin.Log(e.ToString()); }
        if (state == null || !Guard(co).Resolve()) return false;
        s.State = state; s.Protected = false; Save(co, s);
        if (!s.Protected) s.Status = Text.Get("Cracker.paused");
        return !s.Protected;
    }

    /// <summary>The three links: which of the cracker's ports, which port on the store, and the store's commodity.</summary>
    internal enum Link { Ammonia, Nitrogen, Hydrogen }
    private static readonly Link[] Links = { Link.Ammonia, Link.Nitrogen, Link.Hydrogen };
    private static string Commodity(Link link) => link == Link.Ammonia ? ManufacturingRules.Ammonia : link == Link.Nitrogen ? ManufacturingRules.Nitrogen : ManufacturingRules.Hydrogen;
    private static string Kind(Link link) => link == Link.Ammonia ? "ammonia" : link == Link.Nitrogen ? "nitrogen" : "hydrogen";
    /// <summary>The shared links (Manufacturing 0.23.0): each store touches the cracker or shares its gas line, and
    /// each store-side port is a bank several machines share.</summary>
    private static readonly VesselLink[] vesselLinks =
    {
        new(CrackerRules.AmmoniaInPort, CrackerRules.StoreOutPort, ManufacturingRules.Ammonia),
        new(CrackerRules.NitrogenOutPort, CrackerRules.StoreInPort, ManufacturingRules.Nitrogen),
        new(CrackerRules.HydrogenOutPort, CrackerRules.StoreInPort, ManufacturingRules.Hydrogen)
    };
    internal static VesselLink LinkOf(Link link) => vesselLinks[(int)link];
    private static string Peer(CondOwner co, Link link) => LinkOf(link).PeerId(co);
    internal static string AmmoniaPeer(CondOwner co) => Peer(co, Link.Ammonia);
    internal static string NitrogenPeer(CondOwner co) => Peer(co, Link.Nitrogen);
    internal static string HydrogenPeer(CondOwner co) => Peer(co, Link.Hydrogen);
    private static IEnumerable<CondOwner> Candidates(CondOwner co, Link link) => LinkOf(link).Candidates(co, v => GasStores.Holds(v.strCODef, Commodity(link)));
    internal static IEnumerable<CondOwner> AmmoniaCandidates(CondOwner co) => Candidates(co, Link.Ammonia);
    internal static IEnumerable<CondOwner> NitrogenCandidates(CondOwner co) => Candidates(co, Link.Nitrogen);
    internal static IEnumerable<CondOwner> HydrogenCandidates(CondOwner co) => Candidates(co, Link.Hydrogen);

    /// <summary>A linked store ready for this transfer, or null with the reason.</summary>
    // Runs every power step while the cracker works: reasons are formatted only on the way out, the store read once.
    private static CondOwner? Linked(CondOwner co, Link link, out string reason) => Linked(co, link, out reason, out _);
    // The snapshot taken for the readiness test is handed back, so a caller that also needs the quantities reads the
    // store once, and the text key is built only when a reason is given (Manufacturing 0.31.0).
    private static CondOwner? Linked(CondOwner co, Link link, out string reason, out BulkVesselSnapshot? snapshot)
    {
        snapshot = null;
        var store = CrewWork.Resolve(Peer(co, link));
        if (store == null || !GasStores.Holds(store.strCODef, Commodity(link))) { reason = Text.Get("Cracker." + Kind(link) + "_none"); return null; }
        if (!LinkOf(link).Connected(co, store)) { reason = Text.Get("Cracker." + Kind(link) + "_not_ready"); return null; }
        snapshot = BulkVessel.Snapshot(store);
        if (snapshot.Protected || CommodityReservations.Held(store.strID)) { reason = Text.Get("Cracker.store_protected"); return null; }
        if (snapshot.CatchKg > 1e-8) { reason = Text.Get("Cracker.store_catch"); return null; }
        reason = ""; return store;
    }
    private static CondOwner? AmmoniaSource(CondOwner co, double needKg, out string reason)
    {
        var store = Linked(co, Link.Ammonia, out reason, out var s);
        if (store == null || s == null) return null;
        if (s.AvailableKg + 1e-9 < needKg) { reason = Text.Get("Cracker.ammonia_short", s.AvailableKg, needKg); return null; }
        return store;
    }
    private static CondOwner? Destination(CondOwner co, Link link, double kg, out string reason)
    {
        var store = Linked(co, link, out reason, out var s);
        if (store == null || s == null) return null;
        if (s.HeadroomKg + 1e-9 < kg) { reason = Text.Get("Cracker." + Kind(link) + "_full", s.HeadroomKg, kg); return null; }
        return store;
    }

    internal static string? MachineProblem(CondOwner co)
    {
        if (!Content.Ready) return Content.Status;
        if (co == null || co.bDestroyed || co.strCODef != CrackerRules.Installed || !co.HasCond("IsInstalled")) return Text.Get("Cracker.install_first");
        if (co.HasCond("IsDamaged")) return Text.Get("Cracker.repair_first");
        if (co.ship == null || (int)co.ship.LoadState < 2) return Text.Get("Content.ship_not_loaded");
        if (co.HasCond("IsLocked")) return Text.Get("Cracker.unlock");
        if (co.HasCond("IsOverrideOff") || co.HasCond("IsSignalOff")) return Text.Get("Cracker.switched_off");
        return null;
    }
    // Written only when it changes: an idle machine reaches this on every power step.
    private static void SetWorking(CondOwner co, bool value) { if (co.HasCond(ManufacturingRules.Reacting) != value) co.SetCondAmount(ManufacturingRules.Reacting, value ? 1 : 0); }

    /// <summary>afterLoad: the one start a machine that was running gets after a reload, where no crew member need stand by.</summary>
    internal static bool Start(CondOwner co, ConsoleBinding? binding = null, bool afterLoad = false)
    {
        var s = Get(co);
        string? problem = (afterLoad ? null : Content.Access(co, binding)) ?? MachineProblem(co);
        if (problem != null) { s.Status = problem; return false; }
        if (s.Protected) { s.Status = Text.Get("Cracker.protected"); return false; }
        s.LastStop = null; s.NeedsAttention = false; s.Running = true; Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Mark(co, true); s.Status = Text.Get("Cracker.armed");
        return true;
    }
    internal static bool Pause(CondOwner co, bool cancel, ConsoleBinding? binding = null)
    {
        var s = Get(co);
        string? problem = Content.Access(co, binding);
        if (problem != null) { s.Status = problem; return false; }
        CrewWork.ManualStop(co);
        Stop(co, s, Text.Get(cancel ? "Cracker.cancelled" : "Cracker.paused_retained"), needsAttention: false);
        // Cancelling forfeits the cycle's energy; held ammonia and made products stay (they are real mass).
        if (cancel && !s.Protected) { s.State.CycleKWh = 0; Save(co, s); }
        return !s.Protected;
    }
    private static void Stop(CondOwner co, Session s, string message, bool needsAttention = true)
    {
        s.LastStop = message; s.NeedsAttention = needsAttention; s.Running = false; Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Mark(co, false); s.HeatWait = false; s.OutputWait = false;
        s.Status = message; SetWorking(co, false);
    }
    internal static void Fault(CondOwner co, Exception ex)
    {
        var s = Get(co);
        Stop(co, s, Text.Get("Cracker.fault")); s.NeedsAttention = true;
        Plugin.Log(ex.ToString());
    }
    private static void Wait(CondOwner co, Session s, string status)
    {
        // Rechecks follow real time: a game-time interval would shrink to every frame at fast-forward.
        s.OutputWait = true; s.NextCheck = Cadence.RealTime + ManufacturingRules.VesselRecheckSeconds; s.Status = status; SetWorking(co, false);
    }

    /// <summary>Before the native power step: deliver waiting products first, then charge the cracker, and work only
    /// when this cycle's products will have somewhere to go.</summary>
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
        if (s.Protected) { Stop(co, s, Text.Get("Cracker.protected")); return; }
        if (s.OutputWait && Cadence.RealTime < s.NextCheck) { SetWorking(co, false); return; }
        s.OutputWait = false;
        try
        {
            if (s.State.HoldsProducts && !Deliver(co, s)) return;
            if (!Charge(co, s)) return;
            if (Destination(co, Link.Nitrogen, CrackerRules.NitrogenKgPerCycle, out string nitrogenWhy) == null) { Wait(co, s, Text.Get("Cracker.waiting_output", nitrogenWhy)); return; }
            if (Destination(co, Link.Hydrogen, CrackerRules.HydrogenKgPerCycle, out string hydrogenWhy) == null) { Wait(co, s, Text.Get("Cracker.waiting_output", hydrogenWhy)); return; }
        }
        catch (Exception ex) { Fault(co, ex); return; }
        SetWorking(co, true);
    }
    /// <summary>Tops up the ammonia hold from the linked store under both guards.</summary>
    private static bool Charge(CondOwner co, Session s)
    {
        double need = CrackerRules.AmmoniaKgPerCycle - s.State.AmmoniaKg;
        if (need <= 1e-9) return true;
        var store = AmmoniaSource(co, need, out string why);
        if (store == null) { Wait(co, s, Text.Get("Cracker.waiting_ammonia", why)); return false; }
        LiquidTransferGuard.Commit(new BulkVessel.Endpoint(store), new Hold(co, s, ManufacturingRules.Ammonia), need, BulkVessel.Guard(store), Guard(co));
        if (s.Protected) { Stop(co, s, Text.Get("Cracker.protected")); return false; }
        return s.State.Charged;
    }
    /// <summary>Sends made products to their stores; returns true when nothing is left waiting.</summary>
    private static bool Deliver(CondOwner co, Session s)
    {
        foreach (var link in new[] { Link.Nitrogen, Link.Hydrogen })
        {
            double kg = link == Link.Nitrogen ? s.State.NitrogenKg : s.State.HydrogenKg;
            if (kg <= 1e-9) continue;
            var store = Destination(co, link, kg, out string why);
            if (store == null) { Wait(co, s, Text.Get("Cracker.waiting_output", why)); return false; }
            LiquidTransferGuard.Commit(new Hold(co, s, Commodity(link)), new BulkVessel.Endpoint(store), kg, Guard(co), BulkVessel.Guard(store));
            if (s.Protected) { Stop(co, s, Text.Get("Cracker.protected")); return false; }
            if (link == Link.Nitrogen) s.State.ProducedNitrogenKg += kg; else s.State.ProducedHydrogenKg += kg;
            Save(co, s);
        }
        if (s.State.HoldsProducts) return false;
        Plugin.Log(Text.Get("Cracker.cycle_log", co.strID, CrackerRules.NitrogenKgPerCycle, CrackerRules.HydrogenKgPerCycle));
        return true;
    }

    internal static bool BeginPower(Powered power, CondOwner co, ref double amount, out Transfer? transfer)
    {
        transfer = null;
        pending.Remove(power);
        bool working = co.HasCond(ManufacturingRules.Reacting);
        double demand = working ? CrackerRules.WorkingKW : CrackerRules.IdleKW;
        double seconds = amount * Units.SecondsPerHour / demand;
        // Crew upkeep (0.55.0): a tuned machine asks for more power while it works and does that much more work.
        double tune = working ? Phobos.Ostranauts.Framework.Crew.Upkeep.Draw(co, ref amount, seconds) : 1;
        var air = RoomHeat.Read(co);
        var heat = RoomHeat.Check(air, (CrackerRules.RoomHeatKW(working)) * tune, seconds);
        if (!heat.Admitted)
        {
            if (sessions.TryGetValue(co, out var s) && s.Running) { s.HeatWait = true; s.Status = RoomHeat.Describe(heat); }
            co.ZeroCondAmount("IsPowered");
            return false;
        }
        if (sessions.TryGetValue(co, out var ready)) ready.HeatWait = false;
        transfer = new Transfer { Air = air!, Working = working, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
        pending.Add(power, transfer);
        return true;
    }
    internal static void FinishPower(Powered power, CondOwner co, Transfer? transfer)
    {
        pending.Remove(power);
        if (transfer == null) return;
        double supplied = NativeEnergyReceipts.Complete(power, co, transfer.Receipt);
        if (!ManufacturingRules.Finite(supplied)) throw new InvalidOperationException("Invalid cracker energy receipt.");
        bool progressing = sessions.TryGetValue(co, out var s) && s.Running && transfer.Working && co.HasCond(ManufacturingRules.Reacting) && !s.Protected && s.State.Charged;
        if (!progressing) { RoomHeat.Deposit(transfer.Air, supplied); return; }
        double before = s!.State.CycleKWh;
        var (credited, complete) = CrackerRules.Advance(before, supplied);
        // The reaction stores its share of the cycle's electricity in the products; the rest warms the room.
        RoomHeat.Deposit(transfer.Air, Math.Max(0, supplied - CrackerRules.AbsorbedKWh(credited - before)));
        s.State.CycleKWh = credited;
        s.Status = co.HasCond("IsPowered") ? Text.Get("Cracker.working", ManufacturingRules.MinutesLeft(s.State.CycleKWh, CrackerRules.CycleKWh, CrackerRules.WorkingKW)) : Text.Get("Cracker.waiting_power");
        if (!complete) { Save(co, s); return; }
        s.State.Convert();
        Save(co, s);
        if (!s.Protected) Deliver(co, s);
    }
    internal static void Forget(Powered power) { pending.Remove(power); NativeEnergyReceipts.Forget(power); }

    /// <summary>One of the cracker's own holds as a reservoir for guarded transfers: ammonia coming in, nitrogen or
    /// hydrogen going out. The quantity is the saved record's, and setting it saves.</summary>
    private sealed class Hold : ILiquidReservoir
    {
        private readonly CondOwner co; private readonly Session s; private readonly string commodity;
        internal Hold(CondOwner co, Session s, string commodity) { this.co = co; this.s = s; this.commodity = commodity; }
        public string Identity => co.strID + "." + commodity;
        public string ShipId => co.ship.strRegID;
        public string Commodity => commodity;
        public double QuantityKg => commodity == ManufacturingRules.Ammonia ? s.State.AmmoniaKg : commodity == ManufacturingRules.Nitrogen ? s.State.NitrogenKg : s.State.HydrogenKg;
        public double CapacityKg => commodity == ManufacturingRules.Ammonia ? CrackerRules.AmmoniaKgPerCycle : commodity == ManufacturingRules.Nitrogen ? CrackerRules.NitrogenKgPerCycle : CrackerRules.HydrogenKgPerCycle;
        public void SetQuantity(double kg)
        {
            if (commodity == ManufacturingRules.Ammonia) s.State.AmmoniaKg = kg; else if (commodity == ManufacturingRules.Nitrogen) s.State.NitrogenKg = kg; else s.State.HydrogenKg = kg;
            Save(co, s);
        }
    }

    /// <summary>After the native damage switch: the cracker's gases escape. Ammonia and nitrogen go into the room as
    /// the game's own gases; its hydrogen burns by the fuel-store rule when the room has oxygen and an ignition
    /// source, otherwise it escapes too.</summary>
    internal static void Damaged(CondOwner co)
    {
        if (co == null || co.bDestroyed || !co.HasCond("IsDamaged") || !co.HasCond("IsInstalled")) return;
        try { Dump(co, "Cracker.damaged_log"); } catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    internal static void Destroying(CondOwner co)
    {
        if (co == null || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || co.HasCond("IsModeSwitching", false) || !co.HasCond("IsInstalled")) return;
        try { Dump(co, "Cracker.destroyed_log"); } catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    private static void Dump(CondOwner co, string logKey)
    {
        var s = Get(co);
        if (s.Protected) return;
        double nh3 = s.State.AmmoniaKg, n2 = s.State.NitrogenKg, h2 = s.State.HydrogenKg;
        if (nh3 + n2 + h2 <= 1e-9) return;
        Stop(co, s, Text.Get("Cracker.repair_first"), needsAttention: false);
        s.State.AmmoniaKg = 0; s.State.NitrogenKg = 0; s.State.HydrogenKg = 0; s.State.CycleKWh = 0;
        Save(co, s);
        var air = RoomHeat.Read(co);
        if (air != null)
        {
            if (nh3 > 0) RoomGas.Emit(air, CrackerRules.AmmoniaSpecies, nh3);
            if (n2 > 0) RoomGas.Emit(air, CrackerRules.NitrogenSpecies, n2);
        }
        bool burned = h2 > 0 && StoreService.Ignite(co, GasStores.Hydrogen, h2, "Sabatier.burn_log");
        string log = Text.Get(logKey, co.strNameFriendly, nh3, n2, h2, burned ? Text.Get("Sabatier.hydrogen_burned") : Text.Get("Sabatier.hydrogen_escaped"));
        Plugin.Log(log);
        PlayerNotices.Post(co.ship, "PhobosManufacturing.cracker", NoticeLevel.Caution, log, Text.Get("Cracker.dump_banner"));
    }

    internal static EquipmentState State(CondOwner co)
    {
        if (co.HasCond("IsDamaged") || co.HasCond("IsLocked")) return EquipmentState.Blocked;
        var s = Get(co);
        if (s.NeedsAttention || s.Protected) return EquipmentState.Blocked;
        if (co.HasCond(ManufacturingRules.Reacting)) return co.HasCond("IsPowered") ? EquipmentState.Running : EquipmentState.Waiting;
        return s.Running ? EquipmentState.Waiting : EquipmentState.Paused;
    }
    internal static string Describe(CondOwner co)
    {
        var s = Get(co); var st = s.State;
        return Text.Get("Cracker.status", s.Status, st.AmmoniaKg, st.NitrogenKg, st.HydrogenKg, ManufacturingRules.PercentDone(st.CycleKWh, CrackerRules.CycleKWh), CrackerRules.CycleKWh, st.Cycles,
                st.ProducedNitrogenKg, st.ProducedHydrogenKg, st.ConsumedAmmoniaKg,
                co.HasCond("IsPowered") ? Text.Get("Content.powered") : Text.Get("Content.no_power"),
                ObjectPresentation.Name(AmmoniaPeer(co)), ObjectPresentation.Name(NitrogenPeer(co)), ObjectPresentation.Name(HydrogenPeer(co))) +
            "\n" + Text.Get("Cracker.demand", CrackerRules.WorkingKW, RoomHeat.Machine(CrackerRules.RoomHeatKW(true))) + (s.LastStop == null ? "" : "\n" + Text.Get("Content.last_stop", s.LastStop));
    }
    internal static bool LinkTo(CondOwner co, string kind, string id, ConsoleBinding? binding, out string reason)
    {
        reason = Content.Access(co, binding) ?? "";
        if (reason.Length > 0) return false;
        var s = Get(co);
        if (co.HasCond(ManufacturingRules.Reacting) || s.Running) { reason = Text.Get("Cracker.link_busy"); return false; }
        if (!Links.Any(l => Kind(l) == kind)) { reason = Text.Get("Content.unsupported_action"); return false; }
        var link = Links.First(l => Kind(l) == kind);
        if (id == "none") { LinkOf(link).Unlink(co, CrewWork.Resolve); reason = Text.Get("Cracker.unlinked"); return true; }
        var target = Candidates(co, link).FirstOrDefault(v => v.strID == id);
        if (target == null) { reason = Text.Get("Cracker.link_missing"); return false; }
        if (!LinkOf(link).Link(co, target, CrewWork.Resolve, out reason)) return false;
        reason = Text.Get("Cracker.linked"); return true;
    }
    internal static string? MaintenanceReason(CondOwner co, bool dismantle)
    {
        if (!CrackerRules.IsFamily(co.strCODef)) return null;
        var s = Get(co);
        string? key = RemovalRules.Reason(s.Protected, s.Running, s.State.HeldKg, dismantle, "Maintenance.cracker");
        return key == null ? null : Text.Get(key);
    }
    /// <summary>After the game copies this machine's record onto a new form (uninstall to loose, or install from the
    /// loose item; Manufacturing 0.56.1): the record moves by itself, the mass does not, so the new form is set to housing
    /// plus hold. A record that cannot be read is left for the owner's Accept.</summary>
    internal static void Carried(CondOwner co)
    {
        if (co == null || co.bDestroyed) return;
        sessions.Remove(co);
        var status = Store(co).Read(out var fields);
        if (status != SavedStateStatus.Ready) return;
        try { var state = CrackerState.Read(fields); co.AddMass(CrackerRules.MachineKg + state.HeldKg - co.GetCondAmount("StatMass"), true); }
        catch (Exception e) { Plugin.Log(e.ToString()); }
    }

    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Text.Get("Cracker.fault");
        if (!Content.Ready) { message = Content.Status; return false; }
        foreach (var link in Links)
            if (action.StartsWith(Kind(link) + ":", StringComparison.Ordinal)) return LinkTo(co, Kind(link), action.Substring(Kind(link).Length + 1), binding, out message);
        bool result;
        switch (action)
        {
            case "start": result = Start(co, binding); break;
            case "pause": result = Pause(co, false, binding); break;
            case "cancel": result = Pause(co, true, binding); break;
            case "accept": result = Content.Access(co, binding) == null && Accept(co); message = Text.Get(result ? "Cracker.accept_done" : "Cracker.accept_unavailable"); return result;
            case "status": result = true; break;
            default: message = Text.Get("Content.unsupported_action"); return false;
        }
        message = Describe(co);
        return result;
    }
}
