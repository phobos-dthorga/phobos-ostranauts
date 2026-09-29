using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PhobosWarDeclared.Core;

/// <summary>What happens to a destroyed part when the ship stands down.</summary>
public enum SchematicAction { Lay, Hold, Ignore }

public enum PartFootprint { Walkable, Blocks, Unknown }

/// <summary>What a schematic may test about a destroyed part: its intact definition ID, the native
/// conditions that definition starts with, the INSTALL tab that lists it and whether its placeholder
/// would block walking.</summary>
public sealed class PartFacts
{
    public PartFacts(string part, IEnumerable<string>? conditions, string? menu, PartFootprint footprint)
    {
        Part = part ?? ""; Conditions = new HashSet<string>(conditions ?? Array.Empty<string>(), StringComparer.Ordinal);
        Menu = menu; Footprint = footprint;
    }
    public string Part { get; }
    public IReadOnlyCollection<string> Conditions { get; }
    public string? Menu { get; }
    public PartFootprint Footprint { get; }
}

public sealed class SchematicRule
{
    internal SchematicRule(SchematicAction action, string[] parts, string[] conditions, string[] menus, PartFootprint? footprint, string note)
    {
        Action = action; Parts = parts; Conditions = conditions; Menus = menus; Footprint = footprint; Note = note;
        patterns = parts.Select(p => new Regex("^" + Regex.Escape(p).Replace("\\*", ".*") + "$", RegexOptions.CultureInvariant)).ToArray();
    }
    private readonly Regex[] patterns;
    public SchematicAction Action { get; }
    public IReadOnlyList<string> Parts { get; }
    public IReadOnlyList<string> Conditions { get; }
    public IReadOnlyList<string> Menus { get; }
    public PartFootprint? Footprint { get; }
    public string Note { get; }

    /// <summary>Every field the rule states must match; a list matches when any of its entries does.</summary>
    public bool Matches(PartFacts part)
    {
        if (patterns.Length > 0 && !patterns.Any(p => p.IsMatch(part.Part))) return false;
        if (Conditions.Count > 0 && !Conditions.Any(part.Conditions.Contains)) return false;
        if (Menus.Count > 0 && (part.Menu == null || !Menus.Any(m => string.Equals(m, part.Menu, StringComparison.OrdinalIgnoreCase)))) return false;
        if (Footprint.HasValue && Footprint.Value != part.Footprint) return false;
        return true;
    }
}

/// <summary>A player-editable filter deciding which destroyed parts get build sites. The first matching
/// rule decides; parts no rule matches take the default action. See docs/war-declared-player-guide.md.</summary>
public sealed class Schematic
{
    private static readonly Regex KeyPattern = new("^[a-z0-9][a-z0-9_-]{0,47}$", RegexOptions.CultureInvariant);
    private static readonly string[] TopLevel = { "title", "description", "default", "rules" };
    private static readonly string[] RuleFields = { "action", "parts", "conditions", "menus", "footprint", "note" };

    private Schematic(string key, string title, string description, SchematicAction fallback, IReadOnlyList<SchematicRule> rules)
    { Key = key; Title = title; Description = description; Default = fallback; Rules = rules; }

    /// <summary>The lower-case file name without extension; what settings and commands select.</summary>
    public string Key { get; }
    public string Title { get; }
    public string Description { get; }
    public SchematicAction Default { get; }
    public IReadOnlyList<SchematicRule> Rules { get; }

    public SchematicAction Evaluate(PartFacts part, out SchematicRule? rule)
    {
        rule = Rules.FirstOrDefault(r => r.Matches(part));
        return rule?.Action ?? Default;
    }

    /// <summary>A schematic key from a file name, or null when the name cannot be one.</summary>
    public static string? KeyFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return null;
        var key = fileName.Substring(0, fileName.Length - 5).Trim().ToLowerInvariant();
        return KeyPattern.IsMatch(key) ? key : null;
    }

    /// <summary>Reads a schematic; throws <see cref="FormatException"/> naming the first problem found.</summary>
    public static Schematic Parse(string key, string json)
    {
        if (key == null || !KeyPattern.IsMatch(key)) throw new FormatException("Schematic file names use lower-case letters, digits, '-' and '_' (at most 48).");
        JObject root;
        try { root = JObject.Parse(json ?? ""); }
        catch (JsonReaderException ex) { throw new FormatException("Not valid JSON: " + ex.Message); }
        Unknown(root, TopLevel, "the schematic");
        var fallback = root["default"] == null ? SchematicAction.Hold : Action(root["default"], "default");
        var rules = new List<SchematicRule>();
        if (root["rules"] != null)
        {
            if (root["rules"] is not JArray array) throw new FormatException("'rules' must be a list.");
            for (int i = 0; i < array.Count; i++)
            {
                string where = "rule " + (i + 1);
                if (array[i] is not JObject rule) throw new FormatException(where + " must be an object.");
                Unknown(rule, RuleFields, where);
                if (rule["action"] == null) throw new FormatException(where + " needs an 'action' (lay, hold or ignore).");
                PartFootprint? footprint = null;
                if (rule["footprint"] != null)
                {
                    var text = Text(rule["footprint"], where + " footprint");
                    footprint = text.ToLowerInvariant() switch
                    {
                        "walkable" => PartFootprint.Walkable, "blocks" => PartFootprint.Blocks, "unknown" => PartFootprint.Unknown,
                        _ => throw new FormatException(where + " footprint must be walkable, blocks or unknown.")
                    };
                }
                rules.Add(new SchematicRule(Action(rule["action"], where + " action"), List(rule["parts"], where + " parts"),
                    List(rule["conditions"], where + " conditions"), List(rule["menus"], where + " menus"), footprint,
                    rule["note"] == null ? "" : Text(rule["note"], where + " note")));
            }
        }
        return new Schematic(key, root["title"] == null ? key : Text(root["title"], "title"),
            root["description"] == null ? "" : Text(root["description"], "description"), fallback, rules);
    }

    private static void Unknown(JObject o, string[] allowed, string where)
    {
        var extra = o.Properties().Select(p => p.Name).FirstOrDefault(n => !allowed.Contains(n, StringComparer.Ordinal));
        if (extra != null) throw new FormatException("Unknown field '" + extra + "' in " + where + ".");
    }
    private static string Text(JToken? token, string where) =>
        token is JValue { Type: JTokenType.String } v && !string.IsNullOrWhiteSpace((string?)v.Value) ? ((string)v.Value!).Trim()
            : throw new FormatException(where + " must be text.");
    private static SchematicAction Action(JToken? token, string where) => Text(token, where).ToLowerInvariant() switch
    {
        "lay" => SchematicAction.Lay, "hold" => SchematicAction.Hold, "ignore" => SchematicAction.Ignore,
        _ => throw new FormatException(where + " must be lay, hold or ignore.")
    };
    private static string[] List(JToken? token, string where)
    {
        if (token == null) return Array.Empty<string>();
        if (token is not JArray array || array.Count == 0) throw new FormatException(where + " must be a non-empty list of text.");
        return array.Select((t, i) => Text(t, where + " entry " + (i + 1))).ToArray();
    }
}
