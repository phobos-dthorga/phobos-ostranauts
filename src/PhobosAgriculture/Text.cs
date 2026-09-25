using Phobos.Ostranauts.Framework.Localization;

namespace PhobosAgriculture;

internal static class Text
{
    internal const string Owner = "phobosgekko.ostranauts.agriculture";
    private static readonly TranslationCatalog Catalog = Translations.Register(Owner, typeof(Text).Assembly, "PhobosAgriculture.en.json", "PhobosAgriculture.equipment-names.json");
    internal static void EnsureLoaded() { _ = Catalog; }
    internal static string Get(string key, params object[] args) => Catalog.Get(key, args);
}
