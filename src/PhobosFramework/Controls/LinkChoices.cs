using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Liquids;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>How a link candidate is shown (Framework 0.57.0): its name, how the machine reaches it (touching, or the
/// line it shares), and whether it can serve the link now (full for a destination, empty for a source). Presentation
/// only: the lists themselves come from the services' reach rule.</summary>
public static class LinkChoices
{
    public static string Label(CondOwner machine, CondOwner vessel, FluidSegmentFamily? family, bool deposit)
    {
        var parts = new List<string>(2);
        var reach = LineReach.Of(machine, vessel, family);
        if (reach == LineReachKind.Adjacent) parts.Add(Text.Get("LinkChoices.touching"));
        else if (reach == LineReachKind.Line && family?.Label != null) parts.Add(family.Label());
        if (BulkVessels.IsVessel(vessel))
        {
            var s = BulkVessel.Snapshot(vessel);
            if (deposit && s.HeadroomKg <= 1e-6) parts.Add(Text.Get("LinkChoices.full"));
            else if (!deposit && s.AvailableKg <= 1e-6) parts.Add(Text.Get("LinkChoices.empty"));
        }
        string name = ObjectPresentation.Name(vessel);
        return parts.Count switch
        {
            0 => name,
            1 => Text.Get("LinkChoices.one", name, parts[0]),
            _ => Text.Get("LinkChoices.two", name, parts[0], parts[1])
        };
    }
    public static string Label(CondOwner machine, CondOwner vessel, VesselLink link, bool deposit) => Label(machine, vessel, link.Family, deposit);

    /// <summary>Why a link picker does not offer what is aboard (Framework 0.69.0): one line for each object in
    /// <paramref name="aboard"/> that is not in <paramref name="offered"/>, with the single thing to fix first (loose,
    /// damaged, locked, no pipe touching it, a drained run, two separate runs), or a line saying nothing of the kind
    /// is aboard. Empty when everything aboard is offered. Read-only; nothing here decides reach.</summary>
    public static string Note(CondOwner machine, FluidSegmentFamily? family, IEnumerable<CondOwner> aboard, IEnumerable<CondOwner> offered)
    {
        if (machine == null) return "";
        var known = new HashSet<CondOwner>(offered ?? Enumerable.Empty<CondOwner>());
        var all = (aboard ?? Enumerable.Empty<CondOwner>()).Where(c => c != null && c != machine && !c.bDestroyed).Distinct().ToArray();
        if (all.Length == 0) return known.Count == 0 ? Text.Get("LinkChoices.none_aboard") : "";
        var lines = new List<string>();
        foreach (var co in all.Where(c => !known.Contains(c)).OrderBy(c => c.strID, System.StringComparer.Ordinal))
        {
            var problem = LineReach.Problem(machine, co, family);
            // In reach but not offered: the machine's own rule left it out (the wrong contents, say), which it explains itself.
            if (problem == ReachProblem.None) continue;
            lines.Add(Text.Get("LinkChoices.not_offered_line", ObjectPresentation.Name(co), Reason(problem, family)));
        }
        return lines.Count == 0 ? "" : Text.Get("LinkChoices.not_offered") + "\n" + string.Join("\n", lines);
    }
    /// <summary>The note for a bulk-vessel link: every vessel of the link's commodity on the machine's ship, in any state.</summary>
    public static string Note(CondOwner machine, VesselLink link, IEnumerable<CondOwner> offered) =>
        machine?.ship == null ? "" : Note(machine, link.Family, BulkVessels.AboardAnyState(machine.ship, link.Commodity), offered);
    /// <summary>The plain reason for one problem; the line's own name ("water line") fills the pipe reasons.</summary>
    public static string Reason(ReachProblem problem, FluidSegmentFamily? family)
    {
        string line = family?.Label?.Invoke() ?? Text.Get("LinkChoices.a_line");
        return problem switch
        {
            ReachProblem.MachineNotReady => Text.Get("LinkChoices.reason_machine"),
            ReachProblem.NotInstalled => Text.Get("LinkChoices.reason_loose"),
            ReachProblem.Damaged => Text.Get("LinkChoices.reason_damaged"),
            ReachProblem.Locked => Text.Get("LinkChoices.reason_locked"),
            ReachProblem.NotReady => Text.Get("LinkChoices.reason_not_ready"),
            ReachProblem.TouchOnly => Text.Get("LinkChoices.reason_touch_only"),
            ReachProblem.LayoutTooLarge => Text.Get("LinkChoices.reason_too_large", line),
            ReachProblem.NoPipeAtStore => Text.Get("LinkChoices.reason_no_pipe_store", line),
            ReachProblem.DrainedAtStore => Text.Get("LinkChoices.reason_drained_store", line),
            ReachProblem.NoPipeAtMachine => Text.Get("LinkChoices.reason_no_pipe_machine", line),
            ReachProblem.DrainedAtMachine => Text.Get("LinkChoices.reason_drained_machine", line),
            ReachProblem.SeparateRuns => Text.Get("LinkChoices.reason_separate", line),
            _ => ""
        };
    }
    /// <summary>What an object is joined to through each line it has a port for, by pipe or by touching, one line of
    /// text per family ("Joined through the water line or by touching: ..."), for a panel's Details page; empty for
    /// an object with no line port.</summary>
    public static string NetworkSummary(CondOwner co)
    {
        if (co?.ship == null) return "";
        var lines = new List<string>();
        foreach (var family in FluidRouteCache.Families.Where(f => f.IsNetwork && f.Label != null).OrderBy(f => f.Id, System.StringComparer.Ordinal))
        {
            if (!(family.Ports!(co) is { Count: > 0 })) continue;
            var names = LineReach.Members(co, family).Where(c => c != null && !c.bDestroyed).Select(ObjectPresentation.Name)
                .OrderBy(n => n, System.StringComparer.CurrentCulture).ToArray();
            lines.Add(names.Length == 0 ? Text.Get("LinkChoices.network_none", family.Label!()) : Text.Get("LinkChoices.network_members", family.Label!(), string.Join(", ", names)));
        }
        return string.Join("\n", lines);
    }
    /// <summary>The machines a vessel's saved links name, for its panel ("Linked: X2, K2"), or null when none.</summary>
    public static string? LinkedMachines(CondOwner vessel)
    {
        var names = LinkedNames(vessel);
        return names.Count == 0 ? null : Text.Get("LinkChoices.linked_machines", string.Join(", ", names));
    }
    /// <summary>The names of the objects a vessel's saved links name, in any mod and any role, sorted.</summary>
    public static IReadOnlyList<string> LinkedNames(CondOwner vessel) =>
        SharedPorts.Peers(vessel).Distinct().Select(id => Crew.CrewWork.Resolve(id)).Where(c => c != null && !c.bDestroyed)
            .Select(c => ObjectPresentation.Name(c!)).OrderBy(n => n, System.StringComparer.CurrentCulture).ToArray();
}
