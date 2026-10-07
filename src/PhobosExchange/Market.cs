using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phobos.Ostranauts.Framework.Diagnostics;
using Phobos.Ostranauts.Framework.Notices;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Trading;
using PhobosExchange.Core;

namespace PhobosExchange;

/// <summary>One company as the panel and F3 show it.</summary>
internal sealed class CompanyView
{
    public string Id = "", Ticker = "", Name = "", Sector = "", SectorName = "", Profile = "";
    public int Index = -1;
    public Quote Quote;
    /// <summary>Change over the last game day and week, as shares of the price; NaN until the history has them.</summary>
    public double DayChange = double.NaN, WeekChange = double.NaN;
    public Holding? Held;
    public Alert? Alert;
    /// <summary>What the holding would fetch at the selling price now.</summary>
    public double Value;
    /// <summary>Why the company cannot be traded now (no longer listed, or its record is from a newer version), or null.</summary>
    public string? Unavailable;
}

/// <summary>The exchange service (Phobos Exchange 0.1.0). Reads the player's record, steps the market to the game's clock
/// once a real second, reads the game's cargo market every game hour for the drivers, and carries out trades, alerts and
/// test commands. The panel and F3 commands only call it. Money moves through the player's <c>StatUSD</c> and the game's
/// own ledger; nothing else in the game is changed.</summary>
internal static class Market
{
    private static CondOwner? owner;
    private static ExchangeRecord record = new();
    private static MarketModel? model;
    private static MoveWatch? watch;
    private static SavedStateStatus status = SavedStateStatus.Missing;
    private static bool dirty;
    private static long driverHour = long.MinValue;
    private static readonly List<MoveReport> reports = new(64);
    private static readonly List<(int Company, int Side, double Level)> fired = new(16);
    private static (int Company, Alert Alert)[] alertTable = Array.Empty<(int, Alert)>();
    private static readonly Observer observer = new();
    /// <summary>The latest wire lines and summaries, newest first, for the panel's overview (not saved).</summary>
    internal static readonly List<string> RecentWire = new();
    private const int RecentWireLines = 6;
    private static double[] before = Array.Empty<double>();

    /// <summary>Rises whenever prices, holdings or alerts change, so the panel knows to refresh.</summary>
    internal static int Version { get; private set; }
    internal static MarketModel? Model => model;
    internal static ExchangeRecord Record => record;

    /// <summary>A new game or a load: the next poll reads the player's record again.</summary>
    internal static void Reset() { owner = null; record = new ExchangeRecord(); model = null; watch = null; status = SavedStateStatus.Missing; dirty = false; driverHour = long.MinValue; Version++; }

    private static ObjectStateStore Store(CondOwner co) => new(co.mapGUIPropMaps, ExchangeRecord.Name, ExchangeRules.Owner, ExchangeRecord.Version);

    /// <summary>The player's market, read on first use after a load or a change of player character. False with no game,
    /// no pack, or a saved record this version must not touch (a newer version's, or damaged): then nothing trades until
    /// that is sorted out, and the log says so once.</summary>
    internal static bool Attach(out string? refusal)
    {
        refusal = null;
        var player = CrewSim.coPlayer;
        if (player == null || player.bDestroyed || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || StarSystem.fEpoch <= 0)
        { refusal = Text.Get("Panel.no_game"); return false; }
        if (Companies.Pack == null) { refusal = Text.Get("Market.pack_missing"); return false; }
        if (!ReferenceEquals(owner, player))
        {
            owner = player;
            status = Store(player).Read(out var fields);
            model = null; watch = null;
            if (status != SavedStateStatus.Ready && status != SavedStateStatus.Missing) { Plugin.Log(Text.Get("Market.record_kept", status)); refusal = Text.Get("Market.record_unreadable"); return false; }
            record = ExchangeRecord.Decode(fields);
            bool fresh = record.Clock == null;
            model = new MarketModel(Companies.Pack, record);
            ReadDrivers(force: true);
            long now = ExchangeRules.StepOf(StarSystem.fEpoch);
            using (Performance.Measure(PerformanceMetrics.CatchUp)) model.Start(now, StableNoise.Fnv64(player.strID));
            watch = new MoveWatch(model);
            RebuildAlerts();
            if (fresh) Plugin.Log(Text.Get("Market.opened", model.Count));
            dirty = true; Save(); Version++;
        }
        if (model == null) { refusal = Text.Get("Market.record_unreadable"); return false; }
        return true;
    }

    /// <summary>Once a real second: read the drivers when a game hour has turned, step the market to the game's clock, and
    /// tell the player what happened, in one line or a short summary.</summary>
    internal static void Poll()
    {
        if (!Attach(out _) || model == null) return;
        long now = ExchangeRules.StepOf(StarSystem.fEpoch);
        if (now <= model.Clock) return;
        long gap = now - model.Clock;
        ReadDrivers(force: false);
        if (before.Length != model.Count) before = new double[model.Count];
        Array.Copy(model.LnPrice, before, model.Count);
        double heldBefore = HoldingsValue();
        reports.Clear(); fired.Clear();
        observer.Reports = gap * ExchangeRules.StepSeconds <= ExchangeRules.AwayDays * ExchangeRules.DaySeconds;
        using (Performance.Measure(PerformanceMetrics.CatchUp)) model.Advance(now, observer);
        dirty = true; Version++;
        if (!observer.Reports) Away(gap, heldBefore);
        else Tell();
        if (fired.Count > 0) { RebuildAlerts(); Save(); }
    }

    /// <summary>Reads the game's price factor for every driver: once a game hour, or now (at attach).</summary>
    private static void ReadDrivers(bool force)
    {
        if (model == null) return;
        long hour = ExchangeRules.HourOf(ExchangeRules.StepOf(StarSystem.fEpoch));
        if (!force && hour == driverHour) return;
        driverHour = hour;
        using var measure = Performance.Measure(PerformanceMetrics.Drivers);
        for (int i = 0; i < model.Count; i++)
        {
            var drivers = model.Entries[i].drivers;
            for (int j = 0; j < drivers.Count; j++) model.SetDriverFactor(i, j, NativeMarket.Factor(drivers[j].station, drivers[j].category));
        }
    }

    /// <summary>Writes the record when it changed: after a trade, an alert or a test command, and before the game saves.</summary>
    internal static void Save()
    {
        if (!dirty || owner == null || owner.bDestroyed || model == null) return;
        if (Store(owner).TryWriteIfChanged(record.Encode())) { dirty = false; status = SavedStateStatus.Ready; return; }
        Plugin.Log(Text.Get("Market.record_refused"));
    }

    /// <summary>The save boundary: every native save carries the market's latest state.</summary>
    internal static void BeforeShipSave(Ship ship)
    {
        if (dirty && owner != null && !owner.bDestroyed) Save();
    }

    // ---- What the player is told ------------------------------------------------------------------------------

    private static void Tell()
    {
        if (model == null) return;
        foreach (var (company, side, level) in fired.Take(3))
        {
            string name = Companies.Name(model.Ids[company]), ticker = model.Entries[company].ticker, price = Money(model.Price(company));
            string log = Text.Get(side > 0 ? "Alert.fired_above" : "Alert.fired_below", name, ticker, price, Money(level));
            Notify("PhobosExchange.alert." + ticker, NoticeLevel.Caution, log, Text.Get("Alert.banner", ticker, price));
        }
        if (fired.Count > 3) Notify("PhobosExchange.alerts", NoticeLevel.Caution, Text.Get("Alert.more", fired.Count - 3), null);
        var shown = reports.OrderByDescending(r => Math.Abs(r.Change)).Take(3).ToList();
        foreach (var r in shown) Notify("PhobosExchange.wire", NoticeLevel.Info, Wire(r), null);
        if (reports.Count > shown.Count) Notify("PhobosExchange.wire", NoticeLevel.Info, Text.Get("Wire.more", reports.Count - shown.Count), null);
    }

    /// <summary>One summary after a long gap: how long, the biggest movers, and the player's shares.</summary>
    private static void Away(long gap, double heldBefore)
    {
        if (model == null || model.Count == 0) return;
        double days = gap * ExchangeRules.StepSeconds / ExchangeRules.DaySeconds;
        string span = days >= 360 ? Text.Get("Away.years", (days / 360).ToString("0.#")) : Text.Get("Away.days", Math.Round(days).ToString("0"));
        var movers = MoveWatch.Biggest(before, model.LnPrice, 3).Select(i => Text.Get("Away.mover", model.Entries[i].ticker, Percent(Math.Exp(model.LnPrice[i] - before[i]) - 1)));
        string text = Text.Get("Away.summary", span, string.Join(", ", movers));
        double heldAfter = HoldingsValue();
        if (heldBefore > 0 || heldAfter > 0) text += " " + Text.Get("Away.holdings", Money(heldAfter), Money(heldBefore));
        if (fired.Count > 0) text += " " + Text.Get(ExchangeRules.CountKey("Away.alerts", fired.Count), fired.Count);
        Notify("PhobosExchange.away", NoticeLevel.Info, text, null);
    }

    private static void Notify(string key, NoticeLevel level, string log, string? banner)
    {
        RecentWire.Insert(0, log);
        if (RecentWire.Count > RecentWireLines) RecentWire.RemoveAt(RecentWire.Count - 1);
        var player = CrewSim.coPlayer;
        if (player == null) return;
        if (player.ship == null || !PlayerNotices.Post(player.ship, key, level, log, banner)) player.LogMessage(log, level == NoticeLevel.Caution ? "Badish" : "Neutral", player.strID);
    }

    /// <summary>A market wire line: what moved, how far, and the largest part of the price that moved it.</summary>
    internal static string Wire(MoveReport r)
    {
        if (model == null) return "";
        string id = model.Ids[r.Company], name = Companies.Name(id), ticker = model.Entries[r.Company].ticker;
        string cause = Cause(r);
        if (r.Turn) return Text.Get(r.Change >= 0 ? "Wire.turn_up" : "Wire.turn_down", name, ticker, cause);
        return Text.Get(r.Change >= 0 ? "Wire.rose" : "Wire.fell", name, ticker, Percent(Math.Abs(r.Change)), cause);
    }

    private static string Cause(MoveReport r)
    {
        if (model == null) return "";
        var c = model.Entries[r.Company];
        switch (r.Cause)
        {
            case Core.Cause.Market: return Text.Get("Cause.market");
            case Core.Cause.Sector: return Text.Get("Cause.sector", Companies.SectorName(c.sector));
            case Core.Cause.Company: return Text.Get("Cause.company");
            case Core.Cause.Trading: return Text.Get("Cause.trading");
            case Core.Cause.Drift: return Text.Get("Cause.drift");
            case Core.Cause.Driver when r.Driver >= 0 && r.Driver < c.drivers.Count:
            {
                var d = c.drivers[r.Driver];
                // The driver's part moved with the price; the game's factor moved that way times the sign of its weight
                // (a dearer input pulls the price down).
                bool scarcer = (r.Change >= 0) == (d.weight > 0);
                return Text.Get(scarcer ? "Cause.scarce" : "Cause.plenty", NativeMarket.CategoryName(d.category), NativeMarket.StationName(d.station));
            }
            default: return Text.Get("Cause.noise");
        }
    }

    // ---- Trading ----------------------------------------------------------------------------------------------

    internal static bool Buy(string ticker, long shares, out string message) => Trade(ticker, shares, true, out message);
    internal static bool Sell(string ticker, long shares, out string message) => Trade(ticker, shares, false, out message);

    private static bool Trade(string ticker, long shares, bool buy, out string message)
    {
        if (!Attach(out var refusal) || model == null) { message = refusal!; return false; }
        if (!Find(ticker, out int i, out message)) return false;
        var player = owner!;
        var c = model.Entries[i];
        var market = Companies.Pack!.market;
        record.Holdings.TryGetValue(model.Ids[i], out var held);
        double cash = player.GetCondAmount(Ledger.CURRENCY);
        var fill = buy ? TradeRules.Buy(c, market, model.LnPrice[i], shares, held, cash) : TradeRules.Sell(c, market, model.LnPrice[i], shares, held);
        string name = Companies.Name(model.Ids[i]);
        if (!fill.Ok) { message = Refusal(fill, name, c.ticker); return false; }
        if (held == null) record.Holdings[model.Ids[i]] = held = new Holding();
        TradeRules.Apply(held, fill);
        model.Push(i, fill.Impact);
        string exchange = Companies.ExchangeName;
        player.AddCondAmount(Ledger.CURRENCY, buy ? -fill.Total : fill.Total);
        Ledger.RecordTransaction(player, exchange, buy ? -fill.Total : fill.Total,
            ExchangeRules.Clean(Text.Get(buy ? "Ledger.bought" : "Ledger.sold", fill.Shares, c.ticker, Money(fill.PerShare))));
        dirty = true; Save(); Version++;
        message = Text.Get(buy ? "Trade.bought" : "Trade.sold", fill.Shares, name, c.ticker, Money(fill.PerShare), Money(fill.Commission), Money(fill.Total), held.Shares);
        player.LogMessage(message, "Neutral", player.strID);
        return true;
    }

    internal static string Refusal(Fill fill, string name, string ticker) => fill.Refusal switch
    {
        Core.Refusal.NoShares => Text.Get("Trade.no_shares"),
        Core.Refusal.OrderTooLarge => Text.Get("Trade.order_too_large", fill.Limit.ToString("n0"), ticker),
        Core.Refusal.HoldingCap => Text.Get("Trade.holding_cap", Money(fill.Limit), name),
        Core.Refusal.NotEnoughCash => Text.Get("Trade.cash", Money(fill.Total), Money(fill.Limit)),
        Core.Refusal.NotHeld => Text.Get("Trade.not_held", fill.Limit.ToString("n0"), ticker),
        Core.Refusal.TooSmall => Text.Get("Trade.too_small", Money(fill.Limit)),
        _ => Text.Get("Trade.refused")
    };

    /// <summary>Sells all of one holding, in orders no bigger than the largest allowed.</summary>
    internal static bool SellEverything(string ticker, out string message)
    {
        if (!Attach(out var refusal) || model == null) { message = refusal!; return false; }
        if (!Find(ticker, out int i, out message)) return false;
        string id = model.Ids[i];
        long left = record.Holdings.TryGetValue(id, out var h) ? h.Shares : 0;
        if (left <= 0) { message = Text.Get("Trade.not_held", 0, model.Entries[i].ticker); return false; }
        long most = TradeRules.MaxOrder(model.Entries[i], Companies.Pack!.market);
        var lines = new List<string>();
        while (left > 0)
        {
            if (!Sell(model.Entries[i].ticker, Math.Min(left, most), out string line)) { lines.Add(line); break; }
            lines.Add(line);
            left = record.Holdings[id].Shares;
        }
        message = string.Join(" ", lines);
        return left == 0;
    }

    /// <summary>Sells every holding, press twice (a panel second press or <c>confirm</c> in F3): what to do before removing
    /// the mod, or to cash out.</summary>
    internal static bool SellAll(bool confirmed, out string message)
    {
        if (!Attach(out var refusal) || model == null) { message = refusal!; return false; }
        var held = model.Ids.Select((id, i) => (id, i)).Where(p => record.Holdings.TryGetValue(p.id, out var h) && h.Shares > 0).ToList();
        if (held.Count == 0) { message = Text.Get("SellAll.nothing"); return false; }
        // The warning names only what will not change between the two presses (prices tick every second).
        if (!Phobos.Ostranauts.Framework.Controls.Confirmations.Ask(Text.Get(ExchangeRules.CountKey("SellAll.warning", held.Count), held.Count), confirmed, out message)) return false;
        var problems = new List<string>();
        double before = owner!.GetCondAmount(Ledger.CURRENCY);
        foreach (var (id, i) in held)
            if (!SellEverything(model.Entries[i].ticker, out string line)) problems.Add(line);
        message = Text.Get("SellAll.done", Money(owner.GetCondAmount(Ledger.CURRENCY) - before)) + (problems.Count > 0 ? " " + string.Join(" ", problems) : "");
        return problems.Count == 0;
    }

    /// <summary>Sets or clears one side of a price alert.</summary>
    internal static bool SetAlert(string ticker, bool above, double? level, out string message)
    {
        if (!Attach(out var refusal) || model == null) { message = refusal!; return false; }
        if (!Find(ticker, out int i, out message)) return false;
        string id = model.Ids[i];
        double price = model.Price(i);
        if (!record.Alerts.TryGetValue(id, out var alert)) record.Alerts[id] = alert = new Alert();
        if (level is not double l)
        {
            if (above) alert.Above = null; else alert.Below = null;
            message = Text.Get(above ? "Alert.cleared_above" : "Alert.cleared_below", model.Entries[i].ticker);
        }
        else
        {
            if (!AlertRules.Valid(l, price, above)) { message = Text.Get(above ? "Alert.must_be_above" : "Alert.must_be_below", Money(price)); return false; }
            if (above) alert.Above = l; else alert.Below = l;
            message = Text.Get(above ? "Alert.set_above" : "Alert.set_below", model.Entries[i].ticker, Money(l));
        }
        RebuildAlerts();
        dirty = true; Save(); Version++;
        return true;
    }

    private static void RebuildAlerts()
    {
        if (model == null) { alertTable = Array.Empty<(int, Alert)>(); return; }
        foreach (var key in record.Alerts.Where(p => p.Value.Empty).Select(p => p.Key).ToList()) record.Alerts.Remove(key);
        alertTable = record.Alerts.Select(p => (model.IndexOf(p.Key), p.Value)).Where(p => p.Item1 >= 0).ToArray();
    }

    // ---- Test commands (owner rule: unlockdebug, a warning, confirm, and the save marked) -------------------------

    internal static bool TestShock(string ticker, double percent, bool confirmed, out string message)
    {
        if (!Attach(out var refusal) || model == null) { message = refusal!; return false; }
        if (!Find(ticker, out int i, out message)) return false;
        if (!(percent > -90 && percent < 1000)) { message = Text.Get("Test.shock_range"); return false; }
        string what = Text.Get("Test.shock_what", model.Entries[i].ticker, Percent(percent / 100));
        if (!DebugCommands.Gate(Plugin.ModName, what, confirmed, out message)) return false;
        model.Jump(i, Math.Log(1 + percent / 100));
        MarkTested(what);
        message = DebugCommands.Done(what);
        return true;
    }

    internal static bool TestReset(bool confirmed, out string message)
    {
        if (!Attach(out var refusal) || model == null || owner == null) { message = refusal!; return false; }
        string what = Text.Get("Test.reset_what");
        if (!DebugCommands.Gate(Plugin.ModName, what, confirmed, out message)) return false;
        var fresh = new ExchangeRecord();
        foreach (var pair in record.Holdings) fresh.Holdings[pair.Key] = pair.Value;
        fresh.TestChanges = record.TestChanges;
        record = fresh;
        model = new MarketModel(Companies.Pack!, record);
        ReadDrivers(force: true);
        // A new seed, so the reset market differs from the one it replaces.
        model.Start(ExchangeRules.StepOf(StarSystem.fEpoch), StableNoise.Mix(StableNoise.Fnv64(owner.strID) + (ulong)(record.TestChanges + 1)));
        watch = new MoveWatch(model);
        RebuildAlerts();
        MarkTested(what);
        message = DebugCommands.Done(what);
        return true;
    }

    private static void MarkTested(string what)
    {
        record.TestChanges++;
        record.LastTestStep = model?.Clock ?? 0;
        record.LastTest = what;
        DebugCommands.Record(ExchangeRules.Owner, what);
        dirty = true; Save(); Version++;
    }

    // ---- Views ------------------------------------------------------------------------------------------------

    private static bool Find(string ticker, out int index, out string message)
    {
        index = model?.IndexOfTicker(ticker ?? "") ?? -1;
        message = index >= 0 ? "" : Text.Get("Trade.unknown", ticker ?? "");
        return index >= 0;
    }

    /// <summary>Every listed company, then holdings in companies no longer listed.</summary>
    internal static List<CompanyView> Views()
    {
        var views = new List<CompanyView>();
        if (!Attach(out _) || model == null) return views;
        for (int i = 0; i < model.Count; i++)
        {
            var c = model.Entries[i];
            var v = new CompanyView
            {
                Id = model.Ids[i], Index = i, Ticker = c.ticker, Name = Companies.Name(model.Ids[i]), Sector = c.sector,
                SectorName = Companies.SectorName(c.sector), Profile = Companies.Profile(model.Ids[i]), Quote = TradeRules.QuoteOf(model.LnPrice[i], c.spread)
            };
            var h = model.History(i);
            if (h.Hourly.Count >= 25) v.DayChange = Math.Exp(model.LnPrice[i] - h.Hourly[h.Hourly.Count - 25]) - 1;
            if (h.Daily.Count >= 8) v.WeekChange = Math.Exp(model.LnPrice[i] - h.Daily[h.Daily.Count - 8]) - 1;
            record.Holdings.TryGetValue(v.Id, out v.Held);
            record.Alerts.TryGetValue(v.Id, out v.Alert);
            v.Value = TradeRules.Value(v.Held, model.LnPrice[i], c.spread);
            views.Add(v);
        }
        foreach (var pair in record.Holdings.Where(p => p.Value.Shares > 0 && model.IndexOf(p.Key) < 0))
        {
            record.Companies.TryGetValue(pair.Key, out var state);
            views.Add(new CompanyView
            {
                Id = pair.Key, Ticker = pair.Key, Name = pair.Key, Held = pair.Value,
                Unavailable = record.Unreadable.Contains(pair.Key) ? Text.Get("Company.unreadable") : Text.Get("Company.unlisted", state == null ? "-" : Money(ExchangeRules.Price(state.LastLn)))
            });
        }
        return views;
    }

    /// <summary>What the player's listed shares would fetch at the selling prices now.</summary>
    internal static double HoldingsValue()
    {
        if (model == null) return 0;
        double sum = 0;
        for (int i = 0; i < model.Count; i++)
            if (record.Holdings.TryGetValue(model.Ids[i], out var h)) sum += TradeRules.Value(h, model.LnPrice[i], model.Entries[i].spread);
        return sum;
    }

    /// <summary>The line for other mods' overviews (Framework <see cref="PlayerHoldings"/>): null when nothing is held.</summary>
    internal static HoldingLine? HoldingLine()
    {
        if (!Plugin.Ready || model == null || owner == null || !ReferenceEquals(owner, CrewSim.coPlayer)) return null;
        int companies = record.Holdings.Count(p => p.Value.Shares > 0);
        if (companies == 0) return null;
        return new HoldingLine
        {
            Label = Text.Get("Holdings.label"), Value = HoldingsValue(), App = ExchangeRules.AppName,
            Detail = Text.Get(ExchangeRules.CountKey("Holdings.detail", companies), companies, Companies.ExchangeName)
        };
    }

    internal static string Money(double amount) => Text.Get("Money", amount.ToString("n"));
    internal static string Percent(double share) => Text.Get("Percent", (share * 100).ToString("+0.0;-0.0;0.0"));

    /// <summary>Watches each step without allocating: alerts on every step, reports at each hourly close.</summary>
    private sealed class Observer : IMarketObserver
    {
        public bool Reports = true;
        public void Stepped(MarketModel m, long step, bool fine)
        {
            var table = alertTable;
            for (int k = 0; k < table.Length; k++)
            {
                var (company, alert) = table[k];
                double? above = alert.Above, below = alert.Below;
                int side = AlertRules.Check(alert, m.Price(company));
                if (side != 0 && fired.Count < fired.Capacity) fired.Add((company, side, side > 0 ? above ?? 0 : below ?? 0));
            }
        }

        public void HourClosed(MarketModel m, long hour)
        {
            if (Reports && watch != null) watch.HourClosed(m, hour, reports);
        }
    }
}
