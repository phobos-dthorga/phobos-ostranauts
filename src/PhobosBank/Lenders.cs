using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Localization;
using Phobos.Ostranauts.Framework.Story;
using PhobosBank.Core;

namespace PhobosBank;

/// <summary>The lenders pack (Phobos Banking 0.2.0), from <c>framework/lenders.json</c>, add-ons and player files in
/// <c>BepInEx/config/PhobosBank/lenders</c>. Read at each content load; checked against the story library once it is
/// built, when a lender naming an unknown place, person or story entry is left out with a log line.</summary>
internal static class Lenders
{
    public const string Resource = "PhobosBank.lenders.json";
    public static DataPackSource Source => new(BankRules.Owner, BankRules.ModFolder, LenderSchema.Name, typeof(Lenders).Assembly, Resource);
    private static Dictionary<string, LenderEntry> lenders = new(StringComparer.Ordinal);
    private static readonly List<string> problems = new();

    public static IReadOnlyDictionary<string, LenderEntry> All => lenders;
    public static IReadOnlyList<string> Problems => problems;

    /// <summary>Reads the pack (content loading). A shipped pack that fails is a packaging fault: no lenders, said in the log.</summary>
    public static void Load()
    {
        problems.Clear();
        try { lenders = new Dictionary<string, LenderEntry>(DataPacks.Load<LenderPack>(Source, LenderSchema.Validate).lenders, StringComparer.Ordinal); }
        catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is InvalidOperationException)
        {
            lenders = new Dictionary<string, LenderEntry>(StringComparer.Ordinal);
            problems.Add(Text.Get("Lenders.pack_failed", ex.Message)); Plugin.Log(problems[problems.Count - 1]);
        }
    }

    /// <summary>Leaves out lenders whose home, person or requirements name story entries the library does not have
    /// (content loaded, after the story library is built).</summary>
    public static void Check(StoryLibrary library)
    {
        foreach (var pair in lenders.ToArray())
            if (LenderSchema.Unknown(pair.Value, library) is string problem)
            {
                lenders.Remove(pair.Key);
                problems.Add(Text.Get("Lenders.refused", pair.Key, problem)); Plugin.Log(problems[problems.Count - 1]);
            }
    }

    /// <summary>The lender's name and pitch in the player's language: a translation of <c>Lenders.&lt;id&gt;.name</c> or
    /// <c>.pitch</c> when the catalogue has one, else the pack's own words.</summary>
    public static string Name(string id) => lenders.TryGetValue(id, out var l) ? LenderSchema.LedgerSafe(Words(id, "name", l.name)) ? Words(id, "name", l.name) : l.name : id;
    public static string Pitch(string id) => lenders.TryGetValue(id, out var l) ? Words(id, "pitch", l.pitch) : "";
    private static string Words(string id, string field, string inline) => Translations.Get(BankRules.Owner, "Lenders." + id + "." + field, inline);

    /// <summary>Where the lender trades, by the story places' own name.</summary>
    public static string Home(LenderEntry lender) => StoryContent.Library.Places.Name(lender.home) ?? lender.home;
}
