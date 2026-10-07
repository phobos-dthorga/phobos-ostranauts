using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhobosBank.Core;

public enum LoanState { Open, Repaid, Settled }

/// <summary>One loan from a Phobos lender. The balance is the game's own mortgage line, found by
/// <see cref="Payee"/> and <see cref="Description"/>; this record holds what the game does not: the lender, the terms
/// fixed when it was taken, and how far its interest has been billed.</summary>
public sealed class Loan
{
    public int Number;
    /// <summary>The lender's id in the lenders pack, and the creditor name on the ledger when the loan was taken.</summary>
    public string Lender = "", Payee = "";
    /// <summary>The mortgage line's description, exactly as written to the ledger.</summary>
    public string Description = "";
    public double Principal, RatePerShift, Opened;
    /// <summary>The game's shift count (<see cref="Phobos.Ostranauts.Framework.GameClock.ShiftCount"/>) interest has been billed to.</summary>
    public long BilledTo;
    /// <summary>All interest billed so far, in credits.</summary>
    public double InterestBilled;
    public LoanState State = LoanState.Open;
    /// <summary><c>cash</c>, <c>ship</c>, <c>home</c> or <c>line</c> (a credit line's balance, 0.6.0); the registration of
    /// the ship or apartment for ship and home.</summary>
    public string Kind = LenderSchema.Cash;
    public string? Collateral;
    /// <summary>When the loan was repaid or settled.</summary>
    public double? Closed;
}

/// <summary>A pre-approval for a ship or home loan (Phobos Banking 0.3.0): the lender, the most it will finance and
/// until when. Used once, at a broker where the lender trades.</summary>
public sealed class Approval
{
    public string Lender = "", Kind = LenderSchema.Ship;
    public double Limit, Expires;
}

/// <summary>A credit line account (Phobos Banking 0.6.0): the service and its terms, copied when it was opened, so a
/// later change to the lenders file never changes an open account. Its balance is the open <c>line</c> loan.</summary>
public sealed class Account
{
    public string Line = "", Payee = "";
    public double Limit, RatePerShift, DrawFee, MinDraw, Opened;
}

/// <summary>The player's loan book, saved on the player character as one Phobos record (<c>PhobosState.PhobosBank</c>).
/// Fields this version does not understand are kept exactly as they were.</summary>
public sealed class LoanBook
{
    public const string Name = "PhobosBank";
    public const int Version = 1;
    private const string LoanPrefix = "loan.", NextKey = "next", ApprovalKey = "approval", AccountPrefix = "account.", FormatTag = "1";
    public int Next = 1;
    public Dictionary<int, Loan> Loans { get; } = new();
    public Approval? Approval;
    /// <summary>Credit line accounts by line id (0.6.0).</summary>
    public Dictionary<string, Account> Accounts { get; } = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> kept = new(StringComparer.Ordinal);

    public IEnumerable<Loan> Open => Loans.Values.Where(l => l.State == LoanState.Open).OrderBy(l => l.Number);

    public Loan Add(Loan loan)
    {
        loan.Number = Next++;
        Loans[loan.Number] = loan;
        return loan;
    }

    public static LoanBook Decode(IReadOnlyDictionary<string, string>? fields)
    {
        var book = new LoanBook();
        foreach (var pair in fields ?? new Dictionary<string, string>())
        {
            if (pair.Key == NextKey && int.TryParse(pair.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int next) && next > 0) book.Next = next;
            else if (pair.Key.StartsWith(LoanPrefix, StringComparison.Ordinal) && int.TryParse(pair.Key.Substring(LoanPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int n) &&
                     TryLoan(n, pair.Value, out var loan)) book.Loans[n] = loan;
            else if (pair.Key == ApprovalKey && TryApproval(pair.Value, out var approval)) book.Approval = approval;
            else if (pair.Key.StartsWith(AccountPrefix, StringComparison.Ordinal) && TryAccount(pair.Key.Substring(AccountPrefix.Length), pair.Value, out var account)) book.Accounts[account.Line] = account;
            else book.kept[pair.Key] = pair.Value;
        }
        // A record whose counter was lost never reuses a number.
        if (book.Loans.Count > 0) book.Next = Math.Max(book.Next, book.Loans.Keys.Max() + 1);
        return book;
    }

    public Dictionary<string, string> Encode()
    {
        var fields = new Dictionary<string, string>(kept, StringComparer.Ordinal) { [NextKey] = Next.ToString(CultureInfo.InvariantCulture) };
        foreach (var loan in Loans.Values)
            fields[LoanPrefix + loan.Number.ToString(CultureInfo.InvariantCulture)] = string.Join("|", FormatTag, loan.Lender, loan.Payee, loan.Description,
                Num(loan.Principal), Num(loan.RatePerShift), Num(loan.Opened), loan.BilledTo.ToString(CultureInfo.InvariantCulture), Num(loan.InterestBilled),
                State(loan.State), loan.Kind, loan.Collateral ?? "", loan.Closed is double c ? Num(c) : "");
        if (Approval is Approval a) fields[ApprovalKey] = string.Join("|", FormatTag, a.Lender, a.Kind, Num(a.Limit), Num(a.Expires));
        foreach (var account in Accounts.Values)
            fields[AccountPrefix + account.Line] = string.Join("|", FormatTag, account.Payee, Num(account.Limit), Num(account.RatePerShift), Num(account.DrawFee), Num(account.MinDraw), Num(account.Opened));
        return fields;
    }

    private static string Num(double v) => v.ToString("R", CultureInfo.InvariantCulture);
    private static bool TryNum(string s, out double v) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && !double.IsNaN(v) && !double.IsInfinity(v);
    private static string State(LoanState s) => s switch { LoanState.Open => "open", LoanState.Repaid => "repaid", _ => "settled" };

    private static bool TryLoan(int number, string value, out Loan loan)
    {
        loan = new Loan { Number = number };
        var p = value.Split('|');
        if (p.Length != 13 || p[0] != FormatTag || p[1].Length == 0 || p[2].Length == 0 || p[3].Length == 0) return false;
        loan.Lender = p[1]; loan.Payee = p[2]; loan.Description = p[3];
        if (!TryNum(p[4], out loan.Principal) || !TryNum(p[5], out loan.RatePerShift) || !TryNum(p[6], out loan.Opened) ||
            !long.TryParse(p[7], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out loan.BilledTo) || !TryNum(p[8], out loan.InterestBilled)) return false;
        switch (p[9]) { case "open": loan.State = LoanState.Open; break; case "repaid": loan.State = LoanState.Repaid; break; case "settled": loan.State = LoanState.Settled; break; default: return false; }
        if (!LenderSchema.LoanKinds.Contains(p[10])) return false;
        loan.Kind = p[10];
        loan.Collateral = p[11].Length == 0 ? null : p[11];
        if (p[12].Length > 0) { if (!TryNum(p[12], out double closed)) return false; loan.Closed = closed; }
        return true;
    }

    private static bool TryAccount(string line, string value, out Account account)
    {
        account = new Account { Line = line };
        var p = value.Split('|');
        if (p.Length != 7 || p[0] != FormatTag || line.Length == 0 || p[1].Length == 0) return false;
        account.Payee = p[1];
        return TryNum(p[2], out account.Limit) && account.Limit > 0 && TryNum(p[3], out account.RatePerShift) && TryNum(p[4], out account.DrawFee) &&
               TryNum(p[5], out account.MinDraw) && TryNum(p[6], out account.Opened);
    }

    private static bool TryApproval(string value, out Approval approval)
    {
        approval = new Approval();
        var p = value.Split('|');
        if (p.Length != 5 || p[0] != FormatTag || p[1].Length == 0 || !LenderSchema.Offers.Contains(p[2]) || p[2] == LenderSchema.Cash) return false;
        approval.Lender = p[1]; approval.Kind = p[2];
        return TryNum(p[3], out approval.Limit) && approval.Limit > 0 && TryNum(p[4], out approval.Expires);
    }
}

/// <summary>The loan rules with no game types: interest, what a lender will still lend, and the ledger wording.</summary>
public static class LoanRules
{
    /// <summary>Interest for shift changes on a balance: the lender's rate on the balance at each change. A long skip is
    /// billed as one line for every shift it crossed.</summary>
    public static double Interest(double balance, double ratePerShift, long shifts) =>
        balance <= BankRules.PaidOffBelow || shifts <= 0 || ratePerShift <= 0 ? 0 : balance * ratePerShift * shifts;

    /// <summary>What interest a loan costs in all if every instalment is paid when it falls due: the game's schedule
    /// (the instalments of <see cref="BankRules.Instalment"/> from a new loan), the lender's rate on each shift's balance.</summary>
    public static double TotalInterest(double principal, double ratePerShift)
    {
        double balance = principal, total = 0;
        for (int left = BankRules.ShiftsLeft(0); left > 0 && balance > BankRules.PaidOffBelow; left--)
        {
            // The game raises the first instalment as the loan is made, then one at each shift change; interest follows
            // the balance left after each.
            balance -= BankRules.Instalment(balance, left);
            total += Interest(balance, ratePerShift, 1);
        }
        return total;
    }

    /// <summary>How much more a lender will lend now: its limit less what is still owed to it, never below zero.</summary>
    public static double Headroom(double maxPrincipal, double owed) => Math.Max(0, maxPrincipal - Math.Max(0, owed));

    /// <summary>A loan amount the player may pick: whole hundreds, within the lender's smallest loan and its headroom.</summary>
    public static double Clamp(double amount, double min, double headroom) =>
        headroom < min ? 0 : Math.Max(min, Math.Min(headroom, Math.Floor(amount / 100) * 100));

    /// <summary>The lowest down payment, as a share of the price, at which a lender finances a broker purchase: its own
    /// least share, or more when the price is above what it will finance. Null when it cannot finance even its smallest
    /// loan at that price (Phobos Banking 0.3.0).</summary>
    public static double? MinDownShare(double price, double lenderShare, double limit, double minPrincipal)
    {
        if (!(price > 0) || limit < minPrincipal || price < minPrincipal) return null;
        double share = Math.Max(lenderShare, 1 - limit / price);
        // Paying more down than this would leave less than the lender's smallest loan to finance.
        return share > 1 - minPrincipal / price + 1e-9 ? null : Math.Min(1, share);
    }

    /// <summary>The step the amount moves by in the app: a thousand for small loans, more for large ones, so the
    /// whole range is a few dozen presses at most.</summary>
    public static double Step(double min, double headroom) => headroom <= 50000 ? 1000 : headroom <= 250000 ? 5000 : 10000;

    /// <summary>The mortgage line's description: the catalogue's wording with the loan number appended in code as
    /// <c>(#n)</c>. The game finds the loan an instalment pays off by looking for this description inside the
    /// instalment's own, so the closing bracket keeps loan 3 from matching loan 30.</summary>
    public static string LoanDescription(string worded, int number) => Clean(worded) + " (#" + number.ToString(CultureInfo.InvariantCulture) + ")";

    /// <summary>Ledger and save-safe text: separators the save store and our record use become plain punctuation.</summary>
    public static string Clean(string text)
    {
        var chars = (text ?? "").Select(c => c == '|' ? '/' : c == ',' ? ';' : c == '=' ? '-' : c == '#' ? 'n' : char.IsControl(c) ? ' ' : c).ToArray();
        return new string(chars).Trim();
    }

    /// <summary>What is left to draw on a credit line: its limit less what is owed, never below zero.</summary>
    public static double LineAvailable(double limit, double owed) => Math.Max(0, limit - Math.Max(0, owed));

    /// <summary>The fee on a draw, to the cent.</summary>
    public static double DrawFee(double amount, double feeShare) => amount <= 0 || feeShare <= 0 ? 0 : Math.Round(amount * feeShare, 2);

    /// <summary>The largest draw, in whole hundreds, whose fee still fits in what is available.</summary>
    public static double MaxDraw(double available, double feeShare) => Math.Max(0, Math.Floor(available / (1 + Math.Max(0, feeShare)) / 100) * 100);

    /// <summary>Whether a draw may be made: whole hundreds, at least the smallest draw, and with its fee within what is
    /// available.</summary>
    public static bool CanDraw(double amount, double feeShare, double minDraw, double available) =>
        amount >= minDraw && Math.Abs(amount / 100 - Math.Round(amount / 100)) < 1e-9 && amount + DrawFee(amount, feeShare) <= available + 0.005;

    /// <summary>How a loan ends when the game's ledger no longer holds a balance for it (0.6.0 fix). The game removes a
    /// mortgage line when the Finances window's Prepay pays it off and when a ship sale's escrow covers it. A cash loan
    /// or a credit line has no collateral, so it was repaid; a ship or apartment loan was repaid unless the collateral
    /// has changed hands, when the sale settled it.</summary>
    public static LoanState Closed(string kind, bool lineGone, bool collateralKept) =>
        !lineGone || kind == LenderSchema.Cash || kind == LenderSchema.Line || collateralKept ? LoanState.Repaid : LoanState.Settled;

    /// <summary>Story flags a lender's loans set: borrowed, repaid, late.</summary>
    public static string Flag(string lender, string what) => "bank-" + lender + "-" + what;
    public const string Borrowed = "borrowed", Repaid = "repaid", Late = "late", LateLong = "late-long";
    /// <summary>A credit line's own event and flag (0.6.0): <c>bank-&lt;id&gt;-line-opened</c> and <c>bank-&lt;id&gt;-line-open</c>.</summary>
    public const string LineOpened = "line-opened", LineOpen = "line-open";

    /// <summary>The story arc a lender's event starts when a story pack has one (Phobos Banking 0.4.0): the
    /// <c>bank-&lt;lender&gt;-&lt;event&gt;</c> arc for <c>borrowed</c>, <c>late</c>, <c>late-long</c> and <c>repaid</c>.</summary>
    public static string Arc(string lender, string what) => "bank-" + lender + "-" + what;
    public static readonly IReadOnlyList<string> Events = new[] { Borrowed, Late, LateLong, Repaid };

    /// <summary>Whether bills have been late long enough for a lender's sterner letter: the oldest late bill raised at
    /// least <see cref="BankRules.LateLongDays"/> game days ago.</summary>
    public static bool LongLate(double? oldestLate, double now) => oldestLate is double t && now - t >= BankRules.LateLongDays * Phobos.Ostranauts.Framework.GameClock.DaySeconds;
}
