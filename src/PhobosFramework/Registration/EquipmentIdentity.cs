using System;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Exact native four-form families, without allocating candidate IDs during discovery.</summary>
public static class EquipmentIdentity
{
    public static bool IsFamily(string? id, string prefix)
    {
        if (id == null || string.IsNullOrEmpty(prefix) || !id.StartsWith(prefix, StringComparison.Ordinal)) return false;
        string? suffix = (id.Length - prefix.Length) switch
        { 9 => "Installed", 12 => "InstalledDmg", 5 => "Loose", 8 => "LooseDmg", _ => null };
        return suffix != null && string.CompareOrdinal(id, prefix.Length, suffix, 0, suffix.Length) == 0;
    }
}
