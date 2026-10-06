using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>The <c>stores</c> schema (Framework 0.116.0; owner direction, 6 October 2026: the store pickers should not
/// offer a gun turret). It says which of the game's containers are not stores: a weapon's magazine, a charger's battery
/// slot, a scrubber's filter, a toilet. Store pickers, crew orders and housekeeping leave them out, and crew never take
/// from them. Everything is named with the game's own condition, container and interaction names, so add-on and other
/// mods' equipment can be covered by a player file.</summary>
public sealed class StorePack : DataPack
{
    /// <summary>The interaction a container must offer to count as a store (the game's <c>Inventory</c>, its "open"
    /// entry). Empty means any container may count.</summary>
    public string requireInteraction = "";
    /// <summary>Conditions that mark an object as not a store (<c>IsShipWeapon</c>, <c>IsToilet01</c>).</summary>
    public List<string> excludeConditions = new();
    /// <summary>Container rules (the game's <c>strContainerCT</c> trigger names) that hold only one kind of thing, such as
    /// a magazine's <c>TIsFitAmmo20mm</c>. A name ending in <c>*</c> covers every rule that starts with the rest.</summary>
    public List<string> excludeContainers = new();
}

public static class StoreSchema
{
    public const string Name = "stores";
    public const int MaxNames = 48;

    /// <summary>The checks every file passes, shipped or player.</summary>
    public static void Validate(StorePack pack)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (pack.requireInteraction == null || pack.requireInteraction.Length > 0 && !LineSchema.Identifier(pack.requireInteraction))
            throw new ArgumentException(Text.Get("StoreSchema.interaction"));
        if (!Names(pack.excludeConditions, LineSchema.Identifier)) throw new ArgumentException(Text.Get("StoreSchema.conditions", MaxNames));
        if (!Names(pack.excludeContainers, Container)) throw new ArgumentException(Text.Get("StoreSchema.containers", MaxNames));
    }
    /// <summary>A container rule name, or a prefix of one ending in <c>*</c> (at least three letters before it).</summary>
    public static bool Container(string? name) =>
        name != null && (name.EndsWith("*", StringComparison.Ordinal) ? name.Length > 3 && LineSchema.Identifier(name.Substring(0, name.Length - 1)) : LineSchema.Identifier(name));
    private static bool Names(List<string>? names, Func<string?, bool> valid) =>
        names != null && names.Count <= MaxNames && names.All(valid) && names.Distinct(StringComparer.Ordinal).Count() == names.Count;
}
