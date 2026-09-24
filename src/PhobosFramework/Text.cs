using Phobos.Ostranauts.Framework.Localization;

namespace Phobos.Ostranauts.Framework;

internal static class Text
{
    internal const string Owner = "phobosgekko.ostranauts.framework";
    private static readonly TranslationCatalog Catalog = Translations.Register(Owner, typeof(Text).Assembly, "PhobosFramework.en.json", "PhobosFramework.equipment-names.json");
    internal static void EnsureLoaded() { _ = Catalog; }
    internal static string Get(string key, params object[] args) => Catalog.Get(key, args);
}
