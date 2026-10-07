using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Story;
using PhobosBank.Core;

namespace PhobosBank;

/// <summary>One credit line as the app shows it: its terms, the player's account if open, and why it cannot be opened
/// or drawn on when it cannot.</summary>
internal sealed class LineView
{
    public string Id = "", Name = "", Pitch = "";
    public CreditLineEntry Entry = null!;
    public Account? Account;
    /// <summary>The open balance cycle, if any.</summary>
    public Loan? Cycle;
    /// <summary>Owed now, what may still be drawn (fee included), and the minimum due this shift (the game's instalment).</summary>
    public double Owed, Available, Minimum;
    public double Limit, RatePerShift, DrawFee, MinDraw;
    /// <summary>Why the line cannot be opened now (no account yet), or null.</summary>
    public string? CannotOpen;
}

/// <summary>Credit lines (Phobos Banking 0.6.0; owner choices, 7 October 2026: a system-wide service, opened from
/// anywhere through the PDA, one revolving balance, a draw fee plus a higher rate). The balance is one of the game's own
/// mortgage lines: its shift instalment is the minimum payment, and the Finances window's Prepay pays more. A draw adds
/// the amount and its fee to that line and restarts its term, as the game's own Prepay does, so the minimum follows the
/// new balance. Interest is billed by <see cref="Loans.Poll"/> like any Phobos loan, and the game's late fee applies to
/// unpaid bills as to any other. The panel and F3 command only call this service.</summary>
internal static class CreditLines
{
    internal static List<LineView> Views()
    {
        bool attached = Loans.Attach(out var refusal);
        var views = new List<LineView>();
        foreach (var pair in Lenders.Lines)
        {
            var c = pair.Value;
            var view = new LineView { Id = pair.Key, Entry = c, Name = Lenders.LineName(pair.Key), Pitch = Lenders.LinePitch(pair.Key) };
            Account? account = attached && Loans.Book.Accounts.TryGetValue(pair.Key, out var a) ? a : null;
            view.Account = account;
            view.Limit = account?.Limit ?? c.limit; view.RatePerShift = account?.RatePerShift ?? c.ratePerShift;
            view.DrawFee = account?.DrawFee ?? c.drawFee; view.MinDraw = account?.MinDraw ?? c.minDraw;
            if (attached && CrewSim.coPlayer != null)
            {
                view.Cycle = Loans.Book.Open.FirstOrDefault(l => l.Kind == LenderSchema.Line && l.Lender == pair.Key);
                var line = view.Cycle == null ? null : Loans.Line(view.Cycle, CrewSim.coPlayer.strID);
                view.Owed = line?.fAmount ?? 0;
                view.Minimum = line == null ? 0 : Minimum(line);
            }
            view.Available = LoanRules.LineAvailable(view.Limit, view.Owed);
            view.CannotOpen = !attached ? refusal : account != null ? null : StoryGates.Blocked(c.requires) is string blocked ? Text.Get("Line.requires", blocked) : null;
            views.Add(view);
        }
        return views.OrderBy(v => v.Account == null ? 1 : 0).ThenBy(v => v.Name, StringComparer.Ordinal).ToList();
    }

    /// <summary>The game's own instalment for the line now, never more than the balance.</summary>
    internal static double Minimum(LedgerLI line)
    {
        double m = MathUtils.MortgagePaymentPerShift(line);
        return double.IsNaN(m) || double.IsInfinity(m) || m <= 0 || m > line.fAmount ? line.fAmount : m;
    }

    /// <summary>Opens the player's account with a credit line, from anywhere. The terms are copied now.</summary>
    internal static bool Open(string id, out string message)
    {
        if (!Loans.Attach(out var refusal)) { message = refusal!; return false; }
        var view = Views().FirstOrDefault(v => v.Id == id);
        if (view == null) { message = Text.Get("Line.unknown", id); return false; }
        if (view.Account != null) { message = Text.Get("Line.already_open", view.Name); return false; }
        if (view.CannotOpen != null) { message = view.CannotOpen; return false; }
        var c = view.Entry;
        Loans.Book.Accounts[id] = new Account
        {
            Line = id, Payee = LoanRules.Clean(view.Name), Limit = c.limit, RatePerShift = c.ratePerShift, DrawFee = c.drawFee, MinDraw = c.minDraw, Opened = StarSystem.fEpoch
        };
        Loans.SaveBook();
        Loans.Flag(LoanRules.Flag(id, LoanRules.LineOpen), true);
        Loans.Story(id, LoanRules.LineOpened);
        message = Text.Get("Line.opened", view.Name, BankPanel.Money(c.limit), Loans.Percent(c.ratePerShift), Percent(c.drawFee));
        CrewSim.coPlayer?.LogMessage(message, "Neutral", CrewSim.coPlayer.strID);
        return true;
    }

    /// <summary>Draws cash on the player's credit line: the amount is paid into their account now, and the amount plus
    /// its fee is added to the line's balance, whose term restarts so the minimum follows the new balance.</summary>
    internal static bool Draw(string id, double amount, out string message)
    {
        if (!Loans.Attach(out var refusal)) { message = refusal!; return false; }
        var view = Views().FirstOrDefault(v => v.Id == id);
        if (view == null) { message = Text.Get("Line.unknown", id); return false; }
        if (view.Account is not Account account) { message = Text.Get("Line.not_open", view.Name); return false; }
        if (!LoanRules.CanDraw(amount, account.DrawFee, account.MinDraw, view.Available))
        {
            message = Text.Get("Line.amount", BankPanel.Money(account.MinDraw), BankPanel.Money(LoanRules.MaxDraw(view.Available, account.DrawFee)));
            return false;
        }
        var player = CrewSim.coPlayer!;
        double fee = LoanRules.DrawFee(amount, account.DrawFee), added = amount + fee;
        var line = view.Cycle == null ? null : Loans.Line(view.Cycle, player.strID);
        if (view.Cycle != null && line != null)
        {
            // As the game's own Prepay does: the balance changes and the term starts again from now.
            line.fAmount += (float)added;
            line.fTime = StarSystem.fEpoch;
            view.Cycle.Principal += added;
        }
        else
        {
            var cycle = Loans.Open(id, account.Payee, account.RatePerShift, added, LenderSchema.Line, null,
                LoanRules.LoanDescription(Text.Get("Ledger.line", account.Payee), Loans.Book.Next));
            Ledger.AddLI(new LedgerLI(cycle.Payee, player.strID, (float)added, cycle.Description, Ledger.CURRENCY, StarSystem.fEpoch, LedgerLI.Frequency.Mortgage));
        }
        player.AddCondAmount(Ledger.CURRENCY, amount);
        Ledger.RecordTransaction(player, account.Payee, amount, LoanRules.Clean(Text.Get("Ledger.drawn", account.Payee)));
        Loans.SaveBook();
        message = Text.Get("Line.drawn", BankPanel.Money(amount), view.Name, BankPanel.Money(fee), BankPanel.Money(view.Owed + added), Loans.Percent(account.RatePerShift));
        player.LogMessage(message, "Neutral", player.strID);
        return true;
    }

    /// <summary>The line to act on when the player names none: their open account, if they have exactly one, else the
    /// only line there is.</summary>
    internal static string? DefaultId()
    {
        var views = Views();
        var open = views.Where(v => v.Account != null).ToList();
        return open.Count == 1 ? open[0].Id : views.Count == 1 ? views[0].Id : null;
    }

    /// <summary>F3 <c>phobosbank line</c>.</summary>
    internal static string Describe()
    {
        var views = Views();
        if (views.Count == 0) return Text.Get("Line.none");
        return string.Join("\n", views.Select(v => v.Account == null
            ? Text.Get("Console.line_closed", v.Id, v.Name, BankPanel.Money(v.Limit), Loans.Percent(v.RatePerShift), Percent(v.DrawFee), v.CannotOpen ?? Text.Get("Line.can_open"))
            : Text.Get("Console.line_open", v.Id, v.Name, BankPanel.Money(v.Owed), BankPanel.Money(v.Available), BankPanel.Money(v.Minimum), Loans.Percent(v.RatePerShift), Percent(v.DrawFee))));
    }

    internal static string Percent(double share) => (share * 100).ToString("0.#");
}
