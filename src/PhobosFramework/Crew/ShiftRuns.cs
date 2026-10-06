using System;
using System.Collections.Generic;
using System.Text;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>A crew member's coming hours as runs of the same shift (Framework 0.117.0): the Time-skip view says
/// "Working for 3 h, then resting for 2 h" instead of listing every hour. No game types, so the offline checks run it.</summary>
public static class ShiftRuns
{
    /// <summary>Consecutive equal shift ids collapsed into (shift, hours) runs, in order.</summary>
    public static List<(int Shift, int Hours)> Compress(IEnumerable<int> shiftIds)
    {
        if (shiftIds == null) throw new ArgumentNullException(nameof(shiftIds));
        var runs = new List<(int Shift, int Hours)>();
        foreach (int shift in shiftIds)
        {
            if (runs.Count > 0 && runs[runs.Count - 1].Shift == shift) runs[runs.Count - 1] = (shift, runs[runs.Count - 1].Hours + 1);
            else runs.Add((shift, 1));
        }
        return runs;
    }
    /// <summary>The runs as one sentence: <paramref name="word"/> names a shift ("Working", "Resting", "Free time"),
    /// <paramref name="first"/> formats the first run ("{0} for {1} h") and <paramref name="then"/> the rest
    /// (", then {0} for {1} h"); empty runs give an empty string.</summary>
    public static string Text(IReadOnlyList<(int Shift, int Hours)> runs, Func<int, string> word, Func<string, int, string> first, Func<string, int, string> then)
    {
        if (runs == null || word == null || first == null || then == null) throw new ArgumentNullException();
        var text = new StringBuilder();
        for (int i = 0; i < runs.Count; i++)
            text.Append(i == 0 ? first(word(runs[i].Shift), runs[i].Hours) : then(word(runs[i].Shift).ToLowerInvariant(), runs[i].Hours));
        return text.ToString();
    }
}
