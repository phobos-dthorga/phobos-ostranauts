using System;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Framework 0.116.0: the right-click Maintenance sheet (owner direction, 6 October 2026) shows an installed
/// machine's standing orders and upkeep, with buttons into the Crew panel, then removal and repair for everything.</summary>
internal static class MaintenanceSheetChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string? Opened() => null;
        MaintenanceSheet.Facts Facts(bool installed, bool orders, bool owned, string upkeep) =>
            new() { Installed = installed, Orders = orders, Owned = owned, OrderStatus = "Load feed: Waiting", Upkeep = upkeep, Removal = "Empty it first." };
        var pipe = MaintenanceSheet.Sections(Facts(true, false, true, ""), Opened, Opened);
        check(pipe.Count == 1 && pipe[0].Heading.Length == 0 && pipe[0].Body == "Empty it first." && pipe[0].Links.Count == 0,
            "A pipe or tank with no orders or upkeep shows only its removal notes, as before");
        var loose = MaintenanceSheet.Sections(Facts(false, true, true, "Tune: none."), Opened, Opened);
        check(loose.Count == 1, "A loose machine shows only removal notes: orders and upkeep belong to installed machines");
        var tuned = MaintenanceSheet.Sections(Facts(true, false, true, "Tune: none."), Opened, Opened);
        check(tuned.Count == 3 && tuned[0].Body == Text.Get("MaintenanceInfo.orders_none") && tuned[0].Links.Count == 0 &&
              tuned[1].Body == "Tune: none." && tuned[1].Links.Count == 1 && tuned[2].Heading == Text.Get("MaintenanceInfo.removal_heading"),
            "A machine with upkeep but no orders says it takes none, and offers the upkeep switches");
        var ordered = MaintenanceSheet.Sections(Facts(true, true, true, "Tune: none."), Opened, Opened);
        check(ordered[0].Body == "Load feed: Waiting" && ordered[0].Links.Single().Label == Text.Get("MaintenanceInfo.orders_open"),
            "A machine with orders shows the order's state and a button to its standing orders");
        var derelict = MaintenanceSheet.Sections(Facts(true, true, false, "Tune: none."), Opened, Opened);
        check(derelict[0].Body == Text.Get("MaintenanceInfo.orders_not_owned") && derelict.All(s => s.Links.Count == 0),
            "On someone else's ship the sheet says orders are for your own ship and offers no buttons");
        bool asked = false;
        var link = MaintenanceSheet.Sections(Facts(true, true, true, ""), () => { asked = true; return "Select a crew member first."; }, Opened)[0].Links[0];
        check(link.Open() == "Select a crew member first." && asked, "A button reports why it could not open the Crew panel");
        check(Text.Get("MaintenanceInfo.title") == "Maintenance", "The right-click entry is called Maintenance");
    }
}
