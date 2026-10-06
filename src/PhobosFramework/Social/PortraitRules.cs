using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Phobos.Ostranauts.Framework.Social;

/// <summary>The rules of game-made faces with no game types (Framework 0.121.0), so the offline checks run them: which
/// looks there are, what a saved list of face parts may hold, and the stable picture name for a key.</summary>
public static class PortraitRules
{
    public const string Any = "any", Masculine = "masculine", Feminine = "feminine";
    public static readonly IReadOnlyList<string> Looks = new[] { Any, Masculine, Feminine };
    /// <summary>The game's faces have ten parts; a saved list is checked against a generous bound.</summary>
    public const int MaxParts = 32;
    private static readonly Regex Part = new("^[A-Za-z0-9_]{1,64}$", RegexOptions.CultureInvariant);

    /// <summary>The game's face roll takes a male and a female flag: both (or neither) is its nonbinary pool.</summary>
    public static (bool Male, bool Female) Flags(string? look) => look switch
    {
        Masculine => (true, false),
        Feminine => (false, true),
        _ => (true, true)
    };

    public static bool IsLook(string? look) => look == null || Looks.Contains(look);

    /// <summary>A saved part list: one to <see cref="MaxParts"/> plain names, as the game's portrait file names are.</summary>
    public static bool ValidParts(IReadOnlyList<string>? parts) => parts != null && parts.Count > 0 && parts.Count <= MaxParts && parts.All(p => p != null && Part.IsMatch(p));

    /// <summary>The picture name a face is registered under, stable for a key (FNV-1a), with no path or extension, as
    /// the game's goal panel wants its portrait name.</summary>
    public static string ImageName(string key)
    {
        uint h = 2166136261;
        foreach (char c in key ?? "") { h ^= c; h *= 16777619; }
        return "PhobosFace" + h.ToString("x8");
    }
}
