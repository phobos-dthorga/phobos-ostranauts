using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>The <c>materials</c> schema: the loose items a mod adds (stock, remainders, mined chunks, packets), keyed
/// by definition id: unit mass, price, stack, footprint side, market category, terminal flag and art name. Ids and
/// the donor each clones stay in code, so a player file can tune an entry but cannot add one.</summary>
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
}

public sealed class MaterialContext
{
    public IReadOnlyCollection<string> Known { get; }
    public IReadOnlyCollection<string>? Kinds { get; set; }
    public MaterialContext(IReadOnlyCollection<string> known) { Known = known ?? throw new ArgumentNullException(nameof(known)); }
}

public static class MaterialSchema
{
    public const string Name = "materials";
    public const int MaximumStack = 1000, MaximumSide = 8;
    public static void Validate(MaterialPack pack, MaterialContext context)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (context == null) throw new ArgumentNullException(nameof(context));
        foreach (string id in context.Known)
            if (!pack.materials.ContainsKey(id)) throw new ArgumentException(Text.Get("MaterialSchema.missing", id));
        foreach (var pair in pack.materials)
        {
            string id = pair.Key; var m = pair.Value;
            if (!context.Known.Contains(id)) throw new ArgumentException(Text.Get("MaterialSchema.unknown", id));
            if (context.Kinds != null && !context.Kinds.Contains(m.kind)) throw new ArgumentException(Text.Get("MaterialSchema.kind", id, m.kind));
            if (!Finite(m.kg) || m.kg <= 0) throw new ArgumentException(Text.Get("MaterialSchema.positive", id, "kg"));
            if (!Finite(m.price) || m.price <= 0) throw new ArgumentException(Text.Get("MaterialSchema.positive", id, "price"));
            if (m.stack < 1 || m.stack > MaximumStack) throw new ArgumentException(Text.Get("MaterialSchema.stack", id, MaximumStack));
            if (m.side < 1 || m.side > MaximumSide) throw new ArgumentException(Text.Get("MaterialSchema.side", id, MaximumSide));
        }
    }
    private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
}
