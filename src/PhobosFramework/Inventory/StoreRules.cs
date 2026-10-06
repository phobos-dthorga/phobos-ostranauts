using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Which of the game's containers count as stores, read from Framework's <c>stores</c> data pack
/// (<c>framework/stores.json</c>, Framework 0.116.0) with player and add-on files over it. A weapon's magazine, a
/// charger's battery slot, a scrubber's filter holder or a toilet has a native container, but nobody keeps feed in it:
/// store pickers, crew-order stores and housekeeping leave such things out (<see cref="Crew.CrewWork.IsStore"/>), and
/// crew hauling never takes from them. The decision itself has no game types, so offline checks run it on the shipped
/// rules; the game asks once per definition and remembers the answer until the packs are reread.</summary>
public static class StoreRules
{
    public const string ModFolder = "PhobosFramework", Resource = "PhobosFramework.stores.json";
    private static StorePack? pack;
    private static readonly Dictionary<string, bool> notStore = new(StringComparer.Ordinal), sealedSocket = new(StringComparer.Ordinal);
    public static StorePack Pack => pack ??= Load();
    public static DataPackSource Source => new(FrameworkInfo.PluginId, ModFolder, StoreSchema.Name, typeof(StoreRules).Assembly, Resource);
    public static StorePack Load()
    {
        pack = DataPacks.Load<StorePack>(Source, StoreSchema.Validate);
        notStore.Clear(); sealedSocket.Clear();
        return pack;
    }

    /// <summary>A container that holds only one kind of thing, or an object marked as not a store: a weapon, a charger,
    /// a filter holder. Crew never take from these, whatever the order.</summary>
    public static bool Sealed(StorePack rule, Func<string, bool> has, string? containerRule)
    {
        if (rule == null || has == null) return false;
        foreach (string condition in rule.excludeConditions) if (has(condition)) return true;
        if (string.IsNullOrEmpty(containerRule)) return false;
        foreach (string name in rule.excludeContainers)
            if (name.EndsWith("*", StringComparison.Ordinal) ? containerRule!.StartsWith(name.Substring(0, name.Length - 1), StringComparison.Ordinal) :
                string.Equals(containerRule, name, StringComparison.Ordinal)) return true;
        return false;
    }
    /// <summary>The whole decision, pure: not a store when <see cref="Sealed"/>, or when it lacks the interaction the
    /// pack requires (a container the player cannot open from its menu, such as the toilet or the coffee machine).</summary>
    public static bool Excluded(StorePack rule, Func<string, bool> has, IEnumerable<string>? interactions, string? containerRule)
    {
        if (Sealed(rule, has, containerRule)) return true;
        if (rule == null || rule.requireInteraction.Length == 0 || interactions == null) return false;
        foreach (string interaction in interactions) if (interaction == rule.requireInteraction) return false;
        return true;
    }

    /// <summary>Whether a root object is not a store under the loaded rules, judged from its definition.</summary>
    public static bool NotAStore(CondOwner co) => co != null && Remember(notStore, co, d => Excluded(Pack, Has(d), d.aInteractions, d.strContainerCT));
    /// <summary>Whether crew must never take from inside this object (<see cref="Sealed"/>), judged from its definition.</summary>
    public static bool SealedContainer(CondOwner co) => co != null && Remember(sealedSocket, co, d => Sealed(Pack, Has(d), d.strContainerCT));

    private static bool Remember(Dictionary<string, bool> verdicts, CondOwner co, Func<JsonCondOwner, bool> judge)
    {
        string id = co.strCODef ?? "";
        if (verdicts.TryGetValue(id, out bool known)) return known;
        // An object with no definition (none in the game today) is judged by its live conditions only.
        bool verdict = DataHandler.dictCOs != null && DataHandler.dictCOs.TryGetValue(id, out var definition) && definition != null
            ? judge(definition) : Sealed(Pack, co.HasCond, null);
        verdicts[id] = verdict;
        return verdict;
    }
    private static Func<string, bool> Has(JsonCondOwner definition)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string entry in definition.aStartingConds ?? Array.Empty<string>())
        {
            int cut = entry.IndexOf('=');
            names.Add(cut < 0 ? entry : entry.Substring(0, cut));
        }
        return names.Contains;
    }
}
