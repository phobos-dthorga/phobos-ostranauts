using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>The T2 ice thaw unit: one saved job per ice block in its feed bin (progress on the block's own
/// conditions, as the D4 does), measured electricity with a declared share into the room's air, and one
/// conversion per block into the linked vessel under that vessel's journal. Reload pauses; Start or a crew
/// order resumes. Nothing warms until the linked vessel can take the water.</summary>
internal static class ThawService
{
    private sealed class Session
    {
        internal ProcessJob? Job;
        internal CondOwner? Input;
        internal double Last, NextVesselCheck;
        internal bool AwaitingFeed, CrewManaged, NeedsAttention, HeatWait, VesselWait;
        internal string Status = Text.Get("Thaw.paused");
    }
    internal sealed class Transfer
    {
        internal GasContainer Gas = null!;
        internal double Mols, WorkSeconds, HeatFraction;
        internal EnergyReceipt Receipt = null!;
    }
    private static ConditionalWeakTable<CondOwner, Session> sessions = new();
    private static readonly ConditionalWeakTable<Powered, Transfer> pending = new();
    private const double VesselRecheckSeconds = 5;
    internal static void Reset() => sessions = new();

    internal static CondOwner? Feed(CondOwner co) => co.compSlots?.GetCOs(ThawRules.InputSlot, true, null)?.FirstOrDefault(c => c != null && c.strCODef == ThawRules.InputBin);
    internal static bool ValidIce(CondOwner? input) => input != null && !input.bDestroyed && input.Crew == null &&
        ThawRules.ValidIce(input.strCODef, input.GetTotalMass(), !input.HasCond("IsInstalled"),
            input.GetCOsSafe(true).Count == 0 && input.GetLotCOs(true).Count == 0, input.coStackHead == null && input.aStack.Count == 0);
    internal static bool CrewFeed(CondOwner input) => CrewLogistics.Loose(input) && input.strCODef == ThawRules.Ice && ProcessRules.MassMatches(input.GetCondAmount("StatMass"), ThawRules.IceKg);
    internal static bool CanFeed(CondOwner bin, CondOwner input) => !bin.HasCond("IsLocked") &&
        (CrewLogistics.IsUnitPreflight(input) ? CrewFeed(input) : ValidIce(input)) &&
        bin.objContainer != null && (bin.objContainer.ContainedCOs.Contains(input) || bin.objContainer.ContainedCOs.Count < ThawRules.FeedCapacity);

    internal static MaterialPort Outlet(CondOwner t2) => new(t2.strID, ThawRules.OutPort, t2.mapGUIPropMaps);
    internal static MaterialPort Inlet(CondOwner vessel) => new(vessel.strID, ThawRules.VesselPort, vessel.mapGUIPropMaps);
    internal static string Peer(CondOwner t2) => PortPairing.Read(Outlet(t2)).PeerObjectId;
    private static int Footprint(CondOwner co) => Math.Max(1, DataHandler.GetCondOwnerDef(co.strCODef)?.inventoryWidth ?? 1);
    internal static bool Adjacent(CondOwner t2, CondOwner vessel)
    { var a = t2.GetPos(); var b = vessel.GetPos(); return ThawRules.Adjacent(a.x, a.y, b.x, b.y, Footprint(vessel)); }
    /// <summary>Registered water vessels within one tile on the same ship.</summary>
    internal static IEnumerable<CondOwner> Candidates(CondOwner t2) => BulkVessels.Aboard(t2.ship, ThawRules.Commodity).Where(v => v != t2 && Adjacent(t2, v));
    /// <summary>The linked vessel when it can take one block's water now; otherwise null with the reason.</summary>
    internal static CondOwner? Vessel(CondOwner t2, out string reason)
    {
        reason = Text.Get("Thaw.no_vessel");
        var vessel = CrewWork.Resolve(Peer(t2));
        if (vessel == null || !BulkVessels.IsVessel(vessel)) return null;
        reason = Text.Get("Thaw.vessel_not_ready");
        if (vessel.ship != t2.ship || !NativeFluidRoute.EndpointReady(vessel) || !PortPairing.Matches(Outlet(t2), Inlet(vessel)) || !Adjacent(t2, vessel)) return null;
        reason = Text.Get("Thaw.vessel_protected");
        if (BulkVessel.Protected(vessel) || CommodityReservations.Held(vessel.strID)) return null;
        var s = BulkVessel.Snapshot(vessel);
        reason = Text.Get("Thaw.vessel_catch");
        if (s.CatchKg > 1e-8) return null;
        reason = Text.Get("Thaw.vessel_full", s.HeadroomKg, ThawRules.WaterKg);
        if (s.HeadroomKg + 1e-8 < ThawRules.WaterKg) return null;
        reason = ""; return vessel;
    }
    internal static string? MachineProblem(CondOwner co)
    {
        if (!Content.Ready) return Content.Status;
        if (co == null || co.bDestroyed || co.strCODef != ThawRules.Installed || !co.HasCond("IsInstalled")) return Text.Get("Thaw.install_first");
        if (co.HasCond("IsDamaged")) return Text.Get("Thaw.repair_first");
        if (co.ship == null || (int)co.ship.LoadState < 2) return Text.Get("ProcessingService.ship_is_not_loaded");
        if (co.HasCond("IsLocked") || Feed(co)?.HasCond("IsLocked") == true || co.objContainer?.Locked == true || Feed(co)?.objContainer?.Locked == true) return Text.Get("Thaw.unlock");
        if (co.HasCond("IsOverrideOff") || co.HasCond("IsSignalOff")) return Text.Get("Thaw.switched_off");
        if (co.objContainer == null || Feed(co)?.objContainer == null) return Text.Get("Thaw.missing_feed");
        return null;
    }
    private static void SetWorking(CondOwner co, bool value) => co.SetCondAmount(ProcessRules.Working, value ? 1 : 0);
    private static CondOwner? NextInput(IEnumerable<CondOwner>? inputs) => inputs?
        .OrderByDescending(c => c.GetCondAmount(ProcessRules.Revision) != 0 || c.GetCondAmount(ProcessRules.Progress) != 0 || c.GetCondAmount(ProcessRules.Duration) != 0)
        .ThenByDescending(c => c.GetCondAmount(ProcessRules.Progress)).FirstOrDefault();
    private static ProcessJob ReadJob(CondOwner input) => ProcessJob.CreateOrResume(ThawRules.Recipes, input.strID,
        input.GetCondAmount(ProcessRules.Progress), input.GetCondAmount(ProcessRules.Revision), input.GetCondAmount(ProcessRules.Duration), ThawRules.CycleSeconds);
    private static bool MatchesJob(CondOwner input, ProcessJob job) => job.MatchesSaved(input.strID,
        input.GetCondAmount(ProcessRules.Progress), input.GetCondAmount(ProcessRules.Revision), input.GetCondAmount(ProcessRules.Duration));

    internal static bool Start(CondOwner co, ConsoleBinding? binding = null)
    {
        var s = sessions.GetValue(co, _ => new Session());
        string? problem = ProcessingService.AccessProblem(co, binding) ?? MachineProblem(co);
        if (problem != null) { s.Status = problem; return false; }
        try
        {
            IndustryObservations.ClearStop(co);
            s.CrewManaged = false; s.NeedsAttention = false; s.AwaitingFeed = true;
            if (s.Job?.Running == true) return true;
            if (Feed(co)!.objContainer.ContainedCOs.Count == 0) { s.Status = Text.Get("Thaw.feed_empty"); return true; }
            bool started = StartNext(co, s);
            // A vessel that cannot take water yet keeps the queue armed; anything else disarms it.
            if (!started && !s.VesselWait) s.AwaitingFeed = false;
            return started || s.VesselWait;
        }
        catch (Exception ex) { Fault(co, ex); return false; }
    }
    internal static bool CrewStart(CondOwner co)
    {
        if (ProcessingService.AccessProblem(co) != null || MachineProblem(co) != null) return false;
        var s = sessions.GetValue(co, _ => new Session());
        s.CrewManaged = true; s.NeedsAttention = false;
        return s.Job?.Running == true || StartNext(co, s);
    }
    private static bool StartNext(CondOwner co, Session s)
    {
        SetWorking(co, false); s.Job = null; s.Input = null; s.VesselWait = false;
        var inputs = Feed(co)?.objContainer?.ContainedCOs;
        if (inputs == null || inputs.Count == 0) { s.Status = Text.Get("Thaw.feed_empty"); return false; }
        var input = NextInput(inputs)!;
        if (!ValidIce(input)) { s.Status = Text.Get("Thaw.invalid_feed", ThawRules.IceKg); return false; }
        ProcessJob job;
        try { job = ReadJob(input); }
        catch (ArgumentException ex) { s.Status = ex.Message; return false; }
        if (Vessel(co, out string why) == null) { s.VesselWait = true; s.NextVesselCheck = StarSystem.fEpoch + VesselRecheckSeconds; s.Status = Text.Get("Thaw.waiting_vessel", why); return false; }
        s.Job = job; s.Input = input; s.Last = StarSystem.fEpoch;
        input.SetCondAmount(ProcessRules.Revision, job.Recipe.Revision);
        input.SetCondAmount(ProcessRules.Duration, job.Duration);
        s.Status = Text.Get("Thaw.thawing");
        if (job.Complete) return Finish(co, s);
        SetWorking(co, true);
        return true;
    }
    internal static bool Pause(CondOwner co, bool cancel, ConsoleBinding? binding = null)
    {
        var s = sessions.GetValue(co, _ => new Session());
        string? problem = ProcessingService.AccessProblem(co, binding);
        if (problem != null) { s.Status = problem; return false; }
        s.NeedsAttention = false;
        CrewWork.ManualStop(co);
        Stop(co, s, Text.Get(cancel ? "Thaw.cancelled" : "Thaw.paused_retained"), needsAttention: false);
        if (!cancel) return true;
        foreach (var input in Feed(co)?.objContainer?.ContainedCOs ?? Array.Empty<CondOwner>())
        {
            if (input.strCODef != ThawRules.Ice) continue;
            input.ZeroCondAmount(ProcessRules.Progress); input.ZeroCondAmount(ProcessRules.Revision); input.ZeroCondAmount(ProcessRules.Duration);
        }
        s.Input = null; s.Job = null;
        return true;
    }
    private static void Stop(CondOwner co, Session s, string message, bool needsAttention = true)
    {
        IndustryObservations.RecordStop(co, message);
        s.NeedsAttention = needsAttention; s.AwaitingFeed = false; s.VesselWait = false;
        s.Job?.Pause(); s.Status = message; SetWorking(co, false);
    }
    internal static void Block(CondOwner co, string reason) => Stop(co, sessions.GetValue(co, _ => new Session()), reason);
    internal static void Fault(CondOwner co, Exception ex)
    {
        var s = sessions.GetValue(co, _ => new Session());
        Stop(co, s, Text.Get("Thaw.fault")); s.NeedsAttention = true;
        Plugin.Log(ex.ToString());
    }

    /// <summary>Before the native power step: feed that arrived while armed starts, a finished block waiting for
    /// vessel room retries, and a running job is re-validated against its saved block.</summary>
    internal static void BeforePower(CondOwner co)
    {
        if (!sessions.TryGetValue(co, out var s)) { SetWorking(co, false); return; }
        if (s.VesselWait && StarSystem.fEpoch >= s.NextVesselCheck)
        {
            s.NextVesselCheck = StarSystem.fEpoch + VesselRecheckSeconds;
            var fault = MachineProblem(co);
            if (fault != null) { Stop(co, s, fault); return; }
            if (s.Job?.Complete == true && s.Input != null) Finish(co, s);
            else if (s.AwaitingFeed || s.CrewManaged) StartNext(co, s);
        }
        else if (s.AwaitingFeed && s.Job?.Running != true && !s.VesselWait)
        {
            var problem = MachineProblem(co);
            if (problem != null) { Stop(co, s, problem); return; }
            if (Feed(co)?.objContainer?.ContainedCOs.Count == 0) { s.Status = Text.Get("Thaw.feed_empty"); return; }
            if (!StartNext(co, s) && !s.VesselWait) s.AwaitingFeed = false;
        }
        if (s.Job?.Running != true) { if (!s.VesselWait) SetWorking(co, false); return; }
        bool bound = s.Input != null && Feed(co)?.objContainer?.ContainedCOs.Contains(s.Input) == true;
        string? machineProblem = MachineProblem(co);
        if (!bound || !ValidIce(s.Input) || machineProblem != null) { Stop(co, s, machineProblem ?? Text.Get("Thaw.input_changed")); return; }
        if (!MatchesJob(s.Input!, s.Job)) { Stop(co, s, Text.Get("ProcessingService.saved_job_changed_queue_paused_without_overwriting")); return; }
        SetWorking(co, !s.Job.Complete);
    }
    internal static bool BeginPower(Powered power, CondOwner co, ref double amount, out Transfer? transfer)
    {
        transfer = null;
        pending.Remove(power);
        bool working = co.HasCond(ProcessRules.Working);
        double demand = working ? ThawRules.WorkingKW : ThawRules.IdleKW;
        double seconds = amount * Units.SecondsPerHour / demand, heatKW = ThawRules.RoomHeatKW(working);
        var room = co.ship?.GetRoomAtWorldCoords1(co.GetPos("use"), false)?.CO;
        var gas = room?.GasContainer;
        double mols = 0;
        if (gas == null || !gas.mapGasMols1.TryGetValue("StatGasMolTotal", out mols) ||
            !ReclaimerRules.CoolingBudget(mols, room!.GetCondAmount("StatGasTemp"), gas.fDGasTemp, room.GetCondAmount("StatGasPressure"), heatKW, seconds, out _))
        {
            // Not a stop: no power is drawn this step, the job keeps its permission and progress, and work
            // continues by itself once the room can take the heat.
            if (sessions.TryGetValue(co, out var s) && s.Job?.Running == true) { s.HeatWait = true; s.Status = ReclaimerHeat.WaitStatus(room, gas, mols); }
            co.ZeroCondAmount("IsPowered");
            return false;
        }
        if (sessions.TryGetValue(co, out var ready)) ready.HeatWait = false;
        transfer = new Transfer { Gas = gas, Mols = mols, WorkSeconds = seconds, HeatFraction = heatKW / demand, Receipt = NativeEnergyReceipts.Begin(power, co, amount) };
        pending.Add(power, transfer);
        return true;
    }
    internal static void FinishPower(Powered power, CondOwner co, Transfer? transfer)
    {
        pending.Remove(power);
        if (transfer == null) return;
        double supplied = NativeEnergyReceipts.Complete(power, co, transfer.Receipt);
        if (double.IsNaN(supplied) || double.IsInfinity(supplied)) throw new InvalidOperationException("Invalid thaw energy receipt.");
        // The declared share warms the room's air; the rest went into the ice. Native mixing and cooling follow.
        transfer.Gas.fDGasTemp += supplied * transfer.HeatFraction * Units.SecondsPerHour * ReclaimerRules.JoulesPerKilojoule / (transfer.Mols * ReclaimerRules.GasHeatCapacity);
    }
    internal static void Forget(Powered power) { pending.Remove(power); NativeEnergyReceipts.Forget(power); }
    internal static void AfterPower(CondOwner co, bool workingRequest, double? poweredSeconds)
    {
        if (!sessions.TryGetValue(co, out var s) || s.Job?.Running != true) return;
        try
        {
            double now = StarSystem.fEpoch, elapsed = now - s.Last; s.Last = now;
            var input = s.Input!;
            bool powered = workingRequest && co.HasCond("IsPowered");
            s.Job.Advance(input.strID, poweredSeconds.HasValue ? Math.Min(elapsed, poweredSeconds.Value) : elapsed, powered, true);
            input.SetCondAmount(ProcessRules.Progress, s.Job.Progress);
            if (!s.Job.Running) { Stop(co, s, Text.Get("Thaw.input_changed")); return; }
            if (!powered && s.HeatWait) return;
            s.Status = powered ? Text.Get("Thaw.thawing") : Text.Get("Thaw.waiting_power");
            if (!s.Job.Complete || !powered) return;
            Finish(co, s);
        }
        catch (Exception ex) { Fault(co, ex); }
    }
    /// <summary>One block becomes water in the vessel and gangue in the tray. The gangue is placed first and
    /// the block removed second, as the D4 places products before consuming its input; the vessel's conversion
    /// journal is open across both, so an interruption leaves evidence rather than duplicated or vanished mass.</summary>
    private static bool Finish(CondOwner co, Session s)
    {
        SetWorking(co, false);
        var input = s.Input!; var job = s.Job!;
        if (!job.Complete || !MatchesJob(input, job) || input.objCOParent != Feed(co) || !ValidIce(input) || MachineProblem(co) != null)
        { Stop(co, s, Text.Get("Thaw.input_changed")); return false; }
        var vessel = Vessel(co, out string why);
        if (vessel == null) { s.VesselWait = true; s.NextVesselCheck = StarSystem.fEpoch + VesselRecheckSeconds; s.Status = Text.Get("Thaw.waiting_vessel", why); return false; }
        var gangue = DataHandler.GetCondOwner(ThawRules.Gangue);
        bool published = false;
        try
        {
            if (gangue == null || !ProcessRules.MassMatches(gangue.GetTotalMass(), ThawRules.GangueKg) || gangue.coStackHead != null || gangue.aStack.Count != 0 || gangue.GetCOsSafe(true).Count != 0)
                throw new InvalidOperationException(Text.Get("Thaw.output_definition_changed"));
            if (co.objContainer == null || !co.objContainer.AllowedCO(gangue) || !co.objContainer.CanAddSimple(gangue, out var cell))
            { s.VesselWait = true; s.NextVesselCheck = StarSystem.fEpoch + VesselRecheckSeconds; s.Status = Text.Get("Thaw.tray_full"); return false; }
            var state = BulkVessel.Read(vessel);
            BulkVessel.BeginConversion(vessel, input.strID, state.TotalKg);
            co.objContainer.AddCOSimple(gangue, cell);
            if (gangue.objCOParent != co || !co.objContainer.ContainedCOs.Contains(gangue)) throw new InvalidOperationException(Text.Get("Thaw.output_placement_failed"));
            published = true;
            bool retired;
            try { input.RemoveFromCurrentHome(bForce: true); }
            finally { retired = input.objCOParent == null && input.ship == null; }
            if (!retired)
            {
                gangue.RemoveFromCurrentHome(bForce: true); gangue.Destroy(); published = false;
                BulkVessel.EndConversion(vessel);
                throw new InvalidOperationException(Text.Get("Thaw.input_not_removed"));
            }
            input.Destroy();
            state.SetService(state.ServiceKg + ThawRules.WaterKg); BulkVessel.Save(vessel, state);
            BulkVessel.EndConversion(vessel);
            co.objContainer.Redraw(); Feed(co)?.objContainer?.Redraw();
        }
        catch { if (gangue != null && !published && gangue.objCOParent == null && !gangue.bDestroyed) gangue.Destroy(); throw; }
        Plugin.Log(Text.Get("Thaw.completed_log", job.InputId, ThawRules.WaterKg, vessel.strID, ThawRules.GangueKg));
        s.Job = null; s.Input = null; s.VesselWait = false;
        if (!s.CrewManaged && Plugin.Options.ContinueQueue)
        {
            if (!StartNext(co, s) && !s.VesselWait && Feed(co)?.objContainer?.ContainedCOs.Count > 0) s.AwaitingFeed = false;
        }
        else { s.AwaitingFeed = false; s.Status = Text.Get("Thaw.complete"); }
        s.CrewManaged = false;
        return true;
    }

    internal static EquipmentState State(CondOwner co)
    {
        if (co.HasCond("IsDamaged") || co.HasCond("IsLocked")) return EquipmentState.Blocked;
        sessions.TryGetValue(co, out var s);
        if (s?.NeedsAttention == true) return EquipmentState.Blocked;
        if (co.HasCond(ProcessRules.Working)) return co.HasCond("IsPowered") ? EquipmentState.Running : EquipmentState.Waiting;
        return s != null && (s.AwaitingFeed || s.VesselWait) ? EquipmentState.Waiting : EquipmentState.Paused;
    }
    internal static string Describe(CondOwner co)
    {
        var s = sessions.GetValue(co, _ => new Session());
        var inputs = Feed(co)?.objContainer?.ContainedCOs;
        var input = s.Input != null && !s.Input.bDestroyed && inputs?.Contains(s.Input) == true ? s.Input : NextInput(inputs);
        double progress = input?.GetCondAmount(ProcessRules.Progress) ?? 0, duration = input != null && input.GetCondAmount(ProcessRules.Duration) > 0 ? input.GetCondAmount(ProcessRules.Duration) : ThawRules.CycleSeconds;
        return Text.Get("Thaw.status", s.Status, inputs?.Count ?? 0, ThawRules.FeedCapacity, progress, duration,
            co.HasCond("IsPowered") ? Text.Get("ProcessingService.powered") : Text.Get("ProcessingService.no_power"), ObjectPresentation.Name(Peer(co))) +
            "\n" + Text.Get("Thaw.demand", ThawRules.WorkingKW, ThawRules.WorkingKW * ThawRules.RoomHeatFraction) + IndustryObservations.ExplainStop(co);
    }
    internal static bool Link(CondOwner co, string id, ConsoleBinding? binding, out string reason)
    {
        reason = ProcessingService.AccessProblem(co, binding) ?? "";
        if (reason.Length > 0) return false;
        if (co.HasCond(ProcessRules.Working) || sessions.TryGetValue(co, out var s) && s.Job?.Running == true) { reason = Text.Get("Thaw.link_busy"); return false; }
        var current = CrewWork.Resolve(Peer(co));
        if (id == "none")
        {
            PortPairing.Unlink(Outlet(co), current == null ? null : Inlet(current));
            reason = Text.Get("Thaw.unlinked"); return true;
        }
        var vessel = Candidates(co).FirstOrDefault(v => v.strID == id);
        if (vessel == null) { reason = Text.Get("Thaw.link_missing"); return false; }
        if (current != null && current != vessel) PortPairing.Unlink(Outlet(co), Inlet(current));
        if (PortPairing.Matches(Outlet(co), Inlet(vessel))) { reason = Text.Get("Thaw.linked"); return true; }
        if (!PortPairing.TryLink(Outlet(co), Inlet(vessel), out reason)) return false;
        reason = Text.Get("Thaw.linked"); return true;
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Text.Get("Thaw.fault");
        if (!Content.Ready) { message = Content.Status; return false; }
        if (action.StartsWith("link:", StringComparison.Ordinal)) return Link(co, action.Substring(5), binding, out message);
        bool result;
        switch (action)
        {
            case "start": result = Start(co, binding); break;
            case "pause": result = Pause(co, false, binding); break;
            case "cancel": result = Pause(co, true, binding); break;
            case "status": result = true; break;
            default: message = Text.Get("Industry.unsupported_action"); return false;
        }
        message = Describe(co);
        return result;
    }
}
