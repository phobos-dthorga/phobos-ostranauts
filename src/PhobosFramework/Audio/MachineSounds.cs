using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Audio;

/// <summary>Looping work sounds for Phobos machines (Framework 0.119.0; owner request, 6 October 2026: every machine
/// sounds while it works, each with its own loop, at the game's own level). A content mod registers each installed
/// machine definition with the loop and pitch it hard-codes; Framework owns the clips, the player, the mix and the
/// lifetime. The default working test is the machine's own power override: the condition that switches it to its
/// working draw, while it is powered. A machine whose work is not shown that way passes its own test, read from its
/// service and never changing anything. Sound is presentation only: it never changes work, power or saved records.</summary>
public static class MachineSounds
{
    public sealed class Entry
    {
        public string Definition { get; }
        public MachineLoop Loop { get; }
        public float Pitch { get; }
        internal Func<CondOwner, bool>? Working { get; }
        internal Entry(string definition, MachineLoop loop, float pitch, Func<CondOwner, bool>? working)
        { Definition = definition; Loop = loop; Pitch = pitch; Working = working; }
    }
    private static readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string?> overrides = new(StringComparer.Ordinal);
    public static IReadOnlyDictionary<string, Entry> Entries => entries;

    /// <summary>Gives an installed machine definition its loop. Registering a definition again replaces its entry, so a
    /// mod may call its table more than once. <paramref name="working"/> is only needed when the machine's power
    /// override does not show its work.</summary>
    public static void Register(string installedDefinition, MachineLoop loop, float pitch, Func<CondOwner, bool>? working = null)
    {
        if (string.IsNullOrEmpty(installedDefinition)) throw new ArgumentException("A machine sound needs a definition id.", nameof(installedDefinition));
        if (!(pitch >= MachineSoundRules.MinPitch && pitch <= MachineSoundRules.MaxPitch)) throw new ArgumentOutOfRangeException(nameof(pitch));
        entries[installedDefinition] = new Entry(installedDefinition, loop, pitch, working);
    }
    public static void Unregister(string installedDefinition) { if (installedDefinition != null) entries.Remove(installedDefinition); }
    public static bool Has(string? definition) => definition != null && entries.ContainsKey(definition);

    /// <summary>The condition that switches a definition to its working draw (its power info's override), or null.</summary>
    public static string? OverrideCondition(string definition)
    {
        if (overrides.TryGetValue(definition, out var known)) return known;
        string? condition = null;
        if (DataHandler.dictCOs != null && DataHandler.dictCOs.TryGetValue(definition, out var co) && !string.IsNullOrEmpty(co?.jsonPI) &&
            DataHandler.dictPowerInfo != null && DataHandler.dictPowerInfo.TryGetValue(co!.jsonPI, out var power) && !string.IsNullOrEmpty(power?.strOverrideCond))
            condition = power!.strOverrideCond;
        overrides[definition] = condition;
        return condition;
    }
    /// <summary>Whether a registered machine is doing work right now: its own test, or its override condition while it
    /// is powered. Read-only; a test that throws counts as not working.</summary>
    public static bool Working(CondOwner co)
    {
        if (co == null || co.bDestroyed || !entries.TryGetValue(co.strCODef ?? "", out var entry)) return false;
        try
        {
            if (entry.Working != null) return entry.Working(co);
            var condition = OverrideCondition(entry.Definition);
            return condition != null && co.HasCond(condition) && co.HasCond("IsPowered");
        }
        catch { return false; }
    }
    /// <summary>Definitions are read again on every game load.</summary>
    internal static void ForgetDefinitions() => overrides.Clear();
    internal static MachineAudio? Player;
}
