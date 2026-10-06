using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Phobos.Ostranauts.Framework.Controls;
using PhobosBank.Core;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;

namespace PhobosBank;

/// <summary>The CREDIT app's panel (Phobos Banking 0.1.0): the player's loans, bills and regular charges from the game's
/// own ledger, an overview of what is owed, and a button into the game's Finances window where bills are paid.
/// Presentation only: it reads <see cref="Debts"/> and delegates the one action to it.</summary>
public sealed partial class BankPanel : GUIData
{
    public const string Key = "PhobosBankPanel";
    private const string LoanGroup = "loans", BillGroup = "bills", ChargeGroup = "charges";
    // Regular charges start folded; remembered while the game runs, not saved (agent default).
    private static readonly HashSet<string> folded = new(StringComparer.Ordinal) { ChargeGroup };
    private ConsoleShell shell = null!;
    private string selected = "", signature = "";
    private float next;
    private Button? back;

    /// <summary>Opens the panel. Returns null when it opened, or the reason it did not.</summary>
    public static string? Show()
    {
        var player = CrewSim.coPlayer;
        if (player == null || CrewSim.goIntUIPanel == null) return Text.Get("Panel.no_game");
        if (CrewSim.bUILock) return Text.Get("Finances.busy");
        CrewSim.LowerUI(); if (CrewSim.goUI != null) return Text.Get("Panel.other_window");
        var root = W.Rect(CrewSim.goIntUIPanel.transform, Key); W.Fill(root);
        CrewSim.goUI = root.gameObject; var panel = root.gameObject.AddComponent<BankPanel>();
        panel.Init(player, new Dictionary<string, string>(), Key); panel.strFriendlyName = Text.Get("Panel.title"); panel.bActive = true;
        CrewSim.tplLastUI = CrewSim.tplCurrentUI; CrewSim.tplCurrentUI = new global::Ostranauts.Core.Models.Tuple<string, CondOwner>(Key, player);
        CanvasManager.instance.ShipGUI(); CrewSim.SetUIArrows(); panel.Build(); return null;
    }

    private void Build()
    {
        shell = ConsoleShell.Create(transform, Text.Get("Panel.title"), C.Slate);
        C.Button(shell.Navigation, Text.Get("Panel.overview"), () => { lenders = false; selected = ""; shell.Page(true); Render(); });
        // Lenders (Banking 0.2.0): who lends here, on what terms, and borrowing.
        C.Button(shell.Navigation, Text.Get("Panel.lenders"), () => { lenders = true; lender = ""; shell.Page(false); Render(); });
        // Back only pages from a debt to the list on a narrow screen; on a wide one both show at once.
        back = C.Button(shell.Navigation, C.Text("back"), () => { shell.Page(false); Render(); });
        back.gameObject.SetActive(shell.IsNarrow);
        C.Button(shell.Navigation, C.Text("close"), shell.Close);
        Render();
    }

    private void Render()
    {
        if (lenders) { RenderLenders(); return; }
        var summary = Debts.Read();
        signature = summary.Signature;
        float listScroll = shell.ListScroll.verticalNormalizedPosition, detailScroll = shell.DetailScroll.verticalNormalizedPosition;
        W.Clear(shell.List); W.Clear(shell.Detail); W.Clear(shell.Actions);
        if (selected.Length > 0 && summary.Debts.All(d => d.Id != selected)) selected = "";
        var rows = W.Rect(shell.List, "Rows"); var group = rows.gameObject.AddComponent<VerticalLayoutGroup>(); group.spacing = 6; group.childControlWidth = group.childControlHeight = true; group.childForceExpandHeight = false;
        if (summary.Debts.Count == 0) C.Label(rows, Text.Get("Panel.list_empty"));
        else
        {
            var list = new GroupedList(rows, folded);
            var rowData = summary.Debts.Select(d => new GroupedList.Row(Group(d.Kind), d.Id, RowText(d), () => { selected = d.Id; shell.Page(true); Render(); })).ToList();
            var groups = new List<(string Key, string Label, Tone Tone)>
            {
                (LoanGroup, Text.Get("Group.loans"), Tone.Neutral),
                (BillGroup, Text.Get("Group.bills"), summary.Late > 0 ? Tone.Attention : Tone.Neutral),
                (ChargeGroup, Text.Get("Group.charges"), Tone.Neutral)
            };
            list.Render(groups, rowData, selected.Length > 0 ? selected : null, Render);
        }
        var debt = summary.Debts.FirstOrDefault(d => d.Id == selected);
        if (debt == null) Overview(summary); else Detail(debt);
        var finances = C.Button(shell.Actions, Text.Get("Panel.open_finances"), OpenFinances);
        C.Accent(finances, summary.Bills > 0 ? Tone.Good : Tone.Neutral);
        Canvas.ForceUpdateCanvases();
        shell.ListScroll.verticalNormalizedPosition = listScroll; shell.DetailScroll.verticalNormalizedPosition = detailScroll;
    }

    private void Overview(DebtSummary summary)
    {
        C.Heading(shell.Detail, Text.Get("Overview.heading"));
        C.Label(shell.Detail, Text.Get("Overview.cash", Money(Debts.Cash())));
        // A standing pre-approval for a broker purchase (Banking 0.3.0).
        if (Financing.Current() is Approval approval)
            C.Label(shell.Detail, Text.Get(approval.Kind == LenderSchema.Home ? "Financing.overview_home" : "Financing.overview_ship", Lenders.Name(approval.Lender), Money(approval.Limit), MathUtils.GetUTCFromS(approval.Expires)));
        if (summary.Debts.Count == 0) { C.Status(shell.Detail, Text.Get("Overview.clear"), Tone.Good); return; }
        if (summary.Loans > 0)
            C.Label(shell.Detail, Text.Get(BankRules.CountKey("Overview.loans", summary.Loans), summary.Loans, Money(summary.LoanBalance), Money(summary.NextInstalments)));
        if (summary.Bills > 0)
        {
            string bills = Text.Get(BankRules.CountKey("Overview.bills", summary.Bills), summary.Bills, Money(summary.BillsDue));
            if (summary.Late > 0) bills += " " + Text.Get(BankRules.LateKey(summary.Bills, summary.Late), summary.Late) + " " + Text.Get("Overview.late_fee", Percent(BankRules.LateFeeShare));
            C.Status(shell.Detail, bills, summary.Late > 0 ? Tone.Attention : Tone.Neutral);
        }
        if (summary.Charges > 0) C.Label(shell.Detail, Text.Get(BankRules.CountKey("Overview.charges", summary.Charges), summary.Charges));
        C.Label(shell.Detail, Text.Get("Overview.how_to_pay", Percent(BankRules.LateFeeShare)));
    }

    private void Detail(Debt debt)
    {
        C.Heading(shell.Detail, debt.Creditor);
        if (debt.Description.Length > 0) C.Label(shell.Detail, debt.Description);
        string since = MathUtils.GetUTCFromS(debt.Since);
        switch (debt.Kind)
        {
            case DebtKind.Loan:
                C.Label(shell.Detail, Text.Get("Loan.balance", Money(debt.Amount)));
                C.Label(shell.Detail, debt.ShiftsLeft > 0 ? Text.Get(BankRules.CountKey("Loan.instalment", debt.ShiftsLeft), Money(debt.Instalment), debt.ShiftsLeft) : Text.Get("Loan.term_over", Money(debt.Instalment)));
                C.Label(shell.Detail, Text.Get("Loan.since", since));
                // A loan from a Phobos lender also carries the lender's interest (Banking 0.2.0).
                if (Loans.Attach(out _) && Loans.Book.Open.FirstOrDefault(l => l.Payee == debt.Creditor && l.Description == debt.Description) is Loan ours)
                    C.Label(shell.Detail, Text.Get("Loan.interest", Loans.Percent(ours.RatePerShift), Money(ours.InterestBilled)));
                C.Label(shell.Detail, Text.Get("Loan.explain", Percent(BankRules.LateFeeShare)));
                break;
            case DebtKind.Bill:
                C.Label(shell.Detail, Text.Get("Bill.amount", Money(debt.Amount)));
                C.Label(shell.Detail, Text.Get("Bill.since", since));
                C.Status(shell.Detail, debt.Late ? Text.Get("Bill.late", Percent(BankRules.LateFeeShare), Money(BankRules.LateFee(debt.Amount)))
                    : Text.Get("Bill.due", Percent(BankRules.LateFeeShare), Money(BankRules.LateFee(debt.Amount))), debt.Late ? Tone.Attention : Tone.Neutral);
                break;
            default:
                C.Label(shell.Detail, Text.Get("Charge.amount", Money(debt.Amount), Text.Get("Every." + debt.Every)));
                C.Label(shell.Detail, Text.Get("Charge.since", since));
                C.Label(shell.Detail, Text.Get("Charge.explain"));
                break;
        }
    }

    private void OpenFinances()
    {
        // The Finances window takes this panel's place; a refusal stays on the panel.
        if (!Debts.OpenFinances(out string message) && shell != null) shell.Notice.text = message;
    }

    private static string Group(DebtKind kind) => kind == DebtKind.Loan ? LoanGroup : kind == DebtKind.Bill ? BillGroup : ChargeGroup;

    private static string RowText(Debt d) => d.Kind switch
    {
        DebtKind.Loan => Text.Get("Row.loan", d.Creditor, Money(d.Amount)),
        DebtKind.Bill => Text.Get(d.Late ? "Row.bill_late" : "Row.bill", d.Creditor, Money(d.Amount)),
        _ => Text.Get("Row.charge", d.Creditor, Money(d.Amount), Text.Get("Every." + d.Every))
    };

    internal static string Money(double amount) => Text.Get("Money", amount.ToString("n"));
    private static string Percent(double share) => (share * 100).ToString("0.#");

    private void Update()
    {
        if (!bActive || CrewSim.goUI != gameObject || shell == null) return;
        if (back != null && back.gameObject.activeSelf != shell.IsNarrow) back.gameObject.SetActive(shell.IsNarrow);
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + BankRules.PanelRefreshSeconds;
        // A paid bill, a new instalment or a late fee redraws the panel, keeping its scroll; on the lenders page, a move
        // into or out of a lender's reach does.
        if ((lenders ? LendersSignature() : Debts.Read().Signature) != signature) Render();
    }
}

[HarmonyPatch(typeof(CrewSim), nameof(CrewSim.RaiseUI))]
internal static class BankPanelRestore
{
    private static bool Prefix(string strCOGUIKey) { if (strCOGUIKey != BankPanel.Key) return true; BankPanel.Show(); return false; }
}
