using System;
using System.Linq;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>An optional Phobos mod whose item identities a recipe needs: the plugin must be loaded at or above a
/// minimum version and must have published each identity at the mass the recipe expects. A soft dependency makes the
/// provider load first, so its definitions exist, or not, when Manufacturing registers. Recipes that need it stay
/// unavailable without it; nothing is required.</summary>
internal sealed class OptionalStock
{
    private readonly (string Id, double Kg)[] items;
    internal string PluginId { get; }
    internal Version Minimum { get; }
    internal bool PluginPresent { get; private set; }
    internal bool Available { get; private set; }
    internal OptionalStock(string pluginId, Version minimum, params (string Id, double Kg)[] items) { PluginId = pluginId; Minimum = minimum; this.items = items; }
    internal void Detect() => PluginPresent = BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(PluginId, out var plugin) && plugin.Metadata.Version >= Minimum;
    internal bool Definitions() => DataHandler.dictCOs != null && items.All(i => DataHandler.dictCOs.TryGetValue(i.Id, out var co) && Mass(co) == i.Kg);
    internal void Resolve() => Available = PluginPresent && Definitions();
    internal void Reset() => Available = false;
    /// <summary>The starting StatMass of a definition, from its "StatMass=1x4" condition string.</summary>
    internal static double Mass(JsonCondOwner co)
    {
        foreach (string s in co.aStartingConds ?? Array.Empty<string>())
            if (s.StartsWith("StatMass=", StringComparison.Ordinal) && double.TryParse(s.Substring(s.LastIndexOf('x') + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double kg)) return kg;
        return 0;
    }
}

/// <summary>The optional Shipbreaker gate for the steel recipe: Shipbreaker 0.38.0 or newer with its steel ingot and
/// remainder at the masses the recipe expects.</summary>
internal static class ShipbreakerStock
{
    internal const string PluginId = "phobosgekko.ostranauts.shipbreaker";
    internal static readonly Version Minimum = new(0, 38, 0);
    private static readonly OptionalStock provider = new(PluginId, Minimum, (RefineryRules.SteelIngot, RefineryRules.SteelIngotKg), (RefineryRules.SteelRemainder, RefineryRules.SteelRemainderKg));
    internal static bool PluginPresent => provider.PluginPresent;
    internal static bool Available => provider.Available;
    internal static void Detect() => provider.Detect();
    internal static bool Definitions() => provider.Definitions();
    internal static double Mass(JsonCondOwner co) => OptionalStock.Mass(co);
    internal static void Resolve() => provider.Resolve();
    internal static void Reset() => provider.Reset();
}

/// <summary>The optional Agriculture gate for the LC-3's makeup formulation: Agriculture 0.9.0 or newer (the release
/// that added the Groundwork makeup packet) with the packet at 40 g. Without it the salts stay stock.</summary>
internal static class AgricultureStock
{
    internal const string PluginId = "phobosgekko.ostranauts.agriculture";
    internal static readonly Version Minimum = new(0, 9, 0);
    private static readonly OptionalStock provider = new(PluginId, Minimum, (LeachRules.MakeupPacket, LeachRules.MakeupPacketKg));
    /// <summary>The nutrient hopper arrived in Agriculture 0.27.0; its vessels are found by commodity at link time.</summary>
    internal static readonly Version HopperMinimum = new(0, 27, 0);
    private static readonly OptionalStock hoppers = new(PluginId, HopperMinimum);
    /// <summary>The straw bale arrived in Agriculture 0.44.0, at 1 kg (the V4's straw charges).</summary>
    internal static readonly Version StrawMinimum = new(0, 44, 0);
    private static readonly OptionalStock straw = new(PluginId, StrawMinimum, (RefineryRules.StrawBale, RefineryRules.StrawBaleKg));
    internal static bool PluginPresent => provider.PluginPresent;
    internal static bool Available => provider.Available;
    /// <summary>Whether the LC-3's complete formulation can deposit into Agriculture hoppers.</summary>
    internal static bool Hoppers => hoppers.Available;
    /// <summary>Whether the V4's straw charges can bind Agriculture's bale.</summary>
    internal static bool Straw => straw.Available;
    internal static void Detect() { provider.Detect(); hoppers.Detect(); straw.Detect(); }
    internal static bool Definitions() => provider.Definitions();
    /// <summary>Whether Agriculture's straw bale is published at the 1 kg the straw charges expect.</summary>
    internal static bool StrawDefinitions() => straw.Definitions();
    internal static void Resolve() { provider.Resolve(); hoppers.Resolve(); straw.Resolve(); }
    internal static void Reset() { provider.Reset(); hoppers.Reset(); straw.Reset(); }
}
