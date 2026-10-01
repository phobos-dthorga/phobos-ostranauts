using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>What an inventory on a piece of equipment is for (Framework 0.70.0; owner direction, 1 October 2026:
/// size every inventory to its job). A product tray holds about one to two batches of the machine's largest recipe;
/// a service rack is the few cells a vessel needs for hand work (a charge, a drain canister); storage is declared
/// storage (the material bins); a feed is a hidden internal bin; a legacy receptacle is a hidden grid that takes
/// nothing new and only keeps saved contents attached until they are put on the deck.</summary>
public enum InventoryRole { None, ProductTray, ServiceRack, Storage, Feed, LegacyReceptacle }

/// <summary>One declared inventory: its role, its grid in cells and the native trigger that admits cargo.</summary>
public readonly struct InventorySpec
{
    public InventoryRole Role { get; }
    public int Width { get; }
    public int Height { get; }
    public string? Trigger { get; }
    public int Cells => Width * Height;
    private InventorySpec(InventoryRole role, int width, int height, string? trigger)
    {
        if (role == InventoryRole.None ? width != 0 || height != 0 || trigger != null : width < 1 || height < 1 || string.IsNullOrWhiteSpace(trigger))
            throw new ArgumentException("An inventory needs a grid of at least one cell and a trigger; none has neither.");
        Role = role; Width = width; Height = height; Trigger = trigger;
    }
    /// <summary>No inventory at all. Only for equipment that never had one in a save; equipment that did uses
    /// <see cref="LegacyReceptacle"/>, because the game leaves saved contents of a removed container unattached.</summary>
    public static InventorySpec None => new(InventoryRole.None, 0, 0, null);
    public static InventorySpec ProductTray(int width, int height, string trigger = EquipmentInventory.Solid) => new(InventoryRole.ProductTray, width, height, trigger);
    public static InventorySpec ServiceRack(int width, int height, string trigger = EquipmentInventory.Solid) => new(InventoryRole.ServiceRack, width, height, trigger);
    public static InventorySpec Storage(int width, int height, string trigger) => new(InventoryRole.Storage, width, height, trigger);
    public static InventorySpec Feed(int width, int height, string trigger) => new(InventoryRole.Feed, width, height, trigger);
    /// <summary>A hidden grid of the size the equipment had before, admitting nothing: saved contents load attached
    /// and in view of <see cref="Persistence.ContainerFit"/>, which puts them on the deck.</summary>
    public static InventorySpec LegacyReceptacle(int width, int height, string trigger = EquipmentInventory.NoNewCargo) => new(InventoryRole.LegacyReceptacle, width, height, trigger);
    /// <summary>The same role and trigger with another grid.</summary>
    public InventorySpec Sized(int width, int height) => new(Role, width, height, Trigger);
}

/// <summary>Applies and records declared inventories, so content states intent instead of grid literals and offline
/// checks can require a role on every Phobos container. Declarations are per definition id and are rebuilt on every
/// content load. Changing a declared size needs no migration entry: <see cref="Persistence.ContainerFit"/> re-packs
/// saved contents against the live grid when a save loads.</summary>
public static class EquipmentInventory
{
    /// <summary>The game's ordinary solid-cargo container trigger.</summary>
    public const string Solid = "TIsFitContainerSolid";
    /// <summary>Framework's trigger that no object satisfies, published with Framework's own definitions.</summary>
    public const string NoNewCargo = "PhobosFrameworkNoNewCargo";
    private static readonly Dictionary<string, InventorySpec> declared = new(StringComparer.Ordinal);
    internal static void Reset() => declared.Clear();

    /// <summary>The trigger behind <see cref="NoNewCargo"/>: it requires and forbids the same condition.</summary>
    internal static void AddTrigger(NativeDefinitions d) =>
        d.Triggers[NoNewCargo] = new CondTrigger { strName = NoNewCargo, fChance = 1, fCount = 1, bAND = true,
            aReqs = new[] { "IsSolid" }, aForbids = new[] { "IsSolid" }, aTriggers = Array.Empty<string>() };

    /// <summary>Sets the family's inventory on all four forms and records it. A tray, rack or storage keeps the
    /// ordinary Inventory action; a legacy receptacle and no inventory remove it.</summary>
    public static void Apply(NativeDefinitions d, string prefix, InventorySpec spec)
    {
        foreach (string form in MachineFamilies.Forms)
        {
            if (!d.Objects.TryGetValue(prefix + form, out var co)) throw new ArgumentException("Unknown equipment form: " + prefix + form);
            Set(co, spec);
            declared[co.strName] = spec;
        }
    }
    /// <summary>Records a container whose access the content builds by hand (a hidden feed bin, a family with its own
    /// recovery action): sets only its trigger and grid on that one definition, and leaves actions and flags alone.</summary>
    public static void Declare(JsonCondOwner co, InventorySpec spec)
    {
        if (co == null) throw new ArgumentNullException(nameof(co));
        co.strContainerCT = spec.Trigger; co.nContainerWidth = spec.Width; co.nContainerHeight = spec.Height;
        declared[co.strName] = spec;
    }
    // A family that already has what its role needs is left exactly as it was, so declaring a role alone changes no
    // exported definition.
    private static void Set(JsonCondOwner co, InventorySpec spec)
    {
        bool visible = spec.Role != InventoryRole.None && spec.Role != InventoryRole.LegacyReceptacle;
        var conds = co.aStartingConds ?? Array.Empty<string>();
        bool flagged = conds.Any(s => s.StartsWith("IsContainer=", StringComparison.Ordinal));
        if (visible && !flagged) co.aStartingConds = conds.Concat(new[] { "IsContainer=1x1" }).ToArray();
        // The game builds the container from the trigger, not the flag; a hidden receptacle drops the flag so nothing
        // treats it as a store (the F6-R and F6-P precedent).
        else if (!visible && flagged) co.aStartingConds = conds.Where(s => !s.StartsWith("IsContainer=", StringComparison.Ordinal)).ToArray();
        var actions = co.aInteractions ?? Array.Empty<string>();
        if (visible && !actions.Contains("Inventory")) co.aInteractions = new[] { "Inventory" }.Concat(actions).ToArray();
        else if (!visible && actions.Contains("Inventory")) co.aInteractions = actions.Where(a => a != "Inventory").ToArray();
        co.strContainerCT = spec.Trigger; co.nContainerWidth = spec.Width; co.nContainerHeight = spec.Height;
        if (visible) co.mapGUIPropMaps = new[] { "GUIInv", "Inventory" };
        else if (spec.Role == InventoryRole.None) co.mapGUIPropMaps = Array.Empty<string>();
        if (spec.Role == InventoryRole.None) { co.strLoot = "Blank"; co.aSlotsWeHave = Array.Empty<string>(); }
    }
    /// <summary>The declared inventory of a definition, or null when none was declared.</summary>
    public static InventorySpec? Of(string? definition) => definition != null && declared.TryGetValue(definition, out var spec) ? spec : null;
    public static IReadOnlyCollection<string> Declared => declared.Keys;
}
