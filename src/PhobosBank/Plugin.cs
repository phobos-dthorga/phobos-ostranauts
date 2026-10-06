using System;
using System.Linq;
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
    public const string Version = "0.1.1";
    public const string MinimumFrameworkVersion = "0.126.0";
    internal const string ModName = "Phobos Banking";
    internal static Action<string> Log = _ => { };
    /// <summary>Whether the package's data folder is enabled in the game's mod list, checked at each content load.</summary>
    internal static bool Ready { get; private set; }
    private Harmony? harmony;

    private void Awake()
    {
        Log = x => Logger.LogInfo(x); Text.EnsureLoaded();
        PerformanceMetrics.Initialize();
        harmony = new Harmony(Id); harmony.PatchAll(typeof(Plugin).Assembly);
        FrameworkLifecycle.ContentLoading += Load;
        Log(Text.Get("Plugin.loaded", Version));
    }

    private static void Load()
    {
        Ready = DataHandler.dictModInfos?.Values.Any(m => m.strName == ModName && !m.GetIsDisabled()) == true;
        if (!Ready) { Log(Text.Get("Content.missing_package")); return; }
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

    private void OnDestroy()
    {
        FrameworkLifecycle.ContentLoading -= Load;
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
            default: message = Text.Get("Console.help"); __result = false; break;
        }
        strInput += "\n" + message;
        return false;
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
