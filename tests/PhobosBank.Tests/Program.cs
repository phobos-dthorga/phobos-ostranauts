using System;
using System.Linq;
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

Console.WriteLine($"Phobos Banking checks passed: {checks}.");
