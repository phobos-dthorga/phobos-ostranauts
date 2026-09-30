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
/// valid on its own. Unknown fields are refused everywhere, as the construction registry refuses them.</summary>
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

    /// <summary>Loads the shipped pack, applies valid player files and returns the result. <paramref name="validate"/>
    /// runs on the shipped pack (a failure throws) and on every candidate overlay (a failure rejects that file).</summary>
    public static T Load<T>(DataPackSource source, Action<T> validate) where T : DataPack
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return LoadText(ReadResource(source.Assembly, source.ResourceName), UserDirectory(source), source.Owner, source.Schema, validate);
    }

    /// <summary>The same load from the shipped pack's text and an explicit player folder (empty for none). Tools and
    /// offline checks use this; the game uses <see cref="Load{T}"/>.</summary>
    public static T LoadText<T>(string shippedJson, string userDirectory, string owner, string schema, Action<T> validate) where T : DataPack
    {
        if (validate == null) throw new ArgumentNullException(nameof(validate));
        string key = owner + "/" + schema;
        var merged = Parse(shippedJson, schema, shipped: true);
        var current = Materialize<T>(merged);
        validate(current);
        int applied = 0, rejected = 0;
        if (!string.IsNullOrEmpty(userDirectory))
        {
            foreach (string path in UserFiles(userDirectory, owner, schema))
            {
                string file = Path.GetFileName(path);
                try
                {
                    if (new FileInfo(path).Length > MaxFileBytes) throw new FormatException(Text.Get("DataPacks.file_too_large"));
                    var overlay = Parse(File.ReadAllText(path), schema, shipped: false);
                    RefuseNulls(overlay, "");
                    var candidate = (JObject)merged.DeepClone();
                    candidate.Merge(overlay, Merge);
                    var result = Materialize<T>(candidate);
                    validate(result);
                    merged = candidate; current = result; applied++;
                }
                catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
                {
                    rejected++;
                    problems.Add(new DataPackProblem(owner, schema, file, ex.Message));
                    Log(Text.Get("DataPacks.rejected_file", schema, file, ex.Message));
                }
            }
        }
        status[key] = Text.Get("DataPacks.status_line", applied, rejected, string.IsNullOrEmpty(userDirectory) ? Text.Get("DataPacks.no_override_folder") : userDirectory);
        return current;
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
