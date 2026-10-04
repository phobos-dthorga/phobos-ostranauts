using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>The <c>materials</c> schema: the loose items a mod adds (stock, remainders, mined chunks, packets), keyed
/// by definition id: unit mass, price, stack, footprint side, market category, terminal flag and art name. The ids a
/// mod ships and the donor each clones stay in code. Since Framework 0.92.0 an owner may let override files add
/// materials of their own (<see cref="MaterialContext.AllowAdditions"/>): an added entry carries its own name, text and
/// image, and follows the same rules plus the owner's rule that trash is always a declared remainder.</summary>
public sealed class MaterialPack : DataPack
{
    public Dictionary<string, MaterialEntry> materials = new(StringComparer.Ordinal);
}

public sealed class MaterialEntry
{
    public string? notes;
    /// <summary>How the owner builds it: <c>stock</c>, <c>mined</c>, <c>packet</c> or another kind the owner names.</summary>
    public string kind = "stock";
    public double kg;
    public double price;
    public int stack = 1;
    public int side = 1;
    public string? category;
    /// <summary>A terminal remainder: never re-processed, priced at the technical minimum by authoring rule.</summary>
    public bool terminal;
    public string? art;
    /// <summary>Added materials only: the name and description shown when no translation names it.</summary>
    public string? name, description;
    /// <summary>Added materials only: the picture, as a path under an enabled mod's <c>images</c> folder without
    /// <c>.png</c> (for example <c>myaddon/SteelNugget</c>); a <c>...Normal.png</c> beside it is its normal map.</summary>
    public string? image;
}

public sealed class MaterialContext
{
    public IReadOnlyCollection<string> Known { get; }
    public IReadOnlyCollection<string>? Kinds { get; set; }
    /// <summary>Whether a file may add materials beyond the ones the owner knows (Framework 0.92.0).</summary>
    public bool AllowAdditions { get; set; }
    public MaterialContext(IReadOnlyCollection<string> known) { Known = known ?? throw new ArgumentNullException(nameof(known)); }
}

public static class MaterialSchema
{
    public const string Name = "materials";
    public const int MaximumStack = 1000, MaximumSide = 8;
    public const string TrashCategory = "IsCategoryTrash";
    /// <summary>The ids an added material may not start with: ours and the game's own.</summary>
    public static readonly IReadOnlyList<string> ReservedPrefixes = AddOns.ReservedPrefixes;
    private static bool IdOk(string id) => id.Length >= 3 && id.Length <= 64 && char.IsLetter(id[0]) && id.All(c => char.IsLetterOrDigit(c) && c < 128) &&
        !ReservedPrefixes.Any(r => id.StartsWith(r, StringComparison.OrdinalIgnoreCase));
    private static bool ImageOk(string? image) => !string.IsNullOrEmpty(image) && image!.Length <= 128 && !image.StartsWith("/", StringComparison.Ordinal) && !image.Contains("..") &&
        image.All(c => char.IsLetterOrDigit(c) && c < 128 || c == '/' || c == '_' || c == '-');
    public static void Validate(MaterialPack pack, MaterialContext context)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (context == null) throw new ArgumentNullException(nameof(context));
        foreach (string id in context.Known)
            if (!pack.materials.ContainsKey(id)) throw new ArgumentException(Text.Get("MaterialSchema.missing", id));
        foreach (var pair in pack.materials)
        {
            string id = pair.Key; var m = pair.Value;
            bool added = !context.Known.Contains(id);
            if (added)
            {
                if (!context.AllowAdditions) throw new ArgumentException(Text.Get("MaterialSchema.unknown", id));
                if (!IdOk(id)) throw new ArgumentException(Text.Get("MaterialSchema.added_id", id));
                if (string.IsNullOrWhiteSpace(m.name) || string.IsNullOrWhiteSpace(m.category) || !ImageOk(m.image)) throw new ArgumentException(Text.Get("MaterialSchema.added_fields", id));
                // Owner rule (4 October 2026): no trash without a consumer. Added trash is a terminal remainder, which
                // its owner declares for the reaction mass feeder.
                if (m.category == TrashCategory && !m.terminal) throw new ArgumentException(Text.Get("MaterialSchema.added_trash", id));
            }
            else if (m.name != null || m.description != null || m.image != null) throw new ArgumentException(Text.Get("MaterialSchema.shipped_fields", id));
            if (context.Kinds != null && !context.Kinds.Contains(m.kind)) throw new ArgumentException(Text.Get("MaterialSchema.kind", id, m.kind));
            if (!Finite(m.kg) || m.kg <= 0) throw new ArgumentException(Text.Get("MaterialSchema.positive", id, "kg"));
            if (!Finite(m.price) || m.price <= 0) throw new ArgumentException(Text.Get("MaterialSchema.positive", id, "price"));
            if (m.stack < 1 || m.stack > MaximumStack) throw new ArgumentException(Text.Get("MaterialSchema.stack", id, MaximumStack));
            if (m.side < 1 || m.side > MaximumSide) throw new ArgumentException(Text.Get("MaterialSchema.side", id, MaximumSide));
        }
    }
    private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
}
