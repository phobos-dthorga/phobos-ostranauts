using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;

/// <summary>Framework 0.117.0 (owner direction, 6 October 2026): the Crew operations lists group by state with the
/// ones that need the player open, the time-skip estimate reads shifts as runs, and the Upkeep list shows only what
/// needs attention unfolded.</summary>
internal static class CrewPanelRuleChecks
{
    internal static void Run(Action<bool, string> check)
    {
        // Orders: stops and unreadable records need the player; running and waiting are the crew's business.
        check(OrderGroups.Group(OrderState.Blocked) == OrderGroups.NeedsYou && OrderGroups.Group(OrderState.Stopped) == OrderGroups.NeedsYou &&
              OrderGroups.Group(OrderState.Running) == OrderGroups.Working && OrderGroups.Group(OrderState.Waiting) == OrderGroups.Working &&
              OrderGroups.Group(OrderState.Disabled) == OrderGroups.Off && OrderGroups.Group(OrderState.NeedsSetup) == OrderGroups.Off,
            "Every order state has one group: needs you, working, or not set up or off");
        check(OrderGroups.Order.First() == OrderGroups.NeedsYou && !OrderGroups.DefaultFolded.Contains(OrderGroups.NeedsYou) &&
              OrderGroups.DefaultFolded.Contains(OrderGroups.Working) && OrderGroups.DefaultFolded.Contains(OrderGroups.Off),
            "Needs you comes first and starts open; the rest start folded");
        var waiting = new OrderStatus(OrderState.Waiting, "Keep it loaded with feed", "Unassigned", "No loose feed aboard.");
        check(OrderGroups.RowText("V4 Volatiles Refinery", waiting, "Unassigned") == "V4 Volatiles Refinery — " + waiting.Label + "\nKeep it loaded with feed · No loose feed aboard.",
            "A row names the machine and state, then the work and the one thing it waits for");
        var running = new OrderStatus(OrderState.Running, "Keep it loaded with feed", "Jeffrey Bryan", "");
        check(OrderGroups.RowText("V4", running, "Unassigned").EndsWith("Keep it loaded with feed · Jeffrey Bryan", StringComparison.Ordinal) &&
              OrderGroups.RowText("V4", new OrderStatus(OrderState.Disabled, "Work", "Unassigned", ""), "Unassigned").EndsWith("\nWork", StringComparison.Ordinal),
            "With nothing to wait for, the row names who is on it, and an unassigned worker is left out");
        check(OrderGroups.LoadingText("By hand.", "", "") == "By hand." && OrderGroups.LoadingText("By hand.", "Feed store: bin.", "Product store: crate.") == "By hand.\nFeed store: bin.\nProduct store: crate.",
            "A machine without orders says it is loaded by hand, then any store it takes from or sends to");

        // Time-skip: hours collapse into runs of the same shift.
        var runs = ShiftRuns.Compress(new[] { 2, 2, 2, 0, 0, 1 });
        check(runs.Count == 3 && runs[0] == (2, 3) && runs[1] == (0, 2) && runs[2] == (1, 1), "Hours of the same shift become one run");
        string Word(int s) => s == 2 ? "Working" : s == 0 ? "Resting" : "Free time";
        string text = ShiftRuns.Text(runs, Word, (w, h) => w + " for " + h + " h", (w, h) => ", then " + w + " for " + h + " h");
        check(text == "Working for 3 h, then resting for 2 h, then free time for 1 h", "Runs read as one sentence: " + text);
        check(ShiftRuns.Compress(new[] { 0 }).Single() == (0, 1) && ShiftRuns.Compress(Array.Empty<int>()).Count == 0 &&
              ShiftRuns.Text(ShiftRuns.Compress(Array.Empty<int>()), Word, (w, h) => w, (w, h) => w) == "",
            "One hour is one run; no hours read as nothing");

        // Upkeep: only what needs attention is listed open, and only for the switches that are on.
        var A = UpkeepRules.Attention.None;
        check(UpkeepRules.Needs(true, true, 0, null, false, false, 24) == UpkeepRules.Attention.Protected, "An unreadable record always needs attention");
        check(UpkeepRules.Needs(true, false, 0, 1, true, false, 24) == UpkeepRules.Attention.Untuned && UpkeepRules.Needs(true, false, 0, 1, false, false, 24) == A &&
              UpkeepRules.Needs(false, false, 0, 1, true, false, 24) == A && UpkeepRules.Needs(true, false, 6.2, 1, true, false, 24) == A,
            "No tune yet needs attention only while tuning is on, and only for a machine that can be tuned");
        check(UpkeepRules.Needs(true, false, 6, null, false, true, 24) == UpkeepRules.Attention.InspectionDue && UpkeepRules.Needs(true, false, 6, 30, false, true, 24) == UpkeepRules.Attention.InspectionDue &&
              UpkeepRules.Needs(true, false, 6, 3, false, true, 24) == A && UpkeepRules.Needs(true, false, 6, null, false, false, 24) == A,
            "An inspection never done or past its hours needs attention only while rounds are on");
        check(UpkeepRules.Group(UpkeepRules.Attention.Untuned, true) == UpkeepRules.AttentionGroup && UpkeepRules.Group(A, true) == UpkeepRules.FineGroup &&
              UpkeepRules.Group(A, false) == UpkeepRules.InspectOnlyGroup, "Machines fall into needs attention, looked after, or inspection only");

        // Framework 0.118.0: colour as a signal. Three meanings only, and each sits beside a word that says the same.
        check(Tones.ForOrder(OrderState.Blocked) == Tone.Attention && Tones.ForOrder(OrderState.Stopped) == Tone.Attention &&
              Tones.ForOrder(OrderState.Running) == Tone.Good && Tones.ForOrder(OrderState.Waiting) == Tone.Neutral &&
              Tones.ForOrder(OrderState.Disabled) == Tone.Neutral && Tones.ForOrder(OrderState.NeedsSetup) == Tone.Neutral,
            "Stopped and unreadable orders are amber, running ones green, waiting and unset ones plain");
        check(OrderGroups.Order.All(g => (Tones.ForOrderGroup(g) == Tone.Attention) == (g == OrderGroups.NeedsYou)), "Only the Needs you group header is amber");
        check(Tones.ForUpkeepGroup(UpkeepRules.AttentionGroup) == Tone.Attention && Tones.ForUpkeepGroup(UpkeepRules.FineGroup) == Tone.Neutral &&
              Tones.ForUpkeepGroup(UpkeepRules.InspectOnlyGroup) == Tone.Neutral && Tones.ForUpkeep(UpkeepRules.Attention.InspectionDue) == Tone.Attention &&
              Tones.ForUpkeep(UpkeepRules.Attention.None) == Tone.Neutral, "Upkeep that needs attention is amber; the rest is plain");
        check(Tones.ForSkipGroup(CrewSkip.WillRun) == Tone.Good && Tones.ForSkipGroup(CrewSkip.Waits) == Tone.Attention && Tones.ForSkipGroup(CrewSkip.Paused) == Tone.Neutral,
            "In the time-skip estimate, what will run is green, what waits amber, what pauses plain");
        check(Tones.ForApply(true) == Tone.Good && Tones.ForApply(false) == Tone.Neutral, "Apply lights only while there are changes to apply");
        check(Tones.ForResume(false, WorkPermission.Stopped, OrderState.Stopped) == Tone.Good && Tones.ForResume(false, WorkPermission.Suspended, OrderState.Stopped) == Tone.Good &&
              Tones.ForResume(false, WorkPermission.Disabled, OrderState.Disabled) == Tone.Good && Tones.ForResume(true, WorkPermission.Stopped, OrderState.Stopped) == Tone.Neutral &&
              Tones.ForResume(false, WorkPermission.Enabled, OrderState.Running) == Tone.Neutral && Tones.ForResume(false, WorkPermission.Disabled, OrderState.NeedsSetup) == Tone.Neutral,
            "Resume lights when nothing is pending and an order with work chosen is not running; never while changes wait or work is not chosen");

        // The shared collapsible list: header text and the signature that decides a redraw.
        check(GroupedList.HeaderText(true, "Working", 4) == "+ Working (4)" && GroupedList.HeaderText(false, "Needs you", 1) == "- Needs you (1)",
            "A folded group shows +, an open one -, each with its count");
        var a = new GroupedList.Row("working", "x1", "X1", () => { });
        var b = new GroupedList.Row("needs_you", "x1", "X1 changed", () => { });
        check(GroupedList.Signature(new[] { a }) == "x1:working" && GroupedList.Signature(new[] { a }) != GroupedList.Signature(new[] { b }) &&
              GroupedList.Signature(new[] { a }) == GroupedList.Signature(new[] { new GroupedList.Row("working", "x1", "new text", () => { }) }),
            "A changed group redraws the list; changed text alone does not");
    }
}
