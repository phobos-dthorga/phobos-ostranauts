using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Notices;

namespace Phobos.Ostranauts.Framework.Liquids;

public enum GasHazardClass { None, Oxidiser, Fuel }

/// <summary>Advice, never enforcement (owner decision, 30 September 2026): an oxygen store and a fuel store on one gas
/// network are allowed, because the P1 manifold and the L2 filling station already mix gases deliberately, but the
/// player is told. Industrial practice keeps oxidising and flammable gas stores apart: the U.S. Occupational Safety
/// and Health Administration's rule for oxygen cylinders in storage, 29 CFR 1910.253(b)(4)(iii), requires 20 feet or
/// a non-combustible barrier at least five feet high between them and fuel-gas cylinders
/// (https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910.253). Our gas line carries no gas between
/// transfers and nothing here models a leak; the warning is a design choice, not a claim that the mix is dangerous in
/// the game. Content classifies its commodities.</summary>
public static class GasNetworkSafety
{
    private static readonly Dictionary<string, GasHazardClass> classes = new(StringComparer.Ordinal);
    private static readonly HashSet<string> noticed = new(StringComparer.Ordinal);
    public static void Classify(string commodity, GasHazardClass hazard)
    {
        if (string.IsNullOrEmpty(commodity)) throw new ArgumentException("A commodity is required.");
        classes[commodity] = hazard;
    }
    public static GasHazardClass ClassOf(string? commodity) => commodity != null && classes.TryGetValue(commodity, out var c) ? c : GasHazardClass.None;
    /// <summary>The oxidiser and fuel stores sharing <paramref name="participant"/>'s gas network (the participant
    /// included), or false when the network holds only one class.</summary>
    public static bool Mixed(CondOwner? participant, out CondOwner? oxidiser, out CondOwner? fuel)
    {
        oxidiser = fuel = null;
        if (participant == null) return false;
        foreach (var co in LineReach.Members(participant, LineFamilies.Gas).Prepend(participant))
        {
            switch (ClassOf(BulkVessels.Of(co)?.Commodity))
            {
                case GasHazardClass.Oxidiser: oxidiser ??= co; break;
                case GasHazardClass.Fuel: fuel ??= co; break;
            }
        }
        return oxidiser != null && fuel != null;
    }
    /// <summary>The status line for a participant on a mixed network, or null.</summary>
    public static string? Warning(CondOwner? participant) =>
        Mixed(participant, out var oxidiser, out var fuel) ? Text.Get("GasNetworkSafety.warning", Controls.ObjectPresentation.Name(oxidiser!), Controls.ObjectPresentation.Name(fuel!)) : null;
    /// <summary>Posts one Caution per mixed network per session (keyed by the pair of stores that first showed it),
    /// and forgets it once no store on the ship is mixed, so a later mix is reported again.</summary>
    public static void Review(Ship? ship, IEnumerable<CondOwner> stores)
    {
        if (ship == null) return;
        string prefix = ship.strRegID + "|";
        bool any = false;
        foreach (var store in stores)
        {
            if (store == null || store.ship != ship || !Mixed(store, out var oxidiser, out var fuel)) continue;
            any = true;
            string key = prefix + string.Join("|", new[] { oxidiser!.strID, fuel!.strID }.OrderBy(x => x, StringComparer.Ordinal));
            if (noticed.Any(k => k.StartsWith(prefix, StringComparison.Ordinal))) continue;
            noticed.Add(key);
            PlayerNotices.Post(ship, "PhobosFramework.gas-mix", NoticeLevel.Caution,
                Text.Get("GasNetworkSafety.notice", Controls.ObjectPresentation.Name(oxidiser), Controls.ObjectPresentation.Name(fuel)), Text.Get("GasNetworkSafety.banner"));
        }
        if (!any) noticed.RemoveWhere(k => k.StartsWith(prefix, StringComparison.Ordinal));
    }
    public static void Reset() => noticed.Clear();
}
