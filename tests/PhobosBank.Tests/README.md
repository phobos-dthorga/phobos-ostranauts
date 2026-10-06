# Phobos Banking checks

`dotnet run --project tests/PhobosBank.Tests -c Release` compiles the mod's
`Core/*.cs` sources against Framework and checks the pure rules:

- the game's mortgage instalment and the count of instalments left, mirrored from the
  game's own formula (a new mortgage counts 709 under it); a paid-off balance owes
  nothing and a loan past its term owes the whole balance; the 17.5% late fee; late
  lines recognised by the game's own overdue and late-fee wording; the overview's counts,
  sums, ordering, redraw signature and counted wording;
- the lenders pack (0.2.0): the shipped pack loads through Framework's loader and every
  lender names only places Framework's story pack knows; bad rates, ranges, names, offers,
  homes, tiers and unknown fields are refused; every lender's story flags are valid;
- loans: interest per shift change, what a whole loan costs if paid on time (printed for
  each shipped lender), headroom, amounts and steps; a loan's ledger description never
  occurs inside another loan's instalment or an interest bill, in the English wording;
- the loan book: loans, a ship loan and a pre-approval round-trip; unknown fields and
  unreadable loans are kept exactly; every saved value passes the save store's rules; a
  lost counter never reuses a loan number.

`tests/PhobosNative.Tests/BankNativeChecks.cs` runs with the local game: the mirrored
instalment matches the game's `MathUtils.MortgagePaymentPerShift` across a mortgage's
life, the late-fee share matches the game's figure, the private loan list the panel reads
exists with the expected type, the game's overdue and late-fee strings exist, Framework's
PDA app prefix and tooltip keys line up with the game's PDA, the embedded lenders pack
loads and asks standing only with the game's own factions, and the game's own
`Ledger.GetMortgageForPayment` matches each loan's instalments to that loan alone and never
takes an interest bill for an instalment. No game session is run by either suite.
