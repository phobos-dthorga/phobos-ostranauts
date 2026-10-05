using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;

namespace PhobosShipbreaker;

/// <summary>Why a Shipbreaker link picker does not offer something aboard (Shipbreaker 0.80.0, the owner's rule of
/// 1 October 2026: never an empty list with no reason). The pickers offer only what the machine reaches; this lists the
/// rest, each with the one thing to fix. Presentation only: the services decide reach.</summary>
internal static class LinkNotes
{
    /// <summary>One line for each object with a reason, under a heading; a line saying nothing suitable is aboard when
    /// <paramref name="anyAboard"/> is false; empty when everything aboard is offered.</summary>
    internal static string Build(bool anyAboard, IEnumerable<(CondOwner Object, string? Reason)> rejected)
    {
        if (!anyAboard) return Text.Get("Links.none");
        var lines = rejected.Where(r => r.Object != null && !string.IsNullOrEmpty(r.Reason)).OrderBy(r => r.Object.strID, System.StringComparer.Ordinal)
            .Select(r => Text.Get("Links.line", ObjectPresentation.Name(r.Object), r.Reason!)).ToArray();
        return lines.Length == 0 ? "" : Text.Get("Links.not_offered") + "\n" + string.Join("\n", lines);
    }
    /// <summary>The note for a list of compatible objects aboard and a test giving each one's problem, or null when it is offered.</summary>
    internal static string For(IEnumerable<CondOwner> aboard, System.Func<CondOwner, string?> problem)
    {
        var all = aboard.Where(c => c != null && !c.bDestroyed).ToArray();
        return Build(all.Length > 0, all.Select(c => (c, problem(c))));
    }
}
