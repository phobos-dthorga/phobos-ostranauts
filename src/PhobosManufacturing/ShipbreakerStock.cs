using System;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>The optional Shipbreaker gate for the steel recipe: Shipbreaker 0.38.0 or newer must be loaded and
/// must have published its steel ingot and remainder at the masses the recipe expects. Shipbreaker's soft
/// dependency makes it load first, so its definitions exist, or not, when Manufacturing registers.</summary>
internal static class ShipbreakerStock
{
    internal const string PluginId = "phobosgekko.ostranauts.shipbreaker";
    internal static readonly Version Minimum = new(0, 38, 0);
    internal static bool PluginPresent { get; private set; }
    internal static bool Available { get; private set; }
    internal static void Detect() => PluginPresent = BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(PluginId, out var plugin) && plugin.Metadata.Version >= Minimum;
    internal static bool Definitions() => DataHandler.dictCOs != null &&
        DataHandler.dictCOs.TryGetValue(RefineryRules.SteelIngot, out var ingot) && Mass(ingot) == RefineryRules.SteelIngotKg &&
        DataHandler.dictCOs.TryGetValue(RefineryRules.SteelRemainder, out var remainder) && Mass(remainder) == RefineryRules.SteelRemainderKg;
    /// <summary>The starting StatMass of a definition, from its "StatMass=1x4" condition string.</summary>
    internal static double Mass(JsonCondOwner co)
    {
        foreach (string s in co.aStartingConds ?? Array.Empty<string>())
            if (s.StartsWith("StatMass=", StringComparison.Ordinal) && double.TryParse(s.Substring(s.LastIndexOf('x') + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double kg)) return kg;
        return 0;
    }
    internal static void Resolve() => Available = PluginPresent && Definitions();
    internal static void Reset() => Available = false;
}
