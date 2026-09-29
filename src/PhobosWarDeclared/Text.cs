using Phobos.Ostranauts.Framework.Localization;

namespace PhobosWarDeclared;

internal static class Text
{
    internal const string Owner = Core.WarRules.Owner;
    private static readonly TranslationCatalog Catalog = Translations.Register(Owner, typeof(Text).Assembly, "PhobosWarDeclared.en.json");
    internal static void EnsureLoaded() { _ = Catalog; }
    internal static string Get(string key, params object[] args) => Catalog.Get(key, args);
}
