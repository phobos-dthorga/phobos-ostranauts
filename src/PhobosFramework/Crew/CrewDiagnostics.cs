using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>F3 explanation of why a crew member is or is not studying or working. Read-only.</summary>
internal static class CrewDiagnostics
{
    private static readonly AccessTools.FieldRef<WorkManager, Dictionary<string, List<Task2>>> TasksByTarget =
        AccessTools.FieldRefAccess<WorkManager, Dictionary<string, List<Task2>>>("dictTasks2ByCOID");

    internal static int TaskCount(WorkManager manager) => TasksByTarget(manager)?.Values.Sum(list => list?.Count ?? 0) ?? 0;

    internal static string Describe(string? filter)
    {
        var manager = CrewSim.objInstance?.workManager;
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || manager == null) return CrewWork.Message("diag_loading");
        var members = CrewRoster.Members().Where(c => string.IsNullOrEmpty(filter) ||
            (c.FriendlyName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 || c.strID == filter).ToArray();
        var text = new StringBuilder(CrewWork.Message("diag_header", TaskCount(manager), manager.nTotalTasks));
        if (members.Length == 0) { text.Append("\n" + CrewWork.Message("diag_none")); return text.ToString(); }
        var openers = CrewSpecialities.All.Select(s => (s.Label, Opener: CrewStudy.Opener(s.Id))).ToArray();
        var canStudy = DataHandler.GetCondTrigger(CrewStudy.CanStudy);
        foreach (var member in members)
        {
            int shift = member.Company?.GetShift(StarSystem.nUTCHour, member).nID ?? -1;
            string doing = member.aQueue?.FirstOrDefault(i => i != null && !i.bCancel)?.strTitle ?? CrewWork.Message("diag_idle");
            string shiftName = CrewWork.Message(shift == 2 ? "diag_shift_work" : shift == 1 ? "diag_shift_sleep" : shift == 0 ? "diag_shift_free" : "diag_shift_unknown");
            text.Append("\n" + CrewWork.Message("diag_member", member.FriendlyName, CrewWork.Message(member.HasCond("IsAIManual") ? "diag_off" : "diag_on"),
                shiftName, doing, Outcome(canStudy, member)));
            foreach (var (label, opener) in openers)
            {
                var needs = member.mapIAHist?.Where(p => p.Value?.mapInteractions != null && p.Value.mapInteractions.TryGetValue(opener, out var h) && h.fAverage < 0)
                    .Select(p => p.Key).ToArray() ?? Array.Empty<string>();
                bool known = member.mapIAHist?.Values.Any(h => h?.mapInteractions?.ContainsKey(opener) == true) == true;
                text.Append("\n  " + CrewWork.Message(known ? "diag_history" : "diag_no_history", label, needs.Length, string.Join(", ", needs)));
            }
        }
        var unused = DataHandler.GetCondTrigger(CrewStudy.MaterialUnused);
        foreach (var ship in members.Select(m => m.ship).Where(s => s != null).Distinct())
            foreach (var terminal in ship.GetCOs(null, false, false, true).Where(c => c.HasCond("IsTerminal")).OrderBy(c => c.strID, StringComparer.Ordinal))
                text.Append("\n" + CrewWork.Message("diag_terminal", terminal.strNameFriendly, terminal.strID, Outcome(unused, terminal),
                    terminal.GetCondAmount("IsStudyUsedShort"), terminal.GetCondAmount("IsStudyUsersMax"),
                    CrewWork.Message(openers.All(o => terminal.aInteractions?.Contains(o.Opener) == true) ? "diag_yes" : "diag_no")));
        var crew = CrewRoster.Members();
        foreach (var ship in members.Select(m => m.ship).Where(s => s != null).Distinct())
            foreach (var co in CrewWork.Equipment(ship))
            {
                var status = CrewWork.ReadStatus(co);
                double retry = CrewWork.RetrySeconds(co);
                text.Append("\n" + CrewWork.Message("diag_order", co.strNameFriendly, status.Label, retry > 0 ? CrewWork.Message("diag_retry", retry) : status.Detail));
                var job = CrewWork.Jobs.Values.FirstOrDefault(j => j.Equipment == co);
                if (job == null) continue;
                // Who the native task search would hand this step to right now, by the same admission the claim uses.
                var claimable = job.Worker != null ? new[] { job.Worker.FriendlyName } : crew.Where(c => Admissible(c, job)).Select(c => c.FriendlyName).ToArray();
                text.Append("\n  " + CrewWork.Message("diag_claimable", claimable.Length == 0 ? CrewWork.Message("diag_nobody") : string.Join(", ", claimable)));
            }
        return text.ToString();
    }

    private static bool Admissible(CondOwner member, CrewWork.Job job)
    {
        try { return CrewWork.Admissible(member, job); } catch { return false; }
    }

    private static string Outcome(CondTrigger? trigger, CondOwner co)
    {
        if (trigger == null) return CrewWork.Message("diag_unknown");
        bool passed = trigger.Triggered(co, null, true);
        return passed ? CrewWork.Message("diag_yes") : CrewWork.Message("diag_blocked", trigger.strFailReasonLast);
    }
}
