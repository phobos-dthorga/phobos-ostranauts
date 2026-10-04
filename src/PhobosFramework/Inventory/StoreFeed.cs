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

/// <summary>Feed taken from one chosen store (Framework 0.85.0; owner request, 4 October 2026: belts and bins as an
/// optional way to keep machines fed). A machine may name one ordinary unlocked container on its own ship, a Rivetline
/// material bin included, that touches it or that a conveyor belt joins to it. While the machine is started, powered
/// and short of feed, it takes what it can use from that store, one checked unit at a time, exactly as it takes feed
/// from its own inventory (<see cref="OwnInventoryFeed"/>). The store is passive and keeps no link data; the choice is
/// saved on the machine and holds across a reload. Nothing is created, and a unit the machine cannot use stays where
/// it is. Entirely optional: a machine with no store chosen behaves as before.</summary>
public static class StoreFeed
{
    /// <summary>The action prefix of the panel choice; a provider lists it in its IsConfiguration.</summary>
    public const string ActionPrefix = "feed-store:";
    /// <summary>The panel field: the stores in reach and "none", with the reason others aboard are not offered.</summary>
    public static EquipmentField Field(CondOwner machine)
    {
        string? id = Selected(machine);
        string current = id == null || id == StoreFeedRecord.None ? StoreFeedRecord.None : id;
        return new EquipmentField(Text.Get("StoreFeed.field"), id == null ? Text.Get("StoreFeed.unreadable_short") : current == StoreFeedRecord.None ? Text.Get("StoreFeed.none") : ObjectPresentation.Name(current),
            Candidates(machine).Select(s => (ActionPrefix + s.strID, ObjectPresentation.Name(s) + " [" + PortPairing.ShortId(s.strID) + "]")).Concat(new[] { (ActionPrefix + StoreFeedRecord.None, Text.Get("StoreFeed.none")) }),
            ActionPrefix + current, () => Note(machine));
    }
    /// <summary>Applies a panel choice carrying <see cref="ActionPrefix"/>. Content checks access first.</summary>
    public static bool Command(CondOwner machine, string action, out string message) => Select(machine, action.Substring(ActionPrefix.Length), out message);
    private static ObjectStateStore Saved(CondOwner machine) => new(machine.mapGUIPropMaps, StoreFeedRecord.Name, FrameworkInfo.PluginId, 1);
    /// <summary>The chosen store's id, "none" when nothing is chosen, or null for an unreadable record.</summary>
    public static string? Selected(CondOwner machine)
    {
        var status = Saved(machine).Read(out var fields);
        return status == SavedStateStatus.Missing ? StoreFeedRecord.None : status == SavedStateStatus.Ready ? StoreFeedRecord.Read(fields) : null;
    }
    private static int[] Cells(CondOwner co) => BeltNetwork.FootprintCells(co);
    /// <summary>The owner's link rule: the store touches the machine, or a conveyor belt joins them.</summary>
    public static bool Reaches(CondOwner machine, CondOwner store) => BeltNetwork.Reaches(machine, store, Cells(machine), Cells(store));
    private static bool Eligible(CondOwner machine, CondOwner? store) => store != null && store != machine && machine.ship != null && store.ship == machine.ship &&
        store.Item != null && store.HasCond("IsInstalled") && CrewWork.IsStore(store);
    /// <summary>Stores on the machine's ship it could be fed from now: touching it, or joined to it by belt.</summary>
    public static IEnumerable<CondOwner> Candidates(CondOwner machine) => machine?.ship == null ? Array.Empty<CondOwner>() :
        CrewWork.Stores(machine.ship).Where(s => Eligible(machine, s) && Reaches(machine, s)).ToArray();
    /// <summary>Stores aboard that are not offered, each with the reason, for the choice sheet; empty when none.</summary>
    public static string Note(CondOwner machine)
    {
        if (machine?.ship == null) return "";
        var lines = CrewWork.Stores(machine.ship).Where(s => Eligible(machine, s) && !Reaches(machine, s))
            .Select(s => Text.Get("LinkChoices.not_offered_line", ObjectPresentation.Name(s), Text.Get("StoreFeed.reason_reach"))).Take(8).ToArray();
        return lines.Length == 0 ? "" : Text.Get("LinkChoices.not_offered") + "\n" + string.Join("\n", lines);
    }

    /// <summary>Saves the choice ("none" clears it). The store must be one of <see cref="Candidates"/>.</summary>
    public static bool Select(CondOwner machine, string? storeId, out string message)
    {
        if (Selected(machine) == null) { message = Text.Get("StoreFeed.unreadable"); return false; }
        if (string.IsNullOrEmpty(storeId) || storeId == StoreFeedRecord.None) { Saved(machine).Clear(); message = Text.Get("StoreFeed.cleared"); return true; }
        var store = CrewWork.Resolve(storeId);
        if (!Eligible(machine, store) || !Reaches(machine, store!)) { message = Text.Get("StoreFeed.reason_reach"); return false; }
        if (!Saved(machine).TryWrite(StoreFeedRecord.Save(store!.strID))) { message = Text.Get("StoreFeed.unreadable"); return false; }
        message = Text.Get("StoreFeed.chosen", ObjectPresentation.Name(store)); return true;
    }

    /// <summary>The chosen store when the machine can draw on it now, otherwise null with the first thing to fix.</summary>
    public static CondOwner? Source(CondOwner machine, out StoreFeedProblem problem)
    {
        string? id = Selected(machine);
        var store = id == null || id == StoreFeedRecord.None ? null : CrewWork.Resolve(id);
        bool eligible = Eligible(machine, store);
        problem = StoreFeedRecord.Classify(id != StoreFeedRecord.None, id != null, store != null, eligible, eligible && Reaches(machine, store!), machine.HasCond("IsPowered"));
        return problem == StoreFeedProblem.None ? store : null;
    }
    /// <summary>One line for the machine's status: where it is fed from, or why its chosen store gives nothing; empty
    /// when no store is chosen.</summary>
    public static string Describe(CondOwner machine)
    {
        var store = Source(machine, out var problem);
        return problem switch
        {
            StoreFeedProblem.NotChosen => "",
            StoreFeedProblem.None => Text.Get("StoreFeed.status", ObjectPresentation.Name(store!)),
            StoreFeedProblem.NoPower => Text.Get("StoreFeed.status_power"),
            StoreFeedProblem.OutOfReach => Text.Get("StoreFeed.status_reach"),
            StoreFeedProblem.Unreadable => Text.Get("StoreFeed.unreadable"),
            _ => Text.Get("StoreFeed.status_missing")
        };
    }

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
