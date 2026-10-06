using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Story;
using PhobosBank.Core;

namespace PhobosBank;

/// <summary>One lender as the app shows it: its terms, what it would lend now, and why not when it would not.</summary>
internal sealed class LenderView
{
    public string Id = "", Name = "", Pitch = "", Home = "";
    public LenderEntry Entry = null!;
    /// <summary>Null when the player may borrow here now; otherwise the one thing in the way.</summary>
    public string? Unavailable;
    public double Owed, Headroom;
    public int OpenLoans;
    /// <summary>What a loan of the lender's largest amount available now would cost in interest if paid on time, as a
    /// share of the amount borrowed.</summary>
    public double InterestShare;
}

/// <summary>Loans from Phobos lenders (Phobos Banking 0.2.0). A loan is the game's own mortgage line with the lender as
/// creditor, so the game raises its instalments each shift, shows and takes payment in the Finances window, adds its
/// late fee and repays it from a mortgaged ship's sale. The game's mortgage charges no interest, so this service adds
/// the lender's interest as a bill at each shift change, on the balance the line holds then. It also notices when a
/// loan is repaid or settled and keeps story flags of what happened. The panel and F3 command only call it.</summary>
internal static class Loans
{
    private static CondOwner? owner;
    private static LoanBook book = new();
    private static SavedStateStatus status = SavedStateStatus.Missing;

    /// <summary>A new game or a load: the next poll reads the player's book again.</summary>
    internal static void Reset() { owner = null; book = new LoanBook(); status = SavedStateStatus.Missing; }

    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, LoanBook.Name, Plugin.Id, LoanBook.Version);

    /// <summary>The player's book, read on first use after a load or a change of player character. False with no game,
    /// or when the saved record is one this version must not touch (a newer version's, or damaged): then nothing
    /// borrows or bills until that is sorted out, and the log says so once.</summary>
    internal static bool Attach(out string? refusal)
    {
        refusal = null;
        var player = CrewSim.coPlayer;
        if (player == null || player.bDestroyed || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) { refusal = Text.Get("Panel.no_game"); return false; }
        if (!ReferenceEquals(owner, player))
        {
            owner = player;
            status = Store(player).Read(out var fields);
            book = status == SavedStateStatus.Ready ? LoanBook.Decode(fields) : new LoanBook();
            if (status != SavedStateStatus.Ready && status != SavedStateStatus.Missing) Plugin.Log(Text.Get("Loans.record_kept", status));
        }
        if (status != SavedStateStatus.Ready && status != SavedStateStatus.Missing) { refusal = Text.Get("Loans.record_unreadable"); return false; }
        return true;
    }

    internal static LoanBook Book => book;

    internal static bool SaveBook() => Save();

    private static bool Save()
    {
        if (owner == null || owner.bDestroyed) return false;
        if (Store(owner).TryWriteIfChanged(book.Encode())) { status = SavedStateStatus.Ready; return true; }
        Plugin.Log(Text.Get("Loans.record_refused"));
        return false;
    }

    /// <summary>The game's mortgage line for a loan, or null when it is gone (sold with its ship, or removed).</summary>
    internal static LedgerLI? Line(Loan loan, string playerId) =>
        Debts.Mortgages().FirstOrDefault(m => m != null && m.strPayor == playerId && m.strPayee == loan.Payee && m.strDesc == loan.Description);

    internal static double Owed(string lender) => owner == null ? 0 : book.Open.Where(l => l.Lender == lender).Sum(l => Line(l, owner.strID)?.fAmount ?? 0);

    /// <summary>Every lender in the pack, with what it would lend now. Lenders here come first.</summary>
    internal static List<LenderView> Offers()
    {
        bool attached = Attach(out var refusal);
        var views = new List<LenderView>();
        foreach (var pair in Lenders.All)
        {
            var l = pair.Value;
            var view = new LenderView { Id = pair.Key, Entry = l, Name = Lenders.Name(pair.Key), Pitch = Lenders.Pitch(pair.Key), Home = Lenders.Home(l) };
            if (attached)
            {
                view.OpenLoans = book.Open.Count(x => x.Lender == pair.Key);
                view.Owed = Owed(pair.Key);
                view.Headroom = LoanRules.Headroom(l.maxPrincipal, view.Owed);
            }
            view.InterestShare = LoanRules.TotalInterest(1000, l.ratePerShift) / 1000;
            view.Unavailable = attached ? Unavailable(pair.Key, l, view) : refusal;
            views.Add(view);
        }
        return views.OrderBy(v => v.Unavailable == null ? 0 : 1).ThenBy(v => v.Name, StringComparer.Ordinal).ToList();
    }

    /// <summary>The one thing that keeps the player from borrowing here now, or null.</summary>
    private static string? Unavailable(string id, LenderEntry l, LenderView view)
    {
        if (!StoryLocation.Near(l.home)) return Text.Get("Lender.away", view.Home);
        if (StoryGates.Blocked(l.requires) is string blocked) return Text.Get("Lender.requires", blocked);
        if (view.OpenLoans >= l.maxLoans) return Text.Get(BankRules.CountKey("Lender.max_loans", l.maxLoans), l.maxLoans);
        if (view.Headroom < l.minPrincipal) return Text.Get("Lender.limit", BankPanel.Money(l.maxPrincipal));
        return null;
    }

    /// <summary>Borrows cash from a lender: the money is paid into the player's account now, and the loan is the game's
    /// own mortgage line with the lender as creditor, plus the lender's interest at each shift change.</summary>
    internal static bool Borrow(string lenderId, double amount, out string message)
    {
        if (!Attach(out var refusal)) { message = refusal!; return false; }
        var view = Offers().FirstOrDefault(v => v.Id == lenderId);
        if (view == null) { message = Text.Get("Lender.unknown", lenderId); return false; }
        if (!view.Entry.offers.Contains(LenderSchema.Cash)) { message = Text.Get("Lender.no_cash", view.Name); return false; }
        if (view.Unavailable != null) { message = view.Unavailable; return false; }
        double sum = LoanRules.Clamp(amount, view.Entry.minPrincipal, view.Headroom);
        if (sum <= 0 || Math.Abs(sum - amount) > 0.5) { message = Text.Get("Lender.amount", BankPanel.Money(view.Entry.minPrincipal), BankPanel.Money(view.Headroom)); return false; }

        var player = owner!;
        var loan = Open(lenderId, view, sum, LenderSchema.Cash, null, LoanRules.LoanDescription(Text.Get("Ledger.loan", view.Name), book.Next));
        player.AddCondAmount(Ledger.CURRENCY, sum);
        Ledger.RecordTransaction(player, loan.Payee, sum, LoanRules.Clean(Text.Get("Ledger.borrowed", loan.Payee)));
        Ledger.AddLI(new LedgerLI(loan.Payee, player.strID, (float)sum, loan.Description, Ledger.CURRENCY, StarSystem.fEpoch, LedgerLI.Frequency.Mortgage));
        Save();
        message = Text.Get("Loans.borrowed", BankPanel.Money(sum), view.Name, Percent(view.Entry.ratePerShift));
        player.LogMessage(message, "Neutral", player.strID);
        return true;
    }

    /// <summary>Records a new loan in the book, terms fixed now, and marks it for story content.</summary>
    internal static Loan Open(string lenderId, LenderView view, double principal, string kind, string? collateral, string description)
    {
        var loan = book.Add(new Loan
        {
            Lender = lenderId, Payee = LoanRules.Clean(view.Name), Description = description, Principal = principal, RatePerShift = view.Entry.ratePerShift,
            Opened = StarSystem.fEpoch, BilledTo = GameClock.ShiftCount(StarSystem.fEpoch), Kind = kind, Collateral = collateral
        });
        Flag(LoanRules.Flag(lenderId, LoanRules.Borrowed), true);
        return loan;
    }

    /// <summary>Bills interest due at shift changes since the last poll, closes loans the game has repaid or settled,
    /// and keeps the late flags. Cheap when there are no loans.</summary>
    internal static void Poll()
    {
        if (CrewSim.coPlayer == null || !Attach(out _)) return;
        // A pre-approval that has run out (or whose lender is gone) is dropped, and the crew log says so.
        if (book.Approval is Approval approval && !Financing.Stands(approval))
        {
            book.Approval = null; Save();
            CrewSim.coPlayer.LogMessage(Text.Get("Financing.expired", Lenders.Name(approval.Lender)), "Neutral", CrewSim.coPlayer.strID);
        }
        if (book.Loans.Count == 0) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Loans);
        var player = owner!;
        double now = StarSystem.fEpoch;
        long count = GameClock.ShiftCount(now);
        bool changed = false;
        foreach (var loan in book.Open.ToArray())
        {
            var line = Line(loan, player.strID);
            if (line == null || line.fAmount <= BankRules.PaidOffBelow || line.Paid)
            {
                loan.State = line == null ? LoanState.Settled : LoanState.Repaid; loan.Closed = now; changed = true;
                if (loan.State == LoanState.Repaid) Flag(LoanRules.Flag(loan.Lender, LoanRules.Repaid), true);
                player.LogMessage(Text.Get(loan.State == LoanState.Repaid ? "Loans.repaid" : "Loans.settled", loan.Payee), "Good", player.strID);
                continue;
            }
            if (count > loan.BilledTo)
            {
                long shifts = count - loan.BilledTo;
                double interest = LoanRules.Interest(line.fAmount, loan.RatePerShift, shifts);
                loan.BilledTo = count; changed = true;
                if (interest >= 0.01)
                {
                    string description = LoanRules.Clean(Text.Get(BankRules.CountKey("Ledger.interest", (int)Math.Min(shifts, int.MaxValue)), loan.Payee, shifts, BankPanel.Money(line.fAmount)));
                    // Paying a bill whose description holds a loan's would pay the loan down instead: never let them meet.
                    if (description.IndexOf(loan.Description, StringComparison.Ordinal) >= 0) description = LoanRules.Clean(Text.Get("Ledger.interest_plain", loan.Payee));
                    Ledger.AddLI(new LedgerLI(loan.Payee, player.strID, (float)interest, description, Ledger.CURRENCY, now, LedgerLI.Frequency.OneTime));
                    loan.InterestBilled += interest;
                }
            }
        }
        // A lender's late flag stands while any of its bills is late.
        foreach (var lender in book.Loans.Values.Select(l => l.Lender).Distinct().ToArray())
        {
            var payees = book.Loans.Values.Where(l => l.Lender == lender).Select(l => l.Payee).Distinct();
            Flag(LoanRules.Flag(lender, LoanRules.Late), payees.Any(p => Debts.LateTo(p, player.strID)));
        }
        if (changed) Save();
    }

    private static void Flag(string flag, bool on)
    {
        try { if (on != StoryFlags.Has(flag)) { if (on) StoryFlags.Set(flag); else StoryFlags.Clear(flag); } }
        catch (ArgumentException ex) { Plugin.Log(ex.Message); }
    }

    internal static string Percent(double share) => (share * 100).ToString(share * 100 < 0.1 ? "0.###" : "0.##");

    /// <summary>F3 <c>phobosbank loans</c>: the book as text.</summary>
    internal static string Describe()
    {
        if (!Attach(out var refusal)) return refusal!;
        if (book.Loans.Count == 0) return Text.Get("Loans.none");
        var lines = new List<string>();
        foreach (var loan in book.Loans.Values.OrderBy(l => l.Number))
        {
            var line = owner != null ? Line(loan, owner.strID) : null;
            lines.Add(Text.Get("Loans.line", loan.Number, loan.Payee, Text.Get("Loans.state_" + loan.State), BankPanel.Money(loan.Principal), BankPanel.Money(line?.fAmount ?? 0),
                Percent(loan.RatePerShift), BankPanel.Money(loan.InterestBilled)));
        }
        return string.Join("\n", lines);
    }
}
