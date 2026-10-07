using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using HarmonyLib;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Pda;
using PhobosBank.Core;

namespace PhobosBank;

[BepInPlugin(Id, "Phobos Banking", Version)]
[BepInDependency(FrameworkInfo.PluginId, MinimumFrameworkVersion)]
[BepInProcess("Ostranauts.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = BankRules.Owner;
    public const string Version = "0.8.0";
    public const string MinimumFrameworkVersion = "0.132.0";
    internal const string ModName = "Phobos Banking";
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
        // The lenders' officers, threads and letters (0.4.0): a story pack, loaded and checked by Framework.
        Phobos.Ostranauts.Framework.Story.StoryContent.Register(new Phobos.Ostranauts.Framework.Data.DataPackSource(
            BankRules.Owner, BankRules.ModFolder, Phobos.Ostranauts.Framework.Story.StorySchema.Name, typeof(Plugin).Assembly, "PhobosBank.story.json"));
        FrameworkLifecycle.ContentLoading += Load;
        FrameworkLifecycle.ContentLoaded += Loaded;
        Log(Text.Get("Plugin.loaded", Version));
    }

    private static void Load()
    {
        Ready = DataHandler.dictModInfos?.Values.Any(m => m.strName == ModName && !m.GetIsDisabled()) == true;
        Loans.Reset(); Financing.Reset();
        if (!Ready) { Log(Text.Get("Content.missing_package")); return; }
        Lenders.Load();
        // The app appears only when the package is enabled, so a disabled mod leaves no icon behind.
        PdaApps.Register(new PdaApp
        {
            Name = BankRules.AppName, Icon = BankRules.Icon,
            Label = () => Text.Get("App.label"), Title = () => Text.Get("App.title"), Tooltip = () => Text.Get("App.tooltip"),
            Open = BankPanel.Show
        });
        if (!Debts.CanReadLoans) Log(Text.Get("Content.loans_unreadable"));
        Log(Text.Get("Content.ready"));
    }

    /// <summary>Content loaded and the story library built: lenders naming unknown places or people are left out.</summary>
    private static void Loaded()
    {
        if (!Ready) return;
        Lenders.Check(Phobos.Ostranauts.Framework.Story.StoryContent.Library);
        Log(Text.Get("Lenders.ready", Lenders.All.Count));
    }

    private void Update()
    {
        if (!Ready || UnityEngine.Time.unscaledTime < nextPoll) return;
        nextPoll = UnityEngine.Time.unscaledTime + BankRules.LoanPollSeconds;
        try { Loans.Poll(); }
        catch (Exception ex) { Log(Text.Get("Loans.poll_failed", ex.ToString())); }
    }

    private void OnDestroy()
    {
        FrameworkLifecycle.ContentLoading -= Load;
        FrameworkLifecycle.ContentLoaded -= Loaded;
        harmony?.UnpatchSelf();
    }
}

/// <summary>F3 <c>phobosbank</c>: the same debts as the panel, as text, and the panel itself.</summary>
[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsolePatch
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        var parts = strInput.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !parts[0].Equals("phobosbank", StringComparison.OrdinalIgnoreCase)) return true;
        string verb = parts.Length > 1 ? parts[1].ToLowerInvariant() : "debts";
        string message;
        switch (verb)
        {
            case "debts": message = Describe(); __result = true; break;
            case "open":
                string? refusal = Plugin.Ready ? BankPanel.Show() : Text.Get("Content.missing_package");
                __result = refusal == null; message = refusal ?? Text.Get("Console.opened"); break;
            case "finances": __result = Debts.OpenFinances(out message); break;
            case "lenders": message = DescribeLenders(); __result = true; break;
            case "approve" when parts.Length == 4: __result = Financing.PreApprove(parts[2], parts[3].ToLowerInvariant(), out message); break;
            case "withdraw": __result = Financing.Withdraw(out message); break;
            // The credit line (0.6.0).
            case "line": message = CreditLines.Describe(); __result = true; break;
            case "openline":
            {
                string? id = parts.Length >= 3 ? parts[2] : CreditLines.DefaultId();
                if (id == null) { message = Text.Get("Console.help"); __result = false; break; }
                __result = CreditLines.Open(id, out message); break;
            }
            case "draw":
            {
                string? id = parts.Length >= 4 ? parts[3] : CreditLines.DefaultId();
                if (parts.Length < 3 || id == null || !double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double sum))
                { message = Text.Get("Console.help"); __result = false; break; }
                __result = CreditLines.Draw(id, sum, out message); break;
            }
            case "loans": message = Loans.Describe(); __result = true; break;
            case "borrow":
                if (parts.Length != 4 || !double.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double amount))
                { message = Text.Get("Console.help"); __result = false; break; }
                __result = Loans.Borrow(parts[2], amount, out message); break;
            default: message = Text.Get("Console.help"); __result = false; break;
        }
        strInput += "\n" + message;
        return false;
    }

    private static string DescribeLenders()
    {
        var views = Loans.Offers();
        if (views.Count == 0) return Text.Get("Lenders.none");
        return string.Join("\n", views.Select(v => Text.Get("Console.lender", v.Id, v.Name, v.Home, Loans.Percent(v.Entry.ratePerShift), BankPanel.Money(v.Entry.minPrincipal),
            BankPanel.Money(v.Headroom), v.Unavailable ?? Text.Get("Lender.open"))));
    }

    private static string Describe()
    {
        if (CrewSim.coPlayer == null) return Text.Get("Panel.no_game");
        var summary = Debts.Read();
        var text = new StringBuilder(Text.Get("Overview.cash", BankPanel.Money(Debts.Cash())));
        if (summary.Debts.Count == 0) return text.Append('\n').Append(Text.Get("Overview.clear")).ToString();
        foreach (var d in summary.Debts)
        {
            text.Append('\n');
            text.Append(d.Kind switch
            {
                DebtKind.Loan => Text.Get("Console.loan", d.Creditor, BankPanel.Money(d.Amount), BankPanel.Money(d.Instalment), d.ShiftsLeft, d.Description),
                DebtKind.Bill => Text.Get(d.Late ? "Console.bill_late" : "Console.bill", d.Creditor, BankPanel.Money(d.Amount), d.Description),
                _ => Text.Get("Console.charge", d.Creditor, BankPanel.Money(d.Amount), Text.Get("Every." + d.Every), d.Description)
            });
        }
        return text.ToString();
    }
}

// A new game or a load reads the player's loan book again.
[HarmonyPatch]
internal static class LoanReloadPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    private static void Prefix() { Loans.Reset(); Financing.Reset(); }
}
