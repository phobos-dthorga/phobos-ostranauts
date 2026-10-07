using Phobos.Ostranauts.Framework.Localization;

namespace PhobosExchange;

internal static class Text
{
    internal const string Owner = Core.ExchangeRules.Owner;
    private static readonly TranslationCatalog Catalog = Translations.Register(Owner, typeof(Text).Assembly, "PhobosExchange.en.json");
    internal static void EnsureLoaded() { _ = Catalog; }
    internal static string Get(string key, params object[] args) => Catalog.Get(key, args);
    /// <summary>One of a message's variants (0.6.0, Framework 0.134.0), never the one shown last for the key: for wire
    /// lines, which repeat over weeks of play.</summary>
    internal static string Pick(string key, params object[] args) => Catalog.Pick(key, UnityEngine.Random.value, args);
}
