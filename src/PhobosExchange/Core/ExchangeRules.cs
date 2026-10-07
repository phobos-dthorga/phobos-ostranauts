using System;
using Phobos.Ostranauts.Framework;

namespace PhobosExchange.Core;

/// <summary>Identities and the fixed numbers of the market (Phobos Exchange 0.1.0). No game types, so the offline checks
/// run everything in this folder. Authored balance lives in the <c>exchange</c> pack; what is here is structure: the time
/// grid, the history sizes and the catch-up bounds, which saves and tests depend on.</summary>
public static class ExchangeRules
{
    public const string Owner = "phobosgekko.ostranauts.exchange";
    /// <summary>The mod folder: the package's folder and the player override folder under <c>BepInEx/config</c>.</summary>
    public const string ModFolder = "PhobosExchange";
    /// <summary>The PDA app's name; the tooltip strings follow from it.</summary>
    public const string AppName = "phobos_exchange";
    /// <summary>The icon under the package's <c>images/</c> folder, without <c>.png</c>.</summary>
    public const string Icon = "phobos/exchange/Shares";
    /// <summary>The game's currency condition (<c>Ledger.CURRENCY</c>).</summary>
    public const string Currency = "StatUSD";
    /// <summary>The F3 command.</summary>
    public const string Command = "phobosexchange";

    /// <summary>The market's time grid: one step every 60 game seconds, counted from the game's epoch, so a price at a
    /// given moment is the same whether the player watched at 1x or skipped (owner requirement, 7 October 2026).</summary>
    public const double StepSeconds = 60;
    public const double HourSeconds = GameClock.HourSeconds, DaySeconds = GameClock.DaySeconds, YearSeconds = GameClock.YearSeconds;
    public const double WeekSeconds = 7 * DaySeconds;

    /// <summary>History kept for the charts: hourly closes for three days, daily for 120 days, weekly for two years.</summary>
    public const int HourlyCloses = 73, DailyCloses = 120, WeeklyCloses = 104;
    /// <summary>Catch-up bounds (owner requirement: any time jump, up to years at once, at a bounded cost). The last
    /// <see cref="FineDays"/> are stepped minute by minute; from there back to <see cref="DailyDays"/> day by day; from
    /// there back to <see cref="WeeklyWeeks"/> week by week; anything older in one exact step.</summary>
    public const double FineDays = 3, DailyDays = 120;
    public const int WeeklyWeeks = 104;

    /// <summary>How long calm or stormy stretches last: the half-life of the log-volatility, in game days.</summary>
    public const double VolHalfLifeDays = 3;
    /// <summary>How quickly a driver follows the native market: the half-life of its smoothing, in game days.</summary>
    public const double DriverHalfLifeDays = 1;
    /// <summary>How often the game side reads the native market for the drivers, in game seconds.</summary>
    public const double DriverReadSeconds = HourSeconds;
    /// <summary>How often the service polls, in real seconds.</summary>
    public const float PollSeconds = 1;
    /// <summary>How often an open panel refreshes its prices, in real seconds.</summary>
    public const float PanelRefreshSeconds = 1;
    /// <summary>A gap longer than this ends with one "while you were away" summary in the crew log.</summary>
    public const double AwayDays = 3;

    /// <summary>What the exchange tells story content (Phobos Exchange 0.2.0): an event starts the arc
    /// <c>exchange-&lt;company&gt;-&lt;event&gt;</c> when a story pack has one, and sets the flag of the same name.</summary>
    public const string Surge = "surge", Slump = "slump", Bought = "bought", MajorHolder = "major-holder", SoldOut = "sold-out";
    public static readonly string[] Events = { Surge, Slump, Bought, MajorHolder, SoldOut };
    /// <summary>A holding worth this share of the most one may hold makes the player a major holder.</summary>
    public const double MajorHolderShare = 0.5;
    public static string StoryId(string company, string what) => "exchange-" + company + "-" + what;

    /// <summary>Company histories (Phobos Exchange 0.3.0; owner decisions, 7 October 2026). A new game always starts in
    /// <see cref="FirstSaveYear"/> (the game's own new-game epoch, checked natively); founding, listing and history dates
    /// reach back at most <see cref="MaxAgeYears"/> before it and end the year before.</summary>
    public const int FirstSaveYear = 2079, MaxAgeYears = 200;
    public const int EarliestYear = FirstSaveYear - MaxAgeYears, LastHistoryYear = FirstSaveYear - 1;
    /// <summary>Milestones that move a price, and authored listing prices, end in this year (agent choice): the two years
    /// before a new game's start are the history the save itself stores, drawn by the market model.</summary>
    public const int LastMoveYear = FirstSaveYear - 3;
    /// <summary>The save's own lifetime history: monthly points, thinned to stay within this many.</summary>
    public const int LifetimeCapacity = 240;
    /// <summary>A time jump of more than two years is crossed in at most this many month-aligned steps (plus two).</summary>
    public const int LongSteps = 240;
    /// <summary>The generated past steps each path at most this many times; a longer span steps every few months.</summary>
    public const int MaxPastSteps = 4800;
    /// <summary>The bound on an authored listing price: the yearly growth it implies, net of the milestones, lies within
    /// these (agent default, so a short history is not a cliff).</summary>
    public const double MinListingGrowth = -0.05, MaxListingGrowth = 0.25;

    /// <summary>The calendar month a step falls in, numbered as Framework's <see cref="GameClock.MonthIndex"/>.</summary>
    public static long MonthOf(long step) => GameClock.MonthIndex(TimeOf(step));
    /// <summary>The month a year and month (1 to 12) name.</summary>
    public static long MonthIndex(int year, int month) => (long)year * 12 + month - 1;

    /// <summary>The highest expected yearly return the pack may give a company (owner rule, 7 October 2026: at most half
    /// the cheapest Banking loan's yearly cost, so borrowing to hold never pays on average; a test checks this cap
    /// against Banking's shipped lenders).</summary>
    public const double MaxExpectedReturn = 0.14;
    /// <summary>Displayed prices stay within these bounds; the model itself is never clamped.</summary>
    public const double MinPrice = 0.01, MaxPrice = 1e9;
    /// <summary>The native price factor read for a driver is held within these bounds before its logarithm is taken.</summary>
    public const double MinFactor = 0.1, MaxFactor = 3;

    public static long StepOf(double epoch) => double.IsNaN(epoch) || epoch <= 0 ? 0 : (long)Math.Floor(epoch / StepSeconds);
    public static double TimeOf(long step) => step * StepSeconds;
    public static long HourOf(long step) => (long)Math.Floor(TimeOf(step) / HourSeconds);
    public static long DayOf(long step) => (long)Math.Floor(TimeOf(step) / DaySeconds);
    public static long WeekOf(long step) => (long)Math.Floor(TimeOf(step) / WeekSeconds);
    /// <summary>The first step at or after a time.</summary>
    public static long StepAtOrAfter(double seconds) => (long)Math.Ceiling(seconds / StepSeconds);
    public static long FineSteps => StepAtOrAfter(FineDays * DaySeconds);

    /// <summary>The catalogue key for a count: the <c>_one</c> form for exactly one.</summary>
    public static string CountKey(string key, long count) => count == 1 ? key + "_one" : key;

    /// <summary>The price shown for a log price, within the display bounds.</summary>
    public static double Price(double logPrice) => Math.Min(MaxPrice, Math.Max(MinPrice, Math.Exp(Math.Max(Math.Log(MinPrice), Math.Min(Math.Log(MaxPrice), logPrice)))));

    /// <summary>Text that can sit in a ledger line and in the saved record: no separators the save store or the record
    /// use, no rich-text or reference marks.</summary>
    public static bool LedgerSafe(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text!.IndexOfAny(new[] { '|', ',', '=', '#', '[', ']', '<', '>', ';' }) < 0 && !HasControl(text);

    /// <summary>Record and ledger text: separators become plain punctuation.</summary>
    public static string Clean(string? text)
    {
        var chars = (text ?? "").ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            chars[i] = c == '|' ? '/' : c == ',' || c == ';' ? ' ' : c == '=' ? '-' : c == '#' ? 'n' : char.IsControl(c) ? ' ' : c;
        }
        return new string(chars).Trim();
    }

    private static bool HasControl(string text) { foreach (char c in text) if (char.IsControl(c)) return true; return false; }
}
