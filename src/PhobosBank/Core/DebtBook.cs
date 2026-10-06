using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhobosBank.Core;

/// <summary>The three kinds of debt the game's ledger holds against the player.</summary>
public enum DebtKind
{
    /// <summary>A balance repaid in instalments each shift: a ship mortgage, Ogiso's Bank's starting mortgage, a fine.</summary>
    Loan,
    /// <summary>A one-off charge waiting to be paid: an instalment, a docking fee, a late fee, a bill.</summary>
    Bill,
    /// <summary>A charge the game raises again every hour, shift, day, month or year, such as crew wages.</summary>
    Charge
}

/// <summary>How often a regular charge repeats (the game's <c>LedgerLI.Frequency</c> names, without one-time and mortgage).</summary>
public enum ChargeEvery { Hour, Shift, Day, Month, Year }

/// <summary>One debt as the panel shows it. Read from the game's ledger each time; never saved.</summary>
public sealed class Debt
{
    public DebtKind Kind;
    /// <summary>Stable while the line exists, so the panel keeps the player's selection across refreshes.</summary>
    public string Id = "";
    public string Creditor = "", Description = "";
    /// <summary>Balance for a loan, the amount due for a bill, the amount each time for a charge.</summary>
    public double Amount;
    /// <summary>When the loan began, the bill was raised or the charge was set up (game epoch seconds).</summary>
    public double Since;
    /// <summary>Loans: the next instalment and how many are left.</summary>
    public double Instalment;
    public int ShiftsLeft;
    public bool Late;
    public ChargeEvery Every;

    public static string MakeId(DebtKind kind, string payee, double since, string description) =>
        kind + "|" + payee + "|" + since.ToString("R", CultureInfo.InvariantCulture) + "|" + description;
}

/// <summary>What the player owes, summed for the overview. Pure.</summary>
public sealed class DebtSummary
{
    public IReadOnlyList<Debt> Debts = Array.Empty<Debt>();
    public double LoanBalance, BillsDue, NextInstalments;
    public int Loans, Bills, Charges, Late;

    /// <summary>Loans by largest balance, bills late first then oldest, charges by creditor: the order the panel lists them.</summary>
    public static DebtSummary Of(IEnumerable<Debt> debts)
    {
        var list = debts.Where(d => d != null).ToList();
        var ordered = list.Where(d => d.Kind == DebtKind.Loan).OrderByDescending(d => d.Amount).ThenBy(d => d.Id, StringComparer.Ordinal)
            .Concat(list.Where(d => d.Kind == DebtKind.Bill).OrderByDescending(d => d.Late).ThenBy(d => d.Since).ThenBy(d => d.Id, StringComparer.Ordinal))
            .Concat(list.Where(d => d.Kind == DebtKind.Charge).OrderBy(d => d.Creditor, StringComparer.Ordinal).ThenBy(d => d.Id, StringComparer.Ordinal))
            .ToList();
        var loans = ordered.Where(d => d.Kind == DebtKind.Loan).ToList();
        var bills = ordered.Where(d => d.Kind == DebtKind.Bill).ToList();
        return new DebtSummary
        {
            Debts = ordered,
            Loans = loans.Count, Bills = bills.Count, Charges = ordered.Count - loans.Count - bills.Count,
            LoanBalance = loans.Sum(d => d.Amount),
            NextInstalments = loans.Sum(d => d.Instalment),
            BillsDue = bills.Sum(d => d.Amount),
            Late = bills.Count(d => d.Late)
        };
    }

    /// <summary>Changes whenever a line appears, goes or changes amount: an unchanged signature needs no redraw.</summary>
    public string Signature => string.Join(";", Debts.Select(d => d.Id + "=" + d.Amount.ToString("F2", CultureInfo.InvariantCulture) + (d.Late ? "!" : "")));
}
