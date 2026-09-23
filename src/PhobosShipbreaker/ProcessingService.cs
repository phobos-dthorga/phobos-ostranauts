using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ostranauts.Inventory;
using PhobosShipbreaker.Core;
using Phobos.Ostranauts.Framework.Inventory;

namespace PhobosShipbreaker;

internal sealed partial class ProcessingService
{
    private sealed class Session
    {
        internal ProcessJob? Job;
        internal CondOwner? Input;
        internal double Last;
        internal string Status = "Paused. Load panels and start the queue.";
    }
    private ConditionalWeakTable<CondOwner, Session> sessions = new ConditionalWeakTable<CondOwner, Session>();
    private readonly Action<string> log;
    private readonly Settings options;
    internal ProcessingService(Action<string> log, Settings options) { this.log = log; this.options = options; }

    internal static CondOwner? Feed(CondOwner machine) => machine.compSlots?
        .GetCOs(Content.InputSlot, true, null)?.FirstOrDefault(c => c != null && c.strCODef == Content.InputBin);

    internal static bool ValidPanel(CondOwner? panel) => panel != null && !panel.bDestroyed &&
        panel.strCODef == ProcessRules.Wall && !panel.HasCond("IsInstalled") &&
        panel.coStackHead == null && (panel.aStack == null || panel.aStack.Count == 0) &&
        panel.GetCOsSafe(true).Count == 0 && ProcessRules.MassMatches(panel.GetTotalMass(), ProcessRules.InputKg);

    internal static bool CanFeed(CondOwner bin, CondOwner panel) => ValidPanel(panel) &&
        bin.objContainer != null && (bin.objContainer.ContainedCOs.Contains(panel) ||
        bin.objContainer.ContainedCOs.Count < ProcessRules.FeedCapacity);

    private static string? MachineProblem(CondOwner machine)
    {
        if (!Content.Ready) return Content.Status;
        if (machine == null || machine.bDestroyed || machine.strCODef != Content.Installed ||
            !machine.HasCond("IsInstalled")) return "Install the undamaged fixture first.";
        if (machine.HasCond("IsDamaged")) return "Repair the fixture first.";
        if (machine.ship == null || (int)machine.ship.LoadState < 2) return "Ship is not loaded.";
        if (machine.HasCond("IsLocked") || Feed(machine)?.HasCond("IsLocked") == true) return "Unlock the fixture and feed.";
        if (machine.HasCond("IsOverrideOff") || machine.HasCond("IsSignalOff")) return "Fixture is switched off.";
        if (machine.objContainer == null || Feed(machine)?.objContainer == null) return "Missing feed or output tray.";
        return null;
    }

    private static string? AccessProblem(CondOwner machine)
    {
        var actor = CrewSim.GetSelectedCrew();
        if (actor == null || actor.bDestroyed || actor.HasCond("IsDead") || actor.HasCond("Unconscious"))
            return "Select an awake crew member.";
        if (actor.ship != machine.ship || TileUtils.TileRange(actor.tf.position, machine.tf.position) > 4)
            return "Move the selected crew member beside the fixture.";
        return null;
    }

    internal bool Start(CondOwner machine)
    {
        var state = sessions.GetValue(machine, _ => new Session());
        string? problem = AccessProblem(machine) ?? MachineProblem(machine);
        if (problem != null) { state.Status = problem; return false; }
        if (state.Job?.Running == true) return true;
        try { return StartNext(machine, state); }
        catch (Exception ex) { Fault(machine, ex); return false; }
    }

    private bool StartNext(CondOwner machine, Session state)
    {
        SetWorking(machine, false);
        state.Job = null; state.Input = null;
        var inputs = Feed(machine)?.objContainer?.ContainedCOs;
        if (inputs == null || inputs.Count == 0) { state.Status = "Feed empty: load an ordinary loose wall into Wall-panel feed (small grid), then start."; return false; }
        // A saved, partly processed panel resumes before fresh stock. Never transfer its progress.
        var input = inputs.OrderByDescending(c => c.GetCondAmount(ProcessRules.Progress)).First();
        if (!ValidPanel(input)) { state.Status = "Feed requires separate, empty 24 kg ordinary wall panels."; return false; }
        if (!CanFitBatch(machine)) { state.Status = "Output tray needs space for a complete batch."; return false; }
        double progress = input.GetCondAmount(ProcessRules.Progress);
        double savedRevision = input.GetCondAmount(ProcessRules.Revision);
        double savedDuration = input.GetCondAmount(ProcessRules.Duration);
        if (savedRevision != 0 && savedRevision != ProcessRules.RecipeRevision || progress != 0 && savedRevision == 0)
        { state.Status = "Unsupported saved recipe; cancel its work before restarting."; return false; }
        // Pre-release revision-1 panels without a duration used the original 60 seconds.
        double duration = savedRevision == 0 ? options.CycleSeconds : savedDuration == 0 ? ProcessRules.CycleSeconds : savedDuration;
        try { state.Job = new ProcessJob(input.strID, progress, ProcessRules.RecipeRevision, duration); }
        catch (ArgumentException) { state.Status = "Invalid saved progress; cancel its work before restarting."; return false; }
        state.Input = input; state.Last = StarSystem.fEpoch;
        input.SetCondAmount(ProcessRules.Revision, ProcessRules.RecipeRevision);
        input.SetCondAmount(ProcessRules.Duration, duration);
        state.Status = "Processing wall panel";
        if (state.Job.Complete) return Finish(machine, state);
        SetWorking(machine, true);
        return true;
    }

    internal bool Pause(CondOwner machine, bool cancel)
    {
        var state = sessions.GetValue(machine, _ => new Session());
        string? problem = AccessProblem(machine);
        if (problem != null) { state.Status = problem; return false; }
        Stop(machine, state, cancel ? "Work cancelled; panel retained. Spent energy is not refunded." : "Paused; progress retained.");
        if (!cancel) return true;
        // Include saved jobs after reload, when no session binding exists yet.
        foreach (var input in Feed(machine)?.objContainer?.ContainedCOs ?? Array.Empty<CondOwner>())
        {
            if (input.strCODef != ProcessRules.Wall) continue;
            input.ZeroCondAmount(ProcessRules.Progress); input.ZeroCondAmount(ProcessRules.Revision);
            input.ZeroCondAmount(ProcessRules.Duration);
        }
        state.Input = null; state.Job = null;
        return true;
    }

    private static void SetWorking(CondOwner machine, bool value) => machine.SetCondAmount(ProcessRules.Working, value ? 1 : 0);

    private static void Stop(CondOwner machine, Session state, string message)
    {
        state.Job?.Pause(); state.Status = message; SetWorking(machine, false);
    }

    internal bool BeforePower(CondOwner machine)
    {
        if (!Content.IsMachine(machine.strCODef)) return false;
        if (!sessions.TryGetValue(machine, out var state) || state.Job?.Running != true)
        { SetWorking(machine, false); return false; }
        var bin = Feed(machine);
        bool bound = state.Input != null && bin?.objContainer?.ContainedCOs.Contains(state.Input) == true;
        string? problem = MachineProblem(machine);
        if (!bound || !ValidPanel(state.Input) || problem != null)
        { Stop(machine, state, problem ?? "Input changed or was removed; queue paused."); return false; }
        if (!CanFitBatch(machine))
        { Stop(machine, state, "Output blocked; progress retained. Clear space and resume."); return false; }
        return machine.HasCond(ProcessRules.Working);
    }

    internal void AfterPower(CondOwner machine, bool workingRequest)
    {
        if (!Content.IsMachine(machine.strCODef) || !sessions.TryGetValue(machine, out var state) || state.Job?.Running != true) return;
        try
        {
            double now = StarSystem.fEpoch, elapsed = now - state.Last;
            state.Last = now;
            if (!BeforePower(machine)) return;
            var input = state.Input!;
            bool powered = workingRequest && machine.HasCond("IsPowered");
            state.Job.Advance(input.strID, elapsed, powered, true);
            input.SetCondAmount(ProcessRules.Progress, state.Job.Progress);
            if (!state.Job.Running) { Stop(machine, state, "Time gap; progress retained. Resume when ready."); return; }
            state.Status = powered ? "Processing wall panel" : "Waiting for power; progress retained.";
            if (!state.Job.Complete || !powered) return;
            Finish(machine, state);
        }
        catch (Exception ex)
        {
            Stop(machine, state, "Processing fault; queue paused. Check the log before resuming.");
            log(ex.ToString());
        }
    }

    private bool Finish(CondOwner machine, Session state)
    {
        SetWorking(machine, false);
        var delivery = new NativeDelivery(machine, state.Input!);
        if (BatchDelivery.Commit(delivery) == DeliveryResult.Blocked)
        { Stop(machine, state, "Output changed; progress retained. Clear space and resume."); return false; }
        log("Completed panel " + state.Job!.InputId + ": 24 kg -> 11 kg recovered + 13 kg residue.");
        state.Job = null; state.Input = null;
        if (options.ContinueQueue) StartNext(machine, state);
        else state.Status = "Panel complete. Start again for the next panel.";
        return true;
    }

    internal string Describe(CondOwner machine)
    {
        var state = sessions.GetValue(machine, _ => new Session());
        var inputs = Feed(machine)?.objContainer?.ContainedCOs;
        double progress = state.Input != null && !state.Input.bDestroyed ? state.Input.GetCondAmount(ProcessRules.Progress) :
            inputs?.Select(i => i.GetCondAmount(ProcessRules.Progress)).DefaultIfEmpty(0).Max() ?? 0;
        double duration = state.Job?.Duration ?? inputs?.OrderByDescending(i => i.GetCondAmount(ProcessRules.Progress))
            .FirstOrDefault()?.GetCondAmount(ProcessRules.Duration) ?? options.CycleSeconds;
        if (duration <= 0) duration = options.CycleSeconds;
        return state.Status + "\nFeed: " + (inputs?.Count ?? 0) + "/4 panels; work: " +
            progress.ToString("F0") + "/" + duration.ToString("F0") + " s. " + (machine.HasCond("IsPowered") ? "Powered." : "No power.");
    }

    internal void Reset()
    {
        // Per-object numeric progress is native save data; clocks and run permission are deliberately not.
        sessions = new ConditionalWeakTable<CondOwner, Session>();
    }

    internal void Fault(CondOwner machine, Exception ex)
    {
        var state = sessions.GetValue(machine, _ => new Session());
        Stop(machine, state, "Processing fault; queue paused. Check the log before resuming.");
        log(ex.ToString());
    }

    private static bool[,] Occupancy(Container tray)
    {
        var grid = tray.gridLayout;
        var result = new bool[grid.gridMaxX, grid.gridMaxY];
        for (int x = 0; x < grid.gridMaxX; x++)
        for (int y = 0; y < grid.gridMaxY; y++)
            result[x, y] = grid.gridID[x, y] != null || grid.gridInventoryItem[x, y] != null;
        return result;
    }

    private static bool CanFitBatch(CondOwner machine)
    {
        if (machine.objContainer == null) return false;
        var sizes = new List<ItemSize>();
        foreach (var product in ProcessRules.Products)
        {
            var co = DataHandler.GetCondOwnerDef(product.Id);
            if (co == null || !DataHandler.dictItemDefs.TryGetValue(co.strItemDef, out var item)) return false;
            int width = co.inventoryWidth != 0 ? co.inventoryWidth : item.nCols;
            if (item.nCols <= 0) return false;
            int height = co.inventoryHeight != 0 ? co.inventoryHeight : item.aSocketAdds.Length / item.nCols;
            for (int n = 0; n < product.Count; n++) sizes.Add(new ItemSize(width, height));
        }
        return BatchPlacement.Plan(Occupancy(machine.objContainer), sizes) != null;
    }

    private sealed class NativeDelivery : IBatchDelivery
    {
        private readonly CondOwner machine, input;
        private readonly List<CondOwner> products = new List<CondOwner>();
        private Position[]? plan;
        private bool retired;
        internal NativeDelivery(CondOwner machine, CondOwner input) { this.machine = machine; this.input = input; }
        public bool InputConsumed => retired || input == null || input.bDestroyed;
        public bool Prepare()
        {
            if (!ValidPanel(input) || input.objCOParent != Feed(machine) || MachineProblem(machine) != null) return false;
            foreach (var spec in ProcessRules.Products)
            for (int n = 0; n < spec.Count; n++)
            {
                var product = DataHandler.GetCondOwner(spec.Id);
                if (product == null) throw new InvalidOperationException("Missing output " + spec.Id);
                products.Add(product);
                if (product.coStackHead != null || product.aStack.Count != 0 || product.GetCOsSafe(true).Count != 0 ||
                    !ProcessRules.MassMatches(product.GetTotalMass(), spec.Kg))
                    throw new InvalidOperationException("Output definition changed: " + spec.Id);
                if (!machine.objContainer.AllowedCO(product)) return false;
            }
            if (!ProcessRules.Balanced(input.GetTotalMass(), products.Select(p => p.GetTotalMass())))
                throw new InvalidOperationException("Batch would violate material balance");
            var sizes = products.Select(p => GUIInventoryItem.GetWidthHeightForCO(p))
                .Select(s => new ItemSize(s.x, s.y)).ToArray();
            plan = BatchPlacement.Plan(Occupancy(machine.objContainer), sizes);
            return plan != null;
        }

        public void PlaceProducts()
        {
            if (plan == null) throw new InvalidOperationException("Unprepared batch");
            for (int i = 0; i < products.Count; i++)
            {
                var p = products[i];
                machine.objContainer.AddCOSimple(p, new PairXY(plan[i].X, plan[i].Y));
                if (p.objCOParent != machine || !machine.objContainer.ContainedCOs.Contains(p))
                    throw new InvalidOperationException("Output placement failed");
            }
        }

        public void ConsumeInput()
        {
            // Unity executes this synchronous commit without yielding to another gameplay order.
            // Never destroy the input before all products have been placed successfully.
            if (input.objCOParent != Feed(machine) || !ValidPanel(input))
                throw new InvalidOperationException("Input changed during completion");
            try { input.RemoveFromCurrentHome(bForce: true); }
            finally
            {
                // A detached input is retired from the world. Preserve its replacements
                // even if native destruction subsequently fails partway through cleanup.
                retired = input.objCOParent == null && input.ship == null;
            }
            if (!retired) throw new InvalidOperationException("Input could not be removed");
            input.Destroy();
            machine.objContainer.Redraw();
            Feed(machine)?.objContainer?.Redraw();
        }

        public void RollbackProducts()
        {
            foreach (var product in products)
            {
                if (product == null || product.bDestroyed) continue;
                if (product.objCOParent != null || product.ship != null) product.RemoveFromCurrentHome(bForce: true);
                product.Destroy();
            }
            products.Clear();
            machine.objContainer.Redraw();
        }
    }
}
