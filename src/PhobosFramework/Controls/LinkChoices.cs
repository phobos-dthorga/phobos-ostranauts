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
