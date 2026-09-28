using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Additive, idempotent, in-place changes to definitions the game or another mod owns,
/// the same way the game's own Installables.Create appends to them. Never republish a native
/// definition by name after loading: the game keeps private state on the original object
/// (for example job actions) that a clone cannot carry.</summary>
public static class DefinitionAmendments
{
    /// <summary>Appends missing action names to a definition's action list.</summary>
    public static bool AppendInteractions(JsonCondOwner definition, params string[] names)
    {
        var current = definition.aInteractions ?? Array.Empty<string>();
        var missing = Missing(current, names);
        if (missing.Length == 0) return false;
        definition.aInteractions = current.Concat(missing).ToArray();
        return true;
    }

    /// <summary>Appends missing action names to a live object's own list, which the game copied
    /// from its definition when the object was created, and refreshes the game's action flags.</summary>
    public static bool AppendInteractions(CondOwner co, params string[] names)
    {
        co.aInteractions ??= new List<string>();
        var missing = Missing(co.aInteractions, names);
        if (missing.Length == 0) return false;
        co.aInteractions.AddRange(missing);
        co.CheckInteractionFlag();
        return true;
    }

    /// <summary>Inserts a reply ("Name,[us],[them]") into an interaction's reply list once, before the
    /// first reply whose name satisfies <paramref name="before"/>, or at the end.</summary>
    public static bool InsertInverse(JsonInteraction interaction, string entry, Func<string, bool>? before = null)
    {
        var list = (interaction.aInverse ?? Array.Empty<string>()).ToList();
        if (list.Any(e => ReplyName(e) == ReplyName(entry))) return false;
        int at = before == null ? -1 : list.FindIndex(e => before(ReplyName(e)));
        if (at < 0) list.Add(entry); else list.Insert(at, entry);
        interaction.aInverse = list.ToArray();
        return true;
    }

    /// <summary>Appends missing entries to a loot table's own list.</summary>
    public static bool AppendLoot(Loot loot, params string[] entries)
    {
        var current = loot.aCOs ?? Array.Empty<string>();
        var missing = Missing(current, entries);
        if (missing.Length == 0) return false;
        loot.aCOs = current.Concat(missing).ToArray();
        return true;
    }

    public static string ReplyName(string reply)
    {
        int comma = reply.IndexOf(',');
        return comma < 0 ? reply : reply.Substring(0, comma);
    }

    private static string[] Missing(IEnumerable<string> current, IEnumerable<string> wanted)
    {
        var have = new HashSet<string>(current, StringComparer.Ordinal);
        return wanted.Where(n => !string.IsNullOrEmpty(n) && have.Add(n)).ToArray();
    }
}
