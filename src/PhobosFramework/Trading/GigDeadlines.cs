using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Flight;

namespace Phobos.Ostranauts.Framework.Trading;

/// <summary>Fair gig deadlines in play (Framework 0.123.0; owner direction, 6 October 2026). When the game prices a far
/// gig offer (<c>GigManager.CheckLocal</c>, run on offers not yet taken each time a Gig Nexus lists them), the time it
/// allows is raised to what the player's own torch ships need (<see cref="GigTimeRules"/>), never lowered. The game
/// saves that allowance with the gig and reads it for the deadline, the shown duration and the speed bonuses, so all
/// three agree; a taken gig is never touched. The offer's text then says how its time was set. The player's ships and
/// their accelerations are read at most every ten real seconds, so listing gigs costs a few multiplications per offer.</summary>
public static class GigDeadlines
{
    public static bool Enabled { get; set; } = true;
    public static double Margin { get; set; } = GigTimeRules.DefaultMargin;
    public static double DockingHours { get; set; } = GigTimeRules.DefaultDockingHours;
    private static readonly List<Ship> ships = new();
    private static readonly List<double> accelerations = new();
    private static readonly Cadence refresh = new(10);

    /// <summary>Full torch acceleration (AU/s², the game's 2 g limiter on) of each torch ship the player owns, loaded or not.</summary>
    internal static IReadOnlyList<double> Fleet()
    {
        if (!refresh.Due()) return accelerations;
        accelerations.Clear();
        foreach (var ship in PlayerFleet.Owned(ships, includeUnloaded: true))
        {
            if (!TorchPerformance.HasTorch(ship)) continue;
            double a = TorchPerformance.Acceleration(ship, safetyOn: true);
            if (a > 0 && !double.IsInfinity(a) && !double.IsNaN(a)) accelerations.Add(a);
        }
        ships.Clear();
        return accelerations;
    }
    /// <summary>A new game or load reads the fleet again at once.</summary>
    internal static void Reset() { accelerations.Clear(); refresh.Invalidate(); }

    /// <summary>The two ends the game measures a gig between and their straight-line distance in AU, or null when the
    /// game does not count it as far (same place, a transit link, or within 5,000 km).</summary>
    internal static (Ship From, Ship To, double Au)? Route(JsonJobSave jjs, CondOwner? user)
    {
        var from = jjs.strRegIDPickup != null ? CrewSim.system?.GetShipByRegID(jjs.strRegIDPickup) : user?.ship;
        var to = jjs.strRegIDDropoff != null ? CrewSim.system?.GetShipByRegID(jjs.strRegIDDropoff) : jjs.COThem()?.ship;
        if (from == null || to == null || from == to || JsonTransit.IsTransitConnected(from.strRegID, to.strRegID)) return null;
        double au = from.GetRangeTo(to);
        return au > GigTimeRules.FarAu ? (from, to, au) : null;
    }

    /// <summary>Raises an offer's time allowance to the fleet's fair time; true when it did.</summary>
    internal static bool Apply(JsonJobSave jjs, CondOwner? user)
    {
        if (!Enabled || jjs == null || jjs.bTaken) return false;
        var route = Route(jjs, user);
        if (route == null) return false;
        double au = route.Value.Au;
        double duration = jjs.JobTemplate()?.fDuration ?? 0;
        if (!(duration > 0)) return false;
        double game = duration * jjs.fTimeMult;
        double fair = GigTimeRules.Allowance(game, Fleet().Select(a => TorchTrip.Seconds(au, a)), Margin, DockingHours);
        if (!(fair > game)) return false;
        jjs.fTimeMult = fair / duration;
        return true;
    }

    /// <summary>What an untaken far offer's text adds: where it goes, how far, and how its time was set.</summary>
    internal static string? Explain(JsonJobSave jjs)
    {
        if (!Enabled || jjs == null || jjs.bTaken) return null;
        var route = Route(jjs, CrewSim.coPlayer);
        if (route == null) return null;
        var (_, to, au) = route.Value;
        var fleet = Fleet();
        string where = Text.Get("Gigs.route", to.publicName ?? to.strRegID, (au * TorchTrip.KilometresPerAu).ToString("N0", System.Globalization.CultureInfo.InvariantCulture));
        if (fleet.Count == 0)
            return where + "\n" + Text.Get("Gigs.no_torch", MathUtils.GetDurationFromS(GigTimeRules.FerryHours(au) * 3600, 3));
        double average = fleet.Select(a => TorchTrip.Seconds(au, a)).Average();
        return where + "\n" + Text.Get("Gigs.fair", fleet.Count, MathUtils.GetDurationFromS(average, 3),
            Margin.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), DockingHours.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));
    }
}

[HarmonyPatch(typeof(GigManager), nameof(GigManager.CheckLocal), new[] { typeof(JsonJobSave), typeof(CondOwner), typeof(double) })]
internal static class GigDeadlinePatch
{
    private static void Postfix(JsonJobSave jjs, CondOwner coUser, bool __result)
    {
        if (!__result) return;
        try { GigDeadlines.Apply(jjs, coUser); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Gigs.failed", ex.Message)); }
    }
}

[HarmonyPatch(typeof(GUIJobs), nameof(GUIJobs.GetDisplayGigText), new[] { typeof(JsonJobSave) })]
internal static class GigDeadlineText
{
    private static void Postfix(JsonJobSave jjs, ref string __result)
    {
        try { if (GigDeadlines.Explain(jjs) is string line) __result = __result + "\n\n" + line; }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Gigs.failed", ex.Message)); }
    }
}
