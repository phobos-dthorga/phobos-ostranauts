using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>What an add-on says about itself in <c>phobos-addon.json</c>. Unknown fields are refused.</summary>
public sealed class AddOnManifest
{
    public int schemaVersion;
    /// <summary>The add-on's own short id: lower-case letters, digits and hyphens.</summary>
    public string id = "";
    public string name = "";
    public string author = "";
    public string version = "";
    /// <summary>The start of every id the add-on adds (recipes, tables, crops, items, text keys), so two add-ons never
    /// collide and nothing an add-on adds can be mistaken for ours.</summary>
    public string idPrefix = "";
    /// <summary>Phobos mod folder names and the lowest version of each the add-on's files for it need.</summary>
    public Dictionary<string, string> requires = new(StringComparer.Ordinal);
    public string? notes;
}

/// <summary>One enabled add-on: its manifest, its folder and its place in the game's load order.</summary>
public sealed class AddOn
{
    public AddOnManifest Manifest { get; }
    public string Directory { get; }
    public int Order { get; }
    public AddOn(AddOnManifest manifest, string directory, int order) { Manifest = manifest; Directory = directory; Order = order; }
    /// <summary>The folder holding this add-on's files for one Phobos mod and schema, whether or not it exists.</summary>
    public string Folder(string modFolder, string schema) => Path.Combine(Directory, AddOns.Folder, modFolder, schema);
}

/// <summary>Add-ons players publish (Framework 0.90.0; owner direction, 4 October 2026: players may publish their own
/// changes, additions and fixes on Steam Workshop). An add-on is an ordinary enabled game mod folder holding a
/// <c>phobos-addon.json</c> manifest and, under <c>phobos/&lt;Mod&gt;/&lt;schema&gt;/</c>, the same override files a player
/// keeps in <c>BepInEx/config/&lt;Mod&gt;/&lt;schema&gt;/</c>. It is data only: the loader that reads local files reads
/// these, under the same checks, in the game's own load order. This class finds them; it decides nothing about what a
/// file may do.</summary>
public static class AddOns
{
    public const string ManifestFile = "phobos-addon.json", Folder = "phobos", FrameworkKey = "PhobosFramework";
    public const int MinPrefix = 3, MaxPrefix = 24;
    /// <summary>Prefixes an add-on may not claim: ours and the game's own.</summary>
    public static readonly IReadOnlyList<string> ReservedPrefixes = new[] { "Phobos", "Itm", "Sys", "Stat", "Is" };
    /// <summary>The enabled mod folders in the game's load order. The plugin supplies the game's own list; offline
    /// checks supply folders directly; null means no add-ons.</summary>
    public static Func<IEnumerable<string>>? EnabledModDirectories { get; set; }
    public static Action<string> Log { get; set; } = _ => { };
    private static List<AddOn>? current;
    private static readonly List<string> refused = new();

    /// <summary>Forgets what was found, so the next load reads the mod list afresh (a new game load).</summary>
    public static void Reset() { current = null; refused.Clear(); }
    /// <summary>Add-ons that were found but not used, each with its reason.</summary>
    public static IReadOnlyList<string> Refused { get { _ = Current; return refused; } }

    /// <summary>Every usable add-on, in load order. A folder whose manifest is unreadable or breaks a rule, or whose
    /// id or prefix an earlier add-on already holds, is left out with a reason.</summary>
    public static IReadOnlyList<AddOn> Current
    {
        get
        {
            if (current != null) return current;
            var found = new List<AddOn>(); refused.Clear();
            int order = 0;
            IEnumerable<string> directories;
            try { directories = EnabledModDirectories?.Invoke()?.ToArray() ?? Array.Empty<string>(); }
            catch (Exception ex) { Log(ex.Message); directories = Array.Empty<string>(); }
            foreach (string directory in directories)
            {
                order++;
                string path;
                try { path = Path.Combine(directory, ManifestFile); if (!File.Exists(path)) continue; }
                catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException) { continue; }
                try
                {
                    if (new FileInfo(path).Length > DataPacks.MaxFileBytes) throw new FormatException(Text.Get("DataPacks.file_too_large"));
                    var manifest = ParseManifest(File.ReadAllText(path));
                    var clash = found.FirstOrDefault(a => a.Manifest.id == manifest.id || string.Equals(a.Manifest.idPrefix, manifest.idPrefix, StringComparison.OrdinalIgnoreCase));
                    if (clash != null) throw new FormatException(Text.Get("AddOns.clash", clash.Manifest.id));
                    if (manifest.requires.TryGetValue(FrameworkKey, out var need) && !AtLeast(FrameworkInfo.Version, need))
                        throw new FormatException(Text.Get("AddOns.requires", FrameworkKey, need, FrameworkInfo.Version));
                    found.Add(new AddOn(manifest, directory, order));
                }
                catch (Exception ex) when (ex is FormatException || ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
                {
                    string line = Text.Get("AddOns.refused", directory, ex.Message);
                    refused.Add(line); Log(line);
                }
            }
            return current = found;
        }
    }

    /// <summary>Strict parse of a manifest and its rules. Pure.</summary>
    public static AddOnManifest ParseManifest(string json)
    {
        JObject root;
        try { root = JObject.Parse(json ?? "", new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }); }
        catch (JsonException ex) { throw new FormatException(ex.Message); }
        AddOnManifest m;
        try { m = root.ToObject<AddOnManifest>(JsonSerializer.Create(new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error })) ?? throw new FormatException(Text.Get("AddOns.manifest_empty")); }
        catch (JsonException ex) { throw new FormatException(ex.Message); }
        if (m.schemaVersion != 1) throw new FormatException(Text.Get("AddOns.manifest_version"));
        if (m.id.Length < 3 || m.id.Length > 48 || m.id.Any(c => !(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '-'))) throw new FormatException(Text.Get("AddOns.manifest_id"));
        if (string.IsNullOrWhiteSpace(m.name) || string.IsNullOrWhiteSpace(m.author) || !Version.TryParse(m.version, out _)) throw new FormatException(Text.Get("AddOns.manifest_fields"));
        if (m.idPrefix.Length < MinPrefix || m.idPrefix.Length > MaxPrefix || !char.IsLetter(m.idPrefix[0]) || m.idPrefix.Any(c => !(char.IsLetterOrDigit(c) && c < 128)) ||
            ReservedPrefixes.Any(r => m.idPrefix.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
            throw new FormatException(Text.Get("AddOns.manifest_prefix", MinPrefix, MaxPrefix));
        foreach (var pair in m.requires)
            if (string.IsNullOrWhiteSpace(pair.Key) || !Version.TryParse(pair.Value, out _)) throw new FormatException(Text.Get("AddOns.manifest_requires", pair.Key));
        return m;
    }
    /// <summary>Whether an installed version meets a required one. An unreadable version never does.</summary>
    public static bool AtLeast(string installed, string required) =>
        Version.TryParse(installed, out var have) && Version.TryParse(required, out var need) && have >= need;
    /// <summary>Whether an id belongs to an add-on's own namespace: it starts with the prefix, ignoring case.</summary>
    public static bool Owns(AddOnManifest manifest, string id) => id != null && id.StartsWith(manifest.idPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>The add-ons whose files for this mod may load: those that ask no more of it than the installed version.
    /// One that asks for more is named once in the refusals.</summary>
    public static IEnumerable<AddOn> For(string modFolder, string installedVersion)
    {
        foreach (var addOn in Current)
        {
            if (addOn.Manifest.requires.TryGetValue(modFolder, out var need) && !AtLeast(installedVersion, need))
            {
                string line = Text.Get("AddOns.refused", addOn.Manifest.id, Text.Get("AddOns.requires", modFolder, need, installedVersion));
                if (!refused.Contains(line)) { refused.Add(line); Log(line); }
                continue;
            }
            yield return addOn;
        }
    }

    /// <summary>Console text: each add-on with its author and version, then what was refused.</summary>
    public static string Describe()
    {
        var lines = Current.Select(a => Text.Get("AddOns.line", a.Manifest.name, a.Manifest.version, a.Manifest.author, a.Manifest.id, a.Manifest.idPrefix)).ToList();
        if (lines.Count == 0) lines.Add(Text.Get("AddOns.none"));
        lines.AddRange(refused);
        return string.Join("\n", lines);
    }
}
