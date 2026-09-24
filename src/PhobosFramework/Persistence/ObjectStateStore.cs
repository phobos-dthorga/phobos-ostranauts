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

    public ObjectStateStore(Dictionary<string, Dictionary<string, string>> maps, string name, string owner, int version)
    {
        if (!SafeKey(name) || !SafeValue(owner) || version < 1) throw new ArgumentException("Invalid saved-state identity or schema.");
        this.maps = maps ?? throw new ArgumentNullException(nameof(maps));
        key = Prefix + name; this.owner = owner; this.version = version;
    }

    public SavedStateStatus Read(out IReadOnlyDictionary<string, string> fields)
    {
        fields = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        if (!maps.TryGetValue(key, out var map)) return SavedStateStatus.Missing;
        if (map == null || !map.TryGetValue("schema", out var schema) ||
            !int.TryParse(schema, NumberStyles.None, CultureInfo.InvariantCulture, out int found) || found < 1)
            return SavedStateStatus.Invalid;
        if (found != version) return SavedStateStatus.UnsupportedVersion;
        if (!map.TryGetValue("owner", out var savedOwner) || savedOwner != owner) return SavedStateStatus.DifferentOwner;
        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in map)
        {
            if (pair.Key == "schema" || pair.Key == "owner") continue;
            if (!pair.Key.StartsWith(FieldPrefix, StringComparison.Ordinal) ||
                !SafeKey(pair.Key.Substring(FieldPrefix.Length)) || !SafeValue(pair.Value)) return SavedStateStatus.Invalid;
            copy.Add(pair.Key.Substring(FieldPrefix.Length), pair.Value);
        }
        fields = new ReadOnlyDictionary<string, string>(copy);
        return SavedStateStatus.Ready;
    }

    /// <summary>Replaces our map with a detached snapshot. Unknown schemas/owners remain untouched.</summary>
    public bool TryWrite(IReadOnlyDictionary<string, string> fields)
    {
        var state = Read(out _);
        if (state != SavedStateStatus.Missing && state != SavedStateStatus.Ready) return false;
        var copy = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["schema"] = version.ToString(CultureInfo.InvariantCulture), ["owner"] = owner
        };
        foreach (var pair in fields)
        {
            if (!SafeKey(pair.Key) || !SafeValue(pair.Value)) return false;
            copy.Add(FieldPrefix + pair.Key, pair.Value);
        }
        maps[key] = copy;
        return true;
    }

    /// <summary>Explicit consumer-authorized reset only; never call as automatic load recovery.</summary>
    public void Clear() => maps.Remove(key);
    private static bool SafeKey(string? value) => !string.IsNullOrEmpty(value) && value!.Length <= 100 &&
        value.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-');
    public static bool SafeValue(string? value) => !string.IsNullOrWhiteSpace(value) && value!.Length <= 512 &&
        !value.Any(c => char.IsControl(c) || c == '=' || c == ',');
}
