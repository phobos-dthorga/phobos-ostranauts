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
        internal IntakeSession? Intake;
        internal string Status = Text.Get("ProcessingService.paused_load_panels_and_start_the_queue");
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

    private static string PanelProblem(CondOwner panel)
    {
        if (panel.strCODef != ProcessRules.Wall) return Text.Get("ProcessingService.is_not_a_supported_ordinary_wall", panel.strNameFriendly, panel.strCODef);
        if (panel.HasCond("IsInstalled")) return Text.Get("ProcessingService.detach_the_wall_first");
        if (panel.coStackHead != null || panel.aStack.Count != 0) return Text.Get("ProcessingService.separate_the_wall_stack_first");
        if (panel.GetCOsSafe(true).Count != 0) return Text.Get("ProcessingService.remove_anything_attached_to_or_stored_in");
        return Text.Get("ProcessingService.expected_a_kg_wall_actual_mass_is", panel.GetTotalMass(), ProcessRules.InputKg);
    }

    private static string? MachineProblem(CondOwner machine)
    {
        if (!Content.Ready) return Content.Status;
        if (machine == null || machine.bDestroyed || machine.strCODef != Content.Installed ||
            !machine.HasCond("IsInstalled")) return Text.Get("ProcessingService.install_the_undamaged_fixture_first");
        if (machine.HasCond("IsDamaged")) return Text.Get("ProcessingService.repair_the_fixture_first");
        if (machine.ship == null || (int)machine.ship.LoadState < 2) return Text.Get("ProcessingService.ship_is_not_loaded");
        if (machine.HasCond("IsLocked") || Feed(machine)?.HasCond("IsLocked") == true) return Text.Get("ProcessingService.unlock_the_fixture_and_feed");
        if (machine.HasCond("IsOverrideOff") || machine.HasCond("IsSignalOff")) return Text.Get("ProcessingService.fixture_is_switched_off");
        if (machine.objContainer == null || Feed(machine)?.objContainer == null) return Text.Get("ProcessingService.missing_feed_or_output_tray");
        return null;
    }

    internal static string? AccessProblem(CondOwner machine)
    {
        var actor = CrewSim.GetSelectedCrew();
        if (actor == null || actor.bDestroyed || actor.HasCond("IsDead") || actor.HasCond("Unconscious"))
            return Text.Get("ProcessingService.select_an_awake_crew_member");
        if (actor.ship != machine.ship || TileUtils.TileRange(actor.tf.position, machine.tf.position) > ProcessRules.AccessRangeTiles)
            return Text.Get("ProcessingService.move_the_selected_crew_member_beside_the");
        return null;
    }

    internal bool Start(CondOwner machine)
    {
        var state = sessions.GetValue(machine, _ => new Session());
        string? problem = AccessProblem(machine) ?? MachineProblem(machine);
        if (problem != null) { state.Status = problem; return false; }
        try
        {
            bool connected = ArmIntake(machine, out string intakeMessage);
            if (state.Job?.Running == true) return true;
            if (Feed(machine)!.objContainer.ContainedCOs.Count == 0)
            { state.Status = connected ? Text.Get("ProcessingService.pipeline_started_waiting_for_the_grabber") : Text.Get("ProcessingService.use_manual_feed_for_standalone_processing", intakeMessage); return connected; }
            bool started = StartNext(machine, state);
            if (!started) DisarmIntake(machine);
            return started;
        }
        catch (Exception ex) { Fault(machine, ex); return false; }
    }

    private bool StartNext(CondOwner machine, Session state)
    {
        SetWorking(machine, false);
        state.Job = null; state.Input = null;
        var inputs = Feed(machine)?.objContainer?.ContainedCOs;
        if (inputs == null || inputs.Count == 0) { state.Status = Text.Get("ProcessingService.feed_empty_waiting_for_the_grabber_or"); return false; }
        // A saved, partly processed panel resumes before fresh stock. Never transfer its progress.
        var input = NextInput(inputs)!;
        if (!ValidPanel(input)) { state.Status = PanelProblem(input); return false; }
        ProcessJob job;
        try { job = ReadJob(input); }
        catch (ArgumentException ex) { state.Status = ex.Message; return false; }
        if (!CanFitBatch(machine, job.Recipe)) { state.Status = Text.Get("ProcessingService.output_tray_needs_space_for_recipe_revision", job.Recipe.Revision); return false; }
        state.Job = job;
        state.Input = input; state.Last = StarSystem.fEpoch;
        input.SetCondAmount(ProcessRules.Revision, job.Recipe.Revision);
        input.SetCondAmount(ProcessRules.Duration, job.Duration);
        state.Status = Text.Get("ProcessingService.processing_wall_panel");
        if (state.Job.Complete) return Finish(machine, state);
        SetWorking(machine, true);
        return true;
    }

    private static CondOwner? NextInput(IEnumerable<CondOwner>? inputs) => inputs?
        .OrderByDescending(c => c.GetCondAmount(ProcessRules.Revision) != 0 ||
            c.GetCondAmount(ProcessRules.Progress) != 0 || c.GetCondAmount(ProcessRules.Duration) != 0)
        .ThenByDescending(c => c.GetCondAmount(ProcessRules.Progress)).FirstOrDefault();

    private ProcessJob ReadJob(CondOwner input) => ProcessJob.CreateOrResume(ProcessRecipes.WallPanels,
        input.strID, input.GetCondAmount(ProcessRules.Progress), input.GetCondAmount(ProcessRules.Revision),
        input.GetCondAmount(ProcessRules.Duration), options.CycleSeconds);

    private static bool MatchesJob(CondOwner input, ProcessJob job) => job.MatchesSaved(input.strID,
        input.GetCondAmount(ProcessRules.Progress), input.GetCondAmount(ProcessRules.Revision),
        input.GetCondAmount(ProcessRules.Duration));

    internal bool Pause(CondOwner machine, bool cancel)
    {
        var state = sessions.GetValue(machine, _ => new Session());
        string? problem = AccessProblem(machine);
        if (problem != null) { state.Status = problem; return false; }
        Stop(machine, state, cancel ? Text.Get("ProcessingService.work_cancelled_panel_retained_spent_energy_is") : Text.Get("ProcessingService.paused_progress_retained"));
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

    private void Stop(CondOwner machine, Session state, string message)
    {
        state.Job?.Pause(); state.Status = message; SetWorking(machine, false);
        DisarmIntake(machine);
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
        { Stop(machine, state, problem ?? Text.Get("ProcessingService.input_changed_or_was_removed_queue_paused")); return false; }
        if (!MatchesJob(state.Input!, state.Job))
        { Stop(machine, state, Text.Get("ProcessingService.saved_job_changed_queue_paused_without_overwriting")); return false; }
        if (!CanFitBatch(machine, state.Job.Recipe))
        { Stop(machine, state, Text.Get("ProcessingService.output_blocked_progress_retained_clear_space_and")); return false; }
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
            if (!state.Job.Running) { Stop(machine, state, Text.Get("ProcessingService.time_gap_progress_retained_resume_when_ready")); return; }
            state.Status = powered ? Text.Get("ProcessingService.processing_wall_panel") : Text.Get("ProcessingService.waiting_for_power_progress_retained");
            if (!state.Job.Complete || !powered) return;
            Finish(machine, state);
        }
        catch (Exception ex)
        {
            Stop(machine, state, Text.Get("ProcessingService.processing_fault_queue_paused_check_the_log"));
            log(ex.ToString());
        }
    }

    private bool Finish(CondOwner machine, Session state)
    {
        SetWorking(machine, false);
        var delivery = new NativeDelivery(machine, state.Input!, state.Job!);
        if (BatchDelivery.Commit(delivery) == DeliveryResult.Blocked)
        { Stop(machine, state, Text.Get("ProcessingService.output_changed_progress_retained_clear_space_and")); return false; }
        log(Text.Get("ProcessingService.completed_panel_with_recipe_revision_total_kg", state.Job!.InputId, state.Job.Recipe.Revision, string.Join(", ", state.Job.Recipe.Products.Select(p => Text.Get("ProcessingService.x", p.Count, p.Id)))));
        state.Job = null; state.Input = null;
        if (options.ContinueQueue) StartNext(machine, state);
        else { DisarmIntake(machine); state.Status = Text.Get("ProcessingService.panel_complete_start_again_for_the_next"); }
        return true;
    }

    internal string Describe(CondOwner machine)
    {
        var state = sessions.GetValue(machine, _ => new Session());
        var inputs = Feed(machine)?.objContainer?.ContainedCOs;
        var input = state.Input != null && !state.Input.bDestroyed && inputs?.Contains(state.Input) == true
            ? state.Input : NextInput(inputs);
        string work;
        try
        {
            // Read without stamping conditions or granting permission to run after reload.
            var job = input == null ? null : ReadJob(input);
            work = Text.Get("ProcessingService.recipe_revision_work_s", (job?.Recipe.Revision ?? ProcessRecipes.WallPanels.Current.Revision), (job?.Progress ?? 0), (job?.Duration ?? options.CycleSeconds));
        }
        catch (ArgumentException ex) { work = ex.Message; }
        return Text.Get("ProcessingService.feed_panels", state.Status, (inputs?.Count ?? 0), work, (machine.HasCond("IsPowered") ? Text.Get("ProcessingService.powered") : Text.Get("ProcessingService.no_power")), ProcessRules.FeedCapacity);
    }

    internal static string NewPanelProducts() => string.Join(", ", ProcessRecipes.WallPanels.Current.Products.Select(p =>
        Text.Get("ProcessingService.product_count", p.Count, DataHandler.GetCondOwnerDef(p.Id)?.strNameFriendly ?? p.Id)));

    internal void Reset()
    {
        // Per-object numeric progress is native save data; clocks and run permission are deliberately not.
        sessions = new ConditionalWeakTable<CondOwner, Session>();
        intakes = new ConditionalWeakTable<CondOwner, IntakeSession>();
    }

    internal void Fault(CondOwner machine, Exception ex)
    {
        var state = sessions.GetValue(machine, _ => new Session());
        Stop(machine, state, Text.Get("ProcessingService.processing_fault_queue_paused_check_the_log"));
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

    internal static IReadOnlyList<ItemSize>? OutputSizes(ProcessRecipe recipe)
    {
        var sizes = new List<ItemSize>();
        foreach (var product in recipe.Products)
        {
            var co = DataHandler.GetCondOwnerDef(product.Id);
            if (co == null || !DataHandler.dictItemDefs.TryGetValue(co.strItemDef, out var item)) return null;
            int width = co.inventoryWidth != 0 ? co.inventoryWidth : item.nCols;
            if (item.nCols <= 0) return null;
            int height = co.inventoryHeight != 0 ? co.inventoryHeight : item.aSocketAdds.Length / item.nCols;
            for (int n = 0; n < product.Count; n++) sizes.Add(new ItemSize(width, height));
        }
        return sizes;
    }

    private static bool CanFitBatch(CondOwner machine, ProcessRecipe recipe)
    {
        var sizes = OutputSizes(recipe);
        return machine.objContainer != null && sizes != null &&
            BatchPlacement.Plan(Occupancy(machine.objContainer), sizes) != null;
    }

    private sealed class NativeDelivery : IBatchDelivery
    {
        private readonly CondOwner machine, input;
        private readonly ProcessJob job;
        private readonly List<CondOwner> products = new List<CondOwner>();
        private Position[]? plan;
        private bool retired;
        internal NativeDelivery(CondOwner machine, CondOwner input, ProcessJob job)
        { this.machine = machine; this.input = input; this.job = job; }
        public bool InputConsumed => retired || input == null || input.bDestroyed;
        public bool Prepare()
        {
            if (!job.Complete || !MatchesJob(input, job) || !ValidPanel(input) ||
                input.objCOParent != Feed(machine) || MachineProblem(machine) != null) return false;
            foreach (var spec in job.Recipe.Products)
            for (int n = 0; n < spec.Count; n++)
            {
                var product = DataHandler.GetCondOwner(spec.Id);
                if (product == null) throw new InvalidOperationException(Text.Get("ProcessingService.missing_output", spec.Id));
                products.Add(product);
                if (product.coStackHead != null || product.aStack.Count != 0 || product.GetCOsSafe(true).Count != 0 ||
                    !ProcessRules.MassMatches(product.GetTotalMass(), spec.Kg))
                    throw new InvalidOperationException(Text.Get("ProcessingService.output_definition_changed", spec.Id));
                if (!machine.objContainer.AllowedCO(product)) return false;
            }
            if (!ProcessRules.Balanced(input.GetTotalMass(), products.Select(p => p.GetTotalMass())))
                throw new InvalidOperationException(Text.Get("ProcessingService.batch_would_violate_material_balance"));
            var sizes = products.Select(p => GUIInventoryItem.GetWidthHeightForCO(p))
                .Select(s => new ItemSize(s.x, s.y)).ToArray();
            plan = BatchPlacement.Plan(Occupancy(machine.objContainer), sizes);
            return plan != null;
        }

        public void PlaceProducts()
        {
            if (plan == null) throw new InvalidOperationException(Text.Get("ProcessingService.unprepared_batch"));
            for (int i = 0; i < products.Count; i++)
            {
                var p = products[i];
                machine.objContainer.AddCOSimple(p, new PairXY(plan[i].X, plan[i].Y));
                if (p.objCOParent != machine || !machine.objContainer.ContainedCOs.Contains(p))
                    throw new InvalidOperationException(Text.Get("ProcessingService.output_placement_failed"));
            }
        }

        public void ConsumeInput()
        {
            // Unity executes this synchronous commit without yielding to another gameplay order.
            // Never destroy the input before all products have been placed successfully.
            if (!job.Complete || !MatchesJob(input, job) || input.objCOParent != Feed(machine) || !ValidPanel(input))
                throw new InvalidOperationException(Text.Get("ProcessingService.input_changed_during_completion"));
            try { input.RemoveFromCurrentHome(bForce: true); }
            finally
            {
                // A detached input is retired from the world. Preserve its replacements
                // even if native destruction subsequently fails partway through cleanup.
                retired = input.objCOParent == null && input.ship == null;
            }
            if (!retired) throw new InvalidOperationException(Text.Get("ProcessingService.input_could_not_be_removed"));
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
