using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Trading;

/// <summary>One line of what the player owns through a mod, for another mod's overview: what it is, what it is worth now,
/// and the PDA app that shows it.</summary>
public sealed class HoldingLine
{
    /// <summary>The providing mod's plugin id.</summary>
    public string Owner = "";
    /// <summary>What it is, in the player's language (for example, shares).</summary>
    public string Label = "";
    /// <summary>What it would fetch now, in credits.</summary>
    public double Value;
    /// <summary>One short line of detail, or empty.</summary>
    public string Detail = "";
    /// <summary>The PDA app that shows it (<see cref="Pda.PdaApps"/>), or empty.</summary>
    public string App = "";
}

/// <summary>What the player owns through Phobos mods, gathered for overviews (Framework 0.128.0): Phobos Exchange
/// registers its shares, and Phobos Banking lists every registered line on its overview. Neither mod needs the other,
/// and load order does not matter. Read-only: providers report what they hold; nothing here changes it.</summary>
public static class PlayerHoldings
{
    private static readonly Dictionary<string, Func<HoldingLine?>> providers = new(StringComparer.Ordinal);

    /// <summary>Registers (or replaces) a mod's provider. It returns null when the player holds nothing through it.</summary>
    public static void Register(string owner, Func<HoldingLine?> read)
    {
        if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("A holdings provider needs its owner.", nameof(owner));
        providers[owner] = read ?? throw new ArgumentNullException(nameof(read));
    }

    public static void Unregister(string owner) => providers.Remove(owner ?? "");

    /// <summary>Every provider's line, in owner order. A provider that fails is left out and logged.</summary>
    public static IReadOnlyList<HoldingLine> Read()
    {
        var lines = new List<HoldingLine>();
        var owners = new List<string>(providers.Keys);
        owners.Sort(StringComparer.Ordinal);
        foreach (var owner in owners)
        {
            try
            {
                var line = providers[owner]();
                if (line == null) continue;
                line.Owner = owner;
                lines.Add(line);
            }
            catch (Exception e) { FrameworkLifecycle.Log(Text.Get("Holdings.failed", owner, e.Message)); }
        }
        return lines;
    }
}
