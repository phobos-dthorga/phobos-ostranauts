using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhobosWarDeclared.Core;

namespace PhobosWarDeclared;

/// <summary>Shipped schematics (embedded, also copied into the mod folder as examples) and the player's own
/// files in <c>BepInEx/config/PhobosWarDeclared/schematics</c>. A player file overrides a shipped one of the
/// same name. A file that cannot be read is skipped and reported; it never stops the others.</summary>
internal static class Schematics
{
    private const string ResourcePrefix = "PhobosWarDeclared.schematics.";
    private static Dictionary<string, Schematic> all = new(StringComparer.Ordinal);
    private static readonly List<string> problems = new();
    private static string selected = WarRules.DefaultSchematic;

    internal static string UserDirectory { get; set; } = "";
    internal static IReadOnlyList<string> Problems => problems;
    internal static IEnumerable<Schematic> All => all.Values.OrderBy(s => s.Key, StringComparer.Ordinal);
    internal static Schematic Active => all.TryGetValue(selected, out var s) ? s : all[WarRules.DefaultSchematic];
    internal static string Selected => selected;

    /// <summary>Reads every schematic and selects <paramref name="key"/>, falling back to the shipped safe schematic.</summary>
    internal static string Load(string key)
    {
        var found = new Dictionary<string, Schematic>(StringComparer.Ordinal);
        problems.Clear();
        var assembly = typeof(Schematics).Assembly;
        foreach (var resource in assembly.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)))
        {
            var name = Schematic.KeyFromFileName(resource.Substring(ResourcePrefix.Length));
            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            // Shipped files are checked offline; a failure here is a packaging fault worth surfacing loudly.
            if (name != null) found[name] = Schematic.Parse(name, reader.ReadToEnd());
        }
        if (!found.ContainsKey(WarRules.DefaultSchematic)) throw new InvalidOperationException("The shipped safe schematic is missing.");
        if (!string.IsNullOrEmpty(UserDirectory))
        {
            try
            {
                Directory.CreateDirectory(UserDirectory);
                foreach (var path in Directory.GetFiles(UserDirectory, "*.json").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                {
                    var file = Path.GetFileName(path);
                    var name = Schematic.KeyFromFileName(file);
                    if (name == null) { problems.Add(Text.Get("Schematic.bad_name", file)); continue; }
                    try { found[name] = Schematic.Parse(name, File.ReadAllText(path)); }
                    catch (Exception ex) when (ex is FormatException || ex is IOException || ex is UnauthorizedAccessException)
                    { problems.Add(Text.Get("Schematic.bad_file", file, ex.Message)); }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { problems.Add(Text.Get("Schematic.no_folder", UserDirectory, ex.Message)); }
        }
        all = found;
        selected = NormalKey(key);
        if (!all.ContainsKey(selected))
        {
            problems.Add(Text.Get("Schematic.missing", key, WarRules.DefaultSchematic));
            selected = WarRules.DefaultSchematic;
        }
        return selected;
    }

    internal static bool Select(string key, out string message)
    {
        var normal = NormalKey(key);
        if (!all.TryGetValue(normal, out var schematic)) { message = Text.Get("Schematic.unknown", key, string.Join(", ", all.Keys.OrderBy(k => k))); return false; }
        selected = normal;
        message = Text.Get("Schematic.selected", schematic.Key, schematic.Title);
        return true;
    }

    internal static string Describe()
    {
        var lines = All.Select(s => Text.Get(s.Key == selected ? "Schematic.line_active" : "Schematic.line", s.Key, s.Title, s.Description)).ToList();
        lines.Add(Text.Get("Schematic.folder", UserDirectory));
        lines.AddRange(problems);
        return string.Join("\n", lines);
    }

    private static string NormalKey(string? key) => (key ?? "").Trim().ToLowerInvariant();
}
