using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>The one loader for Phobos data packs (owner decision, 30 September 2026). A schema's pack is read from the
/// owning assembly's embedded resource, validated, then overlaid by player files from
/// <c>BepInEx/config/&lt;ModFolder&gt;/&lt;schema&gt;/*.json</c> in name order. Player files are partial packs merged
/// by property name at every level (objects merge, arrays replace), so a file may tune fields of a shipped entry or
/// add an entry; it can never rename or remove one (a null value is refused). Each player file is validated on top of
/// everything accepted before it; a file that fails is reported and skipped, never fatal. The shipped pack must be
/// valid on its own. Unknown fields are refused everywhere, as the construction registry refuses them. Since Framework
/// 0.90.0 the same files may also come from enabled add-ons (<see cref="AddOns"/>), and any file may set its order with
/// a <c>priority</c> header.</summary>
public static class DataPacks
{
    public const int MaxFileBytes = 1048576, MaxDepth = 16;
    /// <summary>The BepInEx config folder; empty disables player overrides (offline checks).</summary>
    public static string UserRoot { get; set; } = "";
    public static Action<string> Log { get; set; } = _ => { };
    private static readonly List<DataPackProblem> problems = new();
    private static readonly Dictionary<string, string> status = new(StringComparer.Ordinal);
    private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.None, MissingMemberHandling = MissingMemberHandling.Error, MaxDepth = MaxDepth,
        DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Double
    });
    private static readonly JsonMergeSettings Merge = new()
    {
        MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Ignore, PropertyNameComparison = StringComparison.Ordinal
    };

    public static IReadOnlyList<DataPackProblem> Problems => problems;
    internal static void Reset() { problems.Clear(); status.Clear(); }

    /// <summary>The player override folder for a source, whether or not it exists yet.</summary>
    public static string UserDirectory(DataPackSource source) => UserRoot.Length == 0 ? "" : Path.Combine(UserRoot, source.ModFolder, source.Schema);

    /// <summary>The shipped pack's own text, before any player file (a read-only baseline for owners that lock a pack).</summary>
    public static string ShippedText(DataPackSource source) => source == null ? throw new ArgumentNullException(nameof(source)) : ReadResource(source.Assembly, source.ResourceName);

    /// <summary>Loads the shipped pack, applies valid add-on and player files and returns the result. <paramref name="validate"/>
    /// runs on the shipped pack (a failure throws) and on every candidate overlay (a failure rejects that file).</summary>
    public static T Load<T>(DataPackSource source, Action<T> validate) where T : DataPack
    {
        if (validate == null) throw new ArgumentNullException(nameof(validate));
        return Load<T>(source, (pack, _) => validate(pack));
    }
    /// <summary>As <see cref="Load{T}(DataPackSource, Action{T})"/>, with the validator also given the merged raw JSON
    /// (for checks defined over the file text, such as the recipe revision freeze).</summary>
    public static T Load<T>(DataPackSource source, Action<T, JObject> validate) where T : DataPack
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        // Add-ons whose requirement of this mod is met (Framework 0.90.0), by the owning assembly's own version.
        string installed = source.Assembly.GetName().Version?.ToString() ?? "0.0";
        return LoadText(ReadResource(source.Assembly, source.ResourceName), UserDirectory(source), AddOns.For(source.ModFolder, installed).ToArray(), source.ModFolder, source.Owner, source.Schema, validate);
    }

    /// <summary>The same load from the shipped pack's text and an explicit player folder (empty for none). Tools and
    /// offline checks use this; the game uses <see cref="Load{T}"/>.</summary>
    public static T LoadText<T>(string shippedJson, string userDirectory, string owner, string schema, Action<T> validate) where T : DataPack
    {
        if (validate == null) throw new ArgumentNullException(nameof(validate));
        return LoadText<T>(shippedJson, userDirectory, owner, schema, (pack, _) => validate(pack));
    }
    public static T LoadText<T>(string shippedJson, string userDirectory, string owner, string schema, Action<T, JObject> validate) where T : DataPack =>
        LoadText(shippedJson, userDirectory, Array.Empty<AddOn>(), "", owner, schema, validate);

    /// <summary>The lowest and highest <c>priority</c> an override file may give itself.</summary>
    public const int MinPriority = -100, MaxPriority = 100;
    private sealed class Candidate
    {
        internal string Label = ""; internal AddOn? AddOn; internal int Priority; internal string Name = ""; internal JObject Overlay = null!;
    }
    /// <summary>Schema-specific preparation of an override file before it is merged: given the file and the pack so far.</summary>
    private static readonly Dictionary<string, Action<JObject, JObject>> Preparers = new(StringComparer.Ordinal) { [RecipeSchema.Name] = RecipeSchema.PrepareOverlay };

    /// <summary>The full load (Framework 0.90.0): the shipped pack, then every override file from the add-ons and the
    /// player's own folder. Files apply by their <c>priority</c> header (lowest first, default 0), then add-ons before
    /// the player's files, add-ons in the game's load order, and files by name. So a player's own file has the last
    /// word unless a file says otherwise. An add-on file may add only entries in its own id namespace. Each file is
    /// validated on top of everything accepted before it; a file that fails is reported and skipped.</summary>
    public static T LoadText<T>(string shippedJson, string userDirectory, IReadOnlyList<AddOn> addOns, string modFolder, string owner, string schema, Action<T, JObject> validate) where T : DataPack
    {
        if (validate == null) throw new ArgumentNullException(nameof(validate));
        string key = owner + "/" + schema;
        var merged = Parse(shippedJson, schema, shipped: true);
        var current = Materialize<T>(merged);
        validate(current, merged);
        int applied = 0, addOnApplied = 0, rejected = 0;
        void Reject(string label, string message)
        {
            rejected++;
            problems.Add(new DataPackProblem(owner, schema, label, message));
            Log(Text.Get("DataPacks.rejected_file", schema, label, message));
        }
        var candidates = new List<Candidate>();
        void Read(string path, AddOn? addOn)
        {
            string file = Path.GetFileName(path), label = addOn == null ? file : addOn.Manifest.id + "/" + file;
            try
            {
                if (new FileInfo(path).Length > MaxFileBytes) throw new FormatException(Text.Get("DataPacks.file_too_large"));
                var overlay = Parse(File.ReadAllText(path), schema, shipped: false);
                RefuseNulls(overlay, "");
                candidates.Add(new Candidate { Label = label, AddOn = addOn, Name = file, Overlay = overlay, Priority = TakePriority(overlay) });
            }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is JsonException || ex is IOException || ex is UnauthorizedAccessException) { Reject(label, ex.Message); }
        }
        foreach (var addOn in addOns ?? Array.Empty<AddOn>())
            foreach (string path in AddOnFiles(addOn.Folder(modFolder, schema), owner, schema)) Read(path, addOn);
        if (!string.IsNullOrEmpty(userDirectory))
            foreach (string path in UserFiles(userDirectory, owner, schema)) Read(path, null);
        foreach (var candidate in candidates.OrderBy(c => c.Priority).ThenBy(c => c.AddOn == null ? 1 : 0).ThenBy(c => c.AddOn?.Order ?? 0).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (candidate.AddOn != null) RequireOwnIds(candidate.Overlay, merged, candidate.AddOn.Manifest);
                if (Preparers.TryGetValue(schema, out var prepare)) prepare(candidate.Overlay, merged);
                var next = (JObject)merged.DeepClone();
                next.Merge(candidate.Overlay, Merge);
                var result = Materialize<T>(next);
                validate(result, next);
                merged = next; current = result;
                if (candidate.AddOn == null) applied++; else addOnApplied++;
            }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is JsonException) { Reject(candidate.Label, ex.Message); }
        }
        status[key] = Text.Get("DataPacks.status_line", applied, rejected, string.IsNullOrEmpty(userDirectory) ? Text.Get("DataPacks.no_override_folder") : userDirectory) +
            (addOnApplied > 0 ? " " + Text.Get("DataPacks.status_addons", addOnApplied) : "");
        return current;
    }

    /// <summary>Reads and removes an override file's <c>priority</c> header: a whole number, 0 when absent.</summary>
    private static int TakePriority(JObject overlay)
    {
        var token = overlay["priority"];
        if (token == null) return 0;
        if (token.Type != JTokenType.Integer || (long)token < MinPriority || (long)token > MaxPriority) throw new FormatException(Text.Get("DataPacks.priority", MinPriority, MaxPriority));
        overlay.Remove("priority");
        return (int)token;
    }
    /// <summary>An add-on may tune any entry, but an entry it adds must carry its own id prefix: every new key in the
    /// pack's top-level tables (recipes, tables, crops, materials) starts with it.</summary>
    private static void RequireOwnIds(JObject overlay, JObject merged, AddOnManifest manifest)
    {
        foreach (var table in overlay.Properties())
        {
            if (!(table.Value is JObject entries)) continue;
            var existing = merged[table.Name] as JObject;
            foreach (var entry in entries.Properties())
                if (existing?[entry.Name] == null && !AddOns.Owns(manifest, entry.Name))
                    throw new FormatException(Text.Get("DataPacks.addon_prefix", entry.Name, manifest.idPrefix));
        }
    }
    private static IEnumerable<string> AddOnFiles(string directory, string owner, string schema)
    {
        try { return Directory.Exists(directory) ? Directory.GetFiles(directory, "*.json").OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase).ToArray() : Array.Empty<string>(); }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
        {
            problems.Add(new DataPackProblem(owner, schema, directory, ex.Message));
            return Array.Empty<string>();
        }
    }

    private static IEnumerable<string> UserFiles(string directory, string owner, string schema)
    {
        try
        {
            Directory.CreateDirectory(directory);
            return Directory.GetFiles(directory, "*.json").OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            problems.Add(new DataPackProblem(owner, schema, directory, ex.Message));
            Log(Text.Get("DataPacks.no_folder", directory, ex.Message));
            return Array.Empty<string>();
        }
    }

    private static string ReadResource(System.Reflection.Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(Text.Get("DataPacks.missing_resource", name));
        if (stream.Length > MaxFileBytes) throw new InvalidOperationException(Text.Get("DataPacks.file_too_large"));
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        return reader.ReadToEnd();
    }

    /// <summary>Strict parse: one object, no duplicate properties, nothing after it, bounded depth, and the schema
    /// header present on a shipped pack (a player file may repeat it, and then it must agree).</summary>
    public static JObject Parse(string json, string schema, bool shipped)
    {
        if (Encoding.UTF8.GetByteCount(json ?? "") > MaxFileBytes) throw new FormatException(Text.Get("DataPacks.file_too_large"));
        using var reader = new JsonTextReader(new StringReader(json ?? "")) { MaxDepth = MaxDepth, DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Double };
        JObject root;
        try { root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error, CommentHandling = CommentHandling.Ignore }); }
        catch (JsonException ex) { throw new FormatException(ex.Message); }
        if (reader.Read()) throw new FormatException(Text.Get("DataPacks.trailing_content"));
        var version = root["schemaVersion"]; var name = root["schema"];
        if (shipped && (version == null || name == null)) throw new FormatException(Text.Get("DataPacks.header_missing"));
        if (version != null && (version.Type != JTokenType.Integer || (int)version != 1)) throw new FormatException(Text.Get("DataPacks.unsupported_version", version.ToString()));
        if (name != null && (name.Type != JTokenType.String || (string?)name != schema)) throw new FormatException(Text.Get("DataPacks.wrong_schema", name.ToString(), schema));
        return root;
    }

    private static T Materialize<T>(JObject root) where T : DataPack
    {
        try { return root.ToObject<T>(Serializer) ?? throw new FormatException(Text.Get("DataPacks.empty_pack")); }
        catch (JsonException ex) { throw new FormatException(ex.Message); }
    }

    private static void RefuseNulls(JToken token, string path)
    {
        switch (token)
        {
            case JObject o: foreach (var p in o.Properties()) RefuseNulls(p.Value, path + "/" + p.Name); break;
            case JArray a: for (int i = 0; i < a.Count; i++) RefuseNulls(a[i], path + "/" + i); break;
            case JValue v when v.Type == JTokenType.Null: throw new FormatException(Text.Get("DataPacks.null_value", path));
        }
    }

    /// <summary>Console status: every loaded pack with its applied and rejected player files, then the problems.</summary>
    public static string Describe()
    {
        var lines = status.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + ": " + p.Value).ToList();
        if (lines.Count == 0) lines.Add(Text.Get("DataPacks.none_loaded"));
        foreach (var problem in problems) lines.Add(Text.Get("DataPacks.problem_line", problem.Owner, problem.Schema, problem.File, problem.Message));
        return string.Join("\n", lines);
    }
}
