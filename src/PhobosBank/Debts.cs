using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PhobosBank.Core;

namespace PhobosBank;

/// <summary>Reads what the player owes from the game's own ledger (Phobos Banking 0.1.0; owner choice, 6 October 2026:
/// the app shows the debts the game already keeps). Read-only: nothing here changes the ledger, pays or charges.</summary>
internal static class Debts
{
    // The game keeps its running loans in a private list; the public calls only reach their instalments.
    private static readonly FieldInfo? MortgageField = typeof(Ledger).GetField("aMortgage", BindingFlags.NonPublic | BindingFlags.Static);
    internal static bool CanReadLoans => MortgageField != null && typeof(List<LedgerLI>).IsAssignableFrom(MortgageField.FieldType);

    /// <summary>The player's loans, bills and regular charges, now. Empty when no game is running.</summary>
    internal static DebtSummary Read()
    {
        var player = CrewSim.coPlayer;
        if (player == null || string.IsNullOrEmpty(player.strID)) return DebtSummary.Of(Array.Empty<Debt>());
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Read);
        string id = player.strID;
        double now = StarSystem.fEpoch;
        string overdue = DataHandler.GetString("GUI_FINANCE_OVERDUE"), late = DataHandler.GetString("GUI_FINANCE_LATE");
        var debts = new List<Debt>();

        if (CanReadLoans && MortgageField!.GetValue(null) is List<LedgerLI> loans)
            foreach (var li in loans.Where(l => l != null && l.strPayor == id && l.strCurrency == BankRules.Currency && l.fAmount > BankRules.PaidOffBelow))
            {
                double instalment = MathUtils.MortgagePaymentPerShift(li);
                // Past its term the game's formula stops making sense; the balance is then what is due.
                if (double.IsNaN(instalment) || double.IsInfinity(instalment) || instalment <= 0 || instalment > li.fAmount) instalment = li.fAmount;
                debts.Add(new Debt
                {
                    Kind = DebtKind.Loan, Id = Debt.MakeId(DebtKind.Loan, li.strPayee, li.fTime, li.strDesc ?? ""),
                    Creditor = Name(li.strPayee), Description = li.strDesc ?? "", Amount = li.fAmount, Since = li.fTime,
                    Instalment = instalment, ShiftsLeft = BankRules.ShiftsLeft(now - li.fTime)
                });
            }

        var bills = Ledger.GetUnpaidLIs(null, id, null, false) ?? new List<LedgerLI>();
        foreach (var li in bills.Where(l => l != null && l.strCurrency == BankRules.Currency && l.fAmount > BankRules.PaidOffBelow))
            debts.Add(new Debt
            {
                Kind = DebtKind.Bill, Id = Debt.MakeId(DebtKind.Bill, li.strPayee, li.fTime, li.strDesc ?? ""),
                Creditor = Name(li.strPayee), Description = li.strDesc ?? "", Amount = li.fAmount, Since = li.fTime,
                Late = BankRules.IsLate(li.strDesc, overdue, late)
            });

        foreach (var li in Ledger.GetUnpaidLIs(null, id, null, true) ?? new List<LedgerLI>())
        {
            if (li == null || li.strCurrency != BankRules.Currency || !Every(li.Repeats, out var every)) continue;
            debts.Add(new Debt
            {
                Kind = DebtKind.Charge, Id = Debt.MakeId(DebtKind.Charge, li.strPayee, li.fTime, li.strDesc ?? ""),
                Creditor = Name(li.strPayee), Description = li.strDesc ?? "", Amount = li.fAmount, Since = li.fTime, Every = every
            });
        }
        return DebtSummary.Of(debts);
    }

    /// <summary>The game's running loans (its private mortgage list), or none when it cannot be read.</summary>
    internal static IEnumerable<LedgerLI> Mortgages() => CanReadLoans && MortgageField!.GetValue(null) is List<LedgerLI> list ? list : Enumerable.Empty<LedgerLI>();

    /// <summary>When the oldest late unpaid bill to this creditor was raised, by the game's own wording, or null when
    /// none is late.</summary>
    internal static double? LateSince(string payee, string playerId)
    {
        string overdue = DataHandler.GetString("GUI_FINANCE_OVERDUE"), late = DataHandler.GetString("GUI_FINANCE_LATE");
        var times = (Ledger.GetUnpaidLIs(payee, playerId, null, false) ?? new List<LedgerLI>()).Where(l => l != null && BankRules.IsLate(l.strDesc, overdue, late)).Select(l => l.fTime).ToList();
        return times.Count == 0 ? null : times.Min();
    }

    /// <summary>Cash the player carries now (the game's money condition).</summary>
    internal static double Cash() => CrewSim.coPlayer?.GetCondAmount(BankRules.Currency) ?? 0;

    /// <summary>Opens the game's own Finances window, where bills are paid. Replaces the app's panel.</summary>
    internal static bool OpenFinances(out string message)
    {
        if (CrewSim.objInstance == null || CrewSim.coPlayer == null) { message = Text.Get("Finances.no_game"); return false; }
        if (CrewSim.bUILock) { message = Text.Get("Finances.busy"); return false; }
        CrewSim.objInstance.ToggleFinances();
        message = Text.Get("Finances.opened");
        return true;
    }

    // The Finances window names a creditor the same way: a person or object the game knows by ID, else the name as written.
    private static string Name(string? payee) =>
        string.IsNullOrEmpty(payee) ? Text.Get("Debt.unknown_creditor") :
        DataHandler.mapCOs != null && DataHandler.mapCOs.TryGetValue(payee!, out var co) && co != null && !string.IsNullOrEmpty(co.strName) ? co.strName : payee!;

    private static bool Every(LedgerLI.Frequency frequency, out ChargeEvery every)
    {
        switch (frequency)
        {
            case LedgerLI.Frequency.Hourly: every = ChargeEvery.Hour; return true;
            case LedgerLI.Frequency.Shiftly: every = ChargeEvery.Shift; return true;
            case LedgerLI.Frequency.Daily: every = ChargeEvery.Day; return true;
            case LedgerLI.Frequency.Monthly: every = ChargeEvery.Month; return true;
            case LedgerLI.Frequency.Yearly: every = ChargeEvery.Year; return true;
            default: every = ChargeEvery.Shift; return false;
        }
    }
}
