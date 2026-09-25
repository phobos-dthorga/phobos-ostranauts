using System;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

// Old explicit F6 section saves predate their industrial merchant categories.
// DEFAULT saves already inherit the current definition. Nothing else is migrated.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.SetData))]
internal static class EconomySaveCompatibility
{
    internal static JsonCondOwnerSave Upgrade(JsonCondOwnerSave saved)
    {
        if (saved?.strCODef != FurnaceRules.Section) return saved!;
        var conditions = saved.aConds ?? Array.Empty<string>();
        if (conditions.Contains("DEFAULT")) return saved;
        var flags = new[] { "IsCategoryIndustrialProducts", "IsSalvageValueHigh" };
        if (flags.All(flag => conditions.Contains(flag + "=1x1") || conditions.Contains(flag + "=1.0x1"))) return saved;
        var copy = NativeDefinitions.Clone(saved);
        copy.aConds = conditions.Where(c => !flags.Any(f => c.StartsWith(f + "=", StringComparison.Ordinal)))
            .Concat(flags.Select(f => f + "=1x1")).ToArray();
        copy.aCondReveals = null;
        return copy;
    }

    private static void Prefix(ref JsonCondOwnerSave jCOSIn)
    {
        if (jCOSIn != null) jCOSIn = Upgrade(jCOSIn);
    }
}
