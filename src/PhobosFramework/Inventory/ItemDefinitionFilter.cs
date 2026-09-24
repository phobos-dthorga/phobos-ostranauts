using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>An immutable, exact-ID allowlist. Unknown definitions fail closed.</summary>
public sealed class ItemDefinitionFilter
{
    private readonly HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
    public ItemDefinitionFilter(IEnumerable<string> definitionIds)
    {
        if (definitionIds == null) throw new ArgumentNullException(nameof(definitionIds));
        foreach (string id in definitionIds)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(Text.Get("ItemDefinitionFilter.filter_ids_must_be_nonempty"));
            ids.Add(id);
        }
    }
    public bool Allows(string? definitionId) => definitionId != null && ids.Contains(definitionId);
}
