using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ostranauts.Inventory;
using Phobos.Ostranauts.Framework.Inventory;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal sealed partial class ProcessingService
{
    private sealed class IntakeSession
    {
        internal CondOwner Grabber = null!, Chute = null!, Processor = null!;
        internal bool Armed;
        internal CondOwner? Panel;
        internal TransferClock? Clock;
        internal double Last;
        internal string Status = Text.Get("IntakeService.intake_paused");
        internal readonly CondOwner?[] Walls = new CondOwner?[IntakeRules.Width];
    }
    private ConditionalWeakTable<CondOwner, IntakeSession> intakes = new ConditionalWeakTable<CondOwner, IntakeSession>();

    internal static bool IsGrabber(CondOwner co) => co != null && co.strCODef == IntakeRules.Grabber + "Installed";
    private static double Angle(CondOwner co) => co.Item.TF.eulerAngles.z;
    private static bool Aligned(CondOwner g, CondOwner c, CondOwner p)
    {
        if (g.Item == null || c.Item == null || p.Item == null || g.objCOParent != null || c.objCOParent != null || p.objCOParent != null) return false;
        var gp = g.GetPos(); var cp = c.GetPos(); var pp = p.GetPos();
        return IntakeRules.Connected(gp.x, gp.y, Angle(g), cp.x, cp.y, Angle(c), pp.x, pp.y, Angle(p));
    }

    private static bool FindIntake(CondOwner processor, out IntakeSession? link, out string problem)
    {
        link = null;
        problem = Text.Get("IntakeService.no_aligned_intake_place_a_x_chute");
        if (processor.ship == null) return false;
        var objects = processor.ship.GetCOs(null, bSubObjects: false, bAllowDocked: false, bAllowLocked: true)
            .Where(c => c != null && !c.bDestroyed && c.HasCond("IsInstalled") && IntakeRules.IsHardware(c.strCODef)).ToArray();
        foreach (var g in objects.Where(c => c.strCODef.StartsWith(IntakeRules.Grabber, StringComparison.Ordinal)))
        foreach (var c in objects.Where(c => c.strCODef.StartsWith(IntakeRules.Chute, StringComparison.Ordinal)))
        {
            if (!Aligned(g, c, processor)) continue;
            if (link != null) { problem = Text.Get("IntakeService.ambiguous_intake_layout_leave_one_aligned_grabber"); link = null; return false; }
            link = new IntakeSession { Grabber = g, Chute = c, Processor = processor };
        }
        if (link == null) return false;
        problem = LinkProblem(link) ?? Text.Get("IntakeService.intake_connected");
        return true;
    }

    private static string? LinkProblem(IntakeSession s)
    {
        if (!Content.Ready) return Content.Status;
        if (s.Grabber.bDestroyed || s.Chute.bDestroyed || s.Processor.bDestroyed ||
            s.Grabber.strCODef != IntakeRules.Grabber + "Installed" || s.Chute.strCODef != IntakeRules.Chute + "Installed" ||
            s.Grabber.HasCond("IsDamaged") || s.Chute.HasCond("IsDamaged")) return Text.Get("IntakeService.install_and_repair_all_three_intake_components");
        var ship = s.Processor.ship;
        if (ship == null || (int)ship.LoadState < 2 || s.Grabber.ship != ship || s.Chute.ship != ship || !Aligned(s.Grabber, s.Chute, s.Processor))
            return Text.Get("IntakeService.intake_connection_changed_align_all_three_components");
        for (int col = 0; col < IntakeRules.Width; col++)
        {
            var offset = IntakeRules.Rotate(col - 1.5, 0, Angle(s.Grabber));
            var pos = s.Chute.GetPos();
            var tile = ship.GetTileAtWorldCoords1((float)(pos.x + offset.X), (float)(pos.y + offset.Y), false);
            if (tile?.coProps == null || !tile.coProps.HasCond("IsWall")) return Text.Get("IntakeService.restore_all_four_supporting_hull_walls_intake");
            if (s.Walls[col] == null)
            {
                var supports = new List<CondOwner>();
                ship.GetCOsAtWorldCoords1(new UnityEngine.Vector2((float)(pos.x + offset.X), (float)(pos.y + offset.Y)), null, false, true, supports);
                s.Walls[col] = supports.FirstOrDefault(w => !w.bDestroyed && w.HasCond("IsWall") && w.HasCond("IsInstalled") && !w.HasCond("IsDamaged"));
            }
            var wall = s.Walls[col];
            if (wall == null || wall.bDestroyed || wall.ship != ship || !wall.HasCond("IsWall") || !wall.HasCond("IsInstalled") || wall.HasCond("IsDamaged") ||
                !IntakeRules.Near(wall.GetPos().x, wall.GetPos().y, pos.x + offset.X, pos.y + offset.Y))
                return Text.Get("IntakeService.repair_all_four_supporting_hull_walls_before");
        }
        if (s.Grabber.HasCond("IsLocked") || s.Chute.HasCond("IsLocked")) return Text.Get("IntakeService.unlock_the_grabber_and_chute");
        if (s.Grabber.HasCond("IsOverrideOff") || s.Grabber.HasCond("IsSignalOff")) return Text.Get("IntakeService.grabber_is_switched_off");
        if (s.Grabber.objContainer == null) return Text.Get("IntakeService.grabber_loading_inventory_is_missing");
        return MachineProblem(s.Processor);
    }

    private bool ArmIntake(CondOwner processor, out string message)
    {
        if (!FindIntake(processor, out var found, out message)) return false;
        string? problem = LinkProblem(found!);
        if (problem != null) { message = problem; return false; }
        // Rebinding clears the short transfer clock; the physical panel stays put.
        intakes.Remove(found!.Grabber);
        found.Armed = true; found.Last = StarSystem.fEpoch; found.Status = Text.Get("IntakeService.waiting_for_a_detached_ordinary_wall_in");
        intakes.Add(found.Grabber, found);
        sessions.GetValue(processor, _ => new Session()).Intake = found;
        message = found.Status;
        return true;
    }

    private void DisarmIntake(CondOwner processor)
    {
        // A disconnected grabber may no longer be discoverable by adjacency.
        // The processor keeps a direct reference to the session it armed.
        if (!sessions.TryGetValue(processor, out var state) || state.Intake == null) return;
        var intake = state.Intake;
        intake.Armed = false; intake.Clock = null; intake.Panel = null;
        intake.Status = Text.Get("IntakeService.intake_paused_panels_retained");
        if (!intake.Grabber.bDestroyed) intake.Grabber.ZeroCondAmount(IntakeRules.Working);
    }

    internal bool BeforeIntakePower(CondOwner grabber)
    {
        grabber.ZeroCondAmount(IntakeRules.Working);
        if (!intakes.TryGetValue(grabber, out var state) || !state.Armed) return false;
        string? problem = LinkProblem(state);
        if (problem != null) { state.Status = problem; state.Armed = false; return false; }
        var source = grabber.objContainer;
        if (state.Panel != null && (!source.Contains(state.Panel) || !ValidPanel(state.Panel)))
        { state.Clock = null; state.Panel = null; state.Status = Text.Get("IntakeService.loading_item_changed_selecting_a_new_panel"); }
        if (state.Panel == null)
        {
            state.Panel = source.ContainedCOs.FirstOrDefault(ValidPanel);
            if (state.Panel == null)
            {
                state.Status = source.ContainedCOs.Count == 0 ? Text.Get("IntakeService.waiting_for_a_detached_ordinary_wall_in") :
                    Text.Get("IntakeService.no_supported_wall_in_grabber", PanelProblem(source.ContainedCOs.First()));
                state.Last = StarSystem.fEpoch; return false;
            }
            state.Clock = new TransferClock(state.Panel.strID, options.TransferSeconds);
            state.Last = StarSystem.fEpoch;
        }
        var destination = Feed(state.Processor)!.objContainer;
        if (!CanFeed(destination.CO, state.Panel) || !destination.AllowedCO(state.Panel) || !destination.CanAddSimple(state.Panel, out _))
        { state.Status = Text.Get("IntakeService.processor_feed_full_or_blocked_panel_remains"); state.Last = StarSystem.fEpoch; return false; }
        grabber.SetCondAmount(IntakeRules.Working, 1);
        return true;
    }

    internal void AfterIntakePower(CondOwner grabber, bool requested)
    {
        if (grabber == null || !intakes.TryGetValue(grabber, out var state) || !state.Armed) return;
        try
        {
            double now = StarSystem.fEpoch, elapsed = now - state.Last;
            state.Last = now;
            var previousClock = state.Clock;
            if (!BeforeIntakePower(grabber) || state.Clock == null || state.Panel == null) return;
            if (!ReferenceEquals(previousClock, state.Clock)) elapsed = 0;
            bool powered = requested && grabber.HasCond("IsPowered");
            if (!state.Clock.Advance(state.Panel.strID, elapsed, powered))
            { state.Status = Text.Get("IntakeService.intake_time_gap_start_again_panel_retained"); state.Armed = false; grabber.ZeroCondAmount(IntakeRules.Working); return; }
            state.Status = powered ? Text.Get("IntakeService.moving_panel_s", state.Clock.Progress.ToString("F0"), state.Clock.Duration.ToString("F0")) : Text.Get("IntakeService.grabber_waiting_for_power");
            if (!powered || !state.Clock.Complete) return;
            var transfer = new NativeItemTransfer(grabber.objContainer, Feed(state.Processor)!.objContainer, state.Panel);
            if (!PhysicalTransfer.Commit(transfer)) { state.Status = Text.Get("IntakeService.transfer_blocked_panel_retained"); return; }
            transfer.Redraw();
            state.Clock = null; state.Panel = null; grabber.ZeroCondAmount(IntakeRules.Working);
            var processor = sessions.GetValue(state.Processor, _ => new Session());
            if (processor.Job?.Running != true && !StartNext(state.Processor, processor))
            { state.Status = Text.Get("IntakeService.processor_needs_attention", processor.Status); state.Armed = false; }
        }
        catch (Exception ex) { IntakeFault(grabber, ex); }
    }

    internal void IntakeFault(CondOwner grabber, Exception ex)
    {
        grabber.ZeroCondAmount(IntakeRules.Working);
        if (intakes.TryGetValue(grabber, out var s)) { s.Armed = false; s.Status = Text.Get("IntakeService.intake_fault_paused_check_the_log_before"); }
        log(ex.ToString());
    }

    internal string DescribeIntake(CondOwner processor)
    {
        if (!FindIntake(processor, out var found, out string problem)) return problem;
        string? invalid = LinkProblem(found!);
        if (invalid != null) return invalid;
        string status = intakes.TryGetValue(found!.Grabber, out var s) ? s.Status : Text.Get("IntakeService.intake_connected_paused_after_loading");
        return Text.Get("IntakeService.grabber_contains_item_s", status, found.Grabber.objContainer.ContainedCOs.Count, (found.Grabber.HasCond("IsPowered") ? Text.Get("IntakeService.grabber_powered") : Text.Get("IntakeService.grabber_needs_conduit_power")));
    }

    internal bool OpenInventory(CondOwner processor, bool manualFeed)
    {
        var state = sessions.GetValue(processor, _ => new Session());
        string? problem = AccessProblem(processor) ?? MachineProblem(processor);
        if (problem != null) { state.Status = problem; return false; }
        var target = manualFeed ? Feed(processor) : processor;
        if (target?.objContainer == null || CrewSim.inventoryGUI == null) { state.Status = Text.Get("IntakeService.inventory_is_unavailable"); return false; }
        // Explicitly opens the hidden internal feed as its own titled window.
        CrewSim.inventoryGUI.SpawnInventoryWindow(target, InventoryWindowType.Container, null);
        state.Status = manualFeed ? Text.Get("IntakeService.manual_feed_opened_load_separate_kg_ordinary") : Text.Get("IntakeService.product_tray_opened");
        return true;
    }
}
