using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>The right-click Maintenance sheet (named "Maintenance information" before Framework 0.116.0). It explains
/// removal and repair without granting work permission or changing equipment, and since 0.116.0 it is also where a
/// machine's standing orders and upkeep are found (owner direction, 6 October 2026): their state, with buttons into the
/// Crew panel. The interaction ids each mod registers are unchanged, so saves and other mods are unaffected.</summary>
public static class MaintenanceInformation
{
    public static void Register(NativeDefinitions definitions, string action, Func<CondOwner, string>? extra = null)
    {
        var ids = definitions.Installables.Values.Where(j => j.strJobType != "install")
            .Select(j => j.strActionCO).Where(definitions.Objects.ContainsKey).Distinct().ToArray();
        ItemInformation.Register(definitions, action, Text.Get("MaintenanceInfo.title"), ids, co => MaintenanceSheet.Sections(Facts(co, extra),
            () => Crew.CrewPanel.Unavailable(co) ?? (Crew.CrewPanel.Show(co, Crew.CrewPanel.OrdersView) ? null : Crew.CrewWork.Message("panel_blocked")),
            () => Crew.CrewPanel.Unavailable(co) ?? (Crew.CrewPanel.Show(co, Crew.CrewPanel.UpkeepView) ? null : Crew.CrewWork.Message("panel_blocked"))));
    }
    /// <summary>What the sheet says about one object, read from the services that own each part.</summary>
    private static MaintenanceSheet.Facts Facts(CondOwner co, Func<CondOwner, string>? extra)
    {
        string? bin = MaintenanceSafety.Actions.TryGetValue("ACT" + co.strCODef + "Dismantle", out var internalBin) ? internalBin : null;
        string removal = Text.Get("MaintenanceInfo.basics") + "\n\n" + (MaintenanceSafety.Reason(co, bin) ?? Text.Get("MaintenanceInfo.empty"));
        string more = extra == null ? "" : extra(co);
        if (more.Length > 0) removal += "\n\n" + more;
        bool orders = Crew.CrewWork.Provider(co) != null;
        string status = "";
        if (orders)
        {
            var s = Crew.CrewWork.ReadStatus(co);
            status = Text.Get("MaintenanceInfo.orders_status", s.Work, s.Label, s.Worker) + (s.Detail.Length > 0 ? "\n" + s.Detail : "");
        }
        return new MaintenanceSheet.Facts
        {
            Installed = co.HasCond("IsInstalled"), Orders = orders, Owned = Crew.CrewWork.CanManage(co), OrderStatus = status,
            Upkeep = Crew.Upkeep.MachineReport(co), Removal = removal
        };
    }
}

/// <summary>The Maintenance sheet's sections from plain facts (Framework 0.116.0), so the offline checks can read
/// them: standing orders and upkeep for an installed machine that has either, then removal and repair for everything.</summary>
public static class MaintenanceSheet
{
    public sealed class Facts
    {
        public bool Installed, Orders, Owned;
        public string OrderStatus = "", Upkeep = "", Removal = "";
    }
    public static IReadOnlyList<InformationSection> Sections(Facts facts, Func<string?> openOrders, Func<string?> openUpkeep)
    {
        if (facts == null) throw new ArgumentNullException(nameof(facts));
        var sections = new List<InformationSection>();
        bool machine = facts.Installed && (facts.Orders || facts.Upkeep.Length > 0);
        if (machine)
        {
            string orders = Text.Get("MaintenanceInfo.orders_heading");
            if (!facts.Orders) sections.Add(new InformationSection(orders, Text.Get("MaintenanceInfo.orders_none")));
            else if (!facts.Owned) sections.Add(new InformationSection(orders, Text.Get("MaintenanceInfo.orders_not_owned")));
            else sections.Add(new InformationSection(orders, facts.OrderStatus, new InformationLink(Text.Get("MaintenanceInfo.orders_open"), openOrders)));
        }
        if (machine && facts.Upkeep.Length > 0)
            sections.Add(facts.Owned
                ? new InformationSection(Text.Get("MaintenanceInfo.upkeep_heading"), facts.Upkeep, new InformationLink(Text.Get("MaintenanceInfo.upkeep_open"), openUpkeep))
                : new InformationSection(Text.Get("MaintenanceInfo.upkeep_heading"), facts.Upkeep));
        sections.Add(new InformationSection(machine ? Text.Get("MaintenanceInfo.removal_heading") : "", facts.Removal));
        return sections;
    }
}
