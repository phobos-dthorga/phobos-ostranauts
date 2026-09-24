using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Phobos.Ostranauts.Framework.Localization;

/// <summary>Content-owned maker/model prefixes for translated equipment descriptions.
/// Display text only: never use these names as item, recipe or save identifiers.</summary>
public sealed class EquipmentNames
{
    public const string Prefix = "Phobos'";
    public const int MaxIdentityCharacters = 64;
    private readonly Dictionary<string, string> prefixes = new Dictionary<string, string>(StringComparer.Ordinal);

    public EquipmentNames(string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > TranslationCatalog.MaxFileBytes)
            throw new FormatException("Equipment names exceed the catalog size limit.");
        using var reader = new JsonTextReader(new System.IO.StringReader(json)) { MaxDepth = 4 };
        var root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        if (reader.Read()) throw new FormatException("Unexpected content after equipment names.");
        foreach (var entry in root.Properties())
        {
            if (string.IsNullOrWhiteSpace(entry.Name) || !(entry.Value is JObject identity) ||
                identity.Properties().Any(p => p.Name != "brand" && p.Name != "model"))
                throw new FormatException("Invalid equipment identity: " + entry.Name);
            string brand = Part(identity, "brand"), model = Part(identity, "model");
            if (brand.Length == 0 && model.Length != 0) throw new FormatException("A model needs a brand.");
            prefixes.Add(entry.Name, Prefix + (brand.Length == 0 ? "" : " " + brand) + (model.Length == 0 ? "" : " " + model));
        }
    }

    public IEnumerable<string> Keys => prefixes.Keys;
    public bool Contains(string key) => prefixes.ContainsKey(key);
    public string Format(string key, string description) => prefixes.TryGetValue(key, out var prefix) ? prefix + " " + description : description;

    // Catalog values contain only the translated type/variant. Reject obsolete
    // full-name overrides instead of producing two prefixes or an old maker name.
    public bool IsDescription(string key, string value) => !Contains(key) ||
        (!value.TrimStart().StartsWith("Phobos", StringComparison.OrdinalIgnoreCase) &&
         !value.TrimStart().StartsWith(prefixes[key].Substring(Prefix.Length).Trim() + " ", StringComparison.OrdinalIgnoreCase));

    private static string Part(JObject identity, string field)
    {
        if (identity[field]?.Type != JTokenType.String) throw new FormatException("Equipment identity needs " + field + ".");
        string value = (string)identity[field]!;
        if (value.Length > MaxIdentityCharacters || value != value.Trim() ||
            value.Any(c => char.IsControl(c) || "{}<>".IndexOf(c) >= 0))
            throw new FormatException("Invalid equipment " + field + ".");
        return value;
    }
}
