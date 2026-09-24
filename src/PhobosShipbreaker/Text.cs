using Phobos.Ostranauts.Framework.Localization;

namespace PhobosShipbreaker;

internal static class Text
{
    internal const string Owner = "phobosgekko.ostranauts.shipbreaker";
    private static readonly TranslationCatalog Catalog = Translations.Register(Owner, typeof(Text).Assembly, "PhobosShipbreaker.en.json", "PhobosShipbreaker.equipment-names.json");
    internal static void EnsureLoaded() { _ = Catalog; }
    internal static string Get(string key, params object[] args) => Catalog.Get(key, args);
}
