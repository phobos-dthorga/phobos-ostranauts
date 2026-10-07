using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Diagnostics;

/// <summary>Which keys a capture's metadata carries (Framework 0.133.0). Until then the loaded Phobos mods came from a
/// fixed list of six, so Banking and Exchange, added later, were never named in a capture (found in the owner's 8 October
/// 2026 recording). Every loaded plugin whose id starts with <see cref="PluginPrefix"/> now names itself: its version
/// under <c>&lt;stem&gt;_version</c> and its build under <c>&lt;stem&gt;_build</c>, the stem being the id's last part,
/// so the six existing keys keep their names and older captures still compare. The recorder refuses more than
/// <see cref="Limit"/> entries and would then not record at all, so builds give way first, then versions, and
/// <see cref="Truncated"/> says that something was left out. Pure: the adapter supplies the plugins.</summary>
internal static class CaptureMetadata
{
    internal const string PluginPrefix = "phobosgekko.ostranauts.";
    /// <summary>The recorder's own limit on metadata entries (Phobos Scope recorder 0.3.0).</summary>
    internal const int Limit = 32;
    internal const string Truncated = "metadata_truncated";

    /// <summary>The metadata stem of a Phobos plugin id, or null for any other plugin and for Framework itself
    /// (which has its own <c>framework_version</c> and <c>framework_build</c>).</summary>
    internal static string? Stem(string? pluginId, string frameworkId)
    {
        if (pluginId == null || pluginId == frameworkId || !pluginId.StartsWith(PluginPrefix, StringComparison.Ordinal)) return null;
        string stem = pluginId.Substring(PluginPrefix.Length);
        if (stem.Length == 0 || stem.Length > 64) return null;
        foreach (char c in stem) if (!(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_')) return null;
        return stem;
    }

    /// <summary>The metadata for one window: the fixed entries, then every mod's version and build in stem order, within
    /// <see cref="Limit"/> less <paramref name="reserved"/> entries the session adds itself.</summary>
    internal static Dictionary<string, string> Plan(IReadOnlyDictionary<string, string> fixedEntries,
        IEnumerable<(string Stem, string Version, string Build)> mods, int reserved)
    {
        int budget = Limit - Math.Max(0, reserved);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var list = mods.Where(m => !string.IsNullOrEmpty(m.Stem)).GroupBy(m => m.Stem, StringComparer.Ordinal).Select(g => g.First())
            .OrderBy(m => m.Stem, StringComparer.Ordinal).ToArray();
        var fixedList = fixedEntries.ToArray();
        // Everything fits: no flag is needed.
        int room = fixedList.Length + 2 * list.Length <= budget ? budget : budget - 1;
        foreach (var pair in fixedList) if (values.Count < room) values[pair.Key] = pair.Value;
        foreach (var m in list) if (values.Count < room) values[m.Stem + "_version"] = m.Version;
        foreach (var m in list) if (values.Count < room) values[m.Stem + "_build"] = m.Build;
        // Otherwise one entry was kept back for the flag: versions went in before builds.
        if (room < budget && budget > 0) values[Truncated] = "true";
        return values;
    }
}
