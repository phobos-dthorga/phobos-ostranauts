using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Localization;
using PhobosExchange.Core;

namespace PhobosExchange;

/// <summary>The exchange pack (Phobos Exchange 0.1.0), from <c>framework/exchange.json</c>, add-ons and player files in
/// <c>BepInEx/config/PhobosExchange/exchange</c>. Read at each content load. Names and profiles can be translated
/// (<c>Companies.&lt;id&gt;.name</c>, <c>.profile</c>, <c>Sectors.&lt;id&gt;.name</c>, <c>Market.name</c>).</summary>
internal static class Companies
{
    public const string Resource = "PhobosExchange.exchange.json";
    public static DataPackSource Source => new(ExchangeRules.Owner, ExchangeRules.ModFolder, ExchangeSchema.Name, typeof(Companies).Assembly, Resource);
    private static readonly List<string> problems = new();

    /// <summary>The pack, or null when the shipped pack failed (a packaging fault, said in the log).</summary>
    public static ExchangePack? Pack { get; private set; }
    public static IReadOnlyList<string> Problems => problems;

    public static void Load()
    {
        problems.Clear();
        try { Pack = DataPacks.Load<ExchangePack>(Source, ExchangeSchema.Validate); }
        catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is InvalidOperationException)
        {
            Pack = null;
            problems.Add(Text.Get("Market.pack_failed", ex.Message)); Plugin.Log(problems[problems.Count - 1]);
        }
    }

    /// <summary>Checks every driver against the game once its data is loaded: a station the game has no market for, or a
    /// category it does not know, reads as no data (the driver holds still), said in the log once.</summary>
    public static void Check()
    {
        if (Pack == null) return;
        foreach (var pair in Pack.companies)
            foreach (var d in pair.Value.drivers)
                if (NativeMarket.Problem(d.station, d.category) is string problem)
                { problems.Add(Text.Get("Market.driver_unknown", pair.Key, d.station, d.category, problem)); Plugin.Log(problems[problems.Count - 1]); }
    }

    public static string ExchangeName => Pack == null ? "" : Safe(Translations.Get(ExchangeRules.Owner, "Market.name", Pack.market.name), Pack.market.name);
    public static string Name(string id) => Pack != null && Pack.companies.TryGetValue(id, out var c) ? Safe(Translations.Get(ExchangeRules.Owner, "Companies." + id + ".name", c.name), c.name) : id;
    public static string Profile(string id) => Pack != null && Pack.companies.TryGetValue(id, out var c) ? Translations.Get(ExchangeRules.Owner, "Companies." + id + ".profile", c.profile) : "";
    /// <summary>A news entry's wire line in the player's language (<c>News.&lt;company&gt;.&lt;flag&gt;</c>), else the pack's own.</summary>
    public static string NewsWire(string id, NewsEntry n) => Translations.Get(ExchangeRules.Owner, "News." + id + "." + n.flag, n.wire ?? "");

    public static string SectorName(string id) => Pack != null && Pack.sectors.TryGetValue(id, out var s) ? Safe(Translations.Get(ExchangeRules.Owner, "Sectors." + id + ".name", s.name), s.name) : id;

    /// <summary>A translated name only when it can sit in a ledger line; otherwise the pack's own.</summary>
    private static string Safe(string translated, string fallback) => ExchangeRules.LedgerSafe(translated) ? translated : fallback;
}
