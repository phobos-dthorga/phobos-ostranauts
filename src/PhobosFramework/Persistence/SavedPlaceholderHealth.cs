using System;
using System.Globalization;

namespace Phobos.Ostranauts.Framework.Persistence;

// A saved construction marker has the target's damage limit, while the generic
// Placeholder definition has none. Never turn ambiguous records into free repair.
internal static class SavedPlaceholderHealth
{
    internal static bool HasRemainingHealth(double? damageOverride, string[]? conditions)
    {
        if (!damageOverride.HasValue || !FiniteNonnegative(damageOverride.Value) || conditions == null ||
            !TryAmount(conditions, "IsPlaceholder", out double marker) || marker != 1 ||
            !TryAmount(conditions, "StatDamageMax", out double maximum) || maximum <= 0 ||
            !TryAmount(conditions, "StatDamage", out double damage)) return false;
        return damageOverride.Value < maximum && damage < maximum;
    }

    private static bool TryAmount(string[] conditions, string name, out double value)
    {
        value = 0;
        bool found = false;
        foreach (string condition in conditions)
        {
            if (condition == null || !condition.StartsWith(name + "=", StringComparison.Ordinal)) continue;
            if (found) return false;
            found = true;
            var terms = condition.Substring(name.Length + 1).Split('x');
            if (terms.Length != 2 || !double.TryParse(terms[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double chance) ||
                chance != 1 || !double.TryParse(terms[1], NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                !FiniteNonnegative(value)) return false;
        }
        return found;
    }

    private static bool FiniteNonnegative(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
}
