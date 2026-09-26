using System;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>Resolve loaded workers through the same company roster used by native time-skip.</summary>
internal static class CrewRoster
{
    internal static CondOwner[] Members() => Members(CrewSim.coPlayer?.Company);

    internal static bool AllAboard(Ship ship) => AllAboard(CrewSim.coPlayer?.Company, ship);

    internal static bool AllAboard(JsonCompany? company, Ship ship)
    {
        if (ship == null || company?.mapRoster == null || company.mapRoster.Count == 0 || DataHandler.mapCOs == null) return false;
        var members = company.GetCrewMembers();
        // Unlike work discovery, departure cannot ignore an unresolved or unloaded member.
        return members.Count == company.mapRoster.Count && members.All(c => c != null && !c.bDestroyed && c.ship == ship);
    }

    internal static CondOwner[] Members(JsonCompany? company)
    {
        // The legacy CrewSim.aCrew field is not populated by the current game.
        // Missing saved NPCs and partially loaded/removed members must not block the others.
        if (company?.mapRoster == null || DataHandler.mapCOs == null) return Array.Empty<CondOwner>();
        return company.GetCrewMembers().Where(c => c != null && !c.bDestroyed && c.ship != null && c.aQueue != null).Distinct().ToArray();
    }
}
