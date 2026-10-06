using System;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Social;

/// <summary>Our own text through the game's own grammar (Framework 0.121.0), for any mod that rewords the game's social
/// lines. The game expands tokens such as <c>[us] [asks] [them]</c> only in strings it registered when it loaded its
/// interactions (<c>GrammarUtils.inflectedStrings</c>, filled by <c>DataHandler.PrepareInflectedString</c>); a string
/// composed later comes back with its tokens untouched, which is how a story line once read "[us] [asks] [them]". Here a
/// lead-in is registered once with the game's own preparation, inflected for the interaction's speakers, and only then
/// given its inserted text, so the table holds lead-ins, never one entry per line, and brackets in the inserted text
/// are never read as tokens.</summary>
public static class Grammar
{
    /// <summary>Where the inserted text goes in a lead-in: pass it as the lead-in's format argument.</summary>
    public const string Slot = "\u0001";
    public static Action<string> Log { get; set; } = _ => { };
    private static Action<object?, string>? prepare;
    private static bool looked, broken;

    /// <summary>The lead-in (holding <see cref="Slot"/> once) inflected for the interaction, with <paramref name="insert"/>
    /// in the slot; null when the game's grammar cannot be used, so the caller keeps the game's own line.</summary>
    public static string? Inflect(string leadIn, Interaction interaction, string insert)
    {
        if (broken || string.IsNullOrEmpty(leadIn) || interaction == null) return null;
        try
        {
            if (!GrammarUtils.inflectedStrings.ContainsKey(leadIn))
            {
                var registered = Prepare();
                if (registered == null) return null;
                registered(null, leadIn);
            }
            string inflected = GrammarUtils.GetInflectedString(leadIn, interaction);
            // A token the game could not expand would show as written: keep the game's own line instead.
            if (inflected.IndexOf('[') >= 0 && inflected.IndexOf(']') > inflected.IndexOf('[')) return null;
            return inflected.Replace(Slot, insert ?? "");
        }
        catch (Exception ex) { Fail(ex.Message); return null; }
    }

    /// <summary>The game's own string preparation, found once by name and parameter types.</summary>
    private static Action<object?, string>? Prepare()
    {
        if (looked) return prepare;
        looked = true;
        var method = AccessTools.Method(typeof(DataHandler), "PrepareInflectedString", new[] { typeof(object), typeof(string) });
        if (method == null) { Fail("the game's DataHandler.PrepareInflectedString(object, string) is missing"); return null; }
        prepare = (o, s) => method.Invoke(null, new[] { o, s });
        return prepare;
    }

    private static void Fail(string message)
    {
        if (broken) return;
        broken = true;
        Log("Phobos social text uses the game's own lines for this session: " + message);
    }
}
