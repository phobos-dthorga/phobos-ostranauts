using Phobos.Ostranauts.Framework.Localization;

namespace PhobosMedical;

internal static class Text
{
    internal const string Owner = Core.MedicalRules.Owner;
    private static readonly TranslationCatalog Catalog = Translations.Register(Owner, typeof(Text).Assembly, "PhobosMedical.en.json", "PhobosMedical.equipment-names.json");
    internal static void EnsureLoaded() { _ = Catalog; }
    internal static bool Has(string key) => Catalog.Contains(key);
    internal static string Get(string key, params object[] args) => Catalog.Get(key, args);
}
