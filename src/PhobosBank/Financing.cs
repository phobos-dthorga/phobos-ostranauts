using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework;
using PhobosBank.Core;

namespace PhobosBank;

/// <summary>Financing at the broker (Phobos Banking 0.3.0; owner choice, 6 October 2026: loans may fund broker purchases,
/// integrated neatly with the broker's own window). The player asks a lender for a pre-approval in the Credit panel; at a
/// broker where that lender trades, the broker's own purchase window then lets the down payment go as low as the
/// lender allows, within the approved amount, and on confirming, the mortgage the broker writes passes to the lender,
/// who bills interest on it as on any of its loans. The game's own sale escrow, prepay and late fees keep working,
/// because the loan stays the game's own mortgage line. Choosing not to use a pre-approval leaves the broker exactly as
/// it is.</summary>
internal static class Financing
{
    /// <summary>A purchase window opened with a lender's offer, until the purchase or the next window.</summary>
    internal sealed class Offer
    {
        public string Lender = "", RegId = "", Kind = LenderSchema.Ship, Name = "";
        public double MinShare, Limit, Rate;
    }
    private static Offer? pending;
    internal static void Reset() => pending = null;

    /// <summary>Asks a lender to pre-approve a ship or home purchase: valid for a game day, up to what the lender would
    /// lend now. Replaces any earlier pre-approval.</summary>
    internal static bool PreApprove(string lenderId, string kind, out string message)
    {
        if (!Loans.Attach(out var refusal)) { message = refusal!; return false; }
        var view = Loans.Offers().FirstOrDefault(v => v.Id == lenderId);
        if (view == null) { message = Text.Get("Lender.unknown", lenderId); return false; }
        if (kind == LenderSchema.Cash || !view.Entry.offers.Contains(kind)) { message = Text.Get("Financing.not_offered", view.Name, Text.Get("Lender.offer_" + kind)); return false; }
        if (view.Unavailable != null) { message = view.Unavailable; return false; }
        Loans.Book.Approval = new Approval { Lender = lenderId, Kind = kind, Limit = view.Headroom, Expires = StarSystem.fEpoch + GameClock.DaySeconds * BankRules.ApprovalDays };
        Loans.SaveBook();
        message = Text.Get(kind == LenderSchema.Home ? "Financing.approved_home" : "Financing.approved_ship", view.Name, BankPanel.Money(view.Headroom), Percent(view.Entry.minDownShare), view.Home);
        return true;
    }

    internal static bool Withdraw(out string message)
    {
        if (!Loans.Attach(out var refusal)) { message = refusal!; return false; }
        if (Loans.Book.Approval == null) { message = Text.Get("Financing.none"); return false; }
        Loans.Book.Approval = null; Loans.SaveBook();
        message = Text.Get("Financing.withdrawn");
        return true;
    }

    /// <summary>The player's pre-approval if it still stands. Read only: the loan poll drops an expired one.</summary>
    internal static Approval? Current() =>
        Loans.Attach(out _) && Loans.Book.Approval is Approval a && Stands(a) ? a : null;

    internal static bool Stands(Approval a) => a.Expires > StarSystem.fEpoch && Lenders.All.ContainsKey(a.Lender);

    /// <summary>The broker's purchase window opened on a mortgage purchase: whether the pre-approved lender finances
    /// it, and with what lowest down payment. Null leaves the window as the game made it; the reason goes to the log.</summary>
    internal static Offer? Open(string regId, double price, bool specialOffer, bool realEstate, out string? note)
    {
        note = null; pending = null;
        var approval = Current();
        if (approval == null) return null;
        string kind = realEstate ? LenderSchema.Home : LenderSchema.Ship;
        var view = Loans.Offers().FirstOrDefault(v => v.Id == approval.Lender);
        if (view == null) return null;
        if (approval.Kind != kind) { note = Text.Get("Financing.wrong_kind", view.Name, Text.Get("Lender.offer_" + approval.Kind)); return null; }
        if (view.Unavailable != null) { note = Text.Get("Financing.not_here", view.Name, view.Unavailable); return null; }
        if (specialOffer) { note = Text.Get("Financing.special_offer", view.Name); return null; }
        double limit = Math.Min(approval.Limit, view.Headroom);
        double? share = LoanRules.MinDownShare(price, view.Entry.minDownShare, limit, view.Entry.minPrincipal);
        if (share == null) { note = Text.Get("Financing.too_small", view.Name, BankPanel.Money(limit), BankPanel.Money(price)); return null; }
        pending = new Offer { Lender = view.Id, RegId = regId, Kind = kind, Name = view.Name, MinShare = share.Value, Limit = limit, Rate = view.Entry.ratePerShift };
        note = Text.Get("Financing.offered", view.Name, Percent(share.Value), BankPanel.Money(Math.Min(limit, price * (1 - share.Value))), Loans.Percent(view.Entry.ratePerShift));
        return pending;
    }

    /// <summary>The broker wrote its mortgage for a purchase: when it is the one the pre-approved lender offered to
    /// finance, the mortgage line (and the first instalment the game raised with it) passes to the lender, and the
    /// loan joins the book. Anything unexpected leaves the broker's mortgage as the game made it, with the reason.</summary>
    internal static void Purchased(string regId, string playerId, string kioskName, string mortgageDescription, out string? message)
    {
        message = null;
        var offer = pending;
        pending = null;
        if (offer == null || offer.RegId != regId || !Loans.Attach(out _) || Current() is not Approval approval || approval.Lender != offer.Lender) return;
        var line = Debts.Mortgages().LastOrDefault(m => m != null && m.strPayor == playerId && m.strPayee == kioskName && m.strDesc == mortgageDescription && !m.Paid);
        // Paid in full: the broker wrote no mortgage, so there is nothing to finance and the pre-approval stands.
        if (line == null) { message = Text.Get("Financing.paid_in_full", offer.Name); return; }
        double principal = line.fAmount;
        var entry = Lenders.All.TryGetValue(offer.Lender, out var e) ? e : null;
        if (entry != null && principal < entry.minPrincipal) { message = Text.Get("Financing.under_minimum", offer.Name, BankPanel.Money(entry.minPrincipal)); return; }
        if (principal > offer.Limit + 0.5) { message = Text.Get("Financing.over_limit", offer.Name, BankPanel.Money(offer.Limit)); return; }
        var view = Loans.Offers().FirstOrDefault(v => v.Id == offer.Lender);
        if (view == null) return;
        string payee = LoanRules.Clean(view.Name);
        // The first instalment the game raised with the mortgage goes to the lender too, so the loan has one creditor.
        foreach (var bill in (Ledger.GetUnpaidLIs(kioskName, playerId, null, false) ?? new List<LedgerLI>())
                 .Where(b => b != null && b.strDesc != null && b.strDesc.StartsWith(mortgageDescription, StringComparison.Ordinal)).ToArray())
        {
            Ledger.RemoveLI(bill);
            bill.strPayee = payee;
            Ledger.AddLI(bill);
        }
        line.strPayee = payee;
        Loans.Open(offer.Lender, view.Name, view.Entry.ratePerShift, principal, offer.Kind, regId, mortgageDescription);
        Loans.Book.Approval = null;
        Loans.SaveBook();
        message = Text.Get(offer.Kind == LenderSchema.Home ? "Financing.done_home" : "Financing.done_ship", offer.Name, BankPanel.Money(principal), regId, Loans.Percent(offer.Rate));
    }

    private static string Percent(double share) => (share * 100).ToString("0.#");
}
