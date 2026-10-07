using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>An authored text that may say the same thing in several ways (Framework 0.132.0; owner direction,
/// 8 October 2026): in a pack it is a string (one variant, as every pack before this version wrote it) or a list of up
/// to <see cref="MaxVariants"/> strings, one of which is picked each time it is shown. Each variant has its own
/// translation key: the base key for the first, <c>&lt;key&gt;.2</c>, <c>.3</c> and on for the others.</summary>
[JsonConverter(typeof(TextVariantsConverter))]
public sealed class TextVariants
{
    public const int MaxVariants = 8;
    public static readonly TextVariants Empty = new();
    private readonly string[] variants;

    public TextVariants(params string[] variants) { this.variants = variants ?? Array.Empty<string>(); }
    public TextVariants(IEnumerable<string> variants) : this(variants?.ToArray() ?? Array.Empty<string>()) { }

    public IReadOnlyList<string> Variants => variants;
    public int Count => variants.Length;
    public string this[int index] => variants[index];
    /// <summary>The first variant, or "" when there is none.</summary>
    public string First => variants.Length > 0 ? variants[0] : "";
    public override string ToString() => First;

    /// <summary>A variant's translation key: the base key for the first (so a single text keeps the key it always had),
    /// <c>&lt;key&gt;.2</c> for the second, and on.</summary>
    public static string Key(string baseKey, int variant) => variant <= 0 ? baseKey : baseKey + "." + (variant + 1).ToString(CultureInfo.InvariantCulture);

    /// <summary>The variant at an index, held within the list ("" when there is none).</summary>
    public string At(int variant) => variants.Length == 0 ? "" : variants[Math.Max(0, Math.Min(variant, variants.Length - 1))];
}

/// <summary>Reads a <see cref="TextVariants"/> from a JSON string or an array of strings; anything else is refused with
/// the path, so the file is rejected as any malformed file is.</summary>
public sealed class TextVariantsConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(TextVariants);

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.Null: return null;
            case JsonToken.String: return new TextVariants((string)reader.Value!);
            case JsonToken.StartArray:
                var list = new List<string>();
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    if (reader.TokenType != JsonToken.String) throw new JsonSerializationException(Text.Get("StorySchema.variant_kind", reader.Path));
                    list.Add((string)reader.Value!);
                }
                return new TextVariants(list);
            default: throw new JsonSerializationException(Text.Get("StorySchema.variant_kind", reader.Path));
        }
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not TextVariants text) { writer.WriteNull(); return; }
        if (text.Count == 1) { writer.WriteValue(text[0]); return; }
        writer.WriteStartArray();
        foreach (var variant in text.Variants) writer.WriteValue(variant);
        writer.WriteEndArray();
    }
}

/// <summary>Which variant of a text to show when it is shown once and not drawn again (Framework 0.132.0): TV news,
/// adverts, small talk and other mods' lines such as the exchange's wire. A pick never repeats the last one for the same
/// key, the vanilla game's own no-repeat rule. The memory is in-memory only, like the game's, and holds one number per
/// key it has picked for (bounded by the loaded content); it is cleared when story content reloads.</summary>
public static class VariantPicks
{
    private static readonly Dictionary<string, int> last = new(StringComparer.Ordinal);

    /// <summary>The variant to show for <paramref name="key"/> among <paramref name="count"/>, from a roll in [0, 1).</summary>
    public static int Next(string key, int count, double roll)
    {
        if (count <= 1) return 0;
        int? previous = last.TryGetValue(key, out int p) ? p : null;
        int pick = StoryRules.Variant(count, roll, previous);
        last[key] = pick;
        return pick;
    }

    /// <summary>How many keys it remembers, for the performance footprint.</summary>
    public static int Count => last.Count;
    public static void Clear() => last.Clear();
}
