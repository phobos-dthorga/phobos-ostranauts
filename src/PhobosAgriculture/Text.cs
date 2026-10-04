using Phobos.Ostranauts.Framework.Localization;

namespace PhobosAgriculture;

internal static class Text
{
    internal const string Owner = "phobosgekko.ostranauts.agriculture";
    private static readonly TranslationCatalog Catalog = Translations.Register(Owner, typeof(Text).Assembly, "PhobosAgriculture.en.json", "PhobosAgriculture.equipment-names.json");
    internal static void EnsureLoaded() { _ = Catalog; }
    internal static string Get(string key, params object[] args) => Catalog.Get(key, args);
    internal static bool Has(string key) => Catalog.Contains(key);
    /// <summary>The wording of an action. A crop a player file adds has no wording of its own, so its planting and feed
    /// actions are worded from its plain name.</summary>
    internal static string Action(string action)
    {
        if (Has(action)) return Get(action);
        if (Definitions.PlantCrop(action) is Core.Crop planted) return Get("plant_other", planted.Name);
        if (action.StartsWith(Definitions.MixPrefix, System.StringComparison.Ordinal) && Core.Crops.Find(action.Substring(Definitions.MixPrefix.Length)) is Core.Crop mixed) return Get("mix_other", mixed.Name);
        return Get(action);
    }
    /// <summary>A feed as players read it: plain water, a shipped feed, or an added crop's feed by its plain name.</summary>
    internal static string Feed(string profile) => Has("solution_" + profile) ? Get("solution_" + profile) :
        Core.Crops.ByFeed(profile) is Core.Crop c ? Get("solution_other", c.Name) : Get("solution_" + profile);
}
