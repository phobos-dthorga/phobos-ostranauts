using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The X2 electrolysis cell: Start arms it; while powered, linked to a water vessel and a hydrogen store,
/// it draws one cycle's water into its hold, credits measured electricity to the cycle, and at 6 kWh delivers
/// oxygen into the linked native canister (or the cabin air when none is linked) and hydrogen into the store.
/// The hold and the cycle's energy are the machine's own saved record; running permission is not, so a reload
/// waits for Resume. Nothing splits until every output has somewhere to go.</summary>
internal static class ProcessorService
{
    private sealed class Session
    {
        internal ProcessorState State = new();
        internal bool Protected, Running, HeatWait, OutputWait, NeedsAttention;
        internal double Last, NextCheck, PendingH2, PendingO2;
        internal string Status = Text.Get("Processor.paused");
        internal string? LastStop;
    }
    internal sealed class Transfer { internal RoomHeat.Air Air = null!; internal double RequestedKWh, HeatFraction; internal EnergyReceipt Receipt = null!; }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    private static readonly ConditionalWeakTable<Powered, Transfer> pending = new();
    internal static void Reset() => sessions = new();

    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, ProcessorRules.Record, Plugin.Id, 1);
    internal static LiquidTransferGuard Guard(CondOwner co) => new(co.mapGUIPropMaps, ProcessorRules.Guard, Plugin.Id);
    private static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co, out var found)) return found;
        var s = new Session();
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Ready) { try { s.State = ProcessorState.Read(fields); } catch (Exception e) { s.Protected = true; Plugin.Log(e.ToString()); } }
        else if (status != SavedStateStatus.Missing) s.Protected = true;
        if (!s.Protected && (Guard(co).Protected || !BulkVessel.MassMatches(co.GetCondAmount("StatMass"), ProcessorRules.MachineKg + s.State.HoldKg))) s.Protected = true;
        if (s.Protected) s.Status = Text.Get("Processor.protected");
        sessions.Add(co, s);
        return s;
    }
    private static void Save(CondOwner co, Session s)
    {
        if (!Store(co).TryWrite(s.State.Save())) { s.Protected = true; s.Status = Text.Get("Processor.protected"); return; }
        co.AddMass(ProcessorRules.MachineKg + s.State.HoldKg - co.GetCondAmount("StatMass"), true);
    }
    internal static bool Protected(CondOwner co) => Get(co).Protected;
    /// <summary>Owner-confirmed recovery: a readable record is trusted and the mass set back to housing plus hold.</summary>
    internal static bool Accept(CondOwner co)
    {
        var s = Get(co);
        var status = Store(co).Read(out var fields);
        ProcessorState? state = null;
        try { state = status == SavedStateStatus.Ready ? ProcessorState.Read(fields) : status == SavedStateStatus.Missing ? new ProcessorState() : null; } catch (Exception e) { Plugin.Log(e.ToString()); }
        if (state == null || !Guard(co).Resolve()) return false;
        s.State = state; s.Protected = false; Save(co, s);
        if (!s.Protected) s.Status = Text.Get("Processor.paused");
        return !s.Protected;
    }

    /// <summary>Touching (footprints within one tile): the rule for the game's own canisters, which have no line port.</summary>
    internal static bool Adjacent(CondOwner a, CondOwner b) => BulkVessels.Adjacent(a, b);
    /// <summary>The shared links (Manufacturing 0.23.0): a water vessel on the process-water line or touching, a
    /// hydrogen store on the gas line or touching; each vessel-side port is a bank several machines share.</summary>
    internal static readonly VesselLink WaterLink = new(ProcessorRules.WaterInPort, ProcessorRules.VesselOutPort, ManufacturingRules.Water);
    internal static readonly VesselLink HydrogenLink = new(ProcessorRules.HydrogenOutPort, ProcessorRules.StoreInPort, ManufacturingRules.Hydrogen);
    internal static string WaterPeer(CondOwner co) => WaterLink.PeerId(co);
    internal static string StorePeer(CondOwner co) => HydrogenLink.PeerId(co);
    internal static IEnumerable<CondOwner> WaterCandidates(CondOwner co) => WaterLink.Candidates(co);
    internal static IEnumerable<CondOwner> StoreCandidates(CondOwner co) => HydrogenLink.Candidates(co);
    /// <summary>A bulk gas store of the commodity on the gas line or touching (no pairing: the oxygen and CO2
    /// destinations are one-sided saved ids).</summary>
    internal static bool StoreReach(CondOwner co, CondOwner store) => LineReach.Of(co, store, LineFamilies.Gas) != LineReachKind.None;
    /// <summary>Where the oxygen can go: installed native oxygen canisters touching the cell (the game's own trigger)
    /// and bulk oxygen stores of any size touching it or on its gas line.</summary>
    internal static IEnumerable<CondOwner> CanisterCandidates(CondOwner co)
    {
        var trigger = NativeDefinitions.Trigger(ProcessorRules.CanisterTrigger);
        if (co.ship == null) return Enumerable.Empty<CondOwner>();
        var canisters = trigger == null ? Enumerable.Empty<CondOwner>() :
            co.ship.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c.ship == co.ship && c != co && trigger.Triggered(c) && Adjacent(co, c));
        return canisters.Concat(BulkVessels.Aboard(co.ship, ManufacturingRules.Oxygen).Where(v => v != co && StoreReach(co, v))).Distinct().OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    }
    // These run every power step while the cell works: reasons are formatted only on the way out, and a vessel is
    // read once (the snapshot carries its protection).
    private static CondOwner? Vessel(CondOwner co, out string reason)
    {
        var vessel = CrewWork.Resolve(WaterPeer(co));
        if (vessel == null || !BulkVessels.IsVessel(vessel)) { reason = Text.Get("Processor.no_vessel"); return null; }
        if (!WaterLink.Connected(co, vessel)) { reason = Text.Get("Processor.vessel_not_ready"); return null; }
        var s = BulkVessel.Snapshot(vessel);
        if (s.Protected || CommodityReservations.Held(vessel.strID)) { reason = Text.Get("Processor.vessel_protected"); return null; }
        if (s.CatchKg > 1e-8) { reason = Text.Get("Processor.vessel_catch"); return null; }
        if (s.AvailableKg + 1e-8 < ProcessorRules.WaterKgPerCycle) { reason = Text.Get("Processor.vessel_empty", s.AvailableKg, ProcessorRules.WaterKgPerCycle); return null; }
        reason = ""; return vessel;
    }
    private static CondOwner? HydrogenStore(CondOwner co, out string reason)
    {
        var store = CrewWork.Resolve(StorePeer(co));
        if (store == null || !HydrogenRules.AnySize(store.strCODef)) { reason = Text.Get("Processor.no_store"); return null; }
        if (!HydrogenLink.Connected(co, store)) { reason = Text.Get("Processor.store_not_ready"); return null; }
        var s = BulkVessel.Snapshot(store);
        if (s.Protected || CommodityReservations.Held(store.strID)) { reason = Text.Get("Processor.store_protected"); return null; }
        if (s.HeadroomKg + 1e-8 < ProcessorRules.HydrogenKgPerCycle) { reason = Text.Get("Processor.store_full", s.HeadroomKg); return null; }
        reason = ""; return store;
    }
    /// <summary>Whether one object is among the oxygen destinations <see cref="CanisterCandidates"/> would list, without
    /// listing them: the same rules applied to that object alone.</summary>
    private static bool IsCanisterCandidate(CondOwner co, CondOwner c)
    {
        if (c == null || c.bDestroyed || c == co || c.ship != co.ship) return false;
        if (BulkVessels.Of(c)?.Commodity == ManufacturingRules.Oxygen) return c.HasCond("IsInstalled") && NativeFluidRoute.EndpointReady(c) && StoreReach(co, c);
        var trigger = NativeDefinitions.Trigger(ProcessorRules.CanisterTrigger);
        return trigger != null && Adjacent(co, c) && trigger.Triggered(c);
    }
    /// <summary>The linked canister when it can take one cycle's oxygen; null with an empty reason means cabin air.</summary>
    private static CondOwner? Canister(CondOwner co, Session s, out string reason)
    {
        reason = "";
        if (s.State.Canister.Length == 0) return null;
        var canister = CrewWork.Resolve(s.State.Canister);
        if (canister == null || !IsCanisterCandidate(co, canister)) { reason = Text.Get("Processor.canister_missing"); return null; }
        if (canister.HasCond("IsDamaged")) { reason = Text.Get("Processor.canister_damaged"); return null; }
        if (BulkVessels.IsVessel(canister))
        {
            // A bulk oxygen store: ready, unreserved and with room for one cycle.
            var snapshot = BulkVessel.Snapshot(canister);
            if (!NativeFluidRoute.EndpointReady(canister) || snapshot.Protected || CommodityReservations.Held(canister.strID)) { reason = Text.Get("Processor.store_protected"); return null; }
            if (snapshot.CatchKg > 1e-8 || snapshot.HeadroomKg + 1e-8 < ProcessorRules.OxygenKgPerCycle) { reason = Text.Get("Processor.oxygen_store_full", snapshot.HeadroomKg); return null; }
            return canister;
        }
        if (!NativeGasCanister.TryRead(canister, out var reading) || reading.Species != ProcessorRules.OxygenSpecies || reading.HeadroomMoles + 1e-9 < ProcessorRules.OxygenMolesPerCycle)
        { reason = Text.Get("Processor.canister_full"); return null; }
        return canister;
    }
    internal static string? MachineProblem(CondOwner co)
    {
        if (!Content.Ready) return Content.Status;
        if (co == null || co.bDestroyed || co.strCODef != ProcessorRules.Installed || !co.HasCond("IsInstalled")) return Text.Get("Processor.install_first");
        if (co.HasCond("IsDamaged")) return Text.Get("Processor.repair_first");
        if (co.ship == null || (int)co.ship.LoadState < 2) return Text.Get("Content.ship_not_loaded");
        if (co.HasCond("IsLocked")) return Text.Get("Processor.unlock");
        if (co.HasCond("IsOverrideOff") || co.HasCond("IsSignalOff")) return Text.Get("Processor.switched_off");
        return null;
    }
    // Written only when it changes: an idle machine reaches this on every power step.
    private static void SetWorking(CondOwner co, bool value) { if (co.HasCond(ManufacturingRules.Electrolysing) != value) co.SetCondAmount(ManufacturingRules.Electrolysing, value ? 1 : 0); }

    internal static bool Start(CondOwner co, ConsoleBinding? binding = null)
    {
        var s = Get(co);
        string? problem = Content.Access(co, binding) ?? MachineProblem(co);
        if (problem != null) { s.Status = problem; return false; }
        if (s.Protected) { s.Status = Text.Get("Processor.protected"); return false; }
        s.LastStop = null; s.NeedsAttention = false; s.Running = true; s.Last = StarSystem.fEpoch;
        s.Status = Text.Get("Processor.armed");
        return true;
    }
    internal static bool Pause(CondOwner co, bool cancel, ConsoleBinding? binding = null)
    {
        var s = Get(co);
        string? problem = Content.Access(co, binding);
        if (problem != null) { s.Status = problem; return false; }
        s.NeedsAttention = false;
        CrewWork.ManualStop(co);
        Stop(co, s, Text.Get(cancel ? "Processor.cancelled" : "Processor.paused_retained"), needsAttention: false);
        // Cancelling forfeits the cycle's energy; the held water stays held (it is real mass) for the next cycle.
        if (cancel && !s.Protected) { s.State.CycleKWh = 0; Save(co, s); }
        return !s.Protected;
    }
    private static void Stop(CondOwner co, Session s, string message, bool needsAttention = true)
    {
        s.LastStop = message; s.NeedsAttention = needsAttention; s.Running = false; s.HeatWait = false; s.OutputWait = false;
        s.Status = message; SetWorking(co, false);
    }
    internal static void Fault(CondOwner co, Exception ex)
    {
        var s = Get(co);
        Stop(co, s, Text.Get("Processor.fault")); s.NeedsAttention = true;
        Plugin.Log(ex.ToString());
    }

    /// <summary>Before the native power step: an armed cell works only while every input and output is ready.</summary>
    internal static void BeforePower(CondOwner co)
    {
        if (!sessions.TryGetValue(co, out var s) || !s.Running) { SetWorking(co, false); return; }
        var problem = MachineProblem(co);
        if (problem != null) { Stop(co, s, problem); return; }
        if (s.Protected) { Stop(co, s, Text.Get("Processor.protected")); return; }
        if (s.OutputWait && Cadence.RealTime < s.NextCheck) { SetWorking(co, false); return; }
        s.OutputWait = false;
        try
        {
            // Water for the cycle comes in when the hold is empty; the cycle's energy is credited only afterwards.
            if (s.State.HoldKg + 1e-9 < ProcessorRules.WaterKgPerCycle)
            {
                var vessel = Vessel(co, out string why);
                if (vessel == null) { Wait(co, s, Text.Get("Processor.waiting_water", why)); return; }
                double need = ProcessorRules.WaterKgPerCycle - s.State.HoldKg;
                LiquidTransferGuard.Commit(new BulkVessel.Endpoint(vessel), new WaterHold(co, s), need, BulkVessel.Guard(vessel), Guard(co));
                if (s.Protected) { Stop(co, s, Text.Get("Processor.protected")); return; }
            }
            if (HydrogenStore(co, out string storeWhy) == null) { Wait(co, s, Text.Get("Processor.waiting_store", storeWhy)); return; }
            if (Canister(co, s, out string canisterWhy) == null && canisterWhy.Length > 0) { Wait(co, s, Text.Get("Processor.waiting_canister", canisterWhy)); return; }
        }
        catch (Exception ex) { Fault(co, ex); return; }
        SetWorking(co, true);
    }
    private static void Wait(CondOwner co, Session s, string status)
    {
        // Rechecks follow real time: a game-time interval would shrink to every frame at fast-forward.
        s.OutputWait = true; s.NextCheck = Cadence.RealTime + ManufacturingRules.VesselRecheckSeconds; s.Status = status; SetWorking(co, false);
    }
    internal static bool BeginPower(Powered power, CondOwner co, ref double amount, out Transfer? transfer)
    {
        transfer = null;
        pending.Remove(power);
        bool working = co.HasCond(ManufacturingRules.Electrolysing);
        double demand = working ? ProcessorRules.WorkingKW : ProcessorRules.IdleKW;
        double seconds = amount * Units.SecondsPerHour / demand, heatKW = ProcessorRules.RoomHeatKW(working);
        var air = RoomHeat.Read(co);
        if (!RoomHeat.Admit(air, heatKW, seconds, out _))
        {
            if (sessions.TryGetValue(co, out var s) && s.Running) { s.HeatWait = true; s.Status = Text.Get("Processor.heat_wait"); }
            co.ZeroCondAmount("IsPowered");
            return false;
        }
        if (sessions.TryGetValue(co, out var ready)) ready.HeatWait = false;
        transfer = new Transfer { Air = air!, RequestedKWh = amount, HeatFraction = heatKW / demand, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
        pending.Add(power, transfer);
        return true;
    }
    internal static void FinishPower(Powered power, CondOwner co, Transfer? transfer)
    {
        pending.Remove(power);
        if (transfer == null) return;
        double supplied = NativeEnergyReceipts.Complete(power, co, transfer.Receipt);
        if (!ManufacturingRules.Finite(supplied)) throw new InvalidOperationException("Invalid electrolysis energy receipt.");
        RoomHeat.Deposit(transfer.Air, supplied, transfer.HeatFraction);
        if (!sessions.TryGetValue(co, out var s) || !s.Running || !co.HasCond(ManufacturingRules.Electrolysing) || s.Protected) return;
        s.Last = StarSystem.fEpoch;
        var (credited, complete) = ProcessorRules.Advance(s.State.CycleKWh, supplied);
        s.State.CycleKWh = credited;
        s.Status = co.HasCond("IsPowered") ? Text.Get("Processor.working", s.State.CycleKWh, ProcessorRules.CycleKWh) : Text.Get("Processor.waiting_power");
        if (complete) Settle(co, s, transfer.Air);
        else Save(co, s);
    }
    internal static void Forget(Powered power) { pending.Remove(power); NativeEnergyReceipts.Forget(power); }
    /// <summary>A completed cycle: hydrogen into the store under both guards, oxygen into the canister or the
    /// cabin, the hold emptied. Every destination is checked before the first mutation.</summary>
    private static void Settle(CondOwner co, Session s, RoomHeat.Air air)
    {
        var store = HydrogenStore(co, out string storeWhy);
        if (store == null) { Save(co, s); Wait(co, s, Text.Get("Processor.waiting_store", storeWhy)); return; }
        var canister = Canister(co, s, out string canisterWhy);
        if (canister == null && canisterWhy.Length > 0) { Save(co, s); Wait(co, s, Text.Get("Processor.waiting_canister", canisterWhy)); return; }
        if (s.State.HoldKg + 1e-9 < ProcessorRules.WaterKgPerCycle) throw new InvalidOperationException("Electrolysis completed without its water.");
        s.PendingH2 = ProcessorRules.HydrogenKgPerCycle;
        LiquidTransferGuard.Commit(new HydrogenHold(co, s), new BulkVessel.Endpoint(store), ProcessorRules.HydrogenKgPerCycle, Guard(co), BulkVessel.Guard(store));
        if (s.PendingH2 > 1e-9) throw new InvalidOperationException("Hydrogen was not delivered.");
        double moles = ProcessorRules.OxygenMolesPerCycle;
        if (canister != null && BulkVessels.IsVessel(canister))
        {
            s.PendingO2 = ProcessorRules.OxygenKgPerCycle;
            LiquidTransferGuard.Commit(new OxygenHold(co, s), new BulkVessel.Endpoint(canister), ProcessorRules.OxygenKgPerCycle, Guard(co), BulkVessel.Guard(canister));
            if (s.PendingO2 > 1e-9) throw new InvalidOperationException("Oxygen was not delivered.");
        }
        else if (canister != null)
        {
            double added = NativeGasCanister.TryAdd(canister, ProcessorRules.OxygenSpecies, moles);
            if (added + 1e-9 < moles) throw new InvalidOperationException("Oxygen canister refused a checked delivery.");
        }
        else { RoomGas.Emit(air, ProcessorRules.OxygenSpecies, ProcessorRules.OxygenKgPerCycle); s.State.CabinO2Kg += ProcessorRules.OxygenKgPerCycle; }
        s.State.HoldKg = Math.Max(0, s.State.HoldKg - ProcessorRules.WaterKgPerCycle);
        s.State.CycleKWh = 0; s.State.ProducedO2Kg += ProcessorRules.OxygenKgPerCycle; s.State.ProducedH2Kg += ProcessorRules.HydrogenKgPerCycle; s.State.Cycles++;
        Save(co, s);
        Plugin.Log(Text.Get("Processor.cycle_log", co.strID, ProcessorRules.OxygenKgPerCycle, canister == null ? Text.Get("Processor.cabin") : canister.strID, ProcessorRules.HydrogenKgPerCycle, store.strID));
    }
    /// <summary>The cell's own one-cycle water hold as a reservoir for guarded transfers.</summary>
    private sealed class WaterHold : ILiquidReservoir
    {
        private readonly CondOwner co; private readonly Session s;
        internal WaterHold(CondOwner co, Session s) { this.co = co; this.s = s; }
        public string Identity => co.strID;
        public string ShipId => co.ship.strRegID;
        public string Commodity => ManufacturingRules.Water;
        public double QuantityKg => s.State.HoldKg;
        public double CapacityKg => ProcessorRules.WaterKgPerCycle;
        public void SetQuantity(double kg) { s.State.HoldKg = kg; Save(co, s); }
    }
    /// <summary>The oxygen a completed cycle has just made, on its way into a bulk oxygen store.</summary>
    private sealed class OxygenHold : ILiquidReservoir
    {
        private readonly CondOwner co; private readonly Session s;
        internal OxygenHold(CondOwner co, Session s) { this.co = co; this.s = s; }
        public string Identity => co.strID + ".oxygen";
        public string ShipId => co.ship.strRegID;
        public string Commodity => ManufacturingRules.Oxygen;
        public double QuantityKg => s.PendingO2;
        public double CapacityKg => ProcessorRules.OxygenKgPerCycle;
        public void SetQuantity(double kg) => s.PendingO2 = kg;
    }
    /// <summary>The hydrogen a completed cycle has just made, on its way into the store.</summary>
    private sealed class HydrogenHold : ILiquidReservoir
    {
        private readonly CondOwner co; private readonly Session s;
        internal HydrogenHold(CondOwner co, Session s) { this.co = co; this.s = s; }
        public string Identity => co.strID + ".hydrogen";
        public string ShipId => co.ship.strRegID;
        public string Commodity => ManufacturingRules.Hydrogen;
        public double QuantityKg => s.PendingH2;
        public double CapacityKg => ProcessorRules.HydrogenKgPerCycle;
        public void SetQuantity(double kg) => s.PendingH2 = kg;
    }

    internal static EquipmentState State(CondOwner co)
    {
        if (co.HasCond("IsDamaged") || co.HasCond("IsLocked")) return EquipmentState.Blocked;
        var s = Get(co);
        if (s.NeedsAttention || s.Protected) return EquipmentState.Blocked;
        if (co.HasCond(ManufacturingRules.Electrolysing)) return co.HasCond("IsPowered") ? EquipmentState.Running : EquipmentState.Waiting;
        return s.Running ? EquipmentState.Waiting : EquipmentState.Paused;
    }
    internal static string Describe(CondOwner co)
    {
        var s = Get(co);
        return Text.Get("Processor.status", s.Status, s.State.HoldKg, s.State.CycleKWh, ProcessorRules.CycleKWh, s.State.Cycles, s.State.ProducedO2Kg, s.State.ProducedH2Kg, s.State.CabinO2Kg,
            co.HasCond("IsPowered") ? Text.Get("Content.powered") : Text.Get("Content.no_power"), ObjectPresentation.Name(WaterPeer(co)), ObjectPresentation.Name(StorePeer(co)),
            s.State.Canister.Length == 0 ? Text.Get("Processor.cabin") : ObjectPresentation.Name(s.State.Canister)) +
            "\n" + Text.Get("Processor.demand", ProcessorRules.WorkingKW, ProcessorRules.WorkingKW * ProcessorRules.RoomHeatFraction) + (s.LastStop == null ? "" : "\n" + Text.Get("Content.last_stop", s.LastStop));
    }
    internal static bool Link(CondOwner co, string kind, string id, ConsoleBinding? binding, out string reason)
    {
        reason = Content.Access(co, binding) ?? "";
        if (reason.Length > 0) return false;
        var s = Get(co);
        if (co.HasCond(ManufacturingRules.Electrolysing) || s.Running) { reason = Text.Get("Processor.link_busy"); return false; }
        if (kind == "canister")
        {
            if (id != "none" && (!CanisterCandidates(co).Any(c => c.strID == id) || !ProcessorState.SafeId(id))) { reason = Text.Get("Processor.link_missing"); return false; }
            s.State.Canister = id == "none" ? "" : id; Save(co, s);
            reason = Text.Get(id == "none" ? "Processor.cabin_selected" : "Processor.linked"); return !s.Protected;
        }
        var link = kind == "water" ? WaterLink : HydrogenLink;
        if (id == "none") { link.Unlink(co, CrewWork.Resolve); reason = Text.Get("Processor.unlinked"); return true; }
        var target = link.Candidates(co).FirstOrDefault(v => v.strID == id);
        if (target == null) { reason = Text.Get("Processor.link_missing"); return false; }
        if (!link.Link(co, target, CrewWork.Resolve, out reason)) return false;
        reason = Text.Get("Processor.linked"); return true;
    }
    internal static string CanisterId(CondOwner co) => Get(co).State.Canister;
    internal static string CanisterName(CondOwner co) { var s = Get(co); return s.State.Canister.Length == 0 ? Text.Get("Processor.cabin") : ObjectPresentation.Name(s.State.Canister); }
    internal static string? MaintenanceReason(CondOwner co)
    {
        if (!ProcessorRules.IsFamily(co.strCODef)) return null;
        var s = Get(co);
        if (s.Protected) return Text.Get("Maintenance.protected");
        return s.State.HoldKg > 1e-8 || s.State.CycleKWh > 1e-8 ? Text.Get("Maintenance.cycle") : null;
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Text.Get("Processor.fault");
        if (!Content.Ready) { message = Content.Status; return false; }
        foreach (string kind in new[] { "water", "store", "canister" })
            if (action.StartsWith(kind + ":", StringComparison.Ordinal)) return Link(co, kind, action.Substring(kind.Length + 1), binding, out message);
        bool result;
        switch (action)
        {
            case "start": result = Start(co, binding); break;
            case "pause": result = Pause(co, false, binding); break;
            case "cancel": result = Pause(co, true, binding); break;
            case "accept": result = Content.Access(co, binding) == null && Accept(co); message = Text.Get(result ? "Processor.accept_done" : "Processor.accept_unavailable"); return result;
            case "status": result = true; break;
            default: message = Text.Get("Content.unsupported_action"); return false;
        }
        message = Describe(co);
        return result;
    }
}
