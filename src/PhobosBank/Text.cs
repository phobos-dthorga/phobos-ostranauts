using Phobos.Ostranauts.Framework.Localization;

namespace PhobosBank;

internal static class Text
{
    internal const string Owner = Core.BankRules.Owner;
    private static readonly TranslationCatalog Catalog = Translations.Register(Owner, typeof(Text).Assembly, "PhobosBank.en.json");
    internal static void EnsureLoaded() { _ = Catalog; }
    internal static string Get(string key, params object[] args) => Catalog.Get(key, args);
}
