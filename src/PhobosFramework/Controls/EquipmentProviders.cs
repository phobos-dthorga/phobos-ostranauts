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

/// <summary>Optional compact selectors. Choices and checked application stay content-owned.</summary>
public interface IEquipmentPanelFields : IEquipmentPanelPresentation
{
    IEnumerable<EquipmentField> Fields(CondOwner equipment);
}
public sealed class EquipmentField
{
    public readonly string Label,Value;
    public readonly IReadOnlyList<(string Id,string Label)> Choices;
    /// <summary>The choice id matching the current setting, so a configuration sheet opens with it marked; empty when
    /// the provider does not say (the sheet then marks nothing, as before).</summary>
    public readonly string Current;
    public EquipmentField(string label,string value,IEnumerable<(string Id,string Label)> choices):this(label,value,choices,""){}
    public EquipmentField(string label,string value,IEnumerable<(string Id,string Label)> choices,string? current)
    {Label=label;Value=value;Choices=Array.AsReadOnly(choices.ToArray());Current=current??"";}
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

    private static readonly Dictionary<string, Func<string>> groups = new(StringComparer.Ordinal);
    /// <summary>A provider names its own snapshot groups, so a console that lists every mod's equipment (Shipbreaker's
    /// C1) shows them in the owner's words instead of needing a translation for each foreign group.</summary>
    public static void RegisterGroup(string group, Func<string> label)
    {
        if (string.IsNullOrEmpty(group) || label == null) throw new ArgumentException("Equipment group needs an id and a label.");
        groups[group] = label;
    }
    /// <summary>The registered label of a group, or null when no provider has named it.</summary>
    public static string? GroupLabel(string group) => groups.TryGetValue(group, out var label) ? label() : null;
    public static IReadOnlyCollection<string> Groups => groups.Keys;
}
