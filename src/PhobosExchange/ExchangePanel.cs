using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Phobos.Ostranauts.Framework.Controls;
using PhobosExchange.Core;
using W = Phobos.Ostranauts.Framework.Controls.PanelWidgets;
using C = Phobos.Ostranauts.Framework.Controls.ConsoleWidgets;

namespace PhobosExchange;

/// <summary>The EXCHANGE app's panel (Phobos Exchange 0.1.0): the listed companies by sector, each with its price, chart,
/// profile and the player's holding; buying and selling with the price worked out before confirming; price alerts; and
/// the player's holdings. Presentation only: figures come from <see cref="Market"/> and every action is one call to it.
/// Prices tick once a game minute, so the panel updates its labels and chart in place and rebuilds only when the page,
/// the selection, the holdings or the alerts change.</summary>
public sealed class ExchangePanel : GUIData
{
    public const string Key = "PhobosExchangePanel";
    private enum Range { Hours, Days, Weeks }
    // Folded sectors and the chosen chart range are remembered while the game runs, not saved (agent default).
    private static readonly HashSet<string> folded = new(StringComparer.Ordinal);
    private static Range range = Range.Days;
    private ConsoleShell shell = null!;
    private Button? back;
    private bool holdingsPage;
    private string selected = "", structure = "";
    private int shownVersion = -1;
    private float next;
    private int lots = 1;
    private readonly PressGuard guard = new();
    // Live parts of the current page, refreshed in place.
    private TMP_Text? priceLabel, changeLabel, quoteLabel, heldLabel, cashLabel, valueLabel;
    private Chart? chart;
    private readonly ChartData chartData = new();
    private readonly ChartSeries series = new() { X = new double[ExchangeRules.WeeklyCloses + 2], Y = new double[ExchangeRules.WeeklyCloses + 2], Fill = true };
    private GroupedList? list;

    /// <summary>Opens the panel. Returns null when it opened, or the reason it did not.</summary>
    public static string? Show()
    {
        var player = CrewSim.coPlayer;
        if (player == null || CrewSim.goIntUIPanel == null) return Text.Get("Panel.no_game");
        if (CrewSim.bUILock) return Text.Get("Panel.busy");
        if (!Market.Attach(out var refusal)) return refusal;
        CrewSim.LowerUI(); if (CrewSim.goUI != null) return Text.Get("Panel.other_window");
        var root = W.Rect(CrewSim.goIntUIPanel.transform, Key); W.Fill(root);
        CrewSim.goUI = root.gameObject; var panel = root.gameObject.AddComponent<ExchangePanel>();
        panel.Init(player, new Dictionary<string, string>(), Key); panel.strFriendlyName = Text.Get("Panel.title"); panel.bActive = true;
        CrewSim.tplLastUI = CrewSim.tplCurrentUI; CrewSim.tplCurrentUI = new global::Ostranauts.Core.Models.Tuple<string, CondOwner>(Key, player);
        CanvasManager.instance.ShipGUI(); CrewSim.SetUIArrows(); panel.Build(); return null;
    }

    private void Build()
    {
        chartData.Series.Add(series);
        shell = ConsoleShell.Create(transform, Text.Get("Panel.title"), C.Slate);
        C.Button(shell.Navigation, Text.Get("Panel.market"), () => { holdingsPage = false; selected = ""; guard.Disarm(); shell.Page(true); Render(); });
        C.Button(shell.Navigation, Text.Get("Panel.holdings"), () => { holdingsPage = true; selected = ""; guard.Disarm(); shell.Page(true); Render(); });
        back = C.Button(shell.Navigation, C.Text("back"), () => { shell.Page(false); Render(); });
        back.gameObject.SetActive(shell.IsNarrow);
        C.Button(shell.Navigation, C.Text("close"), shell.Close);
        Render();
    }

    /// <summary>What forces a rebuild: the page, the selection and what the player holds and watches.</summary>
    private string Structure(List<CompanyView> views) =>
        (holdingsPage ? "h|" : "m|") + selected + "|" + range + "|" + string.Join(",", views.Select(v => v.Id + ":" + (v.Held?.Shares ?? 0) + ":" + v.Alert?.Above + ":" + v.Alert?.Below + ":" + (v.Unavailable != null)));

    private void Render()
    {
        var views = Market.Views();
        structure = Structure(views);
        shownVersion = Market.Version;
        float listScroll = shell.ListScroll.verticalNormalizedPosition, detailScroll = shell.DetailScroll.verticalNormalizedPosition;
        W.Clear(shell.List); W.Clear(shell.Detail); W.Clear(shell.Actions);
        priceLabel = changeLabel = quoteLabel = heldLabel = cashLabel = valueLabel = null; chart = null;
        if (!Market.Attach(out var refusal)) { C.Status(shell.Detail, refusal!, Tone.Attention); return; }
        if (selected.Length > 0 && views.All(v => v.Id != selected)) selected = "";
        var rows = W.Rect(shell.List, "Rows"); var group = rows.gameObject.AddComponent<VerticalLayoutGroup>(); group.spacing = 6; group.childControlWidth = group.childControlHeight = true; group.childForceExpandHeight = false;
        list = new GroupedList(rows, folded);
        var rowData = views.Select(v => new GroupedList.Row(v.Unavailable != null ? "-unlisted" : v.Sector, v.Id, RowText(v), () => { selected = v.Id; guard.Disarm(); shell.Page(true); Render(); })).ToList();
        var groups = views.Where(v => v.Unavailable == null).Select(v => v.Sector).Distinct().Select(s => (s, Companies.SectorName(s), Tone.Neutral)).ToList();
        if (views.Any(v => v.Unavailable != null)) groups.Add(("-unlisted", Text.Get("Group.unlisted"), Tone.Attention));
        if (rowData.Count == 0) C.Label(rows, Text.Get("Panel.list_empty"));
        else list.Render(groups, rowData, selected.Length > 0 ? selected : null, Render);
        var view = views.FirstOrDefault(v => v.Id == selected);
        if (view != null) Company(view);
        else if (holdingsPage) Holdings(views);
        else Overview(views);
        Canvas.ForceUpdateCanvases();
        shell.ListScroll.verticalNormalizedPosition = listScroll; shell.DetailScroll.verticalNormalizedPosition = detailScroll;
    }

    private static string RowText(CompanyView v) => v.Unavailable != null
        ? Text.Get("Row.unlisted", v.Ticker, v.Held?.Shares ?? 0)
        : Text.Get(v.Held != null && v.Held.Shares > 0 ? "Row.held" : "Row.company", v.Ticker, Market.Money(v.Quote.Mid), Change(v.DayChange));

    private static string Change(double share) => double.IsNaN(share) ? Text.Get("Change.none") : Market.Percent(share);
    private static Tone ToneOf(double share) => double.IsNaN(share) || Math.Abs(share) < 0.0005 ? Tone.Neutral : share > 0 ? Tone.Good : Tone.Attention;

    private void Overview(List<CompanyView> views)
    {
        C.Heading(shell.Detail, Companies.ExchangeName);
        C.Label(shell.Detail, Text.Get("Overview.what"));
        cashLabel = C.Label(shell.Detail, Text.Get("Overview.cash", Market.Money(Cash())));
        valueLabel = C.Label(shell.Detail, Text.Get("Overview.holdings", Market.Money(Market.HoldingsValue())));
        C.Heading(shell.Detail, Text.Get("Overview.wire"));
        if (Market.RecentWire.Count == 0) C.Label(shell.Detail, Text.Get("Overview.wire_quiet"));
        foreach (var line in Market.RecentWire) C.Label(shell.Detail, line);
        C.Heading(shell.Detail, Text.Get("Overview.how_heading"));
        C.Label(shell.Detail, Text.Get("Overview.how"));
    }

    private void Holdings(List<CompanyView> views)
    {
        C.Heading(shell.Detail, Text.Get("Holdings.heading"));
        cashLabel = C.Label(shell.Detail, Text.Get("Overview.cash", Market.Money(Cash())));
        valueLabel = C.Label(shell.Detail, Text.Get("Overview.holdings", Market.Money(Market.HoldingsValue())));
        var held = views.Where(v => v.Held != null && v.Held.Shares > 0).ToList();
        if (held.Count == 0) { C.Label(shell.Detail, Text.Get("Holdings.none")); return; }
        foreach (var v in held)
            C.Label(shell.Detail, v.Unavailable != null ? ConsolePatch.Line(v) : Text.Get("Holdings.line", v.Name, v.Ticker, v.Held!.Shares, Market.Money(v.Value), Market.Money(v.Held.Cost), Market.Money(v.Value - v.Held.Cost)));
        var sellAll = C.Button(shell.Actions, guard.Label("sellall", Text.Get("Holdings.sell_all")), () =>
        {
            string message = "";
            guard.Press("sellall", () => Market.SellAll(false, out message));
            shell.Notice.text = message; Render();
        });
        C.Accent(sellAll, guard.Armed("sellall") ? Tone.Attention : Tone.Neutral);
    }

    private void Company(CompanyView v)
    {
        C.Heading(shell.Detail, Text.Get("Company.heading", v.Name, v.Ticker));
        if (v.Unavailable != null) { C.Status(shell.Detail, v.Unavailable, Tone.Attention); return; }
        C.Label(shell.Detail, Text.Get("Company.sector", v.SectorName));
        priceLabel = C.Label(shell.Detail, PriceText(v));
        changeLabel = C.Status(shell.Detail, ChangeText(v), ToneOf(v.DayChange));
        var ranges = C.Row(shell.Detail);
        foreach (var r in new[] { Range.Hours, Range.Days, Range.Weeks })
        {
            var choice = r;
            var b = C.Button(ranges, Text.Get("Chart." + r.ToString().ToLowerInvariant() + "_range"), () => { range = choice; Render(); });
            C.Size(b.transform, C.ControlHeight, 130);
            C.Accent(b, range == r ? Tone.Good : Tone.Neutral);
        }
        chart = Chart.Create(shell.Detail, 220);
        FillChart(v);
        C.Label(shell.Detail, v.Profile);

        // The order: a number of lots, the price worked out before confirming.
        var model = Market.Model!;
        var market = Companies.Pack!.market;
        long canBuy = TradeRules.MaxBuy(model.Entries[v.Index], market, model.LnPrice[v.Index], v.Held, Cash());
        long held = v.Held?.Shares ?? 0;
        long most = Math.Max(canBuy, held);
        heldLabel = C.Label(shell.Detail, HeldText(v));
        C.Heading(shell.Detail, Text.Get("Order.heading"));
        if (most <= 0) C.Label(shell.Detail, Text.Get("Order.cannot", Market.Money(v.Quote.Ask)));
        else
        {
            long lot = LotSize(most);
            int maxLots = (int)Math.Max(1, Math.Min(500, most / lot));
            lots = Math.Max(1, Math.Min(lots, maxLots));
            C.Stepper(shell.Detail, Text.Get("Order.lots", lot), lots, 1, maxLots, n => { lots = n; RefreshQuote(); });
            quoteLabel = C.Label(shell.Detail, "");
            RefreshQuote();
            var buy = C.Button(shell.Actions, Text.Get("Order.buy"), () => Confirm(true));
            C.Accent(buy, Tone.Good);
            buy.interactable = canBuy > 0;
            var sell = C.Button(shell.Actions, Text.Get("Order.sell"), () => Confirm(false));
            sell.interactable = held > 0;
        }
        // Alerts at ten percent either side, or cleared; exact levels through F3.
        C.Heading(shell.Detail, Text.Get("Alert.heading"));
        C.Label(shell.Detail, AlertText(v));
        var alerts = C.Row(shell.Detail);
        var up = C.Button(alerts, Text.Get("Alert.up_button"), () => Act(Market.SetAlert(v.Ticker, true, Math.Round(Market.Model!.Price(v.Index) * 1.1, 2), out string m), m));
        C.Size(up.transform, C.ControlHeight, 170);
        var down = C.Button(alerts, Text.Get("Alert.down_button"), () => Act(Market.SetAlert(v.Ticker, false, Math.Round(Market.Model!.Price(v.Index) * 0.9, 2), out string m), m));
        C.Size(down.transform, C.ControlHeight, 170);
        var clear = C.Button(alerts, Text.Get("Alert.clear_button"), () => { Market.SetAlert(v.Ticker, true, null, out string a); Market.SetAlert(v.Ticker, false, null, out string b); Act(true, a + " " + b); });
        C.Size(clear.transform, C.ControlHeight, 120);
        clear.interactable = v.Alert != null && !v.Alert.Empty;
    }

    private void Act(bool done, string message) { shell.Notice.text = message; Render(); }

    private static long LotSize(long most)
    {
        long lot = 1;
        foreach (long step in new long[] { 1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000 })
            if (most / step <= 50) { lot = step; break; } else lot = step;
        return lot;
    }

    private long Shares(CompanyView v) => lots * LotSize(Math.Max(TradeRules.MaxBuy(Market.Model!.Entries[v.Index], Companies.Pack!.market, Market.Model.LnPrice[v.Index], v.Held, Cash()), v.Held?.Shares ?? 0));

    private void RefreshQuote()
    {
        var v = Market.Views().FirstOrDefault(x => x.Id == selected);
        if (quoteLabel == null || v == null || v.Index < 0 || Market.Model == null) return;
        var model = Market.Model; var market = Companies.Pack!.market; var c = model.Entries[v.Index];
        long n = Shares(v);
        var buy = TradeRules.Buy(c, market, model.LnPrice[v.Index], n, v.Held, Cash());
        var sell = TradeRules.Sell(c, market, model.LnPrice[v.Index], Math.Min(n, v.Held?.Shares ?? 0), v.Held);
        string buyText = buy.Ok ? Text.Get("Order.buy_quote", n, Market.Money(buy.Total), Market.Money(buy.Commission)) : Text.Get("Order.buy_no", n);
        string sellText = sell.Ok ? Text.Get("Order.sell_quote", sell.Shares, Market.Money(sell.Total), Market.Money(sell.Commission)) : Text.Get("Order.sell_no");
        quoteLabel.text = buyText + "\n" + sellText;
    }

    private void Confirm(bool buy)
    {
        var v = Market.Views().FirstOrDefault(x => x.Id == selected);
        if (v == null || Market.Model == null) return;
        var model = Market.Model; var c = model.Entries[v.Index];
        long n = buy ? Shares(v) : Math.Min(Shares(v), v.Held?.Shares ?? 0);
        var fill = buy ? TradeRules.Buy(c, Companies.Pack!.market, model.LnPrice[v.Index], n, v.Held, Cash()) : TradeRules.Sell(c, Companies.Pack!.market, model.LnPrice[v.Index], n, v.Held);
        // A refusal is worded by the service, the same as for F3; nothing is traded.
        if (!fill.Ok) { shell.Notice.text = Market.Refusal(fill, v.Name, v.Ticker); return; }
        string question = Text.Get(buy ? "Order.confirm_buy" : "Order.confirm_sell", n, v.Name, v.Ticker, Market.Money(fill.PerShare), Market.Money(fill.Total), Market.Money(fill.Commission));
        ChoiceCard.Show(shell.transform, question, new[]
        {
            new ChoiceCard.Choice(Text.Get(buy ? "Order.confirm_buy_yes" : "Order.confirm_sell_yes"), () =>
            {
                string message;
                if (buy) Market.Buy(v.Ticker, n, out message); else Market.Sell(v.Ticker, n, out message);
                shell.Notice.text = message; Render();
            }, Tone.Good),
            new ChoiceCard.Choice(Text.Get("Order.confirm_no"), () => { })
        });
    }

    private static string PriceText(CompanyView v) => Text.Get("Company.price", Market.Money(v.Quote.Mid), Market.Money(v.Quote.Bid), Market.Money(v.Quote.Ask));
    private static string ChangeText(CompanyView v) => Text.Get("Company.change", Change(v.DayChange), Change(v.WeekChange));
    private static string HeldText(CompanyView v) => v.Held == null || v.Held.Shares <= 0 ? Text.Get("Company.none_held")
        : Text.Get("Company.held", v.Held.Shares, Market.Money(v.Value), Market.Money(v.Held.Cost), Market.Money(v.Value - v.Held.Cost));
    private static string AlertText(CompanyView v) => v.Alert == null || v.Alert.Empty ? Text.Get("Alert.none")
        : Text.Get("Alert.current", v.Alert.Above is double a ? Market.Money(a) : Text.Get("Change.none"), v.Alert.Below is double b ? Market.Money(b) : Text.Get("Change.none"));

    private static double Cash() => CrewSim.coPlayer == null ? 0 : CrewSim.coPlayer.GetCondAmount(Ledger.CURRENCY);

    /// <summary>The chosen range's closes, oldest first, with the live price as the last point at "now" (0).</summary>
    private void FillChart(CompanyView v)
    {
        if (chart == null || Market.Model == null) return;
        var model = Market.Model;
        var h = model.History(v.Index);
        CloseSeries closes = range == Range.Hours ? h.Hourly : range == Range.Days ? h.Daily : h.Weekly;
        long current = range == Range.Hours ? ExchangeRules.HourOf(model.Clock) : range == Range.Days ? ExchangeRules.DayOf(model.Clock) : ExchangeRules.WeekOf(model.Clock);
        int n = 0;
        for (int i = 0; i < closes.Count && n < series.X.Length - 1; i++)
        {
            // A close belongs to the end of its bucket, so the newest sits at -1 ... and now is 0.
            series.X[n] = closes.FirstBucket + i + 1 - current;
            series.Y[n] = ExchangeRules.Price(closes[i]);
            n++;
        }
        series.X[n] = 0; series.Y[n] = model.Price(v.Index); n++;
        series.Count = n;
        series.Tone = n > 1 && series.Y[n - 1] < series.Y[0] ? Tone.Attention : Tone.Good;
        chartData.Levels.Clear();
        if (v.Alert?.Above is double above) chartData.Levels.Add(new ChartLevel { Y = above, Tone = Tone.Attention, Label = Text.Get("Chart.alert_above", Market.Money(above)) });
        if (v.Alert?.Below is double below) chartData.Levels.Add(new ChartLevel { Y = below, Tone = Tone.Attention, Label = Text.Get("Chart.alert_below", Market.Money(below)) });
        if (v.Held != null && v.Held.Shares > 0)
        {
            double paid = v.Held.Cost / v.Held.Shares;
            chartData.Levels.Add(new ChartLevel { Y = paid, Tone = Tone.Neutral, Label = Text.Get("Chart.paid", Market.Money(paid)) });
        }
        string unit = range == Range.Hours ? "Chart.hours" : range == Range.Days ? "Chart.days" : "Chart.weeks";
        chartData.FormatX = x => Math.Abs(x) < 0.5 ? Text.Get("Chart.now") : Text.Get(unit, Math.Round(-x).ToString("0"));
        chartData.FormatY = y => Market.Money(y);
        chartData.Version++;
        chart.Show(chartData);
    }

    private void Update()
    {
        if (!bActive || CrewSim.goUI != gameObject || shell == null) return;
        if (back != null && back.gameObject.activeSelf != shell.IsNarrow) back.gameObject.SetActive(shell.IsNarrow);
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + ExchangeRules.PanelRefreshSeconds;
        if (Market.Version == shownVersion) return;
        shownVersion = Market.Version;
        var views = Market.Views();
        // A trade, an alert or a company coming or going rebuilds; a price tick only refreshes in place.
        if (Structure(views) != structure) { Render(); return; }
        if (list != null) foreach (var v in views) list.Refresh(v.Id, RowText(v));
        if (cashLabel != null) cashLabel.text = Text.Get("Overview.cash", Market.Money(Cash()));
        if (valueLabel != null) valueLabel.text = Text.Get("Overview.holdings", Market.Money(Market.HoldingsValue()));
        var view = views.FirstOrDefault(v => v.Id == selected);
        if (view == null || view.Unavailable != null) return;
        if (priceLabel != null) priceLabel.text = PriceText(view);
        if (changeLabel != null) { changeLabel.text = ChangeText(view); C.Retint(changeLabel, ToneOf(view.DayChange)); }
        if (heldLabel != null) heldLabel.text = HeldText(view);
        RefreshQuote();
        FillChart(view);
    }
}

[HarmonyPatch(typeof(CrewSim), nameof(CrewSim.RaiseUI))]
internal static class ExchangePanelRestore
{
    private static bool Prefix(string strCOGUIKey) { if (strCOGUIKey != ExchangePanel.Key) return true; ExchangePanel.Show(); return false; }
}
