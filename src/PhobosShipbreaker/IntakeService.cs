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
        internal string Status = "Intake paused.";
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
        problem = "No aligned intake: place a 4 x 1 chute over four walls, a 4 x 3 grabber outside, and this 4 x 4 processor immediately inside with its mouth toward the chute.";
        if (processor.ship == null) return false;
        var objects = processor.ship.GetCOs(null, bSubObjects: false, bAllowDocked: false, bAllowLocked: true)
            .Where(c => c != null && !c.bDestroyed && c.HasCond("IsInstalled") && IntakeRules.IsHardware(c.strCODef)).ToArray();
        foreach (var g in objects.Where(c => c.strCODef.StartsWith(IntakeRules.Grabber, StringComparison.Ordinal)))
        foreach (var c in objects.Where(c => c.strCODef.StartsWith(IntakeRules.Chute, StringComparison.Ordinal)))
        {
            if (!Aligned(g, c, processor)) continue;
            if (link != null) { problem = "Ambiguous intake layout. Leave one aligned grabber and chute per processor."; link = null; return false; }
            link = new IntakeSession { Grabber = g, Chute = c, Processor = processor };
        }
        if (link == null) return false;
        problem = LinkProblem(link) ?? "Intake connected.";
        return true;
    }

    private static string? LinkProblem(IntakeSession s)
    {
        if (!Content.Ready) return Content.Status;
        if (s.Grabber.bDestroyed || s.Chute.bDestroyed || s.Processor.bDestroyed ||
            s.Grabber.strCODef != IntakeRules.Grabber + "Installed" || s.Chute.strCODef != IntakeRules.Chute + "Installed" ||
            s.Grabber.HasCond("IsDamaged") || s.Chute.HasCond("IsDamaged")) return "Install and repair all three intake components.";
        var ship = s.Processor.ship;
        if (ship == null || (int)ship.LoadState < 2 || s.Grabber.ship != ship || s.Chute.ship != ship || !Aligned(s.Grabber, s.Chute, s.Processor))
            return "Intake connection changed; align all three components on this ship and start again.";
        for (int col = 0; col < IntakeRules.Width; col++)
        {
            var offset = IntakeRules.Rotate(col - 1.5, 0, Angle(s.Grabber));
            var pos = s.Chute.GetPos();
            var tile = ship.GetTileAtWorldCoords1((float)(pos.x + offset.X), (float)(pos.y + offset.Y), false);
            if (tile?.coProps == null || !tile.coProps.HasCond("IsWall")) return "Restore all four supporting hull walls. Intake paused.";
            if (s.Walls[col] == null)
            {
                var supports = new List<CondOwner>();
                ship.GetCOsAtWorldCoords1(new UnityEngine.Vector2((float)(pos.x + offset.X), (float)(pos.y + offset.Y)), null, false, true, supports);
                s.Walls[col] = supports.FirstOrDefault(w => !w.bDestroyed && w.HasCond("IsWall") && w.HasCond("IsInstalled") && !w.HasCond("IsDamaged"));
            }
            var wall = s.Walls[col];
            if (wall == null || wall.bDestroyed || wall.ship != ship || !wall.HasCond("IsWall") || !wall.HasCond("IsInstalled") || wall.HasCond("IsDamaged") ||
                !IntakeRules.Near(wall.GetPos().x, wall.GetPos().y, pos.x + offset.X, pos.y + offset.Y))
                return "Repair all four supporting hull walls before using the intake.";
        }
        if (s.Grabber.HasCond("IsLocked") || s.Chute.HasCond("IsLocked")) return "Unlock the grabber and chute.";
        if (s.Grabber.HasCond("IsOverrideOff") || s.Grabber.HasCond("IsSignalOff")) return "Grabber is switched off.";
        if (s.Grabber.objContainer == null) return "Grabber loading inventory is missing.";
        return MachineProblem(s.Processor);
    }

    private bool ArmIntake(CondOwner processor, out string message)
    {
        if (!FindIntake(processor, out var found, out message)) return false;
        string? problem = LinkProblem(found!);
        if (problem != null) { message = problem; return false; }
        // Rebinding clears the short transfer clock; the physical panel stays put.
        intakes.Remove(found!.Grabber);
        found.Armed = true; found.Last = StarSystem.fEpoch; found.Status = "Waiting for a detached ordinary wall in the grabber.";
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
        intake.Status = "Intake paused; panels retained.";
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
        { state.Clock = null; state.Panel = null; state.Status = "Loading item changed; selecting a new panel."; }
        if (state.Panel == null)
        {
            state.Panel = source.ContainedCOs.FirstOrDefault(ValidPanel);
            if (state.Panel == null)
            {
                state.Status = source.ContainedCOs.Count == 0 ? "Waiting for a detached ordinary wall in the grabber." :
                    "No supported wall in grabber. " + PanelProblem(source.ContainedCOs.First());
                state.Last = StarSystem.fEpoch; return false;
            }
            state.Clock = new TransferClock(state.Panel.strID, options.TransferSeconds);
            state.Last = StarSystem.fEpoch;
        }
        var destination = Feed(state.Processor)!.objContainer;
        if (!CanFeed(destination.CO, state.Panel) || !destination.AllowedCO(state.Panel) || !destination.CanAddSimple(state.Panel, out _))
        { state.Status = "Processor feed full or blocked; panel remains in grabber."; state.Last = StarSystem.fEpoch; return false; }
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
            { state.Status = "Intake time gap; start again. Panel retained."; state.Armed = false; grabber.ZeroCondAmount(IntakeRules.Working); return; }
            state.Status = powered ? "Moving panel: " + state.Clock.Progress.ToString("F0") + "/" + state.Clock.Duration.ToString("F0") + " s." : "Grabber waiting for power.";
            if (!powered || !state.Clock.Complete) return;
            var transfer = new NativeItemTransfer(grabber.objContainer, Feed(state.Processor)!.objContainer, state.Panel);
            if (!PhysicalTransfer.Commit(transfer)) { state.Status = "Transfer blocked; panel retained."; return; }
            transfer.Redraw();
            state.Clock = null; state.Panel = null; grabber.ZeroCondAmount(IntakeRules.Working);
            var processor = sessions.GetValue(state.Processor, _ => new Session());
            if (processor.Job?.Running != true && !StartNext(state.Processor, processor))
            { state.Status = "Processor needs attention: " + processor.Status; state.Armed = false; }
        }
        catch (Exception ex) { IntakeFault(grabber, ex); }
    }

    internal void IntakeFault(CondOwner grabber, Exception ex)
    {
        grabber.ZeroCondAmount(IntakeRules.Working);
        if (intakes.TryGetValue(grabber, out var s)) { s.Armed = false; s.Status = "Intake fault; paused. Check the log before resuming."; }
        log(ex.ToString());
    }

    internal string DescribeIntake(CondOwner processor)
    {
        if (!FindIntake(processor, out var found, out string problem)) return problem;
        string? invalid = LinkProblem(found!);
        if (invalid != null) return invalid;
        string status = intakes.TryGetValue(found!.Grabber, out var s) ? s.Status : "Intake connected; paused after loading.";
        return status + "\nGrabber contains " + found.Grabber.objContainer.ContainedCOs.Count + " item(s). " +
            (found.Grabber.HasCond("IsPowered") ? "Grabber powered." : "Grabber needs conduit power.");
    }

    internal bool OpenInventory(CondOwner processor, bool manualFeed)
    {
        var state = sessions.GetValue(processor, _ => new Session());
        string? problem = AccessProblem(processor) ?? MachineProblem(processor);
        if (problem != null) { state.Status = problem; return false; }
        var target = manualFeed ? Feed(processor) : processor;
        if (target?.objContainer == null || CrewSim.inventoryGUI == null) { state.Status = "Inventory is unavailable."; return false; }
        // Explicitly opens the hidden internal feed as its own titled window.
        CrewSim.inventoryGUI.SpawnInventoryWindow(target, InventoryWindowType.Container, null);
        state.Status = manualFeed ? "Manual feed opened: load separate 24 kg ordinary walls, then Start." : "Product tray opened.";
        return true;
    }
}
