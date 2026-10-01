using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Framework 0.70.0 inventory roles (owner direction, 1 October 2026: size every inventory to its job): every
/// Phobos definition with a container states what it is for, its grid is the declared one, hidden roles offer no
/// Inventory action, and no machine's tray is larger than declared storage of the same footprint.</summary>
internal static class InventoryNativeChecks
{
    internal static void Run(IEnumerable<NativeDefinitions> definitions, Action<bool, string> check)
    {
        var sets = definitions.ToArray();
        int containers = 0;
        var roomy = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var co in sets.SelectMany(d => d.Objects.Values).OrderBy(c => c.strName, StringComparer.Ordinal))
        {
            var spec = EquipmentInventory.Of(co.strName);
            if (co.strContainerCT == null)
            {
                check(spec == null || spec.Value.Role == InventoryRole.None, "A definition without a container declares no inventory: " + co.strName);
                continue;
            }
            containers++;
            check(spec != null, "Every container says what it is for: " + co.strName);
            if (spec == null) continue;
            var s = spec.Value;
            check(s.Role != InventoryRole.None && co.nContainerWidth == s.Width && co.nContainerHeight == s.Height && co.strContainerCT == s.Trigger,
                "A container's grid and trigger are the declared ones: " + co.strName);
            bool inventory = (co.aInteractions ?? Array.Empty<string>()).Contains("Inventory");
            bool system = (co.aStartingConds ?? Array.Empty<string>()).Any(c => c.StartsWith("IsSystem=", StringComparison.Ordinal));
            if (s.Role == InventoryRole.Feed) check(system && !inventory, "A feed is a hidden system bin: " + co.strName);
            else if (s.Role == InventoryRole.LegacyReceptacle)
                check(!inventory && !(co.aStartingConds ?? Array.Empty<string>()).Any(c => c.StartsWith("IsContainer=", StringComparison.Ordinal)),
                    "A legacy receptacle is hidden and is no store: " + co.strName);
            else check(inventory, "A tray, rack or store opens through the ordinary Inventory action: " + co.strName);
            // Declared storage is two cells a tile side (the material bins); nothing else may be roomier than that.
            int side = Math.Max(1, co.inventoryWidth), limit = 4 * side * Math.Max(1, co.inventoryHeight);
            if ((s.Role == InventoryRole.ProductTray || s.Role == InventoryRole.ServiceRack) && s.Cells > Math.Max(limit, 4)) roomy.Add(Family(co.strName));
        }
        check(containers >= 60, "The inventory roles cover the equipment of every mod (" + containers + " containers)");
        check(roomy.SetEquals(Oversized), "Only the listed families still have a tray or rack roomier than a storage bin of their footprint: " + string.Join(", ", roomy));
    }
    private static string Family(string id)
    {
        foreach (string form in new[] { "InstalledDmg", "LooseDmg", "Installed", "Loose" })
            if (id.EndsWith(form, StringComparison.Ordinal)) return id.Substring(0, id.Length - form.Length);
        return id;
    }
    /// <summary>Families still at the general 8 x 8 grid of earlier versions although it is roomier than storage of their
    /// footprint, to be sized in turn. A family leaves this list when it is given its fitted size; none may be added.</summary>
    private static readonly HashSet<string> Oversized = new(StringComparer.Ordinal)
    {
        "PhobosAcidPlant", "PhobosCabinAirRegulator", "PhobosLeachUnit", "PhobosProcessSilo", "PhobosProcessSiloCompact",
        "PhobosVerdemorrowGroundworkB2", "PhobosVerdemorrowGroundworkE2", "PhobosVerdemorrowGroundworkE2Medium", "PhobosVerdemorrowGroundworkR3",
        "PhobosVerdemorrowGroundworkW2", "PhobosVerdemorrowHearth2"
    };
}
