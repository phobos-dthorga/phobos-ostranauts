using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Phobos.Ostranauts.Framework.Controls;
using PhobosBank.Core;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;

namespace PhobosBank;

/// <summary>The Credit panel's lenders page (Phobos Banking 0.2.0): who lends, where, on what terms, and borrowing.
/// Presentation only: the figures come from <see cref="Loans.Offers"/> and the one action goes to <see cref="Loans.Borrow"/>.</summary>
public sealed partial class BankPanel
{
    private const string HereGroup = "here", ElsewhereGroup = "elsewhere";
    // Lenders out of reach start folded; remembered while the game runs, not saved (agent default).
    private static readonly HashSet<string> foldedLenders = new(StringComparer.Ordinal) { ElsewhereGroup };
    private bool lenders;
    private string lender = "";
    // The amount being chosen, per lender, while the panel stays open.
    private readonly Dictionary<string, double> amounts = new(StringComparer.Ordinal);

    private static string LendersSignature() =>
        string.Join("|", Loans.Offers().Select(v => v.Id + ":" + (v.Unavailable ?? "") + ":" + v.Headroom.ToString("F0")));

    private void RenderLenders()
    {
        var views = Loans.Offers();
        signature = string.Join("|", views.Select(v => v.Id + ":" + (v.Unavailable ?? "") + ":" + v.Headroom.ToString("F0")));
        float listScroll = shell.ListScroll.verticalNormalizedPosition, detailScroll = shell.DetailScroll.verticalNormalizedPosition;
        W.Clear(shell.List); W.Clear(shell.Detail); W.Clear(shell.Actions);
        var rows = W.Rect(shell.List, "Rows"); var group = rows.gameObject.AddComponent<VerticalLayoutGroup>(); group.spacing = 6; group.childControlWidth = group.childControlHeight = true; group.childForceExpandHeight = false;
        if (views.Count == 0) { C.Label(rows, Text.Get("Lenders.none")); C.Label(shell.Detail, Text.Get("Lenders.none_detail")); return; }
        if (lender.Length == 0 || views.All(v => v.Id != lender)) lender = views[0].Id;
        var list = new GroupedList(rows, foldedLenders);
        var rowData = views.Select(v => new GroupedList.Row(v.Unavailable == null ? HereGroup : ElsewhereGroup, v.Id,
            v.Unavailable == null ? Text.Get("Lenders.row_here", v.Name, Money(v.Headroom)) : Text.Get("Lenders.row_away", v.Name, v.Home),
            () => { lender = v.Id; shell.Page(true); Render(); })).ToList();
        list.Render(new List<(string Key, string Label, Tone Tone)>
        {
            (HereGroup, Text.Get("Lenders.group_here"), views.Any(v => v.Unavailable == null) ? Tone.Good : Tone.Neutral),
            (ElsewhereGroup, Text.Get("Lenders.group_away"), Tone.Neutral)
        }, rowData, lender, Render);
        LenderDetail(views.First(v => v.Id == lender));
        Canvas.ForceUpdateCanvases();
        shell.ListScroll.verticalNormalizedPosition = listScroll; shell.DetailScroll.verticalNormalizedPosition = detailScroll;
    }

    private void LenderDetail(LenderView v)
    {
        var l = v.Entry;
        C.Heading(shell.Detail, v.Name);
        C.Label(shell.Detail, Text.Get(l.accredited ? "Lender.accredited" : "Lender.unaccredited", v.Home));
        C.Label(shell.Detail, Text.Get("Lender.pitch", v.Pitch));
        C.Heading(shell.Detail, Text.Get("Lender.terms"));
        C.Label(shell.Detail, Text.Get("Lender.rate", Loans.Percent(l.ratePerShift), Percent(v.InterestShare)));
        C.Label(shell.Detail, Text.Get("Lender.range", Money(l.minPrincipal), Money(l.maxPrincipal), Money(v.Owed)));
        C.Label(shell.Detail, Text.Get("Lender.offers", string.Join(", ", l.offers.Select(o => Text.Get("Lender.offer_" + o)))) +
            (l.offers.Any(o => o != LenderSchema.Cash) ? " " + Text.Get("Lender.down", Percent(l.minDownShare)) : ""));
        C.Label(shell.Detail, Text.Get("Lender.repay", Percent(BankRules.LateFeeShare)));
        // A pre-approval from this lender (Banking 0.3.0) shows whatever else is in the way, so it can be withdrawn.
        if (Financing.Current() is Approval approval && approval.Lender == v.Id)
        {
            C.Status(shell.Detail, Text.Get(approval.Kind == LenderSchema.Home ? "Financing.standing_home" : "Financing.standing_ship", Money(approval.Limit), MathUtils.GetUTCFromS(approval.Expires)), Tone.Good);
            C.Button(shell.Actions, Text.Get("Financing.withdraw_button"), () => { Financing.Withdraw(out string message); shell.Notice.text = message; Render(); });
        }
        if (v.Unavailable != null) { C.Status(shell.Detail, v.Unavailable, Tone.Attention); return; }
        foreach (string kind in l.offers.Where(o => o != LenderSchema.Cash))
        {
            string k = kind;
            C.Button(shell.Actions, Text.Get(k == LenderSchema.Home ? "Financing.approve_home_button" : "Financing.approve_ship_button", Money(v.Headroom)),
                () => { Financing.PreApprove(v.Id, k, out string message); shell.Notice.text = message; Render(); });
        }
        if (l.offers.Any(o => o != LenderSchema.Cash)) C.Label(shell.Detail, Text.Get("Financing.how", Percent(l.minDownShare)));
        if (!l.offers.Contains(LenderSchema.Cash)) { C.Status(shell.Detail, Text.Get("Lender.no_cash", v.Name), Tone.Neutral); return; }

        double step = LoanRules.Step(l.minPrincipal, v.Headroom);
        double amount = LoanRules.Clamp(amounts.TryGetValue(v.Id, out var chosen) ? chosen : l.minPrincipal, l.minPrincipal, v.Headroom);
        int least = (int)Math.Ceiling(l.minPrincipal / step), most = (int)Math.Floor(v.Headroom / step);
        int units = Math.Min(most, Math.Max(least, (int)Math.Round(amount / step)));
        amount = units * step;
        C.Heading(shell.Detail, Text.Get("Lender.borrow_heading"));
        C.Stepper(shell.Detail, Text.Get("Lender.amount_step", Money(step)), units, least, most, n => { amounts[v.Id] = n * step; Render(); });
        double instalment = BankRules.Instalment(amount, BankRules.ShiftsLeft(0));
        C.Label(shell.Detail, Text.Get("Lender.quote", Money(amount), Money(instalment), Money(LoanRules.TotalInterest(amount, l.ratePerShift))));
        var borrow = C.Button(shell.Actions, Text.Get("Lender.borrow_button", Money(amount), v.Name), () => Confirm(v, amount));
        C.Accent(borrow, Tone.Good);
    }

    private void Confirm(LenderView v, double amount) =>
        ChoiceCard.Show(shell.transform, Text.Get("Lender.confirm", Money(amount), v.Name, Loans.Percent(v.Entry.ratePerShift)), new[]
        {
            new ChoiceCard.Choice(Text.Get("Lender.confirm_yes"), () => { Loans.Borrow(v.Id, amount, out string message); shell.Notice.text = message; Render(); }, Tone.Good),
            new ChoiceCard.Choice(Text.Get("Lender.confirm_no"), () => { })
        });
}
