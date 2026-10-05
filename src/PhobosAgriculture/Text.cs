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
        return Get(action);
    }
}
