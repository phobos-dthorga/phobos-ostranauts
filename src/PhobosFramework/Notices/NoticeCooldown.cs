using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Notices;

/// <summary>Info is a routine log line; Caution is highlighted and may raise the nav-map banner.</summary>
public enum NoticeLevel { Info, Caution }

/// <summary>Limits how often one kind of notice may interrupt the player. Times are caller-supplied seconds.</summary>
public sealed class NoticeCooldown
{
    private readonly Dictionary<string, double> last = new(StringComparer.Ordinal);
    public double Seconds { get; }
    public NoticeCooldown(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        Seconds = seconds;
    }

    /// <summary>True and remembered when this key has been quiet long enough. A clock that runs backwards
    /// (a new session) never suppresses a notice indefinitely.</summary>
    public bool Allow(string key, double now)
    {
        if (double.IsNaN(now) || double.IsInfinity(now)) return false;
        if (last.TryGetValue(key, out double previous) && now >= previous && now - previous < Seconds) return false;
        last[key] = now;
        return true;
    }
}
