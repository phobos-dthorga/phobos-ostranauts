using System;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Framework 0.116.0, compressed in 0.117.0: the right-click Maintenance sheet shows an installed machine's
/// standing order and upkeep, each said once, then what blocks removal, with an About button for the rest.</summary>
internal static class MaintenanceSheetChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string? Opened() => null;
        MaintenanceSheet.Facts Facts(bool installed, bool orders, bool owned, string upkeep, string? blocker = null) =>
            new() { Installed = installed, Orders = orders, Owned = owned, OrderStatus = "Load feed: Waiting", Loading = "Load it by hand through its Inventory.", Upkeep = upkeep, Blocker = blocker };
        string about = Phobos.Ostranauts.Framework.Controls.ConsoleText.Get("about");
        var pipe = MaintenanceSheet.Sections(Facts(true, false, true, ""), Opened, Opened, Opened);
        check(pipe.Count == 1 && pipe[0].Heading.Length == 0 && pipe[0].Body == Text.Get("MaintenanceInfo.removal_clear") && pipe[0].Links.Single().Label == about,
            "A pipe or tank shows one line on removal and an About button");
        var blocked = MaintenanceSheet.Sections(Facts(true, false, true, "", "Empty the equipment first."), Opened, Opened, Opened);
        check(blocked.Single().Body == "Empty the equipment first.", "What blocks removal replaces the all-clear line");
        check(MaintenanceSheet.Sections(Facts(false, true, true, "Not tuned."), Opened, Opened, Opened).Count == 1, "A loose machine shows only removal");
        var x2 = MaintenanceSheet.Sections(Facts(true, false, true, "Not tuned, not inspected yet."), Opened, Opened, Opened);
        check(x2.Count == 3 && x2[0].Heading.Length == 0 && x2[0].Body == Text.Get("MaintenanceInfo.orders_none_line") + "\nLoad it by hand through its Inventory." &&
              x2[0].Links.Count == 0 && x2[1].Body == "Not tuned, not inspected yet." && x2[1].Links.Count == 1 &&
              x2[2].Heading == Text.Get("MaintenanceInfo.removal_heading") && x2[2].Links.Single().Label == about,
            "A machine without orders says so in one unheaded line with how it is loaded; upkeep in a sentence; removal last");
        var v4 = MaintenanceSheet.Sections(Facts(true, true, true, "Tuned +6%, inspected 3 h ago."), Opened, Opened, Opened);
        check(v4[0].Body == "Load feed: Waiting" && v4[0].Links.Single().Label == Text.Get("MaintenanceInfo.orders_open"), "A machine with orders shows its state and a button to them");
        var derelict = MaintenanceSheet.Sections(Facts(true, true, false, "Not tuned."), Opened, Opened, Opened);
        check(derelict[0].Body == Text.Get("MaintenanceInfo.orders_not_owned") && derelict.Take(2).All(s => s.Links.Count == 0),
            "On someone else's ship the sheet offers no buttons into the Crew panel");
        bool asked = false;
        var help = MaintenanceSheet.Sections(Facts(true, false, true, ""), Opened, Opened, () => { asked = true; return "Load a game first."; }).Last().Links.Single();
        check(help.Open() == "Load a game first." && asked, "The About button reports why the encyclopedia could not open");
        check(Text.Get("MaintenanceInfo.title") == "Maintenance", "The right-click entry is called Maintenance");
    }
}
