using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Persistence;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Why a machine's chosen feed store gives it nothing just now. Pure.</summary>
public enum StoreFeedProblem { None, NotChosen, Unreadable, Missing, NotAStore, OutOfReach, NoPower }

/// <summary>The saved choice of one feed store on a machine, with no game types so offline checks can read it.</summary>
public static class StoreFeedRecord
{
    public const string Name = "StoreFeed", Key = "PhobosState." + Name, Field = "store", None = "none";
    public static Dictionary<string, string> Save(string storeId) => new(StringComparer.Ordinal) { [Field] = storeId };
    /// <summary>The chosen store's id, or null for a record this version does not understand (kept, never overwritten).</summary>
    public static string? Read(IReadOnlyDictionary<string, string> fields) =>
        fields.Count == 1 && fields.TryGetValue(Field, out var id) && ObjectStateStore.SafeValue(id) && id.Length > 0 ? id : null;
    /// <summary>The first thing to fix, in the order a player would: the choice, the store itself, the way to it, power.</summary>
    public static StoreFeedProblem Classify(bool chosen, bool readable, bool present, bool store, bool reached, bool powered) =>
        !readable ? StoreFeedProblem.Unreadable : !chosen ? StoreFeedProblem.NotChosen : !present ? StoreFeedProblem.Missing :
        !store ? StoreFeedProblem.NotAStore : !reached ? StoreFeedProblem.OutOfReach : !powered ? StoreFeedProblem.NoPower : StoreFeedProblem.None;
}

/// <summary>One saved choice of a store on a machine, shared by the feed store (<see cref="StoreFeed"/>) and the product
/// store (<see cref="StoreDelivery"/>, Framework 0.98.0): the panel field, the saved record, the owner's link rule and
/// the status line. The two differ only in their record name, action prefix and wording.</summary>
public sealed class StoreChoice
{
    private readonly string record, text;
    /// <summary>The action prefix of the panel choice; a provider lists it in its IsConfiguration.</summary>
    public string ActionPrefix { get; }
    /// <summary>The saved record's full key, for a provider's configuration stamp.</summary>
    public string Key => "PhobosState." + record;
    public StoreChoice(string record, string actionPrefix, string textPrefix) { this.record = record; ActionPrefix = actionPrefix; text = textPrefix; }
    private string T(string key, params object[] args) => Text.Get(text + "." + key, args);
    private ObjectStateStore Saved(CondOwner machine) => new(machine.mapGUIPropMaps, record, FrameworkInfo.PluginId, 1);
    private static int[] Cells(CondOwner co) => BeltNetwork.FootprintCells(co);

    /// <summary>The panel field: the stores in reach and "none", with the reason others aboard are not offered.</summary>
    public EquipmentField Field(CondOwner machine)
    {
        string? id = Selected(machine);
        string current = id == null || id == StoreFeedRecord.None ? StoreFeedRecord.None : id;
        return new EquipmentField(T("field"), id == null ? T("unreadable_short") : current == StoreFeedRecord.None ? T("none") : ObjectPresentation.Name(current),
            Candidates(machine).Select(s => (ActionPrefix + s.strID, ObjectPresentation.Name(s) + " [" + PortPairing.ShortId(s.strID) + "]")).Concat(new[] { (ActionPrefix + StoreFeedRecord.None, T("none")) }),
            ActionPrefix + current, () => Note(machine));
    }
    /// <summary>Applies a panel choice carrying <see cref="ActionPrefix"/>. Content checks access first.</summary>
    public bool Command(CondOwner machine, string action, out string message) => Select(machine, action.Substring(ActionPrefix.Length), out message);
    /// <summary>The chosen store's id, "none" when nothing is chosen, or null for an unreadable record.</summary>
    public string? Selected(CondOwner machine)
    {
        var status = Saved(machine).Read(out var fields);
        return status == SavedStateStatus.Missing ? StoreFeedRecord.None : status == SavedStateStatus.Ready ? StoreFeedRecord.Read(fields) : null;
    }
    /// <summary>The owner's link rule: the store touches the machine, or a conveyor belt joins them.</summary>
    public static bool Reaches(CondOwner machine, CondOwner store) => BeltNetwork.Reaches(machine, store, Cells(machine), Cells(store));
    private static bool Eligible(CondOwner machine, CondOwner? store) => store != null && store != machine && machine.ship != null && store.ship == machine.ship &&
        store.Item != null && store.HasCond("IsInstalled") && CrewWork.IsStore(store);
    /// <summary>Stores on the machine's ship in reach now: touching it, or joined to it by belt.</summary>
    public static IEnumerable<CondOwner> Candidates(CondOwner machine) => machine?.ship == null ? Array.Empty<CondOwner>() :
        CrewWork.Stores(machine.ship).Where(s => Eligible(machine, s) && Reaches(machine, s)).ToArray();
    /// <summary>Stores aboard that are not offered, each with the reason, for the choice sheet; empty when none.</summary>
    public string Note(CondOwner machine)
    {
        if (machine?.ship == null) return "";
        var lines = CrewWork.Stores(machine.ship).Where(s => Eligible(machine, s) && !Reaches(machine, s))
            .Select(s => Text.Get("LinkChoices.not_offered_line", ObjectPresentation.Name(s), Text.Get("StoreFeed.reason_reach"))).Take(8).ToArray();
        return lines.Length == 0 ? "" : Text.Get("LinkChoices.not_offered") + "\n" + string.Join("\n", lines);
    }
    /// <summary>Saves the choice ("none" clears it). The store must be one of <see cref="Candidates"/>.</summary>
    public bool Select(CondOwner machine, string? storeId, out string message)
    {
        if (Selected(machine) == null) { message = T("unreadable"); return false; }
        if (string.IsNullOrEmpty(storeId) || storeId == StoreFeedRecord.None) { Saved(machine).Clear(); message = T("cleared"); return true; }
        var store = CrewWork.Resolve(storeId);
        if (!Eligible(machine, store) || !Reaches(machine, store!)) { message = Text.Get("StoreFeed.reason_reach"); return false; }
        if (!Saved(machine).TryWrite(StoreFeedRecord.Save(store!.strID))) { message = T("unreadable"); return false; }
        message = T("chosen", ObjectPresentation.Name(store)); return true;
    }
    /// <summary>The chosen store when the machine can use it now, otherwise null with the first thing to fix.</summary>
    public CondOwner? Store(CondOwner machine, out StoreFeedProblem problem)
    {
        string? id = Selected(machine);
        var store = id == null || id == StoreFeedRecord.None ? null : CrewWork.Resolve(id);
        bool eligible = Eligible(machine, store);
        problem = StoreFeedRecord.Classify(id != StoreFeedRecord.None, id != null, store != null, eligible, eligible && Reaches(machine, store!), machine.HasCond("IsPowered"));
        return problem == StoreFeedProblem.None ? store : null;
    }
    /// <summary>One line for the machine's status: the store in use, or why the chosen store is not; empty when no
    /// store is chosen.</summary>
    public string Describe(CondOwner machine)
    {
        var store = Store(machine, out var problem);
        return problem switch
        {
            StoreFeedProblem.NotChosen => "",
            StoreFeedProblem.None => T("status", ObjectPresentation.Name(store!)),
            StoreFeedProblem.NoPower => T("status_power"),
            StoreFeedProblem.OutOfReach => T("status_reach"),
            StoreFeedProblem.Unreadable => T("unreadable"),
            _ => T("status_missing")
        };
    }
}

/// <summary>Feed taken from one chosen store (Framework 0.85.0; owner request, 4 October 2026: belts and bins as an
/// optional way to keep machines fed). A machine may name one ordinary unlocked container on its own ship, a Rivetline
/// material bin included, that touches it or that a conveyor belt joins to it. While the machine is started, powered
/// and short of feed, it takes what it can use from that store, one checked unit at a time, exactly as it takes feed
/// from its own inventory (<see cref="OwnInventoryFeed"/>). The store is passive and keeps no link data; the choice is
/// saved on the machine and holds across a reload. Nothing is created, and a unit the machine cannot use stays where
/// it is. Entirely optional: a machine with no store chosen behaves as before.</summary>
public static class StoreFeed
{
    private static readonly StoreChoice Choice = new(StoreFeedRecord.Name, "feed-store:", "StoreFeed");
    /// <summary>The action prefix of the panel choice; a provider lists it in its IsConfiguration.</summary>
    public const string ActionPrefix = "feed-store:";
    public static EquipmentField Field(CondOwner machine) => Choice.Field(machine);
    public static bool Command(CondOwner machine, string action, out string message) => Choice.Command(machine, action, out message);
    public static string? Selected(CondOwner machine) => Choice.Selected(machine);
    public static bool Reaches(CondOwner machine, CondOwner store) => StoreChoice.Reaches(machine, store);
    public static IEnumerable<CondOwner> Candidates(CondOwner machine) => StoreChoice.Candidates(machine);
    public static string Note(CondOwner machine) => Choice.Note(machine);
    public static bool Select(CondOwner machine, string? storeId, out string message) => Choice.Select(machine, storeId, out message);
    /// <summary>The chosen store when the machine can draw on it now, otherwise null with the first thing to fix.</summary>
    public static CondOwner? Source(CondOwner machine, out StoreFeedProblem problem) => Choice.Store(machine, out problem);
    public static string Describe(CondOwner machine) => Choice.Describe(machine);

    /// <summary>The units in the chosen store that <paramref name="accept"/> admits as single units; empty when no
    /// store is chosen or it cannot be drawn on now.</summary>
    public static List<CondOwner> Units(CondOwner machine, Func<CondOwner, bool> accept)
    {
        var store = Source(machine, out _);
        return store == null ? new List<CondOwner>() : OwnInventoryFeed.Units(store, accept);
    }
    /// <summary>With an empty feed compartment, takes the first unit in the chosen store that the compartment admits.
    /// True when a unit was taken.</summary>
    public static bool TopUp(CondOwner machine, CondOwner? feed)
    {
        var contents = feed?.objContainer?.ContainedCOs;
        if (machine == null || contents == null || contents.Count > 0) return false;
        var store = Source(machine, out _);
        if (store?.objContainer == null || store.objContainer.ContainedCOs.Count == 0) return false;
        foreach (var unit in OwnInventoryFeed.Units(store, u => UnitItemTransfer.Fits(feed!, u, out _)))
            if (OwnInventoryFeed.Take(unit, feed!)) return true;
        return false;
    }
}

/// <summary>Products sent to one chosen store (Framework 0.98.0; owner question, 5 October 2026: belts reassessed for
/// the regolith programme). The mirror of <see cref="StoreFeed"/>: a machine may name one ordinary unlocked container
/// on its own ship that touches it or that a conveyor belt joins to it, and while the machine is started and powered
/// its owner moves finished products there from its tray, one checked unit at a time. A store that is full, locked,
/// gone or out of reach leaves the products in the tray and the status says why. Nothing is created or discarded, and
/// content decides what counts as a product. Entirely optional: a machine with no store chosen keeps its products.</summary>
public static class StoreDelivery
{
    /// <summary>The saved record's name; the feed store's record format is reused.</summary>
    public const string Record = "StoreDelivery", Key = "PhobosState." + Record;
    /// <summary>The action prefix of the panel choice; a provider lists it in its IsConfiguration.</summary>
    public const string ActionPrefix = "product-store:";
    private static readonly StoreChoice Choice = new(Record, ActionPrefix, "StoreDelivery");
    public static EquipmentField Field(CondOwner machine) => Choice.Field(machine);
    public static bool Command(CondOwner machine, string action, out string message) => Choice.Command(machine, action, out message);
    public static string? Selected(CondOwner machine) => Choice.Selected(machine);
    /// <summary>The chosen store when the machine can send to it now, otherwise null with the first thing to fix.</summary>
    public static CondOwner? Destination(CondOwner machine, out StoreFeedProblem problem) => Choice.Store(machine, out problem);
    public static string Describe(CondOwner machine) => Choice.Describe(machine);

    /// <summary>Moves one unit that <paramref name="isProduct"/> admits from the machine's own inventory into the chosen
    /// store, when the store takes it. True when a unit moved. The unit is the same physical item throughout; a unit of
    /// a stack leaves the rest of its stack in the tray.</summary>
    public static bool SendOne(CondOwner machine, Func<CondOwner, bool> isProduct)
    {
        if (machine?.objContainer == null || machine.objContainer.ContainedCOs.Count == 0) return false;
        var store = Destination(machine, out _);
        if (store?.objContainer == null) return false;
        foreach (var unit in OwnInventoryFeed.Units(machine, isProduct))
        {
            if (!UnitItemTransfer.Fits(store, unit, out _)) continue;
            if (OwnInventoryFeed.Take(unit, store)) return true;
        }
        return false;
    }
}
