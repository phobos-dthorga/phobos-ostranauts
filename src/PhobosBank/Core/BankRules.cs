using System;

namespace PhobosBank.Core;

/// <summary>Identities and the game's own debt figures, mirrored where the game keeps them private or inside a formula
/// (Ostranauts 1.0.1.5: <c>Ledger</c>, <c>MathUtils.MortgagePaymentPerShift</c>, <c>Ledger.Skip</c>). No game types, so
/// the offline checks run them; the native checks compare the mirrors with the game.</summary>
public static class BankRules
{
    public const string Owner = "phobosgekko.ostranauts.bank";
    /// <summary>The PDA app's saved-free name; the tooltip strings follow from it.</summary>
    public const string AppName = "phobos_bank";
    /// <summary>The icon under the package's <c>images/</c> folder, without <c>.png</c>.</summary>
    public const string Icon = "phobos/bank/Credit";
    /// <summary>The game's currency condition (<c>Ledger.CURRENCY</c>).</summary>
    public const string Currency = "StatUSD";

    /// <summary>A game mortgage runs this long from its start (<c>MathUtils</c>: 720 shifts of six hours).</summary>
    public const double MortgageTermSeconds = 15552000;
    public const double GameDaySeconds = 87658.125, ShiftSeconds = 21600;
    public const int ShiftsPerDay = 4;
    /// <summary>The game's mortgage rate per shift (<c>Ledger.MORTGAGE_RATE</c>).</summary>
    public const double MortgageRatePerShift = 0.0021;
    /// <summary>The share of an unpaid bill the game adds as a late fee at each shift change (<c>Ledger.Skip</c>).</summary>
    public const double LateFeeShare = 0.175;
    /// <summary>Balances below this are paid off, not owed (the game stores money as single-precision floats).</summary>
    public const double PaidOffBelow = 0.005;
    /// <summary>How often an open panel looks again at the ledger, in real seconds.</summary>
    public const float PanelRefreshSeconds = 2;

    /// <summary>Instalments left on a game mortgage, counted as the game counts them: whole days of four shifts, then
    /// whole shifts in what remains. Zero once the term has run.</summary>
    public static int ShiftsLeft(double elapsedSeconds)
    {
        double remaining = MortgageTermSeconds - elapsedSeconds;
        if (remaining <= 0) return 0;
        int days = (int)(remaining / GameDaySeconds);
        int shifts = (int)((remaining - days * GameDaySeconds) / ShiftSeconds) + days * ShiftsPerDay;
        return Math.Max(0, shifts);
    }

    /// <summary>The next instalment on a balance with the given shifts left, as the game charges it (the amortised payment
    /// at the game's rate, never more than the balance; the whole balance once no shift is left).</summary>
    public static double Instalment(double balance, int shiftsLeft)
    {
        if (balance <= PaidOffBelow) return 0;
        if (shiftsLeft <= 0) return balance;
        double growth = Math.Pow(1 + MortgageRatePerShift, shiftsLeft);
        return Math.Min(balance, MortgageRatePerShift * balance * growth / (growth - 1));
    }

    /// <summary>The late fee the game will add at the next shift change on an unpaid bill.</summary>
    public static double LateFee(double unpaid) => unpaid <= PaidOffBelow ? 0 : unpaid * LateFeeShare;

    /// <summary>The catalogue key for a count: the <c>_one</c> form for exactly one, so each language words both (the
    /// catalogues have no plural rules of their own).</summary>
    public static string CountKey(string key, int count) => count == 1 ? key + "_one" : key;

    /// <summary>How the overview says how many bills are late: all of them (one or several), one of several, or some.</summary>
    public static string LateKey(int bills, int late) =>
        late >= bills ? (bills == 1 ? "Overview.late_it" : "Overview.late_all") : late == 1 ? "Overview.late_one" : "Overview.late_some";

    /// <summary>Whether the game has marked a line late: it prefixes overdue loan instalments with one word and names late
    /// fees with another (the game's own strings, passed in, so the check follows the player's language).</summary>
    public static bool IsLate(string? description, string? overdueWord, string? lateFeeWord) =>
        !string.IsNullOrEmpty(description) &&
        (!string.IsNullOrEmpty(overdueWord) && description!.StartsWith(overdueWord, StringComparison.Ordinal) ||
         !string.IsNullOrEmpty(lateFeeWord) && description!.StartsWith(lateFeeWord, StringComparison.Ordinal));
}
