using Phobos.Ostranauts.Framework.Localization;

namespace PhobosAutoNav;

internal static class Text
{
    internal const string Owner = "phobosgekko.ostranauts.autonav";
    private static readonly TranslationCatalog Catalog = Translations.Register(Owner, typeof(Text).Assembly, "PhobosAutoNav.en.json", "PhobosAutoNav.equipment-names.json");
    internal static void EnsureLoaded() { _ = Catalog; }
    internal static string Get(string key, params object[] args) => Catalog.Get(key, args);
}
