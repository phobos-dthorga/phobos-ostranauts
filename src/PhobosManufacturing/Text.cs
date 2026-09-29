using Phobos.Ostranauts.Framework.Localization;

namespace PhobosManufacturing;

internal static class Text
{
    internal const string Owner = Core.ManufacturingRules.Owner;
    private static readonly TranslationCatalog Catalog = Translations.Register(Owner, typeof(Text).Assembly, "PhobosManufacturing.en.json", "PhobosManufacturing.equipment-names.json");
    internal static void EnsureLoaded() { _ = Catalog; }
    internal static string Get(string key, params object[] args) => Catalog.Get(key, args);
}
