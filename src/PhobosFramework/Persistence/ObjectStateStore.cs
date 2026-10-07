using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Persistence;

public enum SavedStateStatus { Missing, Ready, Invalid, DifferentOwner, UnsupportedVersion }

/// <summary>Main-thread adapter for native object property maps. Does not touch files or resume gameplay.</summary>
public sealed class ObjectStateStore
{
    private const string Prefix = "PhobosState.";
    private const string FieldPrefix = "data.";
    private readonly Dictionary<string, Dictionary<string, string>> maps;
    private readonly string key, owner;
    private readonly int version;
    private static readonly IReadOnlyDictionary<string, string> EmptyFields =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    public ObjectStateStore(Dictionary<string, Dictionary<string, string>> maps, string name, string owner, int version)
    {
        if (!SafeKey(name) || !SafeValue(owner) || version < 1) throw new ArgumentException("Invalid saved-state identity or schema.");
        this.maps = maps ?? throw new ArgumentNullException(nameof(maps));
        key = Prefix + name; this.owner = owner; this.version = version;
    }

    public SavedStateStatus Read(out IReadOnlyDictionary<string, string> fields)
    {
        fields = EmptyFields;
        var status = Validate(out var map);
        if (status != SavedStateStatus.Ready) return status;
        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in map!)
            if (pair.Key != "schema" && pair.Key != "owner") copy.Add(pair.Key.Substring(FieldPrefix.Length), pair.Value);
        fields = new ReadOnlyDictionary<string, string>(copy);
        return SavedStateStatus.Ready;
    }

    /// <summary>The record's status without copying it: the same validation as <see cref="Read"/>.</summary>
    public SavedStateStatus Status() => Validate(out _);

    // Validate current native data on every call; do not cache mutable property maps.
    private SavedStateStatus Validate(out Dictionary<string, string>? map)
    {
        if (!maps.TryGetValue(key, out map)) return SavedStateStatus.Missing;
        if (map == null || !map.TryGetValue("schema", out var schema) ||
            !int.TryParse(schema, NumberStyles.None, CultureInfo.InvariantCulture, out int found) || found < 1)
            return SavedStateStatus.Invalid;
        if (found != version) return SavedStateStatus.UnsupportedVersion;
        if (!map.TryGetValue("owner", out var savedOwner) || savedOwner != owner) return SavedStateStatus.DifferentOwner;
        foreach (var pair in map)
        {
            if (pair.Key == "schema" || pair.Key == "owner") continue;
            if (!pair.Key.StartsWith(FieldPrefix, StringComparison.Ordinal) ||
                !SafeKey(pair.Key, FieldPrefix.Length) || !SafeValue(pair.Value)) return SavedStateStatus.Invalid;
        }
        return SavedStateStatus.Ready;
    }

    /// <summary>Replaces our map with a detached snapshot. Unknown schemas/owners remain untouched.</summary>
    public bool TryWrite(IReadOnlyDictionary<string, string> fields) => Write(fields, false);
    /// <summary>As <see cref="TryWrite"/>, but leaves a record that already holds exactly these fields untouched.
    /// Every validation rule still applies; only the replacement of an identical map is skipped, so services that
    /// save on every power step write nothing while nothing changed.</summary>
    public bool TryWriteIfChanged(IReadOnlyDictionary<string, string> fields) => Write(fields, true);
    // Framework 0.133.0 (L105): services that save on every power step mostly write what the record already holds (about
    // 450 such writes a real second on the owner's 8 October 2026 capture). The new fields are checked first and an
    // identical record is recognised without allocating; a record that equals checked fields under this schema and owner
    // passes every rule Validate applies, so only a record that differs is validated in full before it is replaced.
    private bool Write(IReadOnlyDictionary<string, string> fields, bool skipUnchanged)
    {
        using var measurement = Diagnostics.Performance.Measure(Diagnostics.Performance.StateWrite);
        foreach (var pair in fields) if (!SafeKey(pair.Key) || !SafeValue(pair.Value)) return false;
        if (skipUnchanged && maps.TryGetValue(key, out var held) && held != null && Ours(held) && Unchanged(held, fields))
        { Diagnostics.Performance.Increment(Diagnostics.Performance.StateWritesSkipped); return true; }
        var state = Validate(out _);
        if (state != SavedStateStatus.Missing && state != SavedStateStatus.Ready) return false;
        var copy = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["schema"] = version.ToString(CultureInfo.InvariantCulture), ["owner"] = owner
        };
        foreach (var pair in fields) copy.Add(FieldPrefix + pair.Key, pair.Value);
        maps[key] = copy;
        return true;
    }
    // This schema and owner, read as Validate reads them.
    private bool Ours(Dictionary<string, string> map) =>
        map.TryGetValue("schema", out var schema) && int.TryParse(schema, NumberStyles.None, CultureInfo.InvariantCulture, out int found) &&
        found == version && map.TryGetValue("owner", out var savedOwner) && savedOwner == owner;
    private static bool Unchanged(Dictionary<string, string> current, IReadOnlyDictionary<string, string> fields)
    {
        if (current.Count != fields.Count + 2) return false;
        foreach (var pair in fields)
            if (!current.TryGetValue(Prefixed(pair.Key), out var value) || !string.Equals(value, pair.Value, StringComparison.Ordinal)) return false;
        return true;
    }
    // Field names are a small fixed set per service; their stored names are built once. The bound only guards against
    // runaway names; past it each name is built on use, as before.
    private const int PrefixedLimit = 4096;
    private static readonly Dictionary<string, string> prefixed = new(StringComparer.Ordinal);
    private static string Prefixed(string field)
    {
        if (prefixed.TryGetValue(field, out var name)) return name;
        name = FieldPrefix + field;
        if (prefixed.Count < PrefixedLimit) prefixed[field] = name;
        return name;
    }

    /// <summary>Explicit consumer-authorized reset only; never call as automatic load recovery.</summary>
    public void Clear() => maps.Remove(key);
    private static bool SafeKey(string? value, int start = 0)
    {
        if (value == null || value.Length <= start || value.Length - start > 100) return false;
        for (int i = start; i < value.Length; i++)
            if (!char.IsLetterOrDigit(value[i]) && value[i] != '.' && value[i] != '_' && value[i] != '-') return false;
        return true;
    }
    /// <summary>The longest value a field may hold; consumers with longer lists split them across keys.</summary>
    public const int MaxValueLength = 512;
    public static bool SafeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value!.Length > MaxValueLength) return false;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsControl(c) || c == '=' || c == ',') return false;
        }
        return true;
    }
}
