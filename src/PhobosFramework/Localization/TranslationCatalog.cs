using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Phobos.Ostranauts.Framework.Localization;

/// <summary>Content-owned JSON messages with an embedded English safety fallback.</summary>
public sealed class TranslationCatalog
{
    public const int MaxFileBytes = 1048576;
    public const int MaxMessageCharacters = 16384;
    public const int MaxArguments = 64, MaxAlignment = 1024, MaxNumericPrecision = 100;
    private readonly Dictionary<string, string> english;
    private readonly Dictionary<string, string> selected = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
    private readonly Action<string> log;
    public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("en");
    public bool Contains(string key) => english.ContainsKey(key);

    public TranslationCatalog(string englishJson, Action<string>? log = null)
    {
        this.log = log ?? (_ => { });
        english = Parse(englishJson);
        foreach (string message in english.Values) Signature(message);
    }

    public static Dictionary<string, string> Parse(string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxFileBytes) throw new FormatException("Translation file exceeds 1 MiB.");
        using var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 4, DateParseHandling = DateParseHandling.None };
        var obj = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        if (reader.Read()) throw new FormatException("Unexpected content after translation object.");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in obj.Properties())
        {
            if (property.Value.Type != JTokenType.String || property.Name.Length == 0)
                throw new FormatException("Translation entries must be named strings.");
            string value = (string)property.Value!;
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaxMessageCharacters)
                throw new FormatException("Empty or oversized translation: " + property.Name);
            result.Add(property.Name, value);
        }
        return result;
    }

    // Parse indices, validate escaped braces/format syntax, and allow reordered or
    // repeated arguments. Formatting uses a neutral IFormattable probe, not gameplay data.
    public static string Signature(string format)
    {
        foreach (Match match in Regex.Matches(format, @"\{[0-9]+,\s*(-?[0-9]+)"))
            if (!int.TryParse(match.Groups[1].Value, out int alignment) || alignment < -MaxAlignment || alignment > MaxAlignment)
                throw new FormatException("Translation alignment is too large.");
        foreach (Match match in Regex.Matches(format, @":\s*[A-Za-z]([0-9]{3,})"))
            if (!int.TryParse(match.Groups[1].Value, out int precision) || precision > MaxNumericPrecision)
                throw new FormatException("Translation numeric precision is too large.");
        var indices = new SortedSet<int>();
        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] != '{') continue;
            if (i + 1 < format.Length && format[i + 1] == '{') { i++; continue; }
            int start = ++i;
            while (i < format.Length && char.IsDigit(format[i])) i++;
            if (start == i || !int.TryParse(format.Substring(start, i - start), out int index) || index >= MaxArguments)
                throw new FormatException("Invalid translation argument index.");
            indices.Add(index);
        }
        var probes = Enumerable.Repeat<object>(new FormatProbe(), indices.Count == 0 ? 0 : indices.Max + 1).ToArray();
        string.Format(CultureInfo.InvariantCulture, format, probes);
        return string.Join(",", indices);
    }

    private sealed class FormatProbe : IFormattable
    {
        public string ToString(string? format, IFormatProvider? provider) => "value";
    }

    public void Select(string language, params string[] overlays)
    {
        selected.Clear(); reported.Clear();
        try { Culture = CultureInfo.GetCultureInfo(language); }
        catch (CultureNotFoundException) { Culture = CultureInfo.GetCultureInfo("en"); }
        foreach (string json in overlays)
        {
            Dictionary<string, string> entries;
            try { entries = Parse(json); }
            catch (Exception ex) when (ex is JsonException || ex is FormatException)
            { Warn("file:" + ex.Message, "Invalid translation file; retaining fallback: " + ex.Message); continue; }
            foreach (var entry in entries)
            {
                if (!english.TryGetValue(entry.Key, out string original))
                { Warn(entry.Key, "Unknown translation key: " + entry.Key); continue; }
                try
                {
                    if (Signature(original) != Signature(entry.Value)) throw new FormatException("Argument set differs.");
                    var tokens = Regex.Matches(original, @"\[(us|them|crafts|checks)\]").Cast<Match>().Select(m => m.Value).OrderBy(s => s);
                    var translatedTokens = Regex.Matches(entry.Value, @"\[(us|them|crafts|checks)\]").Cast<Match>().Select(m => m.Value).OrderBy(s => s);
                    if (!tokens.SequenceEqual(translatedTokens)) throw new FormatException("Native grammar tokens differ.");
                    selected[entry.Key] = entry.Value;
                }
                catch (FormatException ex) { Warn(entry.Key, "Invalid translation " + entry.Key + ": " + ex.Message); }
            }
        }
    }

    public string Get(string key, params object[] args)
    {
        if (!english.TryGetValue(key, out string original))
        { Warn(key, "Missing English translation key: " + key); return "[" + key + "]"; }
        string format = selected.TryGetValue(key, out var translated) ? translated : original;
        try { return string.Format(Culture, format, args); }
        catch (FormatException)
        {
            Warn(key, "Translation formatting failed; using English: " + key);
            try { return string.Format(CultureInfo.GetCultureInfo("en"), original, args); }
            catch (FormatException) { return original; }
        }
    }

    private void Warn(string key, string message) { if (reported.Add(key)) log(message); }
}

/// <summary>Shared language selection. Content assemblies retain their own keys and English resources.</summary>
public static class Translations
{
    private static readonly Dictionary<string, TranslationCatalog> catalogs = new Dictionary<string, TranslationCatalog>(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> directories = new Dictionary<string, string>(StringComparer.Ordinal);
    public static string Language { get; private set; } = "en";
    public static Action<string> Log { get; set; } = _ => { };
    public static string? UserDirectory { get; set; }

    public static TranslationCatalog Register(string owner, Assembly assembly, string resource)
    {
        if (!Regex.IsMatch(owner, "^[A-Za-z][A-Za-z0-9_.-]{0,199}$")) throw new ArgumentException("Invalid translation owner.");
        if (catalogs.TryGetValue(owner, out var found)) return found;
        using var stream = assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException("Missing English resource: " + resource);
        using var reader = new StreamReader(stream);
        var catalog = new TranslationCatalog(reader.ReadToEnd(), message => Log(owner + ": " + message));
        catalogs.Add(owner, catalog);
        directories.Add(owner, Path.Combine(Path.GetDirectoryName(assembly.Location)!, "translations"));
        Load(owner, catalog);
        return catalog;
    }

    public static string Get(string owner, string key, string fallback) =>
        catalogs.TryGetValue(owner, out var catalog) && catalog.Contains(key) ? catalog.Get(key) : fallback;

    public static void Select(string language)
    {
        language = language ?? "en";
        if (!Regex.IsMatch(language, "^[a-zA-Z]{2,8}(-[a-zA-Z0-9]{2,8})*$")) language = "en";
        Language = language;
        foreach (var pair in catalogs) Load(pair.Key, pair.Value);
    }

    private static void Load(string owner, TranslationCatalog catalog)
    {
        var names = new List<string> { "en" };
        string neutral = Language.Split('-')[0];
        if (!names.Contains(neutral, StringComparer.OrdinalIgnoreCase)) names.Add(neutral);
        if (!names.Contains(Language, StringComparer.OrdinalIgnoreCase)) names.Add(Language);
        var overlays = new List<string>();
        foreach (string name in names)
        foreach (string directory in string.IsNullOrEmpty(UserDirectory) ? new[] { directories[owner] } :
            new[] { directories[owner], Path.Combine(UserDirectory!, owner) })
        {
            string path = Path.Combine(directory, name + ".json");
            try
            {
                if (!File.Exists(path)) continue;
                if (new FileInfo(path).Length > TranslationCatalog.MaxFileBytes) throw new IOException("Translation exceeds 1 MiB.");
                overlays.Add(File.ReadAllText(path));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { Log(owner + ": cannot read translation " + name + ": " + ex.Message); }
        }
        catalog.Select(Language, overlays.ToArray());
    }
}
