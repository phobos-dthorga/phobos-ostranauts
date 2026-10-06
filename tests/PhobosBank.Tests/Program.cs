using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Story;
using PhobosBank.Core;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
bool Near(double a, double b, double tolerance = 1e-9) => Math.Abs(a - b) <= tolerance * Math.Max(1, Math.Abs(b));

// ---- The game's mortgage figures ------------------------------------------------------------------
// The game counts whole days of four shifts, then whole shifts in what is left: a new mortgage has 709 instalments
// to run under this count, not the 720 its constant names (177 days of 87,658.125 s, then one more shift).
Check(BankRules.ShiftsLeft(0) == 709, "a new game mortgage counts 709 instalments left");
Check(BankRules.ShiftsLeft(BankRules.MortgageTermSeconds) == 0 && BankRules.ShiftsLeft(BankRules.MortgageTermSeconds + 1) == 0, "none are left once the term has run");
Check(BankRules.ShiftsLeft(BankRules.MortgageTermSeconds - BankRules.ShiftSeconds) == 1, "the last shift of the term leaves one");
Check(BankRules.ShiftsLeft(BankRules.GameDaySeconds) == 705, "a day later, four fewer");
double growth = Math.Pow(1 + BankRules.MortgageRatePerShift, 709);
Check(Near(BankRules.Instalment(100000, 709), 0.0021 * 100000 * growth / (growth - 1)), "the instalment is the game's amortised payment");
Check(Near(BankRules.Instalment(100000, 1), 100000), "with one instalment left the whole balance is due (never more)");
Check(BankRules.Instalment(100000, 0) == 100000, "past the term the balance is due");
Check(BankRules.Instalment(0, 100) == 0 && BankRules.Instalment(0.001, 100) == 0, "a paid-off balance owes nothing");
Check(BankRules.Instalment(100000, 709) < BankRules.Instalment(100000, 700), "fewer instalments left means larger ones");
Check(Near(BankRules.LateFee(1000), 175) && BankRules.LateFee(0) == 0, "the late fee is 17.5% of the unpaid bill");

// ---- Late lines, as the game names them -------------------------------------------------------------
const string Overdue = "<color=#D63900FF>(Overdue)</color> ", LateFee = "<color=#D63900FF>(Late Payment Fee)</color> For missed payment on ";
Check(BankRules.IsLate(Overdue + "Ship mortgage", Overdue, LateFee), "an overdue instalment is late");
Check(BankRules.IsLate(LateFee + "2048-01-01", Overdue, LateFee), "a late fee is late");
Check(!BankRules.IsLate("Docking fee", Overdue, LateFee) && !BankRules.IsLate(null, Overdue, LateFee), "an ordinary bill is not");
Check(!BankRules.IsLate("Docking fee", "", null), "missing game strings mark nothing late");

// ---- The summary the panel shows --------------------------------------------------------------------
Debt Line(DebtKind kind, string creditor, double amount, double since, bool late = false, double instalment = 0) => new()
{
    Kind = kind, Creditor = creditor, Amount = amount, Since = since, Late = late, Instalment = instalment,
    Id = Debt.MakeId(kind, creditor, since, creditor + amount)
};
var summary = DebtSummary.Of(new[]
{
    Line(DebtKind.Bill, "Docking", 50, 300), Line(DebtKind.Loan, "Ogiso's Bank", 90000, 0, instalment: 140),
    Line(DebtKind.Charge, "Crew", 25, 10), Line(DebtKind.Bill, "Ogiso's Bank", 140, 500, late: true),
    Line(DebtKind.Loan, "Ship broker", 250000, 100, instalment: 380), null!
});
Check(summary.Loans == 2 && summary.Bills == 2 && summary.Charges == 1 && summary.Late == 1, "the summary counts each kind and skips missing lines");
Check(Near(summary.LoanBalance, 340000) && Near(summary.NextInstalments, 520) && Near(summary.BillsDue, 190), "the summary sums balances, instalments and bills");
Check(summary.Debts.Select(d => d.Creditor).SequenceEqual(new[] { "Ship broker", "Ogiso's Bank", "Ogiso's Bank", "Docking", "Crew" }), "loans by balance, late bills first, then charges");
Check(summary.Debts[2].Late, "the late bill leads the bills");
string before = summary.Signature;
Check(DebtSummary.Of(summary.Debts).Signature == before, "the same lines give the same signature");
summary.Debts[3].Amount += 0.01;
Check(DebtSummary.Of(summary.Debts).Signature != before, "a changed amount changes the signature, so the panel redraws");
Check(DebtSummary.Of(Array.Empty<Debt>()).Debts.Count == 0 && DebtSummary.Of(Array.Empty<Debt>()).Signature == "", "no debts, an empty summary");
Check(Debt.MakeId(DebtKind.Bill, "A", 1.5, "x") != Debt.MakeId(DebtKind.Loan, "A", 1.5, "x"), "a bill and a loan never share an id");

// ---- Counted wording (0.1.1) ------------------------------------------------------------------------
Check(BankRules.CountKey("Overview.bills", 1) == "Overview.bills_one" && BankRules.CountKey("Overview.bills", 0) == "Overview.bills" && BankRules.CountKey("Overview.bills", 16) == "Overview.bills", "exactly one takes the _one form");
Check(BankRules.LateKey(1, 1) == "Overview.late_it" && BankRules.LateKey(3, 3) == "Overview.late_all" && BankRules.LateKey(16, 1) == "Overview.late_one" && BankRules.LateKey(16, 14) == "Overview.late_some", "late bills are counted as the overview words them");
string repo = AppContext.BaseDirectory;
while (!Directory.Exists(Path.Combine(repo, "translations", "PhobosBank"))) repo = Path.GetDirectoryName(repo.TrimEnd(Path.DirectorySeparatorChar))!;
var english = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(repo, "translations", "PhobosBank", "en.json"))).RootElement;
bool Has(string key) => english.TryGetProperty(key, out _);
foreach (string key in new[] { "Overview.loans", "Overview.bills", "Overview.charges", "Loan.instalment" })
    Check(Has(key) && Has(BankRules.CountKey(key, 1)), "both counted forms exist: " + key);
foreach (var (bills, late) in new[] { (1, 1), (3, 3), (16, 1), (16, 14) }) Check(Has(BankRules.LateKey(bills, late)), "late wording exists for " + bills + "/" + late);
foreach (var every in Enum.GetNames(typeof(ChargeEvery))) Check(Has("Every." + every), "charge frequency worded: " + every);

// ---- The lenders pack (0.2.0) ---------------------------------------------------------------------------
string shippedLenders = File.ReadAllText(Path.Combine(repo, "mods", "PhobosBank", "framework", "lenders.json"));
LenderPack LoadLenders(string json) => DataPacks.LoadText<LenderPack>(json, "", BankRules.Owner, LenderSchema.Name, LenderSchema.Validate);
bool RefusedLenders(string json) { try { LoadLenders(json); return false; } catch (Exception ex) when (ex is ArgumentException || ex is FormatException) { return true; } }
string WithLender(string find, string replace) { Check(shippedLenders.Contains(find), "fixture has " + find); return shippedLenders.Replace(find, replace); }
var lenderPack = LoadLenders(shippedLenders);
Check(lenderPack.lenders.Count >= 3 && lenderPack.lenders.Values.All(l => l.accredited), "the shipped pack holds the accredited lenders");
Check(RefusedLenders(WithLender("\"ratePerShift\": 0.00025", "\"ratePerShift\": 0")), "a lender charges some interest");
Check(RefusedLenders(WithLender("\"ratePerShift\": 0.00025", "\"ratePerShift\": 0.5")), "and not a ruinous rate");
Check(RefusedLenders(WithLender("\"minPrincipal\": 5000,\n      \"maxPrincipal\": 250000", "\"minPrincipal\": 250000,\n      \"maxPrincipal\": 5000")), "the smallest loan is below the most owed");
Check(RefusedLenders(WithLender("\"name\": \"Corvane Mutual\"", "\"name\": \"Corvane, Mutual\"")), "a name cannot hold a separator the ledger or the save uses");
Check(RefusedLenders(WithLender("\"cash\",\n        \"ship\"", "\"gold\"")), "offers are cash, ship or home");
Check(RefusedLenders(WithLender("\"home\": \"oklg\"", "\"home\": \"OKLG\"")), "a home is a story place key");
Check(RefusedLenders(WithLender("\"atLeast\": \"neutral\"", "\"atLeast\": \"adored\"")), "requirements are checked by the story validator");
Check(RefusedLenders(WithLender("\"corvane-mutual\": {", "\"corvane-mutual\": { \"colour\": \"blue\",")), "an unknown field is refused");
Check(lenderPack.lenders.Keys.All(id => StorySchema.IsId(LoanRules.Flag(id, LoanRules.Borrowed)) && StorySchema.IsId(LoanRules.Flag(id, LoanRules.Late))), "every lender's story flags are valid flag ids");
Check(StorySchema.IsId(LoanRules.Flag(new string('a', LenderSchema.MaxIdLength), LoanRules.Borrowed)), "the longest lender id still makes a valid flag");
// Every lender's home, person and requirements name entries Framework's own story pack has.
var story = DataPacks.LoadText<StoryPack>(File.ReadAllText(Path.Combine(repo, "mods", "PhobosFramework", "framework", "story.json")), "", "framework", StorySchema.Name, p => StorySchema.Validate(p, true));
var library = StoryLibrary.Build(new[] { ("framework", story) }, null, _ => true, null, null);
foreach (var pair in lenderPack.lenders) Check(LenderSchema.Unknown(pair.Value, library) == null, "lender " + pair.Key + " names only known places and people");
Check(LenderSchema.Unknown(new LenderEntry { home = "atlantis-deep" }, library) != null, "an unknown home leaves a lender out");

// ---- Interest and the loan rules ---------------------------------------------------------------------------
Check(LoanRules.Interest(100000, 0.00025, 1) == 25 && LoanRules.Interest(100000, 0.00025, 4) == 100 && LoanRules.Interest(0, 0.00025, 4) == 0 && LoanRules.Interest(100000, 0.00025, 0) == 0,
    "interest is the rate on the balance for each shift change crossed");
foreach (var pair in lenderPack.lenders)
{
    double share = LoanRules.TotalInterest(100000, pair.Value.ratePerShift) / 100000;
    Console.WriteLine("  " + pair.Key + ": a whole loan costs " + share.ToString("P2") + " in interest if paid on time");
    Check(share > 0.03 && share < 0.12, "a whole loan from " + pair.Key + " costs between 3% and 12% in interest if paid on time");
}
Check(LoanRules.TotalInterest(200000, 0.0002) > LoanRules.TotalInterest(100000, 0.0002) * 1.99, "interest grows with the amount borrowed");
Check(LoanRules.Headroom(250000, 100000) == 150000 && LoanRules.Headroom(250000, 300000) == 0 && LoanRules.Headroom(250000, -5) == 250000, "headroom is the limit less what is owed");
Check(LoanRules.Clamp(12345, 5000, 250000) == 12300 && LoanRules.Clamp(1000, 5000, 250000) == 5000 && LoanRules.Clamp(900000, 5000, 250000) == 250000 && LoanRules.Clamp(9000, 5000, 4000) == 0,
    "amounts are whole hundreds within the lender's range, and nothing when it cannot lend its smallest loan");
Check(LoanRules.Step(5000, 40000) == 1000 && LoanRules.Step(5000, 250000) == 5000 && LoanRules.Step(20000, 500000) == 10000, "the amount steps by size");
string d3 = LoanRules.LoanDescription("Corvane Mutual loan", 3), d30 = LoanRules.LoanDescription("Corvane Mutual loan", 30);
Check(d3 == "Corvane Mutual loan (#3)" && !(d30 + "; Remaining: 100").Contains(d3), "loan 30's instalments never pay down loan 3");
Check(LoanRules.Clean("Pay|me, now=#1\n") == "Pay/me; now-n1", "ledger text loses the save's separators");
// The interest bill's wording, in English, never contains a loan's description: paying it must not pay the loan down.
var englishText = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(repo, "translations", "PhobosBank", "en.json"))).RootElement;
string Fmt(string key, params object[] args) => string.Format(englishText.GetProperty(key).GetString()!, args);
foreach (var name in lenderPack.lenders.Values.Select(l => l.name))
{
    string loanDesc = LoanRules.LoanDescription(Fmt("Ledger.loan", name), 7);
    foreach (string key in new[] { "Ledger.interest", "Ledger.interest_one", "Ledger.interest_plain" })
        Check(!LoanRules.Clean(Fmt(key, name, 3, "$1,000.00")).Contains(loanDesc), "an interest bill to " + name + " never reads as its loan (" + key + ")");
    Check(LenderSchema.LedgerSafe(name) && !loanDesc.Contains("|") && !loanDesc.Contains(","), "a loan's description is safe for the ledger and the save: " + loanDesc);
}

// ---- The loan book ----------------------------------------------------------------------------------
var book = new LoanBook();
var first = book.Add(new Loan { Lender = "corvane-mutual", Payee = "Corvane Mutual", Description = d3, Principal = 50000, RatePerShift = 0.00025, Opened = 1e7, BilledTo = 450, InterestBilled = 12.5 });
var second = book.Add(new Loan { Lender = "halcyon-bond", Payee = "Halcyon Bond", Description = "Mortgage on OKLG-1234", Principal = 120000, RatePerShift = 0.0002, Opened = 2e7, BilledTo = 900, Kind = LenderSchema.Ship, Collateral = "OKLG-1234", State = LoanState.Repaid, Closed = 3e7 });
book.Approval = new Approval { Lender = "aerie-savings", Kind = LenderSchema.Home, Limit = 150000, Expires = 4e7 };
var fields = book.Encode();
fields["future.field"] = "kept";
fields["loan.99"] = "2|a|b|c";
var back = LoanBook.Decode(fields);
Check(first.Number == 1 && second.Number == 2 && back.Loans.Count == 2 && back.Next == 3, "loans are numbered once and the counter survives");
var r1 = back.Loans[1]; var r2 = back.Loans[2];
Check(r1.Lender == "corvane-mutual" && r1.Payee == "Corvane Mutual" && r1.Description == d3 && r1.Principal == 50000 && r1.RatePerShift == 0.00025 && r1.BilledTo == 450 && r1.InterestBilled == 12.5 && r1.State == LoanState.Open && r1.Collateral == null,
    "a cash loan round-trips");
Check(r2.Kind == LenderSchema.Ship && r2.Collateral == "OKLG-1234" && r2.State == LoanState.Repaid && r2.Closed == 3e7, "a ship loan and its closing round-trip");
Check(back.Approval != null && back.Approval.Lender == "aerie-savings" && back.Approval.Kind == LenderSchema.Home && back.Approval.Limit == 150000 && back.Approval.Expires == 4e7, "a pre-approval round-trips");
var again = back.Encode();
Check(again["future.field"] == "kept" && again["loan.99"] == "2|a|b|c", "fields and loans this version cannot read are kept exactly");
Check(again.Values.All(v => ObjectStateStore.SafeValue(v)), "every saved value passes the save store's rules");
Check(LoanBook.Decode(new Dictionary<string, string> { ["loan.5"] = again["loan.1"] }).Next == 6, "a book that lost its counter never reuses a loan number");
Check(LoanBook.Decode(null).Loans.Count == 0 && LoanBook.Decode(new Dictionary<string, string>()).Next == 1, "no record is an empty book");
Check(book.Open.Count() == 1 && book.Open.First().Number == 1, "only running loans are open");

// ---- Financing at the broker (0.3.0) ------------------------------------------------------------------
Check(LoanRules.MinDownShare(200000, 0.35, 250000, 5000) == 0.35, "a ship within the lender's limit takes the lender's own least down payment");
Check(Near(LoanRules.MinDownShare(1000000, 0.35, 250000, 5000)!.Value, 0.75), "a dearer ship needs enough down that the rest fits the approved amount");
Check(LoanRules.MinDownShare(4000, 0.35, 250000, 5000) == null, "a price below the lender's smallest loan cannot be financed");
Check(LoanRules.MinDownShare(6000, 0.35, 250000, 5000) == null, "nor one where the lender's least down payment leaves less than its smallest loan");
Check(LoanRules.MinDownShare(100000, 0.35, 3000, 5000) == null && LoanRules.MinDownShare(0, 0.35, 250000, 5000) == null, "an approval below the smallest loan, or no price, finances nothing");
Check(LoanRules.MinDownShare(100000, 0.3, 100000, 5000) == 0.3, "the lender's own share wins when the limit covers the rest");
foreach (var pair in lenderPack.lenders.Where(l => l.Value.offers.Contains(LenderSchema.Ship) || l.Value.offers.Contains(LenderSchema.Home)))
    Check(pair.Value.minDownShare < 0.5, pair.Key + " finances a purchase from less than the broker's own 50% down");

Console.WriteLine($"Phobos Banking checks passed: {checks}.");
