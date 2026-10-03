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
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The K2 Sabatier reactor: Start arms it; while powered and linked, it draws one cycle's hydrogen from a
/// store and CO2 from an installed native CO2 canister into its reactant hold, credits measured electricity to
/// the cycle, and on completion turns the reactant hold into a product hold of water and methane in one saved
/// step. Products then go to the linked water vessel and methane store under both transfer guards. A new cycle
/// begins only once the products are delivered. Running permission is not saved: a reload waits for Start.</summary>
internal static class SabatierService
{
    private sealed class Session
    {
        internal SabatierState State = new();
        internal bool Protected, Running, HeatWait, OutputWait, NeedsAttention;
        internal double NextCheck;
        internal string Status = Text.Get("Sabatier.paused");
        internal string? LastStop;
    }
    internal sealed class Transfer { internal RoomHeat.Air Air = null!; internal bool Working; internal EnergyReceipt Receipt = null!; }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    private static readonly ConditionalWeakTable<Powered, Transfer> pending = new();
    internal static void Reset() => sessions = new();

    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, SabatierRules.Record, Plugin.Id, 1);
    internal static LiquidTransferGuard Guard(CondOwner co) => new(co.mapGUIPropMaps, SabatierRules.Guard, Plugin.Id);
    private static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co, out var found)) return found;
        var s = new Session();
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Ready) { try { s.State = SabatierState.Read(fields); } catch (Exception e) { s.Protected = true; Plugin.Log(e.ToString()); } }
        else if (status != SavedStateStatus.Missing) s.Protected = true;
        if (!s.Protected && (Guard(co).Protected || !BulkVessel.MassMatches(co.GetCondAmount("StatMass"), SabatierRules.MachineKg + s.State.HeldKg))) s.Protected = true;
        if (s.Protected) s.Status = Text.Get("Sabatier.protected");
        sessions.Add(co, s);
        return s;
    }
    private static void Save(CondOwner co, Session s)
    {
        if (!Store(co).TryWrite(s.State.Save())) { s.Protected = true; s.Status = Text.Get("Sabatier.protected"); return; }
        co.AddMass(SabatierRules.MachineKg + s.State.HeldKg - co.GetCondAmount("StatMass"), true);
    }
    internal static bool Protected(CondOwner co) => Get(co).Protected;
    internal static bool Accept(CondOwner co)
    {
        var s = Get(co);
        var status = Store(co).Read(out var fields);
        SabatierState? state = null;
        try { state = status == SavedStateStatus.Ready ? SabatierState.Read(fields) : status == SavedStateStatus.Missing ? new SabatierState() : null; } catch (Exception e) { Plugin.Log(e.ToString()); }
        if (state == null || !Guard(co).Resolve()) return false;
        s.State = state; s.Protected = false; Save(co, s);
        if (!s.Protected) s.Status = Text.Get("Sabatier.paused");
        return !s.Protected;
    }

    /// <summary>The shared links (Manufacturing 0.23.0): each vessel touches the reactor or shares the commodity's line
    /// with it, and each vessel-side port is a bank several machines share.</summary>
    internal static readonly VesselLink WaterLink = new(SabatierRules.WaterOutPort, SabatierRules.VesselInPort, ManufacturingRules.Water);
    internal static readonly VesselLink HydrogenLink = new(SabatierRules.HydrogenInPort, SabatierRules.StoreOutPort, ManufacturingRules.Hydrogen);
    internal static readonly VesselLink MethaneLink = new(SabatierRules.MethaneOutPort, SabatierRules.MethaneStoreInPort, ManufacturingRules.Methane);
    internal static string WaterPeer(CondOwner co) => WaterLink.PeerId(co);
    internal static string HydrogenPeer(CondOwner co) => HydrogenLink.PeerId(co);
    internal static string MethanePeer(CondOwner co) => MethaneLink.PeerId(co);
    internal static IEnumerable<CondOwner> WaterCandidates(CondOwner co) => WaterLink.Candidates(co);
    internal static IEnumerable<CondOwner> HydrogenCandidates(CondOwner co) => HydrogenLink.Candidates(co);
    internal static IEnumerable<CondOwner> MethaneCandidates(CondOwner co) => MethaneLink.Candidates(co);
    /// <summary>Where the CO2 can come from: installed native CO2 canisters touching the reactor (the game's own
    /// trigger) and bulk carbon dioxide stores of any size touching it or on its gas line.</summary>
    internal static IEnumerable<CondOwner> CanisterCandidates(CondOwner co)
    {
        var trigger = NativeDefinitions.Trigger(SabatierRules.CanisterTrigger);
        if (co.ship == null) return Enumerable.Empty<CondOwner>();
        var canisters = trigger == null ? Enumerable.Empty<CondOwner>() :
            co.ship.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c.ship == co.ship && c != co && trigger.Triggered(c) && ProcessorService.Adjacent(co, c));
        var stores = BulkVessels.Aboard(co.ship, ManufacturingRules.CarbonDioxide).Where(v => v != co && ProcessorService.StoreReach(co, v));
        return canisters.Concat(stores).Distinct().OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
    }

    /// <summary>A linked vessel ready for this transfer, or null with the reason.</summary>
    // Runs every power step while the reactor works: reasons are formatted only on the way out, the vessel read once.
    private static CondOwner? Linked(CondOwner co, VesselLink link, string keyPrefix, out string reason) => Linked(co, link, keyPrefix, out reason, out _);
    // The snapshot taken for the readiness test is handed back, so a caller that also needs the quantities reads the
    // vessel once (Manufacturing 0.31.0).
    private static CondOwner? Linked(CondOwner co, VesselLink link, string keyPrefix, out string reason, out BulkVesselSnapshot? snapshot)
    {
        snapshot = null;
        var vessel = CrewWork.Resolve(link.PeerId(co));
        if (vessel == null || BulkVessels.SpecFor(vessel.strCODef)?.Commodity != link.Commodity) { reason = Text.Get(keyPrefix + "_none"); return null; }
        if (!link.Connected(co, vessel)) { reason = Text.Get(keyPrefix + "_not_ready"); return null; }
        snapshot = BulkVessel.Snapshot(vessel);
        if (snapshot.Protected || CommodityReservations.Held(vessel.strID)) { reason = Text.Get("Sabatier.vessel_protected"); return null; }
        if (snapshot.CatchKg > 1e-8) { reason = Text.Get("Sabatier.vessel_catch"); return null; }
        reason = ""; return vessel;
    }
    /// <summary>Whether one object is among the CO2 sources <see cref="CanisterCandidates"/> would list, without listing them.</summary>
    private static bool IsCanisterCandidate(CondOwner co, CondOwner c)
    {
        if (c == null || c.bDestroyed || c == co || c.ship != co.ship) return false;
        if (BulkVessels.Of(c)?.Commodity == ManufacturingRules.CarbonDioxide) return c.HasCond("IsInstalled") && NativeFluidRoute.EndpointReady(c) && ProcessorService.StoreReach(co, c);
        var trigger = NativeDefinitions.Trigger(SabatierRules.CanisterTrigger);
        return trigger != null && ProcessorService.Adjacent(co, c) && trigger.Triggered(c);
    }
    private static CondOwner? HydrogenSource(CondOwner co, double needKg, out string reason)
    {
        var store = Linked(co, HydrogenLink, "Sabatier.hydrogen", out reason, out var s);
        if (store == null || s == null) return null;
        if (s.AvailableKg + 1e-9 < needKg) { reason = Text.Get("Sabatier.hydrogen_short", s.AvailableKg, needKg); return null; }
        return store;
    }
    private static CondOwner? Destination(CondOwner co, bool water, double kg, out string reason)
    {
        BulkVesselSnapshot? s;
        var vessel = water ? Linked(co, WaterLink, "Sabatier.water", out reason, out s) : Linked(co, MethaneLink, "Sabatier.methane", out reason, out s);
        if (vessel == null || s == null) return null;
        if (s.HeadroomKg + 1e-9 < kg) { reason = Text.Get(water ? "Sabatier.water_full" : "Sabatier.methane_full", s.HeadroomKg, kg); return null; }
        return vessel;
    }
    private static CondOwner? Canister(CondOwner co, Session s, double needMoles, out string reason)
    {
        reason = Text.Get("Sabatier.canister_none");
        if (s.State.Canister.Length == 0) return null;
        var canister = CrewWork.Resolve(s.State.Canister);
        reason = Text.Get("Sabatier.canister_missing");
        if (canister == null || !IsCanisterCandidate(co, canister)) return null;
        reason = Text.Get("Sabatier.canister_damaged");
        if (canister.HasCond("IsDamaged")) return null;
        if (BulkVessels.IsVessel(canister))
        {
            reason = Text.Get("Sabatier.vessel_protected");
            if (!NativeFluidRoute.EndpointReady(canister) || BulkVessel.Protected(canister) || CommodityReservations.Held(canister.strID)) return null;
            var snapshot = BulkVessel.Snapshot(canister);
            double needKg = NativeGasCanister.Kilograms(SabatierRules.CarbonDioxide, needMoles);
            reason = Text.Get("Sabatier.canister_short", snapshot.AvailableKg, needKg);
            if (snapshot.CatchKg > 1e-8 || snapshot.AvailableKg + 1e-9 < needKg) return null;
            reason = ""; return canister;
        }
        if (!NativeGasCanister.TryRead(canister, out var reading) || reading.Species != SabatierRules.CarbonDioxide) return null;
        reason = Text.Get("Sabatier.canister_short", NativeGasCanister.Kilograms(SabatierRules.CarbonDioxide, reading.Moles), NativeGasCanister.Kilograms(SabatierRules.CarbonDioxide, needMoles));
        if (reading.Moles + 1e-9 < needMoles) return null;
        reason = ""; return canister;
    }

    internal static string? MachineProblem(CondOwner co)
    {
        if (!Content.Ready) return Content.Status;
        if (co == null || co.bDestroyed || co.strCODef != SabatierRules.Installed || !co.HasCond("IsInstalled")) return Text.Get("Sabatier.install_first");
        if (co.HasCond("IsDamaged")) return Text.Get("Sabatier.repair_first");
        if (co.ship == null || (int)co.ship.LoadState < 2) return Text.Get("Content.ship_not_loaded");
        if (co.HasCond("IsLocked")) return Text.Get("Sabatier.unlock");
        if (co.HasCond("IsOverrideOff") || co.HasCond("IsSignalOff")) return Text.Get("Sabatier.switched_off");
        return null;
    }
    // Written only when it changes: an idle machine reaches this on every power step.
    private static void SetWorking(CondOwner co, bool value) { if (co.HasCond(ManufacturingRules.Reacting) != value) co.SetCondAmount(ManufacturingRules.Reacting, value ? 1 : 0); }

    internal static bool Start(CondOwner co, ConsoleBinding? binding = null)
    {
        var s = Get(co);
        string? problem = Content.Access(co, binding) ?? MachineProblem(co);
        if (problem != null) { s.Status = problem; return false; }
        if (s.Protected) { s.Status = Text.Get("Sabatier.protected"); return false; }
        s.LastStop = null; s.NeedsAttention = false; s.Running = true; s.Status = Text.Get("Sabatier.armed");
        return true;
    }
    internal static bool Pause(CondOwner co, bool cancel, ConsoleBinding? binding = null)
    {
        var s = Get(co);
        string? problem = Content.Access(co, binding);
        if (problem != null) { s.Status = problem; return false; }
        CrewWork.ManualStop(co);
        Stop(co, s, Text.Get(cancel ? "Sabatier.cancelled" : "Sabatier.paused_retained"), needsAttention: false);
        // Cancelling forfeits the cycle's energy; held gases and made products stay (they are real mass).
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
        Stop(co, s, Text.Get("Sabatier.fault")); s.NeedsAttention = true;
        Plugin.Log(ex.ToString());
    }
    private static void Wait(CondOwner co, Session s, string status)
    {
        // Rechecks follow real time: a game-time interval would shrink to every frame at fast-forward.
        s.OutputWait = true; s.NextCheck = Cadence.RealTime + ManufacturingRules.VesselRecheckSeconds; s.Status = status; SetWorking(co, false);
    }

    /// <summary>Before the native power step: deliver waiting products first, then charge the reactor, and work
    /// only when the products of this cycle will have somewhere to go.</summary>
    internal static void BeforePower(CondOwner co)
    {
        if (!sessions.TryGetValue(co, out var s) || !s.Running) { SetWorking(co, false); return; }
        var problem = MachineProblem(co);
        if (problem != null) { Stop(co, s, problem); return; }
        if (s.Protected) { Stop(co, s, Text.Get("Sabatier.protected")); return; }
        if (s.OutputWait && Cadence.RealTime < s.NextCheck) { SetWorking(co, false); return; }
        s.OutputWait = false;
        try
        {
            if (s.State.HoldsProducts && !Deliver(co, s)) return;
            if (!Charge(co, s)) return;
            if (Destination(co, true, SabatierRules.WaterKgPerCycle, out string waterWhy) == null) { Wait(co, s, Text.Get("Sabatier.waiting_output", waterWhy)); return; }
            if (Destination(co, false, SabatierRules.MethaneKgPerCycle, out string methaneWhy) == null) { Wait(co, s, Text.Get("Sabatier.waiting_output", methaneWhy)); return; }
        }
        catch (Exception ex) { Fault(co, ex); return; }
        SetWorking(co, true);
    }
    /// <summary>Tops up the reactant hold: hydrogen from the linked store under both guards, CO2 from the canister.</summary>
    private static bool Charge(CondOwner co, Session s)
    {
        double needH2 = SabatierRules.HydrogenKgPerCycle - s.State.HydrogenKg;
        if (needH2 > 1e-9)
        {
            var store = HydrogenSource(co, needH2, out string why);
            if (store == null) { Wait(co, s, Text.Get("Sabatier.waiting_hydrogen", why)); return false; }
            LiquidTransferGuard.Commit(new BulkVessel.Endpoint(store), new Hold(co, s, ManufacturingRules.Hydrogen), needH2, BulkVessel.Guard(store), Guard(co));
            if (s.Protected) { Stop(co, s, Text.Get("Sabatier.protected")); return false; }
        }
        double needCO2 = SabatierRules.CarbonDioxideKgPerCycle - s.State.CarbonDioxideKg;
        if (needCO2 > 1e-9)
        {
            double moles = NativeGasCanister.Moles(SabatierRules.CarbonDioxide, needCO2);
            var canister = Canister(co, s, moles, out string why);
            if (canister == null) { Wait(co, s, Text.Get("Sabatier.waiting_co2", why)); return false; }
            if (BulkVessels.IsVessel(canister))
                LiquidTransferGuard.Commit(new BulkVessel.Endpoint(canister), new Hold(co, s, ManufacturingRules.CarbonDioxide), needCO2, BulkVessel.Guard(canister), Guard(co));
            else
            {
                double taken = NativeGasCanister.TryTake(canister, SabatierRules.CarbonDioxide, moles);
                s.State.CarbonDioxideKg = Math.Min(SabatierRules.CarbonDioxideKgPerCycle, s.State.CarbonDioxideKg + NativeGasCanister.Kilograms(SabatierRules.CarbonDioxide, taken));
                Save(co, s);
            }
            if (s.Protected) { Stop(co, s, Text.Get("Sabatier.protected")); return false; }
            if (!s.State.Charged) { Wait(co, s, Text.Get("Sabatier.waiting_co2", Text.Get("Sabatier.canister_short", 0.0, needCO2))); return false; }
        }
        return true;
    }
    /// <summary>Sends made products to their vessels; returns true when nothing is left waiting.</summary>
    private static bool Deliver(CondOwner co, Session s)
    {
        foreach (bool water in new[] { true, false })
        {
            double kg = water ? s.State.WaterKg : s.State.MethaneKg;
            if (kg <= 1e-9) continue;
            var vessel = Destination(co, water, kg, out string why);
            if (vessel == null) { Wait(co, s, Text.Get("Sabatier.waiting_output", why)); return false; }
            LiquidTransferGuard.Commit(new Hold(co, s, water ? ManufacturingRules.Water : ManufacturingRules.Methane), new BulkVessel.Endpoint(vessel), kg, Guard(co), BulkVessel.Guard(vessel));
            if (s.Protected) { Stop(co, s, Text.Get("Sabatier.protected")); return false; }
            if (water) s.State.ProducedWaterKg += kg; else s.State.ProducedMethaneKg += kg;
            Save(co, s);
        }
        if (s.State.HoldsProducts) return false;
        Plugin.Log(Text.Get("Sabatier.cycle_log", co.strID, SabatierRules.WaterKgPerCycle, SabatierRules.MethaneKgPerCycle));
        return true;
    }

    internal static bool BeginPower(Powered power, CondOwner co, ref double amount, out Transfer? transfer)
    {
        transfer = null;
        pending.Remove(power);
        bool working = co.HasCond(ManufacturingRules.Reacting);
        double demand = working ? SabatierRules.WorkingKW : SabatierRules.IdleKW;
        double seconds = amount * Units.SecondsPerHour / demand;
        var air = RoomHeat.Read(co);
        var heat = RoomHeat.Check(air, SabatierRules.RoomHeatKW(working), seconds);
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
        if (!ManufacturingRules.Finite(supplied)) throw new InvalidOperationException("Invalid reactor energy receipt.");
        // All of the electricity ends as room heat.
        RoomHeat.Deposit(transfer.Air, supplied);
        if (!sessions.TryGetValue(co, out var s) || !s.Running || !transfer.Working || !co.HasCond(ManufacturingRules.Reacting) || s.Protected || !s.State.Charged) return;
        double before = s.State.CycleKWh;
        var (credited, complete) = SabatierRules.Advance(before, supplied);
        // The reaction releases its heat in step with the cycle's progress.
        double reactionKWh = (credited - before) / SabatierRules.CycleKWh * SabatierRules.ReactionKWhPerCycle;
        if (reactionKWh > 0) RoomHeat.Deposit(transfer.Air, reactionKWh);
        s.State.CycleKWh = credited;
        s.Status = co.HasCond("IsPowered") ? Text.Get("Sabatier.working", s.State.CycleKWh, SabatierRules.CycleKWh) : Text.Get("Sabatier.waiting_power");
        if (!complete) { Save(co, s); return; }
        s.State.Convert();
        Save(co, s);
        if (!s.Protected) Deliver(co, s);
    }
    internal static void Forget(Powered power) { pending.Remove(power); NativeEnergyReceipts.Forget(power); }

    /// <summary>One of the reactor's own holds as a reservoir for guarded transfers: hydrogen coming in, water or
    /// methane going out. The quantity is the saved record's, and setting it saves.</summary>
    private sealed class Hold : ILiquidReservoir
    {
        private readonly CondOwner co; private readonly Session s; private readonly string commodity;
        internal Hold(CondOwner co, Session s, string commodity) { this.co = co; this.s = s; this.commodity = commodity; }
        public string Identity => co.strID + "." + commodity;
        public string ShipId => co.ship.strRegID;
        public string Commodity => commodity;
        public double QuantityKg => commodity == ManufacturingRules.Hydrogen ? s.State.HydrogenKg : commodity == ManufacturingRules.Water ? s.State.WaterKg :
            commodity == ManufacturingRules.CarbonDioxide ? s.State.CarbonDioxideKg : s.State.MethaneKg;
        public double CapacityKg => commodity == ManufacturingRules.Hydrogen ? SabatierRules.HydrogenKgPerCycle : commodity == ManufacturingRules.Water ? SabatierRules.WaterKgPerCycle :
            commodity == ManufacturingRules.CarbonDioxide ? SabatierRules.CarbonDioxideKgPerCycle : SabatierRules.MethaneKgPerCycle;
        public void SetQuantity(double kg)
        {
            if (commodity == ManufacturingRules.Hydrogen) s.State.HydrogenKg = kg; else if (commodity == ManufacturingRules.Water) s.State.WaterKg = kg;
            else if (commodity == ManufacturingRules.CarbonDioxide) s.State.CarbonDioxideKg = kg; else s.State.MethaneKg = kg;
            Save(co, s);
        }
    }

    /// <summary>After the native damage switch: the reactor's gases escape. CO2 and methane go into the room as
    /// native gases (or to space when there is no air); its hydrogen burns by the fuel-store rule when the room
    /// has oxygen and an ignition source, otherwise it escapes too. Water, a liquid, stays in the product hold.</summary>
    internal static void Damaged(CondOwner co)
    {
        if (co == null || co.bDestroyed || !co.HasCond("IsDamaged") || !co.HasCond("IsInstalled")) return;
        try { Dump(co, "Sabatier.damaged_log"); } catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    internal static void Destroying(CondOwner co)
    {
        if (co == null || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || co.HasCond("IsModeSwitching", false)) return;
        try { Dump(co, "Sabatier.destroyed_log"); } catch (Exception e) { Plugin.Log(e.ToString()); }
    }
    private static void Dump(CondOwner co, string logKey)
    {
        var s = Get(co);
        if (s.Protected) return;
        double h2 = s.State.HydrogenKg, co2 = s.State.CarbonDioxideKg, ch4 = s.State.MethaneKg;
        if (h2 + co2 + ch4 <= 1e-9) return;
        Stop(co, s, Text.Get("Sabatier.repair_first"), needsAttention: false);
        s.State.HydrogenKg = 0; s.State.CarbonDioxideKg = 0; s.State.MethaneKg = 0; s.State.CycleKWh = 0;
        Save(co, s);
        var air = RoomHeat.Read(co);
        if (air != null)
        {
            if (co2 > 0) RoomGas.Emit(air, SabatierRules.CarbonDioxide, co2);
            if (ch4 > 0) RoomGas.Emit(air, SabatierRules.MethaneSpecies, ch4);
        }
        bool burned = h2 > 0 && StoreService.Ignite(co, GasStores.Hydrogen, h2, "Sabatier.burn_log");
        string log = Text.Get(logKey, co.strNameFriendly, co2, ch4, h2, burned ? Text.Get("Sabatier.hydrogen_burned") : Text.Get("Sabatier.hydrogen_escaped"));
        Plugin.Log(log);
        PlayerNotices.Post(co.ship, "PhobosManufacturing.reactor", NoticeLevel.Caution, log, Text.Get("Sabatier.dump_banner"));
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
        return Text.Get("Sabatier.status", s.Status, st.HydrogenKg, st.CarbonDioxideKg, st.WaterKg, st.MethaneKg, st.CycleKWh, SabatierRules.CycleKWh, st.Cycles,
                st.ProducedWaterKg, st.ProducedMethaneKg, st.ConsumedCarbonDioxideKg,
                co.HasCond("IsPowered") ? Text.Get("Content.powered") : Text.Get("Content.no_power"),
                ObjectPresentation.Name(HydrogenPeer(co)), CanisterName(co), ObjectPresentation.Name(WaterPeer(co)), ObjectPresentation.Name(MethanePeer(co))) +
            "\n" + Text.Get("Sabatier.demand", SabatierRules.WorkingKW, SabatierRules.RoomHeatKW(true)) + (s.LastStop == null ? "" : "\n" + Text.Get("Content.last_stop", s.LastStop));
    }
    internal static string CanisterId(CondOwner co) => Get(co).State.Canister;
    internal static string CanisterName(CondOwner co) { var s = Get(co); return s.State.Canister.Length == 0 ? ConsoleText.Get("not_selected") : ObjectPresentation.Name(s.State.Canister); }
    internal static bool Link(CondOwner co, string kind, string id, ConsoleBinding? binding, out string reason)
    {
        reason = Content.Access(co, binding) ?? "";
        if (reason.Length > 0) return false;
        var s = Get(co);
        if (co.HasCond(ManufacturingRules.Reacting) || s.Running) { reason = Text.Get("Sabatier.link_busy"); return false; }
        if (kind == "canister")
        {
            if (id != "none" && (!CanisterCandidates(co).Any(c => c.strID == id) || !ProcessorState.SafeId(id))) { reason = Text.Get("Sabatier.link_missing"); return false; }
            s.State.Canister = id == "none" ? "" : id; Save(co, s);
            reason = Text.Get(id == "none" ? "Sabatier.unlinked" : "Sabatier.linked"); return !s.Protected;
        }
        VesselLink link;
        switch (kind)
        {
            case "water": link = WaterLink; break;
            case "hydrogen": link = HydrogenLink; break;
            case "methane": link = MethaneLink; break;
            default: reason = Text.Get("Content.unsupported_action"); return false;
        }
        if (id == "none") { link.Unlink(co, CrewWork.Resolve); reason = Text.Get("Sabatier.unlinked"); return true; }
        var target = link.Candidates(co).FirstOrDefault(v => v.strID == id);
        if (target == null) { reason = Text.Get("Sabatier.link_missing"); return false; }
        if (!link.Link(co, target, CrewWork.Resolve, out reason)) return false;
        reason = Text.Get("Sabatier.linked"); return true;
    }
    internal static string? MaintenanceReason(CondOwner co)
    {
        if (!SabatierRules.IsFamily(co.strCODef)) return null;
        var s = Get(co);
        if (s.Protected) return Text.Get("Maintenance.protected");
        return s.State.HeldKg > 1e-8 || s.State.CycleKWh > 1e-8 ? Text.Get("Maintenance.reactor") : null;
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Text.Get("Sabatier.fault");
        if (!Content.Ready) { message = Content.Status; return false; }
        foreach (string kind in new[] { "water", "hydrogen", "methane", "canister" })
            if (action.StartsWith(kind + ":", StringComparison.Ordinal)) return Link(co, kind, action.Substring(kind.Length + 1), binding, out message);
        bool result;
        switch (action)
        {
            case "start": result = Start(co, binding); break;
            case "pause": result = Pause(co, false, binding); break;
            case "cancel": result = Pause(co, true, binding); break;
            case "accept": result = Content.Access(co, binding) == null && Accept(co); message = Text.Get(result ? "Sabatier.accept_done" : "Sabatier.accept_unavailable"); return result;
            case "status": result = true; break;
            default: message = Text.Get("Content.unsupported_action"); return false;
        }
        message = Describe(co);
        return result;
    }
}
