# Phobos Exchange checks

`dotnet run --project tests/PhobosExchange.Tests -c Release -p:OstranautsPath=<game>` compiles the
mod's `Core/*.cs` sources against Framework and checks the market with no game running:

- stable noise: the same save, company, step and stream always give the same number; normal
  draws have the right mean, variance and tails; the inverse normal matches known quantiles;
- the exact process maths: the series used for short steps agree with the direct forms, a
  one-minute step never produces a negative variance, a trend phase has its stated spread,
  a year's exact step has the variance the return guard uses, and phases keep their
  direction over days (momentum);
- the exchange pack: a fixture loads with its defaults; repeated or malformed tickers, a
  drift above the cap, phases strong enough to break the return guard, unknown sectors,
  malformed stations and categories, zero-weight drivers, unsafe names and unknown fields
  are refused;
- the model: the same seed backfills the same two years of history ending at the pack's
  prices; minute-by-minute stepping, one catch-up and any chunking give bit-identical prices,
  also across a save and load halfway; the clock never steps backwards; a change to the
  pack never makes a price jump; a newly added company opens at its price; a company the
  pack drops keeps its saved state;
- time jumps of a minute, an hour, a day, three days, a month, a year and fifty years: each
  stays within the fixed step bound, lands at the same result every time, keeps prices
  finite and history within its caps (the times printed are for information only);
- the saved record: every key and value passes the save store's rules, it stays under
  16,000 characters after 400 game days, it round-trips exactly, history round-trips to
  0.005%, and fields from a newer version are kept untouched while the rest still works;
- trading: impact grows with the square root of the order; selling straight back always
  loses; each refusal (no shares, order too large, holding cap, cash, not held, too small)
  is reported; the most the player can buy is exactly affordable; a partial sale takes the
  average cost off the basis;
- alerts fire once per crossing, also inside a catch-up; a sudden jump is reported as a
  move with its cause, phases turn and are named by the phase that turned them, and each
  company is reported at most once per cooldown;
- the owner's drift guard: the expected-return cap is at most half the yearly cost of the
  cheapest loan in Phobos Banking's shipped lenders file, and over ten seeds and three game
  years, borrowing at that rate to buy and hold loses on average. Following the trend is
  printed as a balance figure (hidden phase and what the weekly chart shows), not checked.
