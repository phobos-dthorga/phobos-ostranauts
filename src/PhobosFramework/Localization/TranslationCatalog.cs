using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Phobos.Ostranauts.Framework.Story;

namespace Phobos.Ostranauts.Framework.Localization;

/// <summary>One translation file's text, and which keys beyond the catalog's own it may add (Framework 0.91.0): an
/// add-on's file may name the things that add-on adds, under its own id prefix, and nothing else new.</summary>
public sealed class TranslationOverlay
{
    public string Json { get; }
    public Func<string, bool>? MayAdd { get; }
    public TranslationOverlay(string json, Func<string, bool>? mayAdd = null) { Json = json ?? ""; MayAdd = mayAdd; }
}

/// <summary>Content-owned JSON messages with an embedded English safety fallback.</summary>
public sealed class TranslationCatalog
{
    public const int MaxFileBytes = 1048576;
    public const int MaxMessageCharacters = 16384;
    public const int MaxArguments = 64, MaxAlignment = 1024, MaxNumericPrecision = 100;
    private readonly Dictionary<string, string> english;
    private readonly Dictionary<string, string> selected = new Dictionary<string, string>(StringComparer.Ordinal);
    // Keys add-ons add for the things they add; the last language level loaded wins, as for ordinary keys.
    private readonly Dictionary<string, string> added = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
    // The variant last shown per key with variants (Framework 0.134.0), in memory only: at most one entry per such key.
    private readonly Dictionary<string, int> lastPick = new Dictionary<string, int>(StringComparer.Ordinal);
    private static readonly Regex VariantKey = new Regex(@"^(.+)\.([0-9]+)$", RegexOptions.CultureInvariant);
    private readonly Action<string> log;
    private readonly EquipmentNames? equipment;
    public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("en");
    public bool Contains(string key) => english.ContainsKey(key) || added.ContainsKey(key);

    public TranslationCatalog(string englishJson, Action<string>? log = null) : this(englishJson, log, null) { }

    public TranslationCatalog(string englishJson, Action<string>? log, EquipmentNames? equipment)
    {
        this.log = log ?? (_ => { });
        this.equipment = equipment;
        english = Parse(englishJson);
        foreach (string message in english.Values) Signature(message);
        CheckVariants(english);
        if (equipment != null)
        foreach (string key in equipment.Keys)
            if (!english.TryGetValue(key, out var description) || !equipment.IsDescription(key, description))
                throw new FormatException("Equipment key needs an unbranded English description: " + key);
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

    /// <summary>Framework 0.134.0: a message may have variants, written as further keys <c>&lt;key&gt;.2</c>, <c>.3</c>
    /// and on (<see cref="TextVariants.Key"/>), up to <see cref="TextVariants.MaxVariants"/> in all. A numbered key
    /// whose base key exists is a variant: it must take the same arguments as the base and follow the one before it.
    /// A packaging fault otherwise, refused when the catalog loads.</summary>
    private static void CheckVariants(Dictionary<string, string> messages)
    {
        foreach (var pair in messages)
        {
            var match = VariantKey.Match(pair.Key);
            if (!match.Success || !messages.TryGetValue(match.Groups[1].Value, out string? first)) continue;
            string baseKey = match.Groups[1].Value;
            if (!int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int number) || number < 2 || number > TextVariants.MaxVariants)
                throw new FormatException("Variant " + pair.Key + ": variants are numbered 2 to " + TextVariants.MaxVariants + ".");
            if (!messages.ContainsKey(TextVariants.Key(baseKey, number - 2)))
                throw new FormatException("Variant " + pair.Key + " has no variant before it.");
            if (Signature(pair.Value) != Signature(first))
                throw new FormatException("Variant " + pair.Key + " takes different arguments from " + baseKey + ".");
        }
    }

    /// <summary>How many variants a message has (Framework 0.134.0): 1 for a message without any, 0 for an unknown key.</summary>
    public int Variants(string key)
    {
        if (!english.ContainsKey(key)) return added.ContainsKey(key) ? 1 : 0;
        int count = 1;
        while (count < TextVariants.MaxVariants && english.ContainsKey(TextVariants.Key(key, count))) count++;
        return count;
    }

    /// <summary>One variant of a message, counting from 0 (the base key), held to the variants it has. Each variant
    /// translates under its own key; one a translation leaves out shows in English.</summary>
    public string GetVariant(string key, int variant, params object[] args)
    {
        int count = Variants(key);
        return Get(count <= 1 ? key : TextVariants.Key(key, Math.Max(0, Math.Min(count - 1, variant))), args);
    }

    /// <summary>A message for a line shown once and repeated over time (a wire report, a routine notice): one of its
    /// variants by <paramref name="roll"/> (0 to 1), never the one this catalog showed last for the key
    /// (<see cref="StoryRules.Variant(int, double, int?)"/>). A message without variants is <see cref="Get"/>.</summary>
    public string Pick(string key, double roll, params object[] args)
    {
        int count = Variants(key);
        if (count <= 1) return Get(key, args);
        int variant = StoryRules.Variant(count, roll, lastPick.TryGetValue(key, out int previous) ? previous : null);
        lastPick[key] = variant;
        return GetVariant(key, variant, args);
    }

    /// <summary>Keys whose last shown variant is remembered (a performance footprint).</summary>
    public int PickCount => lastPick.Count;

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

    public void Select(string language, params string[] overlays) => Select(language, overlays.Select(json => new TranslationOverlay(json)));

    public void Select(string language, IEnumerable<TranslationOverlay> overlays)
    {
        selected.Clear(); added.Clear(); reported.Clear();
        try { Culture = CultureInfo.GetCultureInfo(language); }
        catch (CultureNotFoundException) { Culture = CultureInfo.GetCultureInfo("en"); }
        foreach (var overlay in overlays)
        {
            Dictionary<string, string> entries;
            try { entries = Parse(overlay.Json); }
            catch (Exception ex) when (ex is JsonException || ex is FormatException)
            { Warn("file:" + ex.Message, "Invalid translation file; retaining fallback: " + ex.Message); continue; }
            foreach (var entry in entries)
            {
                if (!english.TryGetValue(entry.Key, out string original))
                {
                    // A new key is taken only from a file allowed to add it (an add-on naming its own additions).
                    if (overlay.MayAdd?.Invoke(entry.Key) == true)
                    {
                        try { Signature(entry.Value); added[entry.Key] = entry.Value; }
                        catch (FormatException ex) { Warn(entry.Key, "Invalid added text " + entry.Key + ": " + ex.Message); }
                    }
                    else Warn(entry.Key, "Unknown translation key: " + entry.Key);
                    continue;
                }
                try
                {
                    if (Signature(original) != Signature(entry.Value)) throw new FormatException("Argument set differs.");
                    if (equipment != null && !equipment.IsDescription(entry.Key, entry.Value))
                        throw new FormatException("Equipment translations contain the type/variant, not the maker prefix.");
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
        {
            if (added.TryGetValue(key, out var extra))
            {
                try { return string.Format(Culture, extra, args); }
                catch (FormatException) { return extra; }
            }
            Warn(key, "Missing English translation key: " + key); return "[" + key + "]";
        }
        string format = selected.TryGetValue(key, out var translated) ? translated : original;
        string Name(string value) => equipment?.Format(key, value) ?? value;
        try { return Name(string.Format(Culture, format, args)); }
        catch (FormatException)
        {
            Warn(key, "Translation formatting failed; using English: " + key);
            try { return Name(string.Format(CultureInfo.GetCultureInfo("en"), original, args)); }
            catch (FormatException) { return Name(original); }
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
    /// <summary>Translation folders from enabled add-ons for one owner, in load order, each with the keys that add-on
    /// may add (Framework 0.91.0). Null means none. Read between the packaged catalog and the player's own folder.</summary>
    public static Func<string, IEnumerable<(string Directory, Func<string, bool> MayAdd)>>? AddOnDirectories { get; set; }
    /// <summary>Reads every catalog's files again: called when the game's mod list is known, so add-on translations load.</summary>
    public static void Reload() { foreach (var pair in catalogs) Load(pair.Key, pair.Value); }

    public static TranslationCatalog Register(string owner, Assembly assembly, string resource)
        => Register(owner, assembly, resource, null);

    public static TranslationCatalog Register(string owner, Assembly assembly, string resource, string? equipmentResource)
    {
        if (!Regex.IsMatch(owner, "^[A-Za-z][A-Za-z0-9_.-]{0,199}$")) throw new ArgumentException("Invalid translation owner.");
        if (catalogs.TryGetValue(owner, out var found)) return found;
        using var stream = assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException("Missing English resource: " + resource);
        using var reader = new StreamReader(stream);
        EquipmentNames? equipment = null;
        if (equipmentResource != null)
        {
            using var names = assembly.GetManifestResourceStream(equipmentResource) ?? throw new InvalidOperationException("Missing equipment names: " + equipmentResource);
            using var nameReader = new StreamReader(names);
            equipment = new EquipmentNames(nameReader.ReadToEnd());
        }
        var catalog = new TranslationCatalog(reader.ReadToEnd(), message => Log(owner + ": " + message), equipment);
        catalogs.Add(owner, catalog);
        directories.Add(owner, Path.Combine(Path.GetDirectoryName(assembly.Location)!, "translations"));
        Load(owner, catalog);
        return catalog;
    }

    public static string Get(string owner, string key, string fallback) =>
        catalogs.TryGetValue(owner, out var catalog) && catalog.Contains(key) ? catalog.Get(key) : fallback;

    /// <summary>Keys with variants whose last shown variant the catalogs remember (Framework 0.134.0).</summary>
    public static int VariantPickCount => catalogs.Values.Sum(c => c.PickCount);

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
        // At each language level: the packaged file, then add-ons in load order, then the player's own file.
        var sources = new List<(string Directory, Func<string, bool>? MayAdd)> { (directories[owner], null) };
        try { if (AddOnDirectories != null) foreach (var source in AddOnDirectories(owner)) sources.Add((source.Directory, source.MayAdd)); }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) { Log(owner + ": cannot read add-on translations: " + ex.Message); }
        if (!string.IsNullOrEmpty(UserDirectory)) sources.Add((Path.Combine(UserDirectory!, owner), null));
        var overlays = new List<TranslationOverlay>();
        foreach (string name in names)
        foreach (var source in sources)
        {
            string path = Path.Combine(source.Directory, name + ".json");
            try
            {
                if (!File.Exists(path)) continue;
                if (new FileInfo(path).Length > TranslationCatalog.MaxFileBytes) throw new IOException("Translation exceeds 1 MiB.");
                overlays.Add(new TranslationOverlay(File.ReadAllText(path), source.MayAdd));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { Log(owner + ": cannot read translation " + name + ": " + ex.Message); }
        }
        catalog.Select(Language, overlays);
    }
}
