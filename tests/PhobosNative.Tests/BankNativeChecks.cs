using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Pda;
using Phobos.Ostranauts.Framework.Story;
using PhobosBank;
using PhobosBank.Core;

/// <summary>Phobos Banking 0.1.0 and Framework 0.126.0 PDA apps against the installed game: the mortgage figures the
/// panel mirrors, the private loan list it reads, the game's late wording, and the PDA's own app switch, icon entry and
/// tooltip keys. Nothing here runs a game session or touches a ledger in play.</summary>
internal static class BankNativeChecks
{
    internal static void Run(string game, string repo, Action<bool, string> check)
    {
        // ---- Mortgages ------------------------------------------------------------------------------
        check(Debts.CanReadLoans, "The game's running loans are a private List<LedgerLI> named aMortgage, as the panel reads them");
        check(Math.Abs(Ledger.MORTGAGE_RATE - BankRules.MortgageRatePerShift) < 1e-9, "The mirrored mortgage rate is the game's");
        check(Ledger.CURRENCY == BankRules.Currency, "The mirrored currency is the game's");
        double savedEpoch = StarSystem.fEpoch;
        try
        {
            const double start = 1000000;
            foreach (double elapsed in new[] { 0, 3600, BankRules.ShiftSeconds, BankRules.GameDaySeconds, 30 * BankRules.GameDaySeconds, 120 * BankRules.GameDaySeconds, BankRules.MortgageTermSeconds - BankRules.ShiftSeconds - 1 })
            {
                foreach (float balance in new[] { 97395f, 250000f, 910636f })
                {
                    var loan = new LedgerLI("Ogiso's Bank", "player", balance, "check", Ledger.CURRENCY, start, LedgerLI.Frequency.Mortgage);
                    StarSystem.fEpoch = start + elapsed;
                    // The game charges its formula's figure but never more than the balance (Ledger.ProcessRepeatingOfType).
                    double native = Math.Min(MathUtils.MortgagePaymentPerShift(loan), balance), ours = BankRules.Instalment(balance, BankRules.ShiftsLeft(elapsed));
                    check(Math.Abs(native - ours) <= 1e-3 * ours, $"The mirrored instalment matches the game's at {elapsed:0} s on {balance:0}: game {native:0.00}, ours {ours:0.00}");
                }
            }
        }
        finally { StarSystem.fEpoch = savedEpoch; }
        check(SingleConstant(typeof(Ledger).GetMethod(nameof(Ledger.Skip))!, (float)BankRules.LateFeeShare), "The game's shift change still adds a 17.5% late fee");

        // ---- The game's wording for late lines ---------------------------------------------------
        var strings = GameStrings(game);
        check(strings.ContainsKey("GUI_FINANCE_OVERDUE") && strings.ContainsKey("GUI_FINANCE_LATE"), "The game still names overdue instalments and late fees");
        check(BankRules.IsLate(strings["GUI_FINANCE_OVERDUE"] + "Mortgage", strings["GUI_FINANCE_OVERDUE"], strings["GUI_FINANCE_LATE"]) &&
              BankRules.IsLate(strings["GUI_FINANCE_LATE"] + "date", strings["GUI_FINANCE_OVERDUE"], strings["GUI_FINANCE_LATE"]), "The game's own late wording is recognised");

        // ---- The PDA ---------------------------------------------------------------------------------
        var open = typeof(GUIPDA).GetMethod(nameof(GUIPDA.OpenApp), BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
        check(open != null, "GUIPDA.OpenApp(string) is public and static, as Framework's prefix expects");
        var literals = Literals(open!);
        // "actions" is the job-paint page the orders app opens, passed as an argument, not an app name.
        var switchNames = literals.Where(s => s.Length > 0 && s != "actions" && s.All(c => c >= 'a' && c <= 'z')).ToHashSet(StringComparer.Ordinal);
        check(switchNames.SetEquals(PdaApps.NativeNames), "Framework's list of the game's app names matches the game's own switch: " + string.Join(", ", switchNames.Except(PdaApps.NativeNames).Concat(PdaApps.NativeNames.Except(switchNames))));
        var icons = JArray.Parse(File.ReadAllText(Path.Combine(game, "Ostranauts_Data", "StreamingAssets", "data", "pda_apps", "pda_apps.json")));
        check(icons.All(i => PdaApps.NativeNames.Contains((string)i["strName"]!)), "Every icon the game ships is one of its own app names");
        check(new[] { "strName", "strFriendlyName", "strIcon", "bHidden" }.All(p => typeof(JsonPDAAppIcon).GetProperty(p) != null), "A PDA icon entry still has a name, label, icon and hidden flag");
        check(typeof(DataHandler).GetField(nameof(DataHandler.dictPDAAppIcons))?.IsStatic == true && typeof(DataHandler).GetField(nameof(DataHandler.dictStrings))?.IsStatic == true, "The icon and string tables Framework writes are public statics");
        var tooltip = typeof(GUIPDAApp).GetMethod("SetToolTip", BindingFlags.NonPublic | BindingFlags.Instance);
        var tooltipLiterals = tooltip == null ? new List<string>() : Literals(tooltip);
        check(tooltipLiterals.Contains(PdaApps.TooltipPrefix) && tooltipLiterals.Contains(PdaApps.TitleSuffix), "The game still builds an app's tooltip keys as GUI_PDA_BUTTON_<NAME> and _TITLE");
        check(PdaApps.NameProblem(BankRules.AppName) == null, "The CREDIT app's name passes Framework's rules");
        check(typeof(GUIPDA).GetField(nameof(GUIPDA.instance))?.IsStatic == true && typeof(GUIPDA).GetProperty(nameof(GUIPDA.State))?.SetMethod?.IsPublic == true, "The PDA can be closed through its public instance and State");
        check(typeof(CrewSim).GetMethod(nameof(CrewSim.ToggleFinances), Type.EmptyTypes) != null, "The game's Finances window still opens through CrewSim.ToggleFinances");

        // ---- Loans (0.2.0) ------------------------------------------------------------------------------
        // The shipped lenders load through Framework's loader and name only places Framework's story pack knows.
        var lenderPack = DataPacks.LoadText<LenderPack>(DataPacks.ShippedText(Lenders.Source), "", BankRules.Owner, LenderSchema.Name, LenderSchema.Validate);
        var storyPack = DataPacks.LoadText<StoryPack>(File.ReadAllText(Path.Combine(repo, "mods", "PhobosFramework", "framework", "story.json")), "", "framework", StorySchema.Name, p => StorySchema.Validate(p, true));
        var bankStory = DataPacks.LoadText<StoryPack>(DataPacks.ShippedText(new DataPackSource(BankRules.Owner, BankRules.ModFolder, StorySchema.Name, typeof(Plugin).Assembly, "PhobosBank.story.json")),
            "", BankRules.Owner, StorySchema.Name, p => StorySchema.Validate(p, false));
        // With the game's own items and conditions, as Framework builds it in play (Banking 0.4.0's story pack).
        var library = StoryLibrary.Build(new[] { ("framework", storyPack), ("bank", bankStory) }, null, _ => true,
            id => DataHandler.dictCOs.ContainsKey(id), c => DataHandler.dictConds.ContainsKey(c));
        check(library.Problems.Count == 0 && library.Arcs.Count == bankStory.arcs.Count, "The embedded Banking story pack loads whole against the game's data: " + string.Join("; ", library.Problems));
        check(bankStory.arcs.Values.SelectMany(a => a.steps).SelectMany(s => StoryLibrary.Outcomes(s)).SelectMany(o => o?.standing ?? new List<StoryStandingChange>()).All(c => FactionKnown(game, c.faction)),
            "Every faction a Banking letter changes standing with is one of the game's");
        check(lenderPack.lenders.Count > 0 && lenderPack.lenders.All(l => LenderSchema.Unknown(l.Value, library) == null), "The embedded lenders pack loads and every lender's home is a known place");
        check(lenderPack.lenders.Values.SelectMany(l => l.requires?.standing ?? new List<StoryStanding>()).All(s => FactionKnown(game, s.faction)), "Every faction a lender asks standing with is one of the game's");
        // The game pays a loan down when a paid bill's description holds the loan's: an interest bill must never do so.
        Ledger.Init(Array.Empty<JsonLedgerLI>());
        var mortgages = (List<LedgerLI>)typeof(Ledger).GetField("aMortgage", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        string loanDesc = LoanRules.LoanDescription("Corvane Mutual loan", 3), otherDesc = LoanRules.LoanDescription("Corvane Mutual loan", 30);
        var loan3 = new LedgerLI("Corvane Mutual", "player", 50000, loanDesc, Ledger.CURRENCY, 1000, LedgerLI.Frequency.Mortgage);
        var loan30 = new LedgerLI("Corvane Mutual", "player", 80000, otherDesc, Ledger.CURRENCY, 1000, LedgerLI.Frequency.Mortgage);
        mortgages.Add(loan3); mortgages.Add(loan30);
        LedgerLI Bill(string desc) => new("Corvane Mutual", "player", 100, desc, Ledger.CURRENCY, 2000, LedgerLI.Frequency.OneTime);
        string remaining = strings["GUI_FINANCE_MORTGAGE02"];
        check(Ledger.GetMortgageForPayment(Bill(loanDesc + remaining + "49,000.00")) == loan3 && Ledger.GetMortgageForPayment(Bill(strings["GUI_FINANCE_OVERDUE"] + loanDesc + remaining + "1.00")) == loan3,
            "The game matches a loan's instalment, overdue or not, to its loan");
        check(Ledger.GetMortgageForPayment(Bill(otherDesc + remaining + "79,000.00")) == loan30, "Loan 30's instalment pays loan 30, never loan 3");
        check(Ledger.GetMortgageForPayment(Bill("Interest to Corvane Mutual for 3 shifts on $49,000.00")) == null && Ledger.GetMortgageForPayment(Bill("Interest to Corvane Mutual")) == null,
            "Paying an interest bill never pays the loan down");
        check(Ledger.GetMortgageForShip("OKLG-1234") == null, "A cash loan is never taken for a ship's mortgage at a sale");
        mortgages.Clear();
        check(typeof(Ledger).GetMethod(nameof(Ledger.RecordTransaction)) != null && typeof(Ledger).GetMethod(nameof(Ledger.AddLI), new[] { typeof(LedgerLI) }) != null,
            "The ledger calls a loan uses are still there");

        // ---- Credit lines (0.6.0) ----------------------------------------------------------------------
        // The game's own Prepay re-spreads a balance by resetting the line's start time; a draw does the same.
        var prepay = typeof(LedgerLI).Assembly.GetType("Ostranauts.UI.Finance.PrepayWindow");
        check(prepay != null && prepay.GetMethod("OnPrepayConfirm", BindingFlags.NonPublic | BindingFlags.Instance) != null && prepay.GetField("_mortgageLI", BindingFlags.NonPublic | BindingFlags.Instance) != null,
            "The game's Prepay window still pays a mortgage line down, the repayment a credit line relies on");
        double saved = StarSystem.fEpoch;
        try
        {
            var respread = new LedgerLI("Orrery Credit", "player", 5000, LoanRules.LoanDescription("Orrery Credit credit line", 2), Ledger.CURRENCY, 1000, LedgerLI.Frequency.Mortgage);
            StarSystem.fEpoch = 1000 + 200 * Phobos.Ostranauts.Framework.GameClock.ShiftSeconds;
            respread.fAmount += 3090; respread.fTime = StarSystem.fEpoch;
            double native = Math.Min(MathUtils.MortgagePaymentPerShift(respread), respread.fAmount), mirror = BankRules.Instalment(8090, BankRules.ShiftsLeft(0));
            check(Math.Abs(native - mirror) <= 1e-3 * mirror, $"After a draw restarts the line's term, the game's instalment is the panel's new minimum: game {native:0.00}, ours {mirror:0.00}");
        }
        finally { StarSystem.fEpoch = saved; }

        // ---- Financing at the broker (0.3.0) ------------------------------------------------------------
        var popup = typeof(Ostranauts.ShipGUIs.ShipBroker.ConfirmBuyShipPopup);
        var any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        check(popup.GetField("sldrMortgage", any)?.FieldType == typeof(UnityEngine.UI.Slider), "The broker's purchase window still holds its down-payment slider as sldrMortgage");
        check(popup.GetMethod("OnMortgageSliderchanged", any) != null, "and updates its price and payment line from the slider");
        var broker = typeof(Ostranauts.ShipGUIs.ShipBroker.GUIShipBroker);
        check(broker.GetMethod("UpdateCash", any, null, new[] { typeof(Ostranauts.Events.DTOs.ShipPurchaseDTO) }, null) != null && broker.GetField("_coUser", any)?.FieldType == typeof(CondOwner),
            "The broker still writes its mortgage in UpdateCash for the user in _coUser");
        check(typeof(GUIData).GetProperty("COSelf") != null || typeof(GUIData).GetField("COSelf") != null, "A broker window knows its kiosk (COSelf)");
        check(strings.ContainsKey("GUI_FINANCE_MORTGAGE01"), "The broker's mortgage description still starts with the game's own words");
        var kiosks = Directory.GetFiles(Path.Combine(game, "Ostranauts_Data", "StreamingAssets", "data", "condowners"), "*.json", SearchOption.AllDirectories)
            .SelectMany(f => JArray.Parse(File.ReadAllText(f)).Select(t => (string?)t["strName"])).Where(n => n != null && n!.StartsWith("ItmKiosk", StringComparison.Ordinal)).ToList();
        check(kiosks.Any(k => k!.Contains(BankRules.RealEstateKioskMark)) && kiosks.Any(k => k!.Contains("ShipBroker") && !k.Contains(BankRules.RealEstateKioskMark)),
            "The real-estate broker kiosk is told apart from the ship broker by its name");
        check(Enum.GetNames(typeof(Ostranauts.Events.DTOs.TransactionTypes)).Contains("Mortgage"), "A mortgage purchase is still its own transaction type");

        // ---- The icon ---------------------------------------------------------------------------------
        string icon = Path.Combine(repo, "mods", "PhobosBank", "images", BankRules.Icon.Replace('/', Path.DirectorySeparatorChar) + ".png");
        check(File.Exists(icon), "The CREDIT icon is in the package where the game looks: images/" + BankRules.Icon + ".png");
        var header = File.ReadAllBytes(icon).Take(26).ToArray();
        int Big(int at) => header[at] << 24 | header[at + 1] << 16 | header[at + 2] << 8 | header[at + 3];
        check(Big(16) == 256 && Big(20) == 256 && header[25] == 6, "The CREDIT icon is a 256-pixel RGBA image like the game's own");
    }

    private static bool FactionKnown(string game, string faction)
    {
        foreach (string file in Directory.GetFiles(Path.Combine(game, "Ostranauts_Data", "StreamingAssets", "data", "star_systems"), "*.json", SearchOption.AllDirectories))
            if (File.ReadAllText(file).Contains("\"" + faction + "\"")) return true;
        return false;
    }

    private static Dictionary<string, string> GameStrings(string game)
    {
        var strings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in Directory.GetFiles(Path.Combine(game, "Ostranauts_Data", "StreamingAssets", "data", "strings"), "*.json"))
            foreach (var record in JArray.Parse(File.ReadAllText(file)))
            {
                var values = record["aValues"] as JArray;
                if (values == null) continue;
                for (int i = 0; i + 1 < values.Count; i += 2) strings[(string)values[i]!] = (string)values[i + 1]!;
            }
        return strings;
    }

    // String literals loaded by a method (ldstr), read from its IL.
    private static List<string> Literals(MethodInfo method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray() ?? Array.Empty<byte>();
        var found = new List<string>();
        for (int i = 0; i + 4 < il.Length; i++)
        {
            if (il[i] != 0x72) continue;
            int token = BitConverter.ToInt32(il, i + 1);
            if ((token >> 24) != 0x70) continue;
            try { found.Add(method.Module.ResolveString(token)); i += 4; } catch (ArgumentException) { }
        }
        return found;
    }

    // Whether a method loads the given single-precision constant (ldc.r4).
    private static bool SingleConstant(MethodInfo method, float value)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray() ?? Array.Empty<byte>();
        var bytes = BitConverter.GetBytes(value);
        for (int i = 0; i + 4 < il.Length; i++)
            if (il[i] == 0x22 && il[i + 1] == bytes[0] && il[i + 2] == bytes[1] && il[i + 3] == bytes[2] && il[i + 4] == bytes[3]) return true;
        return false;
    }
}
