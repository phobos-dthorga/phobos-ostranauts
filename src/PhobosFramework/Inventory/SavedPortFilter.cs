using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Inventory;

public enum PortFilterState { Default, Configured, Invalid }

/// <summary>Saved exact definition IDs, separate from pairing and native signal settings.
/// Consumers choose compatible defaults, validate eligibility and own access/run permission.</summary>
public sealed class PortFilterSnapshot
{
    public PortFilterState State { get; }
    public IReadOnlyList<string> DefinitionIds { get; }
    internal PortFilterSnapshot(PortFilterState state, IEnumerable<string>? ids = null)
    { State = state; DefinitionIds = Array.AsReadOnly((ids ?? Array.Empty<string>()).ToArray()); }
    public bool Allows(string id, ItemDefinitionFilter defaults) => State == PortFilterState.Default
        ? defaults.Allows(id) : State == PortFilterState.Configured && DefinitionIds.Contains(id, StringComparer.Ordinal);
}

public static class SavedPortFilter
{
    public const int MaximumDefinitions = 64;
    private static string Key(MaterialPort port) => "PhobosMaterialFilter." + port.PortId;
    public static PortFilterSnapshot Read(MaterialPort port)
    {
        if (!port.Maps.TryGetValue(Key(port), out var map)) return new PortFilterSnapshot(PortFilterState.Default);
        string Value(string key) => map != null && map.TryGetValue(key, out var value) ? value : "";
        if (Value("schema") != "1" || Value("owner") != port.ObjectId || Value("port") != port.PortId ||
            !int.TryParse(Value("count"), NumberStyles.None, CultureInfo.InvariantCulture, out int count) || count < 0 || count > MaximumDefinitions)
            return new PortFilterSnapshot(PortFilterState.Invalid);
        var ids = Enumerable.Range(0, count).Select(i => Value("id" + i.ToString(CultureInfo.InvariantCulture))).ToArray();
        return ids.Any(id => !MaterialPort.SafeValue(id)) || ids.Distinct(StringComparer.Ordinal).Count() != count
            ? new PortFilterSnapshot(PortFilterState.Invalid) : new PortFilterSnapshot(PortFilterState.Configured, ids);
    }
    public static void Set(MaterialPort port, IEnumerable<string> definitionIds)
    {
        if (definitionIds == null) throw new ArgumentNullException(nameof(definitionIds));
        var ids = definitionIds.Take(MaximumDefinitions + 1).ToArray();
        if (ids.Length > MaximumDefinitions || ids.Any(id => !MaterialPort.SafeValue(id)) || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new ArgumentException(Text.Get("SavedPortFilter.invalid_definitions"), nameof(definitionIds));
        var record = new Dictionary<string, string>(StringComparer.Ordinal)
        { ["schema"] = "1", ["owner"] = port.ObjectId, ["port"] = port.PortId, ["count"] = ids.Length.ToString(CultureInfo.InvariantCulture) };
        for (int i = 0; i < ids.Length; i++) record["id" + i.ToString(CultureInfo.InvariantCulture)] = ids[i];
        port.Maps[Key(port)] = record;
    }
}
