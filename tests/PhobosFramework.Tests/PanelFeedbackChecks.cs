using System;
using Phobos.Ostranauts.Framework.Controls;

internal static class PanelFeedbackChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string status = "Collector: paired\nReceiving paused\nCargo retained";
        check(PanelFeedback.Additional(true, status, status) == "", "Successful full status is rendered only once");
        check(PanelFeedback.Additional(true, "Receiving paused\nCargo retained", status) == "", "Complete echoed paragraphs are omitted");
        check(PanelFeedback.Additional(true, "Receiving paused", status) == "", "Furnace-style notice already in live status is omitted");
        check(PanelFeedback.Additional(true, status.Replace("\n", "\r\n"), status) == "", "Native and catalog newline conventions agree");
        check(PanelFeedback.Additional(false, status, status) == status, "Rejected command explanations remain explicit even when status matches");
        check(PanelFeedback.Additional(true, "Work queued", status) == "Work queued", "Distinct queued-work notice remains visible");
        check(PanelFeedback.Additional(true, "paused", status) == "paused", "Substring coincidence cannot hide a response");
        check(PanelFeedback.Additional(true, "Other collector paired", status) == "Other collector paired", "Feedback about another endpoint remains visible");
        check(PanelFeedback.Additional(true, "", status) == "", "Empty response adds no placeholder");
    }
}
