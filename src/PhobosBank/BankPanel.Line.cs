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

/// <summary>The Credit panel's credit line page (Phobos Banking 0.6.0): the system-wide line's terms, opening it, the
/// account, and drawing. Presentation only: figures come from <see cref="CreditLines.Views"/> and the two actions go to
/// <see cref="CreditLines.Open"/> and <see cref="CreditLines.Draw"/>.</summary>
public sealed partial class BankPanel
{
    private bool credit;
    private string creditLine = "";
    // The amount being chosen per line, while the panel stays open.
    private readonly Dictionary<string, double> draws = new(StringComparer.Ordinal);

    private static string LineSignature() =>
        string.Join("|", CreditLines.Views().Select(v => v.Id + ":" + (v.Account != null) + ":" + v.Owed.ToString("F2") + ":" + (v.CannotOpen ?? "")));

    private void RenderLine()
    {
        var views = CreditLines.Views();
        signature = string.Join("|", views.Select(v => v.Id + ":" + (v.Account != null) + ":" + v.Owed.ToString("F2") + ":" + (v.CannotOpen ?? "")));
        float listScroll = shell.ListScroll.verticalNormalizedPosition, detailScroll = shell.DetailScroll.verticalNormalizedPosition;
        W.Clear(shell.List); W.Clear(shell.Detail); W.Clear(shell.Actions);
        var rows = W.Rect(shell.List, "Rows"); var group = rows.gameObject.AddComponent<VerticalLayoutGroup>(); group.spacing = 6; group.childControlWidth = group.childControlHeight = true; group.childForceExpandHeight = false;
        if (views.Count == 0) { C.Label(rows, Text.Get("Line.none")); C.Label(shell.Detail, Text.Get("Line.none_detail")); return; }
        if (creditLine.Length == 0 || views.All(v => v.Id != creditLine)) creditLine = views[0].Id;
        foreach (var v in views)
        {
            var view = v;
            var button = C.Button(rows, v.Account == null ? Text.Get("Line.row_closed", v.Name) : Text.Get("Line.row_open", v.Name, Money(v.Available)),
                () => { creditLine = view.Id; shell.Page(true); Render(); }, C.RowHeight);
            C.Accent(button, v.Id == creditLine ? Tone.Good : Tone.Neutral);
        }
        LineDetail(views.First(v => v.Id == creditLine));
        Canvas.ForceUpdateCanvases();
        shell.ListScroll.verticalNormalizedPosition = listScroll; shell.DetailScroll.verticalNormalizedPosition = detailScroll;
    }

    private void LineDetail(LineView v)
    {
        C.Heading(shell.Detail, v.Name);
        C.Label(shell.Detail, Text.Get("Line.what"));
        C.Label(shell.Detail, Text.Get("Lender.pitch", v.Pitch));
        C.Heading(shell.Detail, Text.Get("Lender.terms"));
        C.Label(shell.Detail, Text.Get("Line.terms", Money(v.Limit), Loans.Percent(v.RatePerShift), CreditLines.Percent(v.DrawFee), Money(v.MinDraw)));
        C.Label(shell.Detail, Text.Get("Line.repay", Percent(BankRules.LateFeeShare)));
        if (v.Account == null)
        {
            if (v.CannotOpen != null) { C.Status(shell.Detail, v.CannotOpen, Tone.Attention); return; }
            var open = C.Button(shell.Actions, Text.Get("Line.open_button", v.Name), () => ConfirmOpen(v));
            C.Accent(open, Tone.Good);
            return;
        }
        C.Heading(shell.Detail, Text.Get("Line.account"));
        C.Label(shell.Detail, Text.Get("Line.balance", Money(v.Owed), Money(v.Available), Money(v.Limit)));
        if (v.Owed > BankRules.PaidOffBelow) C.Label(shell.Detail, Text.Get("Line.minimum", Money(v.Minimum)));
        if (v.Cycle != null) C.Label(shell.Detail, Text.Get("Loan.interest", Loans.Percent(v.RatePerShift), Money(v.Cycle.InterestBilled)));
        var finances = C.Button(shell.Actions, Text.Get("Panel.open_finances"), OpenFinances);
        C.Accent(finances, v.Owed > BankRules.PaidOffBelow ? Tone.Good : Tone.Neutral);

        double most = LoanRules.MaxDraw(v.Available, v.DrawFee);
        if (most < v.MinDraw) { C.Status(shell.Detail, Text.Get("Line.full", Money(v.MinDraw)), Tone.Attention); return; }
        double step = most <= 5000 ? 100 : most <= 50000 ? 500 : 1000;
        int least = (int)Math.Ceiling(v.MinDraw / step), max = (int)Math.Floor(most / step);
        double chosen = draws.TryGetValue(v.Id, out var d) ? d : v.MinDraw;
        int units = Math.Min(max, Math.Max(least, (int)Math.Round(chosen / step)));
        double amount = units * step, fee = LoanRules.DrawFee(amount, v.DrawFee);
        C.Heading(shell.Detail, Text.Get("Line.draw_heading"));
        C.Stepper(shell.Detail, Text.Get("Lender.amount_step", Money(step)), units, least, max, n => { draws[v.Id] = n * step; Render(); });
        // After a draw the balance runs over a fresh term, so the minimum is the game's instalment on the new balance.
        double newMinimum = BankRules.Instalment(v.Owed + amount + fee, BankRules.ShiftsLeft(0));
        C.Label(shell.Detail, Text.Get("Line.quote", Money(amount), Money(fee), Money(v.Owed + amount + fee), Money(newMinimum)));
        var draw = C.Button(shell.Actions, Text.Get("Line.draw_button", Money(amount)), () => ConfirmDraw(v, amount, fee));
        C.Accent(draw, Tone.Good);
    }

    private void ConfirmOpen(LineView v) =>
        ChoiceCard.Show(shell.transform, Text.Get("Line.confirm_open", v.Name, Money(v.Limit), Loans.Percent(v.RatePerShift), CreditLines.Percent(v.DrawFee)), new[]
        {
            new ChoiceCard.Choice(Text.Get("Line.confirm_open_yes"), () => { CreditLines.Open(v.Id, out string message); shell.Notice.text = message; Render(); }, Tone.Good),
            new ChoiceCard.Choice(Text.Get("Lender.confirm_no"), () => { })
        });

    private void ConfirmDraw(LineView v, double amount, double fee) =>
        ChoiceCard.Show(shell.transform, Text.Get("Line.confirm_draw", Money(amount), v.Name, Money(fee), Loans.Percent(v.RatePerShift)), new[]
        {
            new ChoiceCard.Choice(Text.Get("Line.confirm_draw_yes"), () => { CreditLines.Draw(v.Id, amount, out string message); shell.Notice.text = message; Render(); }, Tone.Good),
            new ChoiceCard.Choice(Text.Get("Lender.confirm_no"), () => { })
        });
}
