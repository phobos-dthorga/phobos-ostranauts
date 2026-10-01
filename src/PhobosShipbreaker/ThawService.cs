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
    /// <summary>What crew may bring: loose water ice always; methane ice only when a methane store is linked, so a
    /// feed never fills with blocks the unit cannot finish.</summary>
    internal static bool CrewFeed(CondOwner t2, CondOwner input) => CrewLogistics.Loose(input) && ThawRules.IsFeed(input.strCODef) &&
        ProcessRules.MassMatches(input.GetCondAmount("StatMass"), ThawRules.FeedKg(input.strCODef)) &&
        (input.strCODef != ThawRules.MethaneIce || CrewWork.Resolve(MethanePeer(t2)) != null);
    internal static bool CanFeed(CondOwner bin, CondOwner input) => !bin.HasCond("IsLocked") &&
        (CrewLogistics.IsUnitPreflight(input) ? bin.objCOParent != null && CrewFeed(bin.objCOParent, input) : ValidIce(input)) &&
        bin.objContainer != null && (bin.objContainer.ContainedCOs.Contains(input) || bin.objContainer.ContainedCOs.Count < ThawRules.FeedCapacity);

    /// <summary>The shared links (Shipbreaker 0.53.0): a water vessel touching the T2 or on its process-water line, a
    /// methane store touching it or on its gas line; each vessel-side port is a bank several machines share. Saved
    /// pairs from before read as the bank's first slot.</summary>
    internal static readonly VesselLink WaterLink = new(ThawRules.OutPort, ThawRules.VesselPort, ThawRules.Commodity);
    internal static readonly VesselLink MethaneLink = new(ThawRules.MethaneOutPort, ThawRules.MethaneInPort, ThawRules.MethaneCommodity);
    internal static string Peer(CondOwner t2) => WaterLink.PeerId(t2);
    internal static string MethanePeer(CondOwner t2) => MethaneLink.PeerId(t2);
    /// <summary>Registered water vessels the T2 reaches: touching or on its process-water line.</summary>
    internal static IEnumerable<CondOwner> Candidates(CondOwner t2) => WaterLink.Candidates(t2);
    /// <summary>Registered methane vessels (Phobos Manufacturing's stores, when installed) touching or on its gas line.</summary>
    internal static IEnumerable<CondOwner> MethaneCandidates(CondOwner t2) => MethaneLink.Candidates(t2);
    /// <summary>The linked water vessel when it can take one block's water now; otherwise null with the reason.</summary>
    internal static CondOwner? Vessel(CondOwner t2, double kg, out string reason) =>
        Destination(t2, WaterLink, kg, "Thaw.no_vessel", "Thaw.vessel_not_ready", "Thaw.vessel_protected", "Thaw.vessel_full", out reason);
    /// <summary>The linked methane store when it can take one block's methane now; otherwise null with the reason.</summary>
    internal static CondOwner? MethaneStore(CondOwner t2, double kg, out string reason) =>
        Destination(t2, MethaneLink, kg, "Thaw.no_methane_store", "Thaw.methane_not_ready", "Thaw.methane_protected", "Thaw.methane_full", out reason);
    private static CondOwner? Destination(CondOwner t2, VesselLink link, double kg, string missing, string notReady, string guarded, string full, out string reason)
    {
        reason = Text.Get(missing);
        var vessel = CrewWork.Resolve(link.PeerId(t2));
        if (vessel == null || !BulkVessels.IsVessel(vessel) || BulkVessels.SpecFor(vessel.strCODef)?.Commodity != link.Commodity) return null;
        reason = Text.Get(notReady);
        if (!link.Connected(t2, vessel)) return null;
        reason = Text.Get(guarded);
        if (BulkVessel.Protected(vessel) || CommodityReservations.Held(vessel.strID)) return null;
        var s = BulkVessel.Snapshot(vessel);
        reason = Text.Get("Thaw.vessel_catch");
        if (s.CatchKg > 1e-8) return null;
        reason = Text.Get(full, s.HeadroomKg, kg);
        if (s.HeadroomKg + 1e-8 < kg) return null;
        reason = ""; return vessel;
    }
    private static double Kg(ProcessRecipe recipe, string id) => recipe.Products.FirstOrDefault(p => p.Id == id)?.Kg ?? 0;
    /// <summary>Every destination one block of this recipe needs, ready now; otherwise null with the first reason.</summary>
    private static CondOwner? Ready(CondOwner t2, ProcessRecipe recipe, out CondOwner? methaneStore, out string reason)
    {
        methaneStore = null;
        var water = Vessel(t2, Kg(recipe, ThawRules.Commodity), out reason);
        if (water == null) return null;
        double methane = Kg(recipe, ThawRules.MethaneCommodity);
        if (methane > 0 && (methaneStore = MethaneStore(t2, methane, out reason)) == null) return null;
        return water;
    }
    internal static string? MachineProblem(CondOwner co)
    {
        if (!Content.Ready) return Content.Status;
        if (co == null || co.bDestroyed || co.strCODef != ThawRules.Installed || !co.HasCond("IsInstalled")) return Text.Get("Thaw.install_first");
        if (co.HasCond("IsDamaged")) return Text.Get("Thaw.repair_first");
        if (co.ship == null || (int)co.ship.LoadState < 2) return Text.Get("ProcessingService.ship_is_not_loaded");
        var feed = Feed(co);
        if (co.HasCond("IsLocked") || feed?.HasCond("IsLocked") == true || co.objContainer?.Locked == true || feed?.objContainer?.Locked == true) return Text.Get("Thaw.unlock");
        if (co.HasCond("IsOverrideOff") || co.HasCond("IsSignalOff")) return Text.Get("Thaw.switched_off");
        if (co.objContainer == null || feed?.objContainer == null) return Text.Get("Thaw.missing_feed");
        return null;
    }
    private static void SetWorking(CondOwner co, bool value) => co.SetCondAmount(ProcessRules.Working, value ? 1 : 0);
    /// <summary>Started blocks first (their progress is saved on them), then the furthest along.</summary>
    private static IEnumerable<CondOwner> Ordered(IEnumerable<CondOwner>? inputs) => (inputs ?? Array.Empty<CondOwner>())
        .OrderByDescending(c => c.GetCondAmount(ProcessRules.Revision) != 0 || c.GetCondAmount(ProcessRules.Progress) != 0 || c.GetCondAmount(ProcessRules.Duration) != 0)
        .ThenByDescending(c => c.GetCondAmount(ProcessRules.Progress));
    private static CondOwner? NextInput(IEnumerable<CondOwner>? inputs) => Ordered(inputs).FirstOrDefault();
    /// <summary>Each feed identity has its own immutable recipe catalog and cycle; a block's saved revision is
    /// read against its own identity's catalog.</summary>
    private static ProcessJob ReadJob(CondOwner input) => ProcessJob.CreateOrResume(ThawRules.RecipesFor(input.strCODef), input.strID,
        input.GetCondAmount(ProcessRules.Progress), input.GetCondAmount(ProcessRules.Revision), input.GetCondAmount(ProcessRules.Duration), ThawRules.CycleSecondsFor(input.strCODef));
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
        // The first block whose destinations are ready runs: a methane block waiting for its store never holds up
        // water ice behind it.
        string? problem = null, wait = null;
        CondOwner? input = null; ProcessJob? job = null;
        foreach (var candidate in Ordered(inputs))
        {
            if (!ValidIce(candidate)) { problem ??= Text.Get("Thaw.invalid_feed", ThawRules.IceKg, ThawRules.MethaneIceKg); continue; }
            ProcessJob read;
            try { read = ReadJob(candidate); }
            catch (ArgumentException ex) { problem ??= ex.Message; continue; }
            if (Ready(co, read.Recipe, out _, out string why) == null) { wait ??= why; continue; }
            input = candidate; job = read; break;
        }
        if (input == null || job == null)
        {
            if (wait != null) { s.VesselWait = true; s.NextVesselCheck = StarSystem.fEpoch + VesselRecheckSeconds; s.Status = Text.Get("Thaw.waiting_vessel", wait); return false; }
            s.Status = problem ?? Text.Get("Thaw.feed_empty"); return false;
        }
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
            if (!ThawRules.IsFeed(input.strCODef)) continue;
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
    /// <summary>One block becomes its recipe's products: water in the linked vessel, methane (from methane ice) in
    /// the linked methane store, gangue in the tray. The gangue is placed first and the block removed second, as
    /// the D4 places products before consuming its input; each vessel's conversion journal is open across both,
    /// so an interruption leaves evidence rather than duplicated or vanished mass.</summary>
    private static bool Finish(CondOwner co, Session s)
    {
        SetWorking(co, false);
        var input = s.Input!; var job = s.Job!; var recipe = job.Recipe;
        if (!job.Complete || !MatchesJob(input, job) || input.objCOParent != Feed(co) || !ValidIce(input) || MachineProblem(co) != null)
        { Stop(co, s, Text.Get("Thaw.input_changed")); return false; }
        var vessel = Ready(co, recipe, out var methaneStore, out string why);
        if (vessel == null) { s.VesselWait = true; s.NextVesselCheck = StarSystem.fEpoch + VesselRecheckSeconds; s.Status = Text.Get("Thaw.waiting_vessel", why); return false; }
        double waterKg = Kg(recipe, ThawRules.Commodity), methaneKg = Kg(recipe, ThawRules.MethaneCommodity), gangueKg = Kg(recipe, ThawRules.Gangue);
        var gangue = DataHandler.GetCondOwner(ThawRules.Gangue);
        bool published = false;
        try
        {
            if (gangue == null || !ProcessRules.MassMatches(gangue.GetTotalMass(), gangueKg) || gangue.coStackHead != null || gangue.aStack.Count != 0 || gangue.GetCOsSafe(true).Count != 0)
                throw new InvalidOperationException(Text.Get("Thaw.output_definition_changed"));
            // The gangue joins a gangue stack already in the tray, or takes a free cell (Shipbreaker 0.65.0).
            var delivery = co.objContainer == null || !co.objContainer.AllowedCO(gangue) ? null : TrayDelivery.Plan(co.objContainer, new[] { gangue });
            if (delivery == null)
            {
                // The unplaced gangue is never left behind while the unit waits and checks again.
                gangue.Destroy();
                s.VesselWait = true; s.NextVesselCheck = StarSystem.fEpoch + VesselRecheckSeconds; s.Status = Text.Get("Thaw.tray_full"); return false;
            }
            var state = BulkVessel.Read(vessel);
            var methaneState = methaneStore == null ? null : BulkVessel.Read(methaneStore);
            BulkVessel.BeginConversion(vessel, input.strID, state.TotalKg);
            if (methaneStore != null) BulkVessel.BeginConversion(methaneStore, input.strID, methaneState!.TotalKg);
            void EndConversions() { BulkVessel.EndConversion(vessel); if (methaneStore != null) BulkVessel.EndConversion(methaneStore); }
            try { delivery.Place(); }
            catch { delivery.Rollback(); EndConversions(); throw; }
            if (!StackUnits.Inside(gangue, co)) throw new InvalidOperationException(Text.Get("Thaw.output_placement_failed"));
            published = true;
            bool retired;
            try { input.RemoveFromCurrentHome(bForce: true); }
            finally { retired = input.objCOParent == null && input.ship == null; }
            if (!retired)
            {
                delivery.Rollback();
                if (gangue.objCOParent != null || gangue.ship != null) gangue.RemoveFromCurrentHome(bForce: true);
                gangue.Destroy(); published = false;
                EndConversions();
                throw new InvalidOperationException(Text.Get("Thaw.input_not_removed"));
            }
            input.Destroy();
            state.SetService(state.ServiceKg + waterKg); BulkVessel.Save(vessel, state);
            if (methaneStore != null) { methaneState!.SetService(methaneState.ServiceKg + methaneKg); BulkVessel.Save(methaneStore, methaneState); }
            EndConversions();
            co.objContainer!.Redraw(); Feed(co)?.objContainer?.Redraw();
        }
        catch { if (gangue != null && !published && gangue.objCOParent == null && !gangue.bDestroyed) gangue.Destroy(); throw; }
        Plugin.Log(methaneStore == null ? Text.Get("Thaw.completed_log", job.InputId, waterKg, vessel.strID, gangueKg)
            : Text.Get("Thaw.completed_methane_log", job.InputId, waterKg, vessel.strID, methaneKg, methaneStore.strID, gangueKg));
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
            (MethanePeer(co).Length > 0 ? Text.Get("Thaw.methane_to", ObjectPresentation.Name(MethanePeer(co))) : "") +
            "\n" + Text.Get("Thaw.demand", ThawRules.WorkingKW, ThawRules.WorkingKW * ThawRules.RoomHeatFraction) + IndustryObservations.ExplainStop(co);
    }
    internal static bool Link(CondOwner co, string id, ConsoleBinding? binding, out string reason) => Link(co, id, false, binding, out reason);
    /// <summary>Links water to a vessel, or (methane) methane ice's methane to a methane store; each touching the T2 or on
    /// the matching line.</summary>
    internal static bool Link(CondOwner co, string id, bool methane, ConsoleBinding? binding, out string reason)
    {
        reason = ProcessingService.AccessProblem(co, binding) ?? "";
        if (reason.Length > 0) return false;
        if (co.HasCond(ProcessRules.Working) || sessions.TryGetValue(co, out var s) && s.Job?.Running == true) { reason = Text.Get("Thaw.link_busy"); return false; }
        var link = methane ? MethaneLink : WaterLink;
        if (id == "none") { link.Unlink(co, CrewWork.Resolve); reason = Text.Get("Thaw.unlinked"); return true; }
        var vessel = link.Candidates(co).FirstOrDefault(v => v.strID == id);
        if (vessel == null) { reason = Text.Get(methane ? "Thaw.methane_link_missing" : "Thaw.link_missing"); return false; }
        if (!link.Link(co, vessel, CrewWork.Resolve, out reason)) return false;
        reason = Text.Get(methane ? "Thaw.methane_linked" : "Thaw.linked"); return true;
    }
    internal static bool Command(CondOwner co, ConsoleBinding? binding, string action, out string message)
    {
        message = Text.Get("Thaw.fault");
        if (!Content.Ready) { message = Content.Status; return false; }
        if (action.StartsWith("link:", StringComparison.Ordinal)) return Link(co, action.Substring(5), false, binding, out message);
        if (action.StartsWith("methane-link:", StringComparison.Ordinal)) return Link(co, action.Substring(13), true, binding, out message);
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
