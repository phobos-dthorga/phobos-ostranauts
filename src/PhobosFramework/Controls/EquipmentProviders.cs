using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Optional presentation hints; existing providers keep their binary contract.</summary>
public interface IEquipmentPanelPresentation
{
    bool IsConfiguration(string action);
    string ConfigurationStamp(CondOwner equipment);
    bool ApplyConfiguration(CondOwner equipment,ConsoleBinding? scope,string expected,string action,out string reason);
}

public sealed class EquipmentAction
{
    public string Id { get; }
    public string Label { get; }
    public EquipmentAction(string id, string label) { Id = id; Label = label; }
}
public sealed class EquipmentSnapshot
{
    public string Id { get; }
    public string Name { get; }
    public string Group { get; }
    public EquipmentActivity Activity { get; }
    public IReadOnlyList<EquipmentAction> Actions { get; }
    public EquipmentSnapshot(string id, string name, string group, EquipmentActivity activity, IEnumerable<EquipmentAction> actions)
    { Id = id; Name = name; Group = group; Activity = activity; Actions = Array.AsReadOnly(actions.ToArray()); }
}
public interface IEquipmentProvider
{
    string Id { get; }
    IReadOnlyList<string> Definitions { get; }
    EquipmentSnapshot Snapshot(CondOwner equipment);
    bool Command(CondOwner equipment, ConsoleBinding? binding, string action, out string message);
}
public static class EquipmentProviders
{
    private static readonly Dictionary<string, IEquipmentProvider> definitions = new(StringComparer.Ordinal);
    public static void Register(IEquipmentProvider provider)
    {
        if (provider.Definitions.Count == 0 || provider.Definitions.Distinct().Count() != provider.Definitions.Count ||
            definitions.Values.Any(p => p.Id == provider.Id) || provider.Definitions.Any(definitions.ContainsKey))
            throw new ArgumentException("Duplicate equipment provider or definition owner.");
        foreach (string id in provider.Definitions) definitions.Add(id, provider);
    }
    public static void Unregister(string id)
    { foreach (string key in definitions.Where(p => p.Value.Id == id).Select(p => p.Key).ToArray()) definitions.Remove(key); }
    public static IEquipmentProvider? For(string definition) => definitions.TryGetValue(definition, out var provider) ? provider : null;
}
