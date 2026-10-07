using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using HarmonyLib;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Pda;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Trading;
using PhobosExchange.Core;

namespace PhobosExchange;

[BepInPlugin(Id, "Phobos Exchange", Version)]
[BepInDependency(FrameworkInfo.PluginId, MinimumFrameworkVersion)]
[BepInProcess("Ostranauts.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = ExchangeRules.Owner;
    public const string Version = "0.2.1";
    public const string MinimumFrameworkVersion = "0.129.0";
    internal const string ModName = "Phobos Exchange";
    internal static Action<string> Log = _ => { };
    /// <summary>Whether the package's data folder is enabled in the game's mod list, checked at each content load.</summary>
    internal static bool Ready { get; private set; }
    private Harmony? harmony;
    private float nextPoll;

    private void Awake()
    {
        Log = x => Logger.LogInfo(x); Text.EnsureLoaded();
        PerformanceMetrics.Initialize();
        harmony = new Harmony(Id); harmony.PatchAll(typeof(Plugin).Assembly);
        // The exchange's story pack (0.2.0): company threads for writers; Framework loads and checks it with the others.
        Phobos.Ostranauts.Framework.Story.StoryContent.Register(new Phobos.Ostranauts.Framework.Data.DataPackSource(
            ExchangeRules.Owner, ExchangeRules.ModFolder, Phobos.Ostranauts.Framework.Story.StorySchema.Name, typeof(Plugin).Assembly, "PhobosExchange.story.json"));
        // Add-ons may write the story events of their own companies: exchange-<their prefix>-... (Framework 0.129.0).
        Phobos.Ostranauts.Framework.Data.AddOns.RegisterNamespace("exchange");
        FrameworkLifecycle.ContentLoading += Load;
        FrameworkLifecycle.ContentLoaded += Loaded;
        SaveBoundary.BeforeShipSave += Market.BeforeShipSave;
        // The Banking overview (or any other overview) lists the player's shares through Framework, never through us.
        PlayerHoldings.Register(Id, Market.HoldingLine);
        Log(Text.Get("Plugin.loaded", Version));
    }

    private static void Load()
    {
        Ready = DataHandler.dictModInfos?.Values.Any(m => m.strName == ModName && !m.GetIsDisabled()) == true;
        Market.Reset();
        if (!Ready) { Log(Text.Get("Content.missing_package")); return; }
        Companies.Load();
        // The app appears only when the package is enabled, so a disabled mod leaves no icon behind.
        PdaApps.Register(new PdaApp
        {
            Name = ExchangeRules.AppName, Icon = ExchangeRules.Icon,
            Label = () => Text.Get("App.label"), Title = () => Text.Get("App.title"), Tooltip = () => Text.Get("App.tooltip"),
            Open = ExchangePanel.Show
        });
        Log(Text.Get("Content.ready", Companies.Pack?.companies.Count ?? 0));
    }

    private static void Loaded()
    {
        if (Ready) Companies.Check();
    }

    private void Update()
    {
        if (!Ready || UnityEngine.Time.unscaledTime < nextPoll) return;
        nextPoll = UnityEngine.Time.unscaledTime + ExchangeRules.PollSeconds;
        try { Market.Poll(); }
        catch (Exception ex) { Log(Text.Get("Market.poll_failed", ex.ToString())); }
    }

    private void OnDestroy()
    {
        FrameworkLifecycle.ContentLoading -= Load;
        FrameworkLifecycle.ContentLoaded -= Loaded;
        SaveBoundary.BeforeShipSave -= Market.BeforeShipSave;
        PlayerHoldings.Unregister(Id);
        harmony?.UnpatchSelf();
    }
}

/// <summary>F3 <c>phobosexchange</c>: the same market as the panel, as text, through the same service. Test commands that
/// change the save need the game's <c>unlockdebug</c> and <c>confirm</c> (owner rule); readouts never do.</summary>
[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsolePatch
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        var all = strInput.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (all.Length == 0 || !all[0].Equals(ExchangeRules.Command, StringComparison.OrdinalIgnoreCase)) return true;
        var parts = Confirmations.TakeWord(all, out bool confirmed);
        string verb = parts.Length > 1 ? parts[1].ToLowerInvariant() : "quotes";
        string message;
        if (!Plugin.Ready) { message = Text.Get("Content.missing_package"); __result = false; strInput += "\n" + message; return false; }
        switch (verb)
        {
            case "quotes": message = Quotes(); __result = true; break;
            case "quote" when parts.Length == 3: __result = Quote(parts[2], out message); break;
            case "holdings": message = Holdings(); __result = true; break;
            case "alerts": message = Alerts(); __result = true; break;
            case "open":
                string? refusal = ExchangePanel.Show();
                __result = refusal == null; message = refusal ?? Text.Get("Console.opened"); break;
            case "buy" when parts.Length == 4 && Shares(parts[3], out long buy): __result = Market.Buy(parts[2], buy, out message); break;
            case "sell" when parts.Length == 4 && parts[3].Equals("all", StringComparison.OrdinalIgnoreCase): __result = Market.SellEverything(parts[2], out message); break;
            case "sell" when parts.Length == 4 && Shares(parts[3], out long sell): __result = Market.Sell(parts[2], sell, out message); break;
            case "alert" when parts.Length == 4 && parts[3].Equals("clear", StringComparison.OrdinalIgnoreCase):
                __result = Market.SetAlert(parts[2], true, null, out string a) & Market.SetAlert(parts[2], false, null, out string b); message = a + " " + b; break;
            case "alert" when parts.Length == 5 && (parts[3] == "above" || parts[3] == "below") && Level(parts[4], out double level):
                __result = Market.SetAlert(parts[2], parts[3] == "above", level, out message); break;
            case "sellall": __result = Market.SellAll(confirmed, out message); break;
            case "drivers": message = Drivers(parts.Length >= 3 ? parts[2] : null); __result = true; break;
            case "state": message = State(parts.Length >= 3 ? parts[2] : null); __result = true; break;
            case "test" when parts.Length == 5 && parts[2].Equals("shock", StringComparison.OrdinalIgnoreCase) && Level(parts[4], out double percent, allowNegative: true):
                __result = Market.TestShock(parts[3], percent, confirmed, out message); break;
            case "test" when parts.Length == 3 && parts[2].Equals("reset", StringComparison.OrdinalIgnoreCase): __result = Market.TestReset(confirmed, out message); break;
            default: message = Text.Get("Console.help"); __result = false; break;
        }
        strInput += "\n" + message;
        return false;
    }

    private static bool Shares(string text, out long shares) => long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out shares) && shares > 0;
    private static bool Level(string text, out double level, bool allowNegative = false) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out level) && !double.IsNaN(level) && !double.IsInfinity(level) && (allowNegative || level > 0);

    private static string Quotes()
    {
        var views = Market.Views();
        if (!Market.Attach(out var refusal)) return refusal!;
        if (views.Count == 0) return Text.Get("Console.none");
        var text = new StringBuilder(Text.Get("Console.quotes_heading", Companies.ExchangeName));
        foreach (var v in views) text.Append('\n').Append(Line(v));
        return text.ToString();
    }

    internal static string Line(CompanyView v) => v.Unavailable != null
        ? Text.Get("Console.quote_unavailable", v.Ticker, v.Held?.Shares ?? 0, v.Unavailable)
        : Text.Get("Console.quote_line", v.Ticker, v.Name, Market.Money(v.Quote.Mid), Change(v.DayChange), Change(v.WeekChange), v.Held?.Shares ?? 0);

    private static string Change(double share) => double.IsNaN(share) ? Text.Get("Change.none") : Market.Percent(share);

    private static bool Quote(string ticker, out string message)
    {
        var v = Market.Views().FirstOrDefault(x => x.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase));
        if (v == null) { message = Market.Attach(out var refusal) ? Text.Get("Trade.unknown", ticker) : refusal!; return false; }
        var text = new StringBuilder(Line(v));
        if (v.Unavailable == null)
        {
            text.Append('\n').Append(Text.Get("Console.quote_prices", Market.Money(v.Quote.Bid), Market.Money(v.Quote.Ask), v.SectorName));
            if (v.Held != null && v.Held.Shares > 0) text.Append('\n').Append(Text.Get("Console.quote_held", v.Held.Shares, Market.Money(v.Value), Market.Money(v.Held.Cost)));
            if (v.Alert != null && !v.Alert.Empty) text.Append('\n').Append(AlertText(v));
            text.Append('\n').Append(v.Profile);
        }
        message = text.ToString();
        return true;
    }

    private static string AlertText(CompanyView v) => Text.Get("Console.alert_line", v.Ticker,
        v.Alert?.Above is double a ? Market.Money(a) : Text.Get("Change.none"), v.Alert?.Below is double b ? Market.Money(b) : Text.Get("Change.none"));

    private static string Holdings()
    {
        var held = Market.Views().Where(v => v.Held != null && v.Held.Shares > 0).ToList();
        if (!Market.Attach(out var refusal)) return refusal!;
        if (held.Count == 0) return Text.Get("Console.no_holdings");
        var text = new StringBuilder(Text.Get("Console.holdings_heading", Market.Money(Market.HoldingsValue())));
        foreach (var v in held)
            text.Append('\n').Append(v.Unavailable != null ? Line(v) : Text.Get("Console.holding_line", v.Ticker, v.Held!.Shares, Market.Money(v.Value), Market.Money(v.Held.Cost), Market.Money(v.Held.Realised)));
        return text.ToString();
    }

    private static string Alerts()
    {
        var set = Market.Views().Where(v => v.Alert != null && !v.Alert.Empty).ToList();
        if (!Market.Attach(out var refusal)) return refusal!;
        return set.Count == 0 ? Text.Get("Console.no_alerts") : string.Join("\n", set.Select(AlertText));
    }

    /// <summary>Read-only: each driver's station, category, the game's factor now, where the driver is heading and where it is.</summary>
    private static string Drivers(string? ticker)
    {
        if (!Market.Attach(out var refusal) || Market.Model is not MarketModel m) return refusal!;
        var text = new StringBuilder(Text.Get("Console.drivers_heading"));
        for (int i = 0; i < m.Count; i++)
        {
            if (ticker != null && !m.Entries[i].ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase)) continue;
            var drivers = m.Entries[i].drivers;
            if (drivers.Count == 0) { text.Append('\n').Append(Text.Get("Console.driver_none", m.Entries[i].ticker)); continue; }
            for (int j = 0; j < drivers.Count; j++)
            {
                var d = drivers[j];
                double? factor = NativeMarket.Factor(d.station, d.category);
                text.Append('\n').Append(Text.Get("Console.driver_line", m.Entries[i].ticker, NativeMarket.StationName(d.station), d.station, NativeMarket.CategoryName(d.category),
                    factor is double f ? f.ToString("0.000", CultureInfo.InvariantCulture) : Text.Get("Console.no_reading"),
                    d.weight.ToString("+0.00;-0.00", CultureInfo.InvariantCulture), m.DriverGoal(i, j).ToString("+0.000;-0.000;0.000", CultureInfo.InvariantCulture),
                    m.DriverPart(i, j).ToString("+0.000;-0.000;0.000", CultureInfo.InvariantCulture)));
            }
        }
        return text.ToString();
    }

    /// <summary>Read-only: the parts of each price and the phases' speeds, for a playtest.</summary>
    private static string State(string? ticker)
    {
        if (!Market.Attach(out var refusal) || Market.Model is not MarketModel m) return refusal!;
        string F(double v) => v.ToString("+0.000;-0.000;0.000", CultureInfo.InvariantCulture);
        var text = new StringBuilder(Text.Get("Console.state_heading", m.Clock, Market.Record.TestChanges));
        for (int i = 0; i < m.Count; i++)
        {
            if (ticker != null && !m.Entries[i].ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase)) continue;
            double drivers = 0;
            for (int j = 0; j < m.DriverCount(i); j++) drivers += m.DriverPart(i, j);
            text.Append('\n').Append(Text.Get("Console.state_line", m.Entries[i].ticker, F(m.Part(i, Cause.Market)), F(m.Part(i, Cause.Sector)), F(m.Part(i, Cause.Company)),
                F(drivers), F(m.Part(i, Cause.Noise)), F(m.Part(i, Cause.Trading)), F(m.Part(i, Cause.Drift)), Market.Percent(Math.Exp(m.TotalSpeed(i)) - 1)));
        }
        return text.ToString();
    }
}

// A new game or a load reads the player's market again.
[HarmonyPatch]
internal static class MarketReloadPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    private static void Prefix() => Market.Reset();
}
