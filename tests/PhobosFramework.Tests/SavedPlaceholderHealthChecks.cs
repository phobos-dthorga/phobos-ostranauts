using System;
using Phobos.Ostranauts.Framework.Persistence;

internal static class SavedPlaceholderHealthChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string[] Saved(string maximum = "20", string damage = "0.0015384719410736") => new[] {
            "IsPlaceholder=1.0x1", "StatDamageMax=1.0x" + maximum, "StatDamage=1.0x" + damage, "DEFAULT" };
        check(SavedPlaceholderHealth.HasRemainingHealth(.0015384719410736, Saved()), "G4 wear does not exhaust its saved twenty-point damage limit");
        check(SavedPlaceholderHealth.HasRemainingHealth(.00196767756542519, Saved("20", "0.00196767756542519")), "H4 recurrence record retains actual damage");
        check(SavedPlaceholderHealth.HasRemainingHealth(.002, Saved("15", "0.002")), "Native wall and floor markers use their own saved limit");
        check(!SavedPlaceholderHealth.HasRemainingHealth(null, Saved()), "No override leaves native behavior alone");
        foreach (double invalid in new[] { -1, double.NaN, double.PositiveInfinity, 20, 21 })
            check(!SavedPlaceholderHealth.HasRemainingHealth(invalid, Saved()), "Invalid or exhausted override is not rescued");
        check(!SavedPlaceholderHealth.HasRemainingHealth(.001, Saved("20", "20")), "Saved exhausted health cannot be hidden by a smaller override");
        foreach (string maximum in new[] { "0", "-1", "NaN", "Infinity", "not-a-number" })
            check(!SavedPlaceholderHealth.HasRemainingHealth(.001, Saved(maximum)), "Unknown/invalid maximum remains native");
        check(!SavedPlaceholderHealth.HasRemainingHealth(.001, new[] { "IsPlaceholder=1x1", "StatDamage=1x0.001", "DEFAULT" }), "DEFAULT cannot invent a missing health limit");
        check(!SavedPlaceholderHealth.HasRemainingHealth(.001, new[] { "IsPlaceholder=1x1", "StatDamageMax=0.5x20", "StatDamage=1x0.001" }), "Probabilistic condition is ambiguous");
        check(!SavedPlaceholderHealth.HasRemainingHealth(.001, new[] { "IsPlaceholder=1x1", "StatDamageMax=1x20", "StatDamageMax=1x30", "StatDamage=1x0.001" }), "Duplicate maxima are ambiguous");
        check(!SavedPlaceholderHealth.HasRemainingHealth(.001, new[] { "StatDamageMax=1x20", "StatDamage=1x0.001" }), "Non-marker records are excluded");
        var original = Saved(); var before = string.Join("|", original);
        SavedPlaceholderHealth.HasRemainingHealth(.0016, original);
        check(before == string.Join("|", original), "Health check never repairs or rewrites saved conditions");
    }
}
