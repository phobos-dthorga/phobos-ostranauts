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
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The Corker-2 bottling unit (Manufacturing 0.40.0): Start arms it; while powered and linked to an ethanol cask
/// and a water vessel that hold a batch, with room in its tray, it credits measured electricity to the batch, and at
/// 0.2 kWh settles it in one journaled step (Framework <see cref="CommoditySettlement"/>): the ethanol and water leave
/// their vessels and seven servings of spirit enter the tray. The batch's energy is its saved record; running permission
/// is not, so a reload waits for Start. It holds no liquid of its own between batches.</summary>
internal static class BottlerService
{
    private sealed class Session
    {
        internal BottlerState State = new();
        internal bool Protected, Running, HeatWait, OutputWait, NeedsAttention;
        internal double NextCheck;
        internal string Status = Text.Get("Bottler.paused");
        internal string? LastStop;
    }
    internal sealed class Transfer { internal RoomHeat.Air Air = null!; internal double HeatFraction; internal EnergyReceipt Receipt = null!; }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    private static readonly ConditionalWeakTable<Powered, Transfer> pending = new();
    internal static void Reset() => sessions = new();

    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, BottlerRules.Record, Plugin.Id, 1);
    private static Session Get(CondOwner co)
    {
        if (sessions.TryGetValue(co, out var found)) return found;
        var s = new Session();
        var status = Store(co).Read(out var fields);
        if (status == SavedStateStatus.Ready) { try { s.State = BottlerState.Read(fields); } catch (Exception e) { s.Protected = true; Plugin.Log(e.ToString()); } }
        else if (status != SavedStateStatus.Missing) s.Protected = true;
        if (s.Protected) s.Status = Text.Get("Bottler.protected");
        sessions.Add(co, s);
        return s;
    }
    private static void Save(CondOwner co, Session s)
    {
        if (!Store(co).TryWrite(s.State.Save())) { s.Protected = true; s.Status = Text.Get("Bottler.protected"); }
    }
    internal static bool Protected(CondOwner co) => Get(co).Protected;
    /// <summary>Owner-confirmed recovery: a readable record is trusted; an unreadable one starts a fresh batch.</summary>
    internal static bool Accept(CondOwner co)
    {
        var s = Get(co);
        var status = Store(co).Read(out var fields);
        BottlerState state = new();
        try { if (status == SavedStateStatus.Ready) state = BottlerState.Read(fields); } catch (Exception e) { Plugin.Log(e.ToString()); }
        s.State = state; s.Protected = false; Save(co, s);
        if (!s.Protected) s.Status = Text.Get("Bottler.paused");
        return !s.Protected;
    }

    /// <summary>The shared links (owner link rule): an ethanol cask touching the unit or on its ethanol line, and a water
    /// vessel touching it or on its process-water line.</summary>
    internal static readonly VesselLink EthanolLink = new(BottlerRules.EthanolPort, BottlerRules.EthanolVesselPort, LiquidStores.Ethanol);
    internal static readonly VesselLink WaterLink = new(BottlerRules.WaterPort, BottlerRules.WaterVesselPort, ManufacturingRules.Water);
    private static readonly IReadOnlyList<SettlementNeed> Needs = SettlementPlan.Build(new[] {
        new SettlementLeg(LiquidStores.Ethanol, SettlementRole.Draw, BottlerRules.EthanolPerBatchKg),
        new SettlementLeg(ManufacturingRules.Water, SettlementRole.Draw, BottlerRules.WaterPerBatchKg) });
    /// <summary>A linked vessel ready to give its share of the batch now; null with the reason otherwise.</summary>
    private static CondOwner? Source(CondOwner co, VesselLink link, string what, out string reason)
    {
        var vessel = CrewWork.Resolve(link.PeerId(co));
        if (vessel == null || BulkVessels.Of(vessel)?.Commodity != link.Commodity) { reason = Text.Get("Bottler.no_" + what); return null; }
        if (!link.Connected(co, vessel)) { reason = Text.Get("Bottler." + what + "_not_ready"); return null; }
        var need = Needs.Single(n => n.Commodity == link.Commodity);
        var snapshot = BulkVessel.Snapshot(vessel);
        switch (SettlementPlan.Check(need, snapshot, CommodityReservations.Held(vessel.strID)))
        {
            case SettlementRefusal.Protected: case SettlementRefusal.Busy: reason = Text.Get("Bottler." + what + "_protected"); return null;
            case SettlementRefusal.Catch: reason = Text.Get("Bottler." + what + "_catch"); return null;
            case SettlementRefusal.Short: reason = Text.Get("Bottler." + what + "_short", snapshot.AvailableKg, need.NeedAvailableKg); return null;
        }
        reason = ""; return vessel;
    }
    internal static string? MachineProblem(CondOwner co)
    {
        if (!Content.Ready) return Content.Status;
        if (co == null || co.bDestroyed || co.strCODef != BottlerRules.Installed || !co.HasCond("IsInstalled")) return Text.Get("Bottler.install_first");
        if (co.HasCond("IsDamaged")) return Text.Get("Bottler.repair_first");
        if (co.ship == null || (int)co.ship.LoadState < 2) return Text.Get("Content.ship_not_loaded");
        if (co.HasCond("IsLocked") || co.objContainer?.Locked == true) return Text.Get("Bottler.unlock");
        if (co.HasCond("IsOverrideOff") || co.HasCond("IsSignalOff")) return Text.Get("Bottler.switched_off");
        return null;
    }
    // Written only when it changes: an idle machine reaches this on every power step.
    private static void SetWorking(CondOwner co, bool value) { if (co.HasCond(ManufacturingRules.Bottling) != value) co.SetCondAmount(ManufacturingRules.Bottling, value ? 1 : 0); }

    /// <summary>afterLoad: the one start a machine that was running gets after a reload, where no crew member need stand by.</summary>
    internal static bool Start(CondOwner co, ConsoleBinding? binding = null, bool afterLoad = false)
    {
        var s = Get(co);
        string? problem = (afterLoad ? null : Content.Access(co, binding)) ?? MachineProblem(co);
        if (problem != null) { s.Status = problem; return false; }
        if (s.Protected) { s.Status = Text.Get("Bottler.protected"); return false; }
        s.LastStop = null; s.NeedsAttention = false; s.Running = true; Phobos.Ostranauts.Framework.Persistence.ResumeAfterLoad.Mark(co, true); s.OutputWait = false;
        s.Status = Text.Get("Bottler.armed");
        return true;
    }
    internal static bool Pause(CondOwner co, bool cancel, ConsoleBinding? binding = null)
    {
        var s = Get(co);
        string? problem = Content.Access(co, binding);
        if (problem != null) { s.Status = problem; return false; }
        s.NeedsAttention = false;
        CrewWork.ManualStop(co);
        Stop(co, s, Text.Get(cancel ? "Bottler.cancelled" : "Bottler.paused_retained"), needsAttention: false);
        if (cancel && !s.Protected) { s.State.BatchKWh = 0; Save(co, s); }
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
        Stop(co, s, Text.Get("Bottler.fault")); s.NeedsAttention = true;
        Plugin.Log(ex.ToString());
    }
    private static void Wait(CondOwner co, Session s, string status)
    {
        // Rechecks follow real time: a game-time interval would shrink to every frame at fast-forward.
        s.OutputWait = true; s.NextCheck = Cadence.RealTime + ManufacturingRules.VesselRecheckSeconds; s.Status = status; SetWorking(co, false);
    }
    /// <summary>Whether seven servings fit the tray now (the game's own stack limits, Framework TrayDelivery).</summary>
    private static bool TrayRoom(CondOwner co)
    {
        if (co.objContainer == null) return false;
        var probe = Enumerable.Range(0, BottlerRules.ServingsPerBatch).Select(_ => DataHandler.GetCondOwner(BottlerRules.Spirit)).ToList();
        try { return probe.All(p => p != null && co.objContainer.AllowedCO(p)) && TrayDelivery.Plan(co.objContainer, probe) != null; }
        finally { foreach (var p in probe) if (p != null && !p.bDestroyed) p.Destroy(); }
    }

    /// <summary>Before the native power step: an armed unit works only while both vessels can give a batch and the tray
    /// has room for it.</summary>
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
        if (s.Protected) { Stop(co, s, Text.Get("Bottler.protected")); return; }
        if (s.OutputWait && Cadence.RealTime < s.NextCheck) { SetWorking(co, false); return; }
        s.OutputWait = false;
        try
        {
            if (Source(co, EthanolLink, "ethanol", out string why) == null || Source(co, WaterLink, "vessel", out why) == null) { Wait(co, s, Text.Get("Bottler.waiting", why)); return; }
            // The tray is probed once per batch, before its first energy (the probe builds seven servings and discards them);
            // the settlement itself is the final check.
            if (s.State.BatchKWh <= 0 && !TrayRoom(co)) { Wait(co, s, Text.Get("Bottler.tray_full")); return; }
        }
        catch (Exception ex) { Fault(co, ex); return; }
        SetWorking(co, true);
    }
    internal static bool BeginPower(Powered power, CondOwner co, ref double amount, out Transfer? transfer)
    {
        transfer = null;
        pending.Remove(power);
        bool working = co.HasCond(ManufacturingRules.Bottling);
        double demand = working ? BottlerRules.WorkingKW : BottlerRules.IdleKW;
        double seconds = amount * Units.SecondsPerHour / demand;
        var air = RoomHeat.Read(co);
        var heat = RoomHeat.Check(air, demand, seconds);
        if (!heat.Admitted)
        {
            if (sessions.TryGetValue(co, out var s) && s.Running) { s.HeatWait = true; s.Status = RoomHeat.Describe(heat); }
            co.ZeroCondAmount("IsPowered");
            return false;
        }
        if (sessions.TryGetValue(co, out var ready)) ready.HeatWait = false;
        // Every watt of a bottler ends up as heat in its room.
        transfer = new Transfer { Air = air!, HeatFraction = 1, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
        pending.Add(power, transfer);
        return true;
    }
    internal static void FinishPower(Powered power, CondOwner co, Transfer? transfer)
    {
        pending.Remove(power);
        if (transfer == null) return;
        double supplied = NativeEnergyReceipts.Complete(power, co, transfer.Receipt);
        if (!ManufacturingRules.Finite(supplied)) throw new InvalidOperationException("Invalid bottling energy receipt.");
        RoomHeat.Deposit(transfer.Air, supplied, transfer.HeatFraction);
        if (!sessions.TryGetValue(co, out var s) || !s.Running || !co.HasCond(ManufacturingRules.Bottling) || s.Protected) return;
        var (credited, complete) = BottlerRules.Advance(s.State.BatchKWh, supplied);
        s.State.BatchKWh = credited;
        s.Status = co.HasCond("IsPowered") ? Text.Get("Bottler.working", ManufacturingRules.MinutesLeft(s.State.BatchKWh, BottlerRules.BatchKWh, BottlerRules.WorkingKW)) : Text.Get("Bottler.waiting_power");
        if (complete) Settle(co, s);
        else Save(co, s);
    }
    internal static void Forget(Powered power) { pending.Remove(power); NativeEnergyReceipts.Forget(power); }
    /// <summary>A completed batch: both vessels are checked, then one settlement draws the ethanol and water and places the
    /// seven servings; a full tray changes nothing and waits.</summary>
    private static void Settle(CondOwner co, Session s)
    {
        var ethanol = Source(co, EthanolLink, "ethanol", out string why);
        var water = ethanol == null ? null : Source(co, WaterLink, "vessel", out why);
        if (ethanol == null || water == null) { Save(co, s); Wait(co, s, Text.Get("Bottler.waiting", why)); return; }
        var vessels = new List<(SettlementNeed, CondOwner)> { (Needs.Single(n => n.Commodity == LiquidStores.Ethanol), ethanol), (Needs.Single(n => n.Commodity == ManufacturingRules.Water), water) };
        var result = CommoditySettlement.Commit(vessels, co.strID, new Bottling(co));
        if (result != DeliveryResult.Completed) { Save(co, s); Wait(co, s, Text.Get("Bottler.tray_full")); return; }
        s.State.BatchKWh = 0; s.State.Batches++; Save(co, s);
        Plugin.Log(Text.Get("Bottler.batch_log", co.strID, BottlerRules.ServingsPerBatch, ethanol.strID, water.strID));
    }
    /// <summary>The batch's servings: created, checked against the liquid drawn, planned into the tray and placed. There is
    /// no item input; the liquids are the vessels' legs of the settlement.</summary>
    private sealed class Bottling : IBatchDelivery
    {
        private readonly CondOwner machine; private readonly List<CondOwner> servings = new(); private TrayDelivery? plan; private bool done;
        internal Bottling(CondOwner machine) { this.machine = machine; }
        public bool InputConsumed => done;
        public bool Prepare()
        {
            if (machine.objContainer == null) return false;
            for (int n = 0; n < BottlerRules.ServingsPerBatch; n++)
            {
                var serving = DataHandler.GetCondOwner(BottlerRules.Spirit) ?? throw new InvalidOperationException(Text.Get("Bottler.missing_output"));
                servings.Add(serving);
                if (!ProcessMaterial.MassMatches(serving.GetTotalMass(), BottlerRules.ServingKg)) throw new InvalidOperationException(Text.Get("Bottler.output_definition_changed"));
                if (!machine.objContainer.AllowedCO(serving)) return false;
            }
            if (!ProcessMaterial.Balanced(BottlerRules.EthanolPerBatchKg + BottlerRules.WaterPerBatchKg, servings.Select(p => p.GetTotalMass()))) throw new InvalidOperationException(Text.Get("Bottler.unbalanced"));
            plan = TrayDelivery.Plan(machine.objContainer, servings);
            return plan != null;
        }
        public void PlaceProducts()
        {
            if (plan == null) throw new InvalidOperationException(Text.Get("Bottler.unprepared"));
            plan.Place();
            if (servings.Any(p => !StackUnits.Inside(p, machine))) throw new InvalidOperationException(Text.Get("Bottler.output_placement_failed"));
        }
        public void ConsumeInput() { done = true; machine.objContainer?.Redraw(); }
        public void RollbackProducts()
        {
            plan?.Rollback();
            foreach (var serving in servings)
            {
                if (serving == null || serving.bDestroyed) continue;
                if (serving.objCOParent != null || serving.ship != null) serving.RemoveFromCurrentHome(bForce: true);
                serving.Destroy();
            }
            servings.Clear();
            machine.objContainer?.Redraw();
        }
    }

    internal static EquipmentState State(CondOwner co)
    {
        if (co.HasCond("IsDamaged") || co.HasCond("IsLocked")) return EquipmentState.Blocked;
        var s = Get(co);
        if (s.NeedsAttention || s.Protected) return EquipmentState.Blocked;
        if (co.HasCond(ManufacturingRules.Bottling)) return co.HasCond("IsPowered") ? EquipmentState.Running : EquipmentState.Waiting;
        return s.Running ? EquipmentState.Waiting : EquipmentState.Paused;
    }
    internal static string Describe(CondOwner co)
    {
        var s = Get(co);
        return Text.Get("Bottler.status", s.Status, ManufacturingRules.PercentDone(s.State.BatchKWh, BottlerRules.BatchKWh), BottlerRules.BatchKWh, s.State.Batches, s.State.Batches * BottlerRules.ServingsPerBatch,
            co.HasCond("IsPowered") ? Text.Get("Content.powered") : Text.Get("Content.no_power"), ObjectPresentation.Name(EthanolLink.PeerId(co)), ObjectPresentation.Name(WaterLink.PeerId(co))) +
            "\n" + Text.Get("Bottler.demand", BottlerRules.WorkingKW, BottlerRules.ServingsPerBatch, BottlerRules.EthanolPerBatchKg * 1000, BottlerRules.WaterPerBatchKg * 1000) +
            (s.LastStop == null ? "" : "\n" + Text.Get("Content.last_stop", s.LastStop));
    }
    internal static bool Link(CondOwner co, string kind, string id, ConsoleBinding? binding, out string reason)
    {
        reason = Content.Access(co, binding) ?? "";
        if (reason.Length > 0) return false;
        var s = Get(co);
        if (co.HasCond(ManufacturingRules.Bottling) || s.Running) { reason = Text.Get("Bottler.link_busy"); return false; }
        var link = kind == "ethanol" ? EthanolLink : WaterLink;
        if (id == "none") { link.Unlink(co, CrewWork.Resolve); reason = Text.Get("Bottler.unlinked"); return true; }
        var target = link.Candidates(co).FirstOrDefault(v => v.strID == id);
        if (target == null) { reason = Text.Get("Bottler.link_missing"); return false; }
        if (!link.Link(co, target, CrewWork.Resolve, out reason)) return false;
        reason = Text.Get("Bottler.linked"); return true;
    }
    internal static string? MaintenanceReason(CondOwner co)
    {
        if (!BottlerRules.IsFamily(co.strCODef)) return null;
        var s = Get(co);
        if (s.Protected) return Text.Get("Maintenance.protected");
        return s.State.BatchKWh > 1e-8 ? Text.Get("Maintenance.cycle") : null;
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Text.Get("Bottler.fault");
        if (!Content.Ready) { message = Content.Status; return false; }
        foreach (string kind in new[] { "ethanol", "water" })
            if (action.StartsWith(kind + ":", StringComparison.Ordinal)) return Link(co, kind, action.Substring(kind.Length + 1), binding, out message);
        bool result;
        switch (action)
        {
            case "start": result = Start(co, binding); break;
            case "pause": result = Pause(co, false, binding); break;
            case "cancel": result = Pause(co, true, binding); break;
            case "accept": result = Content.Access(co, binding) == null && Accept(co); message = Text.Get(result ? "Bottler.accept_done" : "Bottler.accept_unavailable"); return result;
            case "status": result = true; break;
            default: message = Text.Get("Content.unsupported_action"); return false;
        }
        message = Describe(co);
        return result;
    }
}
