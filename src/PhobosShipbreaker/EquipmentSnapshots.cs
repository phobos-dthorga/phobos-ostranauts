using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal sealed partial class ProcessingService
{
    // One association pass for a displayed ship; do not repeat it for every hardware row.
    internal Dictionary<string, EquipmentActivity> IntakeActivities(CondOwner[] equipment)
    {
        var result = new Dictionary<string, EquipmentActivity>(StringComparer.Ordinal);
        foreach (var hardware in equipment.Where(c => IntakeRules.IsHardware(c.strCODef)))
            result[hardware.strID] = new EquipmentActivity(EquipmentState.Blocked, Text.Get("IntakeService.no_aligned_intake_place_a_x_chute"));
        foreach (var processor in equipment.Where(c => Content.IsMachine(c.strCODef)))
        {
            if (!FindIntake(processor, out var link, out string problem, equipment))
            { result[processor.strID] = new EquipmentActivity(EquipmentState.Blocked, problem); continue; }
            string? issue = LinkProblem(link!);
            var activity = new EquipmentActivity(issue == null ? EquipmentState.Ready : EquipmentState.Blocked, issue ?? DescribeIntake(link!));
            result[processor.strID] = result[link!.Chute.strID] = result[link.Grabber.strID] = activity;
        }
        return result;
    }
    internal EquipmentActivity Activity(CondOwner machine)
    {
        string? issue = MachineProblem(machine);
        if (issue != null) return new EquipmentActivity(EquipmentState.Blocked, issue);
        sessions.TryGetValue(machine, out var s);
        if (s?.NeedsAttention == true) return new EquipmentActivity(EquipmentState.Blocked, Describe(machine));
        if (s?.Job?.Running == true) return new EquipmentActivity(machine.HasCond("IsPowered") ? EquipmentState.Running : EquipmentState.Blocked, Describe(machine));
        var input = NextInput(Feed(machine)?.objContainer?.ContainedCOs);
        if (input != null)
        {
            if (!ValidInput(machine, input)) return new EquipmentActivity(EquipmentState.Blocked, IsReclaimer(machine) ? Text.Get("Reclaimer.invalid_feed") : PanelProblem(input));
            try { if (!CanFitBatch(machine, ReadJob(machine, input).Recipe)) return new EquipmentActivity(EquipmentState.Blocked, Text.Get("Industry.output_full")); }
            catch (ArgumentException ex) { return new EquipmentActivity(EquipmentState.Blocked, ex.Message); }
        }
        bool intake = s?.Intake?.Armed == true;
        return new EquipmentActivity(s?.AwaitingFeed == true || intake ? EquipmentState.Waiting : EquipmentState.Paused, Describe(machine));
    }
    internal static CondOwner? PipelineFor(CondOwner hardware) => hardware.ship?.GetCOs(null, false, false, true)
        .Where(c => c != null && !c.bDestroyed && c.ship == hardware.ship && Content.IsMachine(c.strCODef) && c.HasCond("IsInstalled"))
        .FirstOrDefault(p => FindIntake(p, out var link, out _) && (link?.Chute == hardware || link?.Grabber == hardware));
    internal EquipmentActivity IntakeActivity(CondOwner hardware)
    {
        var processor = PipelineFor(hardware);
        if (processor == null || !FindIntake(processor, out var link, out string problem))
            return new EquipmentActivity(EquipmentState.Blocked, Text.Get("IntakeService.no_aligned_intake_place_a_x_chute"));
        string? issue = LinkProblem(link!);
        return new EquipmentActivity(issue == null ? EquipmentState.Ready : EquipmentState.Blocked, issue ?? DescribeIntake(processor));
    }
}

internal sealed partial class CollectorService
{
    internal EquipmentActivity Activity(CondOwner receiver)
    {
        string? problem = MachineProblem(receiver);
        if (problem != null) return new EquipmentActivity(EquipmentState.Blocked, problem);
        sessions.TryGetValue(receiver, out var s);
        if (s?.NeedsAttention == true) return new EquipmentActivity(EquipmentState.Blocked, Describe(receiver));
        // An intentionally unused/unlinked receiving port is not an alarm.
        if (s?.Armed != true) return new EquipmentActivity(EquipmentState.Paused, Describe(receiver));
        problem = PairProblem(receiver, out var source, out _) ?? SourceProblem(receiver, source) ?? FilterProblem(receiver);
        if (problem != null) return new EquipmentActivity(EquipmentState.Blocked, problem);
        if (Destination(receiver)?.ContainedCOs.Count >= ProcessRules.FeedCapacity)
            return new EquipmentActivity(EquipmentState.Blocked, Text.Get("Industry.output_full"));
        return new EquipmentActivity(!receiver.HasCond("IsPowered") ? EquipmentState.Blocked : s.Item != null ? EquipmentState.Running : EquipmentState.Waiting, Describe(receiver));
    }
}
