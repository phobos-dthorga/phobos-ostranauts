# Phobos Banking checks

`dotnet run --project tests/PhobosBank.Tests -c Release` compiles the mod's
`Core/*.cs` sources directly and checks the pure rules: the game's mortgage
instalment and the count of instalments left, mirrored from the game's own
formula (a new mortgage counts 709 under it); a paid-off balance owes nothing and
a loan past its term owes the whole balance; the 17.5% late fee; late lines
recognised by the game's own overdue and late-fee wording; and the overview's
counts, sums, ordering and redraw signature.

`tests/PhobosNative.Tests/BankNativeChecks.cs` runs with the local game: the
mirrored instalment matches the game's `MathUtils.MortgagePaymentPerShift` across
a mortgage's life, the late-fee share matches the game's figure, the private loan
list the panel reads exists with the expected type, the game's overdue and
late-fee strings exist, and Framework's PDA app prefix and tooltip keys line up
with the game's PDA. No game session is run by either suite.
